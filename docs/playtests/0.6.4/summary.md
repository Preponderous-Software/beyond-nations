# Playtest summary: 064-20261029-0750

10 sessions, build beyond-nations 0.6.3 (4645cdf), generated 2026-10-29 08:55.

**Findings:** 74 reports in 59 groups -- 0 blocker, 11 major, 41 minor, 22 polish.

## Outcomes

| Session | Goal | Outcome | Fun | Minutes | Objectives | Deaths | Findings | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| s01 | Play the first 10 minutes as a brand-new player who has never seen the game; note every moment of confusion. | partial | 3 | 14.5 | 6 | 0 | 11 | The opening loop (tips, top-left objective, chop trees, then N, F and E) is clear, and its feedback is good: floating +wood text, rewards, a helpful 'too close' message. But two of the first six objectives complete without the player doing them ('Eat something' and 'Build a market stall', the stall built by an NPC), so the tutorial teaches less than it seems to. Founding a settlement is the big stall point: the crowded world plus compass directions with no compass on screen cause several minutes of wandering. Several HUD text overflows and overlaps make the screen look unfinished at 960x540. |
| s02 | Reach the Village era as fast as you can (it cannot come before day 3, about 17 minutes of game time); record the game time of every objective. | partial | 3 | 25.7 | 6 | 0 | 7 | The early loop moves fast and is well signposted. Six objectives were done in 143 game seconds, and the 'too close to X, open ground lies N to the south' hints made placing a settlement easy. Once the settlement exists, NPCs build stalls and buildings for you, and the only things left are a fixed day-3 timer and recruiting members, so the Village era turns into waiting. The two different meanings of 'Village' on screen make that wait confusing. On this loaded machine (about 12 tps) the day-3 gate could not be reached in the session at all. |
| s03 | Survive the first winter. Start with `--season autumn` (a fresh world otherwise reaches winter only after about 57 minutes of game time). Note how food and cold feel. | blocked | 3 | 24.6 | 6 | 0 | 8 | Early autumn plays smoothly: wood, a nation, a settlement and a farm in about 10,000 ticks, with useful refusal messages and a lively chronicle of rival nations. But --season autumn leaves winter two game days away, so neither winter nor the cold could be tested in a 35-minute session. Food creates no pressure in autumn: apples pile up from chopping and energy never fell below 84. Low fps also makes the two-click confirms and the reflowing action bar easy to misclick. |
| s04 | Diplomacy and war: lead a nation, meet a neighbour, try alliance, rivalry and war from the Nations screen (O). | achieved | 3 | 15.1 | 0 | 0 | 7 | The Nations screen is clear and responsive: every action gives a precise status line with the relation number and threshold, Peace and Confirm buttons appear only when they apply, and the chronicle records each step. But because the screen pauses the world, the whole cycle (alliance for 75 coins, rivalry, war, instant peace, war again through a truce) costs no game time and almost nothing else, so diplomacy feels like flipping switches rather than statecraft. The short real-time window for click-again confirmations is also hard to hit at low frame rates. |
| s05 | Trade and caravans: build a market stall, buy and sell, get a Storehouse and send a caravan. | partial | 2 | 24.8 | 6 | 0 | 7 | The early loop (chop, nation, found) flows well, and the error messages are good: they give spacing hints with distance and direction, and say why a merchant can't pay. But the trade goal falls apart. NPCs fill the player's own market before the player can build a stall, and the stall objective completes from their stall. Selling is starved of coins. The Storehouse and caravans sit behind the Town era, at least 3 game days away. The build-confirm window also times out at low tps (the machine was at load ~54, 2 fps / 7-15 tps; the first pt start timed out). |
| s06 | Ruler, laws and heirs: lead a nation, enact and repeal laws, and see what happens when the ruler dies. | partial | 2 | 25.1 | 0 | 0 | 6 | Founding a nation and passing or repealing laws works, and the chronicle reports each change clearly ('Taxes in Redfield rose to High', 'Redfield stood down its conscripts'). But laws do nothing you can see in a one-member nation. Pawns are hard to recruit, and the AI nations sign them all up within minutes. Succession has no presence in the UI at all, so the ruler-and-heirs part of this goal could not be reached or even planned for in normal play. |
| s07 | Crafting: find and use every recipe on the crafting screen (C); are tools worth making? | partial | 3 | 15.3 | 4 | 0 | 6 | Tools are worth making and the screen explains them well. An axe (4 wood, 2 stone) pays for itself after two trees and gives about +60 wood over 20 uses; the pickaxe gives +30 stone over 15 uses. The live 'Ready / Need N more / have 1 (19 uses left)' lines and the context 'Craft Pickaxe' HUD button are good. Cooked Meat is the weak recipe: you can't choose to eat it, and auto-eat spends it first at 89 energy, so most of its 45 energy is thrown away. Planks and Bricks can't be reached in a first session (Village era: 10 members, 2 stalls, day 3), so only 3 of the 5 recipes could be used. |
| s08 | World events: play until world events are offered, take different choices, and judge whether the consequences are clear and fair. | partial | 3 | 55 | 6 | 0 | 6 | The world-event sheet works well. The Drought sheet names the cause, gives each option's cost and effect, pauses the world, and the result line is clear. A save reload replays the same event, so all three choices could be compared. The weak spots are around it. Ration's 'restless' penalty is hidden. Irrigation silently draws on stores. Nation world events are lost in raid spam in a chronicle labelled 'your news'. The first player event comes about 10 game minutes in, so only one event (Drought) was seen in this session, under heavy machine load (about 3-15 tps). |
| s09 | Accessibility and text audit, touching only what a mouse-and-keyboard player would: every screen and panel; cut-off, overlapping, unclear or misspelled text; contrast; anything only explained on hover. | achieved | 3 | 14.3 | 3 | 0 | 10 | Text is almost always plain-language and the in-world dark panels, crafting cards, Controls pages and error banners read well. The main accessibility problems are low contrast on the light-blue menu screens, HUD panels that move and overlap (Inventory Info jumps mid-screen and gets covered by tips), a translucent Chronicle window, and a recent-chronicle panel that truncates every line. Nothing is explained on hover anywhere, so disabled buttons and Config jargon go unexplained. |
| s10 | Save/load and share-seed round trip: save from the pause menu, stop, `pt start --continue`, confirm the world is identical; then start a new world from the seed shown on the Share screen and confirm it matches. | achieved | 3 | 17 | 0 | 0 | 5 | Save/continue is solid: the pause-menu Save gives clear feedback ('Saved: Day 1 - no nation, 08:02'), --continue restored tick, position, heading, energy, inventory, objective and chronicle exactly, with a friendly 'Welcome back' toast, and Main Menu > Load restored the right slot. The Share screen's seed reproduces the starting map exactly, but the world's history (which nations form, their names and timing) diverges, so a shared seed is a shared map rather than a shared story; save slots also need more distinguishing detail than 'Day 1 - no nation'. |

