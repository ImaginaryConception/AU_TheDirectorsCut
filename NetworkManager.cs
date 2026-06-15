using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using AU_TheDirectorsCut.Utils;

namespace AU_TheDirectorsCut
{
    public static class NetworkManager
    {

        public static float LastKillRpcSentAt { get; private set; } = float.NegativeInfinity;
        public static string? LastKillRpcDescription { get; private set; }

        public static void Initialize() =>
            Plugin.Log?.LogInfo("[NetworkManager] Initialisé.");

        

        public static List<PlayerControl> Alive() =>
            PlayerControl.AllPlayerControls.ToArray()
                .Where(p => p?.Data != null && !p.Data.IsDead && !p.Data.Disconnected)
                .ToList();
 
        private static PlayerControl FindById(byte id) =>
            PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p?.PlayerId == id);

        private static bool IsHost() =>
            AmongUsClient.Instance?.AmHost == true;

        private static void Log(string fn, Exception e) =>
            Plugin.Log?.LogError($"[{fn}] {e.Message}");


        

        public static void BlindPlayer(PlayerControl target)
        {
            if (!IsHost() || target == null) return;
            try
            {
                var gameOptions = Utils.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
                gameOptions.SetFloat(AmongUs.GameOptions.FloatOptionNames.CrewLightMod, -1.0f);
                gameOptions.SetFloat(AmongUs.GameOptions.FloatOptionNames.ImpostorLightMod, -1.0f);
                Utils.GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
            }
            catch (Exception e) { Log(nameof(BlindPlayer), e); }
        }

        public static void ResetPlayerVision(PlayerControl target)
        {
            if (!IsHost() || target == null) return;
            try
            {
                var gameOptions = Utils.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
                Utils.GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
            }
            catch (Exception e) { Log(nameof(ResetPlayerVision), e); }
        }

        public static void SetGlobalVision(float factor)
        {
            if (!IsHost()) return;
            try
            {
                foreach (var player in PlayerControl.AllPlayerControls.ToArray())
                {
                    if (player?.Data == null || player.OwnerId < 0) continue;
                    var gameOptions = Utils.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
                    gameOptions.SetFloat(AmongUs.GameOptions.FloatOptionNames.CrewLightMod,
                        gameOptions.GetFloat(AmongUs.GameOptions.FloatOptionNames.CrewLightMod) * factor);
                    gameOptions.SetFloat(AmongUs.GameOptions.FloatOptionNames.ImpostorLightMod,
                        gameOptions.GetFloat(AmongUs.GameOptions.FloatOptionNames.ImpostorLightMod) * factor);
                    Utils.GameOptions.SendGameOptionsToClient(gameOptions, player.OwnerId);
                }
            }
            catch (Exception e) { Log(nameof(SetGlobalVision), e); }
        }

        public static void ResetGlobalVision()
        {
            if (!IsHost()) return;
            try
            {
                foreach (var player in PlayerControl.AllPlayerControls.ToArray())
                {
                    if (player?.Data == null || player.OwnerId < 0) continue;
                    var gameOptions = Utils.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
                    Utils.GameOptions.SendGameOptionsToClient(gameOptions, player.OwnerId);
                }
            }
            catch (Exception e) { Log(nameof(ResetGlobalVision), e); }
        }


        

        public static void Teleport(PlayerControl player, Vector2 pos)
        {
            if (!IsHost() || player?.NetTransform == null) return;
            try
            {
                Utils.Teleporter.TeleportTo(player, pos);
            }
            catch (Exception e) { Log(nameof(Teleport), e); }
        }

        public static void SwapPlayers(PlayerControl p1, PlayerControl p2)
        {
            if (p1 == null || p2 == null) return;
            try
            {
                var a = p1.GetTruePosition();
                var b = p2.GetTruePosition();
                Utils.Teleporter.TeleportTo(p1, b);
                Utils.Teleporter.TeleportTo(p2, a);
            }
            catch (Exception e) { Log(nameof(SwapPlayers), e); }
        }

