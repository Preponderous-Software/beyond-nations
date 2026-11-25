# Playtest summary: 0.9.0

5 sessions, build beyond-nations 0.8.1-SNAPSHOT (2167959c (develop) + uncommitted lanes @ /root/wt/bn-full), beyond-nations 0.8.1-SNAPSHOT (2167959c (develop) + uncommitted lanes @ /root/wt/bn-integration), generated 2026-11-24 17:11.

**Findings:** 27 reports in 25 groups -- 0 blocker, 1 major, 11 minor, 15 polish.

## Outcomes

| Session | Goal | Outcome | Fun | Minutes | Objectives | Deaths | Findings | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| f1 | bn-full: new game, seed 103, normal start, about 5 minutes: contextual HUD, unlock cards, Next line, Journal, overlays. | achieved | 4 | 7 | 0 | 0 | 5 | The first bar starts empty and unlocks in a sensible order. The Next line is clear and the picture Journal reads well (Goals tile detail, Era and Nation pages). The 'one overlay at a time' promise does not hold: toasts stack with cards and with each other, and the old 5-tip track still runs beside the new Next line. |
| w1 | bn-integration: normal-start opening, seed 103: gather, nation, found, recruit, order a Stranger member, Pay with Coins. | achieved | 3 | 7 | 0 | 0 | 7 | The opening works end to end. The open-ground hint gets you to a valid site, ordering a Stranger member works, and Pay with Coins toggles cleanly. The rough edges are price wording and buttons that move or vanish at low fps. |
| w2 | bn-integration: winter from autumn (--season autumn --advance-ticks 25000), seed 103. Stopped at winter start when the coordinator switched trees. | partial | 3 | 20 | 0 | 0 | 3 | The 'no members yet' line, the world-event sheet's long-wait note and the em-dash fix all show correctly. Winter itself was measured on bn-full (w2f) instead. |
| w2f | bn-full: survive the first winter as a prepared ruler, seed 103, --season autumn --advance-ticks 25000, 960x540. Stock members to 14 with Feed Residents, then watch unaided. | partial | 3 | 45 | 0 | 0 | 7 | A nation stocked to 14 food a member gets through winter. It held 56 of 56 at winter's start, and unaided from tick 183400 it burned about 0.00024 food a member a tick (0.0004 at c392340), heading for about 3 a member left at the thaw, with 0 deaths over 50,300 winter ticks (winter day 2, about 70% of the way). I stopped about 10,000 ticks before spring for the deadline. Balance looks right. What misleads is the presentation: a 'too little food' warning from midday of day 1 on a nation that is on track, and HUD boxes that cut off that warning. |
| w3 | bn-full at 390x844 (phone-sized desktop window): settlement screen, tip card vs settlement boxes, Journal, controls list, nations screen, world-event sheet. Touch mode is browser-only (TouchLayout.isAvailable false on desktop), so the touch More sheet could not be tested. | partial | 3 | 10 | 0 | 0 | 5 | Most screens fit a 390 px window well: the settlement screen scrolls, the controls list, the Journal and the world-event sheet all fit. The one real problem is the in-settlement HUD, where the two info boxes bury the tip card and the Next line. The touch-only command bar and More sheet need the browser build to test. |

## Findings

### 1. [major] Inside a settlement on a phone, the info boxes cover the tip card, the toast and the Next line

- **Category:** ux; **reported by** 1 agent(s): w3
- **Detail:** After Stay Inside at 390x844, the tall narrow Market Info box (left) and Settlement Info box (right) draw over the Next line, the 'You are inside the settlement' toast and the 'Walk and look' tip card. The card's Got it button is hidden; only No tips can be reached. So the answer to the coordinator's question is yes, in reverse: the settlement boxes cover the tip card.
- **Screenshots:** [w3_overlap.png](w3/shots/w3_overlap.png)

### 2. [minor] Toasts and cards show together and overlap HUD boxes

- **Category:** ux; **reported by** 2 agent(s): f1, w2f
- **Also reported as:** "Toasts stack and cover the left HUD boxes"
- **Detail:** After founding, the status toast covers the 'Carrying ...' box (cut at 'saplings') while a News card covers the settlement nameplate. At the start, the Welcome toast and the Tip card show together.
- **Screenshots:** [f1_overlap.png](f1/shots/f1_overlap.png) [w2f_pileup.png](w2f/shots/w2f_pileup.png)

### 3. [minor] A --season autumn world starts with an empty command bar

- **Category:** ux; **reported by** 1 agent(s): w2f
- **Detail:** With '--season autumn --advance-ticks 25000', the HUD treats the world as a new game: no Create/Join Nation buttons until N is pressed, and no Talk button for a pawn within reach. If staged worlds are meant to get everything, this flag is not counted as staged.
- **Screenshots:** [w2f_bar.png](w2f/shots/w2f_bar.png)

