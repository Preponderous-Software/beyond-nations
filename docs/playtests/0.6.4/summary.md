# Playtest summary: 064b-20261029-1717

10 sessions, build beyond-nations 0.6.3 (b6a0019), generated 2026-10-29 18:27.

**Findings:** 64 reports in 49 groups -- 0 blocker, 12 major, 37 minor, 15 polish.

## Outcomes

| Session | Goal | Outcome | Fun | Minutes | Objectives | Deaths | Findings | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| s01 | Play the first 10 minutes as a brand-new player who has never seen the game; note every moment of confusion. Also check specifically: after you found a settlement and enter it, does the 'Build a market stall' objective complete only when YOU build a stall, or do residents build stalls in your own settlement first (they should not)? | achieved | 3 | 20.4 | 6 | 0 | 9 | The goal check passes: in my own settlement the 'Build a market stall' objective stayed open for ~1950 ticks with a resident present and 0/4 stalls, and completed only when I pressed B myself. The first-arc objectives and refusal messages are clear and well rewarded, but the onboarding stumbles: tips arrive out of order, 'Eat something' completes before it can be read, and finding a legal settlement site (no compass, NPC nations claiming ground ahead of you) is the frustrating stretch of the first ten minutes. |
| s02 | Reach the Village era as fast as you can (it cannot come before day 3, about 17 minutes of game time); record the game time of every objective. | partial | 3 | 23.5 | 6 | 0 | 6 | The early loop is quick and well guided: wood, nation, settlement and a stall took 95 game seconds, and the stockpile, the clear 'too close' message and the lively chronicle all helped. After that the Village era is just a wait on the Day 3 gate, which this overloaded host (about 6 tps) could not reach in the session. The objective panel also hides two of the four Village conditions (stalls and day). The two-step build confirm, which expires in game ticks, and the lack of a compass were the main frictions. |
| s03 | Survive the first winter. Start with `--season autumn` (a fresh world otherwise reaches winter only after about 57 minutes of game time). Note how food and cold feel. | blocked | 3 | 25.3 | 6 | 0 | 6 | Winter never arrived: at ~6 tps only ~3 game minutes passed in 25 wall minutes, so food and cold were not tested. Food looked like no worry before winter, since every tree gives 1-3 apples and I had 28 apples with energy at 91-98. The early loop (chop, nation, settle, farm, stall) reads well and the objectives guide it, but the 'north-west' hint with no compass and an objective bar that showed full when it was not were the main stumbles. |
| s04 | Diplomacy and war: lead a nation, meet a neighbour, try alliance, rivalry and war from the Nations screen (O). This build adds an envoy: every diplomacy act that takes effect (gift/tribute, alliance, denunciation, war, peace) sends your envoy to that nation and blocks further acts toward that nation for 1,000 ticks (20 s of play, which pass only while the Nations screen is closed). Check that acts toward one nation now take game time, that the status line and the Nations screen row explain the wait, that a refused offer sends no envoy, and that each nation has its own envoy. Record the game tick of each act. | partial | 3 | 19.1 | 0 | 0 | 6 | The envoy works as designed. Accepted acts send a separate envoy to each nation, refused offers send none, and blocked acts take no coins. The Nations row counts down 'envoy back in N s', and the header explains the rule well. The major problem is the Nations screen at low frame rates: with frame gaps over 1 s the two-click confirm never holds and rows re-sort under the pointer, which sent one of my clicks to the wrong nation. The status line's frozen '20 seconds' and relation decay leaving three gifts at +29 also need a look. |
| s05 | Trade and caravans: build a market stall, buy and sell, get a Storehouse and send a caravan. Check specifically that residents of YOUR own settlement leave the market stalls to you (they should not fill your market), and that the 'Build a market stall' objective completes only from a stall you build. | partial | 2 | 22.5 | 6 | 0 | 5 | The stall fix holds: residents don't build in the leader's market, and the objective completes only from the player's own B press. The trade loop itself dead-ends, though: a new market's merchant has 0 coins and no stock, so you can't buy or sell, while the panel shows prices as if you could. Caravans are hidden behind Town era, so 'trade and caravans' can't be done in a short session. The game also ran at 3-14 tps here, which made gathering 70+ wood a slog. |
| s06 | Ruler, laws and heirs: lead a nation, enact and repeal laws, and see what happens when the ruler dies. | partial | 2 | 24 | 0 | 0 | 7 | Enacting and repealing laws is clear and satisfying: the panel explains each effect in a line, shows the pending change with a countdown, and the chronicle announces it when it comes into force. The heir side of the goal can't be reached in a session. Hunger is the only way to die and takes most of an in-game day, recruiting anyone to be an heir means chasing pawns that out-walk you and earning their Loyalty, and nothing on screen tells you who your heir is. Rationing is also hidden off the bottom of the Laws tab. |
| s07 | Crafting: find and use every recipe on the crafting screen (C); are tools worth making? | partial | 3 | 16.7 | 2 | 0 | 6 | The crafting screen is very clear: costs, effects, durability, exact shortfalls ('Need 2 more wood'), 'have 1 (19 uses left)', a contextual Craft button in the HUD, and status lines that credit the tool ('+3 from your axe'). Tools are clearly worth making: the Axe (+3 on a 4-6 base, +60% yield) and Pickaxe (+2 on 5, +40%) pay back after 2 uses and give ~60 wood / ~30 stone over their lifetime. But Cooked Meat has no player-facing eat action to show its value, and Planks/Bricks sit behind a Day-3 Village era nobody reaches in a first session. |
| s08 | World events: play until world events are offered, take different choices, and judge whether the consequences are clear and fair. | blocked | 3 | 26.7 | 6 | 0 | 3 | The world events could not be judged: the first one is scheduled at least 420 game seconds in, and at 6-15 tps the session ended at ~279 game seconds with none offered and no nation event in the chronicle. The early game felt good. Objectives chain cleanly, the 'too close, open ground lies 80 to the south-west' message is excellent, and the chronicle feels alive with rivalries and caravans. Testing this goal needs either a faster-loading start or permission to stage an event. |
| s09 | Accessibility and text audit, touching only what a mouse-and-keyboard player would: every screen and panel; cut-off, overlapping, unclear or misspelled text; contrast; anything only explained on hover. | achieved | 3 | 18.5 | 5 | 0 | 12 | The text is mostly well written and the error messages are helpful: too-close distances, 'Click again to confirm' on war, and a 'Sure?' prompt on delete. Legibility is the problem. Low-contrast light-blue menus, translucent panels whose contrast depends on the scene, truncated chronicle lines, and a giant clipped settlement label all get in the way. The game has no tooltips at all, so every number (Village 0/4, +10, Defence 1) and every greyed button is left for the player to guess. |
| s10 | Save/load and share-seed round trip: save from the pause menu, stop, `pt start --continue`, confirm the world is identical; then start a new world from the seed shown on the Share screen and confirm it matches. This build changed the save format (v6) to store diplomacy envoys. Before saving, lead a nation and make at least one diplomacy act on the Nations screen (O) so an envoy is on the road; save while it is away, quit, `pt start --continue`, and confirm the envoy is still away with the same remaining time (Nations screen row 'envoy back in N s') and the rest of the world matches. | achieved | 3 | 10.5 | 0 | 0 | 2 | The v6 save round trip works: with a tribute envoy away (row read 'envoy back in 15 s' at tick 1440), Save, then pt stop and --continue, resumed at tick 1448 with position, heading, coins, inventory, nation, role, objective, settlement and nearby trees/rocks/chickens all identical, and the envoy row read 'envoy back in 12 s' after 112 more ticks (2.2 s), which is exactly the time that had passed. The Share screen shows seed 1450162717 plus a link, and a new world from that number matched the original start exactly (same HUD, region, distances and bearings, the same pawn names, and Isabella Wood again founding the first AI nation and settlement). Save feedback is very good ('Saved: Day 1 - Upperford, 18:03', 'Welcome back'). Two small UI nits only: envoy-away buttons aren't dimmed, and the seed box is awkward to clear. |

