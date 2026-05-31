using System;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace AU_TheDirectorsCut
{
    // ---------------------------------------------------------------------
    //  DirectorUI — panneau du Réalisateur, affiché UNIQUEMENT chez l'hôte.
    //   - Cases à cocher  -> activent/désactivent des options (DirectorOptions)
    //   - Boutons simples  -> poussent des messages pré-écrits dans le chat
    //                         public, sous le nom du système (via ChatManager)
    //
    //  OnGUI (pas de GUI.Window pour éviter les soucis de délégués IL2CPP).
    // ---------------------------------------------------------------------
    public class DirectorUI : MonoBehaviour
    {
        public DirectorUI(IntPtr ptr) : base(ptr) { } // requis pour un MonoBehaviour injecté

        private static GUIStyle _box;
        private Vector2 _scroll;

        private void OnGUI()
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
            _box ??= new GUIStyle(GUI.skin.box) { richText = true };

            GUILayout.BeginArea(new Rect(16, 90, 300, 560), _box);
            GUILayout.Label("<b><size=15>The Director's Cut</size></b>");
            GUILayout.Space(6);

            // ---- Cases à cocher (options) ----
            GUILayout.Label("<b>Options</b>");
            DirectorOptions.AnnounceInChat = GUILayout.Toggle(
                DirectorOptions.AnnounceInChat, " Annoncer les actions dans le chat");
            DirectorOptions.AntiKick = GUILayout.Toggle(
                DirectorOptions.AntiKick, " Anti-kick (throttle des messages)");
            DirectorOptions.CutKills = GUILayout.Toggle(
                DirectorOptions.CutKills, " Cut elimine les joueurs qui bougent");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Delai chat: {DirectorOptions.MessageWait:0.0}s", GUILayout.Width(110));
            DirectorOptions.MessageWait = GUILayout.HorizontalSlider(
                DirectorOptions.MessageWait, 0.2f, 2f);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // ---- Boutons simples (messages pré-écrits) ----
            GUILayout.Label("<b>Messages publics</b>");
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(220));
            foreach (var preset in ChatManager.Presets)
            {
                if (GUILayout.Button(preset.label, GUILayout.Height(28)))
                    ChatManager.Queue(preset.text);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(4);
            GUILayout.Label("<size=10>Visible en jeu pour l'hote ; pour les autres a la prochaine reunion.</size>");

            GUILayout.EndArea();
        }
    }

    // Enregistre le type IL2CPP (une fois) puis attache l'UI au HudManager.
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
    static class HudManager_Start_AttachUI_Patch
    {
        private static bool _registered;

        static void Postfix(HudManager __instance)
        {
            if (!_registered)
            {
                // Idéalement à mettre dans Plugin.Load, mais marche aussi ici (1re utilisation).
                ClassInjector.RegisterTypeInIl2Cpp<DirectorUI>();
                _registered = true;
            }

            if (__instance.GetComponent<DirectorUI>() == null)
                __instance.gameObject.AddComponent<DirectorUI>();
        }
    }
}
