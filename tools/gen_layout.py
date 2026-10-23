#!/usr/bin/env python3
"""
Generates the BlockingZones panorama: panorama/layout/custom_game/bz.xml + panorama/styles/custom_game/bz.css.
Do not edit those two by hand - change this file and run:  python tools/gen_layout.py

Design "BZ - зелёная панорама" (Claude Design canvas). Digits only, no mouse: nothing has hittest,
no input capture. Three roots, all hidden as authored; the plugin removes .bz-hidden per player:
  bz-menu  - the numbered menu (main, settings, rotation, visibility, quota, model list, aux, confirm)
  bz-prev  - preview strip at the bottom (ghost placement keys 1-5, 0)
  bz-note  - "part of the map is closed" banner for every player

Ids / dialog variables / classes driven by the plugin (Hud.cs) - keep both sides in sync:
  menu   bz-title{title} bz-sub{sub} bz-card(c-off) bz-card-num{cardnum} bz-card-name{cardname}
         bz-card-info{cardinfo} bz-now(c-off) bz-now-cap{nowcap} bz-now-val{nowval}
         bz-r<i>(r-off r-sel r-hit r-danger r-acc r-bar n-chip n-warn n-val) bz-r<i>-k{r<i>k}
         bz-r<i>-t{r<i>t} bz-r<i>-n{r<i>n} bz-r<i>-bar(b-0..b-10)
         bz-nav8(c-off) bz-nav8-t{nav8} bz-nav0-t{nav0} bz-status(s-ok s-err){status}
  prev   bz-p-cap{pcap} bz-p-name{pname} bz-p-state(p-ok p-bad) bz-p-state-t{pstate} bz-p-ang{pang}
         bz-pk<k>(k-hit) bz-pk<k>-t{pk<k>}  k = 1..5, 0
  note   bz-note(n-show: fade in / out) bz-n-title{ntitle} bz-n-text{ntext} bz-n-ladder(lv-0..lv-10)
         bz-n-alive{nalive} bz-n-need{nneed} bz-n-timer-fill(t-run + td-0 / td-<seconds>: countdown bar)
"""
import pathlib

import re
import sys

# 0.75: menu and preview strip next to the PMM / SimpleAdmin panel size (in game 2026-10-23: 1.0 was too big).
MENU_SCALE = 0.75
NOTICE_SECONDS = range(2, 31)   # td-2 .. td-30 (config NoticeSeconds is clamped 2..30)

ROWS = 8                      # menu rows: keys 1-7 and 9 (8 / 0 are the nav line)
PREVIEW_KEYS = [1, 2, 3, 4, 5, 0]
SEGMENTS = 10

# python3 tools/gen_layout.py [plugin folder]   (default: BlockingZones) -> <folder>/panorama/...
_BASE = pathlib.Path(__file__).resolve().parent.parent
ROOT = (pathlib.Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else _BASE / "BlockingZones") / "panorama"

# palette (design)
BG = "#0c1511f2"
HEAD = "#0f1d16"
LINE = "#1f3d2e"
LINE_SOFT = "#16291f"
CHIP = "#13281d"
KEY_LINE = "#2c5540"
SEL = "#173a29"
ACC = "#3ddc84"
LIGHT = "#a3f7bf"
TEXT = "#e8f5ec"
MUTED = "#8fb5a0"
DARK = "#06120c"
DANGER = "#ff8a80"
DANGER_BG = "#2a1414"
DANGER_LINE = "#5a2a2a"
WARN = "#f5c451"
FOOT = "#0a120e"
CARD = "#112219"
HIT = "#24573d"


