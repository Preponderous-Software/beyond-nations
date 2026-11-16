# Playtest summary: 20261115-1734-rerun

4 sessions, build beyond-nations 0.7.1-SNAPSHOT (2f10aca1), generated 2026-11-15 18:02.

**Findings:** 34 reports in 31 groups -- 0 blocker, 6 major, 18 minor, 10 polish.

## Outcomes

| Session | Goal | Outcome | Fun | Minutes | Objectives | Reached | Deaths | Findings | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| s01 | Play the first 10 minutes as a brand-new player who has never seen the game; note every moment of confusion. | achieved | 4 | 14.4 | 6 | - | 0 | 9 | The first ten minutes are well guided: the objective box, tips and the helpful 'too close, open ground lies N paces north-west' message got me from nothing to a Village with two stalls in under four game minutes, and the world feels alive (rival raids, bandit event, newborns). Confusion came from the settlement sheet (what is paid from pack vs stockpile), the objective and the Era panel disagreeing on what the Village era needs, and loyalty tiers ('Stranger') being read as membership. Energy/food is trivially easy: apples pile up (27) and energy never dropped below 82. Night is too dark to play. |
| s02 | Reach the Village era as fast as you can. It can now come from day 2 (about 7 minutes of game time) once your nation has 10 members; use courting/recruiting and housing. Record when each Village requirement was met, what held you back, and whether the wait felt meaningful. | achieved | 3 | 20.5 | 7 | Village | 0 | 5 | Village era came at 537 s of game time (day 2 started at about 420 s). Members was the only thing holding it back: settlement tier and stalls were done by 257 s. The wait was mostly passive, though: 9 of 10 members drifted in through housing, and courting a stranger took 4 talks and 4 gifts with no visible progress, so recruiting felt like guesswork. Making recruiting visible (a relationship meter) and keeping settlement tiers from reusing era names would make this goal read much more clearly. |
| s03 | Survive the first winter as a prepared ruler. Watch for the winter warning and the Season box food count, store food before winter (trees give NO apples in winter now), use Feed Residents and the Eat button. Record how many of your members die, whether the warning was clear and early enough, and whether preparing visibly helped. | partial | 3 | 26.5 | 8 | Village | 1 | 10 | The winter loop reads well: 'Winter in 1 day!', the Season box 'Food: X of Y for winter' gauge, and hunger banners that say who is hungry and where are clear, and Feed Residents does what it says. But preparing only bought about half a winter day: stores went from 32 of 20 to 0 of 44 by the end of Winter day 1 as members kept joining and eating, the market ran dry, and the player had nothing left to do. No member had died by Winter day 2 night, but 10 were hungry and 3 starving. Separately, an --advance-ticks start spawns the player starving and old, so they die of hunger in 10 s and of old age about 1 game day later. |
| s05 | Trade: use the Trade at Market panel to buy and sell with quantities (check the profit/loss lines and receipts), appoint a Trader from your nation and watch whether traders actually carry goods between markets, build a Storehouse and send a caravan. Judge whether selling pays and whether prices make sense over time. | partial | 3 | 22.4 | 8 | Village | 0 | 10 | The Trade at Market panel works: quantity buttons, slippage-aware totals, till limits and a clear receipt make buying and selling understandable, and selling raw goods does pay (16-24 a unit), though home tills run dry after a few units. The caravan loop (Storehouse -> Send Caravan -> sold -> came home with 228 coins) is satisfying, but goods the destination won't buy silently disappear, and panels that jump as pawns walk past cause misclicks. Traders are a black box: no appoint button with a reason, no events, and the one self-appointed trader never visibly left home. |

## Findings

### 1. [major] --advance-ticks start: player spawns at Energy ~1 and dies of hunger ~10 s in, then dies of old age ~1 game day later

- **Category:** bug; **reported by** 1 agent(s): s03
- **Detail:** Entering the world at tick 145000 the player had Energy 1.1 and a 'Starving!' banner; player_death at tick 145496 before reaching the nearest tree (10.9m). 'The founder is growing old' fired at tick 148501, ~70 s into play, and the founder died of old age at tick 178500 (~11 game minutes of actual play). The succession screen reports 'a reign of 5 days and 22 hours' (ticks since world tick 0), though the nation was founded around tick 148300. The pre-advance appears to age and starve the player character, so the player's own life ends almost immediately. Expected the player to spawn fresh (full energy, young) at the end of the pre-advance, and reign length to count from founding.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 103 --season autumn --advance-ticks 25000; Start, Begin; pt state shows energy ~1; pt events shows player_death ~500 ticks later; keep playing ~33000 ticks -> 'You died of old age'
- **Screenshots:** [start_starving.png](s03/shots/start_starving.png)

