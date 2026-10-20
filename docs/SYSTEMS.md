# Systems
The game consists of the following systems. Each is marked *implemented* (working code exists, and is linked
below) or *planned* (described here, but not yet built).

Every implemented system lives in `src/BeyondNations.Core`, the engine-agnostic simulation project. None of them
needs a window, which is why each can be exercised by a test — see the [Architecture Note](./ARCHITECTURE.md).

| System | Status | Primary code |
| --- | --- | --- |
| Event | Implemented | [`src/BeyondNations.Core/event/`](../src/BeyondNations.Core/event/) |
| World Generation | Implemented | [`src/BeyondNations.Core/world/`](../src/BeyondNations.Core/world/) |
| Inventory | Implemented | [`src/BeyondNations.Core/inventory/`](../src/BeyondNations.Core/inventory/), [`src/BeyondNations.Core/item/`](../src/BeyondNations.Core/item/) |
| Nation | Implemented | [`src/BeyondNations.Core/nation/`](../src/BeyondNations.Core/nation/) |
| Resource Collection | Implemented | [`src/BeyondNations.Core/command/interact/`](../src/BeyondNations.Core/command/interact/) |
| Food and Energy | Implemented | [`src/BeyondNations.Core/item/FoodItems.cs`](../src/BeyondNations.Core/item/FoodItems.cs) |
| Building | Implemented | [`src/BeyondNations.Core/building/`](../src/BeyondNations.Core/building/), [`src/BeyondNations.Core/command/settlement/`](../src/BeyondNations.Core/command/settlement/) |
| Pawn | Implemented | [`src/BeyondNations.Core/entity/entities/Pawn.cs`](../src/BeyondNations.Core/entity/entities/Pawn.cs), [`src/BeyondNations.Core/behavior/`](../src/BeyondNations.Core/behavior/) |
| Market | Implemented | [`src/BeyondNations.Core/market/`](../src/BeyondNations.Core/market/) |
| Crafting | Implemented | [`src/BeyondNations.Core/crafting/`](../src/BeyondNations.Core/crafting/), [`src/BeyondNations.Core/command/crafting/`](../src/BeyondNations.Core/command/crafting/) |
| Territory | Implemented | [`src/BeyondNations.Core/territory/`](../src/BeyondNations.Core/territory/) |

**Note:** This document is a work in progress. It will be updated as the project progresses and more systems are planned/implemented. "Implemented" does not mean feature-complete — see the open issues for planned expansions to each system.

## Event
The event system records notable occurrences in the game — chunk generation, nation creation/join/leave/disband, leadership changes, settlements founded, stalls built, pawn and player deaths, pawn spawns, the player falling into the void, pawn relationships improving, nations changing how they stand toward each other (alliances formed and broken, rivalries, hostility), raids, and settlements captured. Events are produced through a single producer and stored in a repository that can be queried by event type, which is what drives the event counts shown in the debug display. Other systems can subscribe to the repository, for every event or for one type; subscribers are called synchronously inside the tick that produced the event, and a subscriber that throws is logged and skipped.

### Chronicle
The first subscriber is the chronicle ([`src/BeyondNations.Core/chronicle/`](../src/BeyondNations.Core/chronicle/)), which turns events into short, dated lines of history — "Day 3, dusk - Mira founded the nation of Ironvale." — and keeps the most recent 200. Diplomacy is part of the story: alliances formed and broken, rivalries and hostility, raids, and settlements captured each get a line. Noise (chunk generation, spawns, relationship ticks) is left out. So that a populated world doesn't flood it, births, pawns joining a nation, and drift between neutral and friendly relations are told only for the player's nation, and a raid between two other nations is told once a day per raider and victim. Phrasing varies, but is chosen from a hash of the world seed and the event rather than from the world's random source, so the same seed tells the same story and keeping a chronicle never changes the world. The HUD shows the last five entries at the top of the screen, fading as they age, and `Y` cycles to a full scrollable history and then hides it; entries about the player or the player's nation are drawn in gold and marked with `*`.

## World Generation
The world generation system creates a procedurally generated world out of chunks of locations. Generated land is populated with resources, settlements, and pawns.