## Findings

### 1. [major] Settlement world label renders huge, clipped off-screen and buried under HUD near your own settlement

- **Category:** ux; **reported by** 5 agent(s): s01, s03, s05, s08, s09
- **Also reported as:** "HUD covers over half the 960x540 screen; Inventory Info panel jumps to mid-screen"; "HUD layout breaks inside own settlement: Inventory panel jumps to screen centre, objective bar text clipped, giant floating resident label"; "Huge world-space settlement label is clipped across the top of the screen when inside your own settlement"; "Inside a settlement the Inventory panel jumps to screen centre; own resident labelled 'Stranger'"; "Inside-settlement layout glitch: objective text overprints a hidden button for a frame"; "Settlement HUD reflows: Inventory panel jumps mid-screen, objective bar text clipped, Season overlaps Player Info"
- **Detail:** After founding and while standing in/near the settlement, the floating label 'Camp of Oldtown / 0 here, 1/8 residents' is drawn at enormous size, cut off by the top edge and overlapped by the status bar, chronicle panel, error banners and toasts. The key info about your own settlement can't be read. Expected a size-clamped label, or one placed clear of the HUD.
- **Repro:** pt start --seed 109; Start, Begin; gather 50 wood; N; walk until F succeeds (~(-13,277)); pt shot
- **Screenshots:** [found4.png](s09/shots/found4.png) [after_stall.png](s05/shots/after_stall.png) [label.png](s09/shots/label.png) [inside.png](s09/shots/inside.png) [pre-stall-zero.png](s01/shots/pre-stall-zero.png) [layout.png](s03/shots/layout.png) [overlap_label.png](s08/shots/overlap_label.png)

### 2. [major] Caravans are locked behind Town era and Storehouse needs 30 stone: out of reach of a 30-minute trade session

- **Category:** balance; **reported by** 2 agent(s): s05, s07
- **Also reported as:** "Planks and Bricks (2 of 5 recipes) are locked behind Village era, which needs Day 3: unreachable in a normal short session"
- **Detail:** Inside my settlement the 'Send Caravan (Town era)' button is greyed; Town is era 3 of 4 (Camp -> Village needs 10 members, 2 buildings, 2 stalls, day 3). The Storehouse costs 10 wood + 30 stone. With the game running at 3-14 tps on a shared machine, reaching the stall objective took ~22 real minutes, so the caravan half of the goal was unreachable. A player hunting for the trade loop will see the caravan button almost immediately but not be able to use it for hours of play.
- **Repro:** as above; enter own settlement and look at the 'Send Caravan (Town era)' button
- **Screenshots:** [inside_overlay.png](s05/shots/inside_overlay.png) [era_overlap.png](s07/shots/era_overlap.png)

### 3. [major] Game runs at 4-11 ticks/s (1-2 fps) under shared load, making the Day 3 Village gate unreachable in a session

