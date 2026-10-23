# Playtest summary: 20261022-1802

12 sessions, build beyond-nations 0.5.0 (3246ba2), beyond-nations 0.5.0 (ae87506), generated 2026-10-22 23:18.

**Findings:** 95 reports in 83 groups -- 1 blocker, 17 major, 54 minor, 23 polish.

## Outcomes

| Session | Goal | Outcome | Fun | Minutes | Objectives | Reached | Deaths | Findings | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| s01 | Play the first 10 minutes as a brand-new player who has never seen the game; note every moment of confusion. Also found a settlement early and, if the game refuses ('too close' or similar), check that it tells you a distance and direction (issue #349). | achieved | 3 | 22.8 | 6 | - | 0 | 10 | The opening is well paced: objectives, tips and the 'need N more wood' messages carried a total beginner from the title screen to a settlement with a stall, a farm and two houses in about 5 game minutes. The #349 founding hint works, though it needs a compass to be usable. After that the player stalls at 'Members 4/10' with no clear way to recruit. Meanwhile the AI world founds 33 nations, makes every neighbour a rival and floods the chronicle, and the 960x540 HUD overlaps and clips once you are inside a settlement. |
| s02 | Reach the Village era as fast as you can (it cannot come before day 3, about 17 minutes of game time); record the game time of every objective. Also note whether trees near spawn are gone before you reach them (#351) and how long you wait idle for the day-3 gate once every other requirement is met (#352). | blocked | 3 | 22.7 | 6 | - | 0 | 8 | The opening is quick and readable. A nation, a settlement and 2 stalls were done by 151 s, and a village settlement by 212 s, with good status messages and a clear era checklist. After that the 10-member requirement stalls the run: no immigrants or births arrived, and members could only come from paid poaching. Then a NullReferenceException in the merchant behaviour (PawnBehaviorCalculator:223) crashed the game at tick ~25700 and crashed it again after loading the autosave. Village was never reached, so the #352 day-3 idle wait could not be measured. |
| s03 | Survive the first winter. Start with `--season autumn` (a fresh world otherwise reaches winter only after about 57 minutes of game time). Note how food and cold feel. Watch NPC food in your settlement: do residents starve, is there any warning, and can you feed them (#353)? | partial | 3 | 48.9 | 8 | Village | 0 | 8 | Getting to a settlement is quick and well signposted: the distance hint ('open ground about 220 to the south-east') is good, NPCs fill the town fast, and the world events are flavourful. The winter does not hold up. For the player it is trivial: no drain in town, and apples are auto-eaten. For NPCs it is invisible: no food stock, no warning, no way to feed a resident. People start starving 'within the walls' on winter day 1, and the only sign is a context-free '<name> has died.' toast. Only about 1.2 of the 2 winter days fit in the budget because the game ran slowly, so whether my own town survives the whole winter is untested. |
| s04 | Diplomacy and war: lead a nation, meet a neighbour, try alliance, rivalry and war from the Nations screen (O). Re-verify recent fixes: a new nation should not be raided or turn hostile in its first days without a shown reason (#346); the Nations screen must scroll to every nation (#347); standing on a neighbour's land, the command bar should offer War/Peace and not offer an alliance to a Hostile nation (#348). | achieved | 3 | 16.7 | 5 | - | 0 | 9 | Diplomacy reads clearly. Refusals say what is needed ('an alliance needs +30. Gifts help'), and Tribute → Ally → Denounce → War → Peace all worked. #346, #347 and #348 held: scroll reached every nation, hostile land offered Peace and no alliance, and Fortburg was not turned on without a reason on day 1. The biggest risk is the Nations screen re-sorting under the pointer with no confirmation on War, which makes it easy to declare war on your own ally. Tribute you receive also never shows up anywhere. |
| s05 | Trade and caravans: build a market stall, buy and sell, get a Storehouse and send a caravan. Note why any Sell attempt fails and whether the message says so (#354), and whether the game tells you before the Town era that Storehouse/caravans exist (#355). | partial | 3 | 15.6 | 6 | - | 0 | 7 | Building a stall, buying and selling all work. The trade loop feels broken early because markets start nearly broke, so a sell only works after the player pays the stall first, and the failure message (#354) blames 'no buyers or not enough resources' when the stalls just have no coins. Caravans are now clearly signposted from the Camp era (#355 fixed), but the Storehouse sits behind a Day-7 Town era that a single session cannot reach. |
| s06 | Ruler, laws and heirs: lead a nation, enact and repeal laws, and see what happens when the ruler dies. Also try talking to, gifting and inviting pawns standing next to you inside your own settlement (#344), use teleport home and check you land within reach of your people, and look for any screen that shows your nation's members, loyalty and heir (#358). | partial | 3 | 14.2 | 6 | - | 0 | 10 | Laws work and are readable: proclaim, countdown, take effect, show up in the chronicle, and repealing works too. Talk, gift and invite now work at home through buttons and G/R, and teleport home lands you among your people. But the reign itself is invisible: no screen names your members, their loyalty or your heir. Ruler death only comes from a day of starving that auto-eating prevents, so succession is out of reach in a normal session. 'Stranger' labels on your own members make the nation feel less like yours. |
| s07 | Crafting: find and use every recipe on the crafting screen (C); are tools worth making? Note whether the crafting screen's directions (e.g. building a Workshop) are actually possible at your era (#356). | partial | 3 | 10 | 5 | - | 0 | 5 | Tools are clearly worth making: an axe costs 4 wood and 2 stone and returns about +60 wood (+3 per tree for 20 trees), and a pickaxe returns +30 stone. That makes them an obvious first craft rather than a real choice, and the feedback ('+3 from your axe, 19 uses left') is excellent. Planks and Bricks cannot be made in the Camp era. The crafting screen now says so clearly once you lead a nation (#356 is mostly fixed), but not before you join one. Cooked Meat is undercut by auto-eat, which eats the raw meat before you can cook it and wastes the cooked meal at 89 energy. |
| s08 | World events: play until world events are offered, take different choices, and judge whether the consequences are clear and fair. | partial | 3 | 26.5 | 6 | - | 0 | 5 | The world event sheet itself is good: it pauses the world, explains the situation in a sentence, and each option states its cost and effect, with unavailable options greyed and explained. But the first event took about 10 game minutes (26 real minutes here), so only one choice fit in the session and comparing choices was not possible. After a choice, the result message drops the concrete numbers it promised, so the player cannot check that the outcome was fair. Pace the first event earlier and have the result toast restate the effects. |
| s09 | Accessibility and text audit, touching only what a mouse-and-keyboard player would: every screen and panel; cut-off, overlapping, unclear or misspelled text; contrast; anything only explained on hover. Pay special attention to: the HUD right column inside a settlement (Season panel vs Player Info, 'Next (K)' cost cut off, #350); whether command-bar buttons ever shift under the cursor between hover and click (#345); and whether drastic actions (war, raid, leaving a nation) ask for confirmation. | achieved | 3 | 11.7 | 6 | - | 0 | 13 | The confirmation system for drastic actions exists but has gaps: Nations-screen War and the L key skip it, and NPC-driven reflows of the command bar reset or retarget it, so #345 is not fixed. The right column still overflows inside a settlement (#350), hiding Energy and the Hostile land warning. The content is good (era checklist, crafting screen, founding guidance), but the HUD needs fixed anchors and solid backgrounds before it is accessible. |
| s10 | Save/load and share-seed round trip: save from the pause menu, stop, `pt start --continue`, confirm the world is identical; then start a new world from the seed shown on the Share screen and confirm it matches. | achieved | 3 | 6.9 | 0 | - | 0 | 4 | Save/continue is solid. Save gives clear feedback ('Saved: Day 1 - no nation, 18:06'), Continue says 'Welcome back', and the loaded world matches the save exactly, down to entity IDs, inventory, position and heading. Two continues even replayed the same events at the same ticks. The Share screen's seed and link (with 'Link copied' feedback) reproduce the starting map exactly. But the nation history diverges as soon as the sharer does anything, so a friend will not see 'Upperfield' or 'Greenfield'. Decide whether sharing a world should also share its history. |
| s11 | Survive the first winter on the build that now includes #340 (pawn energy capped at a full bar, like the player's). Start with --season autumn. Found a settlement early, then watch NPC food through winter: count NPC hunger deaths (in your settlement and elsewhere, from the chronicle and events), note any warning before deaths, and whether you can feed residents (#353). Also note whether pressing E at a tree you face chops it even with pawns standing nearby (#376). To save real time, press Page Down a few times to shorten render distance if the world runs slowly. | partial | 3 | 44.2 | 6 | - | 0 | 9 | Early play feels good. The founding hints, the world events and the busy chronicle make the world feel alive, and founding by the start of autumn's second half is easy. Winter is brutal for NPC towns: 16 starved inside settlement walls across 7 nations, mostly in the last third of winter, and no warning came first. Meanwhile the player eats winter apples off trees and never feels the season. The own-settlement food goal (#353) could not be tested, because no NPC residents arrive in the first season, and #376 could not be set up, because pawns clear trees near towns. |
| s12 | Re-verify the merchant crash fix (#367). Found a nation and settlement, set Borders Open (O > Laws), then go to a neighbouring settlement and recruit (gift with G until Loyal, then invite with R) at least one pawn that keeps a market stall there (a merchant); keep playing in and around both settlements for at least 5 game minutes afterwards, then save from the pause menu, pt stop, and pt start --continue and play 3 more game minutes. Report any crash (check pt status and the game log). Also note whether E at a tree you face chops it even with pawns nearby. | partial | 3 | 25.2 | 6 | - | 0 | 6 | Recruiting an existing merchant (Abigail Watson, Lowerville) and then saving and continuing did not crash, and she survives the reload as a Westshire merchant, so #367 looks fixed, though the soak was much shorter than asked. The recruit loop is costly and opaque: most chronicle stall-builders are nation leaders who refuse R only after you have spent ~150 coins. Only the Market Info 'Merchants' list inside a settlement tells you who is a recruitable merchant, and pawn labels like '(Stranger) - Loyal' confuse more than they explain. |

## Findings

### 1. [blocker] NullReferenceException in PawnBehaviorCalculator:223 (merchant without a stall) crashes the game; the autosave crashes again

- **Category:** crash; **reported by** 1 agent(s): s02
- **Detail:** At tick ~25700 (day 2, night) the game window vanished: NullReferenceException at PawnBehaviorCalculator.computeBehaviorTypeInSettlementByDay line 223. In the MERCHANT branch, market.getStall(pawn.getId()) returns null and its result is used with no null check (lines 75 and 125 do check for null). It happened while I stood in Cityfield's settlement gifting coins to its members, about 2,700 ticks after I poached Oscar Jones from Cityfield with R. Which pawn triggered it is not proven: a merchant visiting a settlement where it has no stall, or a poached ex-stallholder, would both fit. 'pt start --continue' loaded the tick-24000 autosave, and the game crashed again at tick ~25748 with the same stack trace while I stood still, so the save cannot be played. Progress from the run was lost. Crash reports are in crash1.txt and crash2.txt.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 102; N at spawn; chop ~13 trees; walk NW and F to found; enter, craft axe/pickaxe, collect stockpile, K K (farm), K K (house); O > Laws > Borders Open; walk to Cityfield (settlement ~260m away), gift Oscar Jones coins (G) until Loyal, R; walk to Cityfield's other settlement and gift its members; crash ~tick 25700. Then: pt start ... --continue and wait ~1700 ticks for the same crash.
- **Screenshots:** [after_crash_continue.png](s02/shots/after_crash_continue.png)

### 2. [major] #354 still open: Sell Resources failure message never says the stalls cannot afford to buy

- **Category:** text, ux; **reported by** 3 agent(s): s01, s05, s06
- **Also reported as:** "Sell Resources fails with an either/or message despite stalls and listed prices"; "Sell Resources failure message does not say why"; "Sell Resources picks the items itself and the message never says what was sold"
- **Detail:** In my own settlement (4/4 stalls, Market Info 'Total Coins: 1') holding 31 apples, 14 saplings, 14 stone and 1 wood, Sell Resources said '! No one is buying resources at this market or you do not have enough resources to sell.' I plainly had resources; the real reason (from the Market Info panel and Market.sellResources) is that no other stall has enough coins to pay. Same at the rival Village of Whiteport (Total Coins 0, tick 12926). Expected a specific reason such as 'The stalls here only hold 1 coin and cannot pay for anything you carry.' The buy side has the same either/or wording: Purchase Food with 437 coins said 'No one is selling food at this market or you do not have enough money' (tick 8246).
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 105; gather 50 wood; N; F away from others; E to enter; wait for a pawn stall; click Sell Resources (625,475) while Market Info shows Total Coins 0-1
- **Screenshots:** [sell_poor.png](s05/shots/sell_poor.png) [sell.png](s01/shots/sell.png) [sell_wood.png](s05/shots/sell_wood.png) [sell_fail.png](s06/shots/sell_fail.png)

### 3. [major] #345 still present: pawn-targeted command rows appear, vanish and retarget under a still cursor

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** With the pointer held at (100,414) and no input, the button under it changed over 18 s from 'Talk to Charlotte Johnson' to 'Talk to Abigail Evans'. An Order/Appoint row disappeared and 'Send Caravan' moved from y=383 to y=414 and back. Earlier, when a pawn walked into reach, a Talk/Gift/Order row was inserted and pushed 'Send Caravan' up. A click can land on the wrong pawn or the wrong action. Before/after shots: shots/shift1.png and shots/shift6.png.
- **Repro:** inside own settlement with pawns wandering nearby: pt move 100 414; take a shot every 3 s
- **Screenshots:** [shift_after.png](s09/shots/shift_after.png)

### 4. [major] #350 confirmed: right column inside a settlement clips 'Next (K)' cost and Season panel covers Player Info

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** Settlement Info shows 'Next (K): Farm, 25 wood, 10', cut at the screen edge so the stone count is lost (also 'Workshop, 15 wood,' in Townfield). Season (y~432) overlaps Player Info: Energy is hidden, and 'Role: LEADER' and 'Land: Townfield (Hostile)' peek out underneath. Once Market Info grows a 'Merchants:' list, Season is pushed to y=466 and runs off the bottom, and Player Info is entirely off-screen.
- **Repro:** found a settlement, press E to enter; pt shot
- **Screenshots:** [inside.png](s09/shots/inside.png)

### 5. [major] #358 not met: no screen shows nation members, their loyalty, or the heir

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** Nations tab (O) shows only 'Easternland, 5 members, 1 camp. You lead it.'; Laws tab shows laws, defence and tax totals; Settlement Info shows 'Residents 5/16' and a Merchants list. Nowhere lists who the members are, their loyalty to me, or who would succeed me. The High tax description says 'loyalty falls fast', yet loyalty is never visible, so the trade-off cannot be judged. Expected a members/court view with loyalty and the named heir (Succession.chooseHeir already computes one with a reason).
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 106; space; Start; Begin; n; gather 50 wood, f to found; o; click Nations and Laws tabs
- **Screenshots:** [no_heir_screen.png](s06/shots/no_heir_screen.png)

### 6. [major] Click-twice confirmation resets when the command bar reflows, so 'Confirm' clicks only re-arm

- **Category:** bug; **reported by** 1 agent(s): s09
- **Detail:** Build Farm then Confirm Build Farm, clicked three times in the same spot, only gave 'Click again to confirm: Build Farm. (x3)'. Nothing was built and no shortage was reported (K reported 'Need 2 more wood'). Declare War on Townfield did the same thing ('(x2)') when a pawn row appeared between the two clicks. With a still layout, two clicks did confirm. Either the arm state is lost on reflow or the button is enabled when the action is unaffordable.
- **Repro:** inside own settlement with Wood < 25: pt click 386 445 (Build Farm) three times
- **Screenshots:** [farm3.png](s09/shots/farm3.png)

### 7. [major] Declaring war from the Nations screen (O) takes effect on a single click with no confirmation

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** On the Nations tab one click on 'War' next to Blackport declared war at once (relation -13 to -60, 'at war'). The same action in the command bar ('Declare War on Townfield') asks for a second click, and so do Leave Nation and Build Farm. I expected the Nations screen to use the same confirmation.
- **Repro:** pt start --seed 109; create nation (N); press O; pt click 657 218 (War on first row)
- **Screenshots:** [war1.png](s09/shots/war1.png)

### 8. [major] Early markets hold almost no coins, so selling only works after you pay the stall first

- **Category:** balance; **reported by** 1 agent(s): s05
- **Detail:** Every market I visited on Day 1 showed Total Coins 0-1, so Sell failed. At Whiteport the sell failed (tick 12926). I bought one apple for 27 (tick 12954) and then the sell worked for 25 (tick 13046). So the only way to sell was to fund the stall with my own coins first, which loses money. Prices also fell hard within one day: apples 42/37 -> 8/8 and wood 63/56 -> 11/11 between ticks 7694 and 17318. Trade does not earn money early in the game.
- **Repro:** Enter Whiteport (or any market with Total Coins 0); Sell Resources (fails); Purchase Food; Sell Resources (succeeds)
- **Screenshots:** [foreign_sell.png](s05/shots/foreign_sell.png)

### 9. [major] First player world event arrives very late (tick 30554, about 10 game minutes); only one could be seen in a 30-minute session

- **Category:** balance; **reported by** 1 agent(s): s08
- **Detail:** Played normally from a new world (nation at tick 1402, settlement at 8302, house+farm by 12638) and then waited. No world event sheet appeared until tick 30554 (611 game seconds; ~26 real minutes at the 15-28 tps this machine gave). The first AI-nation world event in the chronicle was at tick 19754 (bountiful harvest in Westernland). With the next player event 10-15 game minutes later, a player sees one choice per ~quarter hour, so comparing consequences of different choices is not possible in a normal session. Expected the first event within the first few game minutes after founding a settlement, to introduce the system.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 108; space; click Start, Begin; gather 10 wood, N, gather 50 wood, F in open ground, enter settlement, K twice; wait until state.worldEvent is non-null (tick 30554)
- **Screenshots:** [event1.png](s08/shots/event1.png)

### 10. [major] First winter starves 16 NPCs across 7 nations, all 'within the walls'

- **Category:** balance; **reported by** 1 agent(s): s11
- **Detail:** Winter ran from tick 171002 to 231003. The first hunger death came at tick 196582 (Stan Thompson of Fortwood), 8.5 game minutes in. Deaths then sped up: 16 in total (Upperville 5, Fortwood 4, Fortresston 2, Yellowshire 2, Forttown, Blueburg and Southernville 1 each), and 10 of them came in the last 3 game minutes of winter. Every one died 'within the walls' of an established settlement, even though #340 is in this build. Settlements with farms still starved, so NPC winter food planning looks too weak. Expected: few or no deaths in towns with farms, or deaths spread out after visible shortages.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 103 --season autumn; play through to tick ~231000; grep -iE 'starv\|hunger' out/events.jsonl
- **Screenshots:** [winter_end.png](s11/shots/winter_end.png)

### 11. [major] L key leaves/disbands the nation instantly, bypassing the Leave Nation button's confirmation

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** The red Leave Nation button needs a second click ('Click again to confirm: Leave Nation.'). Pressing L once disbanded the nation straight away ('You disbanded nation Bigland.') with no prompt. Repeated cleanly: N, wait 8 s, then a single L.
- **Repro:** pt key n; pt wait 8; pt key l; pt state (nation:null)
- **Screenshots:** [leave_key_single.png](s09/shots/leave_key_single.png)

### 12. [major] NPCs starve in winter with no warning and no visible food/hunger state for settlement residents

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** From winter day 1 NPCs die of hunger (Fiona Robinson t195253, Nancy Williams t199797, Samantha Brown t200565, Jasmine Taylor t205649), several 'within the walls' of towns. Nothing before the deaths warned of hunger. The top toast only says '<name> has died.' with no nation or cause, so I could not tell whether it was one of my residents; only the truncated chronicle line says 'starved within the walls'. Settlement Info (Nation, Leader, PCIS, Funds, Tier, Residents) shows no food stock, and talking to residents never mentions hunger. Expected: a settlement food stock and a 'residents are going hungry' warning before anyone dies, and death toasts that name the nation and cause.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 103 --season autumn; play to a settlement (N, gather 50 wood, F, E); wait inside the settlement until winter day 1 night (~tick 195000-206000); watch status toasts and chronicle
- **Screenshots:** [deaths.png](s03/shots/deaths.png)

### 13. [major] Nations screen re-sorts rows instantly after an action, leaving the pointer on War/Denounce for your ally

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** Declaring war on Greatshire while scrolled to 'Nations 2-5' moved Greatshire to the top, out of view, and the pointer was left on 'War' for ally Castleton. The same happened after Denounce, which left the pointer on 'Denounce' for the ally. War has no confirmation, so a double-click, or a click that lands late at 5-7 fps, declares war on your ally. Expected: rows stay put until the screen is reopened, or War asks for confirmation.
- **Repro:** pt start --seed 104; Begin; N; walk to Castleton land; O; Tribute Castleton x3, Ally; Denounce Greatshire (row 2) and note the pointer is now over Castleton Denounce; later scroll Down 1 and click War on Greatshire (row 1); the pointer is now on Castleton War
- **Screenshots:** [truce_war.png](s04/shots/truce_war.png)

### 14. [major] No warning before NPC hunger deaths; pawn hunger is not shown on screen

- **Category:** ux; **reported by** 1 agent(s): s11
- **Detail:** The only food warning in the whole run was a generic status line at tick 141002: 'Winter comes tomorrow. Lay in food while the farms still yield.' No status line, toast or chronicle entry said that any settlement was short of food or that any pawn was starving before a death. Deaths show up only as chronicle lines mixed in with raids and stall-building. Pawn energy and hunger never appear on screen. A player can only learn that a pawn is hungry by talking to it, and G (gift) picks the item by itself: a pawn that is not hungry got 10 coins, not food, even though I had 30 apples. Expected: a famine or low-food warning per settlement, and a way to see who is hungry.
- **Repro:** same run; watch status and chronicle between tick 171002 and 196582; walk to a pawn, press e then g
- **Screenshots:** [death1.png](s11/shots/death1.png)

### 15. [major] No way to feed a resident: Gift (G) only ever gives 10 coins

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** In winter I tried to feed residents of my settlement. G / 'Gift <pawn>' gave 10 coins ('Coins? That's generous of you.') with no option to give apples, even though I carried 19. 'Purchase Food' buys food for the player from a merchant. The world events ('Welcome them all (4 food)', 'Share a meal (1 food)') take food from the player's own apples, but outside those events I found no way to put food toward residents (#353).
- **Repro:** pt start ... --season autumn; found and enter a settlement; in winter stand near a resident and press g (or click 'Gift <name>')
- **Screenshots:** [giftcoins.png](s03/shots/giftcoins.png)

### 16. [major] Ruler death and succession cannot be reached in a normal session

- **Category:** balance; **reported by** 1 agent(s): s06
- **Detail:** The only way the leader dies is starving: energy drains about one full bar per in-game day (10 game minutes, about 20 real minutes at the ~25 tps seen here), the player auto-eats any carried apples (24 from chopping trees), and Sell Resources refuses to sell them, so food cannot be shed. Energy stayed 91-96 the whole run. I could not see what happens when the ruler dies, and with no heir shown beforehand a player cannot plan for it either.
- **Repro:** As above; play 15 min; watch Player Info energy; inside settlement click Sell Resources
- **Screenshots:** [no_death_path.png](s06/shots/no_death_path.png)

### 17. [major] Storehouse and caravans cannot be reached in a 30-minute session: Town era needs Day 7

- **Category:** balance; **reported by** 1 agent(s): s05
- **Detail:** Send Caravan needs a Storehouse, which unlocks in the Town era. The Era box shows Village needs Day 3, and Town needs Day 7, 18 members, 2 settlements and 1000 funds. At the 15-21 ticks/s I saw, a game day is about 8-10 real minutes, so caravans are about an hour or more away. I could not build a Storehouse or send a caravan. Meanwhile an AI settlement (Whiteport) already showed 'Town (5 buildings)' with 'Next (K): Storehouse' on Day 1 while the world era was Camp.
- **Repro:** As above; enter own settlement; click the Era box (110,58); click Send Caravan (Town era)
- **Screenshots:** [era_panel.png](s05/shots/era_panel.png)

### 18. [major] Village needs 10 members, but no newcomers or births came in ~15,000 ticks and the world splits into dozens of 1-7 member nations

- **Category:** balance; **reported by** 1 agent(s): s02
- **Detail:** I had a Village settlement (2 buildings), 2 stalls and later 3 houses' worth of room (4/16 residents) from tick ~10600. The borders were Open from tick 16003. The member count still stayed at 4 until I poached a pawn. The game log shows no 'newcomer arrived' line at all up to the crash, and no births were recorded. The Nations screen listed 37 nations of 1-2 members at tick ~14000, then 29 as they disbanded. Pawns cannot be recruited from 1-member nations because they are leaders, and other members need Loyal (30 relationship = about 150 coins of gifts each, chasing a pawn that keeps walking off). At that cost, 6 more members is about 900 coins; I had 473. Pawn-led nations reach 'town' with 2-7 members, while the player needs 10 for a village. Members was the requirement that held me back, not the day-3 gate.
- **Repro:** As above up to the Village settlement; set Borders Open; stand in the settlement from tick ~11000 to ~18000 and watch Members stay 4/10 (pt state objective.progress; pt log \| grep -i newcomer).
- **Screenshots:** [nl1.png](s02/shots/nl1.png)

### 19. [minor] HUD panels overlap and clip at 960x540 once inside a settlement

- **Category:** text, ux; **reported by** 4 agent(s): s01, s05, s06, s08
- **Also reported as:** "HUD clipping and overlap inside settlements"; "Inside a settlement, Season panel overlaps Player Info and Settlement Info text is cut off"; "Settlement HUD overlap: floating 'N here, x/y residents' label and Season over Player Info"
- **Detail:** Inside the settlement the Inventory panel jumps to the middle of the screen over the 'Objective complete' popup. The toast gets cut to 'Objective com'. The objective progress bar reads 'bers 2/10 - Buildings 0/2' and its description runs under the action buttons. 'Next (K): Farm, 25 wood, 10...' is cut off. The Season panel covers Player Info. The giant floating settlement label ('...4/16 residents') is clipped at the top, shows through modals, and near the well covers a third of the screen.
- **Repro:** after founding: pt key e (enter), pt key b (build stall); screenshot
- **Screenshots:** [hud_overlap.png](s01/shots/hud_overlap.png) [nametag_overlap.png](s05/shots/nametag_overlap.png) [hud_overlap.png](s06/shots/hud_overlap.png) [overlap.png](s08/shots/overlap.png)

### 20. [minor] 'Build a market stall' objective completed by an AI pawn's stall, not the player's

- **Category:** bug, ux; **reported by** 3 agent(s): s02, s05, s08
- **Also reported as:** "'Build a market stall' objective completes when an NPC builds a stall in your settlement"; "The 'Build a market stall' objective completed when a pawn built a stall, not me"
- **Detail:** Objective 6 completed with +50 coins at tick 7802 when pawn Wendy Williams built a stall in my settlement. I had 16/20 wood and 0/5 stone and had not built anything. I built my own stall later (tick 11306), which gave no extra feedback. Expected the objective to need the player's own stall, or wording like 'Get a market stall going'.
- **Repro:** Found a settlement, enter it with fewer than 20 wood; wait for a resident pawn to build a stall
- **Screenshots:** [stall_objective_free.png](s05/shots/stall_objective_free.png) [entered.png](s08/shots/entered.png) [afteraxe.png](s02/shots/afteraxe.png)

### 21. [minor] Found-settlement refusal gives compass directions but the HUD has no compass

- **Category:** ux; **reported by** 2 agent(s): s08, s11
- **Also reported as:** "Settlement-founding hints give compass directions, but nothing on screen shows a compass"
- **Detail:** Pressing F too close to another settlement says e.g. 'Open ground lies about 210 to the east', then '161 to the north', then 'south-east', 'north-west'. There is no compass, minimap or heading indicator on screen, so a player has to guess which way east is. It took about 7000 ticks (roughly 2.3 game minutes) of trial walking to find valid ground because the world near spawn is crowded with AI settlements. Expected a compass strip, a marker, or a relative direction ('ahead/behind/left').
- **Repro:** Seed 108; after creating a nation with 50 wood, press F at (-150,253) and follow the hint
- **Screenshots:** [nocompass.png](s08/shots/nocompass.png) [nocompass.png](s11/shots/nocompass.png)

### 22. [minor] Settlement action buttons reflow as pawns move, so two-click confirm actions misfire

- **Category:** bug, ux; **reported by** 2 agent(s): s03, s06
- **Also reported as:** "Action bar rows shift as pawns wander in and out of range, so clicks land on the wrong button"
- **Detail:** The button row gains or loses 'Order/Talk/Gift <pawn>' buttons whenever the nearest pawn changes, so Build Farm/Build House move between the first click and the confirm click. My second click on 'Confirm Build Farm' re-armed the confirm instead of building ('Click again to confirm: Build Farm. (x2)'). A Gift click also landed on empty space after the pawn walked off.
- **Repro:** enter a settlement with pawns milling around; click Build Farm, then click the same spot again to confirm
- **Screenshots:** [reflow.png](s03/shots/reflow.png) [button_shift.png](s06/shots/button_shift.png)

### 23. [minor] #344 partly: E inside/at own settlement exits or enters it instead of talking to the adjacent pawn

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** Inside my settlement with member Judy Taylor 2.4-3 m away, pressing E printed 'Exited settlement.' (tick 5994). The settlement always wins the E priority, so the documented talk key never reaches pawns at home. The on-screen Talk / Gift / Order buttons, plus G (gift) and R (invite), do work: talk gave a line, and gift took 10 coins with 'Coins? That's generous of you.' The player cannot move inside the settlement, so you must wait for a pawn to wander within 5 m.
- **Repro:** Found and enter settlement; pt walkto pawn; pt key e
- **Screenshots:** [e_exits_not_talk.png](s06/shots/e_exits_not_talk.png)

### 24. [minor] #351 confirmed: the three trees nearest spawn are gone within ~19 game seconds, before the player can move

- **Category:** bug; **reported by** 1 agent(s): s02
- **Detail:** At tick 11 the nearest trees were 21.2m, 33.5m and 42.4m from spawn (0,0). At tick 955 the player had not moved, and all three were gone: the nearest tree was now 54.1m away. Every pawn was over 100m away at the start, so it is unclear what removed them. The first chop then needs a ~55m walk, and trees in the Moor start area are sparse (~4 wood each without an axe).
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 102; space; Start; Begin; pt state (note nearest trees); dismiss tip; pt state again ~20 game s later
- **Screenshots:** [trees_gone.png](s02/shots/trees_gone.png)

### 25. [minor] #376 not verified: could not get a pawn within reach of a tree

- **Category:** ux; **reported by** 1 agent(s): s11
- **Detail:** Over 4 polling loops (more than 50 checks) I never had a pawn within 5 units of the tree in front of me. Pawns cleared the trees near towns quickly; trees in reach vanished as pawns chopped them. Every E press at a tree I faced chopped it (no pawn nearby), so the regression case is still untested.
- **Repro:** walkto tree near Smallport/Yellowshire; poll pt state for pawn <5m
- **Screenshots:** [gift2.png](s11/shots/gift2.png)

### 26. [minor] 'Open ground lies about N to the north/south' gives contradictory directions

- **Category:** text; **reported by** 1 agent(s): s06
- **Detail:** Pressing F inside Smallshire's land said 'Open ground lies about 260 to the north.' After walking about 190 in that direction (-z), F said 'Open ground lies about 70 to the south.' Continuing the same way worked. One of the two compass words is wrong.
- **Repro:** Seed 106: chop to 50 wood near x312 z-60; f; face to yaw ~168; hold Shift+w 8 s; f
- **Screenshots:** [founded.png](s06/shots/founded.png)

### 27. [minor] AI nations claim the land and turn into rivals before a new player can found anything

- **Category:** balance; **reported by** 1 agent(s): s01
- **Detail:** Around 7 AI settlements existed by tick ~6000, and my first two F presses were refused because AIs had taken the spot (Southernwood was founded on the exact spot the first hint pointed to, about 100 ticks before I got there). By about 6 game minutes there were 33 nations, and all four of my neighbours were 'Rival (-39)' toward me without my having dealt with any of them. The chronicle and toasts fill with raids and nations falling, all about places I have never seen.
- **Repro:** seed 101; play normally to tick ~17500; press O
- **Screenshots:** [nations.png](s01/shots/nations.png)

### 28. [minor] At war, the enemy settlement can be entered and traded with; no Raid command exists

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Right after declaring war I entered Townfield. It offered Purchase Food and Sell Resources as if at peace. No Raid command appeared anywhere, and nothing explains how to raid.
- **Repro:** declare war via command bar near Townfield; press E
- **Screenshots:** [enemy_inside.png](s09/shots/enemy_inside.png)

### 29. [minor] Auto-eat consumes raw chicken meat before it can be cooked, and eats cooked meat at 89 energy, wasting most of it

- **Category:** balance; **reported by** 1 agent(s): s07
- **Detail:** I harvested chicken meat at energy 90.2 (tick 2882), then opened C. By tick 3018 the meat was gone, energy was 99.9, and the Cooked Meat row read 'Need 1 more chicken meat'. Player.cs eats the most nourishing food once energy drops below 90, so raw meat (25) is eaten ahead of the 8 apples I carried. Cooking only worked when I harvested at about 98 energy and pressed C straight away. The cooked meat (45) I made was then auto-eaten soon after energy dipped under 90, capping at 100 and wasting about 35 energy. As a result Cooked Meat is hard to make and almost pointless. Expected: auto-eat to skip ingredients (or prefer apples), or a toggle, and to eat only when the meal is not wasted.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 107; play until energy is about 90; pt walkto chicken; pt key e; pt key c (Cooked Meat row says 'Need 1 more chicken meat'; pt state shows no chicken_meat and energy jumped)
- **Screenshots:** [cooked_meat_not_recognised.png](s07/shots/cooked_meat_not_recognised.png)

### 30. [minor] Before you have a nation, the crafting screen implies the Workshop can be built and leaves out that it is era-locked (#356 remainder)

- **Category:** text; **reported by** 1 agent(s): s07
- **Detail:** At tick 74, with no nation, the header read 'Planks and bricks need a settlement with a Workshop' with no lock note. The Planks and Bricks rows read 'Need 3 more wood, a Workshop (inside a settlement)', which suggests that gathering and building a Workshop is enough. Once I led a nation (Camp era), both inside and outside the settlement correctly said 'Locked: The Workshop is unlocked in the Village era.' Pressing K in a Camp settlement offers a Farm, never a Workshop. Expected: the same lock note in the nationless state, since the era is already Camp.
- **Repro:** pt start ... --seed 107; pt key space; click Start, Begin; pt key c
- **Screenshots:** [craft1.png](s07/shots/craft1.png)

### 31. [minor] Border rivals slide toward Hostile with no player action on day 1; an NPC nation raids under 2 game minutes after founding

- **Category:** balance; **reported by** 1 agent(s): s04
- **Detail:** #346 for the player nation held: through tick 16190 no nation turned Hostile to Fortburg or raided it, and each drop had a shown reason. But Oldham and Newfield went from -20 to -38/-39 by tick 16190, heading for Hostile within the first day. Between NPC nations, Northernford (founded tick 2702) became Castleton's border rival at 5502, then 'now hostile' with no reason on that line, and raided Castleton (the player's ally) at 8002.
- **Repro:** pt start --seed 104; play to tick ~16000; grep Northernford in out/events.jsonl; O and scroll to the top
- **Screenshots:** [nations_final.png](s04/shots/nations_final.png)

### 32. [minor] Build key confirm (K then K) lapses at low fps; the repeated prompt just counts up (x2, x3)

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** The first K shows 'Press K again to build a House'. A second K pressed after reading the prompt (~4 s wall at 3-5 fps) re-arms the gate instead of building, and the status becomes 'Press K again... (x2)', then '(x3)'. Only presses less than 1 s apart built anything. The ConfirmGate window is 4 s of wall time, and the prompt never says the window has expired.
- **Repro:** Inside own settlement with 20 wood/15 stone: pt key k; (wait ~4 s) pt key k -> prompt again; pt key k; sleep 0.6; pt key k -> House built
- **Screenshots:** [k_confirm.png](s02/shots/k_confirm.png)

### 33. [minor] Command bar reflows after Create Nation: red Leave Nation lands exactly under the cursor

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** After clicking Create Nation (66,517) the bar rebuilds. Leave Nation now fills that spot and the Inventory panel jumps. Exiting a settlement also reflows the bar (Craft Axe ends up near where Exit Settlement was). Inventory Info sometimes moves from the left edge to mid-screen (220,227) over the world and stays there.
- **Repro:** pt click 66 517; pt shot
- **Screenshots:** [created.png](s09/shots/created.png)

### 34. [minor] Could not test feeding residents (#353): own settlement never got any NPC residents

- **Category:** ux; **reported by** 1 agent(s): s11
- **Detail:** I founded a settlement at tick 129199 and built a farm, a stall and a house; it became a Village with 1/12 residents. No newcomers arrived through the whole winter. Recruiting needs a Loyal relationship (R gave "I'm loyal to Smallport." (needs Loyal)), which is far out of reach in the first season. The settlement panel's 'Purchase Food' button said 'No one is selling food at this market or you do not have enough money' even with 231 coins. No control or button feeds residents; 'Collect Stockpile (0)' stayed greyed out. So #353 could not be tested in a first-winter run.
- **Repro:** found a settlement and build a house (K twice); wait through winter; Members stays 1/10
- **Screenshots:** [purchase.png](s11/shots/purchase.png)

### 35. [minor] Directions given ('lies 180 to the west') but there is no compass or heading on the HUD

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** The founding failure message is helpful but gives compass directions. Nothing on screen shows which way is west or south, so I found a site by trial and error.
- **Repro:** with 50 wood press F near another settlement
- **Screenshots:** [compass.png](s09/shots/compass.png)

### 36. [minor] Drought 'Buy grain' resolution does not confirm the promised 12 apples or where they went

- **Category:** ux; **reported by** 1 agent(s): s08
- **Detail:** The Drought sheet itself is clear: each option states its cost and its effect (option 1 shows in red why it is unavailable: needs 15 wood and 10 stone; the farms are dry a quarter day instead of a whole one). After clicking 'Buy grain from traders (25 coins)', 25 coins left my personal purse (401 -> 376) but the status/chronicle line only says 'you bought grain from passing traders to see it through'. The '12 apples go into the stores' never shows: my inventory stayed at 31 apples, and nothing points at the settlement stockpile. Option 3 says 'your people grow restless with you' with no number or indicator of what that means. Expected the result toast to restate the concrete effects (-25 coins, +12 apples to Newport's stockpile, farms idle until day X).
- **Repro:** As above until the Drought sheet shows; click option 2 at (480,405); read the status bar and the inventory panel
- **Screenshots:** [after1b.png](s08/shots/after1b.png)

### 37. [minor] Expanded Era box pushes Objective panel into the command bar; objective progress text clipped

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** Tapping the Era box pushes the Objective panel down until 'Send Caravan' and 'Build Stall' are drawn over its description. The Objective 7 progress bar always reads 'nbers 3/10 - Buildings 0/2' (the start of 'Members' is clipped). 'Village 0/1' inside the box and 'Village: 1/4' in its header use the same word for different counts.
- **Repro:** after objective 6: pt click 110 58
- **Screenshots:** [era.png](s09/shots/era.png)

### 38. [minor] Finding open ground for F is a long hunt: each refusal points somewhere that is also too close to another settlement

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** With 59 wood I pressed F 7 times over about 3,300 ticks. The messages were: claimed by Castleburg, go 190 east; too close to Littleham, go 40 south; too close to Citytown, go 11 north-east; too close to Castleburg, go 20 east; and so on. Each hint accounts only for the nearest settlement. Expected: the hint to point to a spot that is clear of all settlements, or a map marker.
- **Repro:** pt start ... --seed 107; gather 50 wood; N; walk to about (395,212); press F repeatedly and follow the hints
- **Screenshots:** [after_f.png](s07/shots/after_f.png)

### 39. [minor] Found-settlement 'open ground' hint is overtaken by AI settlers and sends the player back and forth

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** F said 'Open ground lies about 40 to the north', then '20 to the north' (tick 5934). After walking about 16 north it said 'Too close to the Camp of Northernburg: 197 away... about 280 to the south-west' (tick 6158), because Northernburg was founded at tick 6050. A later hint said 340 south-east. It took about 1800 ticks of wandering to find a spot. The hints are useful, but the AI fills the map quickly.
- **Repro:** Seed 105; after 50 wood near (-250,-45), press F repeatedly and follow the hints
- **Screenshots:** [found_hint_flip.png](s05/shots/found_hint_flip.png)

### 40. [minor] Founding refusal names a direction ('to the north') but nothing on screen shows which way north is

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** #349 is fixed: pressing F inside Newburg's claim says 'This land is claimed by Newburg... Open ground lies about 250 to the north.', and near the Camp of Northernton it says 'Too close to the Camp of Northernton: 133 away, and settlements must be 250 apart. Open ground lies about 120 to the south-west.' Following the hint worked: the third F press succeeded. But the HUD has no compass, minimap or heading, so a player cannot tell which way 'north' or 'south-west' is. I only worked it out from state.json (north = +Z = yaw 0). The distance also has no unit.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 101; Start, Begin; chop ~10 trees to 50 wood; N; pt walkto settlement 90; pt key f
- **Screenshots:** [f_refused.png](s01/shots/f_refused.png)

### 41. [minor] Game speed drops to 12-20 tps under load; the Page Down render distance fix is not discoverable

- **Category:** perf; **reported by** 1 agent(s): s03
- **Detail:** With several sessions on the machine, tps sat at 12-20 (fps 3-5). Pressing Page Down six times took tps to about 28, and later to 40-50. Two in-game days of autumn plus winter take 40+ wall minutes even at full speed.
- **Repro:** pt start ...; pt status; pt key Next Next Next Next Next Next; pt status
- **Screenshots:** [lowrender.png](s03/shots/lowrender.png)

### 42. [minor] Gifting a nation leader to Loyal costs ~150 coins, then R refuses; nothing warns that they lead a nation

- **Category:** ux; **reported by** 1 agent(s): s12
- **Detail:** Many chronicle stall-builder lines name nation leaders (Quinn Jones, Fiona Davies, Wendy Johnson...). I gave Fiona Davies 15 gifts (150 coins, about half my money) until she was Loyal, then pressed R: 'I lead Lowerville. I won't abandon my people.' Neither the gift line nor the pawn's label says she leads a nation. Only the Settlement Info panel inside her settlement shows it. Gifting is also pricey overall: Stranger to Loyal took 13-15 coin gifts (~130-150 coins) per pawn. One apple to a hungry pawn jumped her to Acquaintance at once, which was a nice touch.
- **Repro:** seed 102; walk to Lowerville; walkto pawn Fiona Davies; G x15; R
- **Screenshots:** [recruit.png](s12/shots/recruit.png)

### 43. [minor] Inside a settlement, W/A/S/D do nothing and no message says why

- **Category:** ux; **reported by** 1 agent(s): s11
- **Detail:** After H (teleport home) and E (enter), the player cannot walk. The walkto commands timed out twice and the player stayed at the same x/z for over a minute, with no message that you must press E or 'Exit Settlement' first. Expected: movement exits the settlement, or a hint appears.
- **Repro:** h; e; hold w 3 -> position unchanged
- **Screenshots:** [stuck.png](s11/shots/stuck.png)

### 44. [minor] Labels of the Borders law do not match its effect: 'Closed: strangers keep away', yet immigrants still come at the base rate

- **Category:** text; **reported by** 1 agent(s): s02
- **Detail:** The Laws tab says 'Borders: Closed. Closed: strangers keep away.' In Realm.immigrationCooldown, closed borders give the base cooldown (9000 ticks), and open borders only halve it. A player reads 'Closed' as 'no immigrants' and would not know that Open is a 2x boost. The Open side also brings thieves ('Thieves slipped through the open borders... took 2 coins and 2 wood').
- **Repro:** O > Laws
- **Screenshots:** [laws.png](s02/shots/laws.png)

### 45. [minor] Low contrast: menus, disabled buttons and text drawn directly over the 3D scene

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** On the main menu and Config, white text sits on light-blue buttons over a light-blue background. Disabled command buttons are white on pale grey and almost unreadable. The Inventory screen (I), Laws descriptions, Nations rows and Share all draw text straight over the world with no solid backing. The huge world-space '2 here, 3/8 residents' label shows through behind every overlay.
- **Repro:** pt key i; pt shot
- **Screenshots:** [inventory.png](s09/shots/inventory.png)

### 46. [minor] Nations screen reopens at the old scroll position, not at the top where enemies are

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** Reopening O showed 'Nations 20-23 of 33' (four 1-member strangers) instead of the top rows (Greatshire at war, rival neighbours). 33 nations by mid-day 1 at 4 rows per page means 9 pages. The 'Scroll for more.' hint stays on the last page.
- **Repro:** O; scroll to the end; Escape; wait; O
- **Screenshots:** [nations4.png](s04/shots/nations4.png)

### 47. [minor] No crash recruiting a merchant: #367 fix holds through recruit, save and continue (short soak only)

- **Category:** crash; **reported by** 1 agent(s): s12
- **Detail:** I recruited Lowerville's merchant Abigail Watson (listed under Market Info > Merchants in Lowerville) with G until Loyal, then R at tick 30491. Status: 'Abigail Watson left Lowerville and joined Westshire! Members: 6. Lowerville resents it: relations Hostile (-54).' The game kept running at about 40-49 tps. I saved from the pause menu at tick 31951, ran pt stop, then pt start --continue. The world loaded ('Welcome back. Day 2 - Westshire.') and ran on to tick 36374. After the load, Westshire's Market Info lists Abigail Watson as a merchant, and Stalls for Sale went from 3 to 0. pt status never showed the game dead, and the log has no error or exception lines. I ran out of time, so the game ran only about 29 game seconds after the recruit before the save and about 84 game seconds after the continue. The goal asked for 5 and 3 game minutes, so this is a short soak.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 102; chop 50 wood; N; F in open ground; O > Laws > Borders Open; walk to Lowerville (approx -470,240), enter, read Market Info > Merchants; walkto that pawn; G repeatedly until '(Loyal)'; R; Escape > Save; pt stop; pt start --continue
- **Screenshots:** [home_market2.png](s12/shots/home_market2.png)

### 48. [minor] No tooltips anywhere: disabled buttons and jargon (PCIS, Defence, Chunk Size) never explained

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Hovering disabled buttons (Plant Sapling, Enter Settlement, Build House) or labels gives nothing. 'PCIS', 'Defence 3', 'Chunk Size', 'Location Scale' and 'Lag Prevention' are never explained. Ally gives no hint that it needs +30 until you read the header line.
- **Repro:** pt move 426 517; pt shot
- **Screenshots:** [hover_disabled.png](s09/shots/hover_disabled.png)

### 49. [minor] Own-nation members are labelled 'Stranger', and 'Loyal' means both a trait and a relationship tier

- **Category:** text; **reported by** 1 agent(s): s06
- **Detail:** Judy Taylor joined Easternland (chronicle) and is a merchant in my settlement. Her button reads 'Talk to Judy Taylor (Stranger) - Loyal' and she says 'I hardly know you. Why should I follow you? I stand by my own.' Inviting a non-member says '(needs Loyal)'. So 'Loyal' is both a personality trait and the relationship tier needed to invite, and a member reads as 'Stranger, Loyal'.
- **Repro:** Enter settlement; click Talk on a member; walk to a non-member; pt key r
- **Screenshots:** [talk_member_stranger.png](s06/shots/talk_member_stranger.png)

### 50. [minor] Pawn label 'Talk to X (Stranger) - Loyal' contradicts itself, even for your own merchant

- **Category:** text; **reported by** 1 agent(s): s12
- **Detail:** Charlie Roberts is a Westshire merchant (listed under Merchants in my settlement) who says 'Yes, leader?'. His action button still reads 'Talk to Charlie Roberts (Stranger) - Loyal', and his reply is 'I hardly know you. Why should I follow you? I stand by my own.' A player can't tell whether he is in their nation, or what 'Loyal' refers to.
- **Repro:** seed 102; after founding Westshire, walk to Charlie Roberts at the home well; E
- **Screenshots:** [talk_charlie.png](s12/shots/talk_charlie.png)

### 51. [minor] Pawns who joined the nation are still labelled '(Stranger)'

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** The Chronicle says 'Charlotte Johnson joined Easternville' and Abigail Evans is listed as our merchant, but both 'Talk to' buttons say '(Stranger) - Greedy'.
- **Repro:** inside own settlement after pawns join
- **Screenshots:** [shift_before.png](s09/shots/shift_before.png)

### 52. [minor] Peace message says 'the fighting has cost them enough' when no fighting happened

- **Category:** text; **reported by** 1 agent(s): s04
- **Detail:** Peace was offered from the command bar about 20 game seconds after declaring war on a far-off nation, with no lands meeting and no combat. Greatshire accepted 'the fighting has cost them enough'. The button under the pointer also changed in place from 'Offer Peace' to 'Propose Alliance to Greatshire'.
- **Repro:** declare war on Greatshire from O; walk onto Greatshire land; click 'Offer Peace to Greatshire'
- **Screenshots:** [peace.png](s04/shots/peace.png)

### 53. [minor] Player can declare war mid-truce with no warning or cost

- **Category:** balance; **reported by** 1 agent(s): s04
- **Detail:** With 'truce 54 s' shown on Greatshire's row, clicking War started war again at once, with no warning and no penalty. The truce binds only the AI.
- **Repro:** make peace with Greatshire; open O within 60 s; click War on Greatshire
- **Screenshots:** [truce_war.png](s04/shots/truce_war.png)

### 54. [minor] Player winter is trivial: no energy drain inside the settlement, and auto-eat removes food decisions

- **Category:** balance; **reported by** 1 agent(s): s03
- **Detail:** Inside my settlement energy stayed at 92.4 for more than 25,000 ticks of winter. Outdoors, winter roughly doubled drain (an apple auto-eaten about every 2,000 ticks, against about 3,000 in autumn), but apples are auto-eaten, so the player never chooses to eat. Cold showed only as faster apple use; I never felt in danger. 'The cold bites outdoors' had no visible effect beyond that (no cold meter or warmth indicator).
- **Repro:** pt start ... --season autumn; reach winter; compare player energy over pt wait 20 loops inside vs outside the settlement
- **Screenshots:** [winter_out.png](s03/shots/winter_out.png)

### 55. [minor] Reason for neighbour resentment after founding flashes for 8 ticks and is lost

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** 'Settlement founded. Newfield and Yellowfield and Smalltown and Townport resent a new settlement so near their land' (tick 8154) is replaced at once by 'You are back on your own land' and 'Objective complete'. It is not in the chronicle, and the Nations rows show Neutral (-5)/(-11) with no reason. (The later 'resents your border' drops are in the chronicle.)
- **Repro:** found a settlement within ~260 of several nations; read the status bar
- **Screenshots:** [founded.png](s04/shots/founded.png)

### 56. [minor] Recruiting for 'Members 4/10' is guesswork; members are labelled 'Stranger' and progress stalls

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Pawns have no name tags, and nothing explains that pawn colour shows their nation. I pressed R on two nearby pawns and both replies were 'already a member of Eastford'. The action bar calls my own members 'Talk to Grace Jones (Stranger)', and Linda Lewis said 'I hardly know you. Why should I follow you?' right after joining. A second house ('homes with room draw newcomers') brought no one in about 7000 ticks: Members stayed 4/10 from tick ~16000 to 26000.
- **Repro:** after the house: pt walkto pawn; pt key r; pt key e
- **Screenshots:** [invite2.png](s01/shots/invite2.png)

### 57. [minor] Same settlement spot read 'Village of Farmfield' then 'Village of Greentown' with 0/12 residents and 6/6 stalls all for sale

- **Category:** ux; **reported by** 1 agent(s): s12
- **Detail:** At about (-200,173), the floating sign read 'Village of Farmfield, 0 here, 2/8 residents' at tick 8075. At tick 18440 the same spot showed 'Village of Greentown', 0 here, 0/12 residents, Stalls 6/6, Stalls for Sale 6, and no merchants. I saw no chronicle line in the recent panel explaining the change, so the recruit target (Farmfield's stall-builder Quinn Jones) disappeared without explanation.
- **Repro:** seed 102; walk to the settlement 250m from Westshire at around (-200,173) at tick ~8000 and again at ~18400 (night)
- **Screenshots:** [greentown_empty.png](s12/shots/greentown_empty.png)

### 58. [minor] Shared seed reproduces the map but not the nations: a few player actions change NPC nation names/founders

- **Category:** ux; **reported by** 1 agent(s): s10
- **Detail:** Original Ironvale world (player chopped a tree and mined a rock before tick 850): chronicle tick 850 'Isabella Wood founded the nation of Upperfield', tick 1402 'Phoebe Davies declared the nation of Greenfield', tick 2150 'Bigtown' (Tina Johnson). New world from the Share-screen seed 1450162717, player idle: tick 901 'Isabella Wood founded the nation of Westernham', tick 1401 'Ruth Wilson raised a banner and called it Greentown'. A second idle world from the same seed matched the first idle one exactly, so the sim is deterministic per seed+inputs, and player gathering perturbs the NPC/name RNG. Terrain, trees, rocks, chickens, region and the first pawns all matched. A friend opening the shared link ('Share this world') will see different nation names and founders from the sharer's world, even if the sharer only chopped one tree. Expected: NPC naming/founding decisions isolated from the player's RNG draws, or the Share screen saying that only the starting map is shared.
- **Repro:** pt start --seed Ironvale; Start; Begin; pt walkto tree; e x5; pt walkto rock; e x3; wait to tick 1500, read chronicle (Upperfield/Greenfield). Escape > Share (seed 1450162717). Main Menu > Start > clear box > type 1450162717 > Begin; idle to tick 1500: chronicle shows Westernham/Greentown.
- **Screenshots:** [seedworld_chron.png](s10/shots/seedworld_chron.png)

### 59. [minor] The Gift button spends coins on nation leaders who can never be recruited, and gifting a walking pawn mostly misses

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** I gave Karl Smith coins before R revealed 'I lead Forttown. I won't abandon my people.' The command bar ('Talk to Karl Smith (Stranger) - Ambitious, Greedy') does not say he leads a nation. Pawns also walk out of the 5m range between presses: of 8 G presses, 3 landed ('There is no one within reach to give a gift to. (x5)'). Recruiting is the only way I found to raise members, and it took constant chasing.
- **Repro:** Walk to Karl Smith at Forttown; G; R
- **Screenshots:** [gift1.png](s02/shots/gift1.png)

### 60. [minor] Three different day numbers for the same moment

- **Category:** text; **reported by** 1 agent(s): s03
- **Detail:** The chronicle said 'Day 5, morning', the Season panel said 'Autumn - day 1/2, year 1' and state.json had day=4, all at the same time. This makes it hard to tell how long until winter.
- **Repro:** pt start ... --season autumn; create a nation; compare the chronicle and Season panel
- **Screenshots:** [daycount.png](s03/shots/daycount.png)

### 61. [minor] Three differently worded 'Village' counters, and 'settlement is now a Village!' while the era stays Camp

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Objective 7 shows 'Members 2/10 - Buildings 0/2', the era box shows 'Village: 0/4', and the description says 'Village 0/1. A settlement with 2 buildings is a village.' After the second building the toast says 'The settlement is now a Village!', but the era is still 'Camp (1/4)'. The settlement tier and the nation era have the same name. There is also a pure time gate ('Day 1/3') that the objective text never mentions.
- **Repro:** build stall, farm (K K) and house (K K); click the Era box
- **Screenshots:** [era2.png](s01/shots/era2.png)

### 62. [minor] Trees and rocks near the player are stripped within seconds by NPCs, while objective 1 asks for 10 wood

- **Category:** balance; **reported by** 1 agent(s): s10
- **Detail:** Right after loading at tick 1842 the nearest tree was 17 m away and rocks were 46-110 m away. By tick ~1994 the 17 m and 46 m trees were gone, by 2138 the 89.9 m rock was gone, and by tick 2550 (about 14 game seconds) the nearest tree was 137 m away. No chopping was visible on screen, so they just vanish. The same thing happened identically on two separate loads, so it is the simulation and not a load bug. The player only had Wood 4/10 for 'Gather wood' and the area went from wooded to bare in seconds (Moor region, 'scarce in everything').
- **Repro:** Save at tick 1838 at (-31,-133) as above; pt start --continue; pt state every 2 s; compare nearest.tree between tick 1842 and 2550.
- **Screenshots:** [load_later.png](s10/shots/load_later.png)

### 63. [minor] Trees give apples all winter, so the player never goes hungry while NPCs starve

- **Category:** balance; **reported by** 1 agent(s): s11
- **Detail:** The season panel says 'Winter: no harvests', but chopping trees in winter still gave 2-3 apples each (for example tick 218980: 'Gathered 7 wood and 3 apples'). My apples went from 30 to 41 during winter, and my energy never fell below 90. The player has no winter pressure while towns lose 16 people.
- **Repro:** in winter, walkto tree; e
- **Screenshots:** [winter_end.png](s11/shots/winter_end.png)

### 64. [minor] Tribute 'to your treasury' from AI nations is invisible; coins never change

- **Category:** bug; **reported by** 1 agent(s): s04
- **Detail:** Status said Smalltown (tick 11002) and Townport (13002, 15002) sent 20 coins of tribute to your treasury. Player coins stayed 224 from tick 8202 to 16190, and no nation treasury is shown on the HUD, Inventory or Nations screen. Expected: the coins appear somewhere the player can see and spend.
- **Repro:** pt start --seed 104; create nation; found settlement near several neighbours; wait ~150 game s; watch status vs Coins
- **Screenshots:** [nations_final.png](s04/shots/nations_final.png)

### 65. [minor] Walking is locked inside a settlement with no explanation

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** After entering the settlement (E), W does nothing until E is pressed again ('Exited settlement.'). pt walkto rock timed out. H says 'Welcome home!' but leaves you outside, so the next K says 'You are not inside a settlement.' with no hint to press E.
- **Repro:** pt key e at own well; pt walkto rock (times out); pt key h; pt key k
- **Screenshots:** [rock2.png](s01/shots/rock2.png)

### 66. [minor] World event 'fed from your stores' gives no visible food store to judge the cost

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** 'Wanderers at the gate' offered 'Welcome them all (4 food) - 2 new members, fed from your stores'. No panel shows any 'stores'. It turned out to take 4 apples from my own inventory (32 to 28). Expected the option to say where the food comes from, and how much is available.
- **Repro:** play into autumn day 2 in a settlement; event fires around tick 141552
- **Screenshots:** [wanderers.png](s03/shots/wanderers.png)

### 67. [polish] Main-menu Controls page lists debug cheats (F2-F6) to ordinary players

- **Category:** text, ux; **reported by** 2 agent(s): s01, s09
- **Also reported as:** "Chronicle HUD lines truncated mid-word; Controls page lists debug cheat keys"
- **Detail:** Page 3 of the Controls screen lists spawn pawn, coins, wood and other cheats, with no warning that they are debug keys.
- **Repro:** Main menu > Controls > Next > Next
- **Screenshots:** [controls_debug.png](s01/shots/controls_debug.png) [controls3.png](s09/shots/controls3.png)

### 68. [polish] Objective 7 progress bar clips its text on the left ('nbers 4/10 · Buildings 0/2')

- **Category:** text; **reported by** 2 agent(s): s02, s11
- **Also reported as:** "Objective progress bar clips 'Members' to 'bers'; Settlement Info cuts off the building cost"
- **Detail:** The 'Reach the Village era' progress bar shows 'nbers 4/10 · Buildings 2/2', with the start of 'Members' cut off. When the era panel is expanded, the objective panel's hint text also runs under the command-bar buttons. Inside a settlement, the Season panel overlaps Player Info (the Relationships line is half hidden).
- **Repro:** Reach objective 7; look at the top-left objective panel; click the Era header
- **Screenshots:** [objective_text_cut.png](s02/shots/objective_text_cut.png) [clipped.png](s11/shots/clipped.png)

### 69. [polish] #355 looks addressed: caravans and Storehouse are explained well before the Town era

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** Positive. In the Camp era the settlement action bar already shows 'Send Caravan (Town era)'. Clicking it says 'This settlement needs a Storehouse before it can send caravans. The Storehouse is unlocked in the Town era.' Small gaps: the Controls screens (3 pages) do not mention trading, caravans or the Sell/Purchase buttons, and the Era box's Village unlocks do not mention the Storehouse.
- **Repro:** Enter own settlement in the Camp era; click Send Caravan (Town era) at (553,445)
- **Screenshots:** [caravan_hint_camp.png](s05/shots/caravan_hint_camp.png)

### 70. [polish] 'Ready: you can make 1' on both Axe and Pickaxe although I could not afford both

- **Category:** text; **reported by** 1 agent(s): s07
- **Detail:** With 6 wood and 5 stone, Axe (4w 2s) and Pickaxe (3w 3s) each said 'Ready: you can make 1'. After I crafted the axe, the Pickaxe row said 'Need 1 more wood'. Each row is right on its own, but nothing warns that the two compete for the same wood.
- **Repro:** gather 1 tree and 1 rock; pt key c
- **Screenshots:** [craft2.png](s07/shots/craft2.png)

### 71. [polish] A second axe shows as 'have 2 (12 uses left)': unclear how many uses the spare has

- **Category:** text; **reported by** 1 agent(s): s07
- **Detail:** After I crafted a second axe while the first had 12 uses left, the row read 'have 2 (12 uses left)'. A player cannot tell whether the spare has 20 uses or whether 12 is shared between both.
- **Repro:** craft Axe twice; pt key c
- **Screenshots:** [second_axe_ambiguous.png](s07/shots/second_axe_ambiguous.png)

### 72. [polish] Apples are eaten silently; 'Eat something' objective completes itself; energy never matters

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** The first chop said '5 wood and 2 apples', but the inventory showed 1 apple and energy jumped 84->97. The Eat objective completed 48 ticks after it appeared, without my doing anything. Energy stayed 90-99 the whole session with 40 apples in the bag. Planting a sapling (P) gives no status message.
- **Repro:** chop first tree; watch Apples and Energy
- **Screenshots:** [apples_mismatch.png](s01/shots/apples_mismatch.png)

### 73. [polish] Day numbering differs: chronicle 'Day 5' while the probe says day 4 and the season panel says 'day 1/2'

- **Category:** text; **reported by** 1 agent(s): s11
- **Detail:** At the start the chronicle said 'Day 5, midday' while state.json said day 4 and the Season panel said 'Autumn - day 1/2'. The chronicle's day counts from 1 and the probe's from 0, and the season-relative day adds a third numbering.
- **Repro:** start; compare chronicle to the Season panel
- **Screenshots:** [where.png](s11/shots/where.png)

### 74. [polish] Helpful 'too close' message when founding: names the nearby village and the direction of open ground

- **Category:** ux; **reported by** 1 agent(s): s12
- **Detail:** Pressing F too close to another settlement gives 'Too close to the Village of Northwood: 165 away, and settlements must be 250 apart. Open ground lies about 90 to the north.' That led me straight to a valid site in two hops. This is good. E at a tree I faced chopped it every time (5-6 wood plus apples per press). But no pawn was within 5 of me during those chops, so the 'pawns nearby' half of the goal is untested.
- **Repro:** seed 102; chop 50 wood near spawn; F
- **Screenshots:** [tree_e.png](s12/shots/tree_e.png)

### 75. [polish] Inventory panel overlaps the command bar; world labels clipped/overlapping

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** At 960x540 the Inventory Info panel covers the 'Create Nation'/'Join Nation' buttons at the start. Settlement labels ('Village of Greatshire / 0 here, 2/8 residents') are clipped at the top of the screen, and pawn labels overlap. The world label says 'Village' while the Nations row said '1 camp'. The 'south-east' hint for open ground has no compass on screen to act on.
- **Repro:** pt start --seed 104; Begin; screenshot
- **Screenshots:** [overlap.png](s04/shots/overlap.png)

### 76. [polish] Law take-effect messages are glued onto unrelated status lines

- **Category:** text; **reported by** 1 agent(s): s06
- **Detail:** Laws took effect correctly: High tax plus Conscription at tick 2502 (taxes 150 coins in total, Defence 7 with 2 conscripts); repealed at tick 10734 and lifted at 12502, with a countdown in the Laws tab. But the notice arrived appended to 'You enter the land of Yellowton (Neutral).' and to 'Westport shares an enemy with you...', so it is easy to miss. Also both changes landed about 5-10 s after the 30 s countdown hit zero.
- **Repro:** o; Laws; High, On; Escape; wait 30 s; then None, Off
- **Screenshots:** [after_repeal.png](s06/shots/after_repeal.png)

### 77. [polish] New World seed box: Ctrl+A does not select all, so pasting a shared seed means backspacing the prefilled seed

- **Category:** ux; **reported by** 1 agent(s): s10
- **Detail:** The seed box is prefilled with the last seed ('Ironvale'). Ctrl+A (tried twice) did not select the text, and the next BackSpace deleted only one character, giving 'Ironval1450162717' (live preview helpfully showed 'is seed 1889372902', which made the mistake visible). Clearing it took ~8 BackSpaces. Possible harness caveat: xdotool modifier timing at a few fps.
- **Repro:** Main Menu > Start; click seed box; pt key ctrl+a BackSpace; pt type 1450162717; shot.
- **Screenshots:** [ctrla_noselect.png](s10/shots/ctrla_noselect.png)

### 78. [polish] Pause menu has no Load button (Load is only on the main menu)

- **Category:** text; **reported by** 1 agent(s): s10
- **Detail:** The pause menu shows Resume, Save, Share, Controls, Main Menu and Quit. The game's own guidance says Escape opens a pause menu with Save, Load, Share and Main Menu. Loading needs Main Menu (which also saves first, and says so) > Load. That works, but the description is wrong or the button is missing.
- **Repro:** In world: pt key Escape; shot.
- **Screenshots:** [pause.png](s10/shots/pause.png)

### 79. [polish] Recent-chronicle lines truncated with '...', including my own nation's name

- **Category:** text; **reported by** 1 agent(s): s01
- **Detail:** The always-on chronicle cuts lines off: 'You raised a banner and named your people Ea...'. Events about me ('Relations between Newburg and Eastford are...') are cut too. There were 91 entries by tick ~11500, mostly about other AI nations. The nation name was picked automatically, with no prompt.
- **Repro:** pt key n; screenshot
- **Screenshots:** [chronicle_trunc.png](s01/shots/chronicle_trunc.png)

### 80. [polish] Settlement HUD text clipped: objective progress, 'Next (K)' line, giant floating label

- **Category:** text; **reported by** 1 agent(s): s03
- **Detail:** Inside the settlement, the objective progress text is cut off on the left ('mbers 9/10 * Buildings 2/2'). The Settlement Info 'Next (K): House, 20 wood, 1...' runs off the right. The floating '<n> here, x/y residents' label is huge and cut off by the top of the screen, behind the chronicle. Settlement Info also said 'Tier: Town' while the era badge said 'Camp (1/4)'.
- **Repro:** found a settlement and press E to enter it
- **Screenshots:** [cutoff.png](s03/shots/cutoff.png)

### 81. [polish] Settlement name sign floats huge across the top of the screen, overlapping the HUD

- **Category:** ux; **reported by** 1 agent(s): s12
- **Detail:** Near any settlement, the 'N here, X/Y residents' sign renders in giant letters behind the chronicle and pause title. Separately, after the continue the Inventory panel sat mid-screen over the world view. The probe reports player.settlement as the generic 'Settlement' for every town.
- **Repro:** seed 102; stand at any well
- **Screenshots:** [pause.png](s12/shots/pause.png)

### 82. [polish] Teleport home gives no 'teleported' feedback

- **Category:** text; **reported by** 1 agent(s): s06
- **Detail:** H from about 170 m away put me at the well, 4 m from the settlement centre, with member Oscar Johnson 3.6 m away, so the Talk/Gift/Order buttons showed at once and gift worked. Good. The only message is 'You are back on your own land.'
- **Repro:** Walk ~150 m from home; pt key h
- **Screenshots:** [teleport_home_ok.png](s06/shots/teleport_home_ok.png)

### 83. [polish] War message strength '2 to your 2' counts the ally but the header says 1 member

- **Category:** text; **reported by** 1 agent(s): s04
- **Detail:** 'Greatshire takes up arms: war, 2 to your 2' while the header says 'Fortburg, 1 member'; later '4 to your 4' with ally Castleton at 3. Nothing says allies are counted. Denounce is still offered on a nation already at war.
- **Repro:** ally Castleton; declare war on Greatshire from O
- **Screenshots:** [war.png](s04/shots/war.png)

## Objective timings

Game seconds from the start of the world to each objective (event tick / the game's ticks per second, from events.jsonl). Game time, not wall time: with many sessions on one machine a world can run slower than real time, and wall-clock timings would mostly measure the machine.

| # | Objective | s01 | s02 | s03 | s04 | s05 | s06 | s07 | s08 | s09 | s10 | s11 | s12 | min | median | max |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Gather wood | 26 | 68.1 | 2424 | 66 | 44 | 30 | 29 | 23 | 48 | - | 2438 | 42 | 23 | 44 | 2438 |
| 2 | Eat something | 27 | 69 | 2425 | 67 | 45 | 31 | 30 | 24 | 49 | - | 2439 | 43.1 | 24 | 45 | 2439 |
| 3 | Create or join a nation | 39 | 70.1 | 2433 | 68 | 55 | 32 | 70 | 28 | 50 | - | 2440 | 44 | 28 | 55 | 2440 |
| 4 | Found a settlement | 129 | 144.1 | 2517 | 164 | 150 | 98 | 195 | 166 | 127 | - | 2584.1 | 131 | 98 | 150 | 2584.1 |
| 5 | Enter your settlement | 134 | 147 | 2520 | 168 | 152 | 102 | 197 | 169 | 130 | - | 2591 | 697 | 102 | 168 | 2591 |
| 6 | Build a market stall | 196 | 151 | 2528 | - | 156 | 106 | - | 172 | 149 | - | 2730.1 | 698 | 106 | 172 | 2730.1 |
| 7 | Reach the Village era | - | - | 2731 | - | - | - | - | - | - | - | - | - | 2731 | 2731 | 2731 |
| 8 | Sell resources at market | - | - | 2805 | - | - | - | - | - | - | - | - | - | 2805 | 2805 | 2805 |

## Problems with the run

- s02: game.log says [error] the game crashed: Object reference not set to an instance of an object.

