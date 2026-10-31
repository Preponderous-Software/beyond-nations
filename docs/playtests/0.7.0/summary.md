# Playtest summary: 064c-20261029-2052

10 sessions, build beyond-nations 0.6.3 (a44aa8d), generated 2026-10-29 21:24.

**Findings:** 74 reports in 64 groups -- 0 blocker, 10 major, 46 minor, 18 polish.

## Outcomes

| Session | Goal | Outcome | Fun | Minutes | Objectives | Deaths | Findings | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| s01 | Play the first 10 minutes as a brand-new player who has never seen the game; note every moment of confusion. Also check specifically: after you found a settlement and enter it, does the 'Build a market stall' objective complete only when YOU build a stall, or do residents build stalls in your own settlement first (they should not)? | achieved | 3 | 16.8 | 6 | 0 | 10 | The opening is clear and well paced: objectives at the top left, helpful refusal messages and a good controls screen got a new player to a nation, a settlement and a stall in under 4 game minutes. Residents correctly leave your market alone, so the stall objective completes only on the player's own B press. The biggest snag is founding a settlement: direction hints with no compass sent me wandering through a crowded map for over a minute. After that, Collect Stockpile hands over so many resources that the gathering objectives feel pointless. |
| s02 | Reach the Village era as fast as you can (it cannot come before day 3, about 17 minutes of game time); record the game time of every objective. | partial | 3 | 22.1 | 6 | 0 | 6 | The Camp loop (chop, eat, nation, settle, stall, build) is clear, and the era checklist with its [x] items is a very good guide. The stockpile collection (76 wood / 74 stone) made the buildings fast. Two things slow the player down: NPC nations grab land so fast that founding becomes a chase after moving 'open ground' hints, and the 10-member gate leaves almost no unaffiliated settlers to recruit. I could not reach the Village era within budget: 6/7 objectives were done, and Members (3/10) and the day-3 gate were still outstanding. |
| s03 | Survive the first winter. Start with `--season autumn` (a fresh world otherwise reaches winter only after about 57 minutes of game time). Note how food and cold feel. | partial | 3 | 24.4 | 6 | 0 | 9 | The opening is clear and well signposted: helpful 'open ground lies 150 to the north-east' hints, quick objective rewards and a good bandit-camp world-event choice. But the winter goal could not be reached because the simulation ran at 3-30 tps, and food and cold never pressed. Apples pile up, eating is automatic and indoors costs no energy, so 'preparing for winter' asks nothing of the player. The season countdown also counts whole days and overstates the time left. |
| s04 | Diplomacy and war: lead a nation, meet a neighbour, try alliance, rivalry and war from the Nations screen (O). This build adds an envoy: every diplomacy act that takes effect (gift/tribute, alliance, denunciation, war, peace) sends your envoy to that nation and blocks further acts toward that nation for 1,000 ticks (20 s of play, which pass only while the Nations screen is closed). Check that acts toward one nation now take game time, that the status line and the Nations screen row explain the wait, that a refused offer sends no envoy, and that each nation has its own envoy. Record the game tick of each act. | achieved | 3 | 16.2 | 3 | 0 | 5 | The envoy works as specified. Acts that take effect lock only that nation for 20 s of play, refusals (Ally at 0/+28/+41, Peace at -60) send no envoy, three nations had envoys out at once, and both the row ('envoy back in N s', counting down correctly) and the status line explain the wait. The biggest risk is the list re-sorting after an act combined with the two-click confirm, which aimed my second Denounce at the wrong nation. The alliance refusal also understates the threshold (+30 shown, +60 needed against a bigger nation). Small polish items: greyed buttons during the wait and an 'envoy returned' notice would make the pacing readable outside the O screen. |
| s05 | Trade and caravans: build a market stall, buy and sell, get a Storehouse and send a caravan. Check specifically that residents of YOUR own settlement leave the market stalls to you (they should not fill your market), and that the 'Build a market stall' objective completes only from a stall you build. | partial | 3 | 17.9 | 6 | 0 | 6 | The #423 fix holds: in my own settlement residents built no stalls, and 'Build a market stall' completed from my own B press. Trading did not work at home, though. A resident took my new stall at once with 0 coins, so I could not buy or sell there and had to travel to a foreign Town. Caravans need a Storehouse, which unlocks only in the Town era, too far off for a short session. The error messages are clear and specific, and gathering resources and founding a settlement feel quick. |
| s06 | Ruler, laws and heirs: lead a nation, enact and repeal laws, and see what happens when the ruler dies. | partial | 3 | 14.6 | 0 | 1 | 7 | The laws loop works and reads well: changes are announced, show a live 30 s countdown and land in the chronicle, and the 'The Rule Passes On' screen with its numbered line is a strong moment. But Rationing never appears in the Laws tab at 960x540, the heir is hidden until after death, and a ruler cannot realistically die in a session (I needed --stage-succession, which my goal justified), so the dynasty fantasy is mostly out of the player's reach. Coin gifting with no cooldown makes recruiting heirs and defectors trivially cheap. |
| s07 | Crafting: find and use every recipe on the crafting screen (C); are tools worth making? | partial | 3 | 18.8 | 6 | 0 | 7 | Tools are clearly worth making. The axe adds +3 wood to a base of about 4-6 per tree (about 65% more) and pays back its 4 wood within 2 trees, and the pickaxe adds +2 to a base of 5 stone. The crafting screen explains costs, readiness, uses left and locks very well. But only 3 of 5 recipes can be used early: Planks and Bricks sit behind a Village era that needs 10 loyal members. Cooked Meat is a trap: it is auto-eaten for small top-ups, there is no Eat control, and it sells for less than its ingredients. |
| s08 | World events: play until world events are offered, take different choices, and judge whether the consequences are clear and fair. | blocked | 2 | 29.8 | 5 | 0 | 5 | I could not test the world-event choices: with the simulation at 5-20 tps, the first player event (420-660 game s) never arrived within 30 wall minutes. Even at full speed a player waits 7+ minutes with no hint that events exist. The one NPC world event I saw was a single plain chronicle line, drowned within a minute by dozens of raid and relation lines, so as built world events are close to invisible to the player. The early loop (wood, nation, settlement) is clear and well signposted apart from the compass-less placement hints. |
| s09 | Accessibility and text audit, touching only what a mouse-and-keyboard player would: every screen and panel; cut-off, overlapping, unclear or misspelled text; contrast; anything only explained on hover. | achieved | 3 | 15.8 | 3 | 0 | 16 | Text is mostly well written and destructive actions are safely confirmed. The Controls pages, Crafting list and error messages are clear. Accessibility suffers from translucent UI over a bright world: contrast collapses to about 2:1 on action buttons and the light-blue menus, and the HUD overlaps itself once a settlement adds panels and 13 buttons. No tooltips exist anywhere, so disabled buttons and jargon go unexplained, and menus can't be driven from the keyboard. |
| s10 | Save/load and share-seed round trip: save from the pause menu, stop, `pt start --continue`, confirm the world is identical; then start a new world from the seed shown on the Share screen and confirm it matches. This build changed the save format (v6) to store diplomacy envoys. Before saving, lead a nation and make at least one diplomacy act on the Nations screen (O) so an envoy is on the road; save while it is away, quit, `pt start --continue`, and confirm the envoy is still away with the same remaining time (Nations screen row 'envoy back in N s') and the rest of the world matches. | achieved | 3 | 9.1 | 0 | 0 | 3 | The v6 save keeps diplomacy envoys correctly. An envoy sent at tick 1686 and saved at tick 2206 came back after --continue showing 'envoy back in 6 s' at tick 2406, which matches the math exactly. Relations (+10), coins, position, energy, objective and nation/leader role also survived, and the envoy returned on schedule. The Share seed (1450162717) typed into New World rebuilt an identical starting world. The rough edges are all diplomacy feedback: a stale countdown in the status line, cooldown buttons that look active, and no notice when the envoy returns. |

