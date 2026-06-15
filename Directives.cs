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

        private static IGameOptions Clone() =>
            Utils.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions);

        private static float BaseSpeed() =>
            GameManager.Instance.LogicOptions.currentGameOptions.GetFloat(FloatOptionNames.PlayerSpeedMod);

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
        private const float StalkerGrace = 8f;
        private static bool _stalkerOn;
        private static byte _stA, _stB;
        private static float _stFar;
        private static bool _stWarned;
        private static byte? _pendStalkerA, _pendStalkerB;

        // Cube
        private static bool _cubeOn;
        private static Vector2 _cubePos;
        private static bool _cubeBonus;

        // Curse
        private static bool _curseOn;
        private static byte _curseId;
        private static float _curseElapsed, _curseResend;
        private const float CurseDur = 30f;

        // Stockholm : (crewmate, impostor)
        private static readonly List<(byte crew, byte imp)> _stockholm = new();

        // Éjection scriptée : 0 = off, 1 = premier votant, 2 = dernier votant
        private static int _ejectMode;

        // ===================== Reset =====================
        public static void Reset()
        {
            _timed.Clear();
            _modifiedOwners.Clear();
            _stalkerOn = false; _stFar = 0f; _stWarned = false;
            _pendStalkerA = null; _pendStalkerB = null;
            _cubeOn = false;
            _curseOn = false; _curseElapsed = 0f; _curseResend = 0f;
            _stockholm.Clear();
            _ejectMode = 0;
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

            // ---- Cube ----
            if (_cubeOn)
            {
                foreach (var p in Living())
                {
                    if (Vector2.Distance(p.GetTruePosition(), _cubePos) <= 1.3f)
                    {
                        _cubeOn = false;
                        if (_cubeBonus)
                        {
                            int owner = p.OwnerId;
                            SetClientSpeed(owner, BaseSpeed() * 1.6f);
                            AddTimed(30f, () => RestoreClient(owner));
                            Whisper(p, "<b><color=#00e676>Cube bonus !</color></b> Boost de vitesse 30s !", "Cube bonus ! Boost de vitesse 30s !");
                            Director($"<b>Cube</b> : {p.Data.PlayerName} a pris le bonus.", $"Cube : {p.Data.PlayerName} a pris le bonus.");
                        }
                        else
                        {
                            int owner = p.OwnerId;
                            SetClientSpeed(owner, 0.02f);
                            AddTimed(8f, () => RestoreClient(owner));
                            Whisper(p, "<b><color=#ff6b6b>Cube piégé !</color></b> Tu es bloqué 8s !", "Cube piege ! Tu es bloque 8s !");
                            Director($"<b>Cube</b> : {p.Data.PlayerName} est tombé dans le piège.", $"Cube : {p.Data.PlayerName} est tombe dans le piege.");
                        }
                        break;
                    }
                }
            }

            // ---- Curse (décroissance de vitesse) ----
            if (_curseOn)
            {
                _curseElapsed += dt;
                _curseResend -= dt;
                var c = Find(_curseId);
                if (c?.Data == null || c.Data.IsDead || c.Data.Disconnected || _curseElapsed >= CurseDur)
                {
                    if (c != null) RestoreClient(c.OwnerId);
                    _curseOn = false;
                }
                else if (_curseResend <= 0f)
                {
                    _curseResend = 2f;
                    float mult = Mathf.Lerp(BaseSpeed(), BaseSpeed() * 0.25f, _curseElapsed / CurseDur);
                    SetClientSpeed(c.OwnerId, mult);
                }
            }
        }

        // ===================== Hooks =====================
        public static void OnDeath(PlayerControl victim)
        {
            if (!Host || victim?.Data == null) return;
            byte id = victim.PlayerId;

            // Stockholm
            for (int i = _stockholm.Count - 1; i >= 0; i--)
            {
                var link = _stockholm[i];
                if (id == link.imp)
                {
                    _stockholm.RemoveAt(i);
                    var crew = Find(link.crew);
                    if (crew?.Data != null && !crew.Data.IsDead)
                    {
                        Broadcast($"<b><color=#ff6b6b>{crew.Data.PlayerName}</color></b> meurt de chagrin…", $"{crew.Data.PlayerName} meurt de chagrin...");
                        NetworkManager.MurderPlayer(crew);
                    }
                }
                else if (id == link.crew)
                {
                    _stockholm.RemoveAt(i);
                    var imp = Find(link.imp);
                    if (imp?.Data != null && !imp.Data.IsDead)
                    {
                        int owner = imp.OwnerId;
                        SetClientSpeed(owner, BaseSpeed() * 0.4f);
                        AddTimed(120f, () => RestoreClient(owner));
                        Whisper(imp, "<b><color=#ff6b6b>Ton lien est brisé</color></b> — ralenti 2 min.", "Ton lien est brise - ralenti 2 min.");
                    }
                }
            }
        }

        public static void OnMeetingStart()
        {
            if (!Host) return;
            // Les directives "de manche" s'arrêtent quand une réunion démarre.
            _stalkerOn = false;
        }

        public static void OnMeetingClose()
        {
            if (!Host) return;

            // Activer un Stalker programmé pour la manche qui commence
            if (_pendStalkerA.HasValue && _pendStalkerB.HasValue)
            {
                _stalkerOn = true; _stA = _pendStalkerA.Value; _stB = _pendStalkerB.Value;
                _stFar = 0f; _stWarned = false;
                _pendStalkerA = null; _pendStalkerB = null;
            }

            // Éjection scriptée : piège sur le 1er/dernier votant
            if (_ejectMode != 0)
            {
                var order = ScriptManager.VotedPlayerIdsInOrder;
                if (order != null && order.Count > 0)
                {
                    byte vid = _ejectMode == 1 ? order[0] : order[order.Count - 1];
                    var victim = Find(vid);
                    string which = _ejectMode == 1 ? "premier" : "dernier";
                    if (victim?.Data != null && !victim.Data.IsDead)
                    {
                        Broadcast($"<b><color=#ff6b6b>Le vaisseau éjecte {victim.Data.PlayerName}</color></b> — piège du {which} votant !", $"Le vaisseau ejecte {victim.Data.PlayerName} - piege du {which} votant !");
                        NetworkManager.MurderPlayer(victim);
                    }
                }
                _ejectMode = 0;
            }
        }

        // ===================== Commandes =====================
        public static void VoiceOver(string text)
        {
            string colored = $"<b><size=150%><color=#dfe6e9>« {text} »</color></size></b>";
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

        public static void Curse(PlayerControl target)
        {
            _curseOn = true; _curseId = target.PlayerId; _curseElapsed = 0f; _curseResend = 0f;
            Director($"<b><color=#b2bec3>Malédiction</color></b> sur {target.Data.PlayerName} : il ralentit peu à peu (30s).", $"Malediction sur {target.Data.PlayerName} : il ralentit peu a peu (30s).");
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

        public static void Cube(bool bonus)
        {
            var locs = Teleporter.GetTeleportLocations();
            if (locs == null || locs.Count == 0) return;
            var values = locs.Values.ToList();
            _cubePos = values[_rng.Next(values.Count)];
            _cubeOn = true; _cubeBonus = bonus;
            Director($"<b><color=#ffd23f>Cube {(bonus ? "bonus" : "piégé")}</color></b> placé. Premier arrivé, premier servi !", $"Cube {(bonus ? "bonus" : "piege")} place. Premier arrive, premier servi !");
        }

        // ---- meeting-only ----
        public static void RegisterStalker(PlayerControl a, PlayerControl b)
        {
            _pendStalkerA = a.PlayerId; _pendStalkerB = b.PlayerId;
            Whisper(a, $"<b><color=#ffd23f>Obsession</color></b> : reste à moins de 3m de <b>{b.Data.PlayerName}</b> toute la manche, sinon… !", $"Obsession : reste a moins de 3m de {b.Data.PlayerName} toute la manche, sinon... !");
            Whisper(b, $"<b><color=#ffd23f>Surveillance</color></b> : <b>{a.Data.PlayerName}</b> doit te suivre de près cette manche.", $"Surveillance : {a.Data.PlayerName} doit te suivre cette manche.");
            Director($"<b>Stalker</b> armé : {a.Data.PlayerName} → {b.Data.PlayerName} (dès la prochaine manche).", $"Stalker arme : {a.Data.PlayerName} -> {b.Data.PlayerName}.");
        }

        public static void Pacifist(PlayerControl target)
        {
            int owner = target.OwnerId;
            byte pid = target.PlayerId;
            SetClientKillCooldown(owner, 9000f);
            Whisper(target, "<b><color=#ffd23f>Pacifiste forcé</color></b> : interdit de tuer pendant 2 min. Tiens bon pour une récompense !", "Pacifiste force : interdit de tuer pendant 2 min. Tiens bon pour une recompense !");
            Director($"<b>Pacifiste</b> appliqué à {target.Data.PlayerName} (2 min).", $"Pacifiste applique a {target.Data.PlayerName} (2 min).");
            AddTimed(120f, () =>
            {
                var p = Find(pid);
                if (p?.Data != null && !p.Data.IsDead)
                {
                    SetClientSpeed(p.OwnerId, BaseSpeed() * 1.3f); // restaure le kill cd + boost permanent
                    Whisper(p, "<b><color=#00e676>Pacifiste accompli !</color></b> Boost de vitesse pour le reste de la partie.", "Pacifiste accompli ! Boost de vitesse pour le reste de la partie.");
                }
            });
        }

        public static void Stockholm(PlayerControl crew, PlayerControl imp)
        {
            _stockholm.Add((crew.PlayerId, imp.PlayerId));
            Whisper(crew, $"<b><color=#ffd23f>Syndrome de Stockholm</color></b> : ton sort est lié à <b>{imp.Data.PlayerName}</b>. S'il meurt, tu meurs.", $"Syndrome de Stockholm : ton sort est lie a {imp.Data.PlayerName}. S'il meurt, tu meurs.");
            Whisper(imp, $"<b><color=#ffd23f>Lien</color></b> : protège <b>{crew.Data.PlayerName}</b>. S'il meurt, tu perds ta force 2 min.", $"Lien : protege {crew.Data.PlayerName}. S'il meurt, tu perds ta force 2 min.");
            Director($"<b>Stockholm</b> : {crew.Data.PlayerName} ⇄ {imp.Data.PlayerName}.", $"Stockholm : {crew.Data.PlayerName} <-> {imp.Data.PlayerName}.");
        }

        public static void ArmEject(int mode)
        {
            _ejectMode = mode;
            string which = mode == 1 ? "premier" : "dernier";
            Director($"<b><color=#ff4d4d>Éjection scriptée</color></b> armée : le <b>{which}</b> votant sera éjecté à la fin de cette réunion.", $"Ejection scriptee armee : le {which} votant sera ejecte a la fin de cette reunion.");
        }

        // Résumé (texte simple) des directives actives, pour /status.
        public static string Status()
        {
            var lines = new List<string>();
            if (_stalkerOn) lines.Add("Stalker actif");
            if (_pendStalkerA.HasValue) lines.Add("Stalker programmé (prochaine manche)");
            if (_cubeOn) lines.Add($"Cube {(_cubeBonus ? "bonus" : "piégé")} posé");
            if (_curseOn) lines.Add("Malédiction active");
            if (_stockholm.Count > 0) lines.Add($"{_stockholm.Count} lien(s) Stockholm");
            if (_ejectMode != 0) lines.Add($"Éjection scriptée armée ({(_ejectMode == 1 ? "premier" : "dernier")})");
            if (_timed.Count > 0) lines.Add($"{_timed.Count} effet(s) temporisé(s)");
            return string.Join(", ", lines);
        }
    }
}