- **Category:** perf; **reported by** 2 agent(s): s02, s03
- **Also reported as:** "Game runs at 3-11 tps / 1-2 fps, so 'survive the first winter' is unreachable in a session even with --season autumn"
- **Detail:** Throughout the session pt status/state showed tps 4-11 of 50 and fps 0-2 (host load average ~94 on 20 cores, so partly environmental). After ~23 min of wall clock the world was only at tick 7324 (day 1, timeOfDay 0.54). Village needs Day 3 (~tick 60000), i.e. 2-3 more hours of wall time. Every other Village condition except Members (7/10) was met by tick 6976 (~140 game s), so the day gate is the entire wait.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-wt-playtest064 --seed 102 on a busy host; pt status repeatedly
- **Screenshots:** [perf.png](s02/shots/perf.png) [perf.png](s03/shots/perf.png)

### 4. [major] A freshly built own market cannot trade at all: Sell and Purchase Food both fail, yet Market Info shows live prices

- **Category:** balance; **reported by** 1 agent(s): s05
- **Detail:** Right after building the first stall in my settlement (resident Nancy Walker becomes its merchant), Sell Resources says 'No merchant here can pay: the richest has 0 coins, and your cheapest good (sapling) sells for 19.' and Purchase Food says 'No one is selling food at this market...'. Still the same ~8 game seconds later. Meanwhile the Market Info panel lists buy/sell prices with trend arrows for 9 goods, which reads as a working market. Expected: either the new stall is seeded with a little coin/stock, or the panel says the merchant has nothing to trade yet. As a player whose goal is 'buy and sell', I had 437 coins and 31 apples and could do neither. Sell also offers saplings first as 'cheapest good', which is surprising.
- **Repro:** pt start --seed 105; gather 50 wood, N, F on open ground, E to enter, gather 20 wood + 5 stone, H, E, B; click Sell Resources (187,444) and Purchase Food (66,444)
- **Screenshots:** [market2.png](s05/shots/market2.png)

### 5. [major] Chronicle HUD lines truncated with '...' and no way to read them on hover, incl. your own nation's name

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** HUD chronicle lines end mid-word: 'You raised a banner and named your people Ol...', 'declared the nation of Farmland, tho...', 'have become rivals ov...'. Hovering gives no tooltip. The announcement of the player's own nation cuts off its name. The full text is only in the Y window. Also, the player is never asked to name the nation; it is auto-named.
- **Repro:** Start a world; press N; pt move 500 84; pt shot
- **Screenshots:** [chron_hover.png](s09/shots/chron_hover.png)

### 6. [major] First world event can't be reached in a 30-minute session: it is scheduled 420-660 game s in, and the game ran at 6-15 tps

- **Category:** perf; **reported by** 1 agent(s): s08
- **Detail:** I played normally (5 objectives, nation, settlement, stall), then let time pass. After about 26 wall minutes the game was at tick 13928 (~279 game seconds). WorldEvents.cs schedules the first player event at FIRST_PLAYER_EVENT_MIN_SECONDS=420 (tick 21000+). Nation events start at 240 s or later (tick 12000+), and none had shown up in the chronicle by tick 13928. state.worldEvent was null the whole session. At the load this box was under (load average ~80-100, tps 5.5-15.9), seeing an event takes about an hour of wall time. Expected: a playtest session with this goal can reach an event, either with a shorter first-event delay, a staged-event switch the brief allows, or a save where an event is pending.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-wt-playtest064 --seed 108; play through objectives 1-6; pt wait in 45 s chunks until about 26 min wall; pt state shows worldEvent null at tick 13928
- **Screenshots:** [no_event.png](s08/shots/no_event.png)

### 7. [major] Laws tab hides the fourth law (Rationing) at 960x540: no scroll and the row is cut

- **Category:** bug; **reported by** 1 agent(s): s06
- **Detail:** The Laws tab shows Taxes, Conscription and Borders. The game defines a fourth law, Rationing (Laws.All). LawsPanel.draw stops adding rows when the next block would not fit above the footer, and the tab has no scroll (the Nations tab does have Up/Down). So at this window size Rationing cannot be seen or enacted. Expected: every law can be reached, by scrolling or a more compact layout.
- **Repro:** pt start ... --seed 106; N; O; click Laws (543,45); look below Borders
- **Screenshots:** [laws5.png](s06/shots/laws5.png)

### 8. [major] Nations screen at ~1 fps treats every frame as a reopen: War/Denounce confirm never holds and rows re-sort under the pointer

- **Category:** bug; **reported by** 1 agent(s): s04
- **Detail:** At 1-2 fps the Nations screen disarms the two-click confirm between frames. Denounce on Northport showed 'Confirm' on the button, but two quick click pairs each left 'Click Denounce again to denounce Northport.' and nothing happened. One War pair did go through. The kept row order is also dropped, so rows re-sort while the screen is open. After War on Whitewood (tick 2644), Whitewood jumped to row 1 and Southernport's War button slid under the pointer. After Peace (tick 3728), Whitewood dropped back to row 2, and a click aimed at Southernport's Tribute hit Whitewood instead. Cause seen in the source (read-only): NationScreen.ReopenGapSeconds = 1.0, measured between frames with ImGui.GetTime, so any frame gap over 1 s clears shownOrder and disarms the ConfirmGate. Expected: a reopen is detected from the screen being closed, not from frame time, so confirms and row order survive slow frames.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-wt-playtest064 --seed 104 (on a loaded machine, fps 1); key n; wait until 2+ AI nations exist; key o; pt click on a War or Denounce button twice ~1 s apart; or act on row 1 and watch the rows re-order
- **Screenshots:** [denounce_np3.png](s04/shots/denounce_np3.png)