        public static void TeleportAllTo(PlayerControl target)
        {
            if (!IsHost() || target == null) return;
            var dest = target.GetTruePosition();
            foreach (var p in Alive())
            {
                if (p.PlayerId == target.PlayerId) continue;
                Utils.Teleporter.TeleportTo(p, dest + new Vector2(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f)
                ));
            }
        }

        public static void ShuffleAllPlayers()
        {
            if (!IsHost()) return;
            var players = Alive();
            var positions = players.Select(p => p.GetTruePosition()).ToList();
            var rnd = new System.Random();
            int n = positions.Count;
            while (n > 1) { n--; int k = rnd.Next(n + 1); (positions[k], positions[n]) = (positions[n], positions[k]); }
            for (int i = 0; i < players.Count; i++) Utils.Teleporter.TeleportTo(players[i], positions[i]);
        }


        

        public static void MurderPlayer(PlayerControl target)
        {
            if (!IsHost() || target == null || target.Data.IsDead) return;
            try
            {




                Plugin.Log?.LogInfo($"[NetworkManager] MurderPlayer (self-RPC) → {target.Data.PlayerName} (PlayerId={target.PlayerId}, NetId={target.NetId})");

                LastKillRpcSentAt = Time.time;
                LastKillRpcDescription = $"MurderPlayer → {target.Data.PlayerName} (PlayerId={target.PlayerId})";

                var writer = AmongUsClient.Instance.StartRpcImmediately(
                    target.NetId,
                    (byte)RpcCalls.MurderPlayer,
                    Hazel.SendOption.Reliable);
                writer.WritePacked(target.NetId);
                writer.Write(true);
                AmongUsClient.Instance.FinishRpcImmediately(writer);


                target.MurderPlayer(target, MurderResultFlags.Succeeded);
            }
            catch (Exception e) { Log(nameof(MurderPlayer), e); }
        }


        

        public static void RandomizeColors()
        {
            if (!IsHost()) return;
            var rnd = new System.Random();
            var used = new HashSet<byte>();
            foreach (var p in Alive())
            {
                byte c; do { c = (byte)rnd.Next(0, 18); } while (used.Contains(c));
                used.Add(c);
                try { p.RpcSetColor(c); } catch (Exception e) { Log("RpcSetColor", e); }
            }
        }


        // ===== /colorblinds =====
        // SetName RPC : payload = (uint32 Data.NetId)(string nom).
        private static void WName(Hazel.MessageWriter w, PlayerControl p, string n)
        { w.StartMessage(2); w.WritePacked(p.NetId); w.Write((byte)RpcCalls.SetName); w.Write(p.Data.NetId); w.Write(n); w.EndMessage(); }

        // Renomme un joueur (admin /rename) : diffusion réseau + application locale.
        public static void SetPlayerName(PlayerControl p, string name)
        {
            if (!IsHost() || p?.Data == null || string.IsNullOrEmpty(name)) return;
            try
            {
                var w = Hazel.MessageWriter.Get(Hazel.SendOption.Reliable);
                w.StartMessage(5);
                w.Write(AmongUsClient.Instance.GameId);
                WName(w, p, name);
                w.EndMessage();
                AmongUsClient.Instance.SendOrDisconnect(w);
                w.Recycle();
                p.Data.PlayerName = name;
            }
            catch (Exception e) { Log(nameof(SetPlayerName), e); }
        }

        // Met tout le monde en gris (couleur 15) et masque les noms (espace).
        // RpcSetColor applique localement + réseau ; les noms sont diffusés (tag 5)
        // puis appliqués localement côté hôte pour cohérence d'affichage.
        public static void GreyAllAndHideNames()
        {
            if (!IsHost()) return;
            try
            {
                foreach (var p in PlayerControl.AllPlayerControls.ToArray())
                {
                    if (p?.Data == null || p.Data.Disconnected) continue;
                    try { p.RpcSetColor(15); } catch (Exception e) { Log("ColorBlindColor", e); }
                }

                var w = Hazel.MessageWriter.Get(Hazel.SendOption.Reliable);
                w.StartMessage(5);
                w.Write(AmongUsClient.Instance.GameId);
                foreach (var p in PlayerControl.AllPlayerControls.ToArray())
                {
                    if (p?.Data == null || p.Data.Disconnected) continue;
                    WName(w, p, " ");
                }
                w.EndMessage();
                AmongUsClient.Instance.SendOrDisconnect(w);
                w.Recycle();

                foreach (var p in PlayerControl.AllPlayerControls.ToArray())
                {
                    if (p?.Data == null || p.Data.Disconnected) continue;
                    p.Data.PlayerName = " ";
                }
            }
            catch (Exception e) { Log(nameof(GreyAllAndHideNames), e); }
        }

        // Restaure couleurs + noms d'origine mémorisés.
        public static void RestoreColorsAndNames(Dictionary<byte, (int colorId, string name)> originals)
        {
            if (!IsHost() || originals == null) return;
            try
            {
                foreach (var kv in originals)
                {
                    var p = FindById(kv.Key);
                    if (p?.Data == null) continue;
                    try { p.RpcSetColor((byte)kv.Value.colorId); } catch (Exception e) { Log("RestoreColor", e); }
                }

                var w = Hazel.MessageWriter.Get(Hazel.SendOption.Reliable);
                w.StartMessage(5);
                w.Write(AmongUsClient.Instance.GameId);
                foreach (var kv in originals)
                {
                    var p = FindById(kv.Key);
                    if (p?.Data == null) continue;
                    WName(w, p, kv.Value.name);
                }
                w.EndMessage();
                AmongUsClient.Instance.SendOrDisconnect(w);
                w.Recycle();

                foreach (var kv in originals)
                {
                    var p = FindById(kv.Key);
                    if (p?.Data == null) continue;
                    p.Data.PlayerName = kv.Value.name;
                }
            }
            catch (Exception e) { Log(nameof(RestoreColorsAndNames), e); }
        }
    }
}