## Findings

### 1. [major] Founding a settlement: NPC nations claim the open ground the F hint points to before the player gets there

- **Category:** balance, ux; **reported by** 2 agent(s): s01, s02
- **Also reported as:** "Founding a settlement turns into a long hunt: the 'open ground' hints keep changing direction and there is no compass to follow them"
- **Detail:** With 50 wood in hand at tick ~4400 I pressed F and followed the hint ('open ground about 40 to the south-east', '11 to the east', '30 to the north'...) seven times. Each time an NPC (Fortressburg, Fortressshire, Blueport) founded or extended a claim on that spot just before I arrived, and the hint jumped 170-230 units away. The world went from 1 to 15+ nations within the first in-game day. Founding took 6700 ticks (134 game s), more than the rest of the Camp objectives combined. I expected the pointed-to ground to still be free a few seconds later, or the hint to account for NPC settlers heading there.
- **Repro:** pt start --profile beyond-nations --seed 102; gather 50 wood near spawn; press f; follow the direction in the status message; press f again; repeat
- **Screenshots:** [chase.png](s02/shots/chase.png) [found_hunt.png](s01/shots/found_hunt.png)

### 2. [major] Context action buttons are translucent and drop to ~1.8:1 contrast over bright grass/sky

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Buttons like 'Talk to Olivia Taylor (Stranger)', 'Gift Olivia Taylor', 'Invite...', 'Propose Alliance to Greatport', 'Gift Greatport 25 Coins', 'Craft Pickaxe' are white text on a translucent teal; over sunlit grass the measured background is ~#75D59B, giving ~1.8:1 contrast. Over sky 'Gift Greatport 25 Coins' is nearly invisible. Disabled buttons ('Found Settlement', 'Enter Settlement', 'Plant Sapling') are ~2.5:1 and indistinguishable from enabled translucent ones. Expected opaque buttons with >=4.5:1 text.
- **Repro:** same as above; take screenshots near Greatport in daylight
- **Screenshots:** [button_contrast.png](s09/shots/button_contrast.png)

### 3. [major] HUD panels overlap each other near a settlement: Inventory jumps to centre under the Tip box, Player Info loses its title

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Walking up to Greatport's settlement and talking to a pawn: the Inventory Info panel moves to the screen centre and the Tip box draws on top of it, hiding 'Chicken Meat' and 'Axes: 1 (20 uses)'. After entering and with the Market Info panel growing, the right column runs out of height and the 'Player Info' title disappears; its energy bar and nation lines appear under the Season panel as if part of it. Expected panels to keep fixed slots or collapse cleanly.
- **Repro:** pt start --profile beyond-nations --seed 109; Start, Begin; N; pt walkto pawn (near Greatport, ~(-65,157)); e; click Enter Settlement (210,476); click Sell Resources
- **Screenshots:** [settlement_overlap.png](s09/shots/settlement_overlap.png)

### 4. [major] Laws tab drops the Rationing law entirely at 960x540

- **Category:** bug; **reported by** 1 agent(s): s06
- **Detail:** The Laws tab lists only Taxes, Conscription and Borders. The fourth law, Rationing (LawType.RATIONING, described in Laws.cs as one of 'the four laws'), is never drawn: LawsPanel.draw breaks out of the row loop when the next block would not fit above height - 3.2 lines, with no scrolling and no hint that more exists. There is visibly empty space below Borders. A leader at this resolution cannot see or enact Rationing. Expected: all four laws shown, or a scroll like the Nations tab has.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-wt-pt064c --seed 106; pt key space; pt click 480 315; pt click 480 315; pt key n; pt key o; pt click 543 45; pt shot laws
- **Screenshots:** [laws-no-rationing.png](s06/shots/laws-no-rationing.png)

### 5. [major] Nations list re-sorts after an act, so the two-click Denounce/War confirm can land on a different nation

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** I denounced Bigford (row 2) at tick 2093. The list then re-sorted, enemies first: Bigford moved to row 1 and Farmwood took row 2. My next two clicks at the same spot armed 'Click Denounce again to denounce Farmwood' and then confirmed it. Only Farmwood's envoy being away stopped an unintended denunciation. Arming is also allowed on a nation whose envoy is away; the envoy block only appears on the confirm click. Expected: a stable row order while the screen is open (or no re-sort under the cursor), and the envoy check on the first click.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-wt-pt064c --seed 104; Start, Begin; pt key n; wait until 3 AI nations exist (~tick 2000); pt key o; Tribute row 1 and row 3, then pt click 835 319 twice (Denounce Bigford in row 2); then pt click 835 319 twice again
- **Screenshots:** [rows_reorder_misclick.png](s04/shots/rows_reorder_misclick.png)

### 6. [major] Own market cannot trade: the resident who takes the player's new stall has 0 coins and nothing to sell

- **Category:** balance; **reported by** 1 agent(s): s05
- **Detail:** Right after I built my first stall (B) in my own camp, Market Info showed 'Merchants: Linda Smith', '1/4 stalls, 0 coins'. Sell Resources then failed with 'No merchant here can pay: the richest has 0 coins, and your cheapest good (sapling) sells for 19.' and Purchase Food failed with 'No one is selling food at this market...'. It was still the same about 3,600 ticks (~72 game s) later. A merchant has to pay 100 coins to buy a stall, but the stall/market showed 0 coins afterwards, so it is unclear where that money went. To buy and sell I had to travel 340 m to a foreign Town (Castletown), where both worked. Expected: the market I just built can trade with me straight away, or the HUD tells me how to stock it or give it money.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-wt-pt064c --seed 105; gather ~90 wood + 30 stone; N; walk away from other settlements; F; E; B; click Sell Resources (186,445); click Purchase Food (66,445)
- **Screenshots:** [sell_nocoins.png](s05/shots/sell_nocoins.png)

