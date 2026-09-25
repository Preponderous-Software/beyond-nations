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
The inventory system manages the items and resources held by an entity. Items are collected from the world and traded at markets.

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

*Planned:* pawn traits that affect behavior and interactions with other pawns.

## Market
The market system is used to buy and sell resources within a settlement. Each settlement has a market with a limited number of stalls, which can be built or purchased by pawns and by the player. Stall owners stock their stalls with items and collect the profit from sales. Item prices are calculated from the item's base cost and the market's current supply of that item, rather than being set per stall.

## Crafting
*Planned.* The crafting system will be used to create items from resources. Players will be able to craft items using the resources they collect from the world.

## Territory
*Planned.* The territory system will be used to create and manage territories. Territories will be used to claim land and build structures.
