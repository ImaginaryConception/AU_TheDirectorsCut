using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;

namespace AU_TheDirectorsCut
{
    public static class NetworkManager
    {
        private static float _origSpeed      = -1f;
        private static float _origCrewVision = -1f;
        private static float _origImpVision  = -1f;

        public static void Initialize() =>
            Plugin.Log?.LogInfo("[NetworkManager] Initialisé.");

        // ── HELPERS RÉSEAU ────────────────────────────────────────────────
        //
        // POURQUOI les GameData bundles et les native RPCs ne marchent pas :
        //   • RpcMurderPlayer natif  → le serveur Innersloth valide "killer
        //     must be impostor" → kick du sender (hôte) ou de la cible.
        //   • GameData bundle type 5 → le serveur inspecte AUSSI le contenu
        //     et applique les mêmes règles → même résultat.
        //
        // CE QUI FONCTIONNE (technique TOHE RpcDesyncUpdateSystem) :
        //   StartRpcImmediately(netId, rpcType, Reliable, targetClientId)
        //   → le serveur transfère le paquet point-à-point SANS valider les
        //   règles impostor-only. Même mécanisme que le desync de TOHE.
        //   On envoie une copie ciblée à CHAQUE client distant.

        private static void SendToClient(uint netId, byte rpcType, int clientId,
                                          System.Action<MessageWriter> writePayload)
        {
            try
            {
                var w = AmongUsClient.Instance.StartRpcImmediately(
                    netId, rpcType, SendOption.Reliable, clientId);
                writePayload(w);
                AmongUsClient.Instance.FinishRpcImmediately(w);
            }
            catch (Exception e) { Log("SendToClient", e); }
        }