### 4. [minor] Famine warning fires on a nation that is on track

- **Category:** ux; **reported by** 1 agent(s): w2f
- **Detail:** The target stays at the whole-winter 56 (14 x 4) all winter. As stores are eaten normally, 'Food: 52 of 56 for winter - Too... !' (too little food) appears by winter day 1 midday, although the burn rate leaves about 10 food at winter's end. The warning should compare against what is left of winter, or say 'on track'.
- **Screenshots:** [wn2.png](w2f/shots/wn2.png)

### 5. [minor] HUD boxes truncate the key winter text

- **Category:** ux; **reported by** 1 agent(s): w2f
- **Detail:** 'Food: 52 of 56 for winter - Too... !' and 'Food: 56 of 56 for winter, 22 rot...' are cut with an ellipsis at 960 px. The famine line, the most important part, is the part lost.
- **Screenshots:** [wn_trunc.png](w2f/shots/wn_trunc.png)

### 6. [minor] Old tutorial tip contradicts the Next line

- **Category:** ux; **reported by** 1 agent(s): f1
- **Detail:** 'Tip 3 of 5: Gather stone' shows while Next says 'Create or join a nation'. Later 'Tip 4 of 5: Found a settlement' says 'Press N to start a nation' after the nation already exists. The 5-step tip track and the Next line/unlock cards are two tutorials at once.
- **Screenshots:** [f1_tip_vs_next.png](f1/shots/f1_tip_vs_next.png)

### 7. [minor] Order button vanishes while the pawn walks away

- **Category:** ux; **reported by** 1 agent(s): w1
- **Detail:** Ordering a Stranger member works: Dave Davies (Stranger) says 'I hardly know you, but you lead Northernfield. I'll gather 10 wood.' But members walk out of reach between the approach and the click (at 5 fps), the Order row disappears and the click lands on nothing. It took 3 tries over two pawns.
- **Screenshots:** [order3.png](w1/shots/order3.png)

### 8. [minor] Pay with Coins is out of reach in the opening

- **Category:** balance; **reported by** 1 agent(s): w1
- **Detail:** At the first visit the player has 211 coins; Buy House is 1,584 and Buy Farm 1,764. Both buttons stay greyed ('Needs 1,584 coins; you have 211.'). Fine as a late-game option, but it does nothing for a new player.
- **Screenshots:** [paycoins_hover.png](w1/shots/paycoins_hover.png)

### 9. [minor] Season box: 'N rotting' sits oddly beside the food total

- **Category:** ux; **reported by** 1 agent(s): w2
- **Detail:** Last autumn night: 'Food: 17 of 14 for winter, 5 rotting' (1 member). 5 rotting is more than the 3 above target, and nothing says what rots or where.
- **Screenshots:** [w2_home.png](w2/shots/w2_home.png)

### 10. [minor] Settlement screen shows two coin prices for the same farm

- **Category:** ux; **reported by** 1 agent(s): w1
- **Detail:** Buildings line: 'Next: Farm, 25 wood, 10 stone, or 2,160 coins'. With Pay with Coins on, the button reads 'Buy Farm (1,764)'. The code means it (full price vs. the price of the shortfall after carried wood/stone), but nothing on screen says why they differ.
- **Screenshots:** [paycoins_price.png](w1/shots/paycoins_price.png)

### 11. [minor] Staged world still runs the new-game Next line and tips

- **Category:** ux; **reported by** 1 agent(s): w3
- **Detail:** In a --stage-settlement world (leader of a 9-resident Town), Next says 'Gather wood - Wood 0/10', Tip 1 of 5 shows, and the Journal shows 0/9 goals with Nation/Settle locked.
- **Screenshots:** [w3_world.png](w3/shots/w3_world.png)

### 12. [polish] Feed Residents label is truncated on the settlement screen

- **Category:** text, ux; **reported by** 2 agent(s): w2f, w3
- **Also reported as:** "Feed Residents label truncated on the phone settlement screen"
- **Detail:** The settlement screen shows 'Feed Residents (1 short fo...' while the in-world bar shows '(1 short for winter)' in full.
- **Screenshots:** [w2f_settle3.png](w2f/shots/w2f_settle3.png) [w3_settle.png](w3/shots/w3_settle.png)

### 13. [polish] 'N rotting' shows beside an exact '56 of 56'

- **Category:** ux; **reported by** 1 agent(s): w2f
- **Detail:** 'Food: 56 of 56 for winter, 22 rot...' does not tell the player whether 22 food is about to be lost.
- **Screenshots:** [wf3.png](w2f/shots/wf3.png)

