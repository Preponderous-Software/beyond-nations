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
| Building | Implemented | [`src/BeyondNations.Core/command/settlement/`](../src/BeyondNations.Core/command/settlement/) |
| Pawn | Implemented | [`src/BeyondNations.Core/entity/entities/Pawn.cs`](../src/BeyondNations.Core/entity/entities/Pawn.cs), [`src/BeyondNations.Core/behavior/`](../src/BeyondNations.Core/behavior/) |
| Market | Implemented | [`src/BeyondNations.Core/market/`](../src/BeyondNations.Core/market/) |
| Crafting | Planned | — |
| Territory | Planned | — |

**Note:** This document is a work in progress. It will be updated as the project progresses and more systems are planned/implemented. "Implemented" does not mean feature-complete — see the open issues for planned expansions to each system.

## Event
The event system records notable occurrences in the game — chunk generation, nation creation/join/leave/disband, pawn and player deaths, pawn spawns, the player falling into the void, and pawn relationships improving. Events are produced through a single producer and stored in a repository that can be queried by event type, which is what drives the event counts shown in the debug display.

*Planned:* letting other systems subscribe to events so that an event can trigger further actions, rather than only being recorded.

## World Generation
The world generation system creates a procedurally generated world out of chunks of locations. Generated land is populated with resources, settlements, and pawns.

## Inventory
The inventory system manages the items and resources held by an entity. Items are collected from the world and traded at markets. When "Keep Inventory" is turned off, a player that dies — or a pawn that dies with "Respawn Pawns" on — leaves its inventory where it died as item drops, one per item type (a pawn that dies for good still takes its inventory with it); the player picks up every drop within reach by interacting (`E`). Pawns do not pick drops up. The player's holdings are summarised in a HUD box in the corner of the world screen; pressing `I` opens the inventory screen, which lists every occupied slot and pauses the world until `I` or `Escape` closes it.

*Planned:* using held resources as crafting inputs.

## Nation
The nation system creates and manages nations. Players can create their own nation or join an existing one, and nations track their members and their roles.

*Planned:* per-nation culture and history, alliances, and conflict between nations.

## Resource Collection
Players and pawns collect resources by interacting with the nearest entity in range. A tree yields wood, a rock yields stone and a chicken yields chicken meat, for both. A pawn only hunts a chicken when it needs food — energy under the threshold with nothing edible carried — and otherwise gathers from whichever of the nearest tree and nearest rock is closer, so pawns after wood or stone leave the chickens alone. These resources are used to build structures, to trade, and — for apples and chicken meat — to eat.

*Planned:* using collected resources as crafting inputs.

## Food and Energy
The player and every pawn carry an energy level that falls each tick by their own metabolism. A pawn inside a settlement burns energy more slowly than one outside it, and the player's energy does not fall at all while inside one. Reaching zero energy produces a death event and, subject to configuration, a respawn at the home settlement. Energy is restored by eating: below a threshold, the most nourishing edible item carried is consumed and its energy added. Which item types are edible, and how much each restores, is held in one table rather than being named at each call site, so an apple and chicken meat are both food and a pawn carrying either has no reason to go looking for more.

Markets read the same table. A hungry pawn in a settlement first takes food from its own stall, if it owns one holding any, and otherwise buys food when some stall it can afford holds any; only when neither is possible does it leave to gather or hunt. Buying picks whichever food gives the most energy per coin at the market's current supply-based prices, and falls back to the next food when that one cannot be afforded.

## Building
The building system creates and manages settlements. Players and pawns can found a settlement and build stalls within it.

*Planned:* additional building types providing shelter and storage for a settlement's pawns.

## Pawn
The pawn system creates and manages pawns. Pawns populate the world and interact with each other, the player, and the world. On each tick a behavior calculator chooses a behavior for the pawn — gathering, wandering, trading, construction, or nation actions — and a behavior executor carries it out.

### Relationships and leadership
Every pawn keeps a relationship number for the player, shown as a named tier: Stranger (below 5), Acquaintance (5+), Friend (15+) and Loyal (30+). Once the player has met a pawn, its nametag reads `Name (Tier)`, and the world command bar names the pawn in reach the same way.

- **Talking** (`E`, or *Talk to …*): the pawn answers with a line built from its situation rather than a random phrase -- how well it knows the player, then the most pressing thing on its mind: hunger, an order it is working on, whether it has a nation (and whether it would join the player's), its loyalty to a rival nation, or what it is doing for the player's own nation. The first conversation with a given pawn in any 30-second window raises the relationship by 2; talking again sooner still gets an answer but does not count.
- **Gifting** (`G`, or *Gift …*): hands over something from the player's inventory, chosen for the pawn. A hungry pawn gets one food item and values it most (+6); otherwise 10 coins (+2), then one food item (+2), then 5 wood or 5 stone (+1).
- **Inviting** (`R`, or *Invite …*): a player in a nation can recruit a nationless pawn that is at least an Acquaintance. A pawn in another nation has to be Loyal to defect; its old nation loses the member, any stall it owned there is given up, and it starts over without a home settlement or order. A pawn that leads its nation never defects, so an invitation can never leave a nation leaderless or disband it.
- **Leading**: when the player leads the pawn's nation, two more commands appear. *Order …: Gather Wood* asks a member that is at least an Acquaintance to bring 10 wood to the nation (the nation needs a settlement); the pawn keeps living its own life, and whatever wood it carries is handed into the settlement's stockpile whenever it is inside one of its nation's settlements, until the quota is met, at which point its relationship rises by 3. The leader takes the stockpile's wood and stone with *Collect Stockpile* inside the settlement. *Appoint … Merchant* hands a serf a free stall in its home settlement, making it a merchant (+6).

*Planned:* pawn traits that affect behavior and interactions with other pawns.

## Market
The market system is used to buy and sell resources within a settlement. Each settlement has a market with a limited number of stalls, which can be built or purchased by pawns and by the player. Stall owners stock their stalls with items and collect the profit from sales. Item prices are calculated from the item's base cost and the market's current supply of that item, rather than being set per stall.

## Crafting
*Planned.* The crafting system will be used to create items from resources. Players will be able to craft items using the resources they collect from the world.

## Territory
*Planned.* The territory system will be used to create and manage territories. Territories will be used to claim land and build structures.