**Pawns.** A new chunk has a 40% chance of at least one pawn and a 15% chance of two (`WorldGenerator.rollNumberOfPawns`), so about half of all land is inhabited. It used to be a 10% chance of one, which left most opening worlds with zero to three pawns. Each pawn rolls its traits (see Pawn) from a stream of its own, so the same seed always gives the same characters. With lag prevention on, neither generation nor growth takes the world past 100 pawns (`GameConfig.getMaxNumPawns()`).

### Population growth
After generation, `PopulationGrowth` adds pawns, checking every 30 seconds:

- **Neighbours.** Pawns inside the same settlement at the same time grow fonder of each other, by 1 per check (2 for a gregarious pawn).
- **Offspring** (#133). Two residents of a settlement with a relationship of at least 10 each way have a child there when the settlement has room for another resident (#304's caps) and 3 food to spare in its stockpile and stalls, which is eaten. A settlement has at most one birth every two minutes. The child takes a parent's surname and one trait from each parent (sometimes a fresh one instead), and is born into their nation and home. Its parents start at a relationship of 10 with it.
- **Immigrants.** A thriving settlement (a Village, or two owned stalls) with room draws a nationless newcomer to its edge every three minutes or so, who joins its nation the ordinary way.
- **Settlers.** A full settlement whose stockpile holds the 50 wood for another sends a serf off with it, freeing a place at home. The settler founds a new settlement for its nation once it is at least 250 units from any other, walking away from the nearest settlement until it is. A settlement needs no stone, so a nation keeps growing after the stone nearby runs out. An **ambitious** serf is chosen first, and it leaves the nation to found a rival one of its own.
- **Wanderers.** A world with fewer than 12 pawns has a nationless wanderer turn up on generated land every minute.

Homeless members of a nation whose settlements are all full found new settlements the same way when they have the wood. Measured headless over 12 seeds, no seed had more than 3 pawns or any trade after 300,000 ticks before this; after it, seed 42 has about 11 nations by the end of its first in-game day, and every seed tried has several nations, settlements and trades by the end of its second (`TestPopulationGrowth`).

## Inventory
The inventory system manages the items and resources held by an entity. Items are collected from the world and traded at markets. When "Keep Inventory" is turned off, a player that dies — or a pawn that dies with "Respawn Pawns" on — leaves its inventory where it died as item drops, one per item type (a pawn that dies for good still takes its inventory with it); the player picks up every drop within reach by interacting (`E`). Pawns do not pick drops up. The player's holdings are summarised in a HUD box in the corner of the world screen; pressing `I` opens the inventory screen, which lists every occupied slot and pauses the world until `I` or `Escape` closes it. Crafted items join the HUD box once the player holds any, and a tool's line shows the uses left on the one in hand. Held resources are the inputs to [crafting](#crafting).

## Nation
The nation system creates and manages nations. Players can create their own nation or join an existing one, and nations track their members and their roles. A nationless pawn joins the nation of a settlement within 100 units of it (60 for an ambitious pawn) and otherwise gathers 50 wood and founds its own; that range was 500, twice the distance between settlements, so every pawn in the opening chunks joined the first nation and a world rarely had a second.

### Relations between nations
Every pair of nations has one score from -100 to +100, starting at zero (`NationRelations`, held by `NationRepository`). Named states are read off it: **Hostile** at -50 and below, **Rival** at -20 and below, **Friendly** at +20 and above, **Neutral** between, and **Allied** while an agreed alliance stands. An alliance ends if the score falls below zero. Every change of named state is recorded as a `NationRelationChange` event.

Every 250 ticks (`Diplomacy.ROUND_TICKS`, five seconds) a diplomacy round runs, with no randomness:

| Cause | Change |
| --- | --- |
| Settlements of the two nations within 450 units (neighbours) | -1 a round; -2 where the claims touch (edges within 25) |
| Allied | +1 a round |
| Each third nation both are Rival or Hostile toward | +1 a round |
| Nothing else pushing | 1 toward zero every 6 rounds |
| Founding a settlement within 450 of another nation's | -10 |
| A member trading at the other's market (the player selling or buying food) | +1 |
| Poaching a member with an invitation (#300) | -15 |
| A gift of coins | +2 per 5 coins (the player's 25-coin gift is +10) |
| Denouncing | -25 (player), -10 (pawn-led) |

**Pawn-led nations choose.** A nation led by a pawn agrees an alliance with another pawn-led nation at +40; pays 20 coins of tribute from its richest treasury to a Rival-or-worse neighbour at least twice its strength (every fourth round); and denounces, once, a weaker nation it has sunk to -40 with. A nation's strength is its members plus its allies' members. A nation led by the player makes no choices on its own.

**Conflict.** Between two Hostile neighbours of unequal strength, the stronger raids the weaker's nearest settlement every fourth round, carrying 5 coins, 5 wood and 5 stone from its treasury and stockpile to its own nearest settlement (`NationRaid` event). If the stronger has a strength of at least 4 and twice the weaker's, and its nearest settlement is of at least the target's tier, it besieges the target; the siege shows on the settlement's nametag (`Besieged by Xland (2/6)`), and kept up for 6 rounds in a row it takes the settlement (#176, `SettlementCapture` event). A siege lapses the round its conditions stop holding, so gifts, an alliance, recruiting or a building that lifts the settlement's tier can save it. Towns cannot be besieged. A captured settlement takes its new nation's colours and name; the losing nation's residents lose their home there, their stalls are given up (a player's stock comes back to them) and those inside are turned out. Its treasury and stockpile go with it. A nation left with no settlement is disbanded and its members become nationless.

**Player diplomacy.** A player who leads a nation can, from the nations screen (`O`) or from the command bar while standing in another nation's land, **gift** 25 coins (paid into that nation's first settlement's treasury), **propose an alliance** (accepted at +30) or **denounce** it (ends any alliance). The nations screen lists every other nation with its members, settlements, relation and whether it is a neighbour, and warns of any siege of the player's settlements. The answer to each action is on the status line. `--stage-diplomacy` sets up a nation between a friendly and a hostile, besieging neighbour.

*Planned:* per-nation culture and history; pawn-led nations poaching each other's members; open battle.

## Resource Collection
Players and pawns collect resources by interacting with the nearest entity in range. A tree yields wood, a rock yields stone and a chicken yields chicken meat, for both. A pawn only hunts a chicken when it needs food — energy under the threshold with nothing edible carried — and otherwise gathers from whichever of the nearest tree and nearest rock is closer, so pawns after wood or stone leave the chickens alone; a pawn with wood enough for a stall but short of stone goes for the nearest rock instead. These resources are used to build structures, to trade, to [craft](#crafting), and — for apples and chicken meat — to eat. A tree or rock gives more to a gatherer carrying the right tool: an axe adds 3 wood per tree and a pickaxe 2 stone per rock (5 becomes 7), and each gather wears the tool by one use. The status line says what the tool added and how many uses are left. A rock gives 5 stone, and a new chunk gets three to six attempts at placing one (it was one or two rocks of 3 stone, which made stone the bottleneck of every settlement). Mined rocks also come back: every 30 seconds one rock surfaces in a random generated chunk, until the world holds three per chunk, so a busy world is not mined out for good the way it used to be.

## Food and Energy
The player and every pawn carry an energy level that falls each tick by their own metabolism. The player's is a fixed configured rate at which a full bar lasts one in-game day; a pawn's varies between pawns but a full bar always lasts several minutes. A pawn inside a settlement burns energy more slowly than one outside it, and the player's energy does not fall at all while inside one. Reaching zero energy produces a death event and, subject to configuration, a respawn at the home settlement. Energy is restored by eating: below a threshold, the most nourishing edible item carried is consumed and its energy added. Which item types are edible, and how much each restores, is held in one table rather than being named at each call site, so an apple (15), chicken meat (25) and cooked meat (45) are all food and a pawn carrying either has no reason to go looking for more.

Markets read the same table. A hungry pawn in a settlement first takes food from its own stall, if it owns one holding any, and otherwise buys food when some stall it can afford holds any; only when neither is possible does it leave to gather or hunt. Buying picks whichever food gives the most energy per coin at the market's current supply-based prices, and falls back to the next food when that one cannot be afforded.

## Building
The building system creates and manages settlements. Players and pawns can found a settlement (50 wood, about ten trees), build market stalls within it (20 wood and 5 stone), and put up buildings that make it grow.

**Buildings.** Besides stalls, a settlement can hold up to 12 buildings, each paid for in wood *and* stone — the reason to quarry. Costs, limits and effects live in one table, `BuildingCatalog`:

| Building | Cost | Limit | Effect |
| --- | --- | --- | --- |
| House | 20 wood, 15 stone | 6 | Room for 4 more residents. A settlement with no houses has room for 8. |
| Storehouse | 10 wood, 30 stone | 2 | 2 more market stalls. |
| Farm | 25 wood, 10 stone | 4 | Every 500 ticks, one apple into the merchant stall holding the fewest. With no merchant the harvest is lost. |
| Workshop | 15 wood, 10 stone | 1 | Anyone inside may craft planks and bricks; merchants there turn their wood and stone into them. |

The player builds with `K` (whatever `BuildingPlanner` says the settlement needs most that they can pay for) or from the command bar, which offers one button per affordable building. Any member of the owning nation may build. The cost is taken from what the builder carries first and then from the settlement's stockpile: the wood and stone that member pawns deliver under orders, and that a serf at home hands in when it carries 20 or more and the market has no coin to buy it (*contributing*). A leader pawn builds the same way when it, with the stockpile, has the materials. In a settlement its leader does not live in (one founded by a settler), the resident serfs build stalls and buildings from the stockpile instead. `BuildingPlanner` puts a house first when the settlement is within two of its resident cap, a farm when farms lag behind houses, and a storehouse when the market is full; after those, a settlement with two or more buildings puts up its workshop.

**Residents.** A pawn calls a settlement home only if there is room: one that cannot find room in any of its nation's settlements stays homeless and keeps gathering until a house goes up or it has the wood to found another settlement. The simulation recounts residents every 50 ticks.

**A day in town.** At nightfall (time of day 0.8 to 0.2) pawns with a home walk back to it and stay in until morning unless hunger drives them out, so towns fill up in the evening. By day an idle pawn at home walks out with a 2% chance a second (4% if industrious, 1% if gregarious); a visitor, 10%. The flat 10% everywhere was tuned when pawns inside were hidden, and emptied a staged town of 5 of its 8 residents within 13 seconds.

**Tiers.** Two buildings make a Camp a Village, and five make it a Town. The tier and the resident count (`3/12 residents`) are shown on the settlement's nametag.

**What a settlement looks like.** `SettlementLayout` draws a settlement from its state, with no randomness: a sandy plaza with a well and the nation's banner at the centre; stalls in a ring around the plaza, an owned stall with a nation-coloured awning and a crate of its goods, a stall for sale with a pale awning and a yellow sign; the stockpile as a log pile and a stone heap that rise with deliveries; buildings in a wider ring, in the order they were built; and, for a town, a palisade with four gates. The settlement redraws itself only when something it shows has changed. Pawns inside a settlement are still inside for every rule, but they stay visible and stroll about the plaza (`SettlementLife`) with their nametags hidden. The player also stays visible when entering.

`--stage-settlement` (desktop and web) opens the game with such a town standing ahead of the player, for screenshots.

A town's palisade is also its defence: towns cannot be besieged (see Nation).

*Planned:* buildings that store food.

## Pawn
The pawn system creates and manages pawns. Pawns populate the world and interact with each other, the player, and the world. On each tick a behavior calculator chooses a behavior for the pawn — gathering, wandering, trading, construction, or nation actions — and a behavior executor carries it out.

### Relationships and leadership
Every pawn keeps a relationship number for the player, shown as a named tier: Stranger (below 5), Acquaintance (5+), Friend (15+) and Loyal (30+). Once the player has met a pawn, its nametag reads `Name (Tier)`, and the world command bar names the pawn in reach the same way.

- **Talking** (`E`, or *Talk to …*): the pawn answers with a line built from its situation rather than a random phrase -- how well it knows the player, then the most pressing thing on its mind: hunger, an order it is working on, whether it has a nation (and whether it would join the player's), its loyalty to a rival nation, or what it is doing for the player's own nation. The first conversation with a given pawn in any 30-second window raises the relationship by 2; talking again sooner still gets an answer but does not count.
- **Gifting** (`G`, or *Gift …*): hands over something from the player's inventory, chosen for the pawn. A hungry pawn gets one food item and values it most (+6); otherwise 10 coins (+2), then one food item (+2), then 5 wood or 5 stone (+1).
- **Inviting** (`R`, or *Invite …*): a player in a nation can recruit a nationless pawn that is at least an Acquaintance. A pawn in another nation has to be Loyal to defect (a pawn with the Loyal trait more than that, see Traits); its old nation loses the member, any stall it owned there is given up, and it starts over without a home settlement or order. A pawn that leads its nation never defects, so an invitation can never leave a nation leaderless or disband it.
- **Leading**: when the player leads the pawn's nation, two more commands appear. *Order …: Gather Wood* asks a member that is at least an Acquaintance to bring 10 wood to the nation (the nation needs a settlement); the pawn keeps living its own life, and whatever wood it carries is handed into the settlement's stockpile whenever it is inside one of its nation's settlements, until the quota is met, at which point its relationship rises by 3. The leader takes the stockpile's wood and stone with *Collect Stockpile* inside the settlement. *Appoint … Merchant* hands a serf a free stall in its home settlement, making it a merchant (+6).

### Traits
Every pawn has one trait, and 40% have a second (`PawnTraits`). Each changes what the pawn does:

| Trait | Effect |
| --- | --- |
| Industrious | Half as much again wood or stone from every tree or rock; idles in town less. |
| Greedy | Sells for a quarter more at the market, and will not sell to a stall that cannot pay that. |
| Gregarious | Warms half as fast again to the player (talking, gifts, orders), twice as fast to neighbours; lingers in town. |
| Glutton | Burns energy half as fast again. |
| Ambitious | Joins a nearby nation only from within 60 units, so it usually founds its own; the first to leave a crowded settlement, and leaves its nation to do it. |
| Loyal | Needs a relationship of 50, not 30 (Loyal tier), before it will defect to the player's nation. |

Traits show on the nametag (`Name (Tier)`, then `Industrious, Greedy`, then what it is doing) and on the command bar's *Talk to Ada (Friend) - Industrious, Greedy*. In conversation a pawn ends with a word about itself by its first trait ("Is it supper time yet?"), and an ambitious or loyal pawn answers questions of allegiance in character ("I'd do almost anything for you, but I won't leave Rivalia.").

*Planned:* pawns interacting with each other beyond trade and neighbourliness.

## Market
The market system is used to buy and sell resources within a settlement. Each settlement has a market with a limited number of stalls, which can be built or purchased by pawns and by the player. Stall owners stock their stalls with items and collect the profit from sales. Item prices are calculated from the item's base cost and the market's current supply of that item, rather than being set per stall: base / (1 + supply / 10), so ten units on offer halve the price, never falling below a quarter of the base cost. A pawn heads to market once it carries 20 or more goods of any kind and the trip has a purpose for its role.

Crafted goods have prices of their own (base cost: axe 360, pickaxe 330, planks 120, bricks 110, cooked meat 90), set so that tools and trade goods sell for more than their inputs. *Sell Resources* sells everything but coin and tools, so a player never sells their axe by accident. A merchant stocking its stall first crafts from its haul: an axe and a pickaxe whenever the stall has none of that tool, cooked meat from up to two chicken meat, and, if the settlement has a workshop, up to two batches each of planks and bricks. The player can buy a stocked tool from the command bar (*Buy Axe (price)*) when they have none and can pay.

## Crafting
Crafting turns resources into tools, better food and trade goods. The recipes are data in `RecipeBook`; `Crafting` checks and performs a craft for the player, the crafting screen and merchant pawns alike.

| Recipe | Inputs | Needs | Makes | Why |
| --- | --- | --- | --- | --- |
| Axe | 4 wood, 2 stone | — | 1 axe | +3 wood per tree; wears out after 20 trees |
| Pickaxe | 3 wood, 3 stone | — | 1 pickaxe | +2 stone per rock; wears out after 15 rocks |
| Cooked Meat | 1 chicken meat, 1 wood | — | 1 cooked meat | 45 energy instead of raw meat's 25 |
| Planks | 3 wood | Workshop | 2 planks | Sell for 240 against the wood's 180 |
| Bricks | 3 stone, 1 wood | Workshop | 2 bricks | Sell for 220 against the inputs' 150 |

A recipe that needs a building can be made only inside a settlement that has one; any settlement's workshop will do. A tool is an ordinary item; the inventory remembers the uses spent on the one in hand, and when it wears out one is removed and a spare, if any, starts fresh — so wood and stone stay in demand for replacements. Pawns that hold a tool get the same bonus when they gather.

Pressing `C` (or *Craft* on a touch screen) opens the crafting screen over the paused world: one row per recipe with its cost, what it is good for, and either how many can be made right now or exactly what is missing ("Need 2 more stone", "Need a Workshop in this settlement"). Clicking or tapping a ready row crafts one batch, and the result is shown on the screen. `C` or `Escape` closes it. The command bar also offers *Craft Axe* and *Craft Pickaxe* outright when the player has none and can make one.

*Planned:* crafted goods as building materials, and more recipes as new resources arrive.

## Territory
Every settlement claims a circle of land around it whose radius grows with its tier: 70 units for a camp, 100 for a village, 130 for a town (`Territory`). Claims never overlap: where two would, each is cut back in proportion to its tier's radius so the two share the gap and the borders touch. Claims are redrawn every diplomacy round. Distances are measured across the ground, ignoring height.

- **Borders are visible.** `SettlementLayout` rings each claim with short posts capped in the nation's colour, about one every 10 units, so two nations' claims meet as two colours of post.
- **Founding is refused inside another nation's claim**, by the player (`This land is claimed by Xland.`) and by leader pawns alike; founding within 450 units of another nation's settlement costs relations with it.
- **The player knows where they are.** The player box has a `Land:` line (`wilds`, `your own land`, or `Xland (Rival)`), and crossing a border puts it on the status line.
- Touching borders are friction between nations, and a siege takes a whole settlement and its claim with it (see Nation).

## Sound, feedback and tips (host)
Not a simulation system: it lives in the host, in [`src/BeyondNations.Desktop/ui/feel/`](../src/BeyondNations.Desktop/ui/feel/), shared by the desktop and the browser. It reads the world and never writes to it, draws nothing from the simulation's `RandomSource`, and runs on real frame time, so the same seed steps identically with it on, off or missing (`TestFeel.testFeelLayerDoesNotChangeTheSimulation`, and the History replay job).

- **What happened.** `FeelObserver` compares the world with the previous frame: the player's inventory (wood next to a tree that has just gone is a chop, stone by a rock is mining, meat by a chicken is a hunt, a crafted item appearing as inputs disappear is a craft, coins either way), the energy tier falling to Hungry or Starving, the objective count, and each settlement's building count. Raids on the player's nation, new settlements and new stalls arrive as events. The first frame after a world is built or loaded only records, so a loaded save is not mistaken for a haul.
- **Sound.** `FeelDirector` turns those into `SoundCue`s for an `ISoundService`: chop, mine, pickup, craft, build complete, coin, hunger warning, combat hit (hunting a chicken, a raid on your nation), UI click (any button, any screen change), objective complete, and an ambient loop that follows the time of day (day: wind and birds; night: a lower wind and crickets). A building going up elsewhere is quieter with distance and silent past 90 units. The browser's service is `WebAudioSoundService`, which synthesises every cue with WebAudio in `wwwroot/audio.js` (no audio files) and starts only after the first key, click or tap. The desktop uses `NullSoundService`: Strata has no audio module and the desktop takes on no audio dependency, so it is silent.
- **Juice.** `JuiceEffects` adds presentation-only items to the frame's `WorldSnapshot` after it is captured (`addEffectItem`, `setEntityItem`): wood chips, leaves, stone flakes, feathers and sparks as small falling cubes; a new building rises out of its plot over 1.6 seconds inside a four-post scaffold; floating "+5 stone" text (ImGui, under the HUD panels); a gold screen flash when an objective completes; and a short camera shake on chops, mining, hunting and raids.
- **The first five minutes.** `OnboardingHints` shows one tip at a time in a new world (never a loaded one): walk and look, chop a tree, gather stone, found a settlement (create a nation first), and where the goals panel is. A tip goes as soon as its goal is met, or on *Got it*; *No tips* turns them off. Finished tips are recorded in `FeelSettings` and never shown again. On a touch-only device the wording names the on-screen pad and *Use* button. The tips are separate from the objectives, which they only read.
- **Settings.** `FeelSettings` (volume, mute, tips on or off, tips finished) belong to the player, not a world, so they are kept beside the saves rather than in them: `feel-settings.txt` under the user data directory on the desktop, the `beyond-nations/feel-settings` key in the browser's localStorage. They are plain `key=value` lines read leniently. The config screen has *Volume*, *Mute Sound* and *Show Tips*; the pause screen has *Sound: On/Off* where there is sound.