### 9. [major] Nobody can be recruited as an heir: pawns out-walk the player and need 'Loyal' before R works

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** Succession needs a member pawn, but recruiting one is very hard early. Pawns wander as fast as the player walks: 6 walkto attempts between ticks 600 and 4,400 mostly ended 'stuck: not getting closer' at 20-50 m. The two pawns I reached were no use. Frank Brown led his own nation already. Charlotte Taylor said 'I'm loyal to Southham.' (needs Loyal). Three 10-coin gifts only moved her from Stranger to Acquaintance before she walked out of reach. Within 6,000 ticks NPCs had founded 9 nations and taken in most pawns. My nation stayed at 1 member, so any death would have ended the run.
- **Repro:** pt start ... --seed 106; create nation (N); pt walkto pawn 60 (repeat); pt key r; pt key g
- **Screenshots:** [chase2.png](s06/shots/chase2.png)

### 10. [major] Ruler death (and so succession) cannot be reached in a play session: hunger is the only death and takes ~25,000 ticks

- **Category:** balance; **reported by** 1 agent(s): s06
- **Detail:** The goal asks to see what happens when the ruler dies. The player only dies of hunger: energy drains 100 per in-game day (30,000 ticks), starting at 85. In 6,156 ticks energy went 85 -> 64.7. There is no combat, hazard or other death. Even at full speed (50 tps) a death takes over 8 real minutes of standing still; on this machine (4-10 tps) it would have taken ~50 min, so the Succession screen ('THE RULE PASSES ON') was never seen. Expected: some way for a ruler to die or abdicate within a reasonable session, or a faster test path.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-wt-playtest064 --seed 106; space; Start; Begin; N; pt wait ... ; pt state (energy falls ~0.0033/tick)
- **Screenshots:** [repeal.png](s06/shots/repeal.png)

### 11. [minor] Found-settlement refusal says 'open ground lies ~81 to the north' but the HUD has no compass

- **Category:** ux; **reported by** 5 agent(s): s01, s02, s03, s05, s09
- **Also reported as:** "'Open ground lies about N to the north' with no compass on the HUD"; "Found-settlement 'open ground lies to the north' hint leads into another camp's exclusion zone; no compass on screen"; "Found-settlement refusal gives a compass direction but the HUD has no compass or map"; "Found-settlement refusal says 'open ground lies about 280 to the west' but nothing on screen shows direction"
- **Detail:** Pressing F too close to another camp gives a helpful message with a compass direction, but nothing on screen shows which way north is. I had to infer it (north = +Z). Expected a compass / heading indicator, or a marker for the suggested site.
- **Repro:** ...gather 50 wood near spawn; pt key f (at ~(-43,132))
- **Screenshots:** [no-compass.png](s01/shots/no-compass.png) [nocompass.png](s02/shots/nocompass.png) [nocompass.png](s03/shots/nocompass.png) [found_chase.png](s05/shots/found_chase.png) [founded.png](s09/shots/founded.png)

### 12. [minor] Huge settlement billboard ('0 here, 0/8 residents') covers the top HUD and disagrees with the panel

- **Category:** text, ux; **reported by** 2 agent(s): s01, s02
- **Also reported as:** "Settlement world label renders huge across the top of the HUD, and pawn name labels overlap the objective panel"
- **Detail:** At/inside my own settlement a giant floating label spans the top of the screen behind the chronicle and toasts, half-hidden. Right after founding it read '0/8 residents' while Settlement Info said Residents 2/8 (later '1 here, 2/8 residents').
- **Repro:** found a settlement; stand at its well; screenshot
- **Screenshots:** [billboard-overlap.png](s01/shots/billboard-overlap.png) [labels.png](s02/shots/labels.png)

### 13. [minor] Objective 7 progress text is clipped on the left and leaves out two of the four Village conditions

- **Category:** text; **reported by** 2 agent(s): s01, s02
- **Also reported as:** "Objective 7 progress bar text clipped: 'nbers 2/10 - Buildings 0/2'"
- **Detail:** The objective bar reads 'nbers 7/10 · Buildings 2/2' (the 'Me' of Members is cut off). The objective tracks only Members and Buildings, but the era panel needs 4 conditions (Village 1/1, Members x/10, Stalls x/2, Day x/3). A player following only the objective will not know about the stalls or the day gate. Right after the second house the objective still said 'Buildings 1/2' while the era panel already said Village 1/1, until a later refresh.
- **Repro:** Complete objective 6 (build a market stall), then click the Era '+' at (168,58) and compare the era list with the Objective panel
- **Screenshots:** [era.png](s02/shots/era.png) [stall-done.png](s01/shots/stall-done.png)

### 14. [minor] Two-step build confirmation expires in ~40-48 game ticks, too short at low frame rates

- **Category:** ux; **reported by** 2 agent(s): s02, s03
- **Also reported as:** "Build confirm (K twice) times out at low frame rates"
- **Detail:** Pressing K (or clicking Build House) shows 'Press K again / Click again to confirm: Build House', but the confirmation expired ('Not confirmed in time, so nothing was done.') five times in a row because a second press 8-15 s later was more than ~48 ticks at 4-6 tps. Only two clicks fired 3 s apart from one shell command worked. Build Stall, by contrast, built on the first click with no confirmation, which is inconsistent. Expected: a real-time window (or a longer one), and the same behaviour for all build buttons.
- **Repro:** Inside own settlement with 20 wood/15 stone: pt key k; (wait ~10 s) pt key k -> 'Not confirmed in time'. Events ticks 6192-6672.
- **Screenshots:** [confirm.png](s02/shots/confirm.png) [confirm_timeout.png](s03/shots/confirm_timeout.png)

