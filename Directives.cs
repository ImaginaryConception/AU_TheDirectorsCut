using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;
using AU_TheDirectorsCut.Utils;

namespace AU_TheDirectorsCut
{
    public static class Directives
    {
        private static bool Host => AmongUsClient.Instance?.AmHost == true;
        private static System.Random _rng = new System.Random();

        private static PlayerControl Find(byte id) =>
            PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p?.PlayerId == id);

        private static List<PlayerControl> Living() =>
            PlayerControl.AllPlayerControls.ToArray()
                .Where(p => p?.Data != null && !p.Data.IsDead && !p.Data.Disconnected).ToList();

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

        private static void Director(string enC, string frC, string enP, string frP)
        {
            byte pid = DirectorCore.DirectorPlayerId ?? (PlayerControl.LocalPlayer != null ? PlayerControl.LocalPlayer.PlayerId : (byte)0);
            var lang = Localization.Get(pid);
            DirectorCore.DirectorNotify(Localization.Tr(lang, enC, frC), Localization.Tr(lang, enP, frP));
        }
        private static void Broadcast(string enC, string frC, string enP, string frP)
            => ChatManager.QueueBroadcastLoc(
                () => Localization.Tr(Localization.CurrentLang, enC, frC),
                () => Localization.Tr(Localization.CurrentLang, enP, frP));
        private static void Whisper(PlayerControl t, string enC, string frC, string enP, string frP)
        {
            if (t == null) return;
            var lang = Localization.Get(t.PlayerId);
            ChatManager.QueueSystemMessage(t, Localization.Tr(lang, enC, frC), Localization.Tr(lang, enP, frP));
        }

        private class Timed { public float t; public Action end; }
        private static readonly List<Timed> _timed = new();
        private static void AddTimed(float seconds, Action end) => _timed.Add(new Timed { t = seconds, end = end });

        private static readonly HashSet<int> _modifiedOwners = new();

        private const float StalkerDist = 3f;
        private const float StalkerStartDelay = 10f;  
        private static bool _stalkerOn;
        private static byte _stA, _stB;
        private static float _stStartGrace;          
        private static byte? _pendStalkerA, _pendStalkerB;

        private static bool _ultimatumOn;
        private static byte _ultimatumId;
        private static float _ultimatumTimer;
        private static bool _ultimatumKilled;         
        private static byte? _pendUltimatumId;
        private static float _pendUltimatumDur;
        private const float UltimatumDefault = 60f;

        public static void Reset()
        {
            try { _pristine = Utils.GameOptions.CreateCloneOptions(GameManager.Instance.LogicOptions.currentGameOptions); }
            catch { _pristine = null; }
            _timed.Clear();
            _modifiedOwners.Clear();
            _stalkerOn = false; _stStartGrace = 0f;
            _pendStalkerA = null; _pendStalkerB = null;
            _ultimatumOn = false; _ultimatumKilled = false; _ultimatumTimer = 0f;
            _pendUltimatumId = null; _pendUltimatumDur = 0f;
        }

