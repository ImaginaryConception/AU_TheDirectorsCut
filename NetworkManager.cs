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

        // ── MEURTRE répliqué ──────────────────────────────────────────────
        public static void MurderPlayer(PlayerControl target)
        {
            if (!IsHost() || target == null || target.Data.IsDead) return;
            try { target.RpcMurderPlayer(target, true); }
            catch (Exception e) { Log("MurderPlayer", e); }
        }

        // ── TÉLÉPORT répliqué ─────────────────────────────────────────────
        public static void Teleport(PlayerControl p, Vector2 pos)
        {
            if (p?.NetTransform == null) return;
            try { p.NetTransform.SnapTo(pos); }
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
                p1.NetTransform.SnapTo(b);
                p2.NetTransform.SnapTo(a);
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

        // ── SIGNAL CUT (réacteur) ─────────────────────────────────────────
        public static void SendCutSignal()
        {
            if (ShipStatus.Instance == null) return;
            try { ShipStatus.Instance.UpdateSystem(SystemTypes.Reactor, PlayerControl.LocalPlayer, 128); }
            catch { }
        }

        public static void StopCutSignal()
        {
            if (ShipStatus.Instance == null) return;
            try { ShipStatus.Instance.UpdateSystem(SystemTypes.Reactor, PlayerControl.LocalPlayer, 16); }
            catch { }
        }

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