### 15. [minor] 'Eat something' objective completes by itself almost instantly, before the player can read it

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Objective 2 'Eat something' appeared at tick 951 and completed at tick 1003 (52 ticks) while I pressed nothing; energy was ~96, no visible eating feedback, food counts did not visibly drop. The catalog says you eat automatically when energy runs low, but the objective flashed past at near-full energy, so a new player never learns how eating works. Expected: either a visible 'You ate an apple' moment or an objective that waits for a real meal.
- **Repro:** pt start --profile beyond-nations --seed 101; Start; Begin; pt walkto tree; e (x2 trees until Wood 10/10); pt wait 4; pt events 10
- **Screenshots:** [eat-autocomplete.png](s01/shots/eat-autocomplete.png)

### 16. [minor] Cooked Meat advertises '45 energy (raw: 25)' but there is no way to eat on purpose

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** No Eat key on any of the 3 Controls pages and no Eat button in Inventory (I). Food seems to be eaten automatically: energy went 84 -> 99 after the first chop while apples went 2 -> 1, and 90.6 -> 99 around the chicken harvest; objective 2 'Eat something' completed on its own at tick 4550. Nothing on screen says food is auto-eaten or which food is eaten first, so the player cannot tell whether crafting Cooked Meat (1 meat + 1 wood) actually gains anything over raw meat.
- **Repro:** craft Cooked Meat via C; open ? Controls pages 1-3 and Inventory (I); look for an eat action
- **Screenshots:** [no_eat.png](s07/shots/no_eat.png)

### 17. [minor] Crafting screen says 'Ready: you can make 1' for both Axe and Pickaxe when only one can be afforded

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** With 6 wood and 5 stone, Axe (4w 2s) and Pickaxe (3w 3s) both show 'Ready: you can make 1'. Making one leaves too little wood for the other. The per-recipe count is correct alone but misleads when recipes share inputs.
- **Repro:** gather 6 wood and 5 stone; press c
- **Screenshots:** [craft2.png](s07/shots/craft2.png)

### 18. [minor] Crafting tools sets back objective 1 'Gather wood 10' because it counts held wood

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** Objective 1 read Wood 9/10 after 13 wood had been gathered (axe crafted in between). Crafting a pickaxe and cooked meat dropped it to 5/10, and a second pickaxe to 2/10. The game nudges you to craft (contextual 'Craft Pickaxe' button) while the first objective punishes doing so. Expected: count wood gathered, or warn before spending.
- **Repro:** chop 1 tree (6 wood), mine 1 rock, craft Axe, chop a tree -> 9/10; craft Pickaxe -> 6/10
- **Screenshots:** [obj_regress.png](s07/shots/obj_regress.png)

### 19. [minor] Era panel numbers contradict the HUD (Village 0/4 vs 0/1; day 1/2 vs Day 1/3)

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** The collapsed era line reads 'Village: 0/4' but the expanded checklist reads 'Village 0/1'. The checklist's 'Day 1/3' disagrees with the Season panel's 'Spring - day 1/2, year 1'. 'day 1/2' also reads like a fraction.
- **Repro:** Start a world; click the Era box at (110,58)
- **Screenshots:** [era.png](s09/shots/era.png)

### 20. [minor] Finding a legal settlement site is a chase: NPC nations keep claiming ground ahead of you

- **Category:** balance; **reported by** 1 agent(s): s01
- **Detail:** With 53 wood I was refused three times (Newburg 175 away, Yellowfield 98 away, Village of Yellowfield 139 away) as NPC nations founded settlements around me; the 250-unit spacing meant walking ~350 units north. Found-settlement objective took 2800 ticks, the longest of the arc. Messages were clear, but the treadmill felt frustrating in the first 10 minutes.
- **Repro:** seed 101; create nation (N); gather 50 wood around spawn; F repeatedly while heading where the message says
- **Screenshots:** [founded.png](s01/shots/founded.png)

### 21. [minor] Game ran at 1-3 fps and 8-12 tps; screenshots and HUD lag a second or more behind state

- **Category:** perf; **reported by** 1 agent(s): s07
- **Detail:** Keys/clicks often landed late (one E press on a chicken registered only on the next attempt), and the HUD showed stale values (Wood 5/10 while the inventory had 2). With trees and rocks 60-280 m apart in the Moor, walking between them took 30-90 real seconds each.
- **Repro:** pt status / pt state during normal play
- **Screenshots:** [objregress.png](s07/shots/objregress.png)

### 22. [minor] Game runs at 1-2 fps and 5-9 ticks/s, so each 20 s envoy wait takes ~3 minutes of real time

- **Category:** perf; **reported by** 1 agent(s): s04
- **Detail:** pt status showed fps 1-3 and tps 5.6-16.7 for the whole session, on a machine shared with other sessions. The envoy design ties every diplomacy act to 1,000 ticks, which made this goal slow to test.
- **Repro:** pt status during play
- **Screenshots:** [scrolled.png](s04/shots/scrolled.png)

### 23. [minor] Gather feedback inconsistent: status says 3 apples, float says +2, saplings omitted

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** First chop: status 'Gathered 5 wood and 3 apples from tree.', floating '+2 apples', inventory 2. Energy rose 83->96, so one apple was eaten without any message. The status line leaves out the +2 saplings. The floating +N text is pale on bright foliage and is often hidden under the 'Objective complete!' toast.
- **Repro:** Start; pt walkto tree; pt key e
- **Screenshots:** [chop.png](s09/shots/chop.png)

### 24. [minor] Law countdown says 30 s but a repeal took 36.6 s to come into force

