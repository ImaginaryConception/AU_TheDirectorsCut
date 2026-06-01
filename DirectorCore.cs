using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace AU_TheDirectorsCut
{
    public static class DirectorCore
    {
        public static byte? DirectorPlayerId { get; private set; }
        public static bool  IsCutActive      { get; private set; }
        public static bool  PendingAutoGG    = false;

        private static int   cutStep;
        private static float cutStepTimer;
        private static readonly Dictionary<byte, Vector2> cutStartPositions = new();

        private static readonly Dictionary<string, float> _cd = new();
        private static readonly Dictionary<string, float> _cdMax = new()
        {
            ["/cut"]          = 30f,
            ["/swap"]         = 15f,
            ["/blind"]        = 25f,
            ["/darkness"]     = 35f,
            ["/freeze"]       = 30f,
            ["/spin"]         = 20f,
            ["/randomcolors"] = 20f,
            ["/shuffle"]      = 25f,
            ["/teleportall"]  = 20f,
        };

        private static readonly Dictionary<byte,(Vector2 pos, float rem)>                  _frozen = new();
        private static readonly Dictionary<byte,(Vector2 center, float angle, float rem)> _spin   = new();
        private static float _visionDur;

        // Données de la partie précédente (pour /gg automatique)
        public static IReadOnlyList<string> LastAlive => _lastAlive;
        public static IReadOnlyList<string> LastDead  => _lastDead;
        private static List<string> _lastAlive = new();
        private static List<string> _lastDead  = new();

        public static void SnapshotEndState()
        {
            _lastAlive = PlayerControl.AllPlayerControls.ToArray()
                .Where(p => p?.Data != null && !p.Data.IsDead && !p.Data.Disconnected)
                .Select(p => p.Data.PlayerName).ToList();
            _lastDead = PlayerControl.AllPlayerControls.ToArray()
                .Where(p => p?.Data != null && p.Data.IsDead && !p.Data.Disconnected)
                .Select(p => p.Data.PlayerName).ToList();
            Plugin.Log?.LogInfo($"[DirectorCore] Snapshot — Vivants:{_lastAlive.Count} Éliminés:{_lastDead.Count}");
        }

        public static float CooldownRemaining(string cmd) =>
            _cd.TryGetValue(cmd, out float r) ? r : 0f;
        public static bool IsOnCooldown(string cmd) =>
            _cd.TryGetValue(cmd, out float r) && r > 0f;

        public static void Initialize() { Reset(); Plugin.Log?.LogInfo("[DirectorCore] Initialisé."); }

        public static void Reset()
        {
            DirectorPlayerId = null;
            IsCutActive      = false;
            PendingAutoGG    = false;
            cutStep = 0; cutStepTimer = 0f;
            cutStartPositions.Clear();
            _frozen.Clear(); _spin.Clear();
            _visionDur = 0f;
            _cd.Clear();
            NetworkManager.ResetGlobalVision();
            
            // Réinitialise le système de bienvenue
            ChatManager.ClearWelcomeSent();
        }

        public static void OnPlayerDie(PlayerControl player)
        {
            if (!AmongUsClient.Instance.AmHost || DirectorPlayerId.HasValue) return;
            DirectorPlayerId = player.PlayerId;
            SendHostMessage($"<color=#ff6b6b>{player.Data.PlayerName}</color> est le RÉALISATEUR ! (/help pour les Directives)");
        }

        public static bool IsDirector(byte id) =>
            DirectorPlayerId.HasValue && DirectorPlayerId.Value == id;

        private static bool TryCooldown(string cmd)
        {
            if (_cd.TryGetValue(cmd, out float r) && r > 0f)
            {
                SendHostMessage($"{cmd} en recharge — {Mathf.CeilToInt(r)}s restantes.");
                return false;
            }
            if (_cdMax.TryGetValue(cmd, out float max)) _cd[cmd] = max;
            return true;
        }

        public static bool TryProcessCommand(PlayerControl sender, string raw)
        {
            if (!AmongUsClient.Instance.AmHost || sender == null) return false;
            string msg = raw.Trim();
            if (!msg.StartsWith("/")) return false;
            var parts = msg.Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;
            string cmd = parts[0].ToLowerInvariant();

            bool inLobby = ShipStatus.Instance == null;

            // ── Commandes disponibles partout (lobby + jeu) ───────────────
            switch (cmd)
            {
                case "/welcome":
                case "/rules":
                    ChatManager.Queue(ChatManager.WelcomeMsg);
                    return true;

                case "/help":
                    foreach (var m in ChatManager.HelpMessages) ChatManager.Queue(m);
                    return true;

                case "/gg":
                    ChatManager.Queue(ChatManager.GenerateGGMessage());
                    return true;

                case "/players":
                    var list = PlayerControl.AllPlayerControls.ToArray()
                        .Where(p => p?.Data != null)
                        .Select(p => $"[{p.PlayerId}] {p.Data.PlayerName}{(p.Data.IsDead ? " <color=#ff6b6b>(éliminé)</color>" : "")}");
                    SendHostMessage("Joueurs : " + string.Join(" | ", list));
                    return true;

                case "/setdirector":
                    if (sender.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                    { SendHostMessage("Hôte uniquement."); return true; }
                    DirectorPlayerId = sender.PlayerId;
                    SendHostMessage($"<color=#ffd23f>{sender.Data.PlayerName}</color> est maintenant le Réalisateur !");
                    return true;

                case "/start":
                    if (sender.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                    { SendHostMessage("Hôte uniquement."); return true; }
                    try { AmongUsClient.Instance.StartGame(); }
                    catch (Exception e) { SendHostMessage(e.Message); }
                    return true;

                // ── /h[directive] — aide détaillée par commande ───────────
                case "/hcut":
                    ChatManager.Queue(
                        "<color=#ffd23f>/cut</color> — Lance le \"1, 2, 3 Soleil\".\n" +
                        "Tous doivent s'immobiliser. Bouger = éliminé.\n" +
                        "Durée : 5s — Cooldown : 30s.");
                    return true;
                case "/hswap":
                    ChatManager.Queue(
                        "<color=#ffd23f>/swap [A] [B]</color> — Échange les positions\n" +
                        "de 2 joueurs. Voir les IDs avec /players.\n" +
                        "Cooldown : 15s.");
                    return true;
                case "/hblind":
                    ChatManager.Queue(
                        "<color=#ffd23f>/blind [ID]</color> — Réduit drastiquement\n" +
                        "la vision d'un joueur pendant 8 secondes.\n" +
                        "Cooldown : 25s.");
                    return true;
                case "/hdarkness":
                    ChatManager.Queue(
                        "<color=#ffd23f>/darkness</color> — Obscurité totale\n" +
                        "pour tout le monde pendant 10 secondes.\n" +
                        "Cooldown : 35s.");
                    return true;
                case "/hfreeze":
                    ChatManager.Queue(
                        "<color=#ffd23f>/freeze [ID]</color> — Immobilise un joueur\n" +
                        "pendant 8s.\n" +
                        "Cooldown : 30s.");
                    return true;
                case "/hspin":
                    ChatManager.Queue(
                        "<color=#ffd23f>/spin [ID]</color> — Fait tourner un joueur\n" +
                        "en cercle pendant 5 secondes.\n" +
                        "Cooldown : 20s.");
                    return true;
                case "/hrandomcolors":
                    ChatManager.Queue(
                        "<color=#ffd23f>/randomcolors</color> — Attribue des couleurs\n" +
                        "aléatoires à tous les joueurs.\n" +
                        "Cooldown : 20s.");
                    return true;
                case "/hshuffle":
                    ChatManager.Queue(
                        "<color=#ffd23f>/shuffle</color> — Téléporte tous les joueurs\n" +
                        "à des positions aléatoires sur la map.\n" +
                        "Cooldown : 25s.");
                    return true;
                case "/hteleportall":
                    ChatManager.Queue(
                        "<color=#ffd23f>/teleportall [ID]</color> — Téléporte tous\n" +
                        "les joueurs vers la position d'un joueur cible.\n" +
                        "Cooldown : 20s.");
                    return true;
            }

            // ── Commandes bloquées en lobby ───────────────────────────────
            if (inLobby)
            {
                Plugin.Log?.LogInfo($"[DirectorCore] {cmd} ignoré en lobby.");
                return true;
            }

            if (!IsDirector(sender.PlayerId))
            {
                SendHostMessage($"{sender.Data.PlayerName} : tu n'es pas le Réalisateur !");
                return true;
            }

            // ── Directives Réalisateur (jeu uniquement) ───────────────────
            switch (cmd)
            {
                case "/cut":
                    if (!TryCooldown("/cut")) return true;
                    SendHostMessage("<color=#ffd23f>CUT !</color> Ne bougez plus dans 2 secondes !");
                    StartCut(); return true;

                case "/swap":
                    if (parts.Length >= 3 && byte.TryParse(parts[1], out byte a) && byte.TryParse(parts[2], out byte b))
                    {
                        if (!TryCooldown("/swap")) return true;
                        var p1 = FindById(a); var p2 = FindById(b);
                        if (p1 == null || p2 == null) { SendHostMessage("Joueur introuvable."); return true; }
                        SendHostMessage($"Échange : <color=#88ccff>{p1.Data.PlayerName}</color> ↔ <color=#88ccff>{p2.Data.PlayerName}</color> !");
                        NetworkManager.SwapPlayers(p1, p2);
                    }
                    else SendHostMessage("Usage : /swap [ID1] [ID2]");
                    return true;

                case "/blind":
                    if (parts.Length >= 2 && byte.TryParse(parts[1], out byte bid))
                    {
                        if (!TryCooldown("/blind")) return true;
                        var bt = FindById(bid);
                        if (bt == null) { SendHostMessage("Joueur introuvable."); return true; }
                        SendHostMessage($"Vision réduite pour <color=#88ccff>{bt.Data.PlayerName}</color> (8s) !");
                        NetworkManager.SetGlobalVision(0.05f);
                        _visionDur = 8f;
                    }
                    else SendHostMessage("Usage : /blind [ID]");
                    return true;

                case "/darkness":
                    if (!TryCooldown("/darkness")) return true;
                    SendHostMessage("<color=#ff6b6b>Obscurité totale</color> — 10 secondes !");
                    NetworkManager.SetGlobalVision(0.05f);
                    _visionDur = 10f;
                    return true;

                case "/freeze":
                    if (parts.Length >= 2 && byte.TryParse(parts[1], out byte fid))
                    {
                        if (!TryCooldown("/freeze")) return true;
                        var ft = FindById(fid);
                        if (ft == null || ft.Data.IsDead) { SendHostMessage("Joueur introuvable."); return true; }
                        _frozen[ft.PlayerId] = (ft.GetTruePosition(), 8f);
                        SendHostMessage($"<color=#88ccff>{ft.Data.PlayerName}</color> est gelé (8s) !");
                    }
                    else SendHostMessage("Usage : /freeze [ID]");
                    return true;

                case "/spin":
                    if (parts.Length >= 2 && byte.TryParse(parts[1], out byte sid))
                    {
                        if (!TryCooldown("/spin")) return true;
                        var st = FindById(sid);
                        if (st == null || st.Data.IsDead) { SendHostMessage("Joueur introuvable."); return true; }
                        _spin[st.PlayerId] = (st.GetTruePosition(), 0f, 5f);
                        SendHostMessage($"<color=#88ccff>{st.Data.PlayerName}</color> tourne en rond (5s) !");
                    }
                    else SendHostMessage("Usage : /spin [ID]");
                    return true;

                case "/randomcolors":
                    if (!TryCooldown("/randomcolors")) return true;
                    SendHostMessage("Couleurs aléatoires pour tous !");
                    NetworkManager.RandomizeColors();
                    return true;

                case "/shuffle":
                    if (!TryCooldown("/shuffle")) return true;
                    SendHostMessage("Mélange des positions !");
                    NetworkManager.ShuffleAllPlayers();
                    return true;

                case "/teleportall":
                    if (parts.Length >= 2 && byte.TryParse(parts[1], out byte tid))
                    {
                        if (!TryCooldown("/teleportall")) return true;
                        var tt = FindById(tid);
                        if (tt == null) { SendHostMessage("Joueur introuvable."); return true; }
                        SendHostMessage($"Tous téléportés vers <color=#88ccff>{tt.Data.PlayerName}</color> !");
                        NetworkManager.TeleportAllTo(tt);
                    }
                    else SendHostMessage("Usage : /teleportall [ID]");
                    return true;

                default:
                    SendHostMessage($"Commande inconnue : {cmd} — /help");
                    return true;
            }
        }

        private static void StartCut()
        {
            if (IsCutActive) return;
            IsCutActive = true; cutStep = 1; cutStepTimer = 2f;
            cutStartPositions.Clear();
            NetworkManager.SendCutSignal();
        }

        private static void AdvanceCutStep()
        {
            switch (cutStep)
            {
                case 1:
                    NetworkManager.StopCutSignal();
                    foreach (var p in NetworkManager.Alive())
                        cutStartPositions[p.PlayerId] = p.GetTruePosition();
                    SendHostMessage("<color=#ffd23f>NE BOUGEZ PLUS !</color> (5 secondes)");
                    cutStep = 2; cutStepTimer = 5f; break;
                case 2:
                    NetworkManager.SendCutSignal();
                    cutStep = 3; cutStepTimer = 2f; break;
                case 3:
                    NetworkManager.StopCutSignal();
                    SendHostMessage("<color=#00ff88>SOLEIL !</color> Vous pouvez rebouger.");
                    IsCutActive = false; cutStep = 0; break;
            }
        }

        public static void Update()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            float dt = Time.deltaTime;

            foreach (var k in _cd.Keys.ToList())
                _cd[k] = Math.Max(0f, _cd[k] - dt);

            if (IsCutActive)
            {
                cutStepTimer -= dt;
                if (cutStepTimer <= 0f) AdvanceCutStep();
                if (cutStep == 2)
                {
                    foreach (var p in NetworkManager.Alive())
                    {
                        if (!cutStartPositions.TryGetValue(p.PlayerId, out Vector2 start)) continue;
                        if (Vector2.Distance(start, p.GetTruePosition()) > 0.1f)
                        {
                            SendHostMessage($"<color=#ff6b6b>{p.Data.PlayerName}</color> a bougé — éliminé !");
                            if (DirectorOptions.CutKills) NetworkManager.MurderPlayer(p);
                            cutStartPositions.Remove(p.PlayerId);
                        }
                    }
                }
            }

            if (_visionDur > 0f)
            {
                _visionDur -= dt;
                if (_visionDur <= 0f)
                { NetworkManager.ResetGlobalVision(); SendHostMessage("Vision restaurée."); }
            }

            foreach (var k in _frozen.Keys.ToList())
            {
                var (pos, rem) = _frozen[k];
                var p = FindById(k);
                if (p == null || p.Data.IsDead) { _frozen.Remove(k); continue; }
                if (Vector2.Distance(p.GetTruePosition(), pos) > 0.15f) NetworkManager.Teleport(p, pos);
                float nr = rem - dt;
                if (nr <= 0f) { _frozen.Remove(k); SendHostMessage($"<color=#88ccff>{p.Data.PlayerName}</color> peut rebouger."); }
                else _frozen[k] = (pos, nr);
            }

            foreach (var k in _spin.Keys.ToList())
            {
                var (center, angle, rem) = _spin[k];
                var p = FindById(k);
                if (p == null || p.Data.IsDead) { _spin.Remove(k); continue; }
                angle += 3.5f * dt;
                NetworkManager.Teleport(p, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.0f);
                float nr = rem - dt;
                if (nr <= 0f) { _spin.Remove(k); NetworkManager.Teleport(p, center); }
                else _spin[k] = (center, angle, nr);
            }
        }

        private static PlayerControl FindById(byte id) =>
            PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p?.PlayerId == id);

        private static void SendHostMessage(string message)
        {
            Plugin.Log?.LogInfo($"[Director's Cut] {message}");
            if (DirectorOptions.AnnounceInChat)
                try { ChatManager.Queue(message); } catch { }
        }
    }

    // ── Patches ───────────────────────────────────────────────────────────

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
    static class Die_P
    { static void Postfix(PlayerControl __instance) => DirectorCore.OnPlayerDie(__instance); }

    // Intercepte la commande de l'hôte AVANT l'envoi réseau → pas de kick
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
    static class RpcSendChat_P
    {
        static bool Prefix(PlayerControl __instance, string chatText)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            if (__instance.PlayerId != PlayerControl.LocalPlayer.PlayerId) return true;
            if (string.IsNullOrWhiteSpace(chatText)) return true;
            if (!chatText.TrimStart().StartsWith("/")) return true;
            DirectorCore.TryProcessCommand(__instance, chatText.Trim());
            return false; // annule l'envoi réseau de la commande
        }
    }

    // Traite les commandes des joueurs non-hôte (directeur etc.)
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    static class Chat_P
    {
        static bool Prefix(PlayerControl sourcePlayer, string chatText)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            if (ChatManager.IsSending) return true;
            if (chatText.StartsWith("/") &&
                sourcePlayer?.PlayerId != PlayerControl.LocalPlayer.PlayerId)
            { DirectorCore.TryProcessCommand(sourcePlayer, chatText); return false; }
            return true;
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SetVisible))]
    static class Visible_P { static bool Prefix() => !AmongUsClient.Instance.AmHost; }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.StartGame))]
    static class Start_P { static void Postfix() => DirectorCore.Reset(); }

    // Snapshot au moment de la destruction du vaisseau (fin de partie)
    // → état vivants/éliminés encore valide, avant le nettoyage
    [HarmonyPatch(typeof(ShipStatus), "OnDestroy")]
    static class ShipDestroy_P
    {
        static void Prefix()
        {
            if (AmongUsClient.Instance?.AmHost != true) return;
            DirectorCore.SnapshotEndState();
            DirectorCore.PendingAutoGG = true;
            Plugin.Log?.LogInfo("[DirectorCore] ShipDestroy → snapshot + GG en attente.");
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    static class HudUp_P
    {
        static void Postfix(HudManager __instance)
        {
            DirectorCore.Update();
            ChatManager.CheckNewPlayers();
            ChatManager.ProcessPendingRules();
            if (AmongUsClient.Instance.AmHost && __instance?.Chat != null)
                __instance.Chat.gameObject.SetActive(true);
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
    static class Begin_P
    {
        static bool Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            AmongUsClient.Instance.StartGame(); return false;
        }
    }
}
