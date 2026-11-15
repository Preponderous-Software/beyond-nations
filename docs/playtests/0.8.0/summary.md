# Playtest summary: 20261115-1503

10 sessions, build beyond-nations 0.7.1-SNAPSHOT (eefb1f7b), generated 2026-11-15 15:41.

**Findings:** 89 reports in 73 groups -- 0 blocker, 12 major, 51 minor, 26 polish.

## Outcomes

| Session | Goal | Outcome | Fun | Minutes | Objectives | Deaths | Findings | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| s01 | Play the first 10 minutes as a brand-new player who has never seen the game; note every moment of confusion. | partial | 3 | 22.1 | 6 | 0 | 13 | The onboarding spine is strong: clear top-left objectives with rewards, helpful 'too close, open ground lies N paces north-west' feedback, and a lively chronicle make the first steps easy to follow. The settlement step breaks it: the Settlement panel vs. in-settlement HUD ('Back' vs 'Exit Settlement') hides where B/K work, the first entry does not count for its objective, and tips and objectives drift out of sync (stone tip shown during the wood and nation steps; 'Eat something' completes itself). Fix the settlement flow and the objective lag, and a new player would get through the first 10 minutes without confusion. |
| s02 | Reach the Village era as fast as you can. It can now come from day 2 (about 7 minutes of game time) once your nation has 10 members; use courting/recruiting and housing. Record when each Village requirement was met, what held you back, and whether the wait felt meaningful. | partial | 2 | 20.7 | 6 | 0 | 9 | Stalls (2/2 at tick 3424) and the first building were quick, because the settlement stockpile was generous. Members 10 is the real wall. Two pawns join on founding, but almost every other pawn goes off to found its own one-person nation, and the Invite/Gift keys appear nowhere on screen, so courting felt hidden and futile rather than a meaningful wait. Performance (about 10 tps) also put the day-2 time gate beyond a 30-minute session. |
| s03 | Survive the first winter as a prepared ruler. Watch for the winter warning and the Season box food count, store food before winter (trees give NO apples in winter now), use Feed Residents and the Eat button. Record how many of your members die, whether the warning was clear and early enough, and whether preparing visibly helped. | blocked | 3 | 20.2 | 5 | 0 | 7 | The winter-prep feedback that exists is good: 'Winter in 2 days' is on screen from the start and the Season box food count visibly climbs as members lay in apples. But the goal could not be tested: at 5-12 tps the first winter is almost two hours of wall-clock away from an autumn start, there is no way to pass time, and there is no settlement food store or active Feed Residents to act on before winter. Shorten seasons for testing, add a wait/sleep, or make confirms and timers frame-tolerant on slow machines. |
| s04 | Diplomacy and war: lead a nation, meet a neighbour, try alliance, rivalry and war from the Nations screen (O). | partial | 3 | 34.2 | 5 | 0 | 8 | Denounce, war and peace all work with clear confirm clicks and good in-character replies, and the world fills with rival nations fast. But alliance, the warm half of diplomacy, is gated by a hidden +60 rule for stronger partners that a new 1-member nation always faces, after a 10-minute gift treadmill against border friction. Show strength and each nation's real alliance threshold on the row, and make the 2-row, reshuffling 36-nation list easier to navigate. |
| s05 | Trade: use the Trade at Market panel to buy and sell with quantities (check the profit/loss lines and receipts), appoint a Trader from your nation and watch whether traders actually carry goods between markets, build a Storehouse and send a caravan. Judge whether selling pays and whether prices make sense over time. | partial | 3 | 23.7 | 6 | 0 | 8 | The Trade at Market panel itself is good: picking quantities, prices that rise as you buy, receipts, a running 'spent/took/net' line, and 'best price in reach / buy here sell there' hints. The economy behind it doesn't hold up, though: tills of 0-100 coins against goods priced 60+ mean selling barely pays, and prices swing to a zero spread within a minute. Traders, the Storehouse and caravans are gated behind members and the Village era, so a playtest session can't reach them, and nothing on screen shows traders moving goods. |
| s06 | Ruling: lead a nation, enact and repeal laws, check the Members tab (loyalty, traits, who is heir), watch for the 'growing old' warning, then either abdicate from the Laws tab or let the ruler die of old age (about 40 minutes into a reign). Record whether succession is clear and what the heir inherits. | partial | 2 | 33.1 | 6 | 0 | 8 | The law UI works well: proclaiming a law shows a clear '(Low in 30 s)' pending state and a status line, the chronicle confirms when it takes effect, and the borders description plainly states the trade-offs. Thieves came through open borders within a minute, which made repealing feel earned. But the succession loop could not be reached: every nearby pawn founded its own nation in the first game minute, leaders refuse invitations, and nothing on screen says how to gain a first member. So there was no heir, no Abdicate button and no old age. At this machine's 4-16 tps, the 40-game-minute old-age timer is also hours of real time away. |
| s07 | Settlements: found a first settlement, then a second one (later settlements cost more wood and stone; Found Settlement on the command bar says what you lack). Open the settlement screen, give a settlement to a member and take it back, and use the nations screen's Manage tab (rename the nation, list settlements). Record anything confusing about costs, ownership or which settlement is home. | partial | 3 | 26.1 | 6 | 0 | 11 | The first settlement, the settlement screen, Give/Take Back and the Manage tab (rename, settlement list with distance and 'your home', Open from afar) all work and give clear status lines. A second settlement is out of reach in a normal session: the one-per-era cap needs the Village era, and that era requires game day 2. The Found Settlement button just disappears instead of saying so. Ownership is the confusing part: held vs home vs 'yours as leader' vs 'yours personally' needs one plain explanation. |
| s08 | World events: play until world events are offered, take different choices, and judge whether the consequences are clear and fair. | partial | 3 | 22.1 | 5 | 0 | 5 | The world-event sheet itself works well: it pauses the world, gives three options with their costs and outcomes in plain text, and the result is echoed in a toast and the chronicle with the exact cost taken. The pacing defeats the goal, though. On a slow machine only one event arrives in a 30-minute session, so I couldn't compare choices. The 'map' reward points to a place the HUD gives no way to find. |
| s09 | Accessibility and text audit, touching only what a mouse-and-keyboard player would: every screen and panel; cut-off, overlapping, unclear or misspelled text; contrast; anything only explained on hover. | achieved | 3 | 17.4 | 5 | 0 | 15 | The text itself is well written (clear objectives, actionable errors like 'Open ground lies about 70 paces to the south', no typos), but layout and contrast let it down. Panels move when the HUD changes (Inventory jumps to the centre, Player Info gets clipped in settlements), the huge world-space settlement label sits behind the UI, and the reasons buttons are disabled live only in hover tooltips that cover other buttons. Contrast fails on the light-blue menus (~2.5:1), the white-on-yellow progress bar (1.9:1) and the floating gain text. |
| s10 | Save/load and share-seed round trip: save from the pause menu, stop, `pt start --continue`, confirm the world is identical; then start a new world from the seed shown on the Share screen and confirm it matches. | achieved | 3 | 8 | 3 | 0 | 3 | Save/continue and share-seed round trips both work: --continue restored tick 1428 exactly (position, yaw, coins, inventory, nation/role, objective 4, chronicle, nearby trees/rocks by id), the pause menu gives clear 'Saved: Day 1 - Westernshire, 15:09' feedback, and a new world from the Share screen's 1450162717 is identical to the 'Ironvale' world at start (same trees, rocks, pawns, region, HUD). Friction is in the edges: replacing the prefilled seed text is awkward, the Load list doesn't show seeds, and Load isn't reachable from the pause menu. |

