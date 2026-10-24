# Playtest summary: 20261024-1151

4 sessions, build beyond-nations 0.6.1 (6ac1e3f), generated 2026-10-24 12:25.

**Findings:** 31 reports in 29 groups -- 0 blocker, 0 major, 21 minor, 10 polish.

## Outcomes

| Session | Goal | Outcome | Fun | Minutes | Objectives | Reached | Deaths | Findings | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| s01 | Re-check sieges (#396, #404): your village starts besieged. Confirm the Era box (top left) names the siege in red ("Your village: Besieged by X (n/12)!") rather than a countdown, time how long the siege takes to go from its first round to capture if you do nothing, then in a second attempt (restart) hold the village with tribute, gifts or an alliance and say whether a minute is enough time to react. | achieved | 3 | 6.5 | 0 | - | 0 | 5 | #396/#404 hold up. The Era box clearly names the siege in red ('Your village: Besieged by Greenshire (n/12)!'), not a countdown, and an undefended village falls at tick 2500, 50 game seconds after spawn (it starts at 2/12 and gains a round every 5 s). That is enough time to react: the Nations panel pauses the game, and Tribute (25 of 216 coins) or Ally (Southernfield is already at +31-33) each lift the siege in one click. The weak spot is feedback: the siege ends with no message, and the banner's count goes stale, so a player who acts can't easily tell that it worked. |
| s02 | Survive the first winter and watch the other nations (#353, #395, #406): found or join a nation early, use the hunger warning and Feed Residents, and count hunger deaths from events.jsonl, noting which were nation leaders and where they died. The 0.6.1 run on this seed saw 5 NPC hunger deaths in two thirds of the winter, 4 of them leaders. | achieved | 3 | 30.7 | 8 | Village | 0 | 9 | Starvation is clearly better than 0.6.1 on this seed. Over the whole rest of winter (tick 180000-231001) there were 3 NPC hunger deaths, none of them a sitting leader (vs 5, 4 leaders, in two thirds of winter), and all 3 died inside a settlement (Castleport 1, Newwood 2). The player's side works: the hunger banner shows even away from home, and Feed Residents does its job. But the banner and button counts never agree, the buttons shift around, and feeding every minute gets tedious. In winter the market cannot afford to buy anything, so objective 8 is stuck until spring. |
| s03 | Audit the settlement HUD at 960x540 (#350, #397): inside your town with a busy market, check every right-column box fits without overlap (Market names merchants on one line, Player Info says your nation), open the Era box (+) and confirm its detail scrolls instead of covering the command bar, and try Sell Resources carrying only food (chicken or apples). Note anything clipped, overlapping or unclear, with screenshots. | partial | 3 | 5.9 | 0 | - | 0 | 7 | The right column fits cleanly at 960x540: nothing overlaps, Player Info names the nation, and the Era detail scrolls without covering the command bar, so #397 holds. #350 is only partly met because the Merchants line still wraps to two lines. Selling food is the weak spot: merchants are usually too poor to buy chicken meat, and neither the HUD nor the sale toast says enough (no per-merchant purse, nothing about which good was sold). |
| s04 | Play the first 20 minutes as a new player and watch the AI nations grow (#402): follow the objectives, and keep an eye on the chronicle and the Nations screen (O) for computer-run nations founding settlements. Note any pawns that seem stuck in place in the wilds, any nation that never founds, and anything confusing in the first minutes. | achieved | 3 | 16.5 | 6 | - | 0 | 10 | #402 works: all 37 AI nations founded a settlement within seconds of declaring, and none of the ~15 pawns I tracked in the wilds were stuck. The problem now is scale. 38 nations in 5 game minutes flood the chronicle with rivalry and raid lines, relations climb to +100 with no action from the player, and sieges kill nations within a game day, with the wrong 'never raised a hut' epitaph. The early objectives move quickly, but an AI member can do the stall step for you, there is no compass for 'go west', and three different 'Village' counters confuse the new player. |

## Findings

### 1. [minor] Disband epitaph says the nation 'never raised so much as a hut' right after its settlement was captured

- **Category:** text; **reported by** 2 agent(s): s02, s04
- **Also reported as:** "Chronicle says captured nations 'never raised so much as a hut' although they founded settlements"
- **Detail:** Chronicle: 'After a siege, a camp of Southernfield fell to Castleport.' then 'Southernfield is no more; it never raised so much as a hut.' The same thing happened for Newport at tick 216252 ('a village of Newport fell to Newwood' then the same epitaph). Expected the epitaph to reflect that it had (and lost) a settlement.
- **Repro:** watch chronicle (Y) / events.jsonl for NationDisband after SettlementCapture
- **Screenshots:** [disband_text.png](s02/shots/disband_text.png) [epitaph_full.png](s04/shots/epitaph_full.png)

### 2. [minor] 'The settlement is now a Village!' but 'Reach the Village era' stays incomplete

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** Building the house (2 buildings) shows 'House built. The settlement is now a Village!' and Settlement Info says Tier: Village, but objective 7 'Reach the Village era' stays open (Members 5/10) and the era badge still says Camp; the objective text says 'Village 0/1' while the era bar says 'Village: 2/4'. Three different 'Village' counters confuse a new player into thinking they finished.
- **Repro:** seed 1; reach objective 7, build Farm then House with K inside the settlement
- **Screenshots:** [village_confusion.png](s04/shots/village_confusion.png)

### 3. [minor] AI nations flood the world: 38 nations in 5 game minutes, chronicle drowned in rivalry lines

- **Category:** balance; **reported by** 1 agent(s): s04
- **Detail:** Nation creation: 12 in the first minute, 10 in the second, 14 in the third, 2 in the fourth (38 total by tick ~16000); every AI nation founded a settlement within 1-30 game seconds (good for #402, none failed to found). But the full chronicle had 133 entries by Day 1 afternoon and 495 by Day 2, most 'X and Y have become rivals over their border' and raid lines; the 5-line recent panel turns over every few seconds so the player's own news and foundings scroll away. By tick ~19500 eight nations had already been disbanded by sieges.
- **Repro:** seed 1; play ~8000 ticks; press Y for full chronicle
- **Screenshots:** [chronicle_spam.png](s04/shots/chronicle_spam.png)

### 4. [minor] Build House cost is never shown

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** 'Build House' button and 'Click again to confirm: Build House.' show no price. It cost 20 wood + 15 stone. Settlement Info only lists 'Next (K): Farm, 25 wood, 10 stone'. Expected: the cost on the button or confirm prompt.
- **Repro:** enter settlement; click Build House once and read the prompt
- **Screenshots:** [house_cost.png](s02/shots/house_cost.png)

### 5. [minor] Feeding residents is a constant once-a-minute chore

- **Category:** balance; **reported by** 1 agent(s): s02
- **Detail:** From tick 189456 to 235000 Feed Residents was used 19 times for 90+ feedings: 4-10 residents became hungry every ~2500 ticks (50 game s) in a 13-17 member town. It can be kept up because trees still drop 1-3 apples each in winter, but the loop is chop, H, walk in, E, Feed, every minute. Apples ran down from 37 to 18 until I crafted an axe. Residents never eat from the 260+ coin stockpile or the market themselves. Nothing gets automated as the town grows.
- **Repro:** play as above for one winter day
- **Screenshots:** [feed_chore.png](s02/shots/feed_chore.png)

### 6. [minor] Found-settlement refusal gives compass directions but the HUD has no compass

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** Pressing F inside another nation's land says 'Open ground lies about 190 to the west' (later '180 to the north'). Nothing on screen shows which way west or north is, no compass, minimap or heading. I walked the wrong way into Eastburg's land and had to read the source to learn north = +Z. Expected a compass strip or a relative direction ('behind you, to the left').
- **Repro:** seed 1; walk to (-178,-93) in Southshire land; press N then F with 50 wood
- **Screenshots:** [no_compass.png](s04/shots/no_compass.png)

### 7. [minor] Harvested chicken meat vanishes silently (auto-eaten?) on first chicken

- **Category:** bug; **reported by** 1 agent(s): s03
- **Detail:** Pressing E on a chicken at energy 82 showed 'Harvested chicken meat.' but Inventory stayed at Chicken Meat: 0, and energy jumped to 99. Nothing said it was eaten. A later chicken at energy 97 did add meat. Separately, at spawn the probe listed a chicken 0.7 m away, but E said 'No entities within range'.
- **Repro:** pt start ... --stage-settlement; enter world; pt key e (no entity); pt walkto chicken; pt key e; check Inventory Info
- **Screenshots:** [meat_eaten.png](s03/shots/meat_eaten.png)

### 8. [minor] Hunger banner count and Feed Residents button count never agree

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** Banner 'Fortton: 6 members hungry' while button said 'Feed Residents (5 hungry)' and the click then 'fed 8 residents'. At tick 193568 banner said 1, Feed fed 2. At tick ~231300 (first night of spring) button showed '(1 hungry)' with no banner at all; right after feeding 1 the button jumped to '(5 hungry)'. After feeding everyone present the banner can still say '1 member hungry' with no Feed button and no hint who or where that member is. Expected: one consistent count, and the banner to say how many are elsewhere.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 103 --season winter; found a nation (n) and settlement (f); gather apples from trees; return (h) and enter (e) every ~2500 ticks; compare the yellow banner with the Feed Residents button
- **Screenshots:** [count_mismatch.png](s02/shots/count_mismatch.png)

### 9. [minor] Left HUD column jumps as townsfolk walk in and out of range

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** The Inventory Info box moves between y~227 and y~288 as pawn Order/Talk command rows appear and disappear. With the Era detail open it moves into the centre (x 220-420) over the plaza. It never overlapped the command bar in my shots, but the movement is distracting.
- **Repro:** (in town) wait near the plaza, take shots every 8 s; compare rows1.png and rows3.png
- **Screenshots:** [rows1.png](s03/shots/rows1.png)

### 10. [minor] Market Info 'Merchants:' line wraps onto two lines at 960x540

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** Inside Town of Cityfield (6/6 stalls) the Market Info box reads 'Merchants: Ruth Thompson,' then 'Xavier Thomas +4' on a second line. The goal (#350/#397) expects the merchants on one line. No box overlaps: Settlement Info, Market Info, Season and Player Info all fit in the right column, and Player Info shows 'Nation: Cityfield (LEADER)'.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 109 --stage-settlement; space; click Start, Begin; pt walkto settlement; pt key e; pt shot
- **Screenshots:** [market_wrap.png](s03/shots/market_wrap.png)

### 11. [minor] Nations screen (O) shows only 4 nations per page with inconsistent paging

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** With 21-29 nations the list shows 4 rows per page; Down moves by 3 or 2 rows (1-4, 4-7, 7-10, 9-12) so rows repeat, a click sometimes does not register, and the header says 'Scroll for more' even on the last page. Early on (no nation) rows have no relation or neighbour marker although the header says 'neighbours and enemies first'. Paging through 29 nations takes ~8 clicks.
- **Repro:** seed 1; press O; click Down repeatedly
- **Screenshots:** [nations_list.png](s04/shots/nations_list.png)

### 12. [minor] Objective 8 'Sell resources at market' blocked all winter: merchants have 2-3 coins

- **Category:** balance; **reported by** 1 agent(s): s02
- **Detail:** Sell Resources: 'No merchant here can pay: the richest has 2 coins, and your cheapest good (stone) sells for 3.' Market Info showed 4/4 stalls, 3 coins for the rest of winter (tick 200800 to ~231000). It only worked in spring when the stalls had 71-389 coins. The objective panel gives no hint that you need to wait or buy from merchants first.
- **Repro:** reach objective 8 in winter; enter settlement; click Sell Resources
- **Screenshots:** [sell_broke.png](s02/shots/sell_broke.png)

### 13. [minor] Raids say 'coins taken' but the player's Coins never change

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Greenshire raids at ticks 500, 1500 and 2500 report 1-2 coins taken, but Inventory Coins stays 216. The coins presumably come from a nation treasury that isn't shown on the HUD, so raids feel like they have no effect. Expected the raided total to be visible somewhere (nation treasury in the Era/Kingdom box or the Nations screen).
- **Repro:** pt start ... --seed 111 --stage-diplomacy; enter world; pt wait 40; compare Inventory Coins with the raid status
- **Screenshots:** [raids_after_ally.png](s01/shots/raids_after_ally.png)

### 14. [minor] Relations with other nations rise to Friendly +100 without the player doing anything

- **Category:** balance; **reported by** 1 agent(s): s04
- **Detail:** By tick ~24000 Oldshire was Friendly (+100) and Redford +69 purely from 'shares an enemy with you' events; I never traded, gifted or talked with them. An alliance needs only +30, so diplomacy happens to the player rather than by them.
- **Repro:** seed 1; found a settlement and play ~24000 ticks; press O and page down
- **Screenshots:** [relations_drift.png](s04/shots/relations_drift.png)

### 15. [minor] Sell Resources with only food fails, and the merchant purse that blocks it is never shown

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** With only 2 Chicken Meat, Sell Resources showed 'No merchant here can pay: the richest has 3 coins, and your cheapest good (chicken meat) sells for 31.' The message is clear and fits. But Market Info only shows the pooled total ('6/6 stalls, 6 coins'), so the player cannot tell from the HUD when a sale will work. Purses swung between 5 and 67 coins within a minute. A sale finally went through at tick 6235 (22 coins, one meat per click). Expected: Market Info shows the richest merchant's purse, or something like 'can buy up to N'.
- **Repro:** (in town) pt walkto chicken; e; pt walkto settlement; e; click Sell Resources (625,476)
- **Screenshots:** [sell_food_broke.png](s03/shots/sell_food_broke.png)

### 16. [minor] Sell toast doesn't say what was sold or how many

- **Category:** text; **reported by** 1 agent(s): s03
- **Detail:** After Purchase Food (1 apple for 5), Sell Resources sold that apple straight back and only said 'You sold resources to Frank Thompson. Relationship increased to 2.' plus a '+5 coins' float. It does not name the good or the quantity, so a player carrying several foods can't tell which one went. It also silently picks the cheapest good.
- **Repro:** (in town, carrying chicken meat) click Purchase Food (505,476); click Sell Resources (625,476)
- **Screenshots:** [sell_toast_vague.png](s03/shots/sell_toast_vague.png)

### 17. [minor] Settlement command bar reflows after Feed/Craft, putting a different command under the cursor

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** After clicking 'Feed Residents (1 hungry)' the button vanished and 'Talk to Bob Roberts (Stranger) - Glutton' took its place. After Craft Axe (no confirm), my second click landed on 'Send Caravan' (which then said a Storehouse is needed). Feed Residents also moves between row 2 and row 3 between visits. With the game at a few fps a repeated click hits the wrong command. Expected: stable positions for Feed Residents and build/craft commands.
- **Repro:** enter settlement with hungry residents; click Feed Residents twice quickly; or click Craft Axe twice
- **Screenshots:** [reflow.png](s02/shots/reflow.png)

### 18. [minor] Siege ends silently after Tribute or Ally: no 'siege lifted' message anywhere

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Run 2: Tribute to Greenshire at tick 12 said only 'will hold off its raiders for 60 seconds'. Some time between tick 344 and 664 the red 'Besieged by Greenshire (2/12)!' line just became amber 'Sieges can begin in 4:49'. Run 3: Ally with Southernfield at tick 692 lifted the siege by tick 864, also with no status or chronicle line. A player who pays tribute has no confirmation the village is safe unless they spot the Era box changing colour. Expected a status and chronicle entry like 'Greenshire lifts its siege of your village'.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 111 --stage-diplomacy; enter world; pt key o; pt click 123 237 (Tribute, Greenshire); pt key o; pt wait 12; pt events 20
- **Screenshots:** [lifted_silently.png](s01/shots/lifted_silently.png)

### 19. [minor] Top status line shows a stale siege count (10/12) while Era box shows 11/12

- **Category:** text; **reported by** 1 agent(s): s01
- **Detail:** With no action, the status banner said 'Greenshire is besieging your village (10/12)!' from tick 2000 until capture at 2500, while the Era box had already moved to 'Besieged by Greenshire (11/12)!'. Raid messages also overwrite the siege warning in the banner. Expected the banner to update each round, or not to show a count.
- **Repro:** pt start ... --seed 111 --stage-diplomacy; enter world; do nothing; pt shot at about tick 2450
- **Screenshots:** [stale_status.png](s01/shots/stale_status.png)

### 20. [minor] Tutorial objective 'Build a market stall' is completed by an AI member, player never presses B

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** Objective 6 asks to gather 20 wood + 5 stone and press B in the settlement. While I was out chopping (4 wood), Eve Smith, who had auto-joined my nation, built a stall at tick 5301 and the objective completed with +50 coins. The player never learns B and gets no explanation of who did it. Expected the objective to require the player's own stall, or the toast to credit Eve Smith.
- **Repro:** seed 1; create nation (N), found settlement (F), enter (E), exit and go chop trees; within ~250 ticks the objective completes on its own
- **Screenshots:** [stall_by_npc.png](s04/shots/stall_by_npc.png)

### 21. [polish] Objective 7 progress text clipped: 'nbers 5/10 · Buildings 0/2'

- **Category:** text; **reported by** 2 agent(s): s02, s04
- **Also reported as:** "Objective 7 progress bar text clipped: 'nbers 5/10' instead of 'Members 5/10'"
- **Detail:** The 'Members x/10 · Buildings y/2' label is wider than its bar and loses its first letters.
- **Repro:** reach objective 7 'Reach the Village era'
- **Screenshots:** [clipped_progress.png](s02/shots/clipped_progress.png) [objective_bar_clip.png](s04/shots/objective_bar_clip.png)

### 22. [polish] Capture message is a five-sentence wall of text in the status banner

- **Category:** text; **reported by** 1 agent(s): s01
- **Detail:** At capture (tick 2500) one banner holds the raid line, capture, nation fallen, 'without a nation', and the 5-minute collapse warning. The important part (found a settlement or win a follower within 5 minutes) comes last. The Era box's 'Collapsing! 4:58 left' is clear, though.
- **Repro:** pt start ... --seed 111 --stage-diplomacy; enter world; pt wait 60; pt shot
- **Screenshots:** [captured.png](s01/shots/captured.png)

### 23. [polish] E at a settlement enters it instead of talking to the pawn you walked up to; own members shown as 'Stranger'

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** Pawns idle around the well, so walking up to one and pressing E enters the settlement. The 'Talk to Mike Johnson' button inside did not respond on my click. Members of my own nation are labelled '(Stranger)' (Eve Smith, Mike Johnson).
- **Repro:** seed 1; after founding, pt walkto pawn near home; press e
- **Screenshots:** [talk2.png](s04/shots/talk2.png)

### 24. [polish] Era (+) detail scrolls correctly and leaves the command bar uncovered (positive)

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** With three command rows showing (Order/Talk/Gift pawn rows), the open Era detail becomes a scroll region with a scrollbar. The mouse wheel scrolls it down to the 'Sheltered until the Town era...' text, and the Objective box stays above the command bar. With no pawn rows it grows to show everything. Works as #397 intends.
- **Repro:** (in town) pt walkto pawn; click Era + (168,58); pt move 100 200; pt scroll 5
- **Screenshots:** [era_scroll_ok.png](s03/shots/era_scroll_ok.png)

### 25. [polish] Settlement floating label renders huge and cut off by the top of the screen

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** Standing at your own settlement, the label ('Camp of Westernshire / 4 here, 5/8 residents') is drawn in giant letters, clipped at the top edge and behind the chronicle panel, so the name cannot be read.
- **Repro:** seed 1; found a settlement and stand next to the well in third person
- **Screenshots:** [label_clip.png](s04/shots/label_clip.png)

### 26. [polish] Tribute's '60 seconds' truce doesn't match the 'Sieges can begin in 4:49' countdown shown afterwards

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Tribute promises a 60 s hold-off, but the Era box then counts down from about 4:49. The countdown seems to end near tick 15000 (5 min from world start) in both runs (4:42 at tick 864, 4:35 at tick 1236), so it looks like a fixed grace window from world start rather than one tied to the truce. Two different timers for 'am I safe' is confusing.
- **Repro:** run 2 above; compare the tribute message with the Era box countdown
- **Screenshots:** [lifted_silently.png](s01/shots/lifted_silently.png)

### 27. [polish] Wanderers event option 3 shows a blank gap where '--' should be

- **Category:** text; **reported by** 1 agent(s): s02
- **Detail:** 'they stay in these lands with no nation    and may found one of their own.' The source (WanderersEvent.cs) has ' -- ', which renders as blank space.
- **Repro:** wait for 'Wanderers at the gate' world event
- **Screenshots:** [wanderers_text.png](s02/shots/wanderers_text.png)

### 28. [polish] World-space 'Town of Cityfield / N here, residents' label is cut off and covered at the top

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** The big floating town label's first line is cut off by the top of the window. Status toasts and the Chronicle box draw over its second line (the residents count), so it is hard to read.
- **Repro:** (in town) pt shot
- **Screenshots:** [title_overlap.png](s03/shots/title_overlap.png)

### 29. [polish] World-space labels draw huge over the HUD in settlements

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** The settlement label ('16 here, 17/24 residents') fills the top band behind the status and chronicle panels in every settlement view. A nearby pawn's name/trait/activity label covered the Market Info panel. Caravan labels overlap the Era panel. Inside a settlement the Inventory panel also moves to the middle of the screen, and the Season panel overlaps Player Info.
- **Repro:** enter your settlement; look at the top of the screen
- **Screenshots:** [world_labels.png](s02/shots/world_labels.png)

## Objective timings

Game seconds from the start of the world to each objective (event tick / the game's ticks per second, from events.jsonl). Game time, not wall time: with many sessions on one machine a world can run slower than real time, and wall-clock timings would mostly measure the machine.

| # | Objective | s01 | s02 | s03 | s04 | min | median | max |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Gather wood | - | 3630 | - | 19.1 | 19.1 | 1824.5 | 3630 |
| 2 | Eat something | - | 3631 | - | 20 | 20 | 1825.5 | 3631 |
| 3 | Create or join a nation | - | 3632 | - | 44 | 44 | 1838 | 3632 |
| 4 | Found a settlement | - | 3706 | - | 97.1 | 97.1 | 1901.5 | 3706 |
| 5 | Enter your settlement | - | 3709 | - | 101.1 | 101.1 | 1905 | 3709 |
| 6 | Build a market stall | - | 3710 | - | 106 | 106 | 1908 | 3710 |
| 7 | Reach the Village era | - | 3991 | - | - | 3991 | 3991 | 3991 |
| 8 | Sell resources at market | - | 4641 | - | - | 4641 | 4641 | 4641 |

