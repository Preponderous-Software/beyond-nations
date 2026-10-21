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
| Seasons | Implemented | [`src/BeyondNations.Core/season/`](../src/BeyondNations.Core/season/) |
| Regions | Implemented | [`src/BeyondNations.Core/region/`](../src/BeyondNations.Core/region/) |
| Rule (laws, war and peace, succession) | Implemented | [`src/BeyondNations.Core/realm/`](../src/BeyondNations.Core/realm/), [`src/BeyondNations.Core/command/nation/RealmCommands.cs`](../src/BeyondNations.Core/command/nation/RealmCommands.cs) |
| Sharing | Implemented | [`src/BeyondNations.Core/sharing/`](../src/BeyondNations.Core/sharing/), [SHARING.md](./SHARING.md) |
| World Events | Implemented | [`src/BeyondNations.Core/worldevents/`](../src/BeyondNations.Core/worldevents/) |
| Trade | Implemented | [`src/BeyondNations.Core/trade/`](../src/BeyondNations.Core/trade/), [`src/BeyondNations.Core/market/MarketPricing.cs`](../src/BeyondNations.Core/market/MarketPricing.cs) |
| Eras and the end of a run | Implemented | [`src/BeyondNations.Core/era/`](../src/BeyondNations.Core/era/) |

**Note:** This document is a work in progress. It will be updated as the project progresses and more systems are planned/implemented. "Implemented" does not mean feature-complete — see the open issues for planned expansions to each system.

## Sharing
A world is its seed. The New World screen takes a number or any word (hashed to a seed, `WorldSeed`), and the pause screen's Share opens the world's link (`?seed=` on the browser build), an export of the run as a `usd-replay/1` replay (`RunRecorder`, `ReplayUsdaWriter`) and an export of the chronicle as a Markdown history (`ChronicleExport`). Recording reads the world and never draws from its random source, so it changes nothing. See [SHARING.md](./SHARING.md).

## Event
The event system records notable occurrences in the game — chunk generation, nation creation/join/leave/disband, leadership changes, settlements founded, stalls built, pawn and player deaths, pawn spawns, the player falling into the void, pawn relationships improving, nations changing how they stand toward each other (alliances formed and broken, rivalries, hostility), raids, settlements captured, and trade caravans sent, sold, escorted and robbed. Events are produced through a single producer and stored in a repository that can be queried by event type, which is what drives the event counts shown in the debug display. Other systems can subscribe to the repository, for every event or for one type; subscribers are called synchronously inside the tick that produced the event, and a subscriber that throws is logged and skipped.

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
| A caravan selling at the other's market (see Trade) | +2 |
| The player escorting the other's caravan | +8 |
| The player robbing the other's caravan | -30 |

**Pawn-led nations choose.** A nation led by a pawn agrees an alliance with another pawn-led nation at +40; pays 20 coins of tribute from its richest treasury to a Rival-or-worse neighbour at least twice its strength (every fourth round); and denounces, once, a weaker nation it has sunk to -40 with. A nation's strength is its members plus its allies' members. A nation led by the player makes no choices on its own.