### 14. [polish] Build Stall keeps its 'Build' label in coin mode, with no tooltip

- **Category:** ux; **reported by** 1 agent(s): w1
- **Detail:** With Pay with Coins on, House and Farm become 'Buy X (price)' but Stall stays 'Build Stall', greyed, with no hover reason.
- **Screenshots:** [paycoins_stall.png](w1/shots/paycoins_stall.png)

### 15. [polish] Feed Residents tooltip runs off the right edge

- **Category:** ux; **reported by** 1 agent(s): w2f
- **Detail:** 'Gives your food to the members standing here: ... before they go h' is clipped at 960 px.
- **Screenshots:** [wn2.png](w2f/shots/wn2.png)

### 16. [polish] Hover tooltip on the greyed Found Settlement drops the open-ground hint

- **Category:** ux; **reported by** 1 agent(s): w1
- **Detail:** Tapping the button gives the full status: '...Open ground lies about 200 paces to the south-east.' The hover tooltip stops at '...must be 250 paces apart.' and lies across the Leave Nation button and the Player Info box.
- **Screenshots:** [found_grey2.png](w1/shots/found_grey2.png)

### 17. [polish] Inventory box jumps to mid-screen when the command bar grows

- **Category:** ux; **reported by** 1 agent(s): w1
- **Detail:** With 3-4 command rows near a pawn, Inventory Info moves to x~220,y~230 and floats over the world. Inside a settlement it covers the Chronicle lines and the settlement nameplate.
- **Screenshots:** [near_pawn3.png](w1/shots/near_pawn3.png)

### 18. [polish] Journal footer clipped at phone height

- **Category:** text; **reported by** 1 agent(s): w3
- **Detail:** '(J or Esc closes; arrows or Tab turn the page; click a tile for more)' wraps and its second line is cut off at the bottom edge.
- **Screenshots:** [w3_journal.png](w3/shots/w3_journal.png)

### 19. [polish] Journal goal track markers are misaligned

- **Category:** ux; **reported by** 1 agent(s): f1
- **Detail:** On the Goals page, the second completed marker sits below the progress line, out of line with the others.
- **Screenshots:** [f1_journal.png](f1/shots/f1_journal.png)

### 20. [polish] Nations screen has no backing panel on a phone

- **Category:** ux; **reported by** 1 agent(s): w3
- **Detail:** At 390 px the text and buttons draw straight over the 3D world (a pawn shows through between Ally and War), which hurts readability. There is also a gap in the Tribute/Ally/_/War/Denounce row.
- **Screenshots:** [w3_nations.png](w3/shots/w3_nations.png)

### 21. [polish] Nations unlock card arrives before the Next line asks for it

- **Category:** ux; **reported by** 1 agent(s): f1
- **Detail:** With Next still 'Gather wood - Wood 5/10', the 'Nations: start a nation of your own, or join one' card and the Create/Join buttons appear. That is sensible, but it pulls a step ahead of the Next line.
- **Screenshots:** [f1_chop1.png](f1/shots/f1_chop1.png)

### 22. [polish] Objective progress lags inventory

- **Category:** ux; **reported by** 1 agent(s): w2
- **Detail:** The objective showed 'Wood 44/50' while the player carried 50 (state.json); it caught up a few seconds later. The same lag shows on bn-full.
- **Screenshots:** [w2_stuck.png](w2/shots/w2_stuck.png)

### 23. [polish] Own members still read '(Stranger)' on Talk

- **Category:** text; **reported by** 1 agent(s): w1
- **Detail:** Oscar Robinson, a member who had just sworn fealty, shows 'Talk to Oscar Robinson (Stranger)'. The new Order flavour text makes this deliberate, but it still looks contradictory next to 'swore fealty'.
- **Screenshots:** [near_pawn.png](w1/shots/near_pawn.png)

### 24. [polish] Purchase Food / Sell Resources look pressable in a stall-less camp

- **Category:** ux; **reported by** 1 agent(s): w2
- **Detail:** Inside a camp with 0 stalls, the command bar shows Purchase Food and Sell Resources in the enabled style.
- **Screenshots:** [w2_inside.png](w2/shots/w2_inside.png)

### 25. [polish] Top-strip meters are low-contrast when idle

- **Category:** ux; **reported by** 1 agent(s): f1
- **Detail:** The energy, apple and coin meters render grey-on-grey (e.g. '97' and '0') until they change.
- **Screenshots:** [f1_chop2.png](f1/shots/f1_chop2.png)

## Objective timings

Game seconds from the start of the world to each objective (event tick / the game's ticks per second, from events.jsonl). Game time, not wall time: with many sessions on one machine a world can run slower than real time, and wall-clock timings would mostly measure the machine.

No objective was completed (or the game has no probe).