### 7. [major] Planks and Bricks (2 of 5 recipes) out of reach in ~20 min: Village era needs 10 members; strangers must be Loyal

- **Category:** balance; **reported by** 1 agent(s): s07
- **Detail:** The crafting screen lists 5 recipes. Axe, Pickaxe and Cooked Meat all work out in the world. Planks x2 and Bricks x2 stay 'Locked: The Workshop is unlocked in the Village era.' I founded a nation and a settlement, built a stall, a house and a farm (Buildings 2/2), and the settlement became a 'Village'. Objective 7 then sat at Members 9/10 for over 6000 ticks. Inviting a nearby stranger with R gave 'Abigail Thomas (Stranger): "I'm loyal to Eastfield." (needs Loyal)'. So the two trade-good recipes, which are the main reason to collect a surplus of wood and stone, stay out of reach for a long time, and nothing on the crafting screen shows how far off they are.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-wt-pt064c --seed 107; gather 50 wood; N; F (outside claimed land); E; B; K K (house); collect stockpile; K K (farm); C -> Planks/Bricks still locked; R on stranger -> needs Loyal
- **Screenshots:** [craft_village.png](s07/shots/craft_village.png)

### 8. [major] Sim runs at 3-30 ticks/s (often far below 50), so an autumn start cannot reach winter in a play session

- **Category:** perf; **reported by** 1 agent(s): s03
- **Detail:** With --season autumn the world starts at day 4, tick 120000, and winter begins on day 6 (tick 180000, 60000 ticks = 20 game minutes away). The measured tps ranged from 0.3 to 33 (2.9 right after entering the world, 0.3 at the final status) at 1-5 fps, with machine load around 20 on 20 cores. In 24 wall minutes the world only got from tick 120000 to 141552, the start of season day 2/2. Winter was still ~38000 ticks off, so the goal could not be finished. Under shared load, an --season autumn start is not a practical way to test winter. A start near the end of autumn, or a --season winter start for this goal, would be.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-wt-pt064c --seed 103 --season autumn; enter world; pt status every few minutes
- **Screenshots:** [day2.png](s03/shots/day2.png)

### 9. [major] Simulation ran at 5-20 tps, so no player world event was offered within a 30-minute session

- **Category:** perf; **reported by** 1 agent(s): s08
- **Detail:** Throughout the session the game ran at 5-20 ticks/s (fps 0-5) instead of 50. The first player event is scheduled 420-660 game seconds in (WorldEvents.FIRST_PLAYER_EVENT_MIN/MAX_SECONDS). After 30 wall minutes the world had reached only tick 28521 (570 game s) and no world_event_offered had appeared, so I could not see a choice sheet, take a choice or judge consequences. Even at full speed a player waits 7-11 minutes for their first event, and nothing on screen hints that events exist or are coming.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-wt-pt064c --seed 108; Start; Begin; n; gather 50 wood; f at open ground; e; idle and watch pt status / pt events until 30 min wall
- **Screenshots:** [final.png](s08/shots/final.png)

### 10. [major] Village era 'Members 10' requirement: almost no recruitable settlers left in the world

- **Category:** balance; **reported by** 1 agent(s): s02
- **Detail:** After I built the house and farm (Village 1/1, Stalls 2/2 at tick 16050), Members stayed at 3/10. The pawns near my settlement were already members ('Frank Jones is already a member of Yellowfield'). The nearest stranger after a sprint was another nation's leader ('I lead Whitefield. I won't abandon my people'), and only 3 pawns were within ~270 m. The objective says 'Befriend settlers and press R', but nothing in the game points to where unaffiliated settlers are, so the member gate became the real blocker. In addition, Interact tries the settlement first, so pawns standing in your settlement cannot be talked to with E (E enters or leaves the settlement instead).
- **Repro:** after founding and building house + farm: pt walkto pawn; pt key e; pt key r
- **Screenshots:** [talk.png](s02/shots/talk.png)

### 11. [minor] Floating settlement label is clipped at the top of the screen and overlaps chronicle and banners, even on the pause screen

- **Category:** text, ux; **reported by** 5 agent(s): s01, s03, s05, s08, s09
- **Also reported as:** "Floating settlement label is clipped at the top of the screen and overlaps the HUD"; "Settlement name banner is drawn huge behind the HUD and cut off at the top of the screen"; "Settlement's world-space label overflows the top of the screen and its name is cut off"; "The floating settlement label is drawn huge behind the HUD and cut off at the top of the screen"
- **Detail:** 'Village of Greatport / 3 here, 5/8 residents' is drawn as huge white text that runs off the top edge, overlaps the chronicle panel and status banner, and stays visible above the pause menu dimmer. White on pale sky is unreadable.
- **Repro:** walk to Greatport settlement (~(-65,157)); Escape
- **Screenshots:** [village_label_cut.png](s09/shots/village_label_cut.png) [inside2.png](s01/shots/inside2.png) [objective_clip.png](s03/shots/objective_clip.png) [home_final2.png](s05/shots/home_final2.png) [label_overlap.png](s08/shots/label_overlap.png)

### 12. [minor] Chronicle lines truncated with '...' in the default view

- **Category:** text; **reported by** 3 agent(s): s01, s06, s09
- **Also reported as:** "Chronicle lines are cut off with '...' right where the nation name is"; "Law-enactment chronicle lines are cut off with an ellipsis in the HUD chronicle"
- **Detail:** 'The nation of Greatport was born, with Una Smi...', 'Samantha Williams founded the nation of Whitet...'. The full text is only in the Y window, which is itself translucent with HUD panels drawing over it and the cryptic '4 entries. Listing 2.' header.
- **Repro:** play ~2000 ticks; read chronicle panel; press y
- **Screenshots:** [chronicle_truncated.png](s09/shots/chronicle_truncated.png) [chron_trunc.png](s01/shots/chron_trunc.png) [talk1.png](s06/shots/talk1.png)

### 13. [minor] 'The settlement is now a Village!' while the era badge still says Camp (1/4)

- **Category:** text, ux; **reported by** 2 agent(s): s03, s07
- **Also reported as:** "'The settlement is now a Village!' but crafting says the Workshop needs the Village era (era still Camp)"
- **Detail:** Building the Farm and House gave the status 'House built. The settlement is now a Village!', but the era badge stayed 'Era: Camp (1/4)' and objective 7 'Reach the Village era' stayed incomplete (Members 5/10). Two meanings of 'Village' (settlement tier and nation era) contradict each other on screen. 'Chronicle: your news' is also full of other nations' border rivalries and raids, which push the player's own events out of the five visible lines.
- **Repro:** inside own settlement: pt key k k (farm); pt key k k (house); pt shot
- **Screenshots:** [village_vs_era.png](s03/shots/village_vs_era.png) [village_vs_era.png](s07/shots/village_vs_era.png)