**Conflict.** Between two Hostile neighbours of unequal strength, the stronger raids the weaker's nearest settlement every fourth round, carrying 5 coins, 5 wood and 5 stone from its treasury and stockpile to its own nearest settlement (`NationRaid` event). If the stronger has a strength of at least 4 and twice the weaker's, and its nearest settlement is of at least the target's tier, it besieges the target; the siege shows on the settlement's nametag (`Besieged by Xland (2/6)`), and kept up for 6 rounds in a row it takes the settlement (#176, `SettlementCapture` event). A siege lapses the round its conditions stop holding, so gifts, an alliance, recruiting or a building that lifts the settlement's tier can save it. Towns cannot be besieged. A captured settlement takes its new nation's colours and name; the losing nation's residents lose their home there, their stalls are given up (a player's stock comes back to them) and those inside are turned out. Its treasury and stockpile go with it. A nation left with no settlement is disbanded and its members become nationless.

**Player diplomacy.** A player who leads a nation can, from the nations screen (`O`) or from the command bar while standing in another nation's land, send **tribute** of 25 coins (paid into that nation's first settlement's treasury), **propose an alliance** or **denounce** it (ends any alliance); from the nations screen they can also **declare war** or, once at odds, **offer peace** (see [Rule](#rule)). The nations screen lists every other nation with its members, settlements, relation, any war or truce, and whether it is a neighbour, and warns of any siege of the player's settlements. The answer to each action, with its reason, is on the status line. `--stage-diplomacy` sets up a nation between a friendly and a hostile, besieging neighbour.

*Planned:* per-nation culture and history; pawn-led nations poaching each other's members; open battle.

## Resource Collection
Players and pawns collect resources by interacting with the nearest entity in range. A tree yields wood, a rock yields stone and a chicken yields chicken meat, for both. A pawn only hunts a chicken when it needs food — energy under the threshold with nothing edible carried — and otherwise gathers from whichever of the nearest tree and nearest rock is closer, so pawns after wood or stone leave the chickens alone; a pawn with wood enough for a stall but short of stone goes for the nearest rock instead. These resources are used to build structures, to trade, to [craft](#crafting), and — for apples and chicken meat — to eat. A tree or rock gives more to a gatherer carrying the right tool: an axe adds 3 wood per tree and a pickaxe 2 stone per rock (5 becomes 7), and each gather wears the tool by one use. The status line says what the tool added and how many uses are left. A rock gives 5 stone, and a new chunk gets three to six attempts at placing one (it was one or two rocks of 3 stone, which made stone the bottleneck of every settlement), scaled by its [region](#regions) but never fewer than two. Mined rocks also come back: every 30 seconds one rock surfaces in a random generated chunk (two in the highlands), until the world holds three per chunk, so a busy world is not mined out for good the way it used to be.

## Food and Energy
The player and every pawn carry an energy level that falls each tick by their own metabolism. The player's is a fixed configured rate at which a full bar lasts one in-game day, and a new world starts the player at 85, just below the line under which they eat (90), so the first food they pick up is eaten at once; a pawn's varies between pawns but a full bar always lasts several minutes. A pawn inside a settlement burns energy more slowly than one outside it, and the player's energy does not fall at all while inside one. Reaching zero energy produces a death event and, subject to configuration, a respawn at the home settlement. Energy is restored by eating: below a threshold, the most nourishing edible item carried is consumed and its energy added. Which item types are edible, and how much each restores, is held in one table rather than being named at each call site, so an apple (15), chicken meat (25) and cooked meat (45) are all food and a pawn carrying either has no reason to go looking for more.

Markets read the same table. A hungry pawn in a settlement first takes food from its own stall, if it owns one holding any, and otherwise buys food when some stall it can afford holds any; only when neither is possible does it leave to gather or hunt. Buying picks whichever food gives the most energy per coin at the market's current supply-based prices, and falls back to the next food when that one cannot be afforded.

## Building
The building system creates and manages settlements. Players and pawns can found a settlement (50 wood, about ten trees), build market stalls within it (20 wood and 5 stone), and put up buildings that make it grow.

**Buildings.** Besides stalls, a settlement can hold up to 12 buildings, each paid for in wood *and* stone — the reason to quarry. Costs, limits and effects live in one table, `BuildingCatalog`:

| Building | Cost | Limit | Effect |
| --- | --- | --- | --- |
| House | 20 wood, 15 stone | 6 | Room for 4 more residents. A settlement with no houses has room for 8. |
| Storehouse | 10 wood, 30 stone | 2 | 2 more market stalls. |
| Farm | 25 wood, 10 stone | 4 | Every 500 ticks, one apple into the merchant stall holding the fewest (two in autumn, none in winter, one more on the plains; see [Seasons](#seasons)). With no merchant the harvest is lost. |
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

**Living prices** (`MarketPricing`, `MarketPrices`). On top of the supply curve, each market leans every price by a pressure, 50% to 200%, that drifts 5 points every pricing round (500 ticks) toward `100 * (demand + 20) / (stock + 20)`. Stock is what the stalls and the settlement's stockpile hold; demand grows with the residents: 3 + 2 per resident for food (all foods count together), 10 + 2 per resident for wood, 6 + 1 for stone, 2 + one per two residents for planks and bricks, 1 + one per four for tools. So a glutted market pays less, a scarce good pays more, and goods carried from one to the other sell for more than they cost. The market box lists each good's buying and selling price with an arrow for its movement since the last round.

**No coins from nowhere.** A unit is bought at the price at the current stock and sold at the price once it has landed (stock + 1) -- the next unit's buying price -- and buying several at once costs each unit at the stock it leaves. Buying and selling straight back therefore never comes out ahead, at any stock or pressure (`TestMarketPricing`); it used to pay the difference between the two stock levels, a slow drain on any merchant's purse. Pinned numbers changed with it: a sale into an empty market now fetches `calculateCost(base, 1)` (planks 109, not 120).

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

## Rule
Being a ruler (`Realm`, in [`src/BeyondNations.Core/realm/`](../src/BeyondNations.Core/realm/)): the laws a nation lives under, the wars and truces its ruler declares and agrees, and the player's line of rulers. Nothing here draws on randomness, and a world that never touches it -- no laws set, no war declared, nobody dying in a nation -- takes exactly the course it always did.

### Laws
The nations screen's **Laws** tab (the court) lets the leader set four laws (`Laws`, `NationLaws`). A change is proclaimed and comes in `Realm.LAW_DELAY_TICKS` (1,500, thirty seconds) later, on the status line and in the chronicle ("Taxes in Townfield rose to High."). Choosing the setting already in force withdraws a pending change. Every 500 ticks (ten seconds) a law round runs, and each law in force does its work:

| Law | Effect | Cost |
| --- | --- | --- |
| Taxes: Low / High | Each member pawn pays 1 / 3 coins into its home settlement's treasury (or the nation's first) | A point of loyalty every 3rd round / every round; a settlement waits twice as long for an immigrant / gets none |
| Conscription | One conscript per two member pawns, rounded up, adds to the nation's **defence**: a hostile nation raids only if its strength is above the defence, and besieges only at twice the defence | Every gather of wood or stone by a member pawn loses 40% (rounded down) to drill |
| Open borders | Immigrants come twice as often; 2 coins of tolls per settlement per round; every 4th round, +1 relations (trade) with every nation not at war with it | Raids on it carry off double; every 3rd round thieves take up to 2 coins and 2 wood from each settlement |
| Rationing | Each member pawn gets back a quarter of a round's hunger | A point of loyalty every 2nd round |

Loyalty is a member's relationship with the player (see Pawn). A member whose loyalty falls to -15 (`Realm.DESERT_AT`) deserts: it gives up its stall, home and order, leaves the nation, and the chronicle says so. Laws only bite while the player leads the nation. The panel shows each law's setting and any change coming in, the nation's defence, the last tax collection and the deserters so far.

### War and peace
From the nations screen a leader can also:

- **Declare war**: any alliance breaks, relations fall to at least -60 (Hostile) and cannot warm while the war lasts, and the enemy's allies think 15 less of the player. Raids and sieges follow wherever the two are neighbours (see Nation). The enemy's answer names the odds: it laughs if it fields twice the player's strength, falls back behind its walls if the player has twice its defence, and otherwise takes up arms.
- **Offer peace** (instead of War, once at war or Rival or worse): refused at -90 or worse ("too much blood"), or by an enemy at least twice the player's defence that thinks it is winning, unless a tribute has bought a truce; accepted, the war ends, relations come up to -19 (Neutral) and a **truce** holds for 3,000 ticks (a minute).
- **Tribute**: the 25-coin gift, paid to a stronger nation the player is at war or at odds with, also buys a truce.
- **Propose an alliance**: as before (+30), but refused by a nation at war with the player, and a nation at least twice the player's strength wants +60.

Under a truce neither side raids nor besieges the other, a pawn-led nation does not denounce, and the pair's relations cannot grate (only warm).

### Succession
When the player dies as a member of a nation (`Succession`), an heir from that nation takes their place instead of the player respawning: family first (a pawn sharing the ruler's surname -- the first ruler has none, but each heir brings theirs and children take a parent's), then the most loyal, then whoever joined first; a pawn starving that same tick is passed over. The player is one entity for the whole game, so the heir is folded into it: the player takes the heir's name, position, energy (at least 50), belongings (the old ruler's are dropped as usual when "Keep Inventory" is off) and home, and a non-leader takes the heir's stall if they had none; the heir pawn leaves the world. The nation, its settlements, laws and relations are untouched. Members' loyalty to the new ruler starts at half what it was, and the heir's own family warm by 10. A succession screen tells who died, after how long a reign, who rules now and why, and lists the line of rulers; the chronicle records it.

With no member left to succeed, the line ends and so does the run: it is a defeat (see *Eras and the end of a run*), told on the end-of-run screen, which offers *New world*, *Load* and *Main menu*, and the world is never saved again (Load brings back the last save, from before the death). A nationless wanderer still rises again as before. `--stage-succession` opens the diplomacy stage with the player about to starve; `--start-screen laws` opens the nations screen on its Laws tab.

## Sound, feedback and tips (host)
Not a simulation system: it lives in the host, in [`src/BeyondNations.Desktop/ui/feel/`](../src/BeyondNations.Desktop/ui/feel/), shared by the desktop and the browser. It reads the world and never writes to it, draws nothing from the simulation's `RandomSource`, and runs on real frame time, so the same seed steps identically with it on, off or missing (`TestFeel.testFeelLayerDoesNotChangeTheSimulation`, and the History replay job).

- **What happened.** `FeelObserver` compares the world with the previous frame: the player's inventory (wood next to a tree that has just gone is a chop, stone by a rock is mining, meat by a chicken is a hunt, a crafted item appearing as inputs disappear is a craft, coins either way), the energy tier falling to Hungry or Starving, the objective count, and each settlement's building count. Raids on the player's nation, new settlements and new stalls arrive as events. The first frame after a world is built or loaded only records, so a loaded save is not mistaken for a haul.
- **Sound.** `FeelDirector` turns those into `SoundCue`s for an `ISoundService`: chop, mine, pickup, craft, build complete, coin, hunger warning, combat hit (hunting a chicken, a raid on your nation), UI click (any button, any screen change), objective complete, and an ambient loop that follows the time of day (day: wind and birds; night: a lower wind and crickets). A building going up elsewhere is quieter with distance and silent past 90 units. The browser's service is `WebAudioSoundService`, which synthesises every cue with WebAudio in `wwwroot/audio.js` (no audio files) and starts only after the first key, click or tap. The desktop uses `NullSoundService`: Strata has no audio module and the desktop takes on no audio dependency, so it is silent.
- **Juice.** `JuiceEffects` adds presentation-only items to the frame's `WorldSnapshot` after it is captured (`addEffectItem`, `setEntityItem`): wood chips, leaves, stone flakes, feathers and sparks as small falling cubes; a new building rises out of its plot over 1.6 seconds inside a four-post scaffold; floating "+5 stone" text (ImGui, under the HUD panels); a gold screen flash when an objective completes; and a short camera shake on chops, mining, hunting and raids.
- **The first five minutes.** `OnboardingHints` shows one tip at a time in a new world (never a loaded one): walk and look, chop a tree, gather stone, found a settlement (create a nation first), and where the goals panel is. A tip goes as soon as its goal is met, or on *Got it*; *No tips* turns them off. Finished tips are recorded in `FeelSettings` and never shown again. On a touch-only device the wording names the on-screen pad and *Use* button. The tips are separate from the objectives, which they only read.
- **Settings.** `FeelSettings` (volume, mute, tips on or off, tips finished) belong to the player, not a world, so they are kept beside the saves rather than in them: `feel-settings.txt` under the user data directory on the desktop, the `beyond-nations/feel-settings` key in the browser's localStorage. They are plain `key=value` lines read leniently. The config screen has *Volume*, *Mute Sound* and *Show Tips*; the pause screen has *Sound: On/Off* where there is sound.

## Seasons
The year turns through **spring, summer, autumn and winter**, `SeasonCalendar.DAYS_PER_SEASON` (2) in-game days each, so a year is eight days: about 80 minutes of play at the default tick rate. A world starts on the first day of spring. What each season does is one table, `SeasonalRules`:

| Season | Farms (apples per round) | Saplings | Chickens hatch | Energy burned outdoors |
| --- | --- | --- | --- | --- |
| Spring | 1 | grow | yes | normal |
| Summer | 1 | grow | yes | normal |
| Autumn | 2 | grow | no | normal |
| Winter | none | take 3x as long | no | x1.3 (cold) |

Plains farms harvest one apple more whenever farms harvest at all. In spring and summer a chicken hatches every 30 seconds in a random chunk (always on the plains, rarely on the moor) until the world holds 50. Indoors is as warm as ever: the cold applies only outside a settlement, to the player and pawns alike.

**Apples spoil, and winter trees are bare.** Without these the good seasons stored up a mild winter: pawns fell trees for wood all year, every tree carries 1 to 3 apples, and nothing rotted, so by the first day of winter a pawn carried about 200 apples and the markets held over 20,000, and both were still growing at winter's end. Now, every 30 seconds (`APPLE_SPOIL_INTERVAL_TICKS`), a pawn's apples beyond 12 (`PAWN_APPLES_KEPT`, three winter stores) and a market stall's beyond 10 (`STALL_APPLES_KEPT`) lose a sixth, rounded up (`SeasonalRules.applesSpoiling`, run by `SeasonalWorld`): a hoard halves in about two minutes, a modest store never rots. And in winter a tree a pawn fells gives wood and saplings but no apples (`treesBearFruit`); the apples stay on a tree left standing, so it bears again in spring. The player is spared both: their pack and their own stalls never rot, and a tree still gives them its apples, so a world started in winter can still teach eating and nothing the player gathered vanishes. Spoilage only removes apples, so the save format is unchanged.

**Pawns respond.** From autumn, a pawn carrying fewer than 4 food (`WINTER_FOOD_STORE`) leans towards food when it gathers: a chicken within 120 units, or else a tree (which carries apples) over a rock; in winter, with the trees bare, only the chicken. In winter an idle pawn at home leaves half as often, since indoors burns less.

**Measured** headless from the first spring into the second for seeds 42, 7 and 2026: pawns carry at most about 23 food each (in autumn), about 10 to 15 as winter comes and under 1 by its last quarter, when two thirds or more of them are empty-handed and the markets are down to an eighth of their stock (about 2,000 to 250 food on seed 42). Pawns go hungry and some die late in winter (0 to 6 a winter), and the population stays at the 100-pawn cap throughout: a pawn that starves comes back at home. Deaths over the first year are 3 to 12, against 6 to 9 before apples spoiled. On the leanest of the three (2026, a world of a few hundred trees) the hunger runs on into the first days of spring, about 20 more deaths, until the farms and the trees restock the markets. Trees fall to a few hundred by midwinter and are back over a thousand in spring, and chickens are hunted down in autumn and hatch back in spring. `TestSeasonalRules` runs seed 42 through its first winter and checks that it is lean and survivable.

**Seeing it.** The ground is tinted fresh in spring, golden in autumn and snowy in winter, and leaves turn orange in autumn and white in winter, blending over the last quarter of each season (`WorldPalette`, applied by `WorldSnapshot` as it captures; nothing stored changes). The HUD's Season box, above the player box, shows `Autumn - day 2/2, year 1`, then `Winter in 2 days` (amber) from two days out or `Winter: no harvests` (blue) in winter, then the region underfoot. The status line announces each new season and, the day before, that winter comes tomorrow. `--season NAME` (`?season=` in the browser) opens a new world on the first day of that season by moving the clock on, without running the world.

**The season API**, for events, caravans and anything else that should care: `simulation.getSeasonCalendar()` gives
- `getSeason()` (`Season.SPRING` .. `Season.WINTER`) and `isWinter()`;
- `getDayOfYear()` (0 .. 7), `getDayOfSeason()`, `getYear()` (0 for the first);
- `getDaysUntil(season)` and `getDaysUntilWinter()` (0 while it is winter);
- `getSeasonProgress()`, how far through the season in [0, 1);
- and the pure `seasonOfDay(day)`, `daysUntil(day, season)`, `name(season)` for any other day.

The calendar holds no state: the season is a pure function of the tick count, so it is saved by saving the tick count and nothing was added to the save format.

## Regions
Every chunk belongs to a **region**, chosen by `RegionMap` from the world seed and the chunk's coordinates: two smooth noise fields, elevation and moisture, sampled on a grid four chunks across, so regions come in patches several chunks wide. High ground is **highlands**; otherwise wet ground is **forest**, dry ground **moor**, and the rest **plains** (about 30% plains, 26% forest, 24% highlands and 19% moor across seeds). Regions draw nothing from the world's random streams and are rebuilt from the seed on load; see [Saves](./SAVES.md).

What a region holds is one table, `RegionCatalog`; each factor is relative to the old, region-less world:

| Region | Trees | Rocks | Chickens | Food overall | Ground |
| --- | --- | --- | --- | --- | --- |
| Plains | 0.9 | 0.8 | 2.0 | 1.6 (+1 apple per farm round) | grassy green |
| Forest | 1.8 | 0.6 | 0.8 | 1.2 | dark green |
| Highlands | 0.6 | 2.2 | 0.5 | 0.6 | grey-green |
| Moor | 0.5 | 0.9 | 0.4 | 0.5 | brown |

**Stone stays reasonable everywhere.** Every chunk gets at least two rock attempts whatever its region; regrowth picks any generated chunk, so stone comes back everywhere, and surfaces two rocks at a time in the highlands. Measured over two years, seed 7 (little highland near its start) kept 15 to 120 rocks where it used to fall to about 10.

**Surpluses and shortages**, for trade (caravans) and diplomacy: `simulation.getNationResources()` (`NationResources`) gives, for a nation,
- `getAbundance(nation, commodity)`: the average of `RegionCatalog.abundance` over the land around its settlements (each settlement's chunk counted twice and the eight around it once), where 1.0 is the old world's;
- `getStanding(nation, commodity)`: `SURPLUS` at 1.35 or more, `SHORTAGE` at 0.75 or less, else `NORMAL`;
- `getSurpluses(nation)` and `getShortages(nation)`, in the order wood, stone, food;
- `canSupply(supplier, buyer, commodity)`: the supplier has a surplus the buyer is short of;
- and `itemTypesOf(commodity)` for the item types each `Commodity` (`WOOD`, `STONE`, `FOOD`) covers.

So a highland nation has a surplus of stone and a shortage of wood and food, and a forest nation a surplus of wood and a shortage of stone. It is the land's endowment, not a stock count: it changes only when a nation gains or loses a settlement, draws no randomness, and changes nothing by being asked. A nation with no settlements has neither surplus nor shortage.

*Planned:* caravans and diplomacy that act on surpluses and shortages; weather.
## World Events
World events make runs diverge (`WorldEvents`, [`src/BeyondNations.Core/worldevents/`](../src/BeyondNations.Core/worldevents/)). They are seeded and deterministic: they draw from a random stream of their own (the world seed xor a salt), so the same seed throws the same events, and having them never shifts the rolls of the rest of the world.

**Two clocks.** About every 10 to 15 minutes of play (`PLAYER_EVENT_MIN_SECONDS`..`MAX`, the first after 7 to 11) an event is offered to the player, chosen by weight from the *personal* events and, when the player leads a nation, that nation's events; the same event is never offered twice running. When nothing fits, the clock looks again a minute later. Every 4 to 7 minutes a nation not led by the player meets a nation event and its leader chooses at once (`chooseForNation`, from the world's state). Both kinds are told in the chronicle; lines about the player's nation are gold as usual.

**Choosing.** A player's event waits as a *pending choice*. The host draws a sheet over the HUD (`WorldEventSheet`): a title, what happened, and two or three full-width buttons with what each choice costs and brings; a choice that cannot be taken is greyed out with the reason. The host stops advancing the world while it is up. A choice nobody answers within 3 minutes of ticks -- possible only when nothing draws the sheet, as in a headless run or the history export -- takes the event's default.

| Event | Who | When (weight) | Choices |
| --- | --- | --- | --- |
| Drought | nation | has farms; not in winter; not already in drought | dig irrigation (15 wood, 10 stone: farms dry a quarter day); buy grain (25 coins: 12 apples now, dry a day); ration and wait (dry a day, members -2 toward the player) |
| Sickness | nation | a settlement with 3+ residents | pay a healer (20 coins, residents +2); quarantine (the 3 weakest -40 energy); let it run (the weakest falls if already run down, the others -70) |
| Wanderers at the gate | nation | has a settlement (more likely with room) | welcome all 2-3 (2 food each); take only the strongest (one joins); turn them away (they stay, nationless) |
| Bandit camp | nation | a camp or village (not a town) with wealth | raise a militia (3+ members; everyone -25 energy); pay 30 coins; bar the gates (robbed of 5 coins, 5 wood, 5 stone a minute for a day and a half; while the camp stands, a "Deal with Bandits" button on the command bar brings the choice back: militia, pay, or keep the gates barred) |
| A claimant stirs | nation | another pawn-led nation of 3+ (likelier if Rival or Hostile) | back them (20 coins: they take power, +15 relations); warn its leader (+8, the claimant is exiled); stay out (half the time they win anyway, -10) |
| A bountiful harvest | nation | not in winter, not in drought | fill the stores (10 + 4 per farm apples); sell (2 coins each); feast (members and player to full energy, members +3) |
| Fire at the storehouse | nation | a storehouse and 10+ wood and stone stockpiled | bucket chain (lose a quarter; residents -25 energy); save the stone (lose all wood); let it burn (lose three quarters) |
| A lost traveller | personal | always | share a meal or pay 15 coins for a map: 4 rich rocks and a 30-coin cache 160 paces off in the direction named; or send them on |
| A travelling merchant | personal | the player has 50 coins or 20 wood | an axe and a pickaxe for 50 coins; 20 wood for 15 stone; decline |

Outcomes go through the ordinary systems: inventories, treasuries and stockpiles, nation membership and joins, leadership changes, relations (`Diplomacy.change`), pawns' energy and relationships, and new entities in the world. A death from sickness is the simulation's ordinary pawn death on the next tick.

**Claimants and the realm.** The claimant goes through the realm's rules (`Realm`): a claim stands only where `Realm.whyClaimFails` allows it, so the player's own rule is never usurped (it passes only to an heir) -- not even when the player has come to lead the nation between the claimant's arrival and the choice, in which case the claim comes to nothing and costs nothing. The seizure is `Realm.usurp`: the deposed leader stays on as a serf (and, as the eldest member, is usually the heir when the usurper dies), the nation keeps its laws, and any law change proclaimed but not yet in force lapses. A warned-off claimant is `Realm.expel`led. Goodwill and grudges are `Realm.changeRelations`, so a war keeps the two nations from warming (backing or warning brings no goodwill) and a truce keeps a spurned claimant from bearing a grudge; the choice sheet says so.

**Lasting effects.** A drought (no farm harvest for the nation, `Simulation` asks `WorldEvents.isDrought`) and a bandit camp (robbing on a timer) are `WorldEffect`s with an end tick; each says so in the chronicle when it ends.

**Seasons and regions.** Events read the `SeasonCalendar`: no drought and no bountiful harvest in winter. The lost traveller's map points into the highlands when any direction leads there (`RegionMap`), and says which land it leads to. Both clocks start on the first tick the world runs, so a world opened part-way through its year (`--season`) does not meet an event at once.

**Adding an event.** Implement `IWorldEvent` (stateless; everything about one happening lives in its `WorldEventInstance`, which a save holds) and register it at the end of `WorldEventRegistry.createDefault`. The instance's fields (nation, other nation, settlement, entity, magnitude) are general, so a new event needs no new save format.

Pending choices, running effects, both clocks and the stream are saved (save format 3, see [SAVES.md](./SAVES.md)).

## Trade
Caravans carry goods from where they are cheap to where they are dear (`TradeCaravans`, one call a tick from `Simulation`). Everything is deterministic: routes, cargo and timing follow from the world's state, with no random draws.

**Sending.** Every 1,500 ticks each nation led by a pawn that has no caravan on the road, and has not sent one in the last 4,500 ticks, looks for a route. A settlement can spare what its stockpile holds above a reserve (40 wood, 30 stone, 4 + 2 food per resident; `TradeNeeds`). Up to 20 of its most plentiful spare goods, at least 5, go to whichever market within 1,200 units -- another nation's or its own -- would pay the most more than its own merchants would, counting only what that market's stalls and treasury could actually pay. A nation never trades with one it is at war with (a declared war, or Hostile relations). The caravan also takes a purse of up to 30 coins from the treasury. The player's nation never sends caravans on its own.

**The land (Regions, `NationResources`).** A nation whose land is short of a commodity keeps half as much again of it back and its markets want half as much again of it; one with a surplus keeps back and wants two thirds as much (`TradeNeeds`). A route that carries what the origin's land has plenty of to a nation whose land lacks it (`canSupply`) is favoured by a quarter.

**War and peace (Realm).** A declared war, as well as Hostile relations, stops caravans going to that nation and turns back any on the road; a truce is peace, so caravans travel under one, and the player cannot rob the caravans of a nation theirs has a truce with. A caravan selling at a market under Open Borders warms relations twice as much (+4).

**The road.** A caravan is an entity (`Caravan`): a cart with four wheels, a bale, a pack in the nation's colour and its drover, labelled with its nation, where it is going and what it carries. It rolls at 8 units a second (a third of the player's walk) straight across the flat ground. If its market is lost, or its nation goes to war with the market's, it turns for home; if home is lost it makes for its nation's nearest settlement; if its nation falls, the cart is abandoned and its load left on the ground as drops.

**Selling.** At its market it sells a unit at a time at the landing price (above): to the stall with the most coin that can pay, then to the settlement's treasury for its stockpile, until no one can pay. The rest rides home. Selling at another nation's market is worth +2 between the two. Home again, the takings and anything unsold go into the treasury and stockpile.

**The player.** Within 20 units of a caravan out in the world, the command bar offers:

| Command | Effect |
| --- | --- |
| *Buy wood (96)* | One of each of up to three of its goods, at the price it expects at market |
| *Sell to Caravan* | One of each good it deals in that the player carries, at 80% of its market's paying price, while its purse lasts |
| *Escort Caravan* | Ride guard: within 60 units when it arrives, the player is paid 15% of its takings (at least 5) from its purse and its nation gains +8 with the player's. Straying 150 units away ends the escort |
| *Raid Caravan* | Take everything it carries; the cart is abandoned. Its nation loses 30 with the player's, enough to turn a Rival Hostile -- war. A nationless raider loses 10 with every one of its people. Not the player's own nation's caravans, nor one they are guarding |

A player who leads a nation and stands in one of its settlements with a Storehouse gets *Send Caravan*, which opens a panel: previous/next to pick a market (with its relation and distance), -5/+5 per good to load up to 40 from what the player carries, each good's price here and there, and an estimate of what the load will fetch. At most two of the player's caravans are on the road at once; their takings go into the home treasury. Every control is a button, so it works the same by touch.

Coins only ever change hands in all of this; `TestTradeCaravans` counts every coin in the world before and after each kind of trade, delivery, escort and raid.

**The chronicle** tells what the player had a hand in -- caravans sent, sold and home, escorts, raids -- and anything to or from the player's nation; of the rest, only the first haul of the day worth 400 coins or more.

`--stage-trade` (desktop and web, `?stage-trade`) opens the game leading a village that can send caravans, with a trading neighbour's caravan passing just ahead.

## Eras and the end of a run
A run has an arc ([`src/BeyondNations.Core/era/`](../src/BeyondNations.Core/era/)): the player's nation advances **Camp → Village → Town → Kingdom**, and the run ends in a victory or a defeat. `EraTracker` judges the player's nation once a second of game time, one era at a time (so each is announced), and eras never go back. The numbers live in `EraRules` and `VictoryRules`.

| To leave | Needs | Reaching the next era unlocks |
| --- | --- | --- |
| Camp | a village (a settlement with 2 buildings), 10 members, 2 market stalls; day 3 | Village: the **workshop** (planks and bricks at home); up to 3 settlements |
| Village | a town (5 buildings), 18 members, 2 settlements, 1,000 funds; day 7 | Town: the **storehouse** (more stalls); up to 6 settlements; Dominion and Concord victories |
| Town | 2 towns, 25 members, 4 settlements, 3,000 funds; day 19 | Kingdom: any number of settlements; the Prosperity victory |

Funds are the player's coins plus the treasuries of the nation's settlements. Each era also waits on the calendar (a day is ten minutes): settlers flock to a new nation, and the pacing bot found most nations a village within two or three minutes, so a camp is a village no sooner than day 3 (about 17 minutes in), a town no sooner than day 7 (about an hour) and a kingdom no sooner than day 19 (about three hours). A camp may put up houses and farms and hold one settlement. **Until the Town era the nation cannot be besieged** (`EraRules.isSheltered`; raids still come): the pacing bot found a new nation besieged and taken within minutes by neighbours twice its size, and every nation it refounded falling the same way ([PACING.md](./PACING.md)). From the Town era its towns have palisades and its other settlements can fall. The limits apply only to a nation **the player leads** (`Settlement.isBuildingUnlocked`, `Nation.canFoundSettlement`, set every tick by `EraTracker.applyRules`), and bind the nation's pawns too: its settlers do not leave for a site the era cannot hold, and its serfs do not build what it has not unlocked. Pawn-led nations are never held back. A locked building says so (`The Workshop is unlocked in the Village era.`), and so does founding past the limit.

**Objectives follow the era.** The arc's seventh step, which was "Grow your nation to 5" and was met by settlers joining on their own, is now *Reach the Village era*, with members and buildings as its counters. After the arc, ambitions cycle through nation size, funds, settlements (or buildings, where the era will not allow more settlements) and the next era -- in the kingdom, *Win the game* -- and each is scaled to the era it was made in: nation size starts at 16 in the village, 20 in the town and 35 in the kingdom, funds at 1,000 (the arc's rewards and treasuries already make 500), 3,000 and 4,000, and every lap adds 5 members or 500 coins.

