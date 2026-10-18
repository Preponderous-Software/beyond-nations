# Inventory System

This document describes the improved inventory system in Beyond Nations.

## Overview

The inventory system has been refactored to use a more formal structure with the following key classes:

- **Item**: Represents an individual item with its type
- **ItemStack**: Represents a stack of items of the same type with a quantity
- **ItemSlot**: Represents a slot that can hold an ItemStack
- **Inventory**: Contains a list of ItemSlots
- **ItemDrop**: An entity that represents an ItemStack existing in the world as a drop

## Architecture

### Item
The `Item` class is the basic building block, representing a single item with a type. It provides:
- `getType()`: Returns the ItemType
- `isSameTypeAs(Item)`: Checks if two items are of the same type

### ItemStack
The `ItemStack` class represents a stack of items with a quantity. It provides:
- `getItem()`: Returns the Item
- `getItemType()`: Returns the ItemType
- `getQuantity()`: Returns the current quantity
- `setQuantity(int)`: Sets the quantity
- `addQuantity(int)`: Adds to the quantity
- `removeQuantity(int)`: Removes from the quantity
- `isEmpty()`: Checks if the stack is empty (quantity <= 0)
- `canStackWith(ItemStack)`: Checks if two stacks can be combined

### ItemSlot
The `ItemSlot` class represents a container that can hold an ItemStack. It provides:
- `isEmpty()`: Checks if the slot is empty
- `getItemStack()`: Returns the ItemStack in the slot
- `setItemStack(ItemStack)`: Sets the ItemStack in the slot
- `clear()`: Clears the slot
- `hasItemOfType(ItemType)`: Checks if the slot contains a specific item type
- `getQuantity()`: Returns the quantity of items in the slot

### Inventory
The `Inventory` class contains a list of ItemSlots. The public API remains unchanged for backward compatibility:
- `getSlots()`: Returns the list of ItemSlots (new method)
- `getNumItems(ItemType)`: Returns the number of items of a specific type
- `addItem(ItemType, int)`: Adds items to the inventory
- `removeItem(ItemType, int)`: Removes items from the inventory
- `hasItem(ItemType)`: Checks if the inventory contains an item type
- `setNumItems(ItemType, int)`: Sets the number of items of a specific type
- `clear()`: Clears all items from the inventory
- `transferContentsOfInventory(Inventory)`: Transfers all items from another inventory
- `containsAbundanceOfResources()`: Checks if the inventory has abundant resources
- `getTotalNumItems()`: Returns the total number of items

### ItemDrop
The `ItemDrop` class extends `Entity` and represents an ItemStack that exists in the world as a physical entity. The player picks up every ItemDrop within reach by interacting (`InteractCommand`); pawns do not pick them up. It provides:
- `getItemStack()`: Returns the ItemStack this drop represents
- `dropContentsOf(Inventory, Vector3)`: Returns one ItemDrop per non-empty slot, resting on the ground below the given position, and empties the inventory

When keep-inventory is off, `Simulation` uses `dropContentsOf` on death, so the inventory of a dead player, or of a pawn that respawns, is left where it died instead of being deleted.

## Usage Examples

### Creating an Item
```csharp
Item woodItem = new Item(ItemType.WOOD);
```

### Creating an ItemStack
```csharp
ItemStack woodStack = new ItemStack(ItemType.WOOD, 10);
```

### Creating an ItemSlot with an ItemStack
```csharp
ItemStack appleStack = new ItemStack(ItemType.APPLE, 5);
ItemSlot slot = new ItemSlot(appleStack);
```

### Using the Inventory (Backward Compatible)
```csharp
Inventory inventory = new Inventory(100); // 100 gold coins
inventory.addItem(ItemType.WOOD, 5);
inventory.addItem(ItemType.STONE, 3);

int woodCount = inventory.getNumItems(ItemType.WOOD); // Returns 5
bool hasWood = inventory.hasItem(ItemType.WOOD); // Returns true
```

### Creating an ItemDrop in the World
```csharp
ItemStack dropStack = new ItemStack(ItemType.COIN, 50);
Vector3 position = new Vector3(10, 0, 10);
ItemDrop drop = new ItemDrop(position, dropStack);
```

## Backward Compatibility

The refactored `Inventory` class maintains full backward compatibility with the previous implementation. All existing code that uses the Inventory class will continue to work without modification.

The internal implementation has changed from a `Dictionary<ItemType, int>` to a `List<ItemSlot>`, but the public API remains the same.

## Benefits

1. **More Flexible**: The slot-based system allows for future extensions like:
   - Stack size limits per slot
   - Item metadata (durability, enchantments, etc.)
   - Different slot types (equipment slots, etc.)

2. **Better Structure**: The separation of concerns makes the code more maintainable:
   - Item: Represents the item type
   - ItemStack: Handles quantity
   - ItemSlot: Handles slot logic
   - Inventory: Manages collections of slots

3. **World Drops**: ItemStacks can now exist in the world as ItemDrop entities, allowing for dropped items that the player can pick up.

4. **Extensible**: The system is designed to be easily extended with new features in the future.