## Findings

### 1. [major] First entry into your settlement does not complete objective 5 'Enter your settlement'

- **Category:** bug, ux; **reported by** 3 agent(s): s01, s07, s08
- **Also reported as:** "'Enter your settlement' objective does not complete if you leave via Exit Settlement"; "Objective 'Enter your settlement' doesn't complete when you leave with the settlement screen's Exit Settlement button"
- **Detail:** Objective 5 became active at tick 4352. I pressed E at the well, got the CAMP OF FARMSHIRE screen with status 'Entered settlement.' (tick 4540), then clicked Exit Settlement. The objective stayed on 'Enter your settlement' for 350+ ticks. Only a second entry (tick 4900) completed it. Expected: the first entry completes it.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 101; title, Start, Begin; chop ~11 trees (pt walkto tree; pt key e) to 50 wood; pt key n; walk >250 paces from Hamletburg; pt key f; pt key e; click Exit Settlement (375,435); pt wait 8; pt state -> objective still 5
- **Screenshots:** [enter_obj_not_done.png](s01/shots/enter_obj_not_done.png) [objective5_not_done.png](s07/shots/objective5_not_done.png) [obj5_done.png](s08/shots/obj5_done.png)

### 2. [major] Storehouse and caravan are locked behind the Village era (day 2), out of reach in a session

- **Category:** balance, ux; **reported by** 2 agent(s): s05, s07
- **Also reported as:** "Second settlement is gated behind the Village era, which needs game day 2: unreachable in a normal-length session"
- **Detail:** In Camp era the Send Caravan (Village era) button is lit. Clicking it gives a clear message: 'This settlement needs a Storehouse before it can send caravans. The Storehouse is unlocked in the Village era.' The Village era needs day 2, 10 members and 2 buildings. At the 7-20 tps this machine ran, that is well over an hour of wall time, so the goal's Storehouse and caravan steps could not be tested. The button should probably look disabled until it can be used.
- **Repro:** found a settlement; E to enter; Escape; click Send Caravan (Village era)
- **Screenshots:** [caravan.png](s05/shots/caravan.png) [era_panel.png](s07/shots/era_panel.png)

### 3. [major] Wandering pawns all found one-person nations by day 1 afternoon, leaving no strangers to recruit for Members 10

- **Category:** balance; **reported by** 2 agent(s): s02, s06
- **Also reported as:** "A new ruler cannot get a first member (heir) in a session: nearby pawns all found their own one-pawn nations within the first game minute"
- **Detail:** After founding my settlement, 2 pawns joined automatically (Members 3/10). By tick ~8400 the chronicle had listed 20+ new nations (Lowerham, Villageport, Townshire, Oldton, Blackburg, Westernshire...), most of them one pawn. The nearest stranger, Tilly Smith, said 'I lead Lowerham. We are 1 strong.' I gained no members after the founding, and the Members requirement looked out of reach. Expected: enough unaffiliated pawns early on that courting/inviting can get a nation to 10 by day 2.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 102; key n; chop 10 trees; key f; watch the chronicle and talk (e) to nearby pawns
- **Screenshots:** [pawns_all_leaders.png](s02/shots/pawns_all_leaders.png) [nation-glut.png](s06/shots/nation-glut.png)

### 4. [major] 'Press B inside your settlement' does nothing on the settlement panel; must click the unexplained 'Back' first

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Objective 6 says to press B inside your settlement. Pressing E opens the CAMP OF FARMSHIRE panel; B there does nothing (no message, pressed twice). Outside, B says 'You are not inside a settlement.' Only after clicking 'Back' (as opposed to 'Exit Settlement') does a separate in-settlement HUD with Build Stall / Build House / etc. appear, and then B works. The difference between Back and Exit Settlement is never explained. A new player would get stuck here.
- **Repro:** ...after founding: gather 20 wood + 5 stone; pt key h; pt key e; pt key b (nothing); pt click 583 435 (Back); pt key b -> 'Stall built.'
- **Screenshots:** [stall_flow.png](s01/shots/stall_flow.png)

### 5. [major] 1-3 fps and 3-12 tps (of 50) all session; the day-2 Village gate is ~35 real minutes away

- **Category:** perf; **reported by** 1 agent(s): s02
- **Detail:** pt state showed tps between 2.8 and 11.7 and fps 0-3 throughout. The world started at timeOfDay 0.30 on day 1, so Day 2/2 needs about 21000 ticks; at ~10 tps that is ~35 minutes of real time, longer than the whole session. At tick 8576 it was still timeOfDay 0.586. (Possibly host load from parallel sessions.)
- **Repro:** pt start ... --seed 102; pt state repeatedly; compare tps to 50
- **Screenshots:** [perf_tps.png](s02/shots/perf_tps.png)

### 6. [major] First winter is unreachable in a play session: sim runs at 5-12 tps, winter is ~52k ticks away

- **Category:** perf; **reported by** 1 agent(s): s03
- **Detail:** Starting with --season autumn puts the world at tick 120000 (Autumn day 1/2). Seasons are 2 days (30000 ticks/day), so the one-day winter banner is due at tick 150000 and winter at tick 180000. The game ran at 5-12 tps (fps 1-2) the whole session, so after ~20 real minutes it was only at tick 128852. At ~8 tps the warning is ~45 more minutes away and winter ~110 minutes away; the goal (survive the first winter) could not be played. There is no in-game way to pass time (sleep/wait) to get there.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 103 --season autumn; enter world; pt status every few minutes (tps 5.9-10.7); pt state tick vs 180000
- **Screenshots:** [winter_unreachable.png](s03/shots/winter_unreachable.png)

### 7. [major] Invite (R) and pawn Gift (G) appear nowhere on screen; near a pawn the buttons are nation-level only

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** Standing next to stranger Tilly Smith, the button row offered Propose Alliance to Lowerham, Gift Lowerham 25 Coins, Denounce and Declare War, with no Invite or Court button and no 'R to invite' prompt. I only found R (invite) and G (gift) on page 1 of the ? Controls screen. The objective text ('Members 3/10') does not say how to get members either. Also, the pawn's floating name tag is drawn behind the Chronicle panel, so you can't see who you're chasing.
- **Repro:** pt start ... --seed 102; create nation, found settlement; walk to a non-member pawn (pt walkto pawn); look at the button row and objective panel
- **Screenshots:** [recruit_hidden.png](s02/shots/recruit_hidden.png)

### 8. [major] Market tills are far too small for the prices: a 100-coin till buys about 1 wood at 64