### 14. [minor] Mouse 'click again to confirm' for Build House never commits at low fps (8 s wall-clock window)

- **Category:** ux; **reported by** 2 agent(s): s03, s04
- **Also reported as:** "Two-click confirm: stale 'Confirm' label, mismatched wording, and a wall-clock expiry at low fps"
- **Detail:** In the settlement I clicked Build House at (473,445), and each later click just re-armed 'Click again to confirm: Build House.' (ticks 129120, 129340, 129528). Nothing was built. Each hover-plus-click took about 9 s of wall time at 2-5 fps, which is longer than ConfirmGate's 8 s window. The first Build Farm arm was also lost when the button row reflowed. Pressing K twice built both Farm and House at once. Expected a confirm window measured in frames or game time, or one that tolerates slow frames.
- **Repro:** inside own settlement with 25+ wood/15 stone: pt click 473 445; pt wait 2; pt click 473 445; pt events 3
- **Screenshots:** [confirm_lapse.png](s03/shots/confirm_lapse.png) [denounce_bigford.png](s04/shots/denounce_bigford.png)

### 15. [minor] Objective 7 progress text is clipped, and four different counters describe the same goal

- **Category:** text; **reported by** 2 agent(s): s01, s07
- **Also reported as:** "Objective 7 progress text is cut off ('mbers 8/10'), and it shows Village 0/1 while the era widget shows 0/4"
- **Detail:** The 'Reach the Village era' progress bar shows 'mbers 4/10 - Buildings 0/2' (the start is cut off). On screen at the same time: era panel 'Village: 0/4', objective body 'Village 0/1', 'Members 4/10', 'Buildings 0/2'. Later the status says 'The settlement is now a Village!' while the era panel still reads 'Era: Camp (1/4)'. The tier and the era are easy to confuse.
- **Repro:** complete the stall objective; look at the objective panel and the era panel
- **Screenshots:** [obj7_clip.png](s01/shots/obj7_clip.png) [obj7_truncated.png](s07/shots/obj7_truncated.png)

### 16. [minor] 'Talk to <pawn>' button re-targets to another pawn between hover and click

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** Inside my settlement I clicked 'Talk to Phoebe Jones (Stranger)', and Alice White answered. The contextual pawn rows follow whichever pawn is nearest, and at a few fps that changes between the hover and the click. The order and gift buttons in the same row have the same risk.
- **Repro:** inside own settlement with several residents: pt click 160 383; pt events 3
- **Screenshots:** [talk_wrong_pawn.png](s03/shots/talk_wrong_pawn.png)

### 17. [minor] 'Village' used for three different things in the Era box

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** The next era ('To reach the Village era'), the checklist counter ('Village: 0/4') and a requirement line ('Village 0/1'). 'Day 1/3' in the era list sits next to 'day 1/2' in the Season panel, which are different counters.
- **Repro:** click Era box (110,58)
- **Screenshots:** [era_detail.png](s09/shots/era_detail.png)

### 18. [minor] A resident buys the stall the player built within ~2 s, with no message and no way for the player to own it

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** I pressed B at tick 9550, and within about 2 seconds Linda Smith was listed as the stall's merchant. Nothing in the chronicle or status line said that a resident had bought my stall. The objective still completed, correctly, from my own build. Residents did not build any stalls themselves: the market was 0/4 for 20 s before I built and 1/4 for about 3,600 ticks after, so the 'leave the market to you' rule holds for construction. Having the stall taken over so fast still feels like the market is not mine. Expected: a chronicle line such as 'Linda Smith took over your stall'.
- **Repro:** same as above up to B; take a screenshot 2 s later and read Market Info
- **Screenshots:** [merchant_took_stall.png](s05/shots/merchant_took_stall.png)

### 19. [minor] A ruler cannot die naturally within a play session, so succession is hard to reach

- **Category:** balance; **reported by** 1 agent(s): s06
- **Detail:** Energy drained from 84.9 to 65.2 over ~6000 ticks (2 game minutes) while I walked, so starving takes about 30 game minutes, and the player has no other way to die (militia, fire and sickness all clamp energy at 1). At the tps on this machine that is well over the 30-minute budget, so I had to use --stage-succession to see a death. Succession is a headline feature, but a normal session will rarely see it.
- **Repro:** Play normally and watch energy in Player Info; compare with the drain rate
- **Screenshots:** [repeal1.png](s06/shots/repeal1.png)

### 20. [minor] After succession the new ruler's name appears nowhere on the HUD

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** After the rule passes to Xavier Lewis, the red status toast says so, but Player Info shows only Nation, Role and Land, and the Nations tab says 'You lead it'. Once the toast fades, nothing on screen shows the player's current name, how long they have reigned or their line. Expected: the ruler's name in Player Info, and the lineage from the succession screen reachable later, for example on the Nations tab.
- **Repro:** pt start --profile beyond-nations --game-dir /root/bn-wt-pt064c --seed 106 --stage-succession; space; Start; Begin; Continue on the succession screen; look at the HUD and press O
- **Screenshots:** [ruler-name-missing.png](s06/shots/ruler-name-missing.png)

### 21. [minor] Alliance refusal says '+30 needed', but a larger nation actually wants +60

- **Category:** text; **reported by** 1 agent(s): s04
- **Detail:** The header says 'An alliance needs +30'. At tick 4193 Southton (3 members to my 1, Friendly +28) refused with 'an alliance needs +30. Gifts help', so I paid 25 coins to reach +38. At tick 5237 (+41) it refused again: 'they are 3 to your 1 and want relations of +60 before taking on so small a partner'. The first refusal should give the real +60 threshold. It cost me an extra tribute and an extra 20 s envoy wait.
- **Repro:** As above; tribute Southton up to ~+28, click Ally (refused with +30), tribute to +38+, wait 20 s, click Ally (refused with +60)
- **Screenshots:** [ally_ok.png](s04/shots/ally_ok.png)

### 22. [minor] An NPC world event shows only as one plain chronicle line and is pushed out of the recent panel almost at once

- **Category:** ux; **reported by** 1 agent(s): s08
- **Detail:** The only world event in the session (tick 19753, 'A bountiful harvest came in across Northernwood, and Isabella Thompson filled the stores with 14 apples.') appeared as an ordinary line in the 5-line recent-chronicle panel. Raid and relation lines pushed it off within about a minute, before I took a screenshot. It had no distinct styling, toast or sound. I expected world events, which the game presents as what changes the world, to stand out from routine news.
- **Repro:** seed 108; found a settlement; wait to tick ~19753; look at the chronicle panel at ~tick 20200
- **Screenshots:** [npc_event_buried.png](s08/shots/npc_event_buried.png)

### 23. [minor] Coin gifts have no cooldown: 160 coins buys a rival nation's member as Loyal in seconds