## Findings

### 1. [major] Objective 6 'Build a market stall' completes by itself when a resident builds a stall

- **Category:** bug, ux; **reported by** 4 agent(s): s01, s02, s03, s05
- **Also reported as:** "Objective 6 'Build a market stall' completes when an NPC builds a stall, not the player"; "Objective 6 'Build a market stall' completes when an NPC builds a stall, without the player doing anything"; "Objectives complete without the player acting: 'Build a market stall' is credited when an NPC builds one"
- **Detail:** Objective needs Wood 20 / Stone 5. It completed at tick 6250 while I had Wood 2 / Stone 0 and had just exited the settlement; nothing was spent. The settlement's market later lists 'Merchants: Lily Williams' (1/4 stalls), so an NPC resident built it. No message said so, and I went on gathering wood and stone for a stall I never needed. Expected the objective to need the player's own B/Build Stall, or at least a message crediting the resident.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-pt-064 --seed 101; reach objective 6 (found a settlement ~ (475,-446), press E to enter, then click Exit Settlement); wait ~200 ticks; pt events shows objective_completed build-stall with Wood still 2
- **Screenshots:** [obj7.png](s01/shots/obj7.png) [market-full.png](s05/shots/market-full.png) [npc_stall_objective.png](s02/shots/npc_stall_objective.png) [founded.png](s03/shots/founded.png)

### 2. [major] Build-confirm window is ~1 game second, so a normal second click times out at low tps

- **Category:** bug, ux; **reported by** 2 agent(s): s03, s05
- **Also reported as:** "Two-click confirm on Build Farm times out in ~70 ticks, too short at low fps"
- **Detail:** Build Farm asks you to 'Click again to confirm'. Three separate clicks on 'Confirm Build Farm' failed with 'Not confirmed in time, so nothing was done.' and the button re-armed. Only two clicks fired 1.7 s apart in real time worked. The window looks like ~50 ticks, which is several real seconds at 9 tps but too short with 2 fps hover-then-click. It should be measured in real time (or be longer).
- **Repro:** Inside own settlement with 25 wood/10 stone: pt click 473 445; pt wait 2; pt click 473 445 -> re-arms instead of building
- **Screenshots:** [farm-confirm.png](s05/shots/farm-confirm.png) [confirm_timeout.png](s03/shots/confirm_timeout.png)

### 3. [major] Game ran at 0.2-16 tps (normally 50) on the shared machine, so the day-3 Village gate could not be reached in a 35-minute session

- **Category:** perf; **reported by** 2 agent(s): s01, s02
- **Also reported as:** "Game ran at 2-4 fps / 5-18 tps on the shared machine; first start timed out"
- **Detail:** pt status/state showed tps between 0.2 and 16, mostly 11-13, with fps 0-3 (load average about 55). Game time ran at roughly a quarter of real time. After 25 minutes of wall-clock time the game was at tick 10270, still day 1 (timeOfDay 0.64). Every other Village requirement was met or nearly met by tick 8800. At this rate day 3 (about tick 60000) would take over an hour of real time. walkto also timed out several times because it steers at 2-3 fps.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-pt-064 --seed 102 with several sessions in parallel; play for 25 minutes; pt state shows tps and tick
- **Screenshots:** [day_gate.png](s02/shots/day_gate.png) [world0.png](s01/shots/world0.png)

### 4. [major] --season autumn starts at autumn day 1/2, so winter is still ~52k ticks (over an hour of wall time) away

- **Category:** balance; **reported by** 1 agent(s): s03
- **Detail:** With --season autumn the world opens at tick 120044 (day 4), and the Season panel says 'Autumn - day 1/2, Winter in 2 days'. Winter begins at tick 180000. At the 9-17 tps this machine managed, I reached tick ~130700 by the end of a 25-minute session, so winter was never reached and the cold could not be tested. Expected: a start that puts winter within a playtest's reach, such as the last day of autumn or a --season winter-eve option.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-pt-064 --seed 103 --season autumn; pt key space; pt click 480 315; pt click 480 315; pt state (tick 120044, season panel 'Winter in 2 days')
- **Screenshots:** [winter_unreachable.png](s03/shots/winter_unreachable.png)

### 5. [major] Cooked Meat is wasted by auto-eat: eaten first at <90 energy, capped at 100

- **Category:** balance; **reported by** 1 agent(s): s07
- **Detail:** Crafted 1 Cooked Meat (screen says '45 energy (raw: 25)'). There is no eat key or button; the player auto-eats when energy drops below 90 and always picks the most nourishing food first. The cooked meat disappeared around tick 3250-3303 (only visible cue: 'Objective complete: Eat something'), and energy went from ~90 to 99.2. So ~35 of its 45 energy was lost while the bag held 12+ apples (15 each) that would have fit exactly. Cooking a chicken (costing a wood) currently buys almost nothing. Expected: eat the smallest food that fills the gap, or let the player choose when to eat.
- **Repro:** pt start --seed 107; key space; click Start/Begin; walkto tree, e; walkto chicken, e; key c; click Craft on Cooked Meat (822,265); keep gathering until energy < 90; watch the cooked_meat count drop and energy cap at 100
- **Screenshots:** [meat_wasted.png](s07/shots/meat_wasted.png)

### 6. [major] Diplomacy happens with no game time passing: Nations screen pauses the world, so ally/denounce/war/peace all resolve on one tick

