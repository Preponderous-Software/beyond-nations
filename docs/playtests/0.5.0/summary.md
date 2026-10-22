# Playtest summary: 20261021-2257

10 sessions, build 0.4.1 (df53ae3), generated 2026-10-22 06:24.

**Findings:** 97 reports in 85 groups -- 0 blocker, 20 major, 53 minor, 24 polish.

## Outcomes

| Session | Goal | Outcome | Fun | Minutes | Objectives | Era | Deaths | Findings | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| s01 | Play the first 10 minutes as a brand-new player who has never seen the game; note every moment of confusion. | partial | 3 | 18.7 | 6 | Camp | 0 | 14 | The first 3 game minutes work well: the tip and objective chain (chop, nation, settle) is clear and fast, and tools and crafting are good. After that a newcomer runs into problems. Founding needs a 400m trek with no guidance, the pawns around your own settlement can't be talked to (which blocks the members objective), and a crowded NPC world raids you, turns hostile and floods the chronicle without explaining anything. One shifting button turned a harmless click into a raid. The biggest win would be fixing the pawn interaction bug, plus a clear 'why' for relations and raids. |
| s02 | Reach the Village era as fast as you can (it cannot come before day 3, about 17 minutes of game time); record the game time of every objective. | achieved | 2 | 35.3 | 7 | Village | 0 | 8 | The Village era came at 1020 s, exactly when the day-3 gate opened, but the run was over in practice at about 180 s: pawns join and NPCs build the stalls and buildings by themselves, so the player only chops about 13 trees, presses N and F, then idles for 14 minutes. The opening (chop, nation, find unclaimed land, found) feels good. After that the player needs something to do, or the gate should line up with what the player actually achieves. |
| s03 | Survive the first winter. Start with --season autumn (pt start --seed 103 --season autumn). Note how food and cold feel, and record how many pawns die over the winter (pt events) and how food per pawn feels. | partial | 2 | 49.8 | 12 | Camp | 0 | 9 | Winter is lopsided: the player shrugs it off (trees still drop apples, energy never fell below 90), while 50 NPCs starved inside their own walls with no warning and no way for the player to feed them, even as a leader with apples in hand. That makes winter something that happens to the world, not something the player plays through. Starving pawns need a visible warning and a way to give, sell or store food, and their autumn stocking needs checking from a real autumn start (I had to use --season winter because autumn-to-winter took ~70 wall minutes at the tick rate I got). |
| s04 | Diplomacy and war: lead a nation, meet a neighbour, try alliance, rivalry and war from the Nations screen (O). | partial | 3 | 19 | 6 | Camp | 0 | 10 | The diplomacy actions read well: every refusal names its reason and the odds ('laughs... 28 to your 2', 'tribute may buy a truce'), and the world comes alive with sieges and captures. But the Nations screen shows only the 8 oldest of ~20 nations with no scrolling, so the neighbours that matter (whose borders grate into rivalry and who raid you) usually can't be warred on or made peace with, and the in-land command bar has no War or Peace to make up for it. War also costs nothing and can be ended in the same paused tick, so it has no weight yet. |
| s05 | Trade and caravans: build a market stall, buy and sell, get a Storehouse and send a caravan. | partial | 2 | 20.8 | 6 | Camp | 0 | 10 | Building a stall is quick and well signposted (157 game seconds), but the trade loop past that point is weak. Selling is one unit per click, with no receipt and one ambiguous error for every failure, so earning 51 coins took a lot of guessing. The Storehouse and caravans sit behind the Town era, which no one reaches in a session, and nothing in the game mentions them. No pawn caravan appeared on the road in 26k ticks. The travelling-merchant event was the clearest and most satisfying trade moment of the run. |
| s06 | Ruler, laws and heirs: lead a nation, enact and repeal laws, and see what happens when the ruler dies. | partial | 3 | 20.6 | 6 | Camp | 0 | 10 | The laws system works well. Changes are proclaimed with a clear 30 s countdown, take effect with chronicle and status messages, can be withdrawn, and have visible results (Defence 6 from 2 conscripts, tax totals, thieves through open borders). The ruler/heir half of the goal can't be reached, though. The founder doesn't come close to starving in a normal session because apples are auto-eaten. No screen shows your members or your heir. You also can't befriend your own members while they're at home, so the succession rules can't be seen or influenced before a death that never comes. |
| s07 | Crafting: find and use every recipe on the crafting screen (C); are tools worth making? | partial | 3 | 17.6 | 6 | Camp | 0 | 6 | Tools are worth making, and the loop around them works. The crafting screen is clear (cost, benefit, 'Need 2 more wood', 'have 1 (8 uses left)'). The axe (6 resources for +60 wood) and pickaxe (6 for +30 stone, and stone is the scarcer resource) pay for themselves within a tree or two, and wear-out to a spare reads well. The rest of the screen falls short: Cooked Meat gets auto-eaten silently at about 90 energy with apples everywhere, and Planks/Bricks sit behind a Village-era Workshop that the crafting screen wrongly says K or the command bar will build. Three of the five recipes are of no real use to the player in the first hour. |
| s08 | World events: play until world events are offered, take different choices, and judge whether the consequences are clear and fair. | partial | 3 | 21.8 | 6 | Camp | 0 | 8 | The world event sheet itself works well: it is readable, pauses the world, names the nations involved, prices each option, and the outcome lands at once in the status line and the chronicle. The weak part is the consequence: the benefit of a diplomatic choice is never shown in numbers before or after, so after paying 20 coins Newport still read Hostile (-86) and I could not tell what I had bought. Events also come rarely enough (the first at ~10 game minutes), and the save slot is single enough, that comparing choices in one session was not possible. |
| s09 | Accessibility and text audit, touching only what a mouse-and-keyboard player would: every screen and panel; cut-off, overlapping, unclear or misspelled text; contrast; anything only explained on hover. Also check whether the Season panel (bottom right) is clipped at the right edge, and whether the "+N wood" floating text is drawn under the "Objective complete" toast. | achieved | 3 | 15.8 | 6 | Camp | 0 | 16 | The opaque HUD boxes and the Controls screen read well, but translucent panels, bottom buttons and progress bars fall to about 2:1 contrast on bright grass or sand, and the right column overflows once you enter a settlement: Season covers Player Info and a Settlement Info line is cut off. The Season panel is not clipped at the right edge. Floating gain text is drawn under all HUD windows, the toast included. Much of the systems text (laws, diplomacy, config) is unexplained and has no hover help. |
| s10 | Save/load and share-seed round trip: save from the pause menu, stop, `pt start --continue`, confirm the world is identical; then start a new world from the seed shown on the Share screen and confirm it matches. | achieved | 3 | 8.4 | 3 | Camp | 0 | 6 | Save/load works: after Save and a hard stop, `--continue` (tested twice) restored the tick, position, facing, inventory, coins, nation/role, objective progress, chronicle and entity IDs exactly. The UI is clear too ('Saved: Day 1 - Farmham, 23:02', 'Welcome back', 'Last played' on the main menu). Typing the Share screen's seed, or the word Ironvale, gives an identical starting world, and idle runs replay the same history. The real problem is that pawns strip every tree within about 165 m of spawn in the first 40 seconds, so a new player starting objective 1 faces a long walk. A smaller one: the Share screen drops the word seed in favour of a 10-digit number. |

## Findings

### 1. [major] Founding a settlement: 'Too close to another settlement' gives no distance; first legal spot was ~400m from spawn

- **Category:** bug, ux; **reported by** 7 agent(s): s01, s04, s05, s06, s07, s08, s09
- **Also reported as:** "'Too close to another settlement' gives no direction or distance"; "'Too close to another settlement' gives no distance or direction while AI settles the land around you"; "'Too close to another settlement.' gives no distance or direction"; "'Too close to another settlement.' gives no minimum distance or direction"; "'Too close to another settlement.' gives no required distance or direction"; "Stale world status ('Too close to another settlement.') shown on Crafting, Nations and Laws screens"
- **Detail:** By game-minute 2, more than 8 NPC settlements surround spawn, and every settlement needs 200 units of clearance. F says 'This land is claimed by Eastham...' (no borders are drawn) and then 'Too close to another settlement.' with the nearest one 150-160m away and no hint of the 200-unit rule or which way to go. Objective 4 took 6000 ticks (120 game s), about half of it spent wandering. The success message ('Settlement founded...') was overwritten 8 ticks later by 'You are back on your own land.'
- **Repro:** pt start --seed 101; gather 50 wood near spawn; pt key n; walk out of Eastham land; pt key f -> 'Too close to another settlement.'
- **Screenshots:** [founded.png](s01/shots/founded.png) [too_close.png](s04/shots/too_close.png) [too_close.png](s05/shots/too_close.png) [settlement_founded.png](s06/shots/settlement_founded.png) [too_close.png](s07/shots/too_close.png) [tooclose.png](s08/shots/tooclose.png) [crafting.png](s09/shots/crafting.png)