- **Category:** balance; **reported by** 1 agent(s): s06
- **Detail:** Talking gives +2 with a 30 s cooldown, but G (gift 10 coins, +2) can be pressed repeatedly with no cooldown. Pressing G 16 times took Hank Thomas (a member of Oldwood) from Stranger to Loyal in ~10 game seconds, and R made him defect to my nation. 160 of the 375 starting coins buys a defector and an heir. Expected: a gift cooldown or diminishing returns, as with talking.
- **Repro:** (as above, with a nation) pt walkto pawn; press g 16 times; pt key r
- **Screenshots:** [gift-spam.png](s06/shots/gift-spam.png)

### 24. [minor] Collect Stockpile makes the hand-gathering objective pointless and nothing points the player at it

- **Category:** balance; **reported by** 1 agent(s): s01
- **Detail:** The 'Build a market stall' objective says gather 20 wood and 5 stone, so I hand-chopped trees and mined a rock. Back inside, one click on 'Collect Stockpile (198)' gave 128 wood + 70 stone, enough for 2 stalls, a farm and a house within seconds (the settlement became a Village at tick ~11780). No tip or objective mentions the stockpile. Expected: the objective text mentions the stockpile, or the stockpile fills more slowly early on.
- **Repro:** found settlement; wait ~2 game minutes; enter (e); click Collect Stockpile
- **Screenshots:** [collect.png](s01/shots/collect.png)

### 25. [minor] Cooked Meat is eaten automatically to top up a few energy points, with no message, while apples are in the bag

- **Category:** balance; **reported by** 1 agent(s): s07
- **Detail:** Between tick ~6000 and ~6240, energy went from 90 to 99 and cooked_meat went from 1 to 0, while I held 13 apples. No status line said I ate anything, and the second cooked meat I made also disappeared later. Neither the Controls screen nor the Inventory has an Eat action, so the 45-energy food is used up on a 9-point top-up. On top of that, the market buys cooked meat for about 85-93, which is less than the ingredients sell for separately (chicken meat ~47-49 plus wood ~55-57). Right now cooking makes the meat worth less and gives the player no say in when it is eaten. I expected the game to eat apples first, or to let me choose when to eat.
- **Repro:** craft Cooked Meat (C, row 3) with energy ~90 and apples in inventory; wait ~5 game seconds; pt state -> cooked_meat gone, energy 99, no status
- **Screenshots:** [ate_cooked_meat.png](s07/shots/ate_cooked_meat.png)

### 26. [minor] Crafting the axe moves the first objective backwards (Wood 6/10 -> 2/10)

- **Category:** ux; **reported by** 1 agent(s): s07
- **Detail:** Objective 1 'Gather wood' counts the wood you are carrying, not the wood you have gathered. Crafting an axe (4 wood) right after the first tree dropped the progress from 6/10 to 2/10, so the tutorial penalises the very tool it should encourage.
- **Repro:** chop one tree (E); C; click Axe Craft >; close; objective shows Wood 2/10
- **Screenshots:** [axe_chop.png](s07/shots/axe_chop.png)

### 27. [minor] Crafting: selected row turns bright blue and its sub-text drops to ~2:1

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** On hover/selection the recipe row goes #4296FA; the green 'Ready: you can make 1 / have 1 (20 uses left)' is ~2.4:1 and the grey effect line '+3 wood per tree, lasts 20 trees' ~2.0:1. That is the moment the player is reading before clicking.
- **Repro:** c; hover/click Axe row
- **Screenshots:** [crafting_hover_contrast.png](s09/shots/crafting_hover_contrast.png)

### 28. [minor] Diplomacy buttons look active while the envoy is away, and nothing tells you when it returns

- **Category:** ux; **reported by** 1 agent(s): s10
- **Detail:** While 'envoy back in N s' is showing, Fortresstown's Tribute/Ally/War/Denounce buttons are drawn exactly like the active ones. Clicking them only shows an 'envoy still on the road' status. When the envoy came back, no status message or chronicle entry appeared; the only sign was that the row suffix was gone (seen at tick 2770). I expected greyed-out buttons during the cooldown and a short 'your envoy has returned from X' notice.
- **Repro:** Same as above; close Nations; wait for about 20 game seconds; pt key o; pt events 15 shows no envoy-return event
- **Screenshots:** [nations_envoyback.png](s10/shots/nations_envoyback.png)

### 29. [minor] Disabled actions and HUD terms have no explanation, not even on hover

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Hovering disabled 'Plant Sapling'/'Enter Settlement'/'Found Settlement', the 'Village: 0/4' counter, or Config options shows no tooltip. Players must guess that Found Settlement needs 50 wood, that Plant needs a sapling, what Chunk Size / Location Scale / Lag Prevention / Respawn Pawns do, or what Gift/Invite/Alliance cost. Nothing is hover-only, but nothing explains these at all.
- **Repro:** pt move 425 517 on the first world screen; pt move 540 304 on Config
- **Screenshots:** [disabled_buttons.png](s09/shots/disabled_buttons.png)

### 30. [minor] Envoy wait only shows on the Nations screen: buttons stay live, HUD toast is static, no 'envoy returned' notice

- **Category:** ux; **reported by** 1 agent(s): s04
- **Detail:** While an envoy is away, all of the row's buttons look enabled; clicking one gives a clear 'still on the road' status but no visual cue beforehand. After closing O, the HUD toast keeps saying 'again in 20 seconds of play' (no countdown) and then fades. Nothing tells the player when the envoy is back, so they have to reopen O to check. The row's countdown itself is accurate: 'envoy back in 11 s' at tick 1493 for an act at 1037.
- **Repro:** Tribute any nation; close O; watch the HUD; reopen O after 20 s
- **Screenshots:** [envoy_blocked_buttons_live.png](s04/shots/envoy_blocked_buttons_live.png)

### 31. [minor] Food and cold pressure is very low: auto-eat, a 20-apple surplus by mid-autumn, no energy drain indoors

- **Category:** balance; **reported by** 1 agent(s): s03
- **Detail:** Within ~3000 ticks of chopping I had 20 apples, and 34 by tick 128600 (each tree gives 1-3 in autumn). Energy burns about 0.33 per 100 ticks outdoors and the player eats automatically (energy 90->99, apple -1). Indoors energy held flat at 91.5. The 'Eat something' objective completed by itself 52 ticks after the first one. That is roughly 13 apples per game day outdoors. With winter's 1.3x cold multiplier and free warmth indoors, a player who stays in the settlement does not need to prepare. Bandit and market pressure happen, but food and cold never felt threatening in autumn.
- **Repro:** pt start ... --season autumn; chop ~8 trees; sample pt state energy/apple every 40s outdoors and inside
- **Screenshots:** [outdoors.png](s03/shots/outdoors.png)

### 32. [minor] Game runs at 3-22 tps (fps 1-4) on a shared machine, so the day-3 era gate takes far longer than the 17 min of game time suggests