- **Category:** balance; **reported by** 1 agent(s): s04
- **Detail:** With the Nations screen open the tick stayed at 1387 (tps 0). In that frozen moment I went Neutral 0 -> three Tributes (+30, 75 of 199 coins) -> Ally accepted -> Denounce x2 (Rival -20) -> War (Hostile -60) -> Peace accepted -> War again. No cooldowns, no diminishing returns on gifts. An alliance with the first neighbour cost 75 coins and ~20 s real time. Expected gifts or relation changes to need some game time or have a cooldown.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-pt-064 --seed 104; title/Start/Begin; pt key n; pt wait 60 (Westshire is founded ~tick 851); pt key o; click Tribute (123,218) x3; click Ally (301,218)
- **Screenshots:** [ally_cheap.png](s04/shots/ally_cheap.png)

### 7. [major] NPCs fill all 4 market stalls in a new settlement before the player can build one; B says 'Market is full'

- **Category:** balance; **reported by** 1 agent(s): s05
- **Detail:** I founded a settlement at tick 5750 with 4 wood left (founding takes 50 of the 54 I had), then left to chop the 20 wood a stall needs. By tick 6802 NPCs (Dave Robinson, Mike Roberts, Lily Wood) had built all 4 stalls in MY settlement ('The market is now full'). Back inside with 22 wood/5 stone, B gave 'Market is full. Cannot build stall.' The player never gets to build a stall in their own settlement. Expected: a stall slot kept for the leader, or NPCs waiting until the player has had a chance.
- **Repro:** pt start --seed 105; gather ~54 wood; N; walk to open ground; F; E (enter); Exit Settlement; chop 20 wood (~1300 ticks); walkto settlement; e; b
- **Screenshots:** [market-full.png](s05/shots/market-full.png)

### 8. [major] Player's nation cannot realistically gain members: pawns flee after one talk and all join AI nations within minutes

- **Category:** balance; **reported by** 1 agent(s): s06
- **Detail:** Irene Taylor: 'I don't even know you.' (needs Acquaintance). After one E-talk pawns walk off, so the next E 1 s later says 'No entities within range'. Gifting (G, 10 coins each) to Wendy Wilson reached Acquaintance after 3 gifts, but by then she and Hank Davies were sworn to Smalltown ('I'm loyal to Smalltown.' (needs Loyal)). By day 1 morning 9 AI nations existed (Smalltown, Newwood, Towntown, Upperburg, Littleburg, Whiteland, Northernland, Townfield, Easternham), leaving Redfield at 1 member for the whole session. With 1 member, taxes collect 0 coins and conscription ('one conscript per 2 members') yields nothing, so the laws had no observable effect and no heir could exist.
- **Repro:** seed 106; key n; walkto pawn; key e; key r -> needs Acquaintance; walkto pawn (Smalltown villager); key g x3; key r -> needs Loyal
- **Screenshots:** [talk-flee.png](s06/shots/talk-flee.png)

### 9. [major] Ruler death / succession cannot be reached or even learned about in normal play within a session

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** The goal's third part (what happens when the ruler dies) was not reachable. Nothing on screen (Nations tab 'You lead it', Laws tab, Player Info 'Role: LEADER', all 3 pages of ? Controls) mentions an heir, succession, or ruler death. The only way to die seems to be energy reaching 0: energy drained ~85 -> 57.6 over 8210 ticks (~164 game s), i.e. ~6 more game minutes, which at the observed 7-16 tps is 25+ real minutes. And with no other nation members there would be no heir anyway. Expected: the Nations tab to name the current heir (or say 'no heir: Redfield ends with you') so a player can plan a dynasty.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-pt-064 --seed 106; space; click Start; click Begin; key n; key o; read both tabs; open ? Controls pages 1-3
- **Screenshots:** [no-heir.png](s06/shots/no-heir.png)

### 10. [major] War/Denounce click-again confirmation expires in a few seconds of real time; at 2-5 fps the second click often misses it

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** Clicking War relabels the button 'Confirm' and arms a confirmation, which expires with 'Not confirmed in time, so nothing was done.' after roughly 4-9 s of real time (it varied, suggesting it may be frame-dependent). With hover-before-click at a few fps, a normal second click arrived after expiry and just re-armed it. It took 5 attempts (~2.5 min) until two back-to-back clicks worked. Expected the confirmation to last longer, or to be a modal, on slow machines.
- **Repro:** At Rival with Westshire, Nations screen: pt click 657 218; pt wait 3; pt click 657 218 -> events show 'Not confirmed in time' then 'Click War again...' repeatedly
- **Screenshots:** [war_declared.png](s04/shots/war_declared.png)

### 11. [minor] Objective panel text clipped: progress bar 'nbers 4/10', hint cut off, toast 'Enter your settlemen'

- **Category:** text; **reported by** 5 agent(s): s01, s02, s03, s05, s08
- **Also reported as:** "HUD text clipped in settlement: objective hint, progress label, toast; world label overlaps HUD"; "Objective 7 progress text clipped: 'bers 6/10 • Buildings 0/2'"; "Objective 7's progress bar text is clipped on the left and does not match the era checklist"; "Objective progress bar text clipped on the left"
- **Detail:** Objective 7 progress bar 'Members 4/10 · Buildings 0/2' overflows and is clipped on the left. Objective 6 hint 'Gather 20 wood and 5 stone' is cut off by the panel bottom inside the settlement. The completion toast reads 'Enter your settlemen'. Objective 7 hint says 'Village 0/1' while the era box says 'Village: 0/4', which is confusing.
- **Repro:** pt start ... --seed 101; play to objective 6 and 7 (found settlement, enter it)
- **Screenshots:** [obj7.png](s01/shots/obj7.png) [npc_stall_objective.png](s02/shots/npc_stall_objective.png) [objective_clip.png](s03/shots/objective_clip.png) [settlement-panel.png](s05/shots/settlement-panel.png) [drought_ration.png](s08/shots/drought_ration.png)

### 12. [minor] Near pawns, floating world labels cover the Chronicle and get clipped, and the Inventory panel jumps to screen centre