### 2. [major] Caravan cargo the destination does not buy (15 saplings, 1 stone) vanishes; homecoming message only mentions coins

- **Category:** bug; **reported by** 1 agent(s): s05
- **Detail:** Send Caravan panel: loaded 5 wood, 10 stone, 15 saplings to the Village of Eastton (rows said saplings 'here 8, there 18'), estimate 'worth about 228 coins'. Chronicle: 'Your caravan sold 5 wood, 9 stone at the Village of Eastton for 228 coins', then 'came home ... with 228 coins for the treasury'. The 15 saplings and 1 stone never came back: not in inventory, and Collect Stockpile returned only wood/stone, then 'The stockpile is empty.' Expected unsold goods returned (the code comment says onto the stockpile) and the homecoming line to say so. The estimate also silently valued the saplings at 0, which contradicts the 'there 18' row.
- **Repro:** pt start --seed 105; reach Village era, build Storehouse inside your settlement; Send Caravan; pick Eastton (4/9); +5 wood, +5x2 stone, +5x3 saplings; Send; wait ~6000 ticks; read chronicle; Collect Stockpile
- **Screenshots:** [home.png](s05/shots/home.png)

### 3. [major] Command bar and Send Caravan panel reflow when pawns walk in/out of range, so clicks land on the wrong button

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** Inside a settlement the command bar adds/removes the Order/Talk/Gift/Invite row for the nearest pawn as residents walk around. Buttons shift under the pointer: a click aimed at 'Trade at Market' ate an apple ('Ate an apple (+15 energy)'), a click on 'Settlement' hovered 'Collect Stockpile'. The Send Caravan panel is anchored to the bar and jumps ~34 px up/down between clicks (and grows when the estimate line appears), so '+5 Saplings' presses hit Wood or nothing and '<' missed. At a few fps this happens on most multi-click sequences.
- **Repro:** Enter your settlement with several residents milling around; open Send Caravan; take screenshots between +5 clicks and watch the panel move
- **Screenshots:** [loaded.png](s05/shots/loaded.png)

### 4. [major] Members keep joining and being born mid-winter, raising the food target during a famine

- **Category:** balance; **reported by** 1 agent(s): s03
- **Detail:** During winter, Grace Thompson was born (tick 171002), Dave Robinson, Charlotte Walker and Rachel Evans joined/swore fealty; residents went 8/12 -> 12/12 and the winter target 28 -> 44 while food was 0. A ruler has no visible way to stop recruitment when the village cannot feed itself. Expected immigration/births to pause or slow when stores are short, or a control to close the gates.
- **Repro:** As above; watch chronicle 'joined Northernton' / 'welcomed a newborn' lines while Season box shows Food 0 of N
- **Screenshots:** [food_collapse.png](s03/shots/food_collapse.png)

### 5. [major] Trader role is invisible: no Appoint Trader button or reason, and the self-made trader never visibly trades

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** Charlotte Evans appeared as 'Trader' in O > Members without any status/chronicle line saying she took up trade. Because the nation already had one trader per settlement, no 'Appoint ... Trader' button appears on any serf and nothing explains why. Over ticks ~19000-35000 (night and the next morning) she was always within ~10 m of home, and no event or chronicle line ever mentions a trader buying or selling. Nothing on screen shows what a trader carries or where it is heading, so I could not judge whether traders move goods between markets.
- **Repro:** pt start --seed 105; found a settlement and let members join; O > Members; walk up to a serf member and look for Appoint Trader on the command bar; track the trader with pt state nearest pawns
- **Screenshots:** [trader_idle.png](s05/shots/trader_idle.png)

### 6. [major] Winter food stores collapse within one winter day despite preparation; nothing left for the player to do

