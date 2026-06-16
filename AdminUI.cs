using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AU_TheDirectorsCut
{
    // Panneau Admin (hôte uniquement) dessiné en IMGUI (OnGUI), à la manière de Hydra.
    // Ouvert/fermé avec la touche Suppr (Delete). Chaque bouton réutilise le pipeline de
    // commandes existant via DirectorCore.TryProcessCommand (donc mêmes contrôles/permissions).
    public class AdminUI : MonoBehaviour
    {
        // Constructeur requis pour les MonoBehaviours injectés en IL2CPP.
        public AdminUI(IntPtr ptr) : base(ptr) { }

        private bool _visible = false;
        private Vector2 _scroll = Vector2.zero;
        private readonly Dictionary<byte, string> _renameBuf = new();
        private readonly Rect _window = new Rect(40f, 40f, 380f, 540f);

        public void Update()
        {
            KeyCode key = ModConfig.AdminPanelKey?.Value ?? KeyCode.Delete;
            if (Input.GetKeyDown(key))
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                    _visible = !_visible;
            }
        }

        public void OnGUI()
        {
            if (!_visible) return;
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
            if (PlayerControl.LocalPlayer == null) return;

            GUI.Box(_window, "The Director's Cut — Admin");
            GUILayout.BeginArea(new Rect(_window.x + 10f, _window.y + 26f, _window.width - 20f, _window.height - 36f));

            GUILayout.Label("Actions globales");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Démarrer")) Run("/start");
            if (GUILayout.Button("Arrêter")) Run("/stop");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Fin réunion")) Run("/endmeeting");
            if (GUILayout.Button("GG à tous")) Run("/gg");
            GUILayout.EndHorizontal();

            // Pouvoirs Réalisateur : affichés seulement si l'hôte EST le Réalisateur.
            bool hostIsDirector = DirectorCore.DirectorPlayerId.HasValue
                && DirectorCore.DirectorPlayerId.Value == PlayerControl.LocalPlayer.PlayerId;
            if (hostIsDirector)
            {
                GUILayout.Space(6f);
                GUILayout.Label("Réalisateur");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Cut")) Run("/cut");
                if (GUILayout.Button("Darkness")) Run("/darkness");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("ColorBlind")) Run("/colorblind");
                if (GUILayout.Button("Shuffle")) Run("/shuffle");
                GUILayout.EndHorizontal();
                if (GUILayout.Button("Couleurs aléatoires")) Run("/randomcolors");
            }

            GUILayout.Space(6f);
            GUILayout.Label("Joueurs");
            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (var p in PlayerControl.AllPlayerControls.ToArray())
            {
                if (p?.Data == null) continue;
                char letter = (char)('A' + p.PlayerId);
                string status = p.Data.IsDead ? "  (mort)" : "";
                GUILayout.Label($"{letter} — {p.Data.PlayerName}{status}");

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Kill")) Run($"/kill {letter}");
                if (GUILayout.Button("Réalisateur")) Run($"/setdirector {letter}");
                if (GUILayout.Button("Kick")) Run($"/kick {letter}");
                GUILayout.EndHorizontal();

                if (!_renameBuf.ContainsKey(p.PlayerId)) _renameBuf[p.PlayerId] = "";
                GUILayout.BeginHorizontal();
                _renameBuf[p.PlayerId] = GUILayout.TextField(_renameBuf[p.PlayerId], 30);
                if (GUILayout.Button("Renommer") && !string.IsNullOrWhiteSpace(_renameBuf[p.PlayerId]))
                {
                    Run($"/rename {letter} {_renameBuf[p.PlayerId]}");
                    _renameBuf[p.PlayerId] = "";
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(4f);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(4f);
            if (GUILayout.Button("Fermer (Suppr)")) _visible = false;

            GUILayout.EndArea();
        }

        private void Run(string cmd)
        {
            try
            {
                if (PlayerControl.LocalPlayer != null)
                    DirectorCore.TryProcessCommand(PlayerControl.LocalPlayer, cmd);
            }
            catch (Exception e) { Plugin.Log?.LogError($"[AdminUI] {e.Message}"); }
        }
    }
}