- **Category:** text, ux; **reported by** 3 agent(s): s03, s04, s09
- **Also reported as:** "Chronicle panel cuts entries off with '...'"; "Chronicle recent panel truncates every entry with '...'"; "Inventory Info panel jumps to mid-screen after founding a nation and is then covered by the tip panel"
- **Detail:** Standing next to Nancy Walker in Blackland, her label 'Gregarious, Ambitious (gathering resources)' was drawn in very large text over the Chronicle panel, another label was cut off at the left edge ('ces)'), and the Inventory Info panel moved to the middle of the screen, on top of the tip and the action bar. The settlement's '0 here / 3/8 residents' label also sits behind the Chronicle panel.
- **Repro:** pt walkto pawn; pt shot
- **Screenshots:** [overlap_labels.png](s03/shots/overlap_labels.png) [rock.png](s09/shots/rock.png) [bkey.png](s09/shots/bkey.png) [chronicle_trunc.png](s04/shots/chronicle_trunc.png)

### 13. [minor] Objective 2 'Eat something' completes in 48 ticks without the player eating; eating is never taught

- **Category:** bug, ux; **reported by** 2 agent(s): s01, s09
- **Also reported as:** "Objective 2 'Eat something' completes on its own without the player eating"
- **Detail:** Right after Gather wood completed (tick 1002), 'Eat something' completed at tick 1050 with apples unchanged at 5. Its hint was never on screen long enough to read. Eating is automatic and silent throughout (energy 83->98 on first chop; Chicken Meat 1->0 and energy 90->99 on entering the settlement) with no 'you ate' message. Expected the objective to wait for a real eat, and some feedback when food is eaten.
- **Repro:** pt start ... --seed 101; Begin; walk to nearest tree and press E twice (two trees); pt events 10
- **Screenshots:** [obj2.png](s01/shots/obj2.png) [obj3.png](s09/shots/obj3.png)

### 14. [minor] 'Open ground lies about 270 to the north' is not usable: no compass or minimap shows north

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** The F 'too close' message is helpful and gives a distance and compass direction, but nothing on screen shows which way north/east/south-east is. I made three attempts (ticks 4450, 4942, 5362) walking in guessed directions and hitting new 'too close' settlements (Newburg, Fortressford twice) before founding at tick 5794. The world is crowded with AI settlements by then (250 spacing), so this is where a new player wanders the longest: Found a settlement took 4552 ticks, more than all other objectives combined.
- **Repro:** pt start ... --seed 101; gather 50 wood near spawn; press F at about (28,107)
- **Screenshots:** [found1.png](s01/shots/found1.png)

### 15. [minor] 'Open ground lies about N to the X' hint changes direction as you follow it

- **Category:** ux; **reported by** 1 agent(s): s08
- **Detail:** When founding was refused, the hint said '220 to the north' at (131,-84). After walking, it said '250 to the south-east'. At (34,140) it said '180 to the east', then after walking about 112 that way it said '210 to the west'. Each hint is the nearest site from where you stand, so the target jumps around and the distance grows. It took 6 F presses and about 2700 ticks to find a legal site. The last two hints (60 W, then 20 W) were accurate. Expected the hint to stay on one target, or to name a landmark.
- **Repro:** seed 108; create nation near (131,-180) with 57 wood; press F, follow hints
- **Screenshots:** [settled.png](s08/shots/settled.png)

### 16. [minor] Breaking a fresh truce with War has no visible cost

- **Category:** balance; **reported by** 1 agent(s): s04
- **Detail:** Straight after peace the row reads 'Neutral (-19), truce 60 s' and War is still offered. The confirm says 'Click War again to break the truce', which is good, but breaking it simply sets relations back to Hostile -60 with no extra penalty, reputation hit or chronicle mention of the broken truce.
- **Repro:** After the peace above, click War twice quickly
- **Screenshots:** [truce_broken.png](s04/shots/truce_broken.png)

### 17. [minor] Chronicle 'Your news' view is flooded with other nations' raids, so nation world events are buried

- **Category:** ux; **reported by** 1 agent(s): s08
- **Detail:** Pressing Y shows 'Showing: > Your news' with 259 entries, but the list is mostly other nations' raids and rivalries (e.g. 'Raiders from Westernport struck Westernland'). The HUD chronicle strip labelled 'your news' also lists other nations' stalls. The only nation world event before tick 24000 (tick 19751, 'A bountiful harvest came in across Castleburg...') sat among about 50 raids and 100 relation changes, so a player would never notice that world events are happening to other nations.
- **Repro:** seed 108, play to tick ~24000, press Y
- **Screenshots:** [chronicle_yournews.png](s08/shots/chronicle_yournews.png)

### 18. [minor] Chronicle full-history window is translucent; HUD text behind it overlaps its controls

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** With Y, the Chronicle window lets the Objective-complete popup, Era panel, Inventory and Season panels show through; 'Close  Y cycles views' collides with 'Objective complete!'. Expected an opaque window or dimmed HUD.
- **Repro:** after founding a nation: pt key y
- **Screenshots:** [chron_full.png](s09/shots/chron_full.png)

### 19. [minor] Denouncing an ally ended the alliance without a visible confirmation

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** Allied +30: my first Denounce click showed nothing (no 'click again' prompt was logged or displayed), the second ended the alliance (Neutral +5). Denouncing at Neutral did show 'Click Denounce again'. Either the first click was dropped at low fps or the confirmation was not shown. The button also does not say that it ends the alliance.
- **Repro:** Allied with Westshire: click Denounce (836,218), take a screenshot, click again
- **Screenshots:** [denounce_no_confirm.png](s04/shots/denounce_no_confirm.png)

### 20. [minor] Disabled action buttons and HUD abbreviations give no reason and have no hover tooltip

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Greyed 'Enter Settlement', 'Plant Sapling', 'Found Settlement' are low-contrast grey-on-teal and hovering 2s shows nothing (Found Settlement needs 50 wood, stated only in the Objective box). 'Village: 0/4' with a yellow swatch only makes sense after clicking the Era box. Config 'Chunk Size', 'Location Scale', 'Lag Prevention', 'Respawn Pawns' have no explanation or tooltip.
- **Repro:** in world: pt move 400 517; pt wait 2; pt shot
- **Screenshots:** [hover_disabled.png](s09/shots/hover_disabled.png)

### 21. [minor] Drought 'Ration and wait for rain' restlessness penalty is never reported