- **Category:** balance; **reported by** 1 agent(s): s03
- **Detail:** I entered winter over-prepared: Season box 'Food: 30-32 of 20', all residents stocked via Feed Residents, ~30 apples carried. By Winter day 1 midday it was 'Food: 22 of 36', by afternoon 'Food: 4 of 40', by dusk 'Food: 0 of 44', and at Winter day 2/2 night the banner read '10 members hungry, 3 starving'. Fed in total ~45 apples, 7 chicken meat and every apple the market would sell (5 apples cost 150 coins, then price rose to 54-80 and 'No one is selling food'). Trees are bare, the nearest chickens were 1700m away after I hunted the close ones. Preparing visibly helped for the first ~half day only; after that, demand outran any food a player can obtain. I expected stocking ~150% of the stated target before winter to carry the village through, or at least for the target to stay fixed.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 103 --season autumn --advance-ticks 25000; create nation (N), found settlement (F), gather ~50 apples, Feed Residents until 0 hungry before winter; pt wait in the settlement through Winter day 1 and watch the Season box
- **Screenshots:** [famine.png](s03/shots/famine.png)

### 7. [minor] Objective 7 'Reach the Village era' progress differs from the Era panel's criteria, and its text is clipped

- **Category:** text, ux; **reported by** 4 agent(s): s01, s02, s03, s05
- **Also reported as:** "Objective progress bar text clipped on the left"; "Objective progress text clipped on the left ('nbers 5/10')"; "Objective progress text clipped: 'bers 5/10 · Buildings 0/2'"
- **Detail:** Objective shows 'Members 5/10 • Buildings 2/2' with the left edge clipped ('mbers 5/10'). The Era detail panel lists Village 1/1, Members 5/10, Stalls 1/2, Day 1/2 - the objective omits stalls and the day requirement, so a player following only the objective does not know to build a second stall or wait a day. 'Village: 1/4' next to 'Era: Camp' also clashes with my settlement already being called 'Village of Easttown'. Era panel text is cut off at the bottom.
- **Repro:** continue from above; build a stall (B in settlement); exit; tap the Era box top-left
- **Screenshots:** [era.png](s01/shots/era.png) [objective_clipped.png](s02/shots/objective_clipped.png) [objective_clipped.png](s03/shots/objective_clipped.png) [objective_clipped.png](s05/shots/objective_clipped.png)

### 8. [minor] Build House button shows no cost on the settlement screen

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** Inside the camp the Build House button is greyed out and hovering gives no tooltip; the Buildings section only lists 'Next: Farm, 25 wood, 10 stone'. A player planning for the Village era (which depends on houses for members) cannot tell what a House needs. It later turned out ~20 wood / 15 stone.
- **Repro:** pt start --seed 102; found settlement; press e to enter; pt move 480 352
- **Screenshots:** [house_no_cost.png](s02/shots/house_no_cost.png)

### 9. [minor] Courting a Stranger shows no relationship progress toward Acquaintance

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** Pressing R on Yvonne Thomas said 'I don't even know you. (needs Acquaintance)'. Talking once and gifting 10 coins changed nothing visible; it took 4 talks + 4 gifts (40 coins) before R worked ('Yvonne Thomas joined Westham! Members: 7'). The pawn label stayed '(Stranger)' throughout with no meter or hint how close Acquaintance is, while another pawn (Xavier, already a member) became Acquaintance after one talk. Expected a visible relationship bar or 'x more talks' hint.
- **Repro:** pt start --seed 102; found a settlement; sprint away; pt walkto pawn 120; pt key r; then click Talk (161,445) and Gift (409,445) repeatedly, pressing r after each round
- **Screenshots:** [court.png](s02/shots/court.png)

### 10. [minor] Duplicate settlement names in caravan destinations and chronicle

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** The caravan destination list shows 'Village of Lowerfield' as both 1/9 and 2/9, told apart only by distance, and the chronicle reads 'A caravan from Lowerfield sold 20 wood at the Village of Lowerfield', which looks like a caravan selling at home.
- **Repro:** Send Caravan; step through destinations with >
- **Screenshots:** [dest2.png](s05/shots/dest2.png)

### 11. [minor] E still interacts ("Gathered 0 wood from tree") while a world-event sheet pauses the world

- **Category:** bug; **reported by** 1 agent(s): s02
- **Detail:** With the 'A bountiful harvest' world event open (tps 0, 'The world waits while you decide'), pressing E beside a tree printed 'Gathered 0 wood from tree. (x6)'. Expected input to be swallowed by the sheet. Also the objective read 'Wood 16/20' while the inventory held 20 wood. A player chopping in a rhythm can lose time not noticing the event because the status line keeps updating.
- **Repro:** pt start --seed 102; gather with walkto tree / key e until a world event appears (~tick 9800); press e next to a tree
- **Screenshots:** [event_e_zero.png](s02/shots/event_e_zero.png)