- **Category:** text; **reported by** 1 agent(s): s06
- **Detail:** Laws proclaimed at tick 452 came into force at tick 2000 (31 s). The repeal proclaimed at 4172 came in at 6000 (36.6 s), while the panel said 'Off in 30 s'. Changes seem to land on 1000-tick law rounds, so the promised time is a lower bound and not exact.
- **Repro:** O; Laws; click Conscription Off and Borders Closed; pt events (Statecraft chronicle entries)
- **Screenshots:** [repeal.png](s06/shots/repeal.png)

### 25. [minor] Low contrast: title/menus white on light blue (2.5:1), Era header 1.9:1, objective bar text on yellow 1.9:1

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Measured: white on the #73A6E6 title, menu, config, load and new-world background is about 2.5:1, and the menu buttons are barely distinguishable from the background. The Era header (#8CD9FF on #4296FA) is about 1.9:1. The white 'Wood 50/50' on the filled yellow objective bar (#E6B300) is about 1.9:1, so it is least legible exactly when the goal is complete. All are below the WCAG 4.5:1 guideline.
- **Repro:** pt start; pt shot (title); main menu; in world collect 50 wood
- **Screenshots:** [founded.png](s09/shots/founded.png)

### 26. [minor] New nations appear mid-list between visits, so a remembered button position hits the wrong nation

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** Between two visits to the Nations screen, the new nation Whitewood was inserted as row 2, above Castleton. My War click meant for Castleton's row declared war on Whitewood (tick 2644). Sorting 'neighbours and enemies first' is reasonable, but nothing marks a new row as new. A 'new' tag or a highlight on new nations would help.
- **Repro:** seed 104; key n; key o at ~tick 1500 (2 nations), close, reopen at ~tick 2600 (Whitewood is row 2)
- **Screenshots:** [war_whitewood.png](s04/shots/war_whitewood.png)

### 27. [minor] No screen shows the heir or line of succession while you rule

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** Nations tab says 'Your nation: Northburg, 1 member, no settlement. You lead it.' Neither the Laws tab, Player Info nor the Controls screen says who would succeed you, or warns that with no heir your death ends the run. The player can't plan for succession or see that they need to recruit. Expected: an 'Heir: <name>' or 'No heir: your death ends the line' line in the Nations tab or Player Info.
- **Repro:** pt start ... ; N; O (Nations tab); check Player Info panel
- **Screenshots:** [nations2.png](s06/shots/nations2.png)

### 28. [minor] No tooltips anywhere: HUD, Config, Nations/Laws actions and disabled buttons are unexplained

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Hovering Config options (Chunk Size, Location Scale, Lag Prevention, Respawn Pawns), the HUD 'Village: 0/4' and Era box, Nations actions (Tribute/Ally/War/Denounce) and greyed buttons (Build Stall, Build Farm, Enter Settlement) shows no tooltip. Laws only describe the currently selected option, so a player can't see what Low/High tax, Conscription On or Borders Open do before committing a 30 s change. 'Tribute 25 coins for +10' and 'Defence 1' don't say what the number measures.
- **Repro:** Main menu > Config, hover each label; in world press O, hover Denounce; click Laws, hover Low
- **Screenshots:** [laws_hover.png](s09/shots/laws_hover.png)

### 29. [minor] Nothing on screen tells the player that world events exist or when one might come

- **Category:** ux; **reported by** 1 agent(s): s08
- **Detail:** In ~279 game seconds, no HUD element, tip or objective mentioned world events. The chronicle showed rivalries, alliances, caravans and new nations, but nothing marked as a world event. A player who has heard about events has no idea whether they are close or what triggers them. Expected: a tip or chronicle line foreshadowing events, e.g. once the player leads a nation.
- **Repro:** same as above; read the tips and the chronicle panel
- **Screenshots:** [final.png](s08/shots/final.png)

### 30. [minor] Objective 6 progress bar shows Wood 20/20 Stone 5/5 after the Farm spent the materials

- **Category:** bug; **reported by** 1 agent(s): s03
- **Detail:** Right after building a Farm (25 wood, 10 stone) inside my settlement, Objective 6 'Build a market stall' showed a full gold bar 'Wood 20/20 • Stone 5/5' while inventory was Wood 5, Stone 0. Expected 5/20 and 0/5. The objective text also runs under the Inventory panel.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-wt-playtest064 --seed 103 --season autumn; pt key space; pt click 480 315; pt click 480 315; gather 55 wood+5 stone; pt key n; walk off claimed land; pt key f; gather 25 wood+10 stone; pt key h; pt key e; pt key k; pt key k
- **Screenshots:** [farm3.png](s03/shots/farm3.png)

### 31. [minor] Objective hint and completion popups are truncated

- **Category:** text; **reported by** 1 agent(s): s02
- **Detail:** Objective 4 hint showed 'Gather 50 wood, then press' with the key cut off (it wrapped correctly a few seconds later). The top toast read 'Objective complete: Create or' and the popup read 'Create or join a nat'. Expected: the full text, wrapped.
- **Repro:** From a fresh world, gather 10 wood, then pt key n; screenshot right away
- **Screenshots:** [textcut.png](s02/shots/textcut.png)

### 32. [minor] Simulation ran at 3.5-10 tps (of 50) and 1-4 fps through the whole session

- **Category:** perf; **reported by** 1 agent(s): s06
- **Detail:** pt status/state showed tps 3.5-10.3 and fps 1-4 the whole time. The machine was shared with other sessions, but it made every timed mechanic (30 s laws, hunger) take 5-10x longer in real time.
- **Repro:** pt status at any point
- **Screenshots:** [chase.png](s06/shots/chase.png)

