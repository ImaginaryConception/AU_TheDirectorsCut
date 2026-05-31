using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AU_TheDirectorsCut
{
    public static class DirectorCore
    {
        public static byte? DirectorPlayerId { get; private set; }
        public static bool IsCutActive { get; private set; }
        private static int cutStep; // 0 = idle, 1 = signal on, 2 = signal off, 3 = checking, 4 = final signal
        private static float cutStepTimer;
        private static Dictionary<byte, Vector2> cutStartPositions = new();
        private static float hyperdriveDuration;
        private static float originalSpeed;

        public static void Initialize()
        {
            DirectorPlayerId = null;
            IsCutActive = false;
            cutStep = 0;
            cutStepTimer = 0f;
            cutStartPositions.Clear();
            hyperdriveDuration = 0f;
            
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[DirectorCore] Initialized completely!");
        }

        public static void Reset()
        {
            DirectorPlayerId = null;
            IsCutActive = false;
            cutStep = 0;
            cutStepTimer = 0f;
            cutStartPositions.Clear();
            hyperdriveDuration = 0f;
            
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[DirectorCore] Reset completed!");
        }

        public static void OnPlayerDie(PlayerControl player)
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[DirectorCore] OnPlayerDie called!");
            
            if (!AmongUsClient.Instance.AmHost) 
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError("[DirectorCore] Not host, can't assign director!");
                return;
            }
            
            if (DirectorPlayerId.HasValue) 
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogInfo("[DirectorCore] Director already exists!");
                return;
            }

            DirectorPlayerId = player.PlayerId;
            SendHostMessage($">> Le joueur {player.Data.PlayerName} est devenu le Directeur ! <<");
            if (Plugin.Log != null)
                Plugin.Log.LogInfo($"[DirectorCore] Player {player.Data.PlayerName} (ID: {player.PlayerId}) is now the Director!");
        }

        public static bool IsDirector(byte playerId)
        {
            bool isDir = DirectorPlayerId.HasValue && DirectorPlayerId.Value == playerId;
            if (Plugin.Log != null)
                Plugin.Log.LogInfo($"[DirectorCore] Checking if player {playerId} is director: {isDir}");
            return isDir;
        }

        // Helper method to send feedback messages ONLY IN BEPINEX LOG (no kick!)
        private static void SendHostMessage(string message)
        {
            try
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogInfo($"[The Director's Cut] {message}");

                if (DirectorOptions.AnnounceInChat)
                    ChatManager.Queue(message);
            }
            catch (Exception e)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError($"[DirectorCore] Error sending message: {e}");
            }
        }

        public static bool TryProcessCommand(PlayerControl sender, string message)
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo($"[DirectorCore] TryProcessCommand called with message: '{message}' from sender: {sender?.Data?.PlayerName} (ID: {sender?.PlayerId})");
            
            if (!AmongUsClient.Instance.AmHost) 
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError("[DirectorCore] Not host, can't process commands!");
                return false;
            }
            
            if (sender == null) 
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogError("[DirectorCore] Sender is null!");
                return false;
            }

            string cleanMessage = message.Trim();
            if (!cleanMessage.StartsWith("/")) 
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogInfo("[DirectorCore] Message doesn't start with /, ignoring!");
                return false;
            }

            string[] parts = cleanMessage.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) 
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogInfo("[DirectorCore] No parts after split!");
                return false;
            }

            string command = parts[0].ToLowerInvariant();
            if (Plugin.Log != null)
                Plugin.Log.LogInfo($"[DirectorCore] Parsed command: '{command}'");

            // /start and /setdirector are only for the HOST
            if (command == "/start" || command == "/setdirector")
            {
                if (sender.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                {
                    SendHostMessage($">> Désolé {sender.Data.PlayerName}, seul l'hôte peut utiliser cette commande ! <<");
                    return true;
                }
            }

            // Allow these commands even before there's a Director!
            switch (command)
            {
                case "/welcome":
                    SendHostMessage($"=== {PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} ===\nCommandes: /welcome, /help, /setdirector, /players\nCommandes Directeur: /cut, /swap [ID1] [ID2], /hyper, /blind [ID], /freeze [ID], /teleportall [ID], /shuffle, /spin [ID], /bouncy, /randomcolors");
                    return true;

                case "/help":
                    SendHostMessage("=== AIDE ===\n1. Le premier joueur mort devient le Directeur\n2. Le Directeur contrôle la partie via les commandes\n3. /cut: 1, 2, 3 Soleil - Les joueurs qui bougent sont éliminés\n4. /swap: Échange deux joueurs de place\n5. /hyper: Augmente la vitesse temporairement\n6. /blind: Cache la lumière d'un joueur\n7. /freeze ID: Gèle un joueur\n8. /teleportall ID: Téléporte tous les joueurs vers un joueur\n9. /shuffle: Mélange toutes les positions\n10. /spin ID: Fait tourner un joueur\n11. /bouncy: Mode rebondissant\n12. /randomcolors: Mélange les couleurs des joueurs");
                    return true;
                
                case "/players":
                    List<string> playerList = new List<string>();
                    foreach (var player in PlayerControl.AllPlayerControls.ToArray())
                    {
                        if (player.Data != null)
                        {
                            playerList.Add($"{player.Data.PlayerName} (ID: {player.PlayerId}, Dead: {player.Data.IsDead}, Disconnected: {player.Data.Disconnected})");
                        }
                        else
                        {
                            playerList.Add($"Player (ID: {player.PlayerId}, Data: NULL)");
                        }
                    }
                    SendHostMessage($"=== PLAYERS ===\n{string.Join("\n", playerList)}");
                    return true;
                
                case "/setdirector":
                    if (sender.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                    {
                        SendHostMessage($">> Désolé {sender.Data.PlayerName}, seul l'hôte peut utiliser cette commande ! <<");
                        return true;
                    }
                    DirectorPlayerId = sender.PlayerId;
                    SendHostMessage($">> Tu es maintenant le Directeur ! <<");
                    if (Plugin.Log != null)
                        Plugin.Log.LogInfo($"[DirectorCore] Player {sender.Data.PlayerName} (ID: {sender.PlayerId}) is now the Director via command!");
                    return true;

                case "/start":
                    SendHostMessage(">> Lancement de la partie... <<");
                    if (Plugin.Log != null)
                        Plugin.Log.LogInfo("[DirectorCore] /start command received! Trying to start game...");
                    try
                    {
                        AmongUsClient.Instance.StartGame();
                    }
                    catch (Exception e)
                    {
                        SendHostMessage(">> ERREUR: Impossible de lancer la partie ! <<");
                        if (Plugin.Log != null)
                            Plugin.Log.LogError($"[DirectorCore] Error starting game: {e}");
                    }
                    return true;
            }

            // Other commands require being Director
            if (!IsDirector(sender.PlayerId))
            {
                SendHostMessage($">> Désolé {sender.Data.PlayerName}, tu dois être le Directeur pour utiliser cette commande ! <<");
                return true;
            }

            switch (command)
            {
                case "/cut":
                    SendHostMessage(">> Cut lancé ! 1, 2, 3... <<");
                    StartCut();
                    return true;

                case "/swap":
                    if (parts.Length >= 3 && byte.TryParse(parts[1], out byte id1) && byte.TryParse(parts[2], out byte id2))
                    {
                        SendHostMessage($">> Échange joueur {id1} ↔ {id2} <<");
                        SwapPlayers(id1, id2);
                    }
                    else
                    {
                        SendHostMessage(">> Utilisation: /swap [ID1] [ID2] <<");
                    }
                    return true;

                case "/hyper":
                    SendHostMessage(">> Hyperdrive activé ! <<");
                    ActivateHyperdrive();
                    return true;

                case "/blind":
                    if (parts.Length >= 2 && byte.TryParse(parts[1], out byte targetId))
                    {
                        SendHostMessage($">> Cécité sur joueur {targetId} <<");
                        BlindPlayer(targetId);
                    }
                    else
                    {
                        SendHostMessage(">> Utilisation: /blind [ID] <<");
                    }
                    return true;

                case "/freeze":
                    if (parts.Length >= 2 && byte.TryParse(parts[1], out byte freezeId))
                    {
                        var freezeTarget = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == freezeId);
                        if (freezeTarget != null)
                        {
                            SendHostMessage($">> Gèle le joueur {freezeTarget.Data.PlayerName} ! <<");
                            NetworkManager.FreezePlayer(freezeTarget);
                        }
                        else
                        {
                            SendHostMessage(">> Joueur introuvable ! <<");
                        }
                    }
                    else
                    {
                        SendHostMessage(">> Utilisation: /freeze [ID] <<");
                    }
                    return true;

                case "/explode":
                    if (parts.Length >= 2 && byte.TryParse(parts[1], out byte explodeId))
                    {
                        var explodeTarget = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == explodeId);
                        if (explodeTarget != null)
                        {
                            SendHostMessage($">> Explode le joueur {explodeTarget.Data.PlayerName} ! <<");
                            NetworkManager.MurderPlayer(explodeTarget);
                        }
                        else
                        {
                            SendHostMessage(">> Joueur introuvable ! <<");
                        }
                    }
                    else
                    {
                        SendHostMessage(">> Utilisation: /explode [ID] <<");
                    }
                    return true;

                case "/teleportall":
                    if (parts.Length >= 2 && byte.TryParse(parts[1], out byte tpAllId))
                    {
                        var tpTarget = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == tpAllId);
                        if (tpTarget != null)
                        {
                            SendHostMessage($">> Téléporte tout le monde vers {tpTarget.Data.PlayerName} ! <<");
                            NetworkManager.TeleportAllTo(tpTarget);
                        }
                        else
                        {
                            SendHostMessage(">> Joueur introuvable ! <<");
                        }
                    }
                    else
                    {
                        SendHostMessage(">> Utilisation: /teleportall [ID] <<");
                    }
                    return true;

                case "/shuffle":
                    SendHostMessage(">> Mélange des positions ! <<");
                    NetworkManager.ShuffleAllPlayers();
                    return true;

                case "/spin":
                    if (parts.Length >= 2 && byte.TryParse(parts[1], out byte spinId))
                    {
                        var spinTarget = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == spinId);
                        if (spinTarget != null)
                        {
                            SendHostMessage($">> Fait tourner {spinTarget.Data.PlayerName} ! <<");
                            NetworkManager.SpinPlayer(spinTarget);
                        }
                        else
                        {
                            SendHostMessage(">> Joueur introuvable ! <<");
                        }
                    }
                    else
                    {
                        SendHostMessage(">> Utilisation: /spin [ID] <<");
                    }
                    return true;

                case "/bouncy":
                    SendHostMessage(">> Mode rebondissant activé ! <<");
                    NetworkManager.BouncyMode();
                    return true;

                case "/randomcolors":
                    SendHostMessage(">> Mélange des couleurs des joueurs ! <<");
                    NetworkManager.RandomizeColors();
                    return true;

                default:
                    SendHostMessage($">> Commande inconnue: {command} <<");
                    return true;
            }
        }

        private static void StartCut()
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[DirectorCore] StartCut called!");
            
            if (IsCutActive)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogInfo("[DirectorCore] Cut already active!");
                return;
            }
            
            IsCutActive = true;
            cutStep = 1;
            cutStepTimer = 2f;
            cutStartPositions.Clear();

            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[DirectorCore] Cut step 1: Starting signal!");
            NetworkManager.SendCutSignal();
        }

        private static void SwapPlayers(byte id1, byte id2)
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[DirectorCore] SwapPlayers called!");
            
            var player1 = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == id1);
            var player2 = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == id2);
            
            if (player1 == null || player2 == null)
            {
                SendHostMessage(">> ERREUR: Joueur(s) introuvable(s) ! <<");
                if (Plugin.Log != null)
                {
                    if (player1 == null)
                        Plugin.Log.LogError($"[DirectorCore] Player1 (ID: {id1}) is null!");
                    if (player2 == null)
                        Plugin.Log.LogError($"[DirectorCore] Player2 (ID: {id2}) is null!");
                }
                return;
            }

            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[DirectorCore] Calling NetworkManager.SwapPlayers!");
            NetworkManager.SwapPlayers(player1, player2);
        }

        private static void ActivateHyperdrive()
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[DirectorCore] ActivateHyperdrive called!");
            
            if (hyperdriveDuration > 0f)
            {
                SendHostMessage(">> Hyperdrive déjà activé ! <<");
                if (Plugin.Log != null)
                    Plugin.Log.LogInfo("[DirectorCore] Hyperdrive already active!");
                return;
            }
            
            originalSpeed = 1f; // Default speed
            hyperdriveDuration = 10f;

            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[DirectorCore] Calling NetworkManager.SetGameSpeed!");
            NetworkManager.SetGameSpeed(3f);
        }

        private static void BlindPlayer(byte targetId)
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[DirectorCore] BlindPlayer called!");
            
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[DirectorCore] Calling NetworkManager.BlindPlayer!");
            NetworkManager.BlindPlayer(targetId);
        }

        public static void Update()
        {
            if (!AmongUsClient.Instance.AmHost) return;

            if (IsCutActive)
            {
                cutStepTimer -= Time.deltaTime;
                if (cutStepTimer <= 0f)
                {
                    AdvanceCutStep();
                }

                if (cutStep == 2) // Checking movement step (during the 5s no signal)
                {
                    CheckCutMovement();
                }
            }

            if (hyperdriveDuration > 0f)
            {
                hyperdriveDuration -= Time.deltaTime;
                if (hyperdriveDuration <= 0f)
                {
                    if (Plugin.Log != null)
                        Plugin.Log.LogInfo("[DirectorCore] Hyperdrive deactivated!");
                    SendHostMessage(">> Hyperdrive terminé <<");
                    NetworkManager.SetGameSpeed(originalSpeed);
                }
            }
        }

        private static void AdvanceCutStep()
        {
            switch (cutStep)
            {
                case 1: // Step 1: 2s signal → stop signal, take positions, check for 5s
                    if (Plugin.Log != null)
                        Plugin.Log.LogInfo("[DirectorCore] Cut step 2: Stop signal, take positions, start checking!");
                    NetworkManager.StopCutSignal();
                    TakeCutPositions();
                    cutStep = 2;
                    cutStepTimer = 5f; // 5s checking
                    break;

                case 2: // Step 2: 5s checking → restart signal for 2s
                    if (Plugin.Log != null)
                        Plugin.Log.LogInfo("[DirectorCore] Cut step 3: Restart final signal!");
                    NetworkManager.SendCutSignal();
                    cutStep = 3;
                    cutStepTimer = 2f;
                    break;

                case 3: // Step 3: 2s final signal → stop signal, end cut
                    if (Plugin.Log != null)
                        Plugin.Log.LogInfo("[DirectorCore] Cut complete!");
                    NetworkManager.StopCutSignal();
                    SendHostMessage(">> Soleil ! Vous pouvez bouger ! <<");
                    IsCutActive = false;
                    cutStep = 0;
                    break;
            }
        }

        private static void TakeCutPositions()
        {
            cutStartPositions.Clear();
            foreach (var player in PlayerControl.AllPlayerControls.ToArray())
            {
                if (player.Data != null && !player.Data.IsDead && !player.Data.Disconnected)
                {
                    cutStartPositions[player.PlayerId] = player.GetTruePosition();
                    if (Plugin.Log != null)
                        Plugin.Log.LogInfo($"[DirectorCore] Recorded position for {player.Data.PlayerName} (ID: {player.PlayerId}) at {player.GetTruePosition()}");
                }
            }
        }

        private static void CheckCutMovement()
        {
            foreach (var player in PlayerControl.AllPlayerControls.ToArray())
            {
                if (player.Data == null || player.Data.IsDead || player.Data.Disconnected) continue;
                
                if (cutStartPositions.TryGetValue(player.PlayerId, out Vector2 startPos))
                {
                    Vector2 currentPos = player.GetTruePosition();
                    float distance = Vector2.Distance(startPos, currentPos);
                    
                    if (distance > 0.1f)
                    {
                        if (Plugin.Log != null)
                            Plugin.Log.LogInfo($"[DirectorCore] Player {player.Data.PlayerName} moved {distance:F2} units - eliminating!");
                        SendHostMessage($">> Joueur {player.Data.PlayerName} éliminé ! <<");

                        if (DirectorOptions.CutKills)
                            NetworkManager.MurderPlayer(player);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
    static class PlayerControl_Die_Patch
    {
        static void Postfix(PlayerControl __instance)
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[DirectorCore] PlayerControl.Die.Postfix called!");
            
            DirectorCore.OnPlayerDie(__instance);
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    static class ChatController_AddChat_Patch
    {
        static bool Prefix(PlayerControl sourcePlayer, string chatText)
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo($"[DirectorCore] ChatController.AddChat.Prefix called with source: {sourcePlayer?.Data?.PlayerName}, text: '{chatText}'");
            
            if (!AmongUsClient.Instance.AmHost) return true;
            
            // If it's a command, process it and don't show it to everyone
            if (chatText.StartsWith("/"))
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogInfo("[DirectorCore] Command detected, processing!");
                DirectorCore.TryProcessCommand(sourcePlayer, chatText);
                return false;
            }
            
            return true;
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SetVisible))]
    static class ChatController_SetVisible_Patch
    {
        static bool Prefix(ChatController __instance, bool visible)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            return false; // Force chat visible for host
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.StartGame))]
    static class GameManager_StartGame_Patch
    {
        static void Postfix()
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[DirectorCore] GameManager.StartGame.Postfix called!");
            
            DirectorCore.Reset();
        }
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    static class HudManager_Update_Patch
    {
        static void Postfix(HudManager __instance)
        {
            DirectorCore.Update();

            if (AmongUsClient.Instance.AmHost && __instance != null && __instance.Chat != null)
            {
                __instance.Chat.gameObject.SetActive(true);
            }
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
    static class GameStartManager_BeginGame_Patch
    {
        static bool Prefix()
        {
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("[DirectorCore] GameStartManager.BeginGame.Prefix called!");
            
            if (!AmongUsClient.Instance.AmHost) return true;
            AmongUsClient.Instance.StartGame();
            return false;
        }
    }
}