- **Category:** balance; **reported by** 1 agent(s): s05
- **Detail:** At the Village of Westernville the market showed 'Wood 66/63' while its stalls held 100 coins. I carried 56 wood (about 3,500 coins of value on paper), sold 1 for 64, and the panel then said 'No till here can pay for one.' Later the till fell to 13 coins. Selling therefore barely pays: across the whole visit the panel reported 'spent 178, took 186, net +8'. The prices tell the player that gathering is lucrative, but in practice nothing can be sold in volume. Expected either tills scaled to the prices or prices that fall as the till empties. My own new stall had a 0-coin till, so it could not buy anything either.
- **Repro:** pt start --profile beyond-nations --seed 105; chop ~14 trees; N; walk to the settlement 131m away; E; Escape; click Trade at Market; select Wood; Sell
- **Screenshots:** [till_dry.png](s05/shots/till_dry.png)

### 9. [major] Nations screen says 'An alliance needs +30' but stronger nations silently require +60

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** Gifted neighbour Farmshire four times (100 coins, ~8000 ticks) up to Friendly (+33), then clicked Ally: 'Farmshire declines: they are 7 to your 1 and want relations of +60 before taking on so small a partner'. The header only states +30, and the rows never show either nation's strength, so the +60 rule is learned only after spending. A new player-led nation is 1 member, so every AI neighbour is likely 2x stronger and the stated +30 rule effectively never applies early. Expected: show strength per row and the actual threshold each nation wants (e.g. 'Ally (needs +60)').
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 104; space; Start; Begin; n; gather 50 wood; f; o; Tribute Farmshire every 20 s until > +30; Ally
- **Screenshots:** [allied.png](s04/shots/allied.png)

### 10. [major] Only one world event fits in a 30-minute session on a loaded machine; the second can't be reached to compare choices

- **Category:** balance; **reported by** 1 agent(s): s08
- **Detail:** With the nation created at tick ~320 and the settlement founded at tick 3859, the first player event (A lost traveller) came at tick 8359, as the 90-second post-settling rule says. The game ran at 5-13 tps on this shared machine, so reaching it took ~19 real minutes. The second event is due 5-7 game minutes later (15000+ ticks, ~25 more real minutes at ~10 tps), so a 30-minute session sees one choice and can't compare outcomes. Expected: the first two choices fit early, or the event pacing follows real play time when the sim runs below 50 tps.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 108; space; click Start; click Begin; n; chop 10 trees (walkto tree; e); f; wait for the worldEvent in pt state
- **Screenshots:** [event1.png](s08/shots/event1.png)

### 11. [minor] Two-click confirm on Build House times out before the second click lands at low frame rate

- **Category:** bug, ux; **reported by** 3 agent(s): s02, s03, s07
- **Also reported as:** "Click-to-confirm on Build Farm expires before a second click on a slow machine"; "Two-click confirm on Build Farm expires before a slow game can register the second click"
- **Detail:** Clicking Build House shows 'Click again to confirm'. Four times in a row the second click (sent ~6-8 s later in real time, as the UI only reacts at 1-2 fps) produced 'Not confirmed in time, so nothing was done.' (ticks 3668, 3756, 3840, 3904), and the click then re-armed the confirm instead. The house was only built when I sent two clicks back to back (tick 3980). The window looks like ~60 ticks; a wall-clock window or a longer one would avoid this.
- **Repro:** inside your settlement with 20+ wood: pt click 233 413; sleep 5; pt click 233 413; pt events 3
- **Screenshots:** [confirm_timeout.png](s02/shots/confirm_timeout.png) [confirm_timeout.png](s03/shots/confirm_timeout.png) [farm_try.png](s07/shots/farm_try.png)

### 12. [minor] Objective 2 'Eat something' completes by itself, so the player never sees it

- **Category:** ux; **reported by** 2 agent(s): s01, s09
- **Also reported as:** "Objective 'Eat something' completes without the player eating"
- **Detail:** Apples are auto-eaten ('Ate an apple (+15 energy).' appeared with no input) so 'Eat something' completed 48 ticks after objective 1 and was never shown as the active goal. The Eat Apple button and the objective teach nothing. The apple count also looked unchanged after the auto-eat (Apples: 1), which was confusing.
- **Repro:** start; chop 3 trees; pt events -> eat completed at tick 1200 without pressing anything
- **Screenshots:** [obj2_skipped.png](s01/shots/obj2_skipped.png) [chop1.png](s09/shots/chop1.png)

### 13. [minor] Objective progress lags the inventory by several seconds

- **Category:** bug; **reported by** 2 agent(s): s01, s03
- **Also reported as:** "Objective 4 wood bar lags inventory: 'Wood 45/50' while holding 51 wood"
- **Detail:** After chopping, inventory said Wood 5 while the objective said 'Wood 0/10'; later 41 vs 36/50, then 52 vs 47/50 with Found Settlement already enabled; after 'Farm built.' it still said Buildings 0/2. Makes players doubt that the action counted.
- **Repro:** pt walkto tree; pt key e; pt shot (compare Inventory Info and Objective panel)
- **Screenshots:** [wood_lag.png](s01/shots/wood_lag.png) [wood_objective_mismatch.png](s03/shots/wood_objective_mismatch.png)

### 14. [minor] Settlement screen title and HUD top are overlapped by the huge world-space settlement label

- **Category:** ux; **reported by** 2 agent(s): s06, s09
- **Also reported as:** "World-space settlement label '0 here, 1/8 residents' overlaps the Settlement and Nations screen headers"
- **Detail:** Inside or near your own settlement, a large translucent white label ('0 here, 1/8 residents') is drawn across the top of the screen behind 'CAMP OF WESTVILLE', the status bar and the chronicle panel, so both become hard to read. The settlement screen text has no backing panel (for example 'Prices (buy/sell): ... Stone 30/28.' sits over the well roof). Expected the world label to be hidden or moved while UI is over it, and a backing panel behind screen text.
- **Repro:** found a settlement (F), then press E at it
- **Screenshots:** [settlement-screen.png](s09/shots/settlement-screen.png) [settlement-header-overlap.png](s06/shots/settlement-header-overlap.png)

### 15. [minor] 'Give to: you' and 'Take Back' are different actions with nearly identical presentation

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** Give with 'you' selected makes the settlement personally yours ('stays yours if you hand on the rule'). Take Back returns it to 'held by you, as leader'. The difference is real but explained by one dense sentence. Take Back stays visible when you already hold it, and after each Give the picker jumps to another person, which invites misclicks.
- **Repro:** settlement screen with Mia holding: click Give with 'Give to: you'; then Take Back
- **Screenshots:** [give_to_you.png](s07/shots/give_to_you.png)

### 16. [minor] 'Send Caravan (Village era)' is drawn as enabled although the era is locked

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** In the Camp era the Send Caravan button uses the enabled blue style, unlike the other unavailable buttons, and clicking it only shows an error. That 2-line status ('This settlement needs a Storehouse...') overlaps and hides the chronicle header '[Y: more]'.
- **Repro:** inside settlement in Camp era; pt click 560 413
- **Screenshots:** [caravan-status.png](s09/shots/caravan-status.png)

### 17. [minor] 'Village 0/1' in the era checklist is unclear and stays unticked after founding a settlement