### 33. [minor] Status line says 'deal with them again in 20 seconds' and never counts down, contradicting the row

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** After a blocked act, the status line reads 'Your envoy to Southernport is still on the road: you can deal with them again in 20 seconds of play.' The text stays on the world HUD and on the Nations screen. 264 ticks later the row above it correctly said 'envoy back in 15 s', but the status line still said 20 seconds. Expected: the status line counts down, or is cleared once the countdown no longer matches.
- **Repro:** lead a nation; key o; Tribute nation A; Tribute A again (blocked); Escape; pt wait 40; key o; compare the row's 'envoy back in N s' with the status line
- **Screenshots:** [countdown2.png](s04/shots/countdown2.png)

### 34. [minor] Three tributes at the envoy pace land at +29 because of relation decay, one short of the +30 an alliance needs

- **Category:** balance; **reported by** 1 agent(s): s04
- **Detail:** The header says tribute gives +10 and an alliance needs +30. Gifts to Southernport at ticks 1508, 2644 and 3728 (as fast as the envoy allows) gave +10, +20, then +29, because relations decay about 1 point per envoy round trip. An alliance therefore needs a 4th gift (100 coins) and a 5th envoy window, over 60 s of play. I could not reach an alliance within the time budget. Either make three gifts enough, or have the header say that relations fade.
- **Repro:** key n; key o; Tribute nation A every time its envoy returns; read relations after the 3rd gift
- **Screenshots:** [after3.png](s04/shots/after3.png)

### 35. [minor] Translucent HUD makes enabled vs disabled buttons indistinguishable depending on the scene

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** In first-person view inside a settlement, the greyed Build Stall/House/Farm look like the enabled Exit Settlement and Withdraw Funds. In bright highlands, Gift/Invite buttons wash out to near-white. 'Send Caravan (Town era)', 'Sell Resources' and 'Purchase Food' are drawn enabled but only produce a red error (no Storehouse / no stalls).
- **Repro:** Enter settlement (E), press V; click Sell Resources
- **Screenshots:** [view_v.png](s09/shots/view_v.png)

### 36. [minor] Tutorial tips arrive out of step with objectives

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Tips 1-2 (walk/look) were never visibly on screen; the first tip I saw was 'Tip 3 of 5: Gather stone' while objective 1 was still Gather wood. 'Tip 5 of 5: Your goals' (explains the top-left goals panel) appeared only after I had founded my settlement, i.e. after four objectives were done. Expected tips to match the current objective and the goals tip to come first.
- **Repro:** seed 101; Start; Begin; pt walkto tree; e; screenshot
- **Screenshots:** [tip-mismatch.png](s01/shots/tip-mismatch.png)

### 37. [minor] Very low fps/tps on the shared machine (fps 0-2, tps 3-10)

- **Category:** perf; **reported by** 1 agent(s): s01
- **Detail:** Game time ran ~5-10x slower than real time; clicks/screens took several seconds to show (the first Start click looked ignored, '? Controls' took >3 s to open). Probably caused by parallel sessions, but worth watching.
- **Repro:** pt status during play
- **Screenshots:** [perf.png](s01/shots/perf.png)

### 38. [minor] War/Denounce ask for confirmation even when the envoy is away and the act will be refused

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** With the envoy to Southernport away (row: 'envoy back in 20 s'), clicking War gives 'Click War again to declare war on Southernport'. Only the confirming click says 'Your envoy to Southernport is still on the road'. Denounce behaves the same. The buttons for a nation whose envoy is away also look exactly as enabled as normal ones. Expected: the buttons are dimmed, or the envoy message shows on the first click.
- **Repro:** key o; Tribute Southernport; click War on Southernport twice quickly
- **Screenshots:** [war_confirm_then_envoy.png](s04/shots/war_confirm_then_envoy.png)

### 39. [minor] Winter countdown stays at 'Winter in 2 days' and day numbers disagree across HUD

- **Category:** text; **reported by** 1 agent(s): s03
- **Detail:** Season panel read 'Autumn - day 1/2, year 1 / Winter in 2 days' from timeOfDay 0.30 to 0.59 without changing. The chronicle labels the same time 'Day 5', state.json says day 4. For a goal about preparing for winter, the player cannot tell how long they have. Expected a countdown that decreases (e.g. '1.5 days', or hours) and one consistent day number.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-wt-playtest064 --seed 103 --season autumn; pt key space; pt click 480 315; pt click 480 315; watch Season panel for several game minutes
- **Screenshots:** [winter_counter.png](s03/shots/winter_counter.png)

### 40. [polish] Chronicle window: translucent, cryptic '3 entries. Listing 2.', overlapping filters

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** Panels behind the Y window bleed through its lower half. The header '3 entries. Listing 2.' doesn't say why one entry is hidden. There are three overlapping filters (Your news/All nations, 'Only my nation', Y cycles views), and the entry order is the reverse of the HUD.
- **Repro:** Press Y
- **Screenshots:** [chron_y.png](s09/shots/chron_y.png)

### 41. [polish] Controls help page 3 lists debug cheats (F2-F6) to all players

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** A third of the Controls screen documents spawn-coins/wood/pawns cheats. Otherwise the Controls screen is the most legible screen in the game.
- **Repro:** Main menu > Controls > Next > Next
- **Screenshots:** [controls3.png](s09/shots/controls3.png)

### 42. [polish] Expanded Era box pushes the Inventory Info panel under the tip card

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** Tapping the Era box pushes the Objective panel down and the Inventory Info panel moves right to x~220, where the Tip card covers part of it.
- **Repro:** with a Tip showing, pt click 110 58
- **Screenshots:** [era_overlap.png](s07/shots/era_overlap.png)