### 2. [major] Context button bar re-laid out under the cursor: clicking 'Enter Settlement' raided a caravan instead

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** The bottom bar showed [Leave Nation][Teleport Home][Plant Sapling][Enter Settlement]. About 1-2 s later I clicked Enter Settlement at (638,516) and got 'You raided a Newham caravan and took 10 stone. Newham will not forget it.' A caravan had come into range and the buttons shifted, which put a hostile, irreversible diplomatic action where a harmless one had been, with no confirmation. Hostile actions (raid, war, denounce) need a fixed slot or a confirm step.
- **Repro:** pt start --seed 101; found settlement; pt key h; pt shot; pt click 638 516 while a Newham caravan passes -> raid status
- **Screenshots:** [house2.png](s01/shots/house2.png)

### 3. [major] Cooked Meat is auto-eaten silently at ~90 energy, wasting most of its 45; the recipe is pointless

- **Category:** balance; **reported by** 1 agent(s): s07
- **Detail:** I crafted 1 cooked meat at tick 14994 while holding 30 apples. By about tick 15750 it was gone (energy about 90 jumped to 98.8, so roughly 35 of the 45 energy was wasted at the cap). No status message, no chronicle line, and the inventory screen has no Eat action. The first cooked meat (tick 4634) and a chicken meat vanished the same silent way. Auto-eat takes the most nourishing food first (FoodItems.foodInDescendingEnergyOrder) and fires near 90 energy, so a 45-energy item is never worth more than an apple. Energy drains slowly (about 1 per 250 ticks) and trees drop 1-3 apples each (I had 42 apples by the end), so food crafting has no purpose. Expected: eat the least nourishing food that fills the gap (or wait until energy is lower), show 'Ate cooked meat (+45)', or give a manual Eat action.
- **Repro:** pt start --seed 107; walkto chicken; e; C, click the Cooked Meat row; close; wait about 15 s game time while watching inventory/energy in pt state
- **Screenshots:** [no_eat_option.png](s07/shots/no_eat_option.png)

### 4. [major] Day-1 diplomacy and raids happen to the player unexplained; 'Sheltered' message contradicts raids

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Without meeting anyone, by the end of day 1 I was Rival with Eastshire, Hostile with Newland and Farmville, and Friendly with others (Nations screen: Eastshire 'Hostile (-51)'). The full chronicle shows that Eastshire and Newland each raided Northernville (5 coins, 5 wood, 5 stone each), and no toast told me at the time. Meanwhile the Era panel says 'Sheltered: no siege can begin until the Town era'. My own nation also raided Newland automatically ('Your people raided Newland...'). A newcomer can't tell why any of this happens or how to influence it.
- **Repro:** pt start --seed 101; play to ~tick 16000; pt key o (Nations); pt key y (full chronicle)
- **Screenshots:** [chron_full.png](s01/shots/chron_full.png)

### 5. [major] Every Village requirement except the day gate is met by NPCs within 3 game minutes; the last 14 minutes are pure waiting

- **Category:** balance; **reported by** 1 agent(s): s02
- **Detail:** After I founded the settlement (tick 6950), pawns joined Eastford on their own (Members 10/10) and NPCs built all 4 market stalls between ticks 7350 and 7902. By tick ~9100 (182 s) the checklist read Village 1/1, Members 10/10, Stalls 2/2 and only 'Day 1/3' was left. Nothing I did could speed things up from there, so the run was about 42,000 ticks of idling. Pawns also built the settlement up to 'Town (7 buildings)' with no input from me. Expected the requirements to need some player effort, or the day gate to be shorter.
- **Repro:** pt start --seed 102; chop ~13 trees; N; walk out of claimed land; F; E; then wait and watch the era checklist
- **Screenshots:** [pawns_do_it.png](s02/shots/pawns_do_it.png)

### 6. [major] Inside a settlement the HUD right column overflows: Season panel covers Player Info, Settlement Info line cut off, bottom buttons hidden

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** After entering my settlement (E), the right column stacks Settlement Info + Market Info + Season + Player Info. The Season panel is pushed down to y~432 and drawn over Player Info ('LEADER' and 'Land: your own land' bleed out from behind it) and its last line touches the window bottom. Settlement Info 'Next (K): Farm, 25 wood, 10...' is cut off at the right edge, so the cost of the next building is unreadable. The bottom action-button row runs under the right column: the 6th button shows only 'Se'. Answer to the goal question: the Season panel itself is NOT clipped at the right edge in any layout I saw (x 750-950 at 960 wide, and its lines word-wrap); its problem is vertical overlap with Player Info inside a settlement.
- **Repro:** pt start --seed 109; Start; Begin; gather 50 wood; N; walk >150m from other settlements; F; walkto settlement; e; pt shot
- **Screenshots:** [enter.png](s09/shots/enter.png)

### 7. [major] Market selling is one unit per click with no receipt; apples and saplings never sold

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** Each Sell Resources click at Whiteburg sold ONE unit (1 stone +16, 1 stone +9, 1 wood +26 coins). The status says only 'You sold resources to Abigail Davies. Relationship increased to 2.', never what was sold or for how much; I had to diff my inventory. After 3 sales the market was dry. 31 apples and 14 saplings never sold. Compare the travelling-merchant event, which gives a clear receipt ('bought an axe and a pickaxe ... for 50 coins'). Expected: a quantity choice and a receipt line.
- **Repro:** At Whiteburg's market (4 stalls, about 250 m from spawn on seed 105) click Sell Resources repeatedly; watch coins and inventory
- **Screenshots:** [sold1.png](s05/shots/sold1.png)

### 8. [major] Mass starvation in the first winter: 50 pawns die of hunger 'within the walls', 0 from anything else

