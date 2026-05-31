using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using UnityEngine;

namespace AU_TheDirectorsCut
{
    // =====================================================================
    //  ChatManager — envoi de messages dans le chat PUBLIC sous un nom
    //  "système", de façon SÛRE (anti-kick).
    //
    //  Technique reprise de Town of Host (GPL-3.0) :
    //   - file d'attente + throttle (1 message / MessageWait sec max) ;
    //   - chaque message part en UN SEUL paquet réseau qui enchaîne
    //     SetName(nom système) -> SendChat -> SetName(restauration).
    //     => le serveur ne voit jamais un nom "bloqué", pas de rafale de RPC.
    //
    //  ⚠ Le chat n'est visible par les AUTRES joueurs qu'en réunion / lobby.
    //     L'hôte le voit en partie car il force le chat visible.
    // =====================================================================
    public static class ChatManager
    {
        // Nom affiché comme expéditeur (texte enrichi TMP supporté).
        public const string SystemName = "<color=#e84d4d>The Director's Cut</color>";

        private static readonly Queue<string> _queue = new();
        private static float _cooldown;

        // Messages pré-écrits proposés par l'UI. Modifie/ajoute librement.
        public static readonly (string label, string text)[] Presets =
        {
            ("Bienvenue", "Bienvenue ! Cette partie utilise le mod The Director's Cut."),
            ("Regles",    "Le premier joueur mort devient le Directeur et controle la partie."),
            ("Cut !",     "CUT imminent : preparez-vous a ne plus bouger !"),
            ("Silence",   "Le Directeur exige le silence radio."),
            ("GG",        "GG a tous, bien joue !"),
        };

        // Met un message en file (utilisé par l'UI et, en option, par SendHostMessage).
        public static void Queue(string text)
        {
            if (!string.IsNullOrEmpty(text))
                _queue.Enqueue(text);
        }

        // Pompe appelée chaque frame par le patch ChatController.Update (hôte only).
        public static void Pump(ChatController chat)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
            if (_queue.Count == 0) return;

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f) return;

            // Throttle anti-kick. Si l'option est coupée, débit quasi libre (à tes risques).
            _cooldown = DirectorOptions.AntiKick ? DirectorOptions.MessageWait : 0.05f;

            var speaker = LowestAlive();
            if (speaker == null) return;

            Send(speaker, _queue.Dequeue());
            chat.timeSinceLastMessage = 0f;
        }

        // Envoi réel : UN paquet GameData = SetName(titre) + SendChat + SetName(restore).
        private static void Send(PlayerControl speaker, string msg)
        {
            string original = speaker.Data.PlayerName;

            // 1) Affichage immédiat pour l'hôte (qui a le chat ouvert).
            speaker.SetName(SystemName);
            HudManager.Instance.Chat.AddChat(speaker, msg);
            speaker.SetName(original);

            // 2) Diffusion réseau ATOMIQUE a tous les clients (vanilla inclus).
            var w = MessageWriter.Get(SendOption.Reliable);
            w.StartMessage(5);                       // 5 = GameData (broadcast)
            w.Write(AmongUsClient.Instance.GameId);

            WriteSetName(w, speaker, SystemName);    // RPC SetName -> nom systeme
            WriteSendChat(w, speaker, msg);          // RPC SendChat
            WriteSetName(w, speaker, original);      // RPC SetName -> restauration

            w.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(w);
            w.Recycle();
        }

        private static void WriteSetName(MessageWriter w, PlayerControl p, string name)
        {
            w.StartMessage(2);                       // 2 = RPC
            w.WritePacked(p.NetId);
            w.Write((byte)RpcCalls.SetName);
            w.Write(p.Data.NetId);                   // NetworkedPlayerInfo NetId
            w.Write(name);
            w.EndMessage();
        }

        private static void WriteSendChat(MessageWriter w, PlayerControl p, string msg)
        {
            w.StartMessage(2);
            w.WritePacked(p.NetId);
            w.Write((byte)RpcCalls.SendChat);
            w.Write(msg);
            w.EndMessage();
        }

        // Plus petit PlayerId vivant = "porte-voix" (un mort ne peut pas parler aux vivants).
        private static PlayerControl LowestAlive()
        {
            PlayerControl best = null;
            foreach (var pc in PlayerControl.AllPlayerControls.ToArray())
            {
                if (pc == null || pc.Data == null) continue;
                if (pc.Data.IsDead || pc.Data.Disconnected) continue;
                if (best == null || pc.PlayerId < best.PlayerId) best = pc;
            }
            return best;
        }
    }

    // Pompe la file dans ChatController.Update (tick côté hôte, chat force visible).
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    static class ChatController_Update_Pump_Patch
    {
        static void Postfix(ChatController __instance)
        {
            ChatManager.Pump(__instance);
        }
    }
}