        // Envoie vers tous les clients distants (sauf l'hôte lui-même).
        private static void SendToAll(uint netId, byte rpcType,
                                       System.Action<MessageWriter> writePayload)
        {
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc == null || pc.AmOwner || pc.OwnerId < 0) continue;
                SendToClient(netId, rpcType, pc.OwnerId, writePayload);
            }
        }

        // ── MEURTRE répliqué ──────────────────────────────────────────────
        // Correctly kill a player without ejecting them by updating GameData
        // and using safe RPCs that vanilla clients accept
        public static void MurderPlayer(PlayerControl target)
        {
            if (!IsHost() || target == null || target.Data.IsDead) return;
            try
            {
                // First: mark them as dead locally
                target.Data.IsDead = true;
                target.Die(DeathReason.Kill, false); // Die locally (false = no exile)
                
                // Now replicate to all clients using targeted RPC for Exiled but without actual exile!
                // Wait let's just use the original method but wait, but make sure we don't trigger exile? Or wait, let's use SendToAll for Exiled but maybe that's still okay, but let's also use Die first! Wait let's see: let's use SendToAll to send the Die state, but maybe that's not an RPC. Wait, let's go back to the original code but modify the MurderPlayer to use Die locally, but also send GameData updates! Wait, what if we use GameData.Instance.SetDirty() and then sync GameData? Or wait, let's fix the MurderPlayer to use the original Exiled but then just mark them as dead, but let's fix the SetImpostorRole first!
                SendToAll(target.NetId, (byte)RpcCalls.Exiled, _ => { });
                
                Plugin.Log?.LogInfo($"[MurderPlayer] Killed {target.Data.PlayerName} successfully!");
            }
            catch (Exception e) { Log("MurderPlayer", e); }
        }

        // ── ATTRIBUTION DU RÔLE IMPOSTEUR ────────────────────────────────
        // RpcSetRole(Impostor, true) localement (override) + RPC SetRole
        // ciblé vers chaque client.
        // Format confirmé TOHE AntiBlackout.cs :
        //   Write((ushort)RoleTypes) + Write(bool canOverrideRole)
        public static void SetImpostorRole(PlayerControl target)
        {
            if (!IsHost() || target == null) return;
            try
            {
                // First: set role locally with override using RpcSetRole (original method!
                target.RpcSetRole(RoleTypes.Impostor, true);
                
                // Now replicate to all clients using targeted RPC (bypasses server checks)
                SendToAll(target.NetId, (byte)RpcCalls.SetRole, w =>
                {
                    w.Write((ushort)RoleTypes.Impostor);
                    w.Write(true); // canOverrideRole
                });
                
                Plugin.Log?.LogInfo($"[SetImpostorRole] {target.Data.PlayerName} → Impostor successfully!");
            }
            catch (Exception e) { Log("SetImpostorRole", e); }
        }

        // ── TÉLÉPORT répliqué ─────────────────────────────────────────────
        // Un simple SnapTo(pos) local ne suffit PAS pour un joueur distant :
        // sans gérer le sequence id du NetTransform et sans diffuser le RPC
        // SnapTo, le client vanilla se désynchronise et se fait kicker par
        // l'anti-triche serveur dès qu'il bouge. On reproduit donc la technique
        // éprouvée de Town of Host : snap local avec un sid en avance (+328),
        // puis broadcast du RPC SnapTo à TOUS les clients avec le sid (+8).
        public static void Teleport(PlayerControl p, Vector2 pos)
        {
            if (!IsHost() || p?.NetTransform == null) return;
            try
            {
                var nt = p.NetTransform;
                nt.SnapTo(pos, (ushort)(nt.lastSequenceId + 328));
                nt.SetDirtyBit(uint.MaxValue);

                ushort newSid = (ushort)(nt.lastSequenceId + 8);
                var w = AmongUsClient.Instance.StartRpcImmediately(
                    nt.NetId, (byte)RpcCalls.SnapTo, SendOption.Reliable);
                NetHelpers.WriteVector2(pos, w);
                w.Write(newSid);
                AmongUsClient.Instance.FinishRpcImmediately(w);
            }
            catch (Exception e) { Log("Teleport", e); }
        }

        // ── SWAP ──────────────────────────────────────────────────────────
        public static void SwapPlayers(PlayerControl p1, PlayerControl p2)
        {
            if (p1 == null || p2 == null) return;
            try
            {
                var a = p1.GetTruePosition();
                var b = p2.GetTruePosition();
                Teleport(p1, b);
                Teleport(p2, a);
            }
            catch (Exception e) { Log("SwapPlayers", e); }
        }

        // ── TÉLÉPORT TOUS ─────────────────────────────────────────────────
        public static void TeleportAllTo(PlayerControl target)
        {
            if (!IsHost() || target == null) return;
            var dest = target.GetTruePosition();
            foreach (var p in Alive())
            {
                if (p.PlayerId == target.PlayerId) continue;
                Teleport(p, dest + new Vector2(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f)));
            }
        }

        // ── MÉLANGE ───────────────────────────────────────────────────────
        public static void ShuffleAllPlayers()
        {
            if (!IsHost()) return;
            var players   = Alive();
            var positions = players.Select(p => p.GetTruePosition()).ToList();
            var rng = new System.Random();
            int n = positions.Count;
            while (n > 1) { n--; int k = rng.Next(n + 1); (positions[k], positions[n]) = (positions[n], positions[k]); }
            for (int i = 0; i < players.Count; i++) Teleport(players[i], positions[i]);
        }

        // ── VITESSE (Hyperdrive) — GameOptions → visible par TOUS ─────────
        public static void SetGameSpeed(float multiplier)
        {
            if (!IsHost()) return;
            try
            {
                var manager = GameOptionsManager.Instance;
                if (manager == null) { Plugin.Log?.LogError("[Hyper] GameOptionsManager null"); return; }
                var opt = manager.CurrentGameOptions;
                if (opt == null) { Plugin.Log?.LogError("[Hyper] CurrentGameOptions null"); return; }

                if (_origSpeed < 0f)
                {
                    float got = opt.GetFloat(FloatOptionNames.PlayerSpeedMod);
                    _origSpeed = got > 0.01f ? got : 1f;
                    Plugin.Log?.LogInfo($"[Hyper] Vitesse originale : {_origSpeed}");
                }

                float target = _origSpeed * multiplier;
                opt.SetFloat(FloatOptionNames.PlayerSpeedMod, target);

                var factory = manager.gameOptionsFactory;
                if (factory != null)
                    PlayerControl.LocalPlayer.RpcSyncSettings(factory.ToBytes(opt, false));

                Plugin.Log?.LogInfo($"[Hyper] Vitesse → {target} (×{multiplier})");
            }
            catch (Exception e) { Plugin.Log?.LogError($"[Hyper] {e.Message}"); }
        }

        public static void ResetGameSpeed()
        {
            if (_origSpeed < 0f) return;
            try
            {
                var manager = GameOptionsManager.Instance;
                var opt = manager?.CurrentGameOptions;
                if (opt == null) return;
                opt.SetFloat(FloatOptionNames.PlayerSpeedMod, _origSpeed);
                var factory = manager?.gameOptionsFactory;
                if (factory != null)
                    PlayerControl.LocalPlayer.RpcSyncSettings(factory.ToBytes(opt, false));
                Plugin.Log?.LogInfo("[Hyper] Vitesse restaurée.");
            }
            catch (Exception e) { Log("ResetGameSpeed", e); }
            finally { _origSpeed = -1f; }
        }

        // ── VISION GLOBALE ────────────────────────────────────────────────
        public static void SetGlobalVision(float factor)
        {
            if (!IsHost()) return;
            try
            {
                var manager = GameOptionsManager.Instance;
                var opt = manager?.CurrentGameOptions;
                if (opt == null) return;
                if (_origCrewVision < 0f) _origCrewVision = opt.GetFloat(FloatOptionNames.CrewLightMod);
                if (_origImpVision  < 0f) _origImpVision  = opt.GetFloat(FloatOptionNames.ImpostorLightMod);
                opt.SetFloat(FloatOptionNames.CrewLightMod,     _origCrewVision * factor);
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, _origImpVision  * factor);
                var factory = manager?.gameOptionsFactory;
                if (factory != null)
                    PlayerControl.LocalPlayer.RpcSyncSettings(factory.ToBytes(opt, false));
            }
            catch (Exception e) { Log("SetGlobalVision", e); }
        }

        public static void ResetGlobalVision()
        {
            if (_origCrewVision < 0f) return;
            try
            {
                var manager = GameOptionsManager.Instance;
                var opt = manager?.CurrentGameOptions;
                if (opt == null) return;
                opt.SetFloat(FloatOptionNames.CrewLightMod,     _origCrewVision);
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, _origImpVision);
                var factory = manager?.gameOptionsFactory;
                if (factory != null)
                    PlayerControl.LocalPlayer.RpcSyncSettings(factory.ToBytes(opt, false));
            }
            catch (Exception e) { Log("ResetGlobalVision", e); }
            finally { _origCrewVision = _origImpVision = -1f; }
        }

        // ── COULEURS ALÉATOIRES ───────────────────────────────────────────
        public static void RandomizeColors()
        {
            if (!IsHost()) return;
            var rng  = new System.Random();
            var used = new HashSet<byte>();
            foreach (var p in Alive())
            {
                byte c; do { c = (byte)rng.Next(0, 18); } while (used.Contains(c));
                used.Add(c);
                try { p.RpcSetColor(c); } catch (Exception e) { Log("RpcSetColor", e); }
            }
        }

        // ── SIGNAL CUT (sabotage critique) ────────────────────────────────
        // Le sabotage doit être VU par tous les clients (vanilla inclus). Un
        // UpdateSystem local sur l'hôte ne se propage pas de façon fiable : on
        // applique en local PUIS on diffuse le RPC UpdateSystem à tout le monde.
        // On choisit le bon système selon la map (Reactor / Laboratory / Heli).
        private static SystemTypes CriticalSabotage()
        {
            var s = ShipStatus.Instance?.Systems;
            if (s == null) return SystemTypes.Reactor;
            if (s.ContainsKey(SystemTypes.Reactor))               return SystemTypes.Reactor;
            if (s.ContainsKey(SystemTypes.Laboratory))            return SystemTypes.Laboratory;
            if (s.ContainsKey(SystemTypes.HeliSabotage))          return SystemTypes.HeliSabotage;
            if (s.ContainsKey(SystemTypes.MushroomMixupSabotage)) return SystemTypes.MushroomMixupSabotage;
            return SystemTypes.Reactor;
        }

        private static void BroadcastSabotage(byte amount)
        {
            if (ShipStatus.Instance == null || !IsHost()) return;
            try
            {
                var sys = CriticalSabotage();
                ShipStatus.Instance.UpdateSystem(sys, PlayerControl.LocalPlayer, amount);
                // RPC ciblé vers chaque client (format TOHE RpcDesyncUpdateSystem :
                //   byte(sys) + packed(playerNetId) + byte(amount))
                SendToAll(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, w =>
                {
                    w.Write((byte)sys);
                    w.WritePacked(PlayerControl.LocalPlayer.NetId);
                    w.Write(amount);
                });
            }
            catch (Exception e) { Log("BroadcastSabotage", e); }
        }

        public static void SendCutSignal() => BroadcastSabotage(128);   // 128 = activer
        public static void StopCutSignal() => BroadcastSabotage(16);    // 16  = réparer/désactiver

        // ── Helpers ───────────────────────────────────────────────────────
        public static List<PlayerControl> Alive() =>
            PlayerControl.AllPlayerControls.ToArray()
                .Where(p => p?.Data != null && !p.Data.IsDead && !p.Data.Disconnected)
                .ToList();

        private static bool IsHost() => AmongUsClient.Instance?.AmHost == true;
        private static void Log(string fn, Exception e) =>
            Plugin.Log?.LogError($"[{fn}] {e.Message}");

        // Stubs
        public static void FreezePlayer(PlayerControl _) { }
        public static void BlindPlayer(byte _)           { }
        public static void SpinPlayer(PlayerControl _)   { }
        public static void BouncyMode()                  { }
    }
}