- **Category:** ux; **reported by** 1 agent(s): s08
- **Detail:** Option 3 says 'your people grow restless with you'. The result toast and chronicle only say 'Drought struck Hamletton, and you rationed the stores and waited for rain.' There is no number and no visible change; Player Info still reads 'Relationships: 0'. The code lowers every member's relationship with the player, so the free option has a hidden cost the player cannot measure. Expected the result line to state the cost (e.g. 'your 7 members think less of you (-N)') or a visible mood or relationship change.
- **Repro:** pt start --seed 108; Start, Begin; create nation (N), found settlement, build a farm (K K); play to tick ~30552 (or pt start --continue from the tick-30003 autosave); click option 3 on the Drought sheet
- **Screenshots:** [drought_ration.png](s08/shots/drought_ration.png)

### 22. [minor] Drought irrigation option takes missing stone from settlement stores without saying so

- **Category:** ux; **reported by** 1 agent(s): s08
- **Detail:** With 5 stone on hand, 'Dig irrigation ditches (15 wood, 10 stone)' was not greyed out. Clicking it went through: wood 44->29 and my stone 5->0. The other 5 stone came from somewhere unstated (probably the settlement store). Paying from stores is fair, but the option should say where the cost comes from, e.g. 'you 5 + stores 5'. Otherwise the player cannot tell whether an option is affordable.
- **Repro:** same world as above; carry 44 wood, 5 stone; click option 1 on the Drought sheet at tick 30551
- **Screenshots:** [drought_after1.png](s08/shots/drought_after1.png)

### 23. [minor] E beside a tree harvested a nearby chicken instead of the tree

- **Category:** bug; **reported by** 1 agent(s): s01
- **Detail:** With walkto tree reporting tree bbd44c1d at 3.6 m, E gave 'Harvested chicken meat.' (tick 2802) and the tree stayed; a second E chopped it. The help says E prefers tree over chicken.
- **Repro:** pt start ... --seed 101; repeated pt walkto tree; pt key e around tick 2800 near (x~0,z~110)
- **Screenshots:** [wood50.png](s01/shots/wood50.png)

### 24. [minor] E next to a rock chops an overlapping tree; no indicator of the E target

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** walkto rock (2.5m) then E, as tip 3 says, gave 'Gathered 4 wood and 3 apples from tree'. No highlight/prompt shows which object E will act on.
- **Repro:** after objective 3: walkto rock; e
- **Screenshots:** [rock.png](s09/shots/rock.png)

### 25. [minor] E next to the settlement centre talks to a pawn instead of entering; the follow-up K says 'You are not inside a settlement'

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** Standing about 16 m from my settlement centre, with a member pawn close by, E opened Linda Wilson's dialogue ('Yes, leader? I hardly know you...') instead of entering. Linda is also labelled 'Stranger' although she is already a member of my nation. The Enter Settlement button worked.
- **Repro:** seed 102; walk back to your own settlement while your residents stand around it; press e then k
- **Screenshots:** [recruit.png](s02/shots/recruit.png)

### 26. [minor] Food creates no pressure in autumn: apples pile up from chopping and energy stays above 90

- **Category:** balance; **reported by** 1 agent(s): s03
- **Detail:** Every tree chopped also dropped 1-3 apples. I had 29 apples by tick 130680 without looking for food. Energy stayed between 92 and 99 the whole session, and early on it rose from 84 to 98 without me pressing anything to eat. The 'Eat something' objective also completed by itself (tick 120852). The settlement showed 'Feed Residents (0 hungry)'. Apart from the orange 'Winter in 2 days' line, nothing nudged me to stock food before winter. Expected: some sign that food matters before winter, such as a winter food-need estimate in Settlement Info.
- **Repro:** start as above; pt walkto tree; pt key e e e e e (repeat ~10 trees); pt state: apple count climbs, energy ~95
- **Screenshots:** [food_easy.png](s03/shots/food_easy.png)

### 27. [minor] Founding a settlement: the 'open ground' hint keeps moving as AI nations claim land

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** After N, pressing F gave 'Open ground lies about 120 to the north'. After walking there: 'Too close to the Camp of Eastville... 310 to the north-west', then 'Northernford... 121 to the west', then 'claimed by Northernford... 211 to the north'. It took four tries and ~2400 ticks (objective 4) to find a legal spot, because AI nations founded camps at the same time. The hints are helpful but they chase a moving target.
- **Repro:** pt start --seed 107; go to around (-27,315) with 50 wood; key n; key f; follow each hint and press f again
- **Screenshots:** [era.png](s07/shots/era.png)

### 28. [minor] Game time ran at 7-16 tps (14-33% speed) under the parallel playtest load

- **Category:** perf; **reported by** 1 agent(s): s06
- **Detail:** fps 2-4 and tps 7-16 throughout, making pawn chasing unreliable (pt walkto repeatedly timed out or reported 'stuck') and any starvation-based death test impossible in a 30-minute session.
- **Repro:** pt status / pt state during play
- **Screenshots:** [stuck.png](s06/shots/stuck.png)

### 29. [minor] Law countdown reads '0 s' while the change is still pending for up to ~10 s more

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** Conscription: Off proclaimed at tick 4574 ('takes effect in 30 seconds'). At tick ~6200 the Laws tab showed 'Conscription: On (Off in 0 s)'; it actually applied at tick 6502 (38.6 game s). The three changes landed on ticks 2002, 4002, 6502, so laws appear to be applied on a coarse periodic check. Expected the countdown to match the real moment, or the text to say 'at the next council'.
- **Repro:** key o; Laws; click Off under Conscription (97,262); reopen Laws ~32 game s later
- **Screenshots:** [law-zero.png](s06/shots/law-zero.png)

### 30. [minor] Laws tab gives no preview of an option's effect before enacting it

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** Each law shows only the description of the law currently in force ('No tax: members keep their coin'). The real effect of High taxes (3 coins per member per 10 s, loyalty falls fast, no immigrants) and Conscription On (members gather 40% less) only appears after it has taken effect 30+ s later. Expected a tooltip or description under each button.
- **Repro:** seed 106; key n; key o; click Laws (543,45); compare text before and after clicking High (353,175)
- **Screenshots:** [laws-preview.png](s06/shots/laws-preview.png)