- **Category:** text; **reported by** 1 agent(s): s02
- **Detail:** After founding the first settlement the era panel still showed '[ ] Village 0/1'. Only later did the Objective 7 text explain 'A settlement with 2 buildings is a village'. The checklist line could say 'Village (settlement with 2 buildings) 0/1' or show 'Buildings 1/2' itself.
- **Repro:** found a settlement; open the Era panel (+)
- **Screenshots:** [village_req_unclear.png](s02/shots/village_req_unclear.png)

### 18. [minor] 'Village' names both a settlement tier and the next era

- **Category:** text; **reported by** 1 agent(s): s07
- **Detail:** Building a House shows 'The settlement is now a Village!' while the era stays Camp (1/4). The era checklist under 'To reach the Village era' has a line 'Village 1/1'. The objective says 'Buildings 2/2' for the same requirement. It is easy to think the era is already reached.
- **Repro:** build stall x2, farm, house inside settlement; read status and era panel
- **Screenshots:** [village_tier.png](s07/shots/village_tier.png)

### 19. [minor] Building the first market stall turned two neighbours Rival (-20) in the same tick

- **Category:** balance; **reported by** 1 agent(s): s05
- **Detail:** Right after B, the status said 'Easttown resents your border: relations are now Rival (-20). Uppershire resents your border...'. The cause was the border rather than the stall, but the land was so crowded that founding took four tries at moving east. Each try was refused as too close to a new NPC village founded meanwhile: 'Open ground lies about 430 / 150 / 241 / 120 / 50 paces...'.
- **Repro:** press F repeatedly while walking east from Westernville
- **Screenshots:** [stall_built.png](s05/shots/stall_built.png)

### 20. [minor] Chronicle lines truncated with '...' and no way to read them in place

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** Most recent-chronicle lines are cut off ('...declared the nation of Villageton, t...', and the player's own 'You raised a banner and named your people We...'). Hovering does not expand them; you have to press Y. The full Chronicle window (Y) is translucent, so the HUD panels behind it show through its text, and its header reads '4 entries. Listing 2.', which is unclear. 'Your news' also lists other nations' foundings while the player has no nation.
- **Repro:** chop a few trees; observe chronicle; pt key y
- **Screenshots:** [chron-translucent.png](s09/shots/chron-translucent.png)

### 21. [minor] Config options have no explanation or tooltip; desktop Volume label says sound only works in the browser

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** 'Chunk Size', 'Location Scale', 'Lag Prevention' and 'Respawn Pawns' show nothing when hovered (waited 2s), so a player cannot tell what they do or what they cost. 'Volume (sound plays in the browser version)' on the desktop build tells the player that sound does not play here, but Volume and Mute controls are still offered.
- **Repro:** Main menu > Config; pt move 528 276
- **Screenshots:** [cfg-no-tooltips.png](s09/shots/cfg-no-tooltips.png)

### 22. [minor] Couldn't appoint a Trader: there is no clear path to a serf member in the time available

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** Appoint Trader needs a serf member who has a home settlement. In 23 minutes I founded a nation and a settlement and built a stall (members 1/10), but recruiting pawns was too slow at 2 fps. Pawns in other nations offer no trade-related option. Nothing on screen shows traders at work (no events and no HUD lines about trader runs), so I couldn't tell whether traders carry goods between markets. The panel's 'Best price in reach: 44 at the Camp of Townfield' and 'Buy here, sell there: +15 a unit' lines are the only hint of trader logic.
- **Repro:** full session as above; walk to a nearby pawn and talk to it
- **Screenshots:** [talk.png](s05/shots/talk.png)

### 23. [minor] Declared war on a neighbour has no felt consequence over ~6000 ticks

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** War on Littleham (neighbour, 2 to my 1) at tick 4063 and on Bluetown at 2067: by tick 10275 no raid, siege or warning had happened (sheltered-era raid cap). War reads as a label change only; the screen does not say I am sheltered or when raids could start. Peace with Bluetown was refused with a clear reason (good).
- **Repro:** pt start ... --seed 104; n; f; o; War Littleham twice; play ~2 min of game time
- **Screenshots:** [war2.png](s04/shots/war2.png)

### 24. [minor] Disabled action buttons explain themselves only on hover, and the tooltips cover the neighbouring buttons

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Greyed buttons (Enter Settlement, Plant Sapling, Found Settlement, Build House, Build Stall, Trade at Market) give the reason only in a hover tooltip ('No settlement has been founded yet.', 'Needs 35 more wood.', 'Needs 17 more wood and 15 more stone...'). A keyboard player never sees these reasons. The tooltips also cover the button itself or the whole next row: Found Settlement's tooltip covers Leave Nation, Build House's covers the second row, and Trade at Market's covers the Player Info header.
- **Repro:** in world: pt move 298 517 (Enter Settlement); after N: pt move 75 476; in settlement: pt move 233 413
- **Screenshots:** [hover-enter-settle-note.png](s09/shots/hover-enter-settle-note.png)

### 25. [minor] E next to a member pawn inside the settlement exits the settlement instead of talking; member shown as 'Stranger'

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** R on Eve Wood said 'Eve Wood is already a member of Farmshire', yet E left the settlement (settlement has interact priority), and the button then read 'Talk to Eve Wood (Stranger)'. A member labelled Stranger is confusing.
- **Repro:** inside settlement HUD: pt walkto pawn; pt key r; pt key e
- **Screenshots:** [e_exits_not_talk.png](s01/shots/e_exits_not_talk.png)

### 26. [minor] Floating gain text (+5 wood etc.) has no outline; 2.2-3.2:1 over grass

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** The floating '+2 saplings', '+2 apples' and '+5 wood' after chopping are light green or brown text with no outline over green grass and the trunk. Measured 2.2:1 for brown '+5 wood' and 3.2:1 for green. The 'Objective complete!' toast is also drawn on top of these floats ('+3 apples', '+50 coins'), hiding them.
- **Repro:** pt walkto tree; pt key e; pt shot
- **Screenshots:** [toast-overlap.png](s09/shots/toast-overlap.png)

### 27. [minor] Found Settlement button vanishes from the command bar when blocked by the era cap

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** Before the first settlement the greyed Found Settlement button had a useful tooltip ('Needs 50 more wood.'). Once you hold one settlement the button disappears entirely, even in the wilds with enough wood, so the player gets no 'what you lack' hint. Only pressing F (or reading the Nations tab) reveals the one-settlement-per-era cap. Expected: a greyed button with that reason as its tooltip.
- **Repro:** after founding the first settlement, walk 120+ m into the wilds; look at the command bar; key f
- **Screenshots:** [press_f.png](s07/shots/press_f.png)

### 28. [minor] Founding a camp near neighbours wiped an earlier gift; border friction makes alliance a treadmill

- **Category:** balance; **reported by** 1 agent(s): s04
- **Detail:** Gift at tick 2067 took Farmshire to +10; founding my camp (tick 3927) cost -10 with five neighbours ('...resent a new settlement so near their land', shown briefly then overwritten by 'Objective complete'), and friction brought Farmshire to -2 by tick 4063 and +8 -> +5 later. Nothing warns before pressing F that founding here will anger five nations, and the resentment message is replaced on the status line almost immediately.
- **Repro:** pt start ... --seed 104; n; o; Tribute Farmshire; gather 50 wood; walk ~120,-60; f; o
- **Screenshots:** [gift_eroded.png](s04/shots/gift_eroded.png)

### 29. [minor] Founding a settlement: direction hints ('10 paces north-west') but no compass or map on screen

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** F gives a helpful 'Too close to the Village of Hamletburg: 241 paces away... Open ground lies about 10 paces to the north-west.' But the only orientation aid is the 'Facing:' text in Player Info, so following the hint is guesswork; it took 3 attempts and once I walked closer (229 paces). The Found Settlement button was enabled at 50 wood with no hint that the spot was invalid.
- **Repro:** with 50 wood near (-62,89): pt key f; follow the hint; pt key f
- **Screenshots:** [founded.png](s01/shots/founded.png)

### 30. [minor] Game ran at 1-3 fps and 2-12 tps on the shared machine

- **Category:** perf; **reported by** 1 agent(s): s01
- **Detail:** World ran at roughly 15-20% speed (tps 2.7-12, fps 1-3), so 22 minutes of wall time covered only about 3 minutes of game time. Clicks and screenshots lagged a frame behind. Probably caused by machine load from parallel sessions, but it prevented playing a full 10 game-minutes.
- **Repro:** pt status during play
- **Screenshots:** [rock1.png](s01/shots/rock1.png)

### 31. [minor] Gift spends the envoy, so Ally can never follow the gift that reaches +30

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** The Tribute that took Farmshire to +33 sent the envoy away; clicking Ally right after answered 'Your envoy to Farmshire is still on the road: ... 20 seconds'. Players must over-gift and then wait 20 s, with border friction eroding the score meanwhile. Expected: Ally offered in the same trip, or the gift reply to say 'you can propose an alliance when your envoy returns'.
- **Repro:** As above; Tribute Farmshire at +23 -> +33; click Ally immediately
- **Screenshots:** [envoy_after_gift.png](s04/shots/envoy_after_gift.png)

### 32. [minor] Greyed-out Build House / Build Farm buttons show no cost or reason on hover

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** Hovering the disabled Build House button showed nothing (before the first stall was built), so I couldn't tell its cost or that houses raise the resident cap. Later a tooltip did appear ('Needs 14 more wood, counting the settlement's stockpile'), so it is inconsistent.
- **Repro:** enter your settlement with <20 wood; pt move 233 413; pt shot
- **Screenshots:** [no_cost_tooltip.png](s02/shots/no_cost_tooltip.png)