### 12. [minor] Home market tills run dry after a handful of units; prices swing hard with merchant cash

- **Category:** balance; **reported by** 1 agent(s): s05
- **Detail:** After selling 6 apples and 3 wood the tills held 10 coins and the panel said 'No till here can pay for one' / 'Can't sell' while I carried 62 wood and 58 stone. Selling pays per unit (apples 16-25, wood 24) but the volume per visit is tiny. Apple sell price went 22 -> 16 -> 25 -> 16 within about a minute of game time, following the till; wood stayed at exactly 24/24 (zero spread) throughout; stone fell from 26/22 to 15/12. Round trip buy 6 apples for 141 / sell 6 for 117 lost 24 (~17%), which is sane. The 'tills can pay for N' feedback is good.
- **Repro:** Trade at Market; sell apples x6, wood x3; watch prices and the till line
- **Screenshots:** [wood2.png](s05/shots/wood2.png)

### 13. [minor] Market panel advertises apple prices when no food can be bought

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** Market Info shows 'Apples 67/32', 'Apples 75/30', 'Apples 80/24' while Purchase Food answers 'No one is selling food at this market or you do not have enough money to purchase food' (I had 104-191 coins). The message does not say which of the two reasons applies. Expected the stock (0) to be shown or the button to be disabled with a clear reason.
- **Repro:** In winter with food scarce, inside own village click Purchase Food repeatedly until it fails; compare Market Info
- **Screenshots:** [famine.png](s03/shots/famine.png)

### 14. [minor] Market receipt / net profit line hidden in a two-line scroll box

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** After a sale the 'This visit: spent X, took Y, net Z' receipt is below the fold of a tiny scroll area under the Buy/Sell buttons; it only shows after scrolling. The receipt itself is clear and correct (spent 141, took 104, net -37).
- **Repro:** Trade at Market; sell 6 apples; look under the Buy/Sell buttons
- **Screenshots:** [receipt.png](s05/shots/receipt.png)

### 15. [minor] Night is near-black: world unreadable, can't find trees, rocks or pawns

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Around tick 17000 (about 6 game minutes in) night falls and the 3D world turns almost completely dark; only the HUD is visible. In my first 10 minutes I had to recruit members at night and could not see anyone to walk to. Expected: moonlight/ambient floor, or lit pawns/torches.
- **Repro:** play ~6 game minutes on seed 101; walk away from the settlement
- **Screenshots:** [quinn.png](s01/shots/quinn.png)

### 16. [minor] No per-member food view: cannot see who is short of winter stores

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** O > Members lists role and loyalty only. With warnings like '4 are short' and 'outside the walls', there is no place to see which members are short or where they are.
- **Repro:** Press O, click Members during the winter warning
- **Screenshots:** [members_no_food.png](s03/shots/members_no_food.png)

### 17. [minor] Objective 2 'Eat something' completes itself via an automatic apple; player never sees it

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** The first chop auto-ate an apple ('Ate an apple (+15 energy)') and objective 2 completed 52 ticks after objective 1 with no input. The tutorial step that teaches eating is skipped, while an 'Eat Apple (+15)' button implies eating is manual. Related: chop status 'Gathered 4 wood and 1 apple from tree.' omits the saplings received (Saplings 1 -> 3).
- **Repro:** pt start --seed 101; chop trees to 10 wood; watch objective 2
- **Screenshots:** [obj2_auto.png](s01/shots/obj2_auto.png)

### 18. [minor] Own nation members are labelled '(Stranger)' and refuse to 'follow' you

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Abigail Brown, announced in the chronicle as having joined Easttown and shown as my heir and merchant, appears as 'Talk to ... (Stranger)' and says 'I hardly know you. Why should I follow you?'. 'Stranger' is a loyalty tier but reads as 'not a member'; the world HUD never tells me who is already in my nation (only the O > Members tab does). Also clicking 'Talk to Karl Watson' talked to Abigail instead: the action target switched as pawns moved between hover and click.
- **Repro:** after founding, stand at your settlement among members; click 'Talk to <name> (Stranger)'
- **Screenshots:** [talk2.png](s01/shots/talk2.png)

### 19. [minor] Settlement sheet says 'Build here with what you carry' but House/Farm are paid from the stockpile