**Victory** comes from one of three, judged on the player's nation:

| Victory | When |
| --- | --- |
| Dominion | from the Town era: more than half of every settlement in the world, and at least 6 |
| Prosperity | in the Kingdom era: 6,000 funds and 40 members |
| Concord | from the Town era: allied with every other nation still standing, at least 2 of them |

**Defeat**: once the player's nation has held a settlement, a nation with no settlement and nobody but the player -- or no nation at all -- is collapsing. The status line warns, the era box counts down, and if nothing changes for five minutes of game time (half a day) the run is lost. Founding a settlement or winning a follower calls it off.

**The end of a run.** A victory or a defeat stops the world under the end-of-run screen: the outcome, the era reached, the days survived, when each era was reached, the nation's members, settlements and funds (now and at their most), objectives completed, and *The History of <Nation>*: every chronicle line about the player or their nation, kept by the tracker beyond the chronicle's 200 (up to 400), with the era milestones in gold. The history scrolls by dragging (finger or mouse), the wheel or a wide scrollbar. After a victory the player can **Continue playing** (the eras go on, no other outcome is recorded); either way they can start a **New world** (the ended one keeps its save) or go to the **Main menu**. A defeated world shows the screen again when it is loaded.

**On the HUD**, an era box above the objective tracker shows the era and a bar of the next era's requirements (`Town: 2/4`); tapping or clicking it opens the requirements, what the next era unlocks and, from the Town era, how close each victory is.

Era state -- the era and when each was reached, the outcome, the collapse countdown, the history and the era each ambition was made in -- is saved (format 5, [SAVES.md](./SAVES.md)). `--stage-victory` and `--stage-defeat` open on the end-of-run screen of a staged run (`EndingShowcase`).