        public static void Update(float dt)
        {
            if (!Host) return;

            for (int i = _timed.Count - 1; i >= 0; i--)
            {
                _timed[i].t -= dt;
                if (_timed[i].t <= 0f)
                {
                    try { _timed[i].end?.Invoke(); } catch (Exception e) { Plugin.Log?.LogError($"[Directives.Timed] {e.Message}"); }
                    _timed.RemoveAt(i);
                }
            }

            if (ShipStatus.Instance == null) return;
            if (MeetingHud.Instance != null) return; 

            if (_stalkerOn)
            {
                var a = Find(_stA); var b = Find(_stB);
                if (a?.Data == null || b?.Data == null || a.Data.IsDead || b.Data.IsDead || a.Data.Disconnected || b.Data.Disconnected)
                {
                    _stalkerOn = false;
                }
                else if (_stStartGrace > 0f)
                {
                    _stStartGrace -= dt;
                }
                else
                {
                    float d = Vector2.Distance(a.GetTruePosition(), b.GetTruePosition());
                    if (d > StalkerDist)
                    {
                        _stalkerOn = false;
                        Broadcast($"<b><color=#ff6b6b>{a.Data.PlayerName}</color></b> lost their target — eliminated!", $"<b><color=#ff6b6b>{a.Data.PlayerName}</color></b> a perdu sa cible — éliminé(e) !", $"{a.Data.PlayerName} lost their target - eliminated!", $"{a.Data.PlayerName} a perdu sa cible - elimine !");
                        NetworkManager.MurderPlayer(a);
                    }
                }
            }

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

        private static void ExposeUltimatum(PlayerControl t)
        {
            try
            {
                string original = t.Data.PlayerName;
                NetworkManager.SetPlayerName(t, $"<color=#ff1f1f>{original}</color>");
                Broadcast($"<b><color=#ff1f1f>{original} is an IMPOSTOR!</color></b> They didn't kill in time.", $"<b><color=#ff1f1f>{original} est un IMPOSTEUR !</color></b> Il n'a pas tué à temps.", $"{original} is an IMPOSTOR! They didn't kill in time.", $"{original} est un IMPOSTEUR ! Il n'a pas tue a temps.");
            }
            catch (Exception e) { Plugin.Log?.LogError($"[Ultimatum] {e.Message}"); }
        }

        public static void NotifyKill(byte killerId)
        {
            if (_ultimatumOn && killerId == _ultimatumId) _ultimatumKilled = true;
        }

        public static void OnDeath(PlayerControl victim)
        {
        }

        public static void OnMeetingStart()
        {
            if (!Host) return;
            _stalkerOn = false;
            _ultimatumOn = false;
        }

        public static void OnMeetingClose()
        {
            if (!Host) return;

            if (_pendStalkerA.HasValue && _pendStalkerB.HasValue)
            {
                _stalkerOn = true; _stA = _pendStalkerA.Value; _stB = _pendStalkerB.Value;
                _stStartGrace = StalkerStartDelay;  
                _pendStalkerA = null; _pendStalkerB = null;
            }

            if (_pendUltimatumId.HasValue)
            {
                _ultimatumOn = true;
                _ultimatumId = _pendUltimatumId.Value;
                _ultimatumTimer = _pendUltimatumDur;
                _ultimatumKilled = false;
                _pendUltimatumId = null;
            }
        }

        public static void VoiceOver(string text)
        {
            string colored = $"<b><size=150%><color=#000000>« {text} »</color></size></b>";
            Broadcast(colored, colored, $"« {text} »", $"« {text} »");
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
            Director($"<b><color=#ffd23f>Spotlight</color></b> aimed at {target.Data.PlayerName} (20s).", $"<b><color=#ffd23f>Projecteur</color></b> braqué sur {target.Data.PlayerName} (20s).", $"Spotlight aimed at {target.Data.PlayerName} (20s).", $"Projecteur braque sur {target.Data.PlayerName} (20s).");
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
            Director("<b><color=#a29bfe>Marathon</color></b>: everyone sped up 15s!", "<b><color=#a29bfe>Marathon</color></b> : tout le monde accéléré 15s !", "Marathon: everyone sped up 15s!", "Marathon : tout le monde accelere 15s !");
        }

        public static void Roulette()
        {
            var living = Living();
            if (living.Count == 0) return;
            byte vid = living[_rng.Next(living.Count)].PlayerId;
            Broadcast("<b><color=#ff6b6b>The roulette is spinning…</color></b>", "<b><color=#ff6b6b>La roulette tourne…</color></b>", "The roulette is spinning...", "La roulette tourne...");
            AddTimed(2.5f, () =>
            {
                var v = Find(vid);
                if (v?.Data != null && !v.Data.IsDead)
                {
                    Broadcast($"<b><color=#ff6b6b>Fate struck {v.Data.PlayerName}!</color></b>", $"<b><color=#ff6b6b>Le sort a frappé {v.Data.PlayerName} !</color></b>", $"Fate struck {v.Data.PlayerName}!", $"Le sort a frappe {v.Data.PlayerName} !");
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
            Director($"<b><color=#a29bfe>Identity swap</color></b>: {na} ⇄ {nb}.", $"<b><color=#a29bfe>Échange d'identités</color></b> : {na} ⇄ {nb}.", $"Identity swap: {na} <-> {nb}.", $"Echange d'identites : {na} <-> {nb}.");
        }

        public static void RegisterStalker(PlayerControl a, PlayerControl b)
        {
            _pendStalkerA = a.PlayerId; _pendStalkerB = b.PlayerId;
            Whisper(a, $"<b><color=#ffd23f>Obsession</color></b>: stay within 3m of <b>{b.Data.PlayerName}</b> the whole round, otherwise… !", $"<b><color=#ffd23f>Obsession</color></b> : reste à moins de 3m de <b>{b.Data.PlayerName}</b> toute la manche, sinon… !", $"Obsession: stay within 3m of {b.Data.PlayerName} the whole round, otherwise... !", $"Obsession : reste a moins de 3m de {b.Data.PlayerName} toute la manche, sinon... !");
            Whisper(b, $"<b><color=#ffd23f>Surveillance</color></b>: <b>{a.Data.PlayerName}</b> must follow you closely this round.", $"<b><color=#ffd23f>Surveillance</color></b> : <b>{a.Data.PlayerName}</b> doit te suivre de près cette manche.", $"Surveillance: {a.Data.PlayerName} must follow you this round.", $"Surveillance : {a.Data.PlayerName} doit te suivre cette manche.");
            Director($"<b>Stalker</b> armed: {a.Data.PlayerName} → {b.Data.PlayerName} (starting next round).", $"<b>Stalker</b> armé : {a.Data.PlayerName} → {b.Data.PlayerName} (dès la prochaine manche).", $"Stalker armed: {a.Data.PlayerName} -> {b.Data.PlayerName}.", $"Stalker arme : {a.Data.PlayerName} -> {b.Data.PlayerName}.");
        }

        public static void Ultimatum(PlayerControl target, float seconds)
        {
            _pendUltimatumId = target.PlayerId;
            _pendUltimatumDur = seconds > 0f ? seconds : UltimatumDefault;
            int s = Mathf.RoundToInt(_pendUltimatumDur);
            Whisper(target, $"<b><color=#ff4d4d>Ultimatum</color></b>: you must kill within <b>{s}s</b> after the round starts, otherwise your role will be revealed to everyone!", $"<b><color=#ff4d4d>Ultimatum</color></b> : tu dois tuer dans les <b>{s}s</b> après le début de la manche, sinon ton rôle sera révélé à tous !", $"Ultimatum: kill within {s}s after the round starts, otherwise your role will be revealed!", $"Ultimatum : tue dans les {s}s apres le debut de la manche, sinon ton role sera revele !");
            Director($"<b>Ultimatum</b> applied to {target.Data.PlayerName} ({s}s) — starting next round.", $"<b>Ultimatum</b> appliqué à {target.Data.PlayerName} ({s}s) — dès la prochaine manche.", $"Ultimatum applied to {target.Data.PlayerName} ({s}s).", $"Ultimatum applique a {target.Data.PlayerName} ({s}s).");
        }

        public static string Status()
        {
            var lines = new List<string>();
            if (_stalkerOn) lines.Add(Localization.Tr(Localization.CurrentLang, "Stalker active", "Stalker actif"));
            if (_pendStalkerA.HasValue) lines.Add(Localization.Tr(Localization.CurrentLang, "Stalker scheduled (next round)", "Stalker programmé (prochaine manche)"));
            if (_ultimatumOn) lines.Add(Localization.Tr(Localization.CurrentLang, $"Ultimatum in progress ({Mathf.CeilToInt(_ultimatumTimer)}s)", $"Ultimatum en cours ({Mathf.CeilToInt(_ultimatumTimer)}s)"));
            if (_pendUltimatumId.HasValue) lines.Add(Localization.Tr(Localization.CurrentLang, "Ultimatum scheduled (next round)", "Ultimatum programmé (prochaine manche)"));
            if (_timed.Count > 0) lines.Add(Localization.Tr(Localization.CurrentLang, $"{_timed.Count} timed effect(s)", $"{_timed.Count} effet(s) temporisé(s)"));
            return string.Join(", ", lines);
        }
    }
}