- **Category:** perf; **reported by** 1 agent(s): s02
- **Detail:** Over 22 minutes of wall clock the world advanced only 20219 ticks (6.7 game minutes). Reaching the day-3 gate (~51000 ticks) would need about 50 more wall minutes. pt walkto timed out twice because the player barely moved. The machine was loaded (load average ~43 on 20 cores), so this is partly environmental.
- **Repro:** pt start ...; pt status (tps 3-22)
- **Screenshots:** [world0.png](s02/shots/world0.png)

### 33. [minor] Gather feedback disagrees with itself: '3 apples' vs '+2 apples', saplings omitted, silent auto-eat

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** First chop: banner 'Gathered 5 wood and 3 apples from tree.', floating text '+2 apples', inventory Apples: 2, +2 saplings missing from the banner; energy rose 81->95 with no message that an apple was eaten (the 'Eat something' objective completed silently).
- **Repro:** Begin; pt walkto tree; e
- **Screenshots:** [chop_mismatch.png](s09/shots/chop_mismatch.png)

### 34. [minor] K build confirmation lapses at low fps: the second K press ~4 s later just re-prompts

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** Pressing K showed 'Press K again to build a Farm (25 wood, 10 stone).' I took a screenshot and pressed K again about 4 real seconds later (at 2-4 fps), and the game showed the same prompt again without building. Sending 'k k' together built the farm. The confirm window seems to be timed in a way that is easy to miss when the frame rate is low.
- **Repro:** inside own camp with 25 wood/10 stone: pt key k; pt wait 2; pt shot x; pt key k; pt state (status still 'Press K again...')
- **Screenshots:** [kmenu.png](s05/shots/kmenu.png)

### 35. [minor] Menus can't be used from the keyboard

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** On the pause menu, Down, Tab and Return do nothing and there is no focus indicator. Only Esc works. A keyboard-only player cannot Save, Share or go to the Main Menu.
- **Repro:** in world: Escape; pt key Down Down; pt key Tab; pt key Return -> still PAUSE
- **Screenshots:** [pause_no_kbd.png](s09/shots/pause_no_kbd.png)

### 36. [minor] NPC raid spam floods the chronicle: 76 raid lines in about 9.5 game minutes, 13 of them on one tick

- **Category:** balance; **reported by** 1 agent(s): s08
- **Detail:** On tick 17001 thirteen NationRaid chronicle entries fired at once ('Raiders from Blacktown struck Easternton...', and so on). By tick 28521 there were 76 raids, 71 relation changes and only 1 WorldEvent entry. This noise is what buries the world events and the player's own news in the chronicle.
- **Repro:** seed 108; play to tick ~17001; pt events 20 / view the chronicle
- **Screenshots:** [late.png](s08/shots/late.png)

### 37. [minor] Nations screen status line shows a stale envoy countdown that contradicts the row

- **Category:** ux; **reported by** 1 agent(s): s10
- **Detail:** After clicking Tribute twice on Fortresstown, the bottom status line says 'you can deal with them again in 20 seconds of play'. Later, when I reopened the Nations screen, the row read 'envoy back in 11 s' but the status line still said 20 seconds. I expected the status line to clear when the screen reopens, or to show the live remaining time.
- **Repro:** pt start --seed Ironvale; Start, Begin; pt key n; wait for an AI nation; pt key o; click Tribute (123,255) twice; Escape; let ~8 s of game time pass; pt key o
- **Screenshots:** [nations_presave2.png](s10/shots/nations_presave2.png)

### 38. [minor] No on-screen heading: direction words in messages ('south-west') cannot be followed

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** The HUD has no compass, no minimap and no cardinal marker. Direction words in status messages are useless without one. Seen on the first refusal of F.
- **Repro:** pt start ... --seed 101; gather 50 wood; pt key n; pt key f
- **Screenshots:** [no_compass.png](s01/shots/no_compass.png)

### 39. [minor] No screen names the heir or warns what happens if the ruler dies

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** With a second member in Smallton, neither the Nations tab, the Laws tab nor the HUD names the heir or says that dying with no members ends the line and the run. Succession.cs has clear rules (family first, then most loyal, then longest sworn), but the player only learns who the heir is on the 'The Rule Passes On' screen after death. Expected: an 'Heir: <name> (reason)' line on the Nations tab, or 'No heir: your line ends if you die'.
- **Repro:** Create nation (N), recruit one pawn (gift G, invite R), pt key o, look at both tabs
- **Screenshots:** [no-heir-shown.png](s06/shots/no-heir-shown.png)

### 40. [minor] Objective panel overlaps the settlement action buttons and its progress text is clipped on the left

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** Inside my own settlement, the Objective 7 panel is drawn over the action rows ('Gather Wood', 'Purchase Food' and others overlap). The progress bar label reads 'mbers 3/10 . Buildings 0/2' because the start of 'Members' is cut off. The Inventory panel also covers the tip box ('Info (I)' is hidden under the tip).
- **Repro:** found a settlement; pt key e (enter); pt key b; pt shot x
- **Screenshots:** [overlap.png](s02/shots/overlap.png)

### 41. [minor] R (invite) gives no feedback when used next to a stranger who is another nation's leader

- **Category:** ux; **reported by** 1 agent(s): s02
- **Detail:** Next to Phoebe Lewis (Stranger, leader of Whitefield) I talked 3 times and pressed R. No status message or event appeared at all. Elsewhere R says 'There is no one within reach to invite' or 'X is already a member'. I expected a refusal message. R is also missing from the controls list in the brief and only shows up in the objective text.
- **Repro:** walk to a pawn who leads another nation (pt walkto pawn); pt key e (x3); pt key r; pt events 5
- **Screenshots:** [r_silent.png](s02/shots/r_silent.png)

### 42. [minor] Rivalry penalties arrive right after founding, with no explanation

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** Right after I founded and entered, status messages said 'Whiteland resents your border: relations are now Rival (-20).' and then the same for Whiteburg and Whiteshire. I founded where the game allowed (250+ from others), so a new player cannot tell what they did wrong or how to avoid it. Expected: a brief reason or tip (for example 'borders within X of theirs annoy neighbours').
- **Repro:** found settlement at about (-924,-104) on seed 101; enter it; wait
- **Screenshots:** [rival_border.png](s01/shots/rival_border.png)

### 43. [minor] Season panel's 'Winter in 2 days' counts whole days and overstates the time left; three different day numbers on screen

- **Category:** text; **reported by** 1 agent(s): s03
- **Detail:** At the night of autumn season day 1 (chronicle 'Day 5, night', state day 4 timeOfDay ~0.8, tick ~135000) the Season panel still said 'Autumn - day 1/2' and 'Winter in 2 days', though winter was about 1.2 days away. It switched to 'Winter in 1 day!' only at the day rollover. SeasonCalendar.daysUntil counts whole days. The same screen also shows three calendars: chronicle 'Day 5', season panel 'day 1/2', and state.json day 4. Expected a countdown that counts down within the day (for example in hours or as a fraction) and one day number.
- **Repro:** pt start ... --season autumn; enter world; pt wait until state timeOfDay > 0.8; pt shot
- **Screenshots:** [winter_countdown.png](s03/shots/winter_countdown.png)

