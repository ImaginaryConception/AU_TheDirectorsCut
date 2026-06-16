using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;
using AU_TheDirectorsCut.Utils;

namespace AU_TheDirectorsCut
{
    // Toutes les "directives" originales du Réalisateur.
    // Architecture host-only : on réplique tout via des RPC vanilla / GameOptions par client.
    public static class Directives
    {
        // ===================== Helpers =====================
        private static bool Host => AmongUsClient.Instance?.AmHost == true;
        private static System.Random _rng = new System.Random();

        private static PlayerControl Find(byte id) =>
            PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p?.PlayerId == id);

        private static List<PlayerControl> Living() =>
            PlayerControl.AllPlayerControls.ToArray()
                .Where(p => p?.Data != null && !p.Data.IsDead && !p.Data.Disconnected).ToList();

        // IMPORTANT : envoyer des GameOptions à l'HÔTE appelle SetGameOptions qui MODIFIE le
        // currentGameOptions partagé. Si on relisait currentGameOptions pour restaurer, on
        // restaurerait des valeurs déjà polluées (→ marathon/quarantine qui ne s'arrêtent pas).
        // On garde donc un instantané PROPRE des options, capturé avant toute modification.
        private static IGameOptions _pristine;
        private static IGameOptions Pristine()
        {
            if (_pristine == null)
                _pristine = Utils.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);
            return _pristine;
        }

        private static IGameOptions Clone() =>
            Utils.GameOptions.CreateCloneOptions(Pristine());

        private static float BaseSpeed() =>
            Pristine().GetFloat(FloatOptionNames.PlayerSpeedMod);

        private static void SetClientSpeed(int ownerId, float value)
        {
            try { var o = Clone(); o.SetFloat(FloatOptionNames.PlayerSpeedMod, value); Utils.GameOptions.SendGameOptionsToClient(o, ownerId); }
            catch (Exception e) { Plugin.Log?.LogError($"[Directives.Speed] {e.Message}"); }
            _modifiedOwners.Add(ownerId);
        }

        private static void SetClientVision(int ownerId, float crew, float imp)
        {
            try { var o = Clone(); o.SetFloat(FloatOptionNames.CrewLightMod, crew); o.SetFloat(FloatOptionNames.ImpostorLightMod, imp); Utils.GameOptions.SendGameOptionsToClient(o, ownerId); }
            catch (Exception e) { Plugin.Log?.LogError($"[Directives.Vision] {e.Message}"); }
            _modifiedOwners.Add(ownerId);
        }

        private static void SetClientKillCooldown(int ownerId, float cd)
        {
            try { var o = Clone(); o.SetFloat(FloatOptionNames.KillCooldown, cd); Utils.GameOptions.SendGameOptionsToClient(o, ownerId); }
            catch (Exception e) { Plugin.Log?.LogError($"[Directives.KillCd] {e.Message}"); }
            _modifiedOwners.Add(ownerId);
        }

        private static void RestoreClient(int ownerId)
        {
            try { Utils.GameOptions.SendGameOptionsToClient(Clone(), ownerId); } catch { }
        }

        private static void RestoreOwners(IEnumerable<int> owners)
        {
            foreach (var id in owners) RestoreClient(id);
        }

        private static void Director(string colored, string plain) => DirectorCore.DirectorNotify(colored, plain);
        private static void Broadcast(string colored, string plain) => ChatManager.Queue(colored, plain);
        private static void Whisper(PlayerControl t, string colored, string plain)
        {
            if (t != null) ChatManager.QueueSystemMessage(t, colored, plain);
        }

        // ===================== État =====================
        private class Timed { public float t; public Action end; }
        private static readonly List<Timed> _timed = new();
        private static void AddTimed(float seconds, Action end) => _timed.Add(new Timed { t = seconds, end = end });

        private static readonly HashSet<int> _modifiedOwners = new();

        // Stalker
        private const float StalkerDist = 3f;
        private const float StalkerGrace = 8f;        // temps toléré hors de portée avant kill
        private const float StalkerStartDelay = 10f;  // grâce après le début de la manche
        private static bool _stalkerOn;
        private static byte _stA, _stB;
        private static float _stFar;
        private static float _stStartGrace;           // décompte avant de commencer à vérifier
        private static bool _stWarned;
        private static byte? _pendStalkerA, _pendStalkerB;

        // Ultimatum : un imposteur doit tuer avant la fin du délai, sinon il est démasqué.
        private static bool _ultimatumOn;
        private static byte _ultimatumId;
        private static float _ultimatumTimer;
        private static bool _ultimatumKilled;         // a-t-il fait un vrai kill depuis l'assignation ?
        private static byte? _pendUltimatumId;
        private static float _pendUltimatumDur;
        private const float UltimatumDefault = 60f;

        // ===================== Reset =====================
        public static void Reset()
        {
            // Capture un instantané PROPRE des options au début de partie (currentGameOptions
            // est encore intact ici). Sert de base fiable pour appliquer/restaurer les effets.
            try { _pristine = Utils.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions); }
            catch { _pristine = null; }
            _timed.Clear();
            _modifiedOwners.Clear();
            _stalkerOn = false; _stFar = 0f; _stWarned = false; _stStartGrace = 0f;
            _pendStalkerA = null; _pendStalkerB = null;
            _ultimatumOn = false; _ultimatumKilled = false; _ultimatumTimer = 0f;
            _pendUltimatumId = null; _pendUltimatumDur = 0f;
        }

        // ===================== Tick =====================
        public static void Update(float dt)
        {
            if (!Host) return;

            // Effets temporisés génériques
            for (int i = _timed.Count - 1; i >= 0; i--)
            {
                _timed[i].t -= dt;
                if (_timed[i].t <= 0f)
                {
                    try { _timed[i].end?.Invoke(); } catch (Exception e) { Plugin.Log?.LogError($"[Directives.Timed] {e.Message}"); }
                    _timed.RemoveAt(i);
                }
            }

            if (ShipStatus.Instance == null) return; // le reste n'a de sens qu'en jeu
            if (MeetingHud.Instance != null) return; // pas de proximité/poursuite pendant une réunion

            // ---- Stalker ----
            if (_stalkerOn)
            {
                var a = Find(_stA); var b = Find(_stB);
                if (a?.Data == null || b?.Data == null || a.Data.IsDead || b.Data.IsDead || a.Data.Disconnected || b.Data.Disconnected)
                {
                    _stalkerOn = false;
                }
                else if (_stStartGrace > 0f)
                {
                    // Grâce de début de manche : on attend avant de vérifier (les joueurs viennent
                    // d'être téléportés à leur spawn à la fin de la réunion).
                    _stStartGrace -= dt;
                }
                else
                {
                    float d = Vector2.Distance(a.GetTruePosition(), b.GetTruePosition());
                    if (d > StalkerDist)
                    {
                        _stFar += dt;
                        if (!_stWarned && _stFar > StalkerGrace * 0.5f)
                        {
                            _stWarned = true;
                            Whisper(a, "<b><color=#ff6b6b>Trop loin !</color></b> Rapproche-toi de ta cible !", "Trop loin ! Rapproche-toi de ta cible !");
                        }
                        if (_stFar >= StalkerGrace)
                        {
                            _stalkerOn = false;
                            // On tue bien le SUIVEUR (A), jamais la cible suivie (B).
                            Broadcast($"<b><color=#ff6b6b>{a.Data.PlayerName}</color></b> a perdu sa cible — éliminé(e) !", $"{a.Data.PlayerName} a perdu sa cible - elimine !");
                            NetworkManager.MurderPlayer(a);
                        }
                    }
                    else
                    {
                        _stFar = 0f; _stWarned = false;
                    }
                }
            }

            // ---- Ultimatum ----
            if (_ultimatumOn)
            {
                var t = Find(_ultimatumId);
                if (t?.Data == null || t.Data.IsDead || t.Data.Disconnected)
                {
                    _ultimatumOn = false;
                }
                else
                {
                    _ultimatumTimer -= dt;
                    if (_ultimatumTimer <= 0f)
                    {
                        _ultimatumOn = false;
                        if (!_ultimatumKilled) ExposeUltimatum(t);
                    }
                }
            }
        }

        // Révélation publique de l'imposteur qui n'a pas tué à temps : pseudo en rouge + meeting auto.
        private static void ExposeUltimatum(PlayerControl t)
        {
            try
            {
                string original = t.Data.PlayerName;
                NetworkManager.SetPlayerName(t, $"<color=#ff1f1f>{original}</color>");
                Broadcast($"<b><color=#ff1f1f>{original} est un IMPOSTEUR !</color></b> Il n'a pas tué à temps.", $"{original} est un IMPOSTEUR ! Il n'a pas tue a temps.");
                var reporter = Living().FirstOrDefault(p => p != null);
                if (reporter != null) reporter.RpcStartMeeting(null); // null = bouton d'urgence
            }
            catch (Exception e) { Plugin.Log?.LogError($"[Ultimatum] {e.Message}"); }
        }

        // Appelé par le patch MurderPlayer quand un VRAI kill est détecté (killer != victime).
        public static void NotifyKill(byte killerId)
        {
            if (_ultimatumOn && killerId == _ultimatumId) _ultimatumKilled = true;
        }

        // ===================== Hooks =====================
        public static void OnDeath(PlayerControl victim)
        {
            // (réservé pour de futures directives liées à la mort)
        }

        public static void OnMeetingStart()
        {
            if (!Host) return;
            // Les directives "de manche" s'arrêtent quand une réunion démarre.
            _stalkerOn = false;
            _ultimatumOn = false;
        }

        public static void OnMeetingClose()
        {
            if (!Host) return;

            // Activer un Stalker programmé pour la manche qui commence
            if (_pendStalkerA.HasValue && _pendStalkerB.HasValue)
            {
                _stalkerOn = true; _stA = _pendStalkerA.Value; _stB = _pendStalkerB.Value;
                _stFar = 0f; _stWarned = false;
                _stStartGrace = StalkerStartDelay; // 10s de grâce avant de vérifier
                _pendStalkerA = null; _pendStalkerB = null;
            }

            // Activer un Ultimatum programmé pour la manche qui commence
            if (_pendUltimatumId.HasValue)
            {
                _ultimatumOn = true;
                _ultimatumId = _pendUltimatumId.Value;
                _ultimatumTimer = _pendUltimatumDur;
                _ultimatumKilled = false;
                _pendUltimatumId = null;
            }
        }

        // ===================== Commandes =====================
        public static void VoiceOver(string text)
        {
            string colored = $"<b><size=150%><color=#000000>« {text} »</color></size></b>";
            Broadcast(colored, $"« {text} »");
        }

        public static void Spotlight(PlayerControl target)
        {
            var affected = new List<int>();
            foreach (var p in Living())
            {
                if (p.PlayerId == target.PlayerId) continue;
                SetClientVision(p.OwnerId, 0f, 0f);
                affected.Add(p.OwnerId);
            }
            AddTimed(20f, () => RestoreOwners(affected));
            Director($"<b><color=#ffd23f>Projecteur</color></b> braqué sur {target.Data.PlayerName} (20s).", $"Projecteur braque sur {target.Data.PlayerName} (20s).");
        }

        public static void Marathon()
        {
            var affected = new List<int>();
            foreach (var p in Living())
            {
                SetClientSpeed(p.OwnerId, BaseSpeed() * 1.6f);
                affected.Add(p.OwnerId);
            }
            AddTimed(15f, () => RestoreOwners(affected));
            Director("<b><color=#a29bfe>Marathon</color></b> : tout le monde accéléré 15s !", "Marathon : tout le monde accelere 15s !");
        }

        public static void Quarantine(PlayerControl target)
        {
            var affected = new List<int>();
            foreach (var p in Living())
            {
                if (p.PlayerId == target.PlayerId) continue;
                SetClientSpeed(p.OwnerId, 0.02f);
                affected.Add(p.OwnerId);
            }
            AddTimed(8f, () => RestoreOwners(affected));
            Director($"<b><color=#74b9ff>Quarantaine</color></b> : tous figés sauf {target.Data.PlayerName} (8s).", $"Quarantaine : tous figes sauf {target.Data.PlayerName} (8s).");
        }

        public static void Roulette()
        {
            var living = Living();
            if (living.Count == 0) return;
            byte vid = living[_rng.Next(living.Count)].PlayerId;
            Broadcast("<b><color=#ff6b6b>La roulette tourne…</color></b>", "La roulette tourne...");
            AddTimed(2.5f, () =>
            {
                var v = Find(vid);
                if (v?.Data != null && !v.Data.IsDead)
                {
                    Broadcast($"<b><color=#ff6b6b>Le sort a frappé {v.Data.PlayerName} !</color></b>", $"Le sort a frappe {v.Data.PlayerName} !");
                    NetworkManager.MurderPlayer(v);
                }
            });
        }

        public static void BodySwap(PlayerControl a, PlayerControl b)
        {
            int ca = a.Data.DefaultOutfit.ColorId, cb = b.Data.DefaultOutfit.ColorId;
            string na = a.Data.PlayerName, nb = b.Data.PlayerName;
            try { a.RpcSetColor((byte)cb); b.RpcSetColor((byte)ca); } catch (Exception e) { Plugin.Log?.LogError($"[Directives.BodySwap] {e.Message}"); }
            NetworkManager.SetPlayerName(a, nb);
            NetworkManager.SetPlayerName(b, na);
            Director($"<b><color=#a29bfe>Échange d'identités</color></b> : {na} ⇄ {nb}.", $"Echange d'identites : {na} <-> {nb}.");
        }

        // ---- meeting-only ----
        public static void RegisterStalker(PlayerControl a, PlayerControl b)
        {
            _pendStalkerA = a.PlayerId; _pendStalkerB = b.PlayerId;
            Whisper(a, $"<b><color=#ffd23f>Obsession</color></b> : reste à moins de 3m de <b>{b.Data.PlayerName}</b> toute la manche, sinon… !", $"Obsession : reste a moins de 3m de {b.Data.PlayerName} toute la manche, sinon... !");
            Whisper(b, $"<b><color=#ffd23f>Surveillance</color></b> : <b>{a.Data.PlayerName}</b> doit te suivre de près cette manche.", $"Surveillance : {a.Data.PlayerName} doit te suivre cette manche.");
            Director($"<b>Stalker</b> armé : {a.Data.PlayerName} → {b.Data.PlayerName} (dès la prochaine manche).", $"Stalker arme : {a.Data.PlayerName} -> {b.Data.PlayerName}.");
        }

        // Ultimatum : un imposteur doit faire un kill dans le délai imparti (dès le début de la
        // manche). S'il n'a tué personne à l'expiration, son rôle est révélé à tous (pseudo en
        // rouge) et une réunion d'urgence est déclenchée automatiquement.
        public static void Ultimatum(PlayerControl target, float seconds)
        {
            _pendUltimatumId = target.PlayerId;
            _pendUltimatumDur = seconds > 0f ? seconds : UltimatumDefault;
            int s = Mathf.RoundToInt(_pendUltimatumDur);
            Whisper(target, $"<b><color=#ff4d4d>Ultimatum</color></b> : tu dois tuer dans les <b>{s}s</b> après le début de la manche, sinon ton rôle sera révélé à tous !", $"Ultimatum : tue dans les {s}s apres le debut de la manche, sinon ton role sera revele !");
            Director($"<b>Ultimatum</b> appliqué à {target.Data.PlayerName} ({s}s) — dès la prochaine manche.", $"Ultimatum applique a {target.Data.PlayerName} ({s}s).");
        }

        // Résumé (texte simple) des directives actives, pour /status.
        public static string Status()
        {
            var lines = new List<string>();
            if (_stalkerOn) lines.Add("Stalker actif");
            if (_pendStalkerA.HasValue) lines.Add("Stalker programmé (prochaine manche)");
            if (_ultimatumOn) lines.Add($"Ultimatum en cours ({Mathf.CeilToInt(_ultimatumTimer)}s)");
            if (_pendUltimatumId.HasValue) lines.Add("Ultimatum programmé (prochaine manche)");
            if (_timed.Count > 0) lines.Add($"{_timed.Count} effet(s) temporisé(s)");
            return string.Join(", ", lines);
        }
    }
}