### 43. [polish] Inventory/pause/nations overlays have no backing panel; player capsule draws over text

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** The I, Esc and O screens draw text straight onto the dimmed 3D world. The player capsule sits on 'Apples/Saplings' in Inventory, on 'World seed 109' in Pause and between the Nations buttons. A tip number was also skipped (Tip 1 then Tip 3 of 5).
- **Repro:** Press I; press Escape
- **Screenshots:** [inventory.png](s09/shots/inventory.png)

### 44. [polish] Jargon: 'Export replay (USD)', 'Rival', 'Chunk Size'; volume label says sound only in browser

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** 'USD' reads as US dollars. 'border quarrels stop at Rival' never defines Rival. The Config volume label reads '(sound plays in the browser version)' but desktop still shows Volume/Mute.
- **Repro:** Pause > Share; Main menu > Config
- **Screenshots:** [share.png](s09/shots/share.png)

### 45. [polish] Nations screen: diplomacy buttons for a nation whose envoy is away are not dimmed

- **Category:** ux; **reported by** 1 agent(s): s10
- **Detail:** After paying tribute, the row reads 'envoy back in 20 s' but Tribute/Ally/War/Denounce look exactly like the buttons of a nation you can deal with. Clicking one correctly refuses ('Your envoy to Westernham is still on the road: you can deal with them again in 20 seconds of play.') and charges nothing (coins stay 231), so this is only a visual hint gap. Expected the row's buttons greyed out while the envoy travels.
- **Repro:** pt start --seed Ironvale; Start, Begin; pt key n; wait until another nation is founded (~tick 950); pt key o; pt click 123 255 (Tribute); pt click 123 255 again
- **Screenshots:** [envoy_again2.png](s10/shots/envoy_again2.png)

### 46. [polish] New World seed box keeps the previous seed and Ctrl+A does not select it

- **Category:** ux; **reported by** 1 agent(s): s10
- **Detail:** Going Main Menu > Start after playing, the seed box still holds 'Ironvale'. Clicking it and pressing Ctrl+A then BackSpace removed only the last character ('Ironval'), so typing the shared seed produced 'Ironval1450162717' (a different world, though the label under the box shows the resolved seed so it is noticeable). Had to press BackSpace 20 times. Expected Ctrl+A to select all, or a clear button, since pasting a shared seed is the main use of this box. (Low frame rate may contribute; tried once.)
- **Repro:** From a world: Escape > Main Menu > Start; pt click 480 235; pt key ctrl+a BackSpace; pt type 1450162717
- **Screenshots:** [newworld_seed.png](s10/shots/newworld_seed.png)

### 47. [polish] Pawn name label is cut off at the right screen edge

- **Category:** text; **reported by** 1 agent(s): s06
- **Detail:** With Frank Brown right beside the player, his floating label ran off the right edge: only 'Frank Br / Ambitio / (looking' showed. Expected the label to be clamped on screen.
- **Repro:** walk up beside a pawn (pt walkto pawn) and screenshot
- **Screenshots:** [invite2b.png](s06/shots/invite2b.png)

### 48. [polish] Probe state.json objective.progress lags the on-screen value

- **Category:** bug; **reported by** 1 agent(s): s01
- **Detail:** state.json said 'Wood 0/10' while the HUD showed 5/10, and 'Wood 3/20 - Stone 0/5' after I already had 5 stone. Not player-facing, but misleads tooling.
- **Repro:** chop one tree; compare pt state with screenshot
- **Screenshots:** [tip-mismatch.png](s01/shots/tip-mismatch.png)

### 49. [polish] WORKS: residents leave my market to me and 'Build a market stall' completes only from my own stall

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** With 3/8 residents in Southtown, Market Info stayed at 0/4 stalls and objective 6 stayed open until I pressed B (tick 6896 'Stall built.', objective completed tick 6900). Oscar Smith's stall built in rival Blackfield at tick 6800 did not complete it. After my build a resident (Nancy Walker) only staffs the stall as merchant. Logged as a positive confirmation.
- **Repro:** as above; watch Market Info and objective 6 before/after B
- **Screenshots:** [before_stall.png](s05/shots/before_stall.png)

## Objective timings

Game seconds from the start of the world to each objective (event tick / the game's ticks per second, from events.jsonl). Game time, not wall time: with many sessions on one machine a world can run slower than real time, and wall-clock timings would mostly measure the machine.

| # | Objective | s01 | s02 | s03 | s04 | s05 | s06 | s07 | s08 | s09 | s10 | min | median | max |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Gather wood | 19 | 17 | 2416 | - | 23 | - | 90 | 31 | 22 | - | 17 | 23 | 2416 |
| 2 | Eat something | 20.1 | 18 | 2417 | - | 24 | - | 91 | 32 | 23 | - | 18 | 24 | 2417 |
| 3 | Create or join a nation | 28.1 | 21 | 2427 | - | 28 | - | - | 33 | 29 | - | 21 | 28.6 | 2427 |
| 4 | Found a settlement | 84.1 | 89 | 2513 | - | 106 | - | - | 93 | 87 | - | 84.1 | 91 | 2513 |
| 5 | Enter your settlement | 86.1 | 92 | 2516 | - | 108 | - | - | 95 | 91 | - | 86.1 | 93.5 | 2516 |
| 6 | Build a market stall | 126.1 | 95 | 2557 | - | 138 | - | - | 151 | - | - | 95 | 138 | 2557 |