### 31. [minor] Load screen slots are indistinguishable: both labelled 'Day 1 - no nation'

- **Category:** ux; **reported by** 1 agent(s): s10
- **Detail:** After saving two different worlds (one where I'd moved and gathered 5 stone, one fresh), the Load screen showed two rows 'Day 1 - no nation 2026-10-29 08:09' and '... 08:05' only. No seed, region, inventory or nation names. Picking the right one relies on remembering the time. Loading the 08:05 slot did correctly restore the first world (tick 2014, pos -77.46,-72.68, stone 5).
- **Repro:** Save two worlds; Main Menu > Load
- **Screenshots:** [load-slots-identical.png](s10/shots/load-slots-identical.png)

### 32. [minor] N creates a nation instantly with an auto name and no explanation

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Pressing N made me LEADER of 'Southburg' with no naming prompt, no confirmation and no explanation of nations or what LEADER implies. The chronicle line 'You founded the nation of Southburg' appears just as the next objective is 'Found a settlement', which mixes up the two ideas.
- **Repro:** pt start ... --seed 101; complete objective 2; pt key n
- **Screenshots:** [nation2.png](s01/shots/nation2.png)

### 33. [minor] No way to eat food, and no message when food is eaten automatically

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** Neither the three Controls pages, the Inventory (I) screen nor the HUD offer an eat action. The crafting screen sells Cooked Meat as '45 energy', but the player never sees it eaten: no status line or floating text, it just disappears from the inventory. Expected: a 'You ate cooked meat (+N energy)' status line, and ideally a line on the Controls page saying food is eaten automatically below 90 energy.
- **Repro:** craft Cooked Meat as above; press I; open ? Controls pages 1-3
- **Screenshots:** [inv_no_eat.png](s07/shots/inv_no_eat.png)

### 34. [minor] Peace accepted with reason 'the fighting has cost them enough' on the same tick war was declared

- **Category:** text; **reported by** 1 agent(s): s04
- **Detail:** Peace was offered and accepted at tick 1387, the tick war was declared, with no fighting at all. Relations went Hostile -60 -> Neutral -19 and a 60 s truce began. The reason given is wrong, and declaring war then making peace costs nothing (net +1 relations versus Rival -20).
- **Repro:** Declare war on Westshire (two quick clicks on War), then click Peace (480,218)
- **Screenshots:** [peace_instant.png](s04/shots/peace_instant.png)

### 35. [minor] Planks and Bricks (2 of 5 recipes) cannot be reached in a normal first session

- **Category:** balance; **reported by** 1 agent(s): s07
- **Detail:** Both need a Workshop, which unlocks in the Village era: 1 village, 10 members, 2 stalls, day 3. I created a nation and founded my own settlement at tick 7303, and both were still locked. At the observed 0-17 ticks a second, day 3 is hours of real time away. The lock reason is shown clearly in orange, which is good, but a crafting-focused player can't try 40% of the screen early on.
- **Repro:** pt start --seed 107; gather 50 wood; key n; walk to open ground; key f; key c
- **Screenshots:** [planks_locked.png](s07/shots/planks_locked.png)

### 36. [minor] Same shared seed reproduces the starting map but not the world's history (nation names/timing differ)

- **Category:** bug; **reported by** 1 agent(s): s10
- **Detail:** Share screen shows 'World seed 1450162717' and a ?seed= link. A new world from 1450162717 starts identical to the Ironvale world (same trees/rocks/pawns/chickens at the same distances/bearings, same region and objective; seedworld.png matches world0.png). But history diverges within ~1 game minute: original world tick 850 'Isabella Wood founded the nation of Eastwood', tick 1550 'Peggy Smith ... Bigwood', tick 1702 Ruth Wilson 'Northernville'; same-seed world tick 900 'Isabella Wood founded the nation of Westernham', tick 1400 Ruth Wilson 'Greentown'. Player behaviour differed (walked to a rock in run 1, stood still in run 2), which may explain timing, but the same pawn founding the first nation got a different name. Expected a shared seed to give a friend the same nations, or the Share screen to say only the map is shared.
- **Repro:** pt start --seed Ironvale; Start; Begin; play ~1600 ticks; Esc > Share (note seed); Main Menu > Start; clear box, type 1450162717; Begin; pt wait 90; pt events 15
- **Screenshots:** [seed-history-diverges.png](s10/shots/seed-history-diverges.png)

### 37. [minor] Selling in own settlement is coin-starved; Sell Resources sells one unit with no choice

- **Category:** balance; **reported by** 1 agent(s): s05
- **Detail:** Right after founding, Sell Resources gave 'No merchant here can pay: the richest has 9 coins'. Buying food (25 coins for 1 apple) gave the merchants enough to then buy 1 wood for 20. After that: 'richest has 6 coins', while Market Info showed '4/4 stalls, 40 coins'. I had 437 coins. I could not choose which good or how many to sell. The messages are clear and helpful, but trading is mostly blocked.
- **Repro:** Inside new settlement: click Sell Resources (186,445); click Purchase Food (66,445); click Sell Resources again; again
- **Screenshots:** [sell-starved.png](s05/shots/sell-starved.png)

### 38. [minor] Settlement action bar reflows under the pointer, so clicks land on the wrong button

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** When the market filled, 'Build Stall' disappeared, and a pawn walking up added Order/Talk/Gift rows. Build Farm moved from about (625,445) to (476,445), so my click opened 'Send Caravan (Town era)' instead. Expected: buttons that keep their positions, with unavailable ones disabled rather than removed.
- **Repro:** enter own settlement; screenshot; wait for a pawn to approach / market to fill; click where Build Farm was
- **Screenshots:** [buttons_shift.png](s03/shots/buttons_shift.png)

### 39. [minor] Settlement shows 'Village' tier while the nation's era is still Camp

- **Category:** text; **reported by** 1 agent(s): s02
- **Detail:** After building a Farm (tick 8810), the Settlement Info panel said 'Tier: Village (3 buildings)' and the floating label said 'Village of Southernford', but the era panel still said 'Era: Camp (1/4)' with Members 6/10 and Day 1/3 left to do. Two different meanings of 'Village' are on screen at once. It reads as though the goal is already reached when it is not.
- **Repro:** seed 102; found a settlement, enter it, press K twice to build a Farm; compare the Settlement Info panel and the era panel
- **Screenshots:** [day_gate.png](s02/shots/day_gate.png)

### 40. [minor] Settlement-founding warning about resentful neighbours is overwritten after 8 ticks

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** 'Settlement founded. Fortressford and Littleford resent a new settlement so near their land.' (tick 5794) is replaced at tick 5802 by 'You are back on your own land. Objective complete...', so the diplomatic consequence is never readable. Also, the large floating settlement label ('0 here, 3/8 residents') draws behind the Chronicle/status HUD and cannot be read.
- **Repro:** pt start ... --seed 101; found a settlement within ~260 m of two AI settlements; pt events 10
- **Screenshots:** [settled.png](s01/shots/settled.png)

### 41. [minor] Storehouse/caravan gated behind Town era (Village alone needs Day 3); not reachable in a session, and not shown in era unlocks

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** 'Send Caravan (Town era)' looks enabled. Clicking it says 'This settlement needs a Storehouse before it can send caravans. The Storehouse is unlocked in the Town era.' The era box lists Village requirements (Village 0/1, Members 6/10, Stalls 2/2, Day 1/3) and Village unlocks (Workshop), but never mentions the Storehouse. With a 10-game-minute day, a caravan is at least 3+ game days away. The button should look disabled, and the era panel should show the path to the Storehouse.
- **Repro:** Inside own settlement in Camp era: click Send Caravan (640,445); tap era box (110,58) and scroll
- **Screenshots:** [caravan-gate.png](s05/shots/caravan-gate.png)

### 42. [minor] Title, main menu and Config screens: white text on light sky-blue, low contrast

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** Title prompt, menu buttons and Config labels are white/small on ~#72A5E5 with buttons ~#5C9EEB: roughly 2.5:1, under WCAG AA 4.5:1; button edges almost invisible against the background. In-world dark panels are fine by comparison.
- **Repro:** pt start --seed 109; pt shot; space; pt shot
- **Screenshots:** [menu_buttons.png](s09/shots/menu_buttons.png)

### 43. [minor] War confirm status says 'Click War again' but the button now reads 'Confirm'

- **Category:** text; **reported by** 1 agent(s): s04
- **Detail:** When armed, the War button is relabelled 'Confirm' while the status line says 'Click War again to declare war on Westshire.' There is no button labelled War on screen at that point.
- **Repro:** At Rival with Westshire, click War once and read the button and status line
- **Screenshots:** [war_confirm_text.png](s04/shots/war_confirm_text.png)

### 44. [polish] Controls screen page 3 lists debug cheats (F2-F6) to every player

- **Category:** text, ux; **reported by** 2 agent(s): s01, s09
- **Also reported as:** "Controls help lists F2-F6 debug cheats and uses touch wording 'Era box (tap)'"
- **Detail:** Main menu > Controls page 3/3 shows 'Debug' keys: generate land, spawn pawn, coins, wood, teleport pawns. A new player sees these as normal controls. Expected them hidden in release builds or clearly labelled as developer cheats.
- **Repro:** pt start ... ; pt key space; click Controls (480,419); Next twice
- **Screenshots:** [controls_debug.png](s01/shots/controls_debug.png) [controls3.png](s09/shots/controls3.png)

### 45. [polish] World-space settlement label draws through full-screen panels and over the HUD

- **Category:** text, ux; **reported by** 2 agent(s): s08, s09
- **Also reported as:** "Overlay screens (Inventory, Nations/Laws, Pause, Share) draw text with no backing panel over the world and player"
- **Detail:** Inside my settlement, the large label '6 here, 7/20 residents' renders through the semi-transparent Nations panel (O) and collides with the Nations/Laws tabs. It also sits behind the pause menu, the Drought sheet and the top-left HUD.
- **Repro:** enter own settlement, press O
- **Screenshots:** [nations2.png](s08/shots/nations2.png) [laws.png](s09/shots/laws.png)

### 46. [polish] 'Found elsewhere' hint gives a compass direction, but nothing on screen shows which way is south-east

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** Pressing F in a claimed area said 'Open ground lies about 170 to the south-east.' The HUD has no compass or heading, so I had to walk and press F again. The second hint said '230 to the north-west' after I had walked about 100 units, which was confusing. After a third try I found a valid spot.
- **Repro:** with 50 wood, stand in another nation's land, press f; walk; press f
- **Screenshots:** [claimed_land.png](s03/shots/claimed_land.png)

### 47. [polish] Ctrl+A does not select all in the New World seed box

- **Category:** ux; **reported by** 1 agent(s): s10
- **Detail:** To paste a shared seed I clicked the seed box ('Ironvale') and pressed Ctrl+A then Backspace; only the last character was deleted, giving 'Ironval1450162717' (seed 1889372902). Needed ~20 Backspaces to clear. The live 'is seed N' preview made the mistake obvious, which is good. (Could partly be key-timing at 2 fps.) A 'Clear' button or select-all on focus would help when pasting a share seed.
- **Repro:** Main Menu > Start; click seed box; pt key Control_L+a; pt key BackSpace; pt shot
- **Screenshots:** [ctrla.png](s10/shots/ctrla.png)

### 48. [polish] Destructive 'Leave Nation' is the most prominent HUD button

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Bright red full-width button directly below the action row; no confirmation observed on hover. Easy misclick for a leader.
- **Repro:** n; pt shot
- **Screenshots:** [nation2.png](s09/shots/nation2.png)

### 49. [polish] First tree's status says '2 apples' but +1 apple shown and Apples = 1

- **Category:** text; **reported by** 1 agent(s): s01
- **Detail:** Status 'Gathered 5 wood and 2 apples from tree.' while the float text says '+1 apple' and the inventory shows 1, with energy jumping 83->98 (one apple presumably auto-eaten, unannounced).
- **Repro:** pt start ... --seed 101; Begin; walk to nearest tree; pt key e
- **Screenshots:** [apples_mismatch.png](s01/shots/apples_mismatch.png)

### 50. [polish] HUD panels briefly drawn off the bottom of the screen right after --continue

- **Category:** ux; **reported by** 1 agent(s): s10
- **Detail:** First screenshot after `pt start --continue` showed the Inventory, Season and Player Info panels pushed down so they overlap the action buttons and the Player Info energy bar is cut off at the bottom edge. A few seconds later the layout was normal (continued2.png). A fresh New World start did not show this.
- **Repro:** Save from pause menu; pt stop; pt start --continue; pt await WORLD; pt shot
- **Screenshots:** [continue-layout.png](s10/shots/continue-layout.png)

### 51. [polish] In-settlement HUD: Inventory panel overlaps action buttons; unexplained Stockpile and price swings

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** After H then E, the Inventory Info panel sits in the screen centre under the Craft Axe/Craft Pickaxe buttons. 'Collect Stockpile (78)' isn't explained. Stone price fell from 31/28 to 9/8 in about 1300 ticks with no reason given.
- **Repro:** pt start ... --seed 101; after founding, go away, pt key h; pt key e
- **Screenshots:** [home.png](s01/shots/home.png)

### 52. [polish] Jargon: 'Export replay (USD)', 'border quarrels stop at Rival', 'day 1/2' vs 'Day 1/3'

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** 'USD' reads as US dollars; 'Rival' is an undefined relation level; Season panel 'day 1/2' (day of season) sits beside Era checklist 'Day 1/3' (days needed) with different meanings. Config Volume says sound only plays in the browser version.
- **Repro:** Escape; Share
- **Screenshots:** [share.png](s09/shots/share.png)

### 53. [polish] Missing-resource message counts the stockpile without saying so

- **Category:** text; **reported by** 1 agent(s): s05
- **Detail:** With 5 stone in inventory, K for a House (15 stone) said 'Need 0 more wood, 5 more stone'. That was only right because 5 stone sat uncollected in the stockpile. Settlement Info 'Next (K)' also changed from Farm to House between visits, while the Build Farm button was still available.
- **Repro:** Inside settlement with 21 wood/5 stone and 5 stone in the stockpile: pt key k
- **Screenshots:** [kmenu.png](s05/shots/kmenu.png)

### 54. [polish] On entering the settlement, popups overlap and several labels are clipped

- **Category:** text; **reported by** 1 agent(s): s02
- **Detail:** On first entering my settlement, two 'Objective complete' popups stacked on top of the chronicle. 'Enter your settlemen' was clipped, the objective panel's 'Gather 20 wood and 5 stone' line was cut off at the bottom of the panel, and a large world-space label '0 here, 1/8 residents' showed through the top HUD.
- **Repro:** seed 102; found a settlement (F), then press E to enter
- **Screenshots:** [settlement_overlap.png](s02/shots/settlement_overlap.png)

### 55. [polish] Pause menu has no Load entry although controls help lists 'Save, Load, Share, Main Menu'

- **Category:** text; **reported by** 1 agent(s): s10
- **Detail:** Pause menu offers Resume, Save, Share, Controls, Main Menu, Quit. Load is only on the main menu. Either add Load or update the help text.
- **Repro:** In world: Esc; pt shot
- **Screenshots:** [pause.png](s10/shots/pause.png)

### 56. [polish] Red status toast is clipped: 'No entities within range to interact w'

- **Category:** text; **reported by** 1 agent(s): s02
- **Detail:** At 960x540 the red error toast at the top centre cuts off the end of its message.
- **Repro:** press e with nothing in reach
- **Screenshots:** [toast_cut.png](s02/shots/toast_cut.png)

### 57. [polish] Settlement floating label is drawn under the top status banner

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** Standing in Smalltown, the label 'Town of Smalltown / 5 here, 7/12 residents' sits at the top centre exactly where the red status banner appears, so both are partly unreadable.
- **Repro:** seed 106; walk into Smalltown (~50,170); press E with nobody in reach
- **Screenshots:** [overlap.png](s06/shots/overlap.png)

### 58. [polish] Using a rock next to a tree chops the tree instead (interact priority)

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** Twice `pt walkto rock` + E chopped a nearby tree ('Gathered 8 wood...') rather than mining the rock, because interact prefers tree over rock. This is documented behaviour, but it spends axe durability when the player meant to use the pickaxe. Facing direction could break the tie.
- **Repro:** walkto rock near trees (around tick 4291); e
- **Screenshots:** [pickmine.png](s07/shots/pickmine.png)

### 59. [polish] Very low frame/tick rate under the shared load; crafting overlay showed a frame late

- **Category:** perf; **reported by** 1 agent(s): s07
- **Detail:** fps 1-5 and tps 0-17 for most of the session (machine load avg ~54). The first pt start timed out after 60s with an empty game.log; the second worked. Once, state.json said CRAFTING while the screenshot still showed the world view; the overlay appeared ~2s later. Probably the environment, not the game.
- **Repro:** pt start under heavy load; key c; pt shot immediately
- **Screenshots:** [craft_settle.png](s07/shots/craft_settle.png)

## Objective timings

Game seconds from the start of the world to each objective (event tick / the game's ticks per second, from events.jsonl). Game time, not wall time: with many sessions on one machine a world can run slower than real time, and wall-clock timings would mostly measure the machine.

| # | Objective | s01 | s02 | s03 | s04 | s05 | s06 | s07 | s08 | s09 | s10 | min | median | max |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Gather wood | 20 | 56 | 2416 | - | 25 | - | 65 | 25 | 18.1 | - | 18.1 | 25 | 2416 |
| 2 | Eat something | 21 | 57 | 2417 | - | 26 | - | 66.1 | 26.1 | 19 | - | 19 | 26.1 | 2417 |
| 3 | Create or join a nation | 25 | 58 | 2427 | - | 28 | - | 97 | 44.1 | 23 | - | 23 | 44.1 | 2427 |
| 4 | Found a settlement | 116 | 124 | 2501 | - | 115 | - | 146.1 | 149 | - | - | 115 | 135.1 | 2501 |
| 5 | Enter your settlement | 121 | 126 | 2503 | - | 117 | - | - | 152.1 | - | - | 117 | 126 | 2503 |
| 6 | Build a market stall | 125 | 143 | 2508 | - | 121 | - | - | 158.1 | - | - | 121 | 143 | 2508 |