def xml() -> str:
    o = []
    a = o.append
    a('<root>')
    a('\t<styles>')
    a('\t\t<include src="s2r://panorama/styles/csgostyles.vcss_c" />')
    a('\t\t<include src="s2r://panorama/styles/custom_game/bz.vcss_c" />')
    a('\t</styles>')
    a('\t<Panel class="bz-screen" hittest="false">')

    # ---------------- menu ----------------
    a('\t\t<Panel id="bz-menu" class="bz-menu bz-hidden" hittest="false">')
    a('\t\t\t<Panel class="bz-head" hittest="false">')
    a('\t\t\t\t<Panel class="bz-logo" hittest="false"><Label class="bz-logo-t stratum-bold-tf" text="BZ" /></Panel>')
    a('\t\t\t\t<Panel class="bz-head-text" hittest="false">')
    a('\t\t\t\t\t<Label id="bz-title" class="bz-title stratum-bold-tf" text="{s:title}" />')
    a('\t\t\t\t\t<Label id="bz-sub" class="bz-sub stratum-medium-tf" text="{s:sub}" />')
    a('\t\t\t\t</Panel>')
    a('\t\t\t</Panel>')
    a('\t\t\t<Panel id="bz-card" class="bz-card c-off" hittest="false">')
    a('\t\t\t\t<Panel class="bz-card-box" hittest="false"><Label id="bz-card-num" class="bz-card-num stratum-bold-tf" text="{s:cardnum}" /></Panel>')
    a('\t\t\t\t<Panel class="bz-card-text" hittest="false">')
    a('\t\t\t\t\t<Label id="bz-card-name" class="bz-card-name stratum-bold-tf" text="{s:cardname}" />')
    a('\t\t\t\t\t<Label id="bz-card-info" class="bz-card-info stratum-medium-tf" text="{s:cardinfo}" />')
    a('\t\t\t\t</Panel>')
    a('\t\t\t</Panel>')
    a('\t\t\t<Panel id="bz-now" class="bz-now c-off" hittest="false">')
    a('\t\t\t\t<Label id="bz-now-cap" class="bz-now-cap stratum-medium-tf" text="{s:nowcap}" />')
    a('\t\t\t\t<Label id="bz-now-val" class="bz-now-val stratum-bold-tf" text="{s:nowval}" />')
    a('\t\t\t</Panel>')
    a('\t\t\t<Panel class="bz-rows" hittest="false">')
    for i in range(ROWS):
        a(f'\t\t\t\t<Panel id="bz-r{i}" class="bz-row r-off" hittest="false">')
        a(f'\t\t\t\t\t<Panel class="bz-key" hittest="false"><Label id="bz-r{i}-k" class="bz-key-t stratum-bold-tf" text="{{s:r{i}k}}" /></Panel>')
        a(f'\t\t\t\t\t<Label id="bz-r{i}-t" class="bz-row-t stratum-medium-tf" text="{{s:r{i}t}}" />')
        a(f'\t\t\t\t\t<Panel class="bz-bar" hittest="false"><Panel id="bz-r{i}-bar" class="bz-bar-fill b-0" hittest="false" /></Panel>')
        a(f'\t\t\t\t\t<Label id="bz-r{i}-n" class="bz-row-n stratum-medium-tf" text="{{s:r{i}n}}" />')
        a('\t\t\t\t</Panel>')
    a('\t\t\t\t<Panel class="bz-sep" hittest="false" />')
    a('\t\t\t\t<Panel class="bz-nav" hittest="false">')
    a('\t\t\t\t\t<Panel id="bz-nav8" class="bz-nav-cell" hittest="false">')
    a('\t\t\t\t\t\t<Panel class="bz-key bz-key-nav" hittest="false"><Label class="bz-key-t stratum-bold-tf" text="8" /></Panel>')
    a('\t\t\t\t\t\t<Label id="bz-nav8-t" class="bz-nav-t stratum-medium-tf" text="{s:nav8}" />')
    a('\t\t\t\t\t</Panel>')
    a('\t\t\t\t\t<Panel class="bz-nav-cell" hittest="false">')
    a('\t\t\t\t\t\t<Panel class="bz-key bz-key-nav" hittest="false"><Label class="bz-key-t stratum-bold-tf" text="0" /></Panel>')
    a('\t\t\t\t\t\t<Label id="bz-nav0-t" class="bz-nav-t stratum-medium-tf" text="{s:nav0}" />')
    a('\t\t\t\t\t</Panel>')
    a('\t\t\t\t</Panel>')
    a('\t\t\t</Panel>')
    a('\t\t\t<Panel class="bz-foot" hittest="false"><Label id="bz-status" class="bz-status stratum-medium-tf" text="{s:status}" /></Panel>')
    a('\t\t</Panel>')

    # ---------------- preview ----------------
    a('\t\t<Panel id="bz-prev" class="bz-prev bz-hidden" hittest="false">')
    a('\t\t\t<Panel class="bz-p-head" hittest="false">')
    a('\t\t\t\t<Panel class="bz-logo bz-logo-s" hittest="false"><Label class="bz-logo-t stratum-bold-tf" text="BZ" /></Panel>')
    a('\t\t\t\t<Panel class="bz-p-text" hittest="false">')
    a('\t\t\t\t\t<Label id="bz-p-cap" class="bz-p-cap stratum-medium-tf" text="{s:pcap}" />')
    a('\t\t\t\t\t<Label id="bz-p-name" class="bz-p-name stratum-bold-tf" text="{s:pname}" />')
    a('\t\t\t\t</Panel>')
    a('\t\t\t\t<Panel id="bz-p-state" class="bz-p-state p-ok" hittest="false">')
    a('\t\t\t\t\t<Panel class="bz-p-dot" hittest="false" />')
    a('\t\t\t\t\t<Label id="bz-p-state-t" class="bz-p-state-t stratum-medium-tf" text="{s:pstate}" />')
    a('\t\t\t\t</Panel>')
    a('\t\t\t\t<Label id="bz-p-ang" class="bz-p-ang stratum-bold-tf" text="{s:pang}" />')
    a('\t\t\t</Panel>')
    a('\t\t\t<Panel class="bz-p-keys" hittest="false">')
    for k in PREVIEW_KEYS:
        extra = " bz-pk-go" if k == 1 else (" bz-pk-stop" if k == 0 else "")
        a(f'\t\t\t\t<Panel id="bz-pk{k}" class="bz-pk{extra}" hittest="false">')
        a(f'\t\t\t\t\t<Panel class="bz-pk-box" hittest="false"><Label class="bz-pk-num stratum-bold-tf" text="{k}" /></Panel>')
        a(f'\t\t\t\t\t<Label id="bz-pk{k}-t" class="bz-pk-t stratum-medium-tf" text="{{s:pk{k}}}" />')
        a('\t\t\t\t</Panel>')
    a('\t\t\t</Panel>')
    a('\t\t</Panel>')

    # ---------------- notice ----------------
    a('\t\t<Panel id="bz-note" class="bz-note bz-hidden" hittest="false">')
    a('\t\t\t<Panel class="bz-n-body" hittest="false">')
    a('\t\t\t<Panel class="bz-n-head" hittest="false">')
    a('\t\t\t\t<Panel class="bz-lock" hittest="false">')
    a('\t\t\t\t\t<Panel class="bz-lock-arc" hittest="false" />')
    a('\t\t\t\t\t<Panel class="bz-lock-body" hittest="false" />')
    a('\t\t\t\t</Panel>')
    a('\t\t\t\t<Panel class="bz-n-text-box" hittest="false">')
    a('\t\t\t\t\t<Label id="bz-n-title" class="bz-n-title stratum-bold-tf" text="{s:ntitle}" />')
    a('\t\t\t\t\t<Label id="bz-n-text" class="bz-n-text stratum-medium-tf" text="{s:ntext}" />')
    a('\t\t\t\t</Panel>')
    a('\t\t\t</Panel>')
    a('\t\t\t<Panel id="bz-n-ladder" class="bz-n-ladder lv-0" hittest="false">')
    for s in range(1, SEGMENTS + 1):
        a(f'\t\t\t\t<Panel class="bz-seg s{s}" hittest="false" />')
    a('\t\t\t</Panel>')
    a('\t\t\t<Panel class="bz-n-foot" hittest="false">')
    a('\t\t\t\t<Label id="bz-n-alive" class="bz-n-alive stratum-medium-tf" text="{s:nalive}" />')
    a('\t\t\t\t<Label id="bz-n-need" class="bz-n-need stratum-bold-tf" text="{s:nneed}" />')
    a('\t\t\t</Panel>')
    a('\t\t\t</Panel>')
    # countdown: full -> empty over NoticeSeconds (clip transition, duration class td-N)
    a('\t\t\t<Panel class="bz-n-timer" hittest="false"><Panel id="bz-n-timer-fill" class="bz-n-timer-fill" hittest="false" /></Panel>')
    a('\t\t</Panel>')

    a('\t</Panel>')
    a('</root>')
    return "\n".join(o) + "\n"