### 44. [minor] Sell Resources at a foreign town sold only 1 apple + 1 sapling with no quantity shown

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** At Castletown (2 merchants, 24 coins in the market) one Sell Resources click went from 437 to 457 coins, and from 37 to 36 apples and 15 to 14 saplings. The status line said only 'You sold resources to Samantha Evans.' It did not say what was sold or for how much. Wood and stone counts were 0 at the time. Expected: the status line names the items and the coins received.
- **Repro:** enter a foreign Town with merchants; click Sell Resources; compare inventory before/after
- **Screenshots:** [sold.png](s05/shots/sold.png)

### 45. [minor] Settlement panels mislead: build hint and unlabeled prices in a foreign settlement

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** Inside Greatport (not the player's nation), Settlement Info still shows 'Next (K): House, 20 wood, 15 stone'. 'Prices (buy/sell)' doesn't say whose buy/sell. The trend is shown only by red/green triangles. 'Purchase Food'/'Sell Resources' give no price before clicking. The Sell error message itself is clear and specific.
- **Repro:** enter Greatport settlement; read right panels
- **Screenshots:** [settlement_info.png](s09/shots/settlement_info.png)

### 46. [minor] Settlement placement hints use compass directions, but nothing on screen shows which way north is

- **Category:** ux; **reported by** 1 agent(s): s08
- **Detail:** 'Too close to ... Open ground lies about 180 to the south-east' and 'Found your settlement outside its borders. Open ground lies about 190 to the south-west.' There is no compass or minimap, so I found a valid spot by trial and error, and it took 4 attempts and about 5 wall minutes. I expected a compass strip, a direction arrow or a minimap marker.
- **Repro:** seed 108; gather 50 wood near spawn; press f near Easternfield/Oldwood
- **Screenshots:** [no_compass.png](s08/shots/no_compass.png)

### 47. [minor] Stale status banners leak into other screens

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** The world status text appears on unrelated full screens: 'Chronicle: recent entries.' under the Crafting list, 'Crafted 1 axe (+3 wood per tree...)' on the Nations and Laws tabs, and 'Saved: Day 1 - Littleshire, 21:02.' still on the pause menu several minutes later.
- **Repro:** press y, then c (Crafting); craft axe, then o (Nations)
- **Screenshots:** [nations_stray.png](s09/shots/nations_stray.png)

### 48. [minor] Stale tip and programmer wording

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** Tip 4 of 5 still says 'Press N to start a nation...' after the player leads Littleshire. Pressing E with nothing near gives '! No entities within range to interact with.' ('entities' is engine jargon; expected 'Nothing in reach').
- **Repro:** n; e away from anything
- **Screenshots:** [stale_tip.png](s09/shots/stale_tip.png)

### 49. [minor] Storehouse and caravans are locked until the Town era (3rd of 4), so a trade-focused player starting out cannot reach them

- **Category:** ux; **reported by** 1 agent(s): s05
- **Detail:** The 'Send Caravan (Town era)' button shows from the first camp. Clicking it says: 'This settlement needs a Storehouse before it can send caravans. The Storehouse is unlocked in the Town era.' K offers only Farm or House in a Camp. The message is clear, but caravans are a long way off. After ~13,000 ticks I was still at Camp, objective 7 (Village: Members 5/10, Buildings 1/2). The Storehouse and caravan parts of my goal could not be done in this session.
- **Repro:** after founding and entering your settlement, click 'Send Caravan (Town era)' (66,414)
- **Screenshots:** [caravan_btn.png](s05/shots/caravan_btn.png)

### 50. [minor] Tip 'Gather stone' shown while the objective is 'Eat something' / 'Found a settlement'

- **Category:** text; **reported by** 1 agent(s): s02
- **Detail:** Tip 3 of 5 'Gather stone: Find a grey rock and press E' kept showing during objectives 2-4, none of which need stone (founding needs 50 wood). I expected tips to follow the current objective.
- **Repro:** pt start ... --seed 102; chop 2 trees to finish Gather wood; pt shot
- **Screenshots:** [wood2.png](s02/shots/wood2.png)

### 51. [minor] Title, main menu, Config and Load screens: white text on light blue at ~2.5:1, buttons barely visible

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Background ~#73A6E6, button fill ~#5FA0EE with no border; white 'Press any key or click to continue' and small button labels are well below 4.5:1. Button shapes barely separate from the background.
- **Repro:** pt start; screenshot title and main menu
- **Screenshots:** [menu_buttons.png](s09/shots/menu_buttons.png)

### 52. [minor] Trees next to the settlement well cannot be chopped: E enters/exits the settlement instead

- **Category:** ux; **reported by** 1 agent(s): s03
- **Detail:** After founding, pt walkto tree went to a tree 5.5 m from the well. Pressing E then toggled 'Entered settlement'/'Exited settlement' 4 times (ticks 125788-126156), and I got no wood. Interact priority puts the settlement first, which is documented, but nothing on screen tells the player why. Expected E to act on the tree I am facing, or a hint like 'Too close to the settlement to chop'.
- **Repro:** found settlement at seed 103 ~(249,630); pt walkto tree; pt key e (x4); pt events 10
- **Screenshots:** [inside.png](s03/shots/inside.png)

### 53. [minor] Very low frame and tick rate (fps 0-5, tps 0.8-22) makes menus slow to respond

- **Category:** perf; **reported by** 1 agent(s): s07
- **Detail:** Measured with several sessions sharing the machine. Era-widget clicks never opened a visible panel, a stockpile click sometimes did nothing on the first try, and game time ran at about 1/3 speed or slower.
- **Repro:** pt status during play
- **Screenshots:** [settle2.png](s07/shots/settle2.png)

### 54. [polish] Ctrl+A does not select all in the New World seed box

- **Category:** ux; **reported by** 1 agent(s): s10
- **Detail:** Pasting the numeric seed from Share: clicking the box, pressing Ctrl+A, then BackSpace removed only one character, so typing gave 'Ironval1450162717' (seed 1889372902, a different world). The preview line ('X is seed N') caught it, which is good. I had to press BackSpace 25 times. I expected Ctrl+A to select all, or the box to select all its text when it gets focus.
- **Repro:** Main Menu > Start; click seed box (480,235); pt key ctrl+a; pt key BackSpace; pt type 1450162717
- **Screenshots:** [nw3.png](s10/shots/nw3.png)

### 55. [polish] First tree's status line says '2 apples' but the floating text and inventory show 1

- **Category:** text; **reported by** 1 agent(s): s07
- **Detail:** The status said 'Gathered 6 wood and 2 apples from tree.' The floating text said '+1 apple' and the inventory showed Apples: 1. Energy jumped from 84 to 99, so one apple was probably eaten automatically without any message.
- **Repro:** pt start ... --seed 107; walkto tree; e
- **Screenshots:** [apple_mismatch.png](s07/shots/apple_mismatch.png)

### 56. [polish] Food is eaten silently, so apple counts don't match the 'Gathered N apples' messages

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** The first tree said 'Gathered 5 wood and 2 apples from tree.', but the inventory showed Apples: 1 and the float text +1 apple. Energy jumped 83 to 97 at the same time, so an apple was probably auto-eaten without any message. 'Eat something' then completed silently 52 ticks after it appeared. Expected: a short 'You ate an apple (+energy)' status.
- **Repro:** pt start ... --seed 101; dismiss tip; pt walkto tree; pt key e
- **Screenshots:** [chop_apples.png](s01/shots/chop_apples.png)

### 57. [polish] Full-screen Inventory/Nations/Pause text has no backing panel and sits on the player model

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Inventory item lines, the pause menu's 'World seed 109' and Resume button, and the Share URL draw straight over the player capsule and world with only a dim. Inventory order also differs from the HUD panel.
- **Repro:** i; Escape
- **Screenshots:** [inventory_overlay.png](s09/shots/inventory_overlay.png)

### 58. [polish] GOAL CHECK (works): residents do not build stalls in the player's own settlement; the stall objective completes only on the player's B

- **Category:** bug; **reported by** 1 agent(s): s01
- **Detail:** From entering my settlement (tick ~6100) to tick 10467, Market Info stayed '0/4 stalls' with 4 residents and a 198 stockpile. Meanwhile the chronicle showed NPCs building many stalls in other nations' settlements. 'Build a market stall' completed only after I pressed B: status 'Stall built.' at tick 10687, objective_completed at tick 10703. Recorded here as a confirmation, not a defect.
- **Repro:** found settlement; enter; wait ~2000 ticks watching Market Info; press b
- **Screenshots:** [stall.png](s01/shots/stall.png)

### 59. [polish] Jargon: 'Export replay (USD)', Config labels, 'Era box (tap)'

- **Category:** text; **reported by** 1 agent(s): s09
- **Detail:** USD (Universal Scene Description) reads as dollars. Config's Chunk Size/Location Scale/Lag Prevention are unexplained, and the Volume label notes sound only plays in the browser version. Controls page 2 says 'Era box (tap)' (touch wording) on desktop. Controls page 3 lists F2-F6 spawn/teleport cheats as ordinary controls.
- **Repro:** Escape > Share; main menu > Config; Controls pages 2-3
- **Screenshots:** [share_usd.png](s09/shots/share_usd.png)

### 60. [polish] Load screen Delete/'Sure?' not styled as destructive

- **Category:** ux; **reported by** 1 agent(s): s09
- **Detail:** Delete and its confirm 'Sure?' are the same blue as Load, while in-world destructive actions (Leave Nation, Declare War) are red with a confirm. Nation founding (N) also auto-names the nation with no chance to choose.
- **Repro:** Main Menu > Load > Delete
- **Screenshots:** [load_delete_style.png](s09/shots/load_delete_style.png)

### 61. [polish] Market apple price swings from 54/48 to 9/9 within ~6000 ticks

- **Category:** balance; **reported by** 1 agent(s): s03
- **Detail:** My stall listed apples at 54 buy / 48 sell at tick ~129200 and 9/9 at tick ~135000. Wood went 57->16 over the same stretch. That may be the autumn double harvest, but the size of the swing makes selling look either hugely profitable or pointless, depending on when the player looks.
- **Repro:** build stall; compare Market Info prices at ticks ~129200 and ~135000
- **Screenshots:** [talk.png](s03/shots/talk.png)

### 62. [polish] Nations list footer says 'Scroll for more' when already at the end

- **Category:** text; **reported by** 1 agent(s): s04
- **Detail:** 'Nations 2-4 of 4, neighbours and enemies first. Scroll for more.' is shown at the bottom of the list. Expected: no 'Scroll for more' when nothing more is below (or 'Scroll up for more').
- **Repro:** With 4 other nations, pt key o; click Down (880,196)
- **Screenshots:** [allied.png](s04/shots/allied.png)

### 63. [polish] Nations screen always reopens on the Nations tab, so clicks meant for Laws hit Tribute

- **Category:** ux; **reported by** 1 agent(s): s06
- **Detail:** Pressing O always opens the Nations tab. At a few fps my click on the Laws tab did not register before my next click, and that click landed on Oldwood's Tribute button and spent 25 coins by mistake. Remembering the last tab would help, as would making Tribute and the other diplomacy actions need a confirm.
- **Repro:** Open the Laws tab, close it, press O again: you are on the Nations tab
- **Screenshots:** [repeal0.png](s06/shots/repeal0.png)

### 64. [polish] Tutorial tips skip numbers (1 then 3) and tip 5 vanished before it could be read

- **Category:** ux; **reported by** 1 agent(s): s01
- **Detail:** After 'Got it' on Tip 1 of 5, the next tip I saw was 'Tip 3 of 5'. state.json showed the 'Your goals' tip at tick 2095, but it was gone from the screen 2 seconds later without being dismissed, because the objective-complete popup replaced it.
- **Repro:** pt start ... --seed 101; click Got it; chop trees; press n
- **Screenshots:** [chop1b.png](s01/shots/chop1b.png)

## Objective timings

Game seconds from the start of the world to each objective (event tick / the game's ticks per second, from events.jsonl). Game time, not wall time: with many sessions on one machine a world can run slower than real time, and wall-clock timings would mostly measure the machine.

| # | Objective | s01 | s02 | s03 | s04 | s05 | s06 | s07 | s08 | s09 | s10 | min | median | max |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Gather wood | 25 | 24 | 2418 | 27.1 | 43 | - | 23 | 31.1 | 32 | - | 23 | 29.1 | 2418 |
| 2 | Eat something | 26.1 | 25 | 2419 | 28 | 44 | - | 24 | 32 | 33 | - | 24 | 30 | 2419 |
| 3 | Create or join a nation | 42.1 | 30 | 2428 | 29.1 | 71 | - | 69 | 33.1 | 48 | - | 29.1 | 45 | 2428 |
| 4 | Found a settlement | 117 | 164 | 2511 | - | 177 | - | 112 | 134 | - | - | 112 | 149 | 2511 |
| 5 | Enter your settlement | 122.1 | 166 | 2516 | - | 179 | - | 128 | 139.1 | - | - | 122.1 | 152.6 | 2516 |
| 6 | Build a market stall | 214.1 | 304 | 2577 | - | 191 | - | 134 | - | - | - | 134 | 214.1 | 2577 |

