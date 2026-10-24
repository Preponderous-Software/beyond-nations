# Playtest summary: 20261023-2305

4 sessions, build beyond-nations 0.6.0 (fc45ac8), generated 2026-10-23 23:53.

**Findings:** 33 reports in 30 groups -- 0 blocker, 4 major, 18 minor, 11 polish.

## Outcomes

| Session | Goal | Outcome | Fun | Minutes | Objectives | Deaths | Findings | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| s01 | Re-check the two-click confirmations (#345, #368): from the Nations screen (O) declare war on and denounce another nation, timing your second click; a second click up to 8 seconds after the first must confirm, not re-arm. Check the Nations list keeps its order while open, that a truce warning appears when war would break one, that L asks twice before you leave or disband, and that the command bar's buttons do not move under the cursor as pawns walk in and out of reach. | achieved | 3 | 8.8 | 0 | 0 | 6 | The #345/#368 confirmations work as intended. Second clicks at 6.3, 7.5 and 7.6 s confirmed and clicks at 8.6 and 11.1 s re-armed. The truce warning appears, L asks twice to both leave and disband, the Nations list holds its order while open (it re-sorts on reopen), and the fixed command-bar rows stay put. Still to fix: the pawn Talk/Gift/Invite buttons still hop between rows as different pawns come into reach, and a stale 'click again' prompt stays on screen after the confirm window expires. The diplomacy stage also loses your only village about 20 seconds in, while the HUD promises almost 5 minutes before sieges begin. |
| s02 | Survive the first winter and keep your nation's people alive (#353). Found or join a nation early. Record whether the HUD warns when members go hungry or starve, whether Feed Residents appears and works when you carry food home, and count hunger deaths (events) over the winter; the 0.6.0 run on this seed saw 16 NPC hunger deaths across 7 nations with no warning. | partial | 3 | 45 | 6 | 0 | 9 | The #353 fix works: in winter an amber HUD line ('Yellowton: N members hungry. Bring food home and Feed Residents.') shows everywhere, a 'Feed Residents (N hungry)' button appears in town when you carry food, and my nation lost no one. World hunger deaths fell from 16 to 5 over two thirds of the winter, but 4 of those 5 were AI nation leaders (two starved inside their own walls), which disbands nations. Feeding is click-heavy: each press gives one apple per pawn, so the warning comes back every ~12 game seconds, and the button moves around in the reflowing command bar. |
| s03 | As a new player, try crafting and trading before and after founding a nation (#356, #354). Before you have a nation, the crafting screen must say the Workshop is locked until the Village era rather than telling you to build one. At a market, try Sell Resources in several situations (nothing to sell, your own stall only, merchants too poor) and record whether each refusal tells you why. | partial | 3 | 17.4 | 6 | 0 | 8 | #356 is fixed: before a nation and in a Camp, the crafting screen says the Workshop unlocks in the Village era, and inside a town that has one it says every recipe is open. #354 is half done: when Sell Resources actually runs, its refusals are clear ('no stalls', 'No merchant here can pay: the richest has 0 coins, and your cheapest good (stone) sells for 18'). But the button greys out silently when you have nothing to sell, and it also greys out when you carry only food, apples or saplings, which the market does buy. Pawns grab every stall in your new camp so quickly that the player can't own one, so the 'only your own stall' refusal never came up. |
| s04 | Audit the HUD at 960x540 inside a settlement (#350): enter your settlement and check that every box in the right-hand column fits on screen without overlapping the others or the command bar, that long lines wrap instead of being cut off, and that nothing important is lost in the compact layout. Also note any other text that is clipped, overlaps, or is unclear, with screenshots. | achieved | 3 | 11 | 7 | 0 | 9 | The #350 fix works for the right column: at 960x540 nothing runs off the bottom and long lines (status, settlement 'Next (K)', objective text) wrap well. But compaction cuts the wrong things: the player's Nation, Role and Land go while the 'PCIS' debug line stays, which hurts most inside someone else's settlement. The left column and centre have no such guard: expanding the Era box covers the command bar, and the inventory and tip overlap the buttons. |

## Findings

### 1. [major] Sell Resources is disabled when carrying only food/apples/saplings, though the market lists prices for them

- **Category:** bug, ux; **reported by** 2 agent(s): s02, s03
- **Also reported as:** "Purchase Food fails with 315 coins while the market lists apple prices"
- **Detail:** With 5 chicken meat and no wood/stone, inside the Village of Greenham (Market Info shows 'Chicken Meat 57/51', 1 merchant), the Sell Resources button is greyed out and clicking it does nothing. The button's enable check only looks at wood/stone/planks/bricks, but the market itself (Market.isSoldInBulk) buys every non-coin, non-tool item, and the refusal text earlier even named 'sapling' as my cheapest good. Expected: the button is enabled for any sellable good, or explains why not.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 107; leave title, Start, Begin; pt walkto chicken; pt key e (x5 chickens); pt walkto stall 250; pt key e; click Sell Resources
- **Screenshots:** [meat_disabled.png](s03/shots/meat_disabled.png) [town.png](s02/shots/town.png)

### 2. [major] AI nation leaders starve during winter: 4 of the 5 hunger deaths were nation leaders, 2 of them 'within the walls'

- **Category:** balance; **reported by** 1 agent(s): s02
- **Detail:** Over ticks 180000-219800 (winter day 1 and two thirds of day 2) the world logged 5 hunger deaths (pawn_death total=5), all in AI nations: Tina Brown, leader of Farmland (wilds, t206864, nation disbanded); Grace Lewis of Newport (wilds, t207460); Frank Taylor, leader of Blueport (within the walls, t212888); Bob Brown, leader of Westton (wilds, t214920); Wendy Walker, leader of Newport (within the walls, t218764). Down from 16 in the 0.6.0 run, but leaders are disproportionately the victims, and some starve inside their own settlement with markets around them. A leader death disbanded Farmland outright ('Tina Brown has died. Farmland has been disbanded.'). Expected AI leaders, who have coins and a market, to keep themselves fed, or at least not to make up 80% of the deaths.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 103 --season winter; Start, Begin; play ~40 min real time (tps ~15-30); grep PawnDeath out/events.jsonl
- **Screenshots:** [w1.png](s02/shots/w1.png)

### 3. [major] Diplomacy stage: home village is captured ~20 game seconds in while the HUD says 'Sieges can begin in 4:58'

- **Category:** balance; **reported by** 1 agent(s): s01
- **Detail:** At tick 68 the HUD shows 'Sieges can begin in 4:58' while the Nations screen already says 'Your village: Besieged by Greenshire (2/6)!'. The siege went to 4/6 at tick 500 and at tick 1000 'Greenshire has captured your village! Upperwood has fallen ... You are without a nation.' In that time I had only opened the Nations screen (world paused), paid tribute, declared war (which broke the 60 s truce) and walked a few metres. Breaking the truce was my choice, but the player gets almost no time to react, and the 'Sieges can begin in' countdown contradicts a siege already in progress. Expected the countdown to hold, or not to be shown while a siege is running.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 111 --stage-diplomacy; space; click 480 315; click 480 315; read HUD + press o; (optionally tribute then War twice on Greenshire); Escape; walk ~20 s; pt events
- **Screenshots:** [nonation.png](s01/shots/nonation.png)

### 4. [major] Expanding the Era box inside a settlement pushes the Objective box over the command bar

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** Inside own settlement at 960x540, clicking the Era box '+' expands it to ~18 lines. The Objective 8 box is pushed down to y~398 and drawn on top of the command bar: 'Build Workshop', 'Build Stall', 'Withdraw Funds' and 'Collect Stockpile (12)' are hidden or half-hidden. The left column has no bottom limit against the command bar (the #350 fix only covers the right column). Expected the objective box to compact or the era panel to scroll.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 109 --stage-diplomacy; title/Start/Begin; N; gather 50 wood; F away from other claims; E to enter; click Era '+' at (168,58)
- **Screenshots:** [era_open_overlap.png](s04/shots/era_open_overlap.png)

### 5. [minor] 'PCIS: N' debug jargon in Settlement Info survives compaction while player lines are cut

- **Category:** text; **reported by** 1 agent(s): s04
- **Detail:** Settlement Info shows 'PCIS: 2' (pawns currently in settlement, the F1 debug box's term). A player can't read it, and it repeats the floating '2 here' label. At 960x540 this line is kept while Region, Nation, Role, Land and Relationships are dropped. Suggest removing or renaming it to free a line.
- **Repro:** enter any settlement
- **Screenshots:** [pcis.png](s04/shots/pcis.png)

### 6. [minor] 'You fed N residents' confirmation is overwritten within 20 ticks by diplomacy status messages

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** At t198480 the status read 'You fed 2 residents in the Village of Yellowton.' and at t198500 it was replaced by 'Citywood shares an enemy with you: relations are now Friendly (+20).' On screen the only sign that feeding worked was the apple count dropping 42->40. The same happened at t214056 with Southernport/Oldville.
- **Repro:** Press Feed Residents during a busy chronicle period (the night of day 7 on seed 103)
- **Screenshots:** [c4fed.png](s02/shots/c4fed.png)

### 7. [minor] 'Your nation is collapsing!' shown after leaving a nation, and leave/join refills the 5-minute clock

- **Category:** text; **reported by** 1 agent(s): s01
- **Detail:** After leaving Newshire as a member (no nation of my own), the banner reads 'Your nation is collapsing! Found a settlement or win a follower within 5 minutes, or your story ends.' and the HUD timer jumps back to 4:59 (it was at ~4:07 before I joined). The player has no nation, so 'your nation' is confusing, and joining then leaving resets the deadline.
- **Repro:** stage-diplomacy, after losing the village: j (join), l, l (leave); read banner + HUD timer
- **Screenshots:** [lm2.png](s01/shots/lm2.png)

### 8. [minor] Command bar reflows when Feed Residents appears or disappears, so the next click lands on Talk/Build

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** The Feed button shows up in different places (297,414 in shots/c10.png; 115,414 in shots/c14.png) depending on which pawn buttons are present. When the last hungry pawn is fed it vanishes and 'Talk to Frank Robinson' slides into its spot (shots/fedagain.png). A second Feed click opened Phoebe Thompson's dialogue (t208124), and on another visit the rows shifted up and my 'Collect Stockpile' click built a Farm instead (t215560). The code comment on the button says it was put last 'so its coming and going moves nothing', but in play it does move things.
- **Repro:** Inside your own settlement with 1 hungry resident: click Feed Residents twice in a row
- **Screenshots:** [fedagain.png](s02/shots/fedagain.png)

### 9. [minor] Command bar: Talk/Gift/Invite buttons jump between rows depending on the nearest pawn's trait text length

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** The fixed buttons (Enter Settlement / Teleport Home / Plant Sapling row and Leave Nation) never moved as pawns came and went, which is the main fix working. But the pawn buttons still move: 'Talk to X (Stranger) - traits' is placed at the end of row 2 (x~386) when its text is short (Mia Brown - Gregarious; Dave Davies - Loyal, Greedy) and at the start of row 1 when long (Karl Smith - Loyal, Industrious; Irene Wilson - Greedy, Ambitious), and Gift/Invite slide between x=10 and x~386 to match. With the pointer parked at (470,476) the Talk button appeared and disappeared under it, and Gift (which spends coins) can take the spot where Talk was a moment earlier.
- **Repro:** stage-diplomacy seed 111; join a nation (j); pt walkto pawn; pt move 470 476; take a screenshot every second as pawns wander in and out of reach
- **Screenshots:** [cmdbar_jump.png](s01/shots/cmdbar_jump.png)

### 10. [minor] Compact right column drops your own Nation/Role/Land and Region, worst inside a foreign settlement

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** Right column fits on screen in every case I saw (no box runs off the bottom, #350 holds). But in compact mode Player Info shows only the Energy bar, and Season shows only 'Spring - day 1/2, year 1' (Region and 'good farming and game' are lost). Inside a FOREIGN settlement (Upperland's village after Westerntown fell at tick 1000), Settlement Info names Upperland and its leader, so nothing on screen says you are now nationless; the code's rationale ('the settlement box already names the nation and leader') only holds in your own settlement. Expected Nation/Role kept when the settlement isn't yours, or the box expandable by click (clicking it does nothing).
- **Repro:** pt start ... --stage-diplomacy; Start/Begin; H; E (enter home village); wait until it is captured at tick 1000
- **Screenshots:** [scroll_player.png](s04/shots/scroll_player.png)

### 11. [minor] Feed Residents gives each hungry pawn one item, so the same member is hungry again within ~600 ticks

- **Category:** balance; **reported by** 1 agent(s): s02
- **Detail:** With 50-60 apples in my pack, each press of 'Feed Residents (1 hungry)' handed the pawn one apple (+15 energy, while 'hungry' is under 80). The amber 'Yellowton: 1 member hungry' line and the button were back about 600 ticks (12 game seconds) later: fed at 216844, hungry again by 217424; fed 217464, hungry again by 218548. Keeping the nation fed meant standing in town pressing Feed every few game seconds. Expected one press to feed each pawn until it is no longer hungry (or to say 'still hungry' when it is not).
- **Repro:** Found a nation and a settlement, carry apples (chop trees), stay inside the settlement in winter; press Feed Residents whenever it appears; pt wait 30 and watch it come back
- **Screenshots:** [nag_again.png](s02/shots/nag_again.png)

### 12. [minor] H (teleport home) and E still act while a world-event sheet says 'The world waits while you decide'

- **Category:** bug; **reported by** 1 agent(s): s02
- **Detail:** The Bandit camp event sheet was up and the tick was frozen at 201552, but my H and E key presses still went through: the status showed 'Welcome home!' and then 'Entered settlement.' Expected world input to be blocked until an option is chosen.
- **Repro:** When a world event sheet appears, press h then e; pt events shows 'Welcome home!' / 'Entered settlement.' at the frozen tick
- **Screenshots:** [c6f.png](s02/shots/c6f.png)

### 13. [minor] Inventory box jumps to a centre column and the Tip window covers it and the command bar

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** When the left column or the command bar is tall (4-line objective, or a merchant in reach adding 'Order ...'/'Talk to ...' rows), the Inventory Info box moves to x~220 over the 3D view. The centre Tip window ('Tip 3 of 5: Gather stone') is then drawn over the inventory's values and over the 'Talk to Fiona Wilson (Stranger) - Glutton, Loyal' command button. The box also flips back and forth every time a pawn walks into or out of reach (shots/built2.png vs shots/later.png).
- **Repro:** found and enter your settlement with tips enabled; wait for a merchant to stand by you
- **Screenshots:** [tip_over_cmdbar.png](s04/shots/tip_over_cmdbar.png)

### 14. [minor] Nothing-to-sell refusal is silent: Sell Resources just greys out with no tooltip or message

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** With an empty inventory inside the Town of Townham, Sell Resources is disabled; clicking or hovering gives no reason. The game has a good message for this ('You have nothing to sell: markets buy resources and food, not coins or tools.') but the disabled button makes it unreachable. Same pattern for Build Stall: greyed out with no cost shown; only pressing B reveals 'Not enough wood. Need 13 more.'
- **Repro:** fresh seed 107 world; wait ~40s for pawns to found settlements; pt walkto settlement 250; pt key e; click Sell Resources (546,517)
- **Screenshots:** [sell_empty2.png](s03/shots/sell_empty2.png)

### 15. [minor] Objective 4 hint tells a nationless player to 'wait for yours, or press L then N'

- **Category:** text; **reported by** 1 agent(s): s03
- **Detail:** After disbanding my nation (L, L), Objective 4 'Found a settlement' reads 'Only a leader can found one. Wait for yours, or press L then N to lead.' I have no nation, so the hint should just say press N to start one.
- **Repro:** seed 107; N; L; L; look at the objective panel
- **Screenshots:** [obj_nonation.png](s03/shots/obj_nonation.png)

### 16. [minor] Pawns fill the player's own new camp market (4/4 stalls) within about a minute, so the player can never build a stall there

- **Category:** balance, ux; **reported by** 1 agent(s): s03
- **Also reported as:** "'Build a market stall' objective completes when a pawn builds a stall in your camp"
- **Detail:** After founding Citytown (tick 10502) the market went 0/4 to 4/4 stalls, all pawn-owned, before I could gather the 20 wood for B. B then says 'Market is full. Cannot build stall.' and none is for sale. As a result I could not set up the 'only my own stall' situation at all, so the 'The only stall here is yours' refusal went untested.
- **Repro:** seed 107; N; gather 50 wood; travel to open ground; F; E to enter; gather 20 wood; H; E; B
- **Screenshots:** [ownstall_full.png](s03/shots/ownstall_full.png) [own_in.png](s03/shots/own_in.png)

### 17. [minor] Settlement becomes a Town but the Era box stays 'Camp (1/4)' and the objective still says 'Reach the Village era'

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** Once I had 3+ buildings the status said 'House built. The settlement is now a Town!' and Settlement Info showed 'Tier: Town', but the top-left Era read 'Camp (1/4)' and Objective 7 was still 'Reach the Village era' (Members 6/10). Settlement tier and nation era are named alike, which makes this confusing.
- **Repro:** Build stall, house, farm, house in your first settlement; compare Era box with Settlement Info tier
- **Screenshots:** [town.png](s02/shots/town.png)

### 18. [minor] Siege countdown contradicts an active siege and vanishes silently

- **Category:** text; **reported by** 1 agent(s): s04
- **Detail:** At tick 620 the Era box says 'Sieges can begin in 4:47' while a 'Besieged by Upperland' banner is shown and the status says 'Upperland is besieging your village (4/6)'. The village is captured at tick 1000 with the countdown still running. Later, in the new nation, the line just disappears at 0:00 (tick ~15000) with no 'sieges now possible' message. (Diplomacy staging may cause the first part.)
- **Repro:** pt start ... --stage-diplomacy; Start/Begin; H; E
- **Screenshots:** [inside1.png](s04/shots/inside1.png)

### 19. [minor] Stale 'Click War again...' status remains after the 8 s confirm window lapses

- **Category:** text; **reported by** 1 agent(s): s01
- **Detail:** After arming War (Nations screen) or 'Declare War on Greenshire' (command bar) and waiting more than 8 s, the button correctly reverts from 'Confirm' to 'War', but the status line keeps saying 'Click War again to declare war on Southernfield. (x2)' / 'Click again to confirm: Declare War on Greenshire.' That instruction is no longer true: a click now only re-arms. The '(x2)' repeat counter on a re-arm also reads like 'click twice'.
- **Repro:** o; click War (657,302); wait 9 s; screenshot
- **Screenshots:** [war_expired_stale.png](s01/shots/war_expired_stale.png)

### 20. [minor] Successful sale message is immediately overwritten by diplomacy toasts

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** Selling in my own camp gave +65 coins (1 wood, 1 stone, 1 apple sold) but the 'You sold resources to X. Relationship increased...' status was replaced at once by 'Bigtown resents your border: relations are now Rival (-20).' Only a faint '+65 coins' float shows. The player can't see what was sold or to whom.
- **Repro:** seed 107; in own camp with pawn merchants holding coins, click Sell Resources
- **Screenshots:** [sell_ok2.png](s03/shots/sell_ok2.png)

### 21. [polish] Huge floating pawn labels and clipped settlement banner / objective bar

- **Category:** text, ux; **reported by** 2 agent(s): s03, s04
- **Also reported as:** "Huge floating settlement label clipped at top and bleeding through overlays"
- **Detail:** A nearby pawn's label 'Xavier Watson / Ambitious / (gathering resources)' covered the centre of the screen in very large text. The settlement banner ('Camp of Citytown / 0 here, 3/8 residents') is cut off at the top edge, and the Village objective bar reads 'mbers 4/10 • Buildings 0/2' (clipped on the left).
- **Repro:** seed 107; found a settlement; stand near a pawn
- **Screenshots:** [bigtext.png](s03/shots/bigtext.png) [nations_label.png](s04/shots/nations_label.png)

### 22. [polish] 'Tier: Camp (1 buildings)' and wrong chronicle line for a captured nation

- **Category:** text; **reported by** 1 agent(s): s04
- **Detail:** Settlement Info says '1 buildings' after the first Farm. The chronicle says 'Westerntown faded from memory before it ever built a home.' right after its village was captured.
- **Repro:** build first building (K K); or let the staged siege capture the home village
- **Screenshots:** [one_buildings.png](s04/shots/one_buildings.png)

### 23. [polish] Camp-era crafting locked text omits that a foreign town's Workshop works

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** Out in the world, Planks/Bricks say 'Locked: The Workshop is unlocked in the Village era.' But inside the AI Town of Bigport with no nation, the header reads 'which has a Workshop: every recipe is open' and planks are craftable. The locked hint could mention visiting a settlement with a Workshop.
- **Repro:** seed 107; walk to an AI town; E; C
- **Screenshots:** [craft_foreign2.png](s03/shots/craft_foreign2.png)

### 24. [polish] Confirmation wording differs between the Nations screen, the command bar and the L key

- **Category:** text; **reported by** 1 agent(s): s01
- **Detail:** Nations screen: the button becomes 'Confirm' but the status says 'Click Denounce again to denounce X'. Command bar: the button becomes 'Confirm Declare War on Greenshire' and the status says 'Click again to confirm: ...'. L: the status says 'Press L again to disband Greattown.' but the Leave Nation button does not change to a confirm state. Each one works, but the three styles do not match, and on the Nations screen the text tells you to click 'Denounce', a label that is no longer on the button.
- **Repro:** o; click Denounce (836,302); screenshot; Escape; press l; screenshot
- **Screenshots:** [den_confirm_ok.png](s01/shots/den_confirm_ok.png)

### 25. [polish] Full chronicle window overlaps the right column with translucent see-through text

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** Y (full history) opens a translucent window from x 160 to 800 that covers about 50px of the right column. Settlement/Market text shows through behind the history lines. 244 entries by day 1 dusk, nearly all AI-vs-AI rivalries, bury the player's own events.
- **Repro:** inside settlement press Y
- **Screenshots:** [chron_full_overlap.png](s04/shots/chron_full_overlap.png)

### 26. [polish] Inventory box jumps between the left edge and mid-screen as command rows come and go

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** Inventory Info sits at x=10 (shots/h2.png) or at x=220 over the scene (shots/h3.png) depending on how many command rows are showing. The Energy bar also overlaps the bottom of the Season box inside settlements.
- **Repro:** Enter your settlement with and without pawns nearby
- **Screenshots:** [h3.png](s02/shots/h3.png)

### 27. [polish] No-stalls refusal says why but not what to do

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** Sell Resources in Bigport (0/8 stalls) shows 'This settlement does not have any stalls.' That's clear, but there's no pointer to a settlement that has stalls or to building one.
- **Repro:** seed 107; enter the first AI settlement early (before its stall is built); click Sell Resources
- **Screenshots:** [sell_nostalls2.png](s03/shots/sell_nostalls2.png)

### 28. [polish] Objective 7 progress bar text is clipped on the left ('nbers 6/10 · Buildings 2/2')

- **Category:** text; **reported by** 1 agent(s): s02
- **Detail:** The progress text 'Members 6/10 · Buildings 2/2' is wider than the objective box and shows up as 'nbers ...'.
- **Repro:** Reach Objective 7 (Reach the Village era)
- **Screenshots:** [objective_clipped.png](s02/shots/objective_clipped.png)

### 29. [polish] Season box sits flush on Player Info with no gap when the market box is long

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** With 2 merchants (or merchants plus 7 prices) the compact Season box is drawn down from the market box and touches the Player Info title bar, with no 8px gap. The other boxes keep the gap.
- **Repro:** enter a settlement whose market lists 2 merchants
- **Screenshots:** [touching_boxes.png](s04/shots/touching_boxes.png)

### 30. [polish] World nameplates and the settlement label draw over HUD panels

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Large pawn nameplates ('Kay Smith / Loyal, Industrious / (going home)') render on top of the Objective panel and the red 'Collapsing!' timer, making both unreadable. The 'Town of Greenshire / N here, 16/32 residents' label also overlaps the status banner and shows through the Nations screen.
- **Repro:** stand in Greenshire's town with pawns walking past the camera; screenshot
- **Screenshots:** [l2.png](s01/shots/l2.png)

## Objective timings

Game seconds from the start of the world to each objective (event tick / the game's ticks per second, from events.jsonl). Game time, not wall time: with many sessions on one machine a world can run slower than real time, and wall-clock timings would mostly measure the machine.

| # | Objective | s01 | s02 | s03 | s04 | min | median | max |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Gather wood | - | 3631 | 26 | 58 | 26 | 58 | 3631 |
| 2 | Eat something | - | 3632 | 27 | 59 | 27 | 59 | 3632 |
| 3 | Create or join a nation | - | 3633 | 56 | 60 | 56 | 60 | 3633 |
| 4 | Found a settlement | - | 3725 | 210 | 182 | 182 | 210 | 3725 |
| 5 | Enter your settlement | - | 3731 | 219 | 184 | 184 | 219 | 3731 |
| 6 | Build a market stall | - | 3733 | 220 | 200 | 200 | 220 | 3733 |
| 7 | Reach the Village era | - | - | - | 201 | 201 | 201 | 201 |

