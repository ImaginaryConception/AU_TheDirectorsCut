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

        public static void Initialize()
        {
            _pendingTPs.Clear();
            Plugin.Log?.LogInfo("[NetworkManager] Initialisé.");
        }

        private struct PendingTP { public PlayerControl Player; public Vector2 Pos; public float Delay; }
        private static readonly List<PendingTP> _pendingTPs = new();

        public static void Tick(float dt)
        {
            for (int i = _pendingTPs.Count - 1; i >= 0; i--)
            {
                var p = _pendingTPs[i];
                float remaining = p.Delay - dt;
                if (remaining <= 0f)
                {
                    try
                    {
                        if (p.Player != null && p.Player.Data != null && !p.Player.Data.IsDead)
                            Utils.Teleporter.TeleportTo(p.Player, p.Pos);
                    }
                    catch (Exception e) { Log(nameof(Tick), e); }
                    _pendingTPs.RemoveAt(i);
                }
                else
                {
                    _pendingTPs[i] = new PendingTP { Player = p.Player, Pos = p.Pos, Delay = remaining };
                }
            }
        }

        public static void ClearPendingTPs() => _pendingTPs.Clear();

        

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


        

        public static void ForceExitVent(PlayerControl player)
        {
            if (player == null || ShipStatus.Instance == null) return;
            if (!player.inVent) return;
            try
            {
                Vent nearest = null;
                float best = float.MaxValue;
                Vector2 pos = player.GetTruePosition();
                foreach (var v in ShipStatus.Instance.AllVents)
                {
                    if (v == null) continue;
                    float d = Vector2.Distance(pos, (Vector2)v.transform.position);
                    if (d < best) { best = d; nearest = v; }
                }
                if (nearest != null && player.MyPhysics != null)
                    player.MyPhysics.RpcBootFromVent(nearest.Id);
            }
            catch (Exception e) { Log(nameof(ForceExitVent), e); }
        }

        private static void TeleportSafe(PlayerControl player, Vector2 pos)
        {
            if (player.inVent)
            {
                ForceExitVent(player);
                _pendingTPs.Add(new PendingTP { Player = player, Pos = pos, Delay = 1.5f });
            }
            else
            {
                Utils.Teleporter.TeleportTo(player, pos);
            }
        }

        public static void Teleport(PlayerControl player, Vector2 pos)
        {
            if (!IsHost() || player?.NetTransform == null) return;
            try
            {
                TeleportSafe(player, pos);
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
                TeleportSafe(p1, b);
                TeleportSafe(p2, a);
            }
            catch (Exception e) { Log(nameof(SwapPlayers), e); }
        }

        public static void TeleportAllTo(PlayerControl target)
        {
            if (!IsHost() || target == null) return;
            ForceExitVent(target);
            var dest = target.GetTruePosition();
            foreach (var p in Alive())
            {
                if (p.PlayerId == target.PlayerId) continue;
                TeleportSafe(p, dest + new Vector2(
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
            for (int i = 0; i < players.Count; i++) TeleportSafe(players[i], positions[i]);
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


        private static void WName(Hazel.MessageWriter w, PlayerControl p, string n)
        { w.StartMessage(2); w.WritePacked(p.NetId); w.Write((byte)RpcCalls.SetName); w.Write(p.Data.NetId); w.Write(n); w.EndMessage(); }

        public static void ApplyNameLocal(PlayerControl p, string name)
        {
            if (p?.Data == null) return;
            p.Data.PlayerName = name;
            try { if (p.cosmetics != null && p.cosmetics.nameText != null) p.cosmetics.nameText.text = name; }
            catch (Exception e) { Log("ApplyNameLocal", e); }
        }

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
                ApplyNameLocal(p, name);
            }
            catch (Exception e) { Log(nameof(SetPlayerName), e); }
        }

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
                    WName(w, p, "Anonyme");
                }
                w.EndMessage();
                AmongUsClient.Instance.SendOrDisconnect(w);
                w.Recycle();

                foreach (var p in PlayerControl.AllPlayerControls.ToArray())
                {
                    if (p?.Data == null || p.Data.Disconnected) continue;
                    ApplyNameLocal(p, "Anonyme");
                }
            }
            catch (Exception e) { Log(nameof(GreyAllAndHideNames), e); }
        }

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
                    ApplyNameLocal(p, kv.Value.name);
                }
            }
            catch (Exception e) { Log(nameof(RestoreColorsAndNames), e); }
        }
    }
}