- **Category:** balance; **reported by** 1 agent(s): s03
- **Detail:** Started in winter (seed 103) because autumn ran at 12-18 tps and winter was ~50k ticks away (~70 wall minutes). From winter start (tick 180000) the first death came at tick 196859 (Fortham's leader Natalie Watson), then a steady drip: 4 by tick 200549, 42 by 226349, 50 by 230430. All 50 were hunger, every one 'within the walls'. Several nations lost their leaders, and my nation Fortham fell from 5 residents to 1 (just me). Deaths stopped as soon as spring began (~tick 231000). The player meanwhile never got below 90 energy: trees still drop 1-2 apples each in winter. So winter is trivial for the player and wipes out the NPCs. Caveat: a --season winter start gives pawns no autumn to stock up (SeasonalRules wants 4 food each), so a real autumn-to-winter run may be milder. Even so, nothing seen in autumn (no visible stores, a 2-day autumn) suggests pawns would cope.
- **Repro:** pt start --seed 103 --season winter; key space; click 480 315; click 480 315; key j (once a nation exists); wait ~50k ticks; pt events 3000 \| grep pawn_death
- **Screenshots:** [fortham_late.png](s03/shots/fortham_late.png)

### 9. [major] Members of your own nation near your settlement can't be gifted or invited, so the heir can't be influenced

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** Bella Jones, Ruth Walker and Charlie Thompson (Farmfield members) stood 1.4-3.6m from me, up to ~10m from the well. Over 10 tries, G replied 'There is no one within reach to give a gift to.' PawnRelations.findPawnInRange skips pawns flagged as inside a settlement, even though they are drawn standing on the plaza. Succession favours the most loyal member, but I couldn't build loyalty with my own people. The game gives no hint why the pawn in front of you is 'out of reach'.
- **Repro:** after founding a settlement and getting members: key e (exit), stand 10m from the well next to a member pawn, key g
- **Screenshots:** [members_unreachable.png](s06/shots/members_unreachable.png)

### 10. [major] Nations screen (O) shows only the first ~7-8 nations, oldest first, with no scrolling: real neighbours are unreachable

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** By tick 7600 the world had ~20 nations (Easternham, Blueham, Oldtown, Lowerport, Northwood, Castleshire, Farmham, Eastfield, Blueford, Northtown...). The O screen lists only Littlefield..Hamletburg (the 8 oldest); rows past the bottom are dropped silently (NationScreen.cs breaks the loop when out of height). No scroll (mouse wheel, Down, Page Down tried), no 'and N more', no sorting by neighbour. My actual neighbours (Oldtown, Northwood, Redford, Northtown, the last two of which became Rival through border friction) never appeared, so I could not declare war on or make peace with them from O. The doc says the screen 'warns of any siege' and marks neighbours; with this cap neither can be relied on. Only after a conquest wave made Townford a neighbour did a 'neighbour' row show up.
- **Repro:** pt start --seed 104; create nation (N); gather 50 wood; found a camp near (-10,-812); press O
- **Screenshots:** [nations_truncated.png](s04/shots/nations_truncated.png)

### 11. [major] No UI shows who your members are or who the heir would be

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** The Nations tab shows only 'Farmfield, 4 members, 1 camp'. Settlement Info shows residents 4/8 and the merchants' names. Nothing names the members, their loyalty, or the current heir. The goal is about heirs, and the succession rules (family first, then the most loyal, then the longest sworn) can't be seen in game until the ruler is already dead.
- **Repro:** key o (Nations tab) with a nation of 4 members
- **Screenshots:** [nations2.png](s06/shots/nations2.png)

### 12. [major] No warning before pawns starve; the first sign is a death notice

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** The HUD never says a resident is hungry, no screen shows settlement or pawn food stores, and there is no 'X is starving' alert. The Season panel only says 'Winter: no harvests'. The first a player hears is 'Hunger claimed Natalie Watson, leader of Fortham' at tick 196859, and with it a silent succession ('You are now the leader of Fortham.'). Rising apple prices (46 -> 68) are the only indirect clue.
- **Repro:** As above; watch the HUD between tick 180000 and 196859
- **Screenshots:** [deaths_screen.png](s03/shots/deaths_screen.png)

### 13. [major] No way to declare war on, or make peace with, a nation reached only through the in-land command bar

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** Standing in Oldtown's land (a neighbour hidden from the O screen), the command bar offers Gift / Propose Alliance / Denounce only. After two Denounces Oldtown was Hostile (-62) and auto-raids started, but there was no Offer Peace anywhere (not on the bar, not on O). 'Propose Alliance' is still offered to a Hostile nation and just says 'needs +30'.
- **Repro:** From the camp, walk ~250 m toward Oldtown; click 'Denounce Oldtown' twice; look at the command bar
- **Screenshots:** [hostile_bar.png](s04/shots/hostile_bar.png)

### 14. [major] Pawns strip all trees and near rocks around spawn within ~40 game seconds; objective 1 has no tree in sight

- **Category:** balance; **reported by** 1 agent(s): s10
- **Detail:** Seed 1450162717, player idle at spawn (0,0). At tick 1 the nearest trees are 33.6/45.0/61.9 m and the rocks 15.0/30.0 m. By tick ~1770-1977 (35-40 game seconds) the nearest tree is 165 m away and the 15 m and 30 m rocks are gone; the horizon screenshot shows only distant trees. Reproduced identically in two separate fresh worlds. A new player who reads the menus or tips for half a minute starts the first objective with a long walk. Expected: trees near spawn survive the first minute, or regrow / are protected around the player's spawn.
- **Repro:** pt start; pt key space; click Start (480,310); click seed box (480,235); pt type 1450162717; click Begin (480,315); pt await WORLD 60; pt state (trees 33.6 m); pt wait 60; pt state (nearest tree 165 m)
- **Screenshots:** [spawn_stripped.png](s10/shots/spawn_stripped.png)

### 15. [major] Pawns walking around the player's own settlement can't be talked to, gifted or invited (E/G/R say nobody is in reach)

- **Category:** bug; **reported by** 1 agent(s): s01
- **Detail:** Objective 7 says 'Befriend settlers and press R'. With pawns visibly 1.9-4.4m away (Olivia Wood, Wendy Wood, Judy Evans, Bella Wilson, Frank Roberts), E says 'No entities within range to interact with.', R says 'There is no one within reach to invite.', G says 'There is no one within reach to give a gift to.' I reproduced it 8+ times between ticks 13101 and 18257, on the plaza and 20-30m outside it. Hypothesis from the code: Environment.getNearestEntityOfType skips pawns flagged as inside a settlement (the plaza label says '5 here'), but those pawns are still rendered walking around, so they look interactable and aren't. Members stayed at 6/10 for about 3 game minutes.
- **Repro:** pt start --seed 101; play through objective 6 (found a settlement ~400m from spawn); pt key e (exit); pt walkto pawn 30; pt key r  -> 'There is no one within reach to invite.'
- **Screenshots:** [talk1.png](s01/shots/talk1.png)

### 16. [major] Planks/Bricks: crafting screen says build a Workshop with K or the command bar, but it is Village-era locked

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** Inside my 2-building Village-tier settlement with 25 wood / 22 stone (Workshop costs 15/10), the crafting screen reads 'In the Camp of Newburg, which has no Workshop yet (build one with K or the command bar)'. The command bar offers only Build Stall/House/Farm, and Settlement Info says 'Next (K): House'. The only place that explains why is the expanded Era panel: 'Unlocks: Workshop' at the Village era (Members 10, Stalls 2, Day 3). So 2 of the 5 recipes cannot be reached in an early session, and the screen that names them points the player the wrong way. Expected: the Planks/Bricks rows (and the Workshop hint) to say 'Workshop unlocks in the Village era' (EraRules.describeLocked already has that text).
- **Repro:** pt start --seed 107; create nation (N), found settlement (F), build two buildings, gather 15+ wood and 10+ stone, enter settlement, press C; then check the command bar for a Workshop button
- **Screenshots:** [craft_inside.png](s07/shots/craft_inside.png)

### 17. [major] Player can't feed starving residents: gifting is off inside a settlement and Sell Resources refuses apples

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** As Fortham's new leader I held 10 apples while residents starved at the well. G (gift) gave 'There is no one within reach to give a gift to.' on 8 tries. The pawns stand inside the settlement circle, and PawnRelations.findPawnInRange returns null while the player is in a settlement. Outside the circle they walk off before the key lands (at 4-10 fps). After I built a stall, Sell Resources said 'No one is buying resources at this market', even though the apple buy price was 66. Collect Stockpile (452) only gave out wood and stone, and there is no way to put food into a stockpile. Expected: some way to feed your own people in winter.
- **Repro:** pt start --seed 103 --season winter; join a nation; wait for the leader to starve; enter the settlement; key g; click Sell Resources
- **Screenshots:** [no_feed.png](s03/shots/no_feed.png)

### 18. [major] Sell Resources fails with the same ambiguous message whatever the cause (own stall, unowned stalls, empty purses)

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** In my own settlement, carrying 30 apples, 10 wood and 5 stone, with one stall (mine): 'No one is buying resources at this market or you do not have enough resources to sell.' You cannot sell to your own stall, but the message doesn't say so, and Market Info lists 'Merchants: Abigail Thompson', which suggests she would buy. At Whiteburg the panel showed 'Total Coins: 33' with apples selling at 29 and the sale was still refused, apparently because the coins were in unowned for-sale stalls. The button is offered even when no stall can pay.
- **Repro:** pt start --seed 105; N; gather 50 wood; F away from other settlements; E; gather 20 wood + 5 stone; E; B; click Sell Resources
- **Screenshots:** [sell_ambiguous.png](s05/shots/sell_ambiguous.png)

### 19. [major] Storehouse and Send Caravan are gated behind the Town era, which cannot be reached in a session; caravans are never mentioned

- **Category:** balance; **reported by** 1 agent(s): s05
- **Detail:** The Storehouse unlocks only in the Town era (EraRules: day 7, 18 members, 2 settlements, 1,000 funds), and Send Caravan needs a Storehouse. By tick ~26,000 (day 2) I was still in Camp with 2/10 members. No objective, tip, Era panel entry or Controls entry mentions the Storehouse or caravans. The refusal text ('This settlement needs a Storehouse...') only appears inside the dispatch panel, and that panel's button is hidden whenever a refusal exists, so the player never sees it. In about 26k ticks no pawn nation sent a caravan either (they need wood/stone surpluses above 40/30 reserves), so I never met one on the road. Expected: the trade loop to be discoverable early, e.g. a locked 'Send Caravan (needs Storehouse, Town era)' button or an Era-panel line.
- **Repro:** pt start --seed 105; create a nation (N), found a settlement (F), build a stall (B), enter the settlement as leader: no Send Caravan button and no explanation; pt events \| grep -i caravan stays empty through tick 26000
- **Screenshots:** [era_panel.png](s05/shots/era_panel.png)

### 20. [major] The ruler cannot die of hunger in a normal session, so succession is never seen

- **Category:** balance; **reported by** 1 agent(s): s06
- **Detail:** Energy drains about 0.17/game-second, even while sprinting. Gathered apples are eaten automatically: chopping 13 trees gave 25 apples, and energy went back to 99. Energy also seemed to hold steady inside my settlement (90.1 at ticks 14789 and 15733). At tick 24000 (8 game-minutes) energy was 94.5 with 21 apples in the pack. Starving the founder would take 10+ game-minutes of deliberately throwing away food, and the player has no way to discard it. Without the --stage-succession switch (not used, per the rules), the heir screen can't be reached in a 30-minute session. Old age or other causes of death, or a faster hunger curve, would make the dynasty mechanic reachable.
- **Repro:** pt start --seed 106; key space; click 480 315; click 480 315; key n; chop ~10 trees (walkto tree; key e); watch player.energy in pt state
- **Screenshots:** [in_settlement.png](s06/shots/in_settlement.png)

### 21. [minor] Objective progress bar label is clipped: 'nbers 6/10 • Buildings 0/2'

- **Category:** text; **reported by** 5 agent(s): s02, s06, s07, s08, s09
- **Also reported as:** "Objective 7 progress label clipped: 'bers 4/10 • Buildings 0/2'"; "Objective 7 progress label is clipped to 'bers 5/10'"; "Objective 7 progress text is clipped on the left and says 'Buildings' where the checklist says 'Stalls'"; "Objective progress label clipped ('nbers 2/10'), and Sell Resources button overlaps the Season panel"
- **Detail:** For objective 7 'Reach the Village era' the centred label 'Members 6/10 · Buildings 0/2' is wider than the 182px bar, so 'Me' is cut off on the left.
- **Repro:** reach objective 7; look at the Objective box
- **Screenshots:** [rock1.png](s09/shots/rock1.png) [obj_clipped.png](s02/shots/obj_clipped.png) [objective_cutoff.png](s06/shots/objective_cutoff.png) [farm.png](s07/shots/farm.png) [truncated.png](s08/shots/truncated.png)

### 22. [minor] Teleport home (H) lands 9m from the settlement centre, out of enter range; 'Enter Settlement' button still offered

- **Category:** bug, ux; **reported by** 2 agent(s): s01, s02
- **Also reported as:** "After H (Teleport Home), E is out of range of the settlement although 'Enter Settlement' is offered"
- **Detail:** After H, E said 'No entities within range to interact with.' and the 'Enter Settlement' button (shown regardless) did nothing useful. I had to walk closer before E would enter.
- **Repro:** pt key h; pt key e
- **Screenshots:** [house2.png](s01/shots/house2.png) [home_e_range.png](s02/shots/home_e_range.png)

### 23. [minor] 'An alliance needs +30' is misleading: every pawn nation wants +60 from a small player nation

- **Category:** text; **reported by** 1 agent(s): s04
- **Detail:** Header and first refusal say 'An alliance needs +30. Gifts help.' After 75 coins of tribute to reach +30, Littlefield then said 'they are 4 to your 1 and want relations of +60'. As a starting 1-member nation, every pawn nation (all have >=2 members) needs +60 = 150 coins. State the doubled threshold up front, e.g. in the row or the first refusal.
- **Repro:** O; Ally on Littlefield; Tribute x3; Ally again
- **Screenshots:** [ally_60.png](s04/shots/ally_60.png)

### 24. [minor] 'Build a market stall' objective (+50 coins) completes when an NPC builds a stall; chronicle logs it twice

- **Category:** bug; **reported by** 1 agent(s): s06
- **Detail:** Objective 6 showed Wood 3/20, Stone 0/5. At tick 13801 it completed with +50 coins because the pawn Bella Jones built a stall in my settlement (the objective counts any nation stall). 'Trade came to Farmfield: Bella Jones built a stall.' was logged at tick 13801 and again at 13853. The title says 'Build', so free completion and a reward for nothing feels wrong.
- **Repro:** found a settlement with borders Open, enter it, wait ~5 game seconds
- **Screenshots:** [after_laws.png](s06/shots/after_laws.png)

### 25. [minor] 'R' invite says 'There is no one within reach to invite.' while three pawns stand within 2-5 m

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** They are presumably already members, but the message claims nobody is there. Objective hint 'Befriend settlers and press R' doesn't say how to befriend (G gift only in Controls).
- **Repro:** pt walkto pawn (own member); pt key r
- **Screenshots:** [invite.png](s09/shots/invite.png)

### 26. [minor] A new player nation is raided 37 times in ~10 game minutes without any provocation

- **Category:** balance; **reported by** 1 agent(s): s08
- **Detail:** Westham (the player's 5-member camp) became rivals and then hostile with Newport, Whiteham and Blackburg by tick 14500, through nothing the player did. The chronicle then logged 37 'raided Westham' / 'struck Westham' lines by tick ~31000. The world had 88 raids and 10 settlement captures in total. The status bar never explained why relations fell.
- **Repro:** pt start --seed 108; create a nation and found a settlement; gather for 10 game minutes; grep the chronicle for 'Westham'
- **Screenshots:** [nations1.png](s08/shots/nations1.png)

### 27. [minor] After H (Teleport Home) the bar shows 'Enter Settlement' but E says 'No entities within range'

- **Category:** bug; **reported by** 1 agent(s): s04
- **Detail:** Objective 5's hint says 'Walk up to it and press E. H takes you home.' After H, E fails with 'No entities within range to interact with.' while the command bar's 'Enter Settlement' works. H should land within E's range, or E should match the bar.
- **Repro:** Found a camp; walk away; press H; press E
- **Screenshots:** [e_vs_bar.png](s04/shots/e_vs_bar.png)

### 28. [minor] Chronicle is flooded by AI nations; your own statecraft scrolls away in seconds

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** 37 nation-creation events, plus rivalries, raids and disbandments, in the first in-game day. Your own law changes are highlighted but leave the 5-line panel almost at once. When two of your laws took effect on the same tick (15501), the status bar showed only conscription.
- **Repro:** play to tick ~15000 with law changes pending; watch the Chronicle panel
- **Screenshots:** [chronicle_noise.png](s06/shots/chronicle_noise.png)

### 29. [minor] Chronicle lines truncated with '...', including the player's own nation name

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** Recent Chronicle cuts lines at panel width ('You raised a banner and named your people Bi...'), so the new nation's name is hidden there; only Y full view shows full lines.
- **Repro:** press N; pt shot
- **Screenshots:** [nation_n.png](s09/shots/nation_n.png)

### 30. [minor] Claimant sheet promises goodwill but the result never shows it; Newport still reads Hostile (-86)

- **Category:** ux; **reported by** 1 agent(s): s08
- **Detail:** I chose 'Back the claimant (20 coins)'. The sheet said 'Newport will be friendlier toward you'. Coins went from 401 to 381 and the status line and chronicle both said 'With Westham's gold behind them, Daisy Jones seized the leadership of Newport.' Neither said anything about relations. On the Nations screen (O) right after, Newport showed Hostile (-86), and Newport had been raiding Westham before the event. A player cannot tell whether the +15 happened or whether 20 coins bought nothing. The resolution line, or the Nations row, should show the change (for example 'Newport +15, now -86').
- **Repro:** pt start --seed 108; play to tick ~30554 as leader of a nation (N, F); the 'A claimant stirs' sheet appears; click option 1; press O
- **Screenshots:** [nations1.png](s08/shots/nations1.png)

### 31. [minor] Controls screen shows debug cheats (F2-F6 spawn coins/wood/pawns) to every player

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Main menu > Controls page 3 lists 'Debug: F1 overlay, F2 generate land, F3 spawn pawn, F4 spawn coins, F5 spawn wood, F6 teleport pawns'. It's visible to a first-time player before they even start. Page 2 also mentions 'Era box (tap)' before the player has seen one, and nothing on the menu says what the game's goal is.
- **Repro:** pt start --seed 101; pt key space; Controls; Next; Next
- **Screenshots:** [controls_debug.png](s01/shots/controls_debug.png)

### 32. [minor] Denouncing a nation removes the War button, so denounce -> war escalation is impossible

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** After denouncing Townford (Rival -25) its War button became Peace. War can only be declared on Neutral/Friendly nations; a Rival you want to fight can only be denounced further. Expected War to remain available (with Peace beside it) for a Rival you are not at war with.
- **Repro:** O; click Denounce on Townford row; the War button now reads Peace
- **Screenshots:** [rival.png](s04/shots/rival.png)

### 33. [minor] Era box and objective describe the Village-era goal differently

- **Category:** text; **reported by** 1 agent(s): s01
- **Detail:** The collapsed Era box says 'Village: 0/4' (4 requirements), while the expanded checklist starts 'Village 0/1' (a settlement count). Objective 7 tracks 'Members · Buildings 2/2' while the Era checklist tracks 'Village, Members, Stalls 1/2, Day 1/3'. Settlement Info says 'Tier: Village (4 buildings)' while the era is still Camp. Tip 5 ('Your goals', which explains these boxes) arrives last, after two objectives are already done.
- **Repro:** pt start --seed 101; pt click 110 58 (expand Era box) at ticks 2589 and 10141
- **Screenshots:** [era2.png](s01/shots/era2.png)

### 34. [minor] Era checklist shows '[ ] Village 0/1' right after founding a settlement; meaning unclear

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** After F and E, the 'To reach the Village era' list still had Village 0/1 unchecked. It later ticked on its own as pawns built. Nothing on screen says what 'Village' needs.
- **Repro:** pt start --seed 102; found a settlement; E; click the Era panel
- **Screenshots:** [village_unchecked.png](s02/shots/village_unchecked.png)

### 35. [minor] First objective toast replaced after ~1 s by an objective the player never did ('Eat something')

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Gather wood completed at tick 1851; 'Eat something' completed by itself at tick 1903 (apples are eaten automatically, energy 80->95), so the first 'Objective complete' toast is overwritten almost instantly. Status said 'Gathered 5 wood and 3 apples' while floating text and inventory showed +2 apples, with no message that one was eaten. Similarly 'Build a market stall' completed at tick 7503 while I had Wood 10/20 Stone 0/5 and never pressed B.
- **Repro:** pt walkto tree; e; e; watch toasts and pt events
- **Screenshots:** [chop2c.png](s09/shots/chop2c.png)

### 36. [minor] First raid on my camp had no status line: same-tick 'Autosaved.' overwrote it

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** At tick 18002 the chronicle has 'Townford raided Southham and carried off 5 coins, 5 wood, 5 stone' but the status line only showed 'Autosaved.' (both tick 18002). Later raids did show 'Townford raided your camp'. An attack on the player should win over an autosave notice.
- **Repro:** Declare war on neighbour Townford at tick ~17546; wait to tick 18002; pt events
- **Screenshots:** [camp_war.png](s04/shots/camp_war.png)

### 37. [minor] First view: nearest tree is directly behind the player, off-screen

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** The first objective is 'Walk up to a tree and press E', but the nearest tree is 15m behind the camera (bearing +180), and the visible trees are 40m+ away at the horizon. Energy starts at 84.8 rather than full, with no explanation. Dragging the right mouse 300px turned the view only about 45 degrees.
- **Repro:** pt start --seed 101; Start; Begin; pt shot
- **Screenshots:** [first_view.png](s01/shots/first_view.png)

### 38. [minor] Floating '+N wood/apples' text is drawn under every HUD window, including the Objective complete toast and the Chronicle panel

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Answer to the goal question: yes. Floating gain text is drawn on ImGui's background draw list (FeelDirector.drawOverlay -> drawFloatingTexts(background,...)), so every HUD window covers it, including the 'Objective complete!' toast which sits at screen centre ~16% down, right where the gain text rises above a tree. Seen directly: in chop2b the '+2 apples' line rises behind the translucent Chronicle panel and is unreadable. I could not get a frame with '+N wood' and the toast at once (objective 1 completed 8 ticks after the chop2a frame), but the toast is an ImGui window at the same height so the same rule applies. Expected: gain text above panels, or toast placed away from the gain text's path.
- **Repro:** pt start --seed 109; Start; Begin; pt walkto tree; e (repeat until Wood 10/10); pt shot right after e
- **Screenshots:** [chop2b.png](s09/shots/chop2b.png)

### 39. [minor] K builds a House instantly with no menu, while Settlement Info advertises 'Next (K): Farm'

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** Pressing K inside my settlement immediately built a House and spent my 10 wood + 5 stone with no choice or confirmation. Settlement Info said 'Next (K): Farm, 25 wood, 10...' before and after. Help says 'K build a building'. A player saving resources for a stall can lose them by pressing K to look.
- **Repro:** Inside own settlement with >=10 wood and >=5 stone, press k
- **Screenshots:** [k_built_house.png](s05/shots/k_built_house.png)

### 40. [minor] K builds a House instantly, with no menu or confirm, spending materials saved for a Workshop

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** On first entering my new settlement I pressed K expecting a build choice. It built a House at once (20 wood, 15 stone) at 1/12 residents. That spent the wood and stone I had gathered for a Workshop. 'Next (K): ...' is shown in Settlement Info, but it is easy to miss on first entry.
- **Repro:** pt start --seed 107; found a settlement with about 70 wood / 21 stone; enter (e); press k
- **Screenshots:** [k_built_house.png](s07/shots/k_built_house.png)

### 41. [minor] Laws tab only describes the law in force, not the option you're about to choose

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** With Taxes: None and High pending, the description still reads 'No tax: members keep their coin.' What Low or High actually do (coin per member, loyalty slip, immigration halved) only shows up 30 s after enacting them. Withdrawing a pending change worked well, though: 'The change to conscription is withdrawn; they stay On.'
- **Repro:** key o; click Laws (543,45); click High (353,175); read the line under Taxes
- **Screenshots:** [laws_high.png](s06/shots/laws_high.png)

### 42. [minor] Leader's two-row command bar covers the Season panel; Sell Resources is offered with no stall

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** 'Craft Pickaxe'/'Exit Settlement' sit on top of the Season panel, making 'Winter: no harvests' unreadable. Sell Resources is clickable with no stall and then says 'This settlement does not have any stalls.'
- **Repro:** Become leader; enter settlement
- **Screenshots:** [button_overlap.png](s03/shots/button_overlap.png)

### 43. [minor] Low contrast HUD text over bright terrain (bottom buttons ~1.9-2.1:1, progress bars ~1.9:1, energy 2.7:1)

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** The bottom action buttons and Objective/Chronicle/Era panels are translucent. Measured contrast: buttons white on (105,195,147) over lime grass = 2.1:1, on sand at a settlement = 1.9:1; 'Wood 50/50' and 'Village: 2/4' white on yellow bar (230,179,0) = 1.9:1; Energy label white on green (64,179,77) = 2.7:1; Objective hint grey on washed-out green = 4.0:1; Chronicle header '[Y: more]' faintest of all. Opaque panels (Inventory, Season, Player Info) are 17:1. Hovered Era button white on (66,150,250) ~3:1.
- **Repro:** midday in Plains: pt shot; compare panels
- **Screenshots:** [found_f.png](s09/shots/found_f.png)

### 44. [minor] Main Menu autosave overwrites the world's only save slot, so a manual save made before an event choice is lost

- **Category:** ux; **reported by** 1 agent(s): s08
- **Detail:** I saved from the pause menu while the claimant sheet was up (tick 30554), chose option 1, then went to Main Menu > Load. Load lists one slot per world, and leaving to the menu had autosaved after the choice. The pre-choice save was gone, so the event could not be retried with another option. A player who saves before a big decision would expect to be able to go back.
- **Repro:** with an event sheet up: Escape > Save; Escape; pick an option; Escape > Main Menu > Load
- **Screenshots:** [load.png](s08/shots/load.png)

### 45. [minor] Market prices swing wildly within a minute (apples 54 -> 32 -> 13)

- **Category:** balance; **reported by** 1 agent(s): s05
- **Detail:** Whiteburg's apple buy price went 54 -> 32 -> 13 between ticks ~10,000 and ~11,400, and stone 42 -> 9. Purchase Food charged 21 coins for an apple while Market Info listed apples at 32 to buy. With prices this jumpy and only ~45 coins in the market, buying low and selling high has no readable signal.
- **Repro:** Enter Whiteburg's market at tick ~10,000, watch Market Info prices across 2-3 visits; click Purchase Food and compare coins spent to the listed price
- **Screenshots:** [sold1.png](s05/shots/sold1.png)

### 46. [minor] Menu screens: white text on light blue ~2.5:1; buttons barely distinguishable from background

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Title, main menu, Config, New World and Load use white text on (115,166,230): 2.5:1 for the small 'Press any key...' and button labels. Button fill (~#5BA0EE) is close to the background, so buttons have almost no visible edge.
- **Repro:** pt start --seed 109; pt shot; pt key space; pt shot
- **Screenshots:** [mainmenu.png](s09/shots/mainmenu.png)

### 47. [minor] No hover help anywhere: Config jargon, Nations actions and Laws options are unexplained

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Config: 'Chunk Size', 'Location Scale', 'Lag Prevention', 'Respawn Pawns' have no explanation, even on hover; Volume label '(sound plays in the browser version)' implies desktop is silent but offers Volume/Mute. Nations: Tribute/Ally/War/Denounce have no tooltips, the '(0)' relation score is unlabelled, 'Tribute 25 coins for +10' (+10 of what?), Ally looks enabled although relation 0 < 30. Laws: only the CURRENT choice is described; Low/High taxes, Conscription On, Borders Open and 'Defence 1.' are never explained. Selected law marked only by '> None <' brackets.
- **Repro:** Main menu > Config, pt move over items; in world O, hover buttons; click Laws
- **Screenshots:** [nations_hover_ally.png](s09/shots/nations_hover_ally.png)

### 48. [minor] Objective 6 'Build a market stall' is completed by an NPC's stall, so the player never builds one

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** Objective 6 asks for 20 wood + 5 stone and B inside the settlement. It completed at tick 7350, when Alice Brown built a stall. I had 6 wood and 0 stone. The market was full (4/4) by tick 7902, so B is never needed at all.
- **Repro:** pt start --seed 102; found a settlement and enter it; wait about 10 s
- **Screenshots:** [pawns_do_it.png](s02/shots/pawns_do_it.png)

### 49. [minor] Objectives 2 and 6 complete themselves with no player action

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** 'Eat something' completed 52 ticks after it appeared (auto-eat; an apple had already been eaten silently on the first chop). 'Build a market stall' (gather 20 wood + 5 stone, press B) completed 48 ticks after it appeared because an NPC opened a stall in my settlement. I never pressed B, and then went to mine stone for an objective that was already done. Neither teaches the mechanic.
- **Repro:** pt start --seed 101; chop 2 trees (obj 2 auto-completes); found + enter settlement (obj 6 auto-completes at ~tick 9001)
- **Screenshots:** [inside.png](s01/shots/inside.png)

### 50. [minor] Opening borders (to draw newcomers) silently invites thieves; the trade-off isn't explained

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Laws > Borders only says 'Closed: strangers keep away.' After I set Open, the next event was 'Thieves slipped through the open borders of Northernville and took 2 coins and 1 wood.' Members rose only to 7/10 after building a House about 4000 ticks later.
- **Repro:** pt key o; Laws tab; Borders Open; wait ~3000 ticks
- **Screenshots:** [laws.png](s01/shots/laws.png)

### 51. [minor] Pawns in a foreign town square are visible but unreachable; E enters the settlement instead

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** In Cityham, a green pawn stood 1.8-3m in front of me. E entered the settlement, because settlement comes first in the interact order, and G/R said there was no one within reach. The command bar showed no pawn button either. You can't approach other nations' people where they actually gather.
- **Repro:** walk to Cityham's well (~100m west of spawn), stand next to a pawn, key g / key r / key e
- **Screenshots:** [pawn_unreachable.png](s06/shots/pawn_unreachable.png)

### 52. [minor] Player energy didn't drop at all while standing inside a settlement in winter

- **Category:** balance; **reported by** 1 agent(s): s03
- **Detail:** Energy stayed at exactly 95 from tick 183512 to 196244 (~0.42 day) inside Fortham, and no apples were eaten. Outside, the player ate about an apple every ~2500 ticks. If shelter is meant to switch hunger off, the UI doesn't say so. Either way, winter has no bite for the player.
- **Repro:** pt start --seed 103 --season winter; join; enter settlement; wait 12000 ticks; pt state
- **Screenshots:** [energy_frozen.png](s03/shots/energy_frozen.png)

### 53. [minor] Right-side and objective HUD text is clipped or overlapping inside a settlement

- **Category:** text; **reported by** 1 agent(s): s05
- **Detail:** 'Next (K): Farm, 25 wood, 10' is cut off mid-cost. The Season panel overlaps Player Info and covers the 'Sell Resources' / 'Exit Settlement' buttons. Objective 7's bar reads 'mbers 2/10 · Buildings 1/2' (Members clipped). Market Info's Bricks row runs into the Season panel when there are 3 merchants. Objective 7 also omits the Stalls and Day requirements that the Era panel shows.
- **Repro:** Enter any settlement at 960x540 and look at the right column and the objective panel
- **Screenshots:** [inside_layout.png](s05/shots/inside_layout.png)

### 54. [minor] Same seed gives a different history once the player acts (first nation Westernburg vs Westernham)

- **Category:** ux; **reported by** 1 agent(s): s10
- **Detail:** Two idle runs of seed 1450162717 produced the same chronicle (Isabella Wood founds Westernham ~tick 901, Ruth Wilson founds Greentown ~1401, stall ~1450, Greentown settlement ~1801). In the first run, where I chopped a tree at about tick 600, the same pawn founded 'Westernburg' at tick 950 and the second nation was Tilly Taylor's Yellowfield at 1650. So the player's actions draw from the same random stream as world history, which changes names and order. Terrain, entities and starting pawns match exactly, so this may be intended, but a player who shares a seed and says 'go join Westernburg' will be confused. Consider separate RNG streams for world history versus player-driven randomness.
- **Repro:** Run A: pt start --seed Ironvale, Begin, walkto tree, e, wait; pt events shows Westernburg at 950. Run B: pt start --seed Ironvale, Begin, wait 60; pt events shows Westernham at 901.
- **Screenshots:** [world2_later.png](s10/shots/world2_later.png)

### 55. [minor] Settlement HUD panels overlap: Energy hidden, action bar wraps under Market Info, text truncated

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Inside the settlement, Season overlaps Player Info (the Energy bar is hidden). After the first building, Market Info grows long enough to push Season/Player Info off-screen, and the 2-row bottom bar's 'Exit Settlement'/'Purchase Food' sit under the panel. Settlement Info shows the jargon 'PCIS: 5' and a cut-off 'Next (K): House, 20 wood, 1'. Objective 7's progress bar text is clipped on the left ('mbers 6/10 • Buildings 2/2'). The settlement's floating 3D label renders huge and clipped across the top of the screen.
- **Repro:** pt start --seed 101; found settlement; pt key e (enter); pt key k
- **Screenshots:** [build1.png](s01/shots/build1.png)

### 56. [minor] Settlement command bar buttons change order between visits, causing a wrong build

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** At tick 228084 the bar read 'Build Stall, Build Farm, Build House'. Clicking Build Farm and then where Build House had been built two farms. Later the bar read 'Build Stall, Build House, Build Farm'. On a two-row bar, buttons need stable positions.
- **Repro:** As leader inside a settlement: click Build Farm (307,485), then click (412,485)
- **Screenshots:** [spring_buttons.png](s03/shots/spring_buttons.png)

### 57. [minor] Settlement command bar reflows under the cursor, so clicks land on the wrong button

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** At Whiteburg a 'Buy Axe (327)' button appeared between my screenshot and click. It pushed 'Exit Settlement' into the slot where 'Purchase Food' had been, so clicking Purchase Food exited the settlement. At low fps this is easy to hit.
- **Repro:** Enter Whiteburg's market; take a screenshot, wait a few seconds, click where Purchase Food was
- **Screenshots:** [reenter2.png](s05/shots/reenter2.png)

### 58. [minor] Settlement tier 'Town' and nation era 'Camp' use the same words for two different ladders

- **Category:** text; **reported by** 1 agent(s): s02
- **Detail:** On Day 2, Settlement Info said 'Tier: Town (7 buildings)' and the world label read 'Town of Eastford', while the era panel said 'Camp (1/4)'. Earlier, caravans had addressed it as 'Village of Eastford' (tick 9002). That is confusing when the goal is to reach the Village era.
- **Repro:** pt start --seed 102; found a settlement; wait until day 2; enter it
- **Screenshots:** [tier_vs_era.png](s02/shots/tier_vs_era.png)

### 59. [minor] Share screen and pause menu show only the numeric seed, not the word the player typed

- **Category:** ux; **reported by** 1 agent(s): s10
- **Detail:** World created from the text seed 'Ironvale' (New World screen says '"Ironvale" is seed 1450162717'). The pause menu shows 'World seed 1450162717' and the Share screen shows 'World seed 1450162717' and ?seed=1450162717. The number does round-trip correctly (verified), but the memorable word the player chose is not shown, so a player telling a friend by voice has to read out a 10-digit number. Expected: show 'Ironvale (1450162717)' and perhaps use the word in the link.
- **Repro:** pt start --seed Ironvale; space; Start; Begin; Escape; click Share (480,367)
- **Screenshots:** [share_numeric.png](s10/shots/share_numeric.png)

### 60. [minor] Talking to or gifting a moving pawn rarely works: it walks out of range before E/G lands

- **Category:** bug; **reported by** 1 agent(s): s03
- **Detail:** Across both worlds, 5 E presses and 8 G presses right after 'pt walkto pawn' (which reported the pawn at 2.9-4m) all got 'No entities within range' or 'no one within reach'. Pawns don't stop when the player comes near. At a few fps that makes talk, gift and invite nearly unusable.
- **Repro:** pt start --seed 103 --season autumn; ...; pt walkto pawn 30; pt key e
- **Screenshots:** [talk3.png](s03/shots/talk3.png)

### 61. [minor] Text drawn over the 3D world without a backing panel (Inventory, Pause, Share) collides with the player capsule

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Inventory screen lines 'Apples: 21' / 'Saplings: 7' sit right on top of the player capsule; Pause 'World seed 109' and the Share URL do the same. Full-history Chronicle window (Y) is translucent, so the Tip card text and the Season panel show through behind its rows (text on text).
- **Repro:** pt key i; pt shot; pt key y; pt shot
- **Screenshots:** [inventory.png](s09/shots/inventory.png)

### 62. [minor] The first player world event is slow to arrive (tick 30554, ~10 game minutes)

- **Category:** balance; **reported by** 1 agent(s): s08
- **Detail:** Nothing was offered until 10.2 game minutes in. That is inside the designed 7-11 minute window, but on a loaded machine (about 20 tps) it took 20 real minutes. Only one AI nation event (a Newton leadership seizure, tick 19754) appeared in the chronicle before that. A tester can see at most one or two choices in a 30-minute session.
- **Repro:** pt start --seed 108; play normally; watch pt events for world_event_offered
- **Screenshots:** [event1.png](s08/shots/event1.png)

### 63. [minor] Village era gated on 'Day 3' (~17+ game minutes); after minute 6 nothing achievable was left

- **Category:** balance; **reported by** 1 agent(s): s01
- **Detail:** A game day is about 30000 ticks (10 game min). I finished objectives 1-6 by tick 9001 (3 game min). Objective 7 was then blocked by the pawn bug and the day gate, so I spent the rest of my session grinding wood and stone with no visible progress. Food is trivial: I had 35 apples and energy never dropped below 81.
- **Repro:** pt start --seed 101; play 7 game minutes
- **Screenshots:** [era2.png](s01/shots/era2.png)

### 64. [minor] War is free and can be ended in the same instant: war+peace is a free 60-second truce

- **Category:** balance; **reported by** 1 agent(s): s04
- **Detail:** The game pauses on the O screen, so War then Peace happen in the same tick. Greenford (9 to my 5) took up arms and then 'accepts peace (the fighting has cost them enough)' with zero fighting; result: relations -19 and a 60 s truce. Littleburg the same. Expected peace to need some time at war or some cost, and the 'fighting has cost them' line to need actual fighting.
- **Repro:** O; War on Greenford; Peace on Greenford (same tick 15158)
- **Screenshots:** [greenford.png](s04/shots/greenford.png)

### 65. [minor] World event choices hide the numbers: claimant odds and relation sizes are not stated

- **Category:** ux; **reported by** 1 agent(s): s08
- **Detail:** Option 3 says 'if the claimant wins anyway, they will remember', but not that this happens half the time or that it costs -10. Options 1 and 2 say 'friendlier' and 'a little goodwill' instead of +15 and +8. Option 1 shows its coin cost, so the costs are clear, but the benefits are not concrete enough to compare.
- **Repro:** same as above; read the sheet
- **Screenshots:** [event1.png](s08/shots/event1.png)

### 66. [minor] World-space settlement label is huge, clipped off the top of the window and hidden under the status bar/Chronicle

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** Standing at my settlement the label ('Camp of Bigshire / N here, 0/8 residents') is ~45px tall, its name line is cut off by the top edge and the rest collides with the status toast, Chronicle panel and Era box. Stepping back 2s still leaves it under the status bar.
- **Repro:** after founding: pt walkto settlement; pt shot; pt hold s 2; pt shot
- **Screenshots:** [found4.png](s09/shots/found4.png)

### 67. [polish] HUD panels overlap and clip inside the camp; world nametags draw over HUD and the O screen

- **Category:** ux; **reported by** 2 agent(s): s04, s06
- **Also reported as:** "HUD panels overlap at 960x540 inside a settlement; world labels draw over menus"
- **Detail:** Inside my camp the Settlement Info panel is clipped at the right edge ('Next (K): Farm, 25 wood, 10'), the Season panel covers Player Info, and the huge 'Camp of Southham / 0 here, 1/8 residents' nametag overlays the top-right HUD and the Nations tab buttons. Objective 7's bar text is clipped on the left ('nbers 2/10 • Buildings 0/2').
- **Repro:** Enter your settlement; press O
- **Screenshots:** [panels_overflow.png](s04/shots/panels_overflow.png) [overlap_settlement.png](s06/shots/overlap_settlement.png)

### 68. [polish] '--' in world event choice text renders as a blank gap

- **Category:** text; **reported by** 1 agent(s): s08
- **Detail:** Option 3 of 'A claimant stirs' reads 'Nothing now    but if the claimant wins anyway, they will remember.' The source text is 'Nothing now -- but ...'. The dash glyph does not render on the sheet.
- **Repro:** same as above
- **Screenshots:** [event1.png](s08/shots/event1.png)

### 69. [polish] 'Objective complete: Create or join a nation +25 coins' toast is shown again after loading a save

- **Category:** ux; **reported by** 1 agent(s): s10
- **Detail:** The objective was completed at tick 3342, before saving at 3494. After pt start --continue the completion toast appears again in the middle of the screen. Coins stay at 306 (not paid twice), so it is only a stale toast, but it suggests the reward was granted again.
- **Repro:** Complete objective 3 (press N), Escape, Save, pt stop, pt start --continue; look at screen
- **Screenshots:** [loaded.png](s10/shots/loaded.png)

### 70. [polish] A world event froze the game while I waited, unnoticed

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** At tick 27854 a 'Sickness' event pauses the world ('The world waits while you decide'). I was idling through the day gate, so about 3 wall minutes were lost before I noticed it. It is fine for a player at the screen, but an idling player gets no audio or other alert.
- **Repro:** pt start --seed 102; found a settlement; wait to day 2 dawn
- **Screenshots:** [event.png](s02/shots/event.png)

### 71. [polish] Chronicle event ticks differ by 1-3 ticks between two identical idle runs of the same seed

- **Category:** bug; **reported by** 1 agent(s): s10
- **Detail:** Same seed, no input: Westernham founded at tick 901 vs 902, its settlement 953 vs 950, Greentown 1401 vs 1402, stall 1453 vs 1450, Greentown settlement 1801 vs 1802. Content and order are identical, only the tick jitters. Could be events stamped at frame time rather than sim tick on a loaded machine, or sim stepping depending on wall-clock frame time. Harmless for players but would break exact replays or tick-based tests.
- **Repro:** Run the same idle seed twice (pt start; type 1450162717 / Ironvale; Begin; pt wait 75) and compare pt events chronicle ticks.
- **Screenshots:** [run3_idle.png](s10/shots/run3_idle.png)

### 72. [polish] Chronicle flooded by NPC-vs-NPC events; nation auto-named without a prompt

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** There were 258 chronicle entries by the end of day 1, and the recent panel was all 'X and Y have become rivals' between nations I'd never met, with lines truncated with '...'. Pressing N created 'Northernville' instantly, with no chance to name or confirm it. A tree trunk stands through the middle of the new settlement's plaza.
- **Repro:** pt key n; pt key y
- **Screenshots:** [founded2.png](s01/shots/founded2.png)

### 73. [polish] Crafting screen doesn't say how to craft; Load 'Delete' not styled as destructive

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Crafting rows have no Craft button and no hover feedback when unaffordable; subtitle lacks a final period. Load screen 'Delete' is the same blue as the save row.
- **Repro:** pt key c; main menu > Load
- **Screenshots:** [load.png](s09/shots/load.png)

### 74. [polish] Era box: 'Village: 0/4' (requirements) next to checklist 'Village 0/1' (settlements) reads as contradictory

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** Header counts requirements met, checklist counts village settlements, both labelled 'Village'. 'Day 1/3' in the checklist vs 'day 1/2' in Season also reuses 'day' for different counts.
- **Repro:** click the Era box
- **Screenshots:** [era_panel.png](s09/shots/era_panel.png)

### 75. [polish] First chop message says '2 apples' but the inventory shows 1 (the other was silently eaten)

- **Category:** text; **reported by** 1 agent(s): s01
- **Detail:** 'Gathered 5 wood and 2 apples from tree.' Inventory showed Apples: 1, Saplings: 1 (saplings aren't mentioned), and energy jumped 81->96. Tip 3 'Gather stone' also showed while Gather wood was still at 5/10. The tree vanishes in one press with no animation.
- **Repro:** pt start --seed 101; pt walkto tree; pt key e
- **Screenshots:** [chop1.png](s01/shots/chop1.png)

### 76. [polish] HUD layout collisions: the settlement button row overlaps Market Info, and the world-space label sits behind the panels

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** Inside the settlement, the bottom buttons wrap to two rows and 'Purchase Food' covers the Planks/Bricks price lines. The big '<Tier> of Eastford / N here, M residents' label renders at the top, behind the chronicle and Settlement Info panels. On the first frame of the world, the Season panel overlapped the Energy bar and the Inventory panel was clipped at the bottom. That fixed itself a moment later. Town checklist 'Funds 565/1000' does not match Settlement Info 'Funds: 12'.
- **Repro:** pt start --seed 102; found a settlement; enter it
- **Screenshots:** [village_reached.png](s02/shots/village_reached.png)

### 77. [polish] HUD panels overlap on the first frame of a new world (Season panel over Player Info, Inventory cut off)

- **Category:** ux; **reported by** 1 agent(s): s10
- **Detail:** On the very first screenshot after Begin (tick 1), the Season panel sits over the energy bar in Player Info and the Inventory panel runs off the bottom of the screen. A few seconds later the layout is correct. Brief, but it is the first thing the player sees.
- **Repro:** pt start; Start; type seed; Begin; pt await WORLD 60; pt shot immediately
- **Screenshots:** [world2.png](s10/shots/world2.png)

### 78. [polish] Nations row wrap leaves ', truce 60' / 's' split and pushes the last nation off the list

- **Category:** text; **reported by** 1 agent(s): s04
- **Detail:** 'Greenford: 5 members, 1 town. Neutral (-19), truce 60' wraps with a lone 's' on the next line; the taller row drops Hamletburg off the bottom (7 rows shown).
- **Repro:** O; War then Peace on Greenford
- **Screenshots:** [truce_wrap.png](s04/shots/truce_wrap.png)

### 79. [polish] Neighbours turn hostile and raid the player's Camp repeatedly with no provocation

- **Category:** balance; **reported by** 1 agent(s): s05
- **Detail:** Oldburg became hostile (tick 12503) without any interaction and raided my camp 3 times, plus Oldwood once, by tick 25351 (4-5 coins each). Small sums, but hostility also shuts trade routes. Expected some cause or warning before a brand-new neighbour turns hostile.
- **Repro:** pt start --seed 105; create nation, found settlement around (-30,-331); wait; pt events \| grep -i raid
- **Screenshots:** [tps0.png](s05/shots/tps0.png)

### 80. [polish] Settlement HUD text clipped and overlapping (title banner, Next (K) line, objective bar, Season panel)

- **Category:** text; **reported by** 1 agent(s): s03
- **Detail:** Inside a settlement the big 'Village of Castleford / N here, M residents' banner overlaps the chronicle and objective panels. 'Next (K): Farm, 25 wood, 10...' runs off the right edge. The objective bar reads 'nbers 7/10' (Members clipped). The Season panel's 'poor in food' is cut off at the bottom of the window.
- **Repro:** pt start --seed 103 --season autumn; join; key h; walkto settlement; key e
- **Screenshots:** [settle_overlap.png](s03/shots/settle_overlap.png)

### 81. [polish] Settlement banner '5 here, 1/8 residents' when only the player is present

- **Category:** bug; **reported by** 1 agent(s): s03
- **Detail:** After Fortham's residents starved, the floating banner still said '5 here' while Residents was 1/8 and nobody else was visible.
- **Repro:** As in the starvation repro; enter Fortham around tick 228000
- **Screenshots:** [fortham_late.png](s03/shots/fortham_late.png)

### 82. [polish] Settlement's floating world label is huge and runs off the top of the screen

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** Standing at my settlement, the label 'Camp of Lowertown / N here, X/8 residents' renders at a huge size, is cut off at the top edge, and sits behind the Chronicle and objective panels.
- **Repro:** Stand on or in any settlement
- **Screenshots:** [label_clip.png](s05/shots/label_clip.png)

### 83. [polish] Tool wear status says '1 uses left'

- **Category:** text; **reported by** 1 agent(s): s07
- **Detail:** The gather status at tick 20334 read 'Gathered 7 wood and 2 apples from tree (+3 from your axe), 1 uses left.' It should say '1 use left'. The screenshot is from after continue, where save/load kept tool wear correctly (Axes 19 uses, Pickaxes 9 uses).
- **Repro:** pt start --seed 107; craft an axe; chop 19 trees; read the status line
- **Screenshots:** [after_continue.png](s07/shots/after_continue.png)

### 84. [polish] Touch wording on desktop: 'Era box (tap)' in Controls and 'tap it' in the Your goals tip

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** Mouse players expect 'click'. The Era box being clickable is otherwise only hinted by a '+' glyph.
- **Repro:** Controls page 2; Tip 5 of 5
- **Screenshots:** [tip_tap.png](s09/shots/tip_tap.png)

### 85. [polish] Unexplained labels: 'PCIS: 2', green triangles after market prices, 'Export replay (USD)'

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** Settlement Info 'PCIS' is jargon; Market Info price triangles have no legend; 'USD' on Share reads as US dollars. Pawn nameplates (name/traits/activity) are ~6px tall and unreadable.
- **Repro:** enter settlement; Esc > Share
- **Screenshots:** [share.png](s09/shots/share.png)

## Objective timings

Game seconds from the world's creation to each objective (tick / 50, from events.jsonl). Game time, not wall time: with many sessions on one machine the world runs slower than real time, and wall-clock timings would mostly measure the machine.

| # | Objective | s01 | s02 | s03 | s04 | s05 | s06 | s07 | s08 | s09 | s10 | min | median | max |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Gather wood | 36 | 55 | 2425 | 32 | 54.1 | 159.1 | 27 | 16 | 37 | 47 | 16 | 42 | 2425 |
| 2 | Eat something | 37.1 | 56 | 2426 | 33 | 55 | 160 | 28 | 17 | 38.1 | 48 | 17 | 43 | 2426 |
| 3 | Create or join a nation | 53.1 | 59 | 2433 | 34 | 56.1 | 161.1 | 94 | 31 | 44.1 | 67 | 31 | 57.5 | 2433 |
| 4 | Found a settlement | 173.1 | 139 | 2434 | 150 | 104.1 | 268 | 140 | 86 | 137 | - | 86 | 140 | 2434 |
| 5 | Enter your settlement | 179.1 | 141 | 2440 | 298 | 110.1 | 273.1 | 143 | 90 | 147 | - | 90 | 147 | 2440 |
| 6 | Build a market stall | 180 | 147 | 2441 | 331 | 157 | 276 | 151 | 91 | 150.1 | - | 91 | 157 | 2441 |
| 7 | Reach the Village era | - | 1020 | - | - | - | - | - | - | - | - | 1020 | 1020 | 1020 |

