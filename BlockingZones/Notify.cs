using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace BlockingZones;

// MessageMode: 0 off, 1 chat, 2 Panorama banner (bz-note of the bz layout), 3 both.
// The CS:GO HUD mode (ShowHudText) has no CS2 counterpart; the banner replaces it.
public partial class BlockingZonesPlugin
{
    private void Announce(int minPlayers, int alive, int count)
    {
        int mode = Cfg.MessageMode;
        if (mode == 0) return;
        bool chat = mode is 1 or 3;
        bool banner = mode is 2 or 3;

        (string, object)[] args = { ("MINPLAYERS", minPlayers), ("PLAYERS", alive), ("COUNT", count),
            ("MISSING", Math.Max(0, minPlayers - alive)) };
        int level = minPlayers <= 0 ? 10 : Math.Clamp((int)Math.Round(alive * 10.0 / minPlayers), 0, 10);

        foreach (var p in Utilities.GetPlayers())
        {
            if (p == null || !p.IsValid || p.IsBot || p.IsHLTV) continue;
            if (chat) Chat(p, T(p, "notify.chat", args));
            if (banner) ShowNotice(p, args, level);
        }
    }

    // Banner sequence (bz-note, see tools/gen_layout.py). Every step is its own tick: a class switched off
    // and on in the same tick never reaches the client, and Panorama may take a transition's duration from
    // the old state or from the new one - so the duration and the clip are never changed together.
    //   now          texts + ladder, root not collapsed, td-0 (bar transitions become instant)
    //   +0.10 s      remove .t-run          -> bar snaps to full
    //   +0.30 s      .n-show (fade + slide in 0.4 s), td-N (bar transitions take N s)
    //   +0.45 s      .t-run                 -> bar drains to empty in N s
    //   +0.45 s + N  remove .n-show         -> fade out 0.4 s (the bar is empty by then)
    // A newer banner (token) cancels the steps of the older one.
    private void ShowNotice(CCSPlayerController p, (string, object)[] args, int level)
    {
        if (p.Slot is < 0 or >= 64 || !EnsureLayout()) return;

        int seconds = Math.Clamp((int)Math.Round(Cfg.NoticeSeconds), 2, 30);
        const string fill = "bz-n-timer-fill";

        SetVar(p, "bz-n-title", "ntitle", StripColors(T(p, "notify.title", args)));
        SetVar(p, "bz-n-text", "ntext", StripColors(T(p, "notify.text", args)));
        SetVar(p, "bz-n-alive", "nalive", StripColors(T(p, "notify.alive", args)));
        SetVar(p, "bz-n-need", "nneed", StripColors(T(p, "notify.need", args)));
        SetOnly(p, "bz-n-ladder", "lv-" + level, "lv-");
        SetOnly(p, fill, "td-0", "td-");
        SetClass(p, RootNote, Hidden, false);

        var st = _hud[p.Slot];
        int token = ++st.NoteToken;
        bool Same() => p.IsValid && st.NoteToken == token;

        AddTimer(0.10f, () =>
        {
            if (Same()) SetClass(p, fill, "t-run", false);
        });
        AddTimer(0.30f, () =>
        {
            if (!Same()) return;
            SetClass(p, RootNote, "n-show", true);
            SetOnly(p, fill, "td-" + seconds, "td-");
        });
        AddTimer(0.45f, () =>
        {
            if (Same()) SetClass(p, fill, "t-run", true);
        });
        AddTimer(0.45f + seconds, () =>
        {
            if (Same()) SetClass(p, RootNote, "n-show", false);
        });
    }
}