- **Category:** text; **reported by** 1 agent(s): s01
- **Detail:** Entering my new camp, objective 6 asked me to carry 20 wood + 5 stone for a stall, and Build Stall was greyed with no cost or tooltip. Meanwhile Build House and Build Farm were enabled and built instantly while I carried 0 wood/0 stone; the stockpile went 45 wood/25 stone -> empty. The heading 'Build here with what you carry:' is wrong for two of the three buttons, and it is unclear what pays for what. Expected: costs on each button and correct wording (e.g. 'Stall: 20 wood, 5 stone from your pack; House/Farm: from the stockpile').
- **Repro:** pt start --seed 101; gather 50 wood; N; F away from settlements; E to enter; hover Build Stall (no tooltip); click Build House with empty pack
- **Screenshots:** [farm.png](s01/shots/farm.png)

### 20. [minor] Settlement tiers reuse era names (Village, Town) and confuse the Village-era checklist

- **Category:** text; **reported by** 1 agent(s): s02
- **Detail:** The Camp-era checklist item 'Village 0/1' means 'have a settlement at Village tier', which isn't explained; it only ticked after building a House + Farm. Two more houses later the settlement screen announced 'The settlement is now a Town!' / 'TOWN OF WESTHAM' while the nation was still in the Camp era. Also the objective bar says 'Buildings 2/2' where the era panel says 'Stalls 2/2', and the build row says 'Build here with what you carry' while houses were paid from the stockpile with an empty inventory.
- **Repro:** pt start --seed 102; found settlement; enter; build 2 stalls, house, farm, then 2 more houses
- **Screenshots:** [town_vs_era.png](s02/shots/town_vs_era.png)

### 21. [minor] Trade at Market advice line recommends a worse market: 'Best price in reach: 21 ... Sell there, not here: -1 a unit'

- **Category:** bug; **reported by** 1 agent(s): s05
- **Detail:** With apples selling at 22 here, the panel said 'Best price in reach: 21 at the Village of Bigwood. Sell there, not here: -1 a unit.' Selling there pays less, so the advice is backwards. Later the line shows 'Buy here, sell there: -5 a unit (a loss)' in orange as the 'best' route, which is also odd to show as advice.
- **Repro:** Inside your settlement with a stall, Trade at Market, select Apples (tick ~13400)
- **Screenshots:** [sell_there_worse.png](s05/shots/sell_there_worse.png)

### 22. [minor] Travelling merchant event says axe+pickaxe are 'worth hundreds at any market' but tools cannot be sold

- **Category:** text; **reported by** 1 agent(s): s05
- **Detail:** The event offers 'an axe and a pickaxe for 50 coins, worth hundreds at any market'. The Trade at Market panel lists only Apples/Wood/Stone/Saplings; tools cannot be sold. The event also says the merchant 'hails you on the road' while I was inside my village.
- **Repro:** Wait for the 'A travelling merchant' world event (tick ~30013 on seed 105), buy the tools, open Trade at Market
- **Screenshots:** [merchant_ev.png](s05/shots/merchant_ev.png)

### 23. [minor] Winter warning arrives only ~half a day before winter and only after founding

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** Season box shows 'Winter in 1 day!' from the start, but the food count and the winter warning banner only appear once you have a settlement; with an autumn start that left about 6.5 game minutes (~20000 ticks). The warning itself was clear and stayed on screen, and the Season box 'Food: X of Y for winter' line is a good, glanceable gauge. But it understated need: 32 of 20 at the start of winter was not nearly enough.
- **Repro:** Start with --season autumn; found settlement; read banner and Season box
- **Screenshots:** [winter_warning_ambiguous.png](s03/shots/winter_warning_ambiguous.png)

### 24. [minor] Winter warning says '1 is short ... Feed Residents' while Feed Residents is disabled ('0 hungry')

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** Before winter the banner read 'Northernton's 4 members carry 25 food of the 16 they want for winter; 1 is short. Bring food home and Feed Residents.' Inside the settlement the Feed Residents button was grey '(0 hungry)' with tooltip 'No one here is hungry or short of winter stores.' The short member was presumably away, but the warning does not say so (later hunger banners do: '1 here, 1 outside the walls' - good). '1 is short' next to '25 of 16' is also confusing at first read (it means one member, not one food).
- **Repro:** Found settlement near end of Autumn day 2/2; enter it while banner says 'N is short'; hover Feed Residents
- **Screenshots:** [feed_disabled_but_short.png](s03/shots/feed_disabled_but_short.png)

### 25. [polish] Controls help lists debug cheats (F2-F6 spawn coins/wood/pawns) to ordinary players