### 33. [minor] In-settlement HUD: Inventory panel jumps to screen centre and Player Info panel is cut off by the bottom edge

- **Category:** ux; **reported by** 1 agent(s): s09
- **Also reported as:** "Inventory screen (I) shows less than the HUD"
- **Detail:** After founding and entering the settlement, the Inventory Info panel moves from bottom-left to the middle of the screen (x~220,y~226), over the well and the 'Objective complete!' toast. The Player Info panel at bottom-right is clipped by the window edge: only Energy and 'Nation: Westville (LEADER)' show, and Relationships/Role/Facing/Land are gone. Expected the panels to stay docked and fully visible. Same displacement happens in the open world when the Era box is expanded (the Tip window then overlaps Inventory and cuts off 'Coins: 33[9]'; shots/era-expand-overlap.png).
- **Repro:** pt start --seed 109; Start; Begin; chop to 50 wood; N; walk south out of range of other camps; F; E; Escape
- **Screenshots:** [in-settlement-hud.png](s09/shots/in-settlement-hud.png) [inventory-sparse.png](s09/shots/inventory-sparse.png)

### 34. [minor] Inside the settlement the player can't move and Escape opens Pause; nothing in the world view says why

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** After entering the settlement and pressing Back on the settlement sheet, the 3D view looks like the open world, but W/A/S/D and walkto do nothing (x/z stayed 16.45/163.07 for ~1400 ticks), and Escape opens the pause menu. Only the 'Exit Settlement' button leaves. Expected: Escape or walking to leave, or a clear 'Inside Blueport - Exit Settlement to leave' banner.
- **Repro:** found a settlement; press e to enter; click Back; hold w for 20 s; pt state shows the same position
- **Screenshots:** [stuck_in_settlement.png](s02/shots/stuck_in_settlement.png)

### 35. [minor] Laws tab no-heir line uses 'was left' on a nation nobody ever joined

- **Category:** text; **reported by** 1 agent(s): s06
- **Detail:** On a brand-new 1-member nation the Laws tab reads 'No heir. No one in Easternwood was left to take your place.' The past tense 'was left' suggests members died or left. Expected wording like the Members tab: 'No heir: no one else has sworn to Easternwood yet.'
- **Repro:** n; o; click Laws
- **Screenshots:** [laws-no-heir-text.png](s06/shots/laws-no-heir-text.png)

### 36. [minor] Light-blue menu screens: white text on #73A6E6 is about 2.5:1 contrast

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** The title ('Press any key or click to continue'), the Config labels, the New World seed prompt, the main menu 'Last played: ...' line and the Load title are all small white text directly on the sky-blue background, measured at 2.52:1, below WCAG AA (4.5:1). The dark-blue buttons themselves read fine.
- **Repro:** pt start; observe title, main menu, Config
- **Screenshots:** [title-contrast.png](s09/shots/title-contrast.png)

### 37. [minor] Lost traveller 'map' reward gives no on-screen marker; '160 paces south-east' can't be followed

- **Category:** ux; **reported by** 1 agent(s): s08
- **Detail:** I chose 'Share a meal for the map (1 food)'. The cost was clear (apples 40->39), and the toast and chronicle said the map marks stone and a cache about 160 paces south-east. But nothing appears on the HUD: no waypoint, compass arrow or distance. 'Paces' isn't a unit used anywhere else, and the only direction cue is the 'Facing:' line in Player Info. I could not tell whether I reached the spot. Expected: a marker or bearing/distance readout while the map is held.
- **Repro:** As above; at the Lost traveller sheet click option 1 (480,348); then try to find the cache from the screen alone
- **Screenshots:** [no_map_marker.png](s08/shots/no_map_marker.png)

### 38. [minor] Market panel overflows the 540px window, and Escape doesn't close it

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** With the full good list, the receipt and visit-total lines and the Close button fall below the bottom edge (only a sliver of Close shows at y~535). Escape opens the pause menu instead of closing the panel. While the panel is open, W and Shift+W do nothing: I held them for 25s and did not move. The only way out was to click the sliver of Close.
- **Repro:** open Trade at Market at Westernville after a couple of trades; press Escape; hold w
- **Screenshots:** [stuck.png](s05/shots/stuck.png)

### 39. [minor] Market panel rows move under the cursor while it is open, so clicks hit the wrong good

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** Between two frames the good list grew (Bricks, Axes and Pickaxes appeared) and the quantity and buy/sell rows moved down. My click on Wood, then All, landed on Saplings. One later click meant for 'All' sold 1 wood instead. At 2 fps a layout that reflows while it is open is hard to hit.
- **Repro:** open Trade at Market at Westernville; click Wood (y~137) and then All quickly
- **Screenshots:** [list_shift.png](s05/shots/list_shift.png)