def rule(sel, *props):
    body = "\n".join(f"\t{p};" for p in props)
    return f"{sel}\n{{\n{body}\n}}\n"


def css() -> str:
    o = ["/* ============================================================",
         "   BlockingZones (bz) - generated by tools/gen_layout.py, do not edit by hand.",
         "   Digits only: no hittest, no input capture. Roots hidden as authored (.bz-hidden).",
         f"   Accent {ACC}, panel {BG}, border {LINE}.",
         "   ============================================================ */", ""]
    a = o.append

    a(rule(".bz-screen", "width: 100%", "height: 100%", "overflow: noclip", "z-index: 99999"))
    a(rule(".bz-hidden", "visibility: collapse"))
    a(rule(".c-off", "visibility: collapse"))

    # ---- everything up to the notice is scaled by MENU_SCALE (see scaled() below) ----
    a("/*@scale-start*/")
    # shared logo
    a(rule(".bz-logo", "width: 58px", "height: 58px", "vertical-align: center", "border-radius: 10px",
           f"background-color: {ACC}", "margin-right: 18px"))
    a(rule(".bz-logo-s", "width: 48px", "height: 48px", "border-radius: 9px"))
    a(rule(".bz-logo-t", "horizontal-align: center", "vertical-align: center", "font-size: 28px", f"color: {DARK}"))
    a(rule(".bz-logo-s .bz-logo-t", "font-size: 22px"))

    # ---------------- menu ----------------
    a("/* ---------- menu ---------- */\n")
    a(rule(".bz-menu", "width: 590px", "horizontal-align: left", "vertical-align: center", "margin-left: 64px",
           "flow-children: down", f"background-color: {BG}", f"border: 1px solid {LINE}", "border-radius: 12px",
           "box-shadow: #00000099 0px 24px 56px 0px", "overflow: clip clip"))
    a(rule(".bz-head", "width: 100%", "flow-children: right", "padding: 20px 24px 18px 24px",
           f"background-color: {HEAD}", f"border-bottom: 1px solid {LINE}"))
    a(rule(".bz-head-text", "width: fill-parent-flow( 1.0 )", "vertical-align: center", "flow-children: down"))
    a(rule(".bz-title", "width: 100%", "font-size: 30px", "letter-spacing: 2px", "text-transform: uppercase",
           f"color: {TEXT}", "text-overflow: shrink", "white-space: nowrap"))
    a(rule(".bz-sub", "width: 100%", "font-size: 18px", f"color: {MUTED}", "margin-top: 2px",
           "text-overflow: shrink", "white-space: nowrap"))

    a(rule(".bz-card", "width: fill-parent-flow( 1.0 )", "flow-children: right", "margin: 16px 18px 4px 18px",
           "padding: 14px 18px", "border-radius: 10px", f"background-color: {CARD}", f"border: 1px solid {LINE}"))
    a(rule(".bz-card-box", "width: 60px", "height: 60px", "vertical-align: center", "border-radius: 8px",
           f"background-color: {SEL}", "margin-right: 18px"))
    a(rule(".bz-card-num", "horizontal-align: center", "vertical-align: center", "font-size: 27px", f"color: {LIGHT}"))
    a(rule(".bz-card-text", "width: fill-parent-flow( 1.0 )", "vertical-align: center", "flow-children: down"))
    a(rule(".bz-card-name", "width: 100%", "font-size: 23px", f"color: {TEXT}", "text-overflow: shrink", "white-space: nowrap"))
    a(rule(".bz-card-info", "width: 100%", "font-size: 16px", f"color: {MUTED}", "margin-top: 3px",
           "text-overflow: shrink", "white-space: nowrap"))

    a(rule(".bz-now", "width: 100%", "flow-children: right", "padding: 14px 26px", f"border-bottom: 1px solid {LINE_SOFT}"))
    a(rule(".bz-now-cap", "width: fill-parent-flow( 1.0 )", "vertical-align: center", "font-size: 16px",
           "letter-spacing: 2px", "text-transform: uppercase", f"color: {MUTED}"))
    a(rule(".bz-now-val", "vertical-align: center", "font-size: 32px", f"color: {ACC}"))

    a(rule(".bz-rows", "width: 100%", "flow-children: down", "padding: 12px 14px 10px 14px"))
    a(rule(".bz-row", "width: 100%", "height: 58px", "flow-children: right", "padding: 0px 14px",
           "margin-bottom: 3px", "border-radius: 9px", "background-color: #00000000",
           "transition-property: background-color", "transition-duration: 0.12s"))
    a(rule(".bz-row.r-off", "visibility: collapse"))
    a(rule(".bz-key", "width: 42px", "height: 42px", "vertical-align: center", "border-radius: 8px",
           f"border: 1px solid {KEY_LINE}", f"background-color: {CHIP}", "margin-right: 18px"))
    a(rule(".bz-key-t", "horizontal-align: center", "vertical-align: center", "font-size: 26px", f"color: {ACC}"))
    a(rule(".bz-row-t", "width: fill-parent-flow( 1.0 )", "vertical-align: center", "font-size: 23px",
           f"color: {TEXT}", "text-overflow: shrink", "white-space: nowrap"))
    a(rule(".bz-row-n", "vertical-align: center", "font-size: 17px", f"color: {MUTED}", "margin-left: 12px",
           "text-overflow: shrink", "white-space: nowrap"))

    # note styles
    a(rule(".bz-row.n-chip .bz-row-n", "padding: 4px 10px", "border-radius: 5px", f"background-color: {CHIP}",
           "font-size: 14px", "letter-spacing: 1px", "text-transform: uppercase", f"color: {MUTED}"))
    a(rule(".bz-row.n-warn .bz-row-n", f"color: {WARN}"))
    a(rule(".bz-row.n-val .bz-row-n", "font-size: 27px", f"color: {ACC}"))

    # mini bar (visibility): clip ladder, not width
    a(rule(".bz-bar", "width: 120px", "height: 8px", "vertical-align: center", "border-radius: 4px",
           f"background-color: {CHIP}", "margin-left: 12px", "visibility: collapse"))
    a(rule(".bz-row.r-bar .bz-bar", "visibility: visible"))
    a(rule(".bz-bar-fill", "width: 100%", "height: 100%", "border-radius: 4px", f"background-color: {ACC}",
           "transition-property: clip", "transition-duration: 0.2s"))
    for b in range(0, 11):
        a(f".bz-bar-fill.b-{b} {{ clip: rect( 0%, {b * 10}%, 100%, 0% ); }}")
    a("")

    # row states
    a(rule(".bz-row.r-sel", f"background-color: {SEL}"))
    a(rule(".bz-row.r-sel .bz-key", f"background-color: {ACC}", f"border: 1px solid {ACC}"))
    a(rule(".bz-row.r-sel .bz-key-t", f"color: {DARK}"))
    a(rule(".bz-row.r-sel .bz-row-t", "color: #ffffff"))
    a(rule(".bz-row.r-hit", f"background-color: {HIT}"))
    a(rule(".bz-row.r-hit .bz-key", f"background-color: {ACC}", f"border: 1px solid {ACC}"))
    a(rule(".bz-row.r-hit .bz-key-t", f"color: {DARK}"))
    a(rule(".bz-row.r-acc .bz-row-t", f"color: {LIGHT}"))
    a(rule(".bz-row.r-danger .bz-key", f"background-color: {DANGER_BG}", f"border: 1px solid {DANGER_LINE}"))
    a(rule(".bz-row.r-danger .bz-key-t", f"color: {DANGER}"))
    a(rule(".bz-row.r-danger .bz-row-t", f"color: {DANGER}"))

    a(rule(".bz-sep", "width: 100%", "height: 1px", f"background-color: {LINE_SOFT}", "margin: 8px 14px 6px 14px"))
    a(rule(".bz-nav", "width: 100%", "height: 52px", "flow-children: right"))
    a(rule(".bz-nav-cell", "width: fill-parent-flow( 1.0 )", "height: 100%", "flow-children: right", "padding: 0px 14px"))
    a(rule(".bz-key-nav", f"border: 1px solid {LINE}", f"background-color: {HEAD}"))
    a(rule(".bz-key-nav .bz-key-t", f"color: {MUTED}"))
    a(rule(".bz-nav-t", "width: fill-parent-flow( 1.0 )", "vertical-align: center", "font-size: 21px", f"color: {MUTED}",
           "text-overflow: shrink", "white-space: nowrap"))

    a(rule(".bz-foot", "width: 100%", "padding: 14px 24px", f"background-color: {FOOT}", f"border-top: 1px solid {LINE_SOFT}"))
    a(rule(".bz-status", "width: 100%", "font-size: 17px", f"color: {MUTED}", "text-overflow: shrink", "white-space: nowrap"))
    a(rule(".bz-status.s-ok", f"color: {LIGHT}"))
    a(rule(".bz-status.s-err", f"color: {DANGER}"))

    # ---------------- preview ----------------
    a("/* ---------- preview ---------- */\n")
    a(rule(".bz-prev", "width: 1040px", "horizontal-align: center", "vertical-align: bottom", "margin-bottom: 170px",
           "flow-children: down", f"background-color: {BG}", f"border: 1px solid {LINE}", "border-radius: 12px",
           "box-shadow: #00000099 0px 24px 56px 0px", "overflow: clip clip"))
    a(rule(".bz-p-head", "width: 100%", "flow-children: right", "padding: 16px 24px", f"background-color: {HEAD}",
           f"border-bottom: 1px solid {LINE}"))
    a(rule(".bz-p-text", "width: fill-parent-flow( 1.0 )", "vertical-align: center", "flow-children: down"))
    a(rule(".bz-p-cap", "font-size: 15px", "letter-spacing: 2px", "text-transform: uppercase", f"color: {MUTED}"))
    a(rule(".bz-p-name", "width: 100%", "font-size: 24px", f"color: {TEXT}", "text-overflow: shrink", "white-space: nowrap"))
    a(rule(".bz-p-state", "vertical-align: center", "flow-children: right", "padding: 7px 15px", "border-radius: 18px",
           f"background-color: {CHIP}", "margin-right: 18px"))
    a(rule(".bz-p-dot", "width: 11px", "height: 11px", "vertical-align: center", "border-radius: 6px",
           f"background-color: {ACC}", "margin-right: 9px"))
    a(rule(".bz-p-state-t", "vertical-align: center", "font-size: 18px", f"color: {LIGHT}"))
    a(rule(".bz-p-state.p-bad", f"background-color: {DANGER_BG}"))
    a(rule(".bz-p-state.p-bad .bz-p-dot", f"background-color: {DANGER}"))
    a(rule(".bz-p-state.p-bad .bz-p-state-t", f"color: {DANGER}"))
    a(rule(".bz-p-ang", "vertical-align: center", "font-size: 20px", f"color: {ACC}"))

    a(rule(".bz-p-keys", "width: 100%", "flow-children: right", "padding: 18px 16px 20px 22px"))
    a(rule(".bz-pk", "width: 158px", "height: 112px", "margin-right: 10px", "flow-children: down",
           "border-radius: 10px", f"background-color: {HEAD}", f"border: 1px solid {LINE}",
           "transition-property: background-color", "transition-duration: 0.12s"))
    a(rule(".bz-pk-box", "width: 54px", "height: 54px", "horizontal-align: center", "margin-top: 13px",
           "border-radius: 9px", f"background-color: {CHIP}", f"border: 1px solid {KEY_LINE}"))
    a(rule(".bz-pk-num", "horizontal-align: center", "vertical-align: center", "font-size: 32px", f"color: {ACC}"))
    a(rule(".bz-pk-t", "width: 100%", "horizontal-align: center", "text-align: center", "margin-top: 9px",
           "font-size: 19px", f"color: {TEXT}", "text-overflow: shrink", "white-space: nowrap"))
    a(rule(".bz-pk.bz-pk-go", f"background-color: {SEL}", f"border: 1px solid {ACC}"))
    a(rule(".bz-pk.bz-pk-go .bz-pk-box", f"background-color: {ACC}", f"border: 1px solid {ACC}"))
    a(rule(".bz-pk.bz-pk-go .bz-pk-num", f"color: {DARK}"))
    a(rule(".bz-pk.bz-pk-go .bz-pk-t", "color: #ffffff"))
    a(rule(".bz-pk.bz-pk-stop", "background-color: #1a0f0f", f"border: 1px solid {DANGER_LINE}"))
    a(rule(".bz-pk.bz-pk-stop .bz-pk-box", f"background-color: {DANGER_BG}", f"border: 1px solid {DANGER_LINE}"))
    a(rule(".bz-pk.bz-pk-stop .bz-pk-num", f"color: {DANGER}"))
    a(rule(".bz-pk.bz-pk-stop .bz-pk-t", f"color: {DANGER}"))
    a(rule(".bz-pk.k-hit", f"background-color: {HIT}", f"border: 1px solid {LIGHT}"))

    a("/*@scale-end*/")

    # ---------------- notice ----------------
    a("/* ---------- notice: fades in/out with .n-show, countdown bar at the bottom ---------- */\n")
    a(rule(".bz-note", "width: 680px", "horizontal-align: center", "vertical-align: top", "margin-top: 140px",
           "flow-children: down", f"background-color: {BG}", f"border: 1px solid {LINE}", "border-radius: 14px",
           "box-shadow: #00000099 0px 24px 56px 0px", "overflow: clip clip",
           "opacity: 0", "transform: translateY( -16px )",
           "transition-property: opacity, transform", "transition-duration: 0.4s",
           "transition-timing-function: ease-out"))
    a(rule(".bz-note.n-show", "opacity: 1", "transform: translateY( 0px )"))
    a(rule(".bz-n-body", "width: 100%", "flow-children: down", "padding: 22px 28px 18px 28px"))
    a(rule(".bz-n-timer", "width: 100%", "height: 6px", "background-color: #1c3327"))
    # Countdown. Panorama may take the transition duration from the old state or from the new one, so the
    # plugin never changes the duration and the clip in the same step:
    #   td-0 (instant) -> remove .t-run (snaps to full) -> td-N -> add .t-run (drains in N s).
    a(rule(".bz-n-timer-fill", "width: 100%", "height: 100%", f"background-color: {ACC}",
           "clip: rect( 0%, 100%, 100%, 0% )", "transition-property: clip", "transition-duration: 0s",
           "transition-timing-function: linear"))
    a(rule(".bz-n-timer-fill.t-run", "clip: rect( 0%, 0%, 100%, 0% )"))
    a(".bz-n-timer-fill.td-0 { transition-duration: 0s; }")
    for sec in NOTICE_SECONDS:
        a(f".bz-n-timer-fill.td-{sec} {{ transition-duration: {sec}s; }}")
    a("")
    a(rule(".bz-n-head", "width: 100%", "flow-children: right"))
    a(rule(".bz-lock", "width: 66px", "height: 66px", "vertical-align: center", "border-radius: 13px",
           f"background-color: {SEL}", "margin-right: 22px", "flow-children: down"))
    a(rule(".bz-lock-arc", "width: 22px", "height: 16px", "horizontal-align: center", "margin-top: 13px",
           f"border: 4px solid {ACC}", "border-radius: 11px 11px 0px 0px"))
    a(rule(".bz-lock-body", "width: 32px", "height: 22px", "horizontal-align: center", "border-radius: 4px",
           f"background-color: {ACC}"))
    a(rule(".bz-n-text-box", "width: fill-parent-flow( 1.0 )", "vertical-align: center", "flow-children: down"))
    a(rule(".bz-n-title", "width: 100%", "font-size: 30px", "letter-spacing: 2px", "text-transform: uppercase",
           f"color: {TEXT}", "text-overflow: shrink", "white-space: nowrap"))
    a(rule(".bz-n-text", "width: 100%", "font-size: 20px", "color: #a3c9b4", "margin-top: 4px",
           "text-overflow: shrink", "white-space: nowrap"))
    a(rule(".bz-n-ladder", "width: 100%", "height: 12px", "flow-children: right", "margin-top: 18px"))
    a(rule(".bz-seg", "width: fill-parent-flow( 1.0 )", "height: 100%", "margin-right: 6px", "border-radius: 3px",
           "background-color: #1c3327"))
    a(rule(".bz-seg.s10", "margin-right: 0px"))
    for lv in range(1, SEGMENTS + 1):
        sels = ", ".join(f".bz-n-ladder.lv-{lv} .s{s}" for s in range(1, lv + 1))
        a(f"{sels} {{ background-color: {ACC}; }}")
    a("")
    a(rule(".bz-n-foot", "width: 100%", "flow-children: right", "margin-top: 10px"))
    a(rule(".bz-n-alive", "width: fill-parent-flow( 1.0 )", "font-size: 17px", f"color: {MUTED}"))
    a(rule(".bz-n-need", "font-size: 17px", f"color: {ACC}"))

    return scaled("\n".join(o))


def scaled(text: str) -> str:
    """Multiplies every px value between the @scale markers by MENU_SCALE (1-2px borders stay)."""
    a = text.index("/*@scale-start*/")
    b = text.index("/*@scale-end*/")

    def px(m):
        v = float(m.group(1))
        if v <= 2:
            return m.group(0)
        return f"{round(v * MENU_SCALE)}px"

    mid = re.sub(r"(\d+(?:\.\d+)?)px", px, text[a:b])
    mid = mid.replace("/*@scale-start*/", f"/* menu + preview: sizes x{MENU_SCALE} */")
    return text[:a] + mid + text[b + len("/*@scale-end*/"):]


def main():
    lay = ROOT / "layout" / "custom_game" / "bz.xml"
    sty = ROOT / "styles" / "custom_game" / "bz.css"
    lay.parent.mkdir(parents=True, exist_ok=True)
    sty.parent.mkdir(parents=True, exist_ok=True)
    lay.write_text(xml(), encoding="utf-8", newline="\n")
    sty.write_text(css(), encoding="utf-8", newline="\n")
    print("written", lay)
    print("written", sty)


if __name__ == "__main__":
    main()