- **Category:** text; **reported by** 1 agent(s): s01
- **Detail:** Page 3 of the in-game Controls lists F4 Spawn coins, F5 Spawn wood, etc. with no warning. A new player can trivialise the economy by accident.
- **Repro:** Main menu > Controls > Next > Next
- **Screenshots:** [controls_debug.png](s01/shots/controls_debug.png)

### 26. [polish] Day numbering differs between chronicle and Season box

- **Category:** text; **reported by** 1 agent(s): s03
- **Detail:** The chronicle labels events 'Day 6' while the probe says day 5 and the Season box says 'Autumn - day 2/2'. Three different day counts on one screen.
- **Repro:** Look at chronicle and Season box at the same moment
- **Screenshots:** [t2.png](s03/shots/t2.png)

### 27. [polish] Depleted rocks stay in the world and give 'Gathered 0 stone from rock.'

- **Category:** bug; **reported by** 1 agent(s): s01
- **Detail:** Trees disappear once chopped, but a rock I had already mined stayed visible and pressing E gave 'Gathered 0 stone from rock.'. Expected the rock to vanish or look depleted.
- **Repro:** mine a rock with E (5 stone); press E at it again
- **Screenshots:** [stuck.png](s01/shots/stuck.png)

### 28. [polish] HUD overlaps inside a settlement: Inventory panel over the Chronicle, nameplates over text

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** The Inventory Info panel's title sits on top of the chronicle lines, pawn nameplates ('Samantha Lewis / Greedy, Ambitious') draw over the chronicle, and the big '7 here, 11/12 residents' world text bleeds through behind the top of the HUD.
- **Repro:** Enter your settlement with Stay Inside and look at the top-middle of the screen
- **Screenshots:** [bar2.png](s05/shots/bar2.png)

### 29. [polish] Missing dash glyph in world event option text

- **Category:** text; **reported by** 1 agent(s): s03
- **Detail:** 'A claimant stirs' option 3 reads 'Nothing now    but if the claimant wins anyway, they will remember.' The dash between the clauses renders as blank space.
- **Repro:** Wait for the 'A claimant stirs' world event
- **Screenshots:** [claimant_dash.png](s03/shots/claimant_dash.png)

### 30. [polish] N creates a nation instantly with an auto-name; own announcement truncated

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Pressing N made 'Easttown' with no chance to name it or confirm. The chronicle line announcing it is truncated: 'You raised a banner and named your people Ea...', so the name is cut off at the moment it is announced.
- **Repro:** pt start --seed 101; press N
- **Screenshots:** [nation_name.png](s01/shots/nation_name.png)

### 31. [polish] World-space labels (pawn names, settlement '0 here, 3/8 residents') render huge and clipped over the HUD

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Near the settlement its label fills the top of the screen and is cut off, and pawn labels such as 'Abigail Br... Greedy (gathering' cover the Season panel. The label also bleeds through the settlement and nations sheets behind their titles. A tree trunk also stood in the middle of my freshly founded settlement, blocking the camera.
- **Repro:** found a settlement; stand at its centre; walk close to a pawn
- **Screenshots:** [found3.png](s01/shots/found3.png)

## Objective timings

Game seconds from the start of the world to each objective (event tick / the game's ticks per second, from events.jsonl). Game time, not wall time: with many sessions on one machine a world can run slower than real time, and wall-clock timings would mostly measure the machine.

| # | Objective | s01 | s02 | s03 | s05 | min | median | max |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Gather wood | 42 | 37 | 2927 | 23.1 | 23.1 | 39.5 | 2927 |
| 2 | Eat something | 43.1 | 38 | 3000 | 24 | 24 | 40.5 | 3000 |
| 3 | Create or join a nation | 50 | 39 | 3001 | 95 | 39 | 72.5 | 3001 |
| 4 | Found a settlement | 153.1 | 106 | 3021.1 | 163 | 106 | 158.1 | 3021.1 |
| 5 | Enter your settlement | 159.3 | 112.5 | 3028.1 | 165.6 | 112.5 | 162.4 | 3028.1 |
| 6 | Build a market stall | 216 | 258.1 | 3131 | 259 | 216 | 258.6 | 3131 |
| 7 | Reach the Village era | - | 537 | 3684.1 | 420.1 | 420.1 | 537 | 3684.1 |
| 8 | Sell resources at market | - | - | 3685 | 421 | 421 | 2053 | 3685 |