### 40. [minor] Market window overlaps the command bar; a layout shift made me sell stone I meant to buy

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** With a pawn nearby, the command bar grows extra rows ('Order Mia Johnson: Gather Wood', Talk, Gift, Craft, Eat Apple) that draw over the market's Buy/Sell row. Selecting the Stone row moved the controls, and my next click at (487,328), meant for +5, sold 1 stone to Mia for 27 coins. I needed stone for a House.
- **Repro:** in settlement: Trade at Market (563,445) with Mia nearby; click Stone row (480,177); click (487,328)
- **Screenshots:** [market_stone.png](s07/shots/market_stone.png)

### 41. [minor] Members and Laws tabs say 'No heir' but never say how to get one

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** Members: 'No heir: if you die now, your line ends.' Laws: 'No heir... Old age will not take you before you have one.' Neither says how to gain a member: invite a nationless pawn you know (Acquaintance) with E, or own 2 stalls so immigrants come, with taxes not High and open borders doubling the rate. I only found the rules by reading code. Laws on immigration are shown as the borders description but taxes' effect on immigration is not visible. A one-line hint here would turn a dead end into a plan.
- **Repro:** Create a nation (n), found a settlement (f), press o, open Members and Laws tabs
- **Screenshots:** [no-heir-path.png](s06/shots/no-heir-path.png)

### 42. [minor] Nations list shows only 2 rows of 36 nations, keeps old scroll offset, and reorders between visits

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** Once you have a settlement the header grows and only 2 nation rows fit; with 36 nations there is no search or jump. The scroll offset persists between openings (reopened at 'Nations 3-4 of 21', hiding the two most important rows: the enemies), and rows reorder from visit to visit, so a remembered position clicks a different nation (my first Ally click went to Farmshire, not Littleham as on the previous visit). Finding Farmshire took 5-8 Up/Down clicks each time at ~2 fps.
- **Repro:** pt start ... --seed 104; n; o; Down; Escape; found a settlement; o
- **Screenshots:** [scroll_persist.png](s04/shots/scroll_persist.png)

### 43. [minor] New World seed box: Ctrl+A does not select all, so pasting a shared seed means backspacing the prefilled one

- **Category:** ux; **reported by** 1 agent(s): s10
- **Detail:** On the New World screen the seed box is prefilled with the previous seed ('Ironvale'). To enter the number from the Share screen I clicked the box, pressed Ctrl+A then Delete/BackSpace: nothing was selected, only one character was removed, and typing appended ('Ironval1450162717' -> seed 1889372902, a different world). I had to press BackSpace ~10 times. Expected Ctrl+A (or click-to-select-all) to select the field. May be partly low-fps modifier timing under xdotool; the seed preview line ('X is seed N') did make the mistake visible, which is good.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed Ironvale; pt key space; pt click 480 315; pt click 480 235; pt key ctrl+a; pt key Delete; pt type 1450162717; pt shot x
- **Screenshots:** [ctrla-noselect.png](s10/shots/ctrla-noselect.png)

### 44. [minor] New member Peggy White still talks as a 'Stranger' who won't follow the leader

- **Category:** text; **reported by** 1 agent(s): s03
- **Detail:** After Peggy White joined Hamletfield (chronicle 'Peggy White joined Hamletfield', residents 2/8), talking to her again shows 'Peggy White (Stranger): "Yes, leader? I hardly know you. Why should I follow you? I stand by my own."' Calling me leader and refusing to follow in one line makes it unclear whether she is in my nation.
- **Repro:** ...create nation (N), found settlement (F); pt walkto pawn; pt key e (she joins); pt key e again
- **Screenshots:** [member_says_stranger.png](s03/shots/member_says_stranger.png)

