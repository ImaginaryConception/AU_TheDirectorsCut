using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using AU_TheDirectorsCut.Hydra;

namespace AU_TheDirectorsCut
{
    public static class NetworkManager
    {
        public static void Initialize() =>
            Plugin.Log?.LogInfo("[NetworkManager] Initialisé.");

        // ── HELPERS ────────────────────────────────────────────────────────

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


        // ── BLIND / VISION (HYDRA) ─────────────────────────────────────────

        public static void BlindPlayer(PlayerControl target)
        {
            if (!IsHost() || target == null) return;
            try
            {
                var gameOptions = Hydra.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
                gameOptions.SetFloat(AmongUs.GameOptions.FloatOptionNames.CrewLightMod, -1.0f);
                gameOptions.SetFloat(AmongUs.GameOptions.FloatOptionNames.ImpostorLightMod, -1.0f);
                Hydra.GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
            }
            catch (Exception e) { Log(nameof(BlindPlayer), e); }
        }

        public static void ResetPlayerVision(PlayerControl target)
        {
            if (!IsHost() || target == null) return;
            try
            {
                var gameOptions = Hydra.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
                Hydra.GameOptions.SendGameOptionsToClient(gameOptions, target.OwnerId);
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
                    var gameOptions = Hydra.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
                    gameOptions.SetFloat(AmongUs.GameOptions.FloatOptionNames.CrewLightMod,
                        gameOptions.GetFloat(AmongUs.GameOptions.FloatOptionNames.CrewLightMod) * factor);
                    gameOptions.SetFloat(AmongUs.GameOptions.FloatOptionNames.ImpostorLightMod,
                        gameOptions.GetFloat(AmongUs.GameOptions.FloatOptionNames.ImpostorLightMod) * factor);
                    Hydra.GameOptions.SendGameOptionsToClient(gameOptions, player.OwnerId);
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
                    var gameOptions = Hydra.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
                    Hydra.GameOptions.SendGameOptionsToClient(gameOptions, player.OwnerId);
                }
            }
            catch (Exception e) { Log(nameof(ResetGlobalVision), e); }
        }


        // ── TELEPORT (HYDRA) ───────────────────────────────────────────────

        public static void Teleport(PlayerControl player, Vector2 pos)
        {
            if (!IsHost() || player?.NetTransform == null) return;
            try
            {
                Hydra.Teleporter.TeleportTo(player, pos);
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
                Hydra.Teleporter.TeleportTo(p1, b);
                Hydra.Teleporter.TeleportTo(p2, a);
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
                Hydra.Teleporter.TeleportTo(p, dest + new Vector2(
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
            for (int i = 0; i < players.Count; i++) Hydra.Teleporter.TeleportTo(players[i], positions[i]);
        }


        // ── MURDER (HYDRA) ─────────────────────────────────────────────────

        public static void MurderPlayer(PlayerControl target)
        {
            if (!IsHost() || target == null || target.Data.IsDead) return;
            try
            {
                PlayerControl.LocalPlayer.RpcMurderPlayer(target, true);
            }
            catch (Exception e) { Log(nameof(MurderPlayer), e); }
        }


        // ── RANDOM COLORS (HYDRA) ──────────────────────────────────────────

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
    }
}
