# Playtest summary: 20261024-1610

8 sessions, build beyond-nations 0.6.2 (7f89db5), beyond-nations 0.6.2 (edb1a62), generated 2026-10-24 17:56.

**Findings:** 50 reports in 42 groups -- 0 blocker, 7 major, 32 minor, 11 polish.

## Outcomes

| Session | Goal | Outcome | Fun | Minutes | Objectives | Reached | Deaths | Findings | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| s01 | Re-check siege and confirmation messages (#409): lift the starting siege with tribute or an alliance and confirm the status line says the siege was lifted; let a two-click confirmation (War on the Nations screen, or L) lapse past 8 seconds and confirm the 'click again' prompt is taken back with no '(x2)'; if a nation is conquered out of existence, read its epitaph in the chronicle (Y) and say whether it fits. | achieved | 3 | 13.8 | 4 | - | 0 | 6 | The #409 fixes hold up. Tribute to Greenshire produced 'Greenshire has lifted its siege of your village.' (tick 503). War and L confirmations both lapse after 8 s wall-clock with 'Not confirmed in time, so nothing was done.' and no (x2). The conquest epitaph 'The banners of Upperwood came down when Greenshire took its last village.' fits. The biggest remaining gap is the moment your own nation dies: the status line says 'Autosaved.' while a stranger's fall gets announced, and 'Your news' then forgets your nation's history. |
| s02 | Survive the first winter and check the hunger readouts agree (#410): found or join a nation early; each time you feed, compare the HUD hunger banner (here vs elsewhere), the 'Feed Residents (N hungry)' button and the 'You fed N' result, and say whether they make sense together. Count hunger deaths from events.jsonl and note which were leaders. | achieved | 3 | 35 | 9 | Village | 0 | 10 | The hunger readouts mostly agree: the banner ('N hungry: X here, Y outside the walls' / 'all in the Town of Yellowton' when away) and the Feed Residents (N hungry) button matched every time, and 'You fed N' always equalled the button count. The problem is what 'fed' means: several times the banner showed new hungry people the very next frame, once adding up to 11 feedings for 8 people present, so 'You fed 7' does not mean they are fed. Winter itself was easy for the player (trees give apples despite 'no harvests'; 0 deaths), while AI nations lost 12 to hunger, including 4 leaders. |
| s03 | Try to make the command bar move under your cursor (#410): inside and outside your town, watch the buttons as pawns walk in and out of reach, as Feed Residents becomes enabled or disabled, and after crafting or buying a tool; click quickly twice on buttons and report any time a click lands on a different command than intended, with screenshots. Also check the Talk button shows the pawn's standing but not its traits. | achieved | 3 | 12.3 | 6 | - | 0 | 7 | The #410 command bar is much steadier than before. Widths no longer change with pawn names, counts or tiers, the Drastic row stays where it is, Craft Axe goes last so a double-click on it does nothing, and Talk shows only the standing ('Talk to X (Stranger)'), with the traits in the tooltip. But the bar still moves under the cursor in three ways: the Craft/Buy group comes after the pawn rows, rows stack upward so a disappearing lower row (caravan, pawns) moves everything above it, and Enter/Exit Settlement are not in the same slot. All of these produced real misclicks, the worst being an unconfirmed 299-coin pickaxe purchase. Feed Residents stayed in a fixed, disabled slot the whole time, but no resident got hungry in about 5 game minutes, so I never saw it switch to enabled. |
| s04 | As a new player, use the chronicle (#411): play the first 15 minutes, then open the full chronicle (Y) and the strip; check that by default it shows your news (your nation, neighbours, foundings) and that the switch (buttons or U) shows all nations. Say whether your own news is easy to find, and whether anything important is hidden by the default. | achieved | 3 | 14 | 6 | - | 0 | 8 | The switch works: the Your news and All nations buttons and U toggle, the toast confirms each change, own entries are highlighted yellow with '*', and foundings show by default. But 'Your news' is too broad to be useful (about 73% of everything, because about 20 nations count as neighbours). The shared 200-entry cap deletes the player's own founding story within about 7 game minutes. Building a Village never appears in the chronicle at all. Give the player's nation priority in both the filter and the retention, and chronicle player milestones. |
| s05 | Play toward the Village era as fast as you can on a fresh world and record the game time of each objective; note any regression, confusion, crash or stuck state along the way. | achieved | 3 | 33.8 | 8 | Village | 0 | 9 | The opening is quick and well signposted: settlement founded at 146 s, and the era panel says exactly what the Village needs. The middle is where it drags. Members 10/10 means poaching AI pawns through an opaque gift grind that wastes gifts on leaders and 'Loyal by nature' pawns. After that the Day 3 timer adds ~3.5 game minutes of waiting with nothing to do. AI nations racing to claim land, and hints in compass words with no compass, made founding the first settlement fiddly. |
| s06 | Re-check that command bar buttons no longer take a click meant for something else (#412): inside and outside your town, hover a button and click as pawns walk in or out of reach, double-click Enter Settlement, Exit Settlement and Craft Axe, and watch for buttons that briefly grey out after appearing. Report any click that landed on a different command, and whether the half-second wait ever got in the way. | achieved | 3 | 7.1 | 6 | - | 0 | 4 | #412 holds for real double-clicks. Every quick double-click on Enter Settlement, Exit Settlement and Craft Axe (where Craft Pickaxe slides under the cursor) was swallowed, and 65 clicks on pawn buttons while pawns came and went never hit another command. The wait never got in the way. But a slower second click (about 0.8 s or more, likely at this frame rate) still hits whatever replaces the button: twice it emptied the treasury through the unconfirmed Withdraw Funds, and once it silently planted a sapling. Consider not putting a different command in the slot a button just left, or giving Withdraw Funds a confirm. |
| s07 | Re-check what the player is told (#413): let Greenshire take your village (do nothing for a minute) and record exactly what the status line says when your nation falls; then open the chronicle (Y) and check your own nation's history (founding, tribute, siege, capture) is still listed. | achieved | 2 | 8.5 | 0 | - | 0 | 5 | The fall itself is clearly announced, but the status line is a five-line run-on that leads with a stale raid notice and then says both 'you are without a nation' and 'your nation is collapsing'. The #413 chronicle fix does not hold: once your nation falls, 'Your news' and 'Only my nation' filter out its raids, siege capture and join history (Only my nation keeps just the founding). The Defeat screen history is better but still never records that Greenshire captured the village. |
| s08 |  | no report | - | - | 7 | Village | 0 | 0 |  |

## Findings

### 1. [major] Double-click on Enter Settlement withdraws settlement funds (Withdraw Funds takes its slot inside)

- **Category:** bug, ux; **reported by** 2 agent(s): s03, s06
- **Also reported as:** "A slow second click on Enter Settlement still lands on Withdraw Funds, which takes the same slot and asks for no confirmation"
- **Detail:** Outside, Enter Settlement is the first button on the bottom row at (10-137, 476). Inside my own town, the Nation group comes first, so Withdraw Funds is in that slot and Exit Settlement is third. A quick double-click on Enter Settlement entered the town and then ran 'Withdrew 9 coins from settlement' (coins 339 -> 348). I expected Exit Settlement in Enter's slot (they are the same toggle), or the second click to do nothing.
- **Repro:** pt start ... --stage-settlement; be outside own town within 50m; pt click 74 476; pt click 74 476; pt events 3
- **Screenshots:** [dbl_enter_withdraw.png](s03/shots/dbl_enter_withdraw.png) [withdraw_after_enter.png](s06/shots/withdraw_after_enter.png)

### 2. [major] 'You fed N' reports residents as fed who are still counted hungry right after (fed 7 then 4 more with 8 present)

- **Category:** bug; **reported by** 1 agent(s): s02
- **Detail:** Tick 217390 in the Town of Yellowton: banner '7 members hungry: all here', button 'Feed Residents (7 hungry)'; click -> 'You fed 7 residents' (apples 15->8). The very next frame the banner says '4 members hungry: all here' and the button 'Feed Residents (4 hungry)'. A second click at 217562 -> 'You fed 4 residents' (apples 8->4). 11 feedings in 172 ticks while the nameplate reads '8 here, 9/20 residents', so at least 3 people were reported fed but stayed hungry. The same pattern (smaller) at 206038 (fed 6, then 3 more, 8 here), 221790 (7 then 1) and 224798 (3 then 1). Banner and button always agree with each other; it is the 'You fed N' result that does not mean 'N are no longer hungry'. Expected: one click leaves 0 hungry among those present, or the result says some still need food.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 103 --season winter; N to create a nation; gather 50 wood, F to found, E to enter; build 2-3 houses (K K); leave for ~2000 ticks, H home, E, click Feed Residents (553,476) and look at the banner/button straight after
- **Screenshots:** [feed13.png](s02/shots/feed13.png)

### 3. [major] Chronicle 'Your news' and 'Only my nation' drop your fallen nation's raids, siege and capture once it falls

- **Category:** bug; **reported by** 1 agent(s): s07
- **Detail:** Before the fall the recent feed starred 'Upperwood and Greenshire are now hostile', 'Greenshire raided Upperwood...', 'Frank Brown joined Upperwood'. After capture (tick 2503), Y > 'Your news' lists 7 of 22 entries: the founding and 'Upperwood passed into history' remain, but hostility, both raids, the join, the caravan and 'After a siege, a village of Upperwood fell to Greenshire' are gone. All nations + 'Only my nation' is worse: '24 entries. Listing 1.' (founding only). The filter seems keyed on the player's current nation (now none). Expected: your own nation's history stays listed after it falls. Unfiltered 'All nations' still shows them, unstarred.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 111 --stage-diplomacy; space; click Start, Begin; wait ~50 s doing nothing until capture at tick 2503; pt key y; then click 'All nations' and tick 'Only my nation'
- **Screenshots:** [chron_yours.png](s07/shots/chron_yours.png)

### 4. [major] Chronicle's shared 200-entry cap evicts the player's own founding history within ~7 game minutes

- **Category:** bug; **reported by** 1 agent(s): s04
- **Detail:** At tick ~21000 the full chronicle header reads '335 entries; the latest 200 are kept.' With 'Only my nation' ticked it lists 5 entries; at tick ~12400 the same view listed 16. 'You raised a banner and named your people Westford', 'You laid the first stones of Westford', Daisy Davies/Smith joining and their stalls are all gone. About 40-50 NPC entries per game minute (mostly other nations' raids, rivalries and foundings) fill the cap, which all nations share, so the player's oldest and most personal entries are the first to go. Expected the player's own nation's entries to be kept, or a separate cap for them.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 1; create a nation (N), found a settlement (F) by tick ~6000; play until tick ~21000; Y to open the full chronicle; tick 'Only my nation'
- **Screenshots:** [own_history_evicted.png](s04/shots/own_history_evicted.png)

### 5. [major] Members 10/10 Village gate forces poaching AI pawns via long gift grinds, with gifts wasted on Loyal or unrecruitable pawns

- **Category:** balance; **reported by** 1 agent(s): s05
- **Detail:** No unaffiliated settlers could be found; every nearby pawn belonged to my nation or an AI nation. Recruiting means gifting a foreign pawn from Stranger through Acquaintance and Friend to Loyal, about 6-8 gifts each (10 coins, then 1 apple, then 5 wood per gift), and then pressing R. Gifts are still accepted (and spent) after a pawn is already Loyal (second apple to Harriet Roberts). Frank Wilson took gifts all the way to Loyal and only then said 'Fortresstown is my home... (Loyal by nature)', so the trait that blocks recruiting is not warned about before you spend. The nation leader (Charlie Thompson) also accepts gifts although he can never be recruited. Pawns wander out of the 5 m reach between gifts. Recruiting 6 members took about 18,000 ticks and drained all my coins and apples.
- **Repro:** pt start --seed 2026; found a settlement; walk to a nearby AI town (Fortresstown); pt walkto pawn; pt key r (says 'needs Loyal'); pt key g repeatedly; pt key r
- **Screenshots:** [inv4.png](s05/shots/inv4.png)

### 6. [major] Pawn leaving reach drops Buy Pickaxe/Buy Axe into Talk's slot; a click meant for Talk spent 299 coins

- **Category:** bug; **reported by** 1 agent(s): s03
- **Detail:** Inside my own town the Craft group (Buy Axe/Buy Pickaxe) is laid out after the People group, and rows stack upward. With a pawn in reach Talk is at (10-313, 414) and the Buy buttons sit on the Order row above; with no pawn, the Buy buttons fall to y=414 starting at x=10, exactly where Talk was. I hovered Talk at (65,414), the pawn walked out of reach, I clicked, and got 'Bought 1 pickaxe for 299 coins from Frank Thompson' (coins 348 -> 49). The purchase is not confirmed. The reverse happens too: hovering Buy Axe at (65,414) with no pawn, a pawn walking in put 'Talk to Victor Taylor (Stranger)' under the cursor (shots/pawn_in_over_buy.png). Expected: putting the Craft group last stops it moving when a Craft button is removed, but because it comes after People it moves every time a pawn comes or goes. It needs its own fixed slot, or the People rows need to sit above it.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 109 --stage-settlement; Start, Begin; click Enter Settlement (74,476); with 339 coins Buy Axe/Buy Pickaxe show; pt move 65 414 and wait for a pawn to walk out of reach (pt state nearest pawn > 5m); pt click 65 414
- **Screenshots:** [pawn_out_click.png](s03/shots/pawn_out_click.png)

### 7. [major] Player's own nation conquered with no status message; a stranger's fall gets one

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Greenshire's 12-round siege took Upperwood's (the player's) last village at tick 18003. The status line at that tick said only 'Autosaved.'; no 'Greenshire took your village / Upperwood is no more' line. 248 ticks later the status announced 'Cityton has fallen and been disbanded.' for an unrelated nation. The HUD silently flips to Nation: none, Create/Join Nation buttons, and a red, unexplained 'Collapsing! 4:40 left' timer; Objective 5 still says 'Enter your settlement' though the player has none. Expected a loud, explicit status (and ideally a sheet) when your own nation is destroyed.
- **Repro:** pt start --profile beyond-nations --game-dir /root/beyond-nations --seed 111 --stage-diplomacy; Start, Begin; Tribute Greenshire once; play/wait ~15000 ticks until 'Your nation's shelter is over'; let Greenshire's siege reach 12/12 (tick ~18003); read status line / events.
- **Screenshots:** [conquered_hud.png](s01/shots/conquered_hud.png)

### 8. [minor] Objective 'Build a market stall' completes when an AI pawn builds one

- **Category:** balance, ux; **reported by** 3 agent(s): s02, s04, s05
- **Also reported as:** "'Build a market stall' objective is completed by an NPC, not the player"; "Tutorial objectives 'Eat something' and 'Build a market stall' complete without the player doing them"
- **Detail:** Objective 6 completed at tick 187200 because Victor Evans built a stall while I had 8/20 wood; the text said 'press B inside your settlement'. The Market panel also lists merchants that do not match the chronicle's builders.
- **Repro:** found settlement, enter, wait
- **Screenshots:** [stall.png](s02/shots/stall.png) [strip2.png](s04/shots/strip2.png) [stall-free.png](s05/shots/stall-free.png)

### 9. [minor] Double-click on Exit Settlement plants a sapling in the town square, with no message

- **Category:** bug, ux; **reported by** 2 agent(s): s03, s06
- **Also reported as:** "A slow second click on Exit Settlement lands on Plant Sapling outside and silently uses up the sapling"; "Planting a sapling gives no status message"
- **Detail:** Exit Settlement is at (314-441, 476) inside town; outside, Plant Sapling sits in that slot. When I double-clicked Exit, saplings went 2 -> 1 and a tree appeared next to the well in the town square. The status line only said 'Exited settlement.', so nothing tells the player why a tree is there.
- **Repro:** carry saplings; inside own town pt click 377 476; pt click 377 476
- **Screenshots:** [dbl_exit.png](s03/shots/dbl_exit.png) [slow_exit_planted.png](s06/shots/slow_exit_planted.png) [slow_exit_planted.png](s06/shots/slow_exit_planted.png)

### 10. [minor] Inventory Info panel jumps from the left column to the middle of the screen whenever pawn rows appear

- **Category:** ux; **reported by** 2 agent(s): s03, s06
- **Also reported as:** "Inventory panel jumps to the middle of the screen when pawn rows grow the command bar inside town"
- **Detail:** In town with no pawn in reach, Inventory Info is in the left column (x~10). When a pawn steps into reach and the bar gains its rows, the panel moves to x~220, y~195, over the 3D view, and it goes back when the pawn leaves. It also hops between in-town and out-of-town layouts.
- **Repro:** stand in own town; compare screenshots with and without a pawn within 5m
- **Screenshots:** [feedpoll5.png](s03/shots/feedpoll5.png) [loop14.png](s06/shots/loop14.png)

### 11. [minor] AI nations starve heavily in winter: 12 hunger deaths, 4 of them leaders

- **Category:** balance; **reported by** 1 agent(s): s02
- **Detail:** 12 PawnDeath (all hunger, all 'within the walls') in AI nations, none in mine. Leaders: Frank Taylor (Bigfield), Xavier Thomas (Smallshire), Wendy Walker and then her successor Grace Lewis 2800 ticks later (Bigville). AI settlements cannot feed themselves while trees still give apples.
- **Repro:** grep PawnDeath out/events.jsonl after a winter run with seed 103
- **Screenshots:** [final_state.png](s02/shots/final_state.png)

### 12. [minor] Action button rows overflow the window bottom in a settlement, hiding Feed Residents

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** In the second settlement with a pawn nearby the action panel grows to 5 rows; the bottom row (Withdraw Funds / Collect Stockpile / Exit Settlement / Feed Residents) is cut off by the 540px window edge and Leave Nation disappears. Feed Residents is the key winter action.
- **Repro:** after reaching Village era, found a 2nd settlement (F 250+ away), enter it (E) and wait for a pawn to come near
- **Screenshots:** [t33.png](s02/shots/t33.png)

### 13. [minor] After conquest, Chronicle 'Your news' loses all of the fallen nation's history

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Once nationless, 'Your news' lists 14 entries (other nations' foundings + the two epitaphs). Upperwood's tribute, war declaration, raids, siege and the 'After a siege, a village of Upperwood fell to Greenshire' capture line are all gone from it. Expected 'Your news' to keep the player's past nation's entries.
- **Repro:** As the major finding above; after the conquest press Y and pick 'Your news'.
- **Screenshots:** [epitaph_yournews.png](s01/shots/epitaph_yournews.png)

### 14. [minor] Build House / Build Farm buttons are removed (not disabled) when the town has no room, so their neighbours slide left

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** When the town grew from 7 to 10 buildings, Build House disappeared and Build Farm and Send Caravan slid left into its slot. At 11 buildings Build Farm also went, and Send Caravan moved from x~450 to x~297. The code disables buttons that are 'only unavailable for now', but a Build button for a full town is removed, which moves the Trade buttons after it.
- **Repro:** stay in own town while the town grows (stage-settlement town reaches 11 buildings in a few game minutes); watch the Purchase Food row
- **Screenshots:** [build_house_gone.png](s03/shots/build_house_gone.png)

### 15. [minor] Chronicle strip lines cut off with '...' before the key fact

- **Category:** text; **reported by** 1 agent(s): s04
- **Detail:** At 960x540 the 5-line strip truncates almost every line, often at the important part: 'Relations between Westford and Littleland are n...' (friendly or rival is cut off), 'You raised a banner and named your people We...', 'The nation of Westland was born, with Yvonne J...'. There is no wrap or hover. The full view (Y) wraps correctly.
- **Repro:** seed 1; play past tick ~3000; read the strip under the top bar
- **Screenshots:** [strip_ignores_onlymine.png](s04/shots/strip_ignores_onlymine.png)

### 16. [minor] Collapse countdown runs silently: no status reminder during the 5 minutes

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** After the fall the status line went blank (only 'Autosaved.' and another nation's fall) for the whole 5-minute window; only the small red HUD 'Collapsing! m:ss left' tracked it. Siege used status progress (10/12); collapse could too.
- **Repro:** as above; poll status between ticks 2503 and 17503
- **Screenshots:** [ending_history.png](s07/shots/ending_history.png)

### 17. [minor] Day 3 gate dominates the Village speed-run; all other requirements were done ~10,700 ticks earlier

- **Category:** balance; **reported by** 1 agent(s): s05
- **Detail:** Village 1/1, Members 10/10 and Stalls 2/2 were all met by tick ~40,300, but the era only changed at tick 51,003 when Day 3 started. There was nothing to do to speed it up. The hint 'An era is not built in a day: hold on' is clear, but the wait felt dead. Meanwhile AI Fortresstown was already a Town with 7 buildings on day 2.
- **Repro:** Complete the Village requirements early; watch the Era panel until Day 3
- **Screenshots:** [members10.png](s05/shots/members10.png)

### 18. [minor] Death toasts for foreign pawns lack the nation and re-announce earlier deaths

- **Category:** text; **reported by** 1 agent(s): s02
- **Detail:** Toasts such as 'Olivia Thomas has died.' appear for other nations' pawns with no nation named. Zach Lewis is announced at 224046, again in 'Zach Lewis and Jasmine Watson have died.' at 224410, then '3 pawns have died. Isabella Roberts is now the leader of Smallshire.' at 224534.
- **Repro:** play through winter; watch the top toast
- **Screenshots:** [t27.png](s02/shots/t27.png)

### 19. [minor] Default 'Your news' barely filters: 73% of all entries, so own news is drowned out

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** Full chronicle (Y), default 'Your news': 'Listing 100' of 137 entries at tick ~12400, and 111 of the 200 kept at tick ~21000. Most of it is other nations' rivalries, alliances, raids and stalls (e.g. 'Raiders from Westburg struck Northernland', 'Castleton and Blueton have become rivals'). About 20 NPC nations appear on day 1, so 'neighbours' ends up meaning nearly everyone. My own entries are highlighted yellow with '*', which helps, but there are only a few among about 100 lines. The 'Only my nation' checkbox (16 of 137) is the view that makes my news easy to find, but it is off by default and is a second control stacked on the Your news / All nations switch.
- **Repro:** seed 1; play to tick ~12000 as leader of a nation with a settlement; Y; compare 'Listing N' for Your news, All nations (U) and 'Only my nation'
- **Screenshots:** [full_yours2.png](s04/shots/full_yours2.png)

### 20. [minor] Defeat screen 'History of Upperwood' omits the capture and who took the village

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** End-of-run history lists founding, hostility, two raids, caravan, join, 'Upperwood lost its last settlement', collapse; but not the third raid nor 'After a siege, a village of Upperwood fell to Greenshire'. The history never says Greenshire took it. No siege-start entry exists anywhere in the chronicle, and no tribute demand happened in this run, so 'tribute' could not be checked.
- **Repro:** as above, then keep waiting ~5 game minutes until END_OF_RUN (tick 17503)
- **Screenshots:** [ending_history.png](s07/shots/ending_history.png)

### 21. [minor] Fall status line glues a stale raid notice onto the capture news and contradicts itself

- **Category:** text; **reported by** 1 agent(s): s07
- **Detail:** Verbatim status at tick 2503: 'Greenshire raided your village: 2 coins taken. Relations: Hostile (-94); gifts or peace end raids. Greenshire has captured your village! Upperwood has fallen: it has no settlements left. You are without a nation. Your nation is collapsing! Found a settlement or win a follower within 5 minutes, or your story ends.' It wraps to 5 lines; advice about gifts ending raids is moot once you have lost; 'Upperwood has fallen... You are without a nation' then 'Your nation is collapsing!' is contradictory (Player Info says Nation: none, yet a HUD timer 'Collapsing! 4:59 left' runs and Kingdom 0/5 / Era Town (3/4) stay shown). Expected: a capture message leading, and one clear statement of what is left to do.
- **Repro:** as above; read the status bar at tick 2503
- **Screenshots:** [fall_status.png](s07/shots/fall_status.png)

### 22. [minor] Founding: AI nations claim the 'open ground' the rejection hint points to, and compass directions have no on-screen compass

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** Pressing F with 50 wood was rejected 3 times ('Too close to the Village of Yellowport... Open ground lies about 120 to the south', then Northville, then Westford). Each time an AI nation had founded a camp where the hint pointed (Westford was founded at tick 6453 while I walked there). The hints use compass words (south, south-east, north-west), but there is no compass or north marker on screen, so a player cannot tell which way to go. It took 4 attempts and about 2,650 ticks to place the first settlement. The rejection toast is also quickly overwritten by 'Autosaved.'.
- **Repro:** pt start --seed 2026; chop to 50 wood near spawn heading toward (118,-269); pt key f; follow the hint direction; pt key f
- **Screenshots:** [found3.png](s05/shots/found3.png)

### 23. [minor] Frank Wilson refused to join as 'Loyal by nature' but later appears as a member at my settlement

- **Category:** bug; **reported by** 1 agent(s): s05
- **Detail:** At tick ~36000 Frank Wilson said 'Fortresstown is my home. I won't turn my back on it. (Loyal by nature)'. At tick ~52000 he stood at my settlement with 'Order Frank Wilson: Gather Wood' and 'Talk to Frank Wilson (Loyal)' buttons, as if he had joined after all. No join message was seen. This is either inconsistent or a misleading button.
- **Repro:** See the recruiting finding; teleport home after Members 10/10
- **Screenshots:** [village.png](s05/shots/village.png)

### 24. [minor] H (Teleport Home) can land outside interact range, so H then E fails

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** H put me at (774.7,-12.7), ~9 units from the settlement centre; E said 'No entities within range to interact with.' and the 'Enter Settlement' button looked enabled but also failed. Other times H+E worked.
- **Repro:** from far away press h, then e
- **Screenshots:** [home_out_of_reach.png](s02/shots/home_out_of_reach.png)

### 25. [minor] Nations header 'An alliance needs +30' hides the size rule (+60 for a small partner)

- **Category:** text; **reported by** 1 agent(s): s01
- **Detail:** At Friendly (+35) Southernfield declined: 'they are 8 to your 4 and want relations of +60 before taking on so small a partner'. The header still says an alliance needs +30, so the player spends a 25-coin gift chasing the wrong number. Expected the per-nation threshold on the row or Ally button.
- **Repro:** pt start ... --stage-diplomacy; O; Ally on Southernfield (declines at +25); Tribute Southernfield (+35); Ally again.
- **Screenshots:** [ally1.png](s01/shots/ally1.png)

### 26. [minor] Pawn action buttons are drawn over the Objective and Inventory panels inside a settlement

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** Inside my settlement with a pawn in reach, a second set of buttons (Order X: Gather Wood, Talk to X, Gift X, Appoint X Merchant, Craft Axe, Craft Pickaxe, Send Caravan) appears on top of the Objective 7 panel and the Inventory panel, so neither can be read.
- **Repro:** Found a settlement; pt key e inside it while a pawn stands within 5 m
- **Screenshots:** [k2.png](s05/shots/k2.png)

### 27. [minor] Pawn nameplates and buttons do not show nation membership, so recruit targets cannot be found

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** Nameplates show name, trait and activity only. The talk button reads 'Talk to Frank Lewis (Stranger)' for a pawn who is already in my nation. Pressing R on nearby pawns kept returning '! Frank Lewis is already a member of Townton.' Nothing on screen marks who is recruitable or which nation a pawn belongs to.
- **Repro:** After founding: pt walkto pawn; pt key r
- **Screenshots:** [invite2.png](s05/shots/invite2.png)

### 28. [minor] Player's own milestones (Farm/House built, settlement became a Village) never reach the chronicle

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** K,K built a Farm ('Farm built.', tick 11217), and a House made Westford a Village ('House built. The settlement is now a Village!', tick 13901). Neither wrote a chronicle entry. At those moments the 'your news' strip showed only other nations' raids and rivalries. NPC stalls ('A new stall opened in the market of Bluewood') and NPC settlement foundings are chronicled. Expected '* Westford grew into a Village' highlighted in my news.
- **Repro:** seed 1; found settlement; gather 25 wood/10 stone; inside settlement K K (Farm); then 20 wood/15 stone K K (House); watch strip and Y full view
- **Screenshots:** [village_not_chronicled.png](s04/shots/village_not_chronicled.png)

### 29. [minor] Player's own offensive siege ends silently and its progress is never shown

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Declared war on Castlefield (2 members, camp) at tick 2239; status 'Your nation is besieging Castlefield's camp.' at 2251, then nothing. Castlefield grew to 5 members/a village, out-strengthened us and started raiding us at tick 3503. No status or chronicle line said our siege broke, and our own x/12 siege progress is shown nowhere (HUD or Nations screen). Expected a progress readout and a 'your siege of X was broken' message.
- **Repro:** pt start ... --stage-diplomacy; O; War, Confirm on Castlefield (after it appears ~tick 1600); Esc; wait ~2000 ticks; O.
- **Screenshots:** [war_castle.png](s01/shots/war_castle.png)

### 30. [minor] Purchase Food error gives two possible reasons and contradicts the listed apple price

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** At my own market with 231 coins: 'No one is selling food at this market or you do not have enough money to purchase food.' The Market panel lists Apples at 80 buy. The player cannot tell which reason applies.
- **Repro:** inside own settlement in winter click Purchase Food (66,445)
- **Screenshots:** [purchase_food_ambiguous.png](s02/shots/purchase_food_ambiguous.png)

### 31. [minor] Season panel says 'Winter: no harvests' but trees yield apples all winter

- **Category:** text; **reported by** 1 agent(s): s02
- **Detail:** Every tree chopped in winter gave 1-3 apples ('Gathered 6 wood and 3 apples from tree'); trees were my whole food supply. Either the label is misleading or winter food is unintentionally easy.
- **Repro:** start with --season winter; walkto tree; e
- **Screenshots:** [winter_apples.png](s02/shots/winter_apples.png)

### 32. [minor] Settlement tier 'Town' vs nation era 'Camp' read as contradictory

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** After a 3rd house: 'The settlement is now a Town!' and 'Tier: Town (5 buildings)', but the Era panel still says 'Camp (1/4)' and Objective 7 still asks to 'Reach the Village era'.
- **Repro:** build 3 houses + farm in the first settlement before 10 members
- **Screenshots:** [tier_vs_era.png](s02/shots/tier_vs_era.png)

### 33. [minor] Strip ignores 'Only my nation' and churns too fast for own entries to stay visible

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** With 'Only my nation' ticked in the full view, the strip still says 'your news' and shows other nations ('Wendy Davies raised a banner...', 'Smallshire and Castleton are now hostile...'). Because there are so many entries (335 in ~7 game min), my own highlighted line is pushed off the 5-line strip within seconds. Example: 'You laid the first stones of Westford' was gone within one batch of rivalries.
- **Repro:** Y to full, tick 'Only my nation', Close, Y to recent strip
- **Screenshots:** [strip_ignores_onlymine.png](s04/shots/strip_ignores_onlymine.png)

### 34. [minor] When a caravan leaves, the Trade row disappears and every pawn/craft row drops a row

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** Outside town near a Fortressshire caravan, the rows were Place / Escort Caravan / Talk+Gift / Order+Craft Axe. When the caravan moved off, the Trade row vanished and every row above it shifted down by one: Talk 414->445, Order 383->414, Craft Axe (337,383)->(337,414). A click aimed at Talk lands on 'Order <pawn>: Gather Wood', and my double-click on Craft Axe hit empty space. The People row starting a new row stops sideways shifts, but rows still stack upward, so a lower group going away moves everything above it.
- **Repro:** pt start ... --stage-settlement; Teleport Home while a caravan is by the town; note the Talk position; wait for the caravan to leave
- **Screenshots:** [dbl_craft.png](s03/shots/dbl_craft.png)

### 35. [polish] Objective progress bar label clipped on the left ('nbers 9/10 • Buildings 2/2')

- **Category:** text; **reported by** 2 agent(s): s02, s03
- **Also reported as:** "Objective 7 progress bar text clipped on the left ('pers 10/10 • Buildings 2/2')"
- **Detail:** Objective 7 bar text is right-aligned and overflows to the left, showing 'nbers' instead of 'Members'. The settlement nameplate ('8 here, 9/16 residents') also renders huge across the top HUD.
- **Repro:** reach objective 7
- **Screenshots:** [obj7_clipped.png](s02/shots/obj7_clipped.png) [objective_clipped.png](s03/shots/objective_clipped.png)

### 36. [polish] Settlement's floating label renders huge and clipped across the top of the screen

- **Category:** ux; **reported by** 2 agent(s): s04, s05
- **Also reported as:** "Settlement floating label is huge and overlaps the top HUD"
- **Detail:** Near the well, the settlement label ('0 here, 1/8 residents', later '2 here, 3/16 residents') is drawn in giant letters across the top of the screen, behind the chronicle strip and the toast.
- **Repro:** found a settlement and stand at its well
- **Screenshots:** [settlement_label_huge.png](s04/shots/settlement_label_huge.png) [found4.png](s05/shots/found4.png)

### 37. [polish] A button that is still settling looks the same as one that is unavailable

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** During the 0.5 s wait, buttons are drawn with the same grey as unavailable ones such as Sell Resources and Build Farm (shots/dbl_exit.png: the whole outside bar is grey for a moment after Exit). This is easy to see and never got in the way: pawn-row Talk buttons became usable about 1 s after a new pawn's row appeared. But a player cannot tell 'wait a moment' from 'you can't do this'.
- **Repro:** click Exit Settlement and pt shot immediately
- **Screenshots:** [dbl_exit.png](s06/shots/dbl_exit.png)

### 38. [polish] Chronicle plural error 'taking 1 coins'; odd nation names; 32 nations standing after day 1

- **Category:** text; **reported by** 1 agent(s): s07
- **Detail:** 'Raiders from Greenshire struck Upperwood, taking 1 coins.' Generated names 'Towntown', 'Hamlettown' read as placeholders. Defeat screen 'Nations still standing: 32' after one day seems very high for the world.
- **Repro:** as above; read the chronicle / pt events
- **Screenshots:** [ending_history.png](s07/shots/ending_history.png)

### 39. [polish] First War click relabels the button 'Confirm' but the prompt says 'Click War again'

- **Category:** text; **reported by** 1 agent(s): s01
- **Detail:** After one click on War (Nations screen) the button reads 'Confirm' while the status says 'Click War again to declare war on Southernfield.' There is no War button left to click. Expected 'Click Confirm...' or the label left as War.
- **Repro:** pt start ... --stage-diplomacy; O; click War on Southernfield once.
- **Screenshots:** [war1.png](s01/shots/war1.png)

### 40. [polish] First tree toast says 3 apples but only 2 are added

- **Category:** text; **reported by** 1 agent(s): s05
- **Detail:** 'Gathered 5 wood and 3 apples from tree.' while the floating text says '+2 apples' and the inventory shows 2. One was probably auto-eaten (energy 84 to 98), but nothing says so.
- **Repro:** pt start --seed 2026; pt walkto tree; pt key e
- **Screenshots:** [apples-mismatch.png](s05/shots/apples-mismatch.png)

### 41. [polish] Own member called 'Stranger' and asks why they should follow you

- **Category:** text; **reported by** 1 agent(s): s04
- **Detail:** Daisy Davies had joined Westford ('Westford welcomed Daisy Davies into its ranks') and runs its market. Talking to her shows 'Daisy Davies (Stranger): "Yes, leader? I hardly know you. Why should I follow you?"'. The objective progress label 'Members 3/10' is also cut off ('nbers 3/10') in the objective bar.
- **Repro:** after Daisy joins, walkto pawn; e
- **Screenshots:** [recruit.png](s04/shots/recruit.png)

### 42. [polish] Raid message pluralization: '1 coins taken'

- **Category:** text; **reported by** 1 agent(s): s01
- **Detail:** Status and chronicle say 'Castlefield raided your village: 1 coins taken.' and 'Raiders from Castlefield struck Upperwood, taking 1 coins.' Expected '1 coin'. Similar: chronicle 'A caravan from Southernfield sold nothing at the Village of Upperwood for 0 coins.' (tick 3027) is noise.
- **Repro:** As above, at war with Castlefield; read raid status at tick 15503.
- **Screenshots:** [siege4.png](s01/shots/siege4.png)

## Objective timings

Game seconds from the start of the world to each objective (event tick / the game's ticks per second, from events.jsonl). Game time, not wall time: with many sessions on one machine a world can run slower than real time, and wall-clock timings would mostly measure the machine.

| # | Objective | s01 | s02 | s03 | s04 | s05 | s06 | s07 | s08 | min | median | max |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Gather wood | 122.1 | 3631 | 71 | 47.1 | 20 | 139 | - | 3629 | 20 | 122.1 | 3631 |
| 2 | Eat something | 123 | 3632 | 72.1 | 48 | 21.1 | 140 | - | 3630.1 | 21.1 | 123 | 3632 |
| 3 | Create or join a nation | 124.1 | 3633 | 73 | 64 | 27.1 | 141 | - | 3631 | 27.1 | 124.1 | 3633 |
| 4 | Found a settlement | 125 | 3739 | 74.1 | 120 | 146 | 142 | - | 3691 | 74.1 | 142 | 3739 |
| 5 | Enter your settlement | - | 3742 | 106.1 | 125.1 | 151.1 | 180 | - | 3695 | 106.1 | 165.6 | 3742 |
| 6 | Build a market stall | - | 3744 | 107 | 131.1 | 152 | 181 | - | 3696.1 | 107 | 166.5 | 3744 |
| 7 | Reach the Village era | - | 4500 | - | - | 1020.1 | - | - | 3772 | 1020.1 | 3772 | 4500 |
| 8 | Sell resources at market | - | 4570 | - | - | 1021 | - | - | - | 1021 | 2795.5 | 4570 |
| 9 | Found a second settlement | - | 4623 | - | - | - | - | - | - | 4623 | 4623 | 4623 |

## Problems with the run

- s08: report.json missing