### 45. [minor] Nowhere to 'store food' in a settlement: Stores shows only funds, wood and stone

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** The settlement sheet's Stores section lists Funds and Stockpile (wood/stone) only; there is no food line and no deposit option. Winter readiness is only the HUD Season box 'Food: N of M for winter', which counts members' packs (not the player's 25 apples). A player told to store food expects to put apples in the settlement; Feed Residents stays greyed '(0 hungry)' all autumn, so there is no visible way to act on the count before winter.
- **Repro:** ...found settlement; walk to it; pt key e (settlement sheet); compare with HUD Season box
- **Screenshots:** [no_food_store.png](s03/shots/no_food_store.png)

### 46. [minor] Objective 7 progress label clipped: 'mbers 2/10 • Buildings 0/2'

- **Category:** text; **reported by** 1 agent(s): s01
- **Detail:** The 'Members' word is cut off on the left of the progress bar. The goal also needs 10 members while the camp shows 'Residents: 2/8', and nothing says how to reach 10 (the only nearby pawn is already a member; others are 250m away).
- **Repro:** reach objective 7 (after building the stall); look at the Objective panel
- **Screenshots:** [obj7_clipped.png](s01/shots/obj7_clipped.png)

### 47. [minor] Objective progress bar uses white text on a yellow fill (~1.9:1)

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** The objective progress bar ('Wood 15/50', 'Wood 50/50') and the Village progress square are yellow (230,179,0) with white text on top, measured at 1.94:1 contrast. When the bar is full the label is almost unreadable. Expected dark text on the yellow, or a darker fill.
- **Repro:** N to create nation; objective 4 'Found a settlement' bar
- **Screenshots:** [found-too-close.png](s09/shots/found-too-close.png)

### 48. [minor] Old age (40 game minutes) is out of reach in real time at the speed the game runs here

- **Category:** balance; **reported by** 1 agent(s): s06
- **Detail:** tps ran between 4 and 16 the whole session (50 is full speed). After 33 minutes of wall clock only tick 11428 (3.8 game minutes) had passed. At that rate the 'growing old' warning (30 game min) and death (40 game min) need about 2.5-5 hours of real play. The world also pauses while the Nations, Settlement and world-event screens are open, which slows it further. The warning could not be observed.
- **Repro:** Play any session; compare pt state tick against wall clock
- **Screenshots:** [stuck2.png](s06/shots/stuck2.png)

### 49. [minor] Ownership vs home is confusing: a settlement 'held by Mia Johnson' is still 'your home'

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** After Give to Mia, the Manage list reads 'Camp of Ashmark: held by Mia Johnson, 2/8 residents, 129 paces south-west (your home).' The settlement screen still says 'It is your home.' Nothing explains what holding means versus home, or what Mia may do with the stockpile (29 wood, 15 stone). Give and Take Back themselves worked and gave clear status lines.
- **Repro:** O -> Manage -> Open -> > (Mia Johnson) -> Give; Back
- **Screenshots:** [manage_after_give.png](s07/shots/manage_after_give.png)

### 50. [minor] Pressing E at your own settlement keeps entering it, which pauses the world and blocks chopping nearby

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** Interact checks for a settlement first. Chopping trees near my camp kept opening the Settlement screen, which pauses the world, and several rounds of chopping did nothing (wood stayed at 2, later at 19). B (build stall) only works after entering and then pressing Back instead of Exit Settlement. On 'your own land' at the well, B says 'You are not inside a settlement.', which is confusing.
- **Repro:** f (found a settlement); walk to a tree within ~10 m of the well; press e repeatedly
- **Screenshots:** [now.png](s06/shots/now.png)

### 51. [minor] Red 'Leave Nation' appears in the exact slot 'Create Nation' just occupied, with no warning

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Pressing N re-flows the action bar: a red 'Leave Nation' button takes the bottom-left spot where 'Create Nation' was a moment earlier, and hovering it gives no warning about consequences. The nation is also auto-named ('Westville') with no naming prompt.
- **Repro:** pt key n; observe bottom-left; pt move 90 517
- **Screenshots:** [nation-created.png](s09/shots/nation-created.png)

### 52. [minor] Sell button total doesn't hold: 'Sell 6 for 178' sold 5 for 122

- **Category:** bug; **reported by** 1 agent(s): s05
- **Detail:** I bought 6 apples for 178 (the price climbed 28 to 33 as I bought, a nice touch). The sell button then read 'Sell 6 for 178', exactly what I had paid. Clicking it sold 5 for 122, and the top status still said 'Bought 6...'. The receipt line was right, and so was 'This visit: spent 178, took 186, net +8'. But the price and the till amount drifted between the preview and the click, and nothing warned about it. Apples swung 34/33 to 21/21 (zero spread) within about one game-minute.
- **Repro:** Trade at Market at Westernville; Apples; +5; Buy; then Sell
- **Screenshots:** [sell6b.png](s05/shots/sell6b.png)

### 53. [minor] Settlement panel contradicts itself and leads with a confusing 'Give to' control

- **Category:** text; **reported by** 1 agent(s): s01
- **Detail:** 'Residents: 1 of 8 (0 here now). You are inside it.' The biggest control on the screen is 'Give to: you / Give' with text about giving the settlement away, which makes no sense to a new player on their first camp. The panel does not point to the next step (K for a farm, B for a stall).
- **Repro:** found settlement; pt key e
- **Screenshots:** [settle_screen.png](s01/shots/settle_screen.png)

### 54. [minor] Seven nations resent a new player settlement at once

- **Category:** balance; **reported by** 1 agent(s): s06
- **Detail:** Founding my first settlement gave 'Cityland and Hamletburg and Blueshire and Bigport and Newville and Blackton and Fortshire resent a new settlement so near their land.' This came 113 game seconds into the world, in a forest region. The long 'and ... and ...' list also reads awkwardly.
- **Repro:** Chop 50 wood; press f
- **Screenshots:** [founded.png](s06/shots/founded.png)

### 55. [minor] Tips and objectives out of sync: 'Gather stone' tip stuck for most of the session; 'Your goals' tip arrives at objective 6

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Tip 3 'Gather stone' was shown from the first chop (objective 1) until objective 6, while objectives asked for wood, a nation, a settlement. Tip 5 'Your goals are at the top left' first appeared after objective 6, long after it would have helped. The tip popup also overlaps the Inventory panel in the in-settlement HUD.
- **Repro:** start; chop one tree; observe tip panel through objectives 1-6
- **Screenshots:** [obj_wood_mismatch.png](s01/shots/obj_wood_mismatch.png)

### 56. [minor] Wandering pawns are very hard to stay within reach of for E/R

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** pt walkto pawn timed out or reported 'stuck: not getting closer' in 6 of 8 tries, because wandering pawns move about as fast as the player. When I did reach Tilly Smith and talked, she had walked out of the 5-unit reach before my next key ('No entities within range'), and R later said 'There is no one within reach to invite.' Pawns could pause when the player approaches or talks to them.
- **Repro:** pt walkto pawn 30; pt key e; pt key r
- **Screenshots:** [invite3.png](s02/shots/invite3.png)

### 57. [polish] Giant floating settlement label '1 here, 2/8 residents' drawn across the top of every screen near home

- **Category:** ux; **reported by** 3 agent(s): s01, s03, s08
- **Also reported as:** "Floating settlement label clipped at top of screen and behind panels; Season box food count works well"; "Settlement floating label ('N here, 3/8 residents') renders huge and half-hidden behind the chronicle panel"
- **Detail:** The world label is huge and translucent, sits behind HUD text, status toasts, the settlement panel title and even the PAUSED screen. It never reads as a settlement name. On founding, my character was also inside the well model and not visible.
- **Repro:** found a settlement; stand at the well; pt key Escape
- **Screenshots:** [pause_label.png](s01/shots/pause_label.png) [season_food_count.png](s03/shots/season_food_count.png) [founded_label.png](s08/shots/founded_label.png)

### 58. [polish] Objective 7 progress text is clipped on the left, and the floating settlement nameplate overlaps the HUD

- **Category:** text; **reported by** 3 agent(s): s05, s06, s07
- **Also reported as:** "Objective 7 progress text clipped on the left ('nbers 1/10')"; "Objective progress bar text clipped on the left ('bers 2/10 · Buildings 0/2')"
- **Detail:** The progress bar reads 'nbers 1/10 • Buildings 0/2' (Members cut off). The large in-world nameplate ('Village of Westernville / 3 here, 4/8 residents') draws over the top HUD and over the settlement screen's own title.
- **Repro:** build the first stall in your settlement; look at the Objective 7 box
- **Screenshots:** [stall_built.png](s05/shots/stall_built.png) [objective-progress-clipped.png](s06/shots/objective-progress-clipped.png) [era2.png](s07/shots/era2.png)

### 59. [polish] Player-facing Controls screen lists debug/cheat keys (F2-F6)

- **Category:** text, ux; **reported by** 2 agent(s): s01, s09
- **Also reported as:** "Controls help spends page 3 of 3 on debug cheat keys"
- **Detail:** Page 3 of the Controls screen on the main menu advertises 'Spawn coins', 'Spawn wood', 'Teleport every pawn here'. A new player sees cheats as normal controls.
- **Repro:** main menu > Controls > Next > Next
- **Screenshots:** [controls3.png](s01/shots/controls3.png) [controls3-debug.png](s09/shots/controls3-debug.png)

### 60. [polish] 'Tribute' button label used for ordinary gifts to neutral nations

- **Category:** text; **reported by** 1 agent(s): s04
- **Detail:** Every row's coin button reads Tribute, but to a Neutral nation the reply is 'accepts your gift'; tribute (with a truce) only applies to a stronger foe. Label could switch between Gift and Tribute.
- **Repro:** n; o; Tribute any Neutral nation
- **Screenshots:** [tribute1.png](s04/shots/tribute1.png)

### 61. [polish] Chronicle day number differs from Season box day

- **Category:** text; **reported by** 1 agent(s): s03
- **Detail:** On the first day of autumn the chronicle says 'Day 5, morning' while the Season box says 'Autumn - day 1/2' (probe day=4). Two different day counts on screen make it hard to time the winter countdown.
- **Repro:** pt start ... --season autumn; create a nation; read chronicle vs Season box
- **Screenshots:** [chronicle_day_offby1.png](s03/shots/chronicle_day_offby1.png)

### 62. [polish] Escape from the settlement screen leaves you frozen in the in-settlement HUD with no hint that you are still inside

- **Category:** ux; **reported by** 1 agent(s): s08
- **Detail:** After Escape from the settlement sheet, W/Shift+W did nothing for 25 seconds; the player was still 'inside'. Only the HUD layout changing (Build Stall/Build House buttons) shows this. I expected Escape to return me to walking, or a visible 'You are inside - Exit Settlement to walk' cue.
- **Repro:** e at own settlement; Escape; hold w
- **Screenshots:** [se.png](s08/shots/se.png)

### 63. [polish] HUD 'Village: 0/4', yellow square and Era '+' are unlabelled and have no tooltip

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** Nothing tells the player what 'Village: 0/4' counts, what the yellow square is, or that 'Era: Camp (1/4) +' can be clicked. Clicking it shows a good checklist ('To reach the Village era...'). Controls page 2 calls it 'Era box (tap)', which is touch wording on desktop.
- **Repro:** pt move 90 84; pt click 110 58
- **Screenshots:** [village-unclear.png](s09/shots/village-unclear.png)

### 64. [polish] Laws tab: 'No one in Townland was left to take your place' reads as if someone left

- **Category:** text; **reported by** 1 agent(s): s04
- **Detail:** For a one-member nation the heir line says 'No heir. No one in Townland was left to take your place.' Suggested: 'No heir: no one else in Townland can take your place.'
- **Repro:** n; o; Laws
- **Screenshots:** [laws.png](s04/shots/laws.png)

### 65. [polish] Load list rows show no seed or world name; Main Menu auto-saves even a just-begun world

- **Category:** ux; **reported by** 1 agent(s): s10
- **Also reported as:** "Pause menu has Save but no Load; Load is only on the main menu"
- **Detail:** After starting a fresh world from the shared seed and going Pause > Main Menu at tick 125, the Load screen lists two rows: 'Day 1 - no nation 15:13' and 'Day 1 - Westernshire 15:11'. Neither shows the seed, so two worlds of the same seed are only distinguishable by nation/timestamp. The throwaway world was auto-saved into its own slot without asking. Expected the seed (or seed word) on each row; possibly skip saving worlds with no progress.
- **Repro:** ...after a saved world exists: Pause > Main Menu; Start; enter same seed; Begin; Escape; Main Menu; Load
- **Screenshots:** [load-list-no-seed.png](s10/shots/load-list-no-seed.png) [pause-no-load.png](s10/shots/pause-no-load.png)

### 66. [polish] Load screen 'Delete' button styled like the load button and flush against it

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** 'Delete' uses the same dark-blue style directly beside the save row, with no danger colour and no tooltip. (Not clicked, so whether it asks for confirmation is untested.) The pause menu has no Load, only the main menu does; that is fine, but the brief expected it in the pause menu.
- **Repro:** Pause > Main Menu > Load
- **Screenshots:** [load-delete.png](s09/shots/load-delete.png)

### 67. [polish] N creates a nation instantly with a random name; no chance to name it

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Pressing N made 'Farmshire' with no prompt or confirmation. Naming your nation is an expected moment of ownership. Chronicle lines are also truncated with '...' (e.g. 'with Yvonne...').
- **Repro:** after objective 2: pt key n
- **Screenshots:** [nation_no_name.png](s01/shots/nation_no_name.png)

### 68. [polish] Settlement action buttons stack over the Objective panel when a resident is nearby

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** With Quinn Jones nearby, extra rows (Appoint Trader, Order: Gather Wood, Talk to, Gift...) cover the Objective panel, so 'Reach the Village era' is hidden behind them.
- **Repro:** inside your settlement with a resident standing next to you; pt shot
- **Screenshots:** [buttons_overlap.png](s02/shots/buttons_overlap.png)

### 69. [polish] Settlement billboard text shows through the settlement sheet and Nations header

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** The large grey world-space label '0 here, 1/8 residents' draws behind the 'CAMP OF TOWNLAND' title and the Nations screen header, cluttering the top of both panels.
- **Repro:** Found a settlement, stand in it, press e or o
- **Screenshots:** [overlay_billboard.png](s04/shots/overlay_billboard.png)

### 70. [polish] Settlement screen text sits over the 3D scene with no backdrop; residents line is confusing

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** The giant floating '0 here 2/8 residents' label, the well and trees show through behind the Ownership/People text. 'Residents: 2 of 8 (0 here now)' next to 'You are inside it' reads as a contradiction.
- **Repro:** walkto settlement; key e
- **Screenshots:** [settlement_screen_overlay.png](s07/shots/settlement_screen_overlay.png)

### 71. [polish] Settlement screen wording is self-contradictory and buttons are vague

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** 'Residents: 1 of 8 (0 here now). You are inside it.' contradicts itself. 'Give it to yourself to keep it if the rule passes on' is hard to parse, and the 'Give to: you' selector is grey on grey. The screen has both 'Exit Settlement' and 'Back', and the HUD has a plain 'Settlement' button (which reopens this screen). 'Purchase Food', 'Sell Resources' and 'Withdraw Funds' have no tooltip giving cost or effect.
- **Repro:** E at own settlement
- **Screenshots:** [hover-purchase-note.png](s09/shots/hover-purchase-note.png)

### 72. [polish] Settlements have no names of their own; a sworn member is labelled 'Stranger'

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** Rename works well ('Easternton is now called Ashmark.'), and the list follows ('Camp of Ashmark'). Settlements are only '<Tier> of <Nation>' (the probe just says 'Settlement'), so several would be told apart only by paces and direction. The Hand-to picker shows my sworn member as 'Mia Johnson (Stranger, 0)'.
- **Repro:** O -> Manage; type a name in the box; Rename
- **Screenshots:** [renamed.png](s07/shots/renamed.png)

### 73. [polish] Share screen shows only the numeric seed, not the word seed the world was made from

- **Category:** text; **reported by** 1 agent(s): s10
- **Detail:** World was created from 'Ironvale'; Share shows 'World seed 1450162717' and a ?seed=1450162717 link. That round-trips correctly (verified), but the memorable word is lost when sharing. Nice-to-have: show 'Ironvale (1450162717)'.
- **Repro:** pt start ... --seed Ironvale; Begin; Escape; Share
- **Screenshots:** [copylink.png](s10/shots/copylink.png)

## Objective timings

Game seconds from the start of the world to each objective (event tick / the game's ticks per second, from events.jsonl). Game time, not wall time: with many sessions on one machine a world can run slower than real time, and wall-clock timings would mostly measure the machine.

| # | Objective | s01 | s02 | s03 | s04 | s05 | s06 | s07 | s08 | s09 | s10 | min | median | max |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Gather wood | 23 | 22 | 2424 | 39 | 24 | 93 | 28 | 26.1 | 29 | 22 | 22 | 27.1 | 2424 |
| 2 | Eat something | 24 | 23 | 2425 | 40.1 | 25 | 94 | 29 | 27 | 30 | 23 | 23 | 28 | 2425 |
| 3 | Create or join a nation | 28 | 24 | 2435 | 41 | 30 | 95 | 30 | 28.1 | 41 | 25 | 24 | 30 | 2435 |
| 4 | Found a settlement | 87 | 59 | 2516 | 79 | 154 | 113 | 62 | 78.1 | 98 | - | 59 | 87 | 2516 |
| 5 | Enter your settlement | 98 | 64 | 2529 | 82.1 | 157 | 119 | 76 | 170.1 | 100 | - | 64 | 100 | 2529 |
| 6 | Build a market stall | 152 | 67 | - | - | 200 | 141 | 121 | - | - | - | 67 | 141 | 200 |

