using System;
using System.Collections.Generic;
using System.Reflection;

using beyondnations;

namespace beyondnationstests {

    /**
    * Every field of every type a Simulation is made of, and what a save does
    * with it (docs/SAVES.md). TestSaveCoverage fails when a type gains a field
    * that is not listed here, so a new piece of world state cannot slip into
    * the game without someone deciding how it is saved.
    *
    *   Saved     -- written to the save and read back.
    *   Derived   -- not written: rebuilt on load, from the settings, from other
    *                saved fields, or by wiring (a reference to a service the
    *                fresh Simulation builds). Still compared by WorldComparer,
    *                so a derived field that comes back wrong fails the
    *                round-trip tests.
    *   Transient -- not world state, and not compared: wall-clock
    *                measurements, and what a fresh event points at.
    */
    public static class SaveCoverage {
        public class Entry {
            public string[] Saved = new string[0];
            public string[] Derived = new string[0];
            public string[] Transient = new string[0];
        }

        private static Entry saved(params string[] fields) {
            return new Entry { Saved = fields };
        }

        public static readonly Dictionary<Type, Entry> Manifest = new Dictionary<Type, Entry> {
            [typeof(Simulation)] = new Entry {
                Saved = new[] { "gameConfig", "random", "tickCounter", "timeOfDay", "player", "eventRepository", "entityRepository",
                    "environment", "worldGenerator", "nationRepository", "pawnNameGenerator", "nationNameGenerator", "settlementLife",
                    "diplomacy", "playerTerritory", "playerTerritoryKnown", "traitRandom", "populationGrowth", "numPawnDeaths", "numPlayerDeaths",
                    "objectiveTracker", "chronicle", "realm", "worldEvents", "tradeCaravans", "eraTracker" },
                // seasons come from the tick count and regions from the seed, so all five are rebuilt (docs/SAVES.md)
                Derived = new[] { "eventProducer", "pawnBehaviorCalculator", "pawnBehaviorExecutor", "lagPreventer",
                    "regionMap", "seasonCalendar", "seasonalWorld", "worldPalette", "nationResources" },
            },
            [typeof(GameConfig)] = new Entry {
                Saved = new[] { "chunkSize", "locationScale", "respawnPawns", "keepInventoryOnDeath", "lagPreventionEnabled", "worldSeed",
                    "ticksPerSecond", "statusExpirationTicks", "playerWalkSpeed", "playerRunSpeed", "ticksBetweenBehaviorCalculations",
                    "ticksBetweenBehaviorExecutions", "minDistanceBetweenSettlements", "settlementJoinRange", "settlementMetabolismMultiplier",
                    "playerMetabolism", "ticksBetweenSettlementIncome", "renderDistance", "maxNumEntities", "maxNumChunks", "maxNumPawns" },
                // the machine's data directory, not the world's
                Derived = new[] { "beyondNationsDirectoryPath" },
            },
            [typeof(RandomSource)] = new Entry {
                Saved = new[] { "seed", "seedArray", "inext", "inextp" },
            },
            [typeof(TickCounter)] = new Entry {
                Saved = new[] { "totalTicks" },
                Derived = new[] { "ticksPerSecond" },
                Transient = new[] { "mtps", "ticksSinceMeasurement", "lastMeasurement" },
            },
            [typeof(TimeOfDay)] = new Entry {
                Saved = new[] { "startFraction" },
                Derived = new[] { "tickCounter", "ticksPerDay" },
            },
            [typeof(EventRepository)] = new Entry {
                Saved = new[] { "events", "droppedCounts" },
                // subscriptions: the fresh Simulation's chronicle subscribes again as it is built
                Derived = new[] { "listeners", "typedListeners" },
            },
            [typeof(EventProducer)] = new Entry { Derived = new[] { "eventRepository" } },
            [typeof(Event)] = new Entry {
                Saved = new[] { "type", "description" },
                // saved, but the clock it reads is the wall clock, so it is not compared
                Transient = new[] { "date" },
            },
            [typeof(EntityRepository)] = new Entry {
                Saved = new[] { "entities", "entityIds", "orderedEntities" },
                Derived = new[] { "random" },
            },
            [typeof(Entity)] = saved("id", "type", "name", "markedForDeletion", "visible", "inventory", "position", "velocity", "appearance"),
            [typeof(EntityId)] = saved("id"),
            [typeof(Pawn)] = saved("random", "speed", "nationId", "homeSettlementId", "targetEntity", "currentBehaviorType", "distanceThreshold",
                "energy", "metabolism", "relationships", "currentSettlementId", "millTarget", "hasMillTargetValue", "lastConversationTicks",
                "order", "traits", "settler"),
            [typeof(Player)] = saved("walkSpeed", "runSpeed", "currentSpeed", "renderDistance", "horizontalInput", "verticalInput",
                "jumpRequested", "yaw", "nationId", "homeSettlementId", "status", "autoWalk", "energy", "metabolism", "relationships",
                "currentSettlementId", "mealsEaten", "marketSales"),
            [typeof(Settlement)] = new Entry {
                Saved = new[] { "color", "nationId", "nationName", "currentlyPresentEntities", "market", "buildings",
                    "residentCount", "drawnLayoutSignature", "claimRadius", "siegeText" },
                // from the saved era, by EraTracker.applyRules at the end of a load
                Derived = new[] { "buildingEra" },
            },
            [typeof(Chicken)] = saved("random", "speed", "wanderTarget", "wanderTimer", "wanderInterval"),
            [typeof(Sapling)] = new Entry { Saved = new[] { "height", "plantedTick", "growTime" }, Derived = new[] { "tickCounter" } },
            [typeof(AppleTree)] = saved("height"),
            [typeof(ItemDrop)] = saved("itemStack"),
            [typeof(Rock)] = new Entry(),
            [typeof(Status)] = new Entry {
                Saved = new[] { "ticksToExpire", "status", "tickLastSet", "duplicateMessageCount" },
                Derived = new[] { "tickCounter" },
                // which messages share the line this step: the saved text is the line
                Transient = new[] { "parts", "stepping", "joinable" },
            },
            [typeof(Inventory)] = saved("slots", "toolWear"),
            [typeof(ItemSlot)] = saved("itemStack"),
            [typeof(ItemStack)] = saved("item", "quantity"),
            [typeof(Item)] = saved("type"),
            [typeof(Appearance)] = saved("parts", "label", "labelOffset", "labelScale"),
            [typeof(AppearancePart)] = saved("kind", "offset", "scale", "color"),
            [typeof(Rgba)] = saved("r", "g", "b", "a"),
            [typeof(Market)] = saved("random", "stalls", "maxNumStalls", "totalNumItemsBought", "totalNumItemsSold", "prices"),
            [typeof(MarketPrices)] = saved("pressure", "reference"),
            [typeof(Caravan)] = saved("nationId", "color", "originId", "destinationId", "state", "escortId", "playerOwned", "earnings",
                "departedTick", "facingX"),
            [typeof(TradeCaravans)] = new Entry {
                Saved = new[] { "lastDispatchTicks", "lastNotableDay", "numDispatched", "numDelivered", "numRaided", "numEscorted" },
                Derived = new[] { "entityRepository", "nationRepository", "eventProducer", "player", "timeOfDay", "needs", "realm" },
            },
            [typeof(TradeNeeds)] = new Entry {
                Derived = new[] { "nationResources", "nationRepository" },
            },
            [typeof(Stall)] = saved("ownerId", "inventory"),
            [typeof(Building)] = saved("type", "slot"),
            [typeof(NationOrder)] = saved("nationId", "itemType", "quantity", "delivered"),
            [typeof(Nation)] = new Entry {
                Saved = new[] { "random", "id", "name", "leaderId", "members", "roles", "color", "settlements" },
                // from the saved era, by EraTracker.applyRules at the end of a load
                Derived = new[] { "settlementLimit", "sheltered", "forewarned" },
            },
            [typeof(NationId)] = saved("guid"),
            [typeof(NationRepository)] = new Entry {
                Saved = new[] { "nations", "nationIds", "relations" },
                Derived = new[] { "random" },
            },
            [typeof(NationRelations)] = new Entry {
                Saved = new[] { "scores", "alliances", "denounced" },
                Derived = new[] { "eventProducer" },
            },
            [typeof(Diplomacy)] = new Entry {
                Saved = new[] { "round", "siegeProgress", "besiegers" },
                Derived = new[] { "nationRepository", "entityRepository", "eventProducer", "player", "realm" },
            },
            [typeof(PopulationGrowth)] = new Entry {
                Saved = new[] { "random", "nameGenerator", "lastBirthTicks", "lastImmigrationTicks", "lastSettlerTicks", "numSettlers",
                    "numSecessions", "lastWandererTick", "numBirths", "numImmigrants", "numWanderers" },
                Derived = new[] { "entityRepository", "nationRepository", "environment", "eventProducer", "gameConfig", "player", "realm" },
            },
            [typeof(PawnNameGenerator)] = new Entry { Saved = new[] { "generated" }, Derived = new[] { "random" } },
            [typeof(NationNameGenerator)] = new Entry { Saved = new[] { "generated" }, Derived = new[] { "random" } },
            [typeof(SettlementLife)] = saved("random"),
            [typeof(beyondnations.Environment)] = new Entry { Saved = new[] { "random", "id", "chunks" }, Derived = new[] { "entityRepository" } },
            [typeof(EnvironmentId)] = saved("guid"),
            [typeof(Chunk)] = new Entry {
                Saved = new[] { "random", "id", "size", "locations", "xpos", "zpos" },
                // from the coordinates and size
                Derived = new[] { "position", "name" },
            },
            [typeof(ChunkId)] = saved("guid"),
            [typeof(Location)] = new Entry {
                Saved = new[] { "id", "color", "entityIds" },
                // from the chunk's coordinates and location scale
                Derived = new[] { "position", "scale", "name" },
            },
            [typeof(LocationId)] = saved("guid"),
            [typeof(WorldGenerator)] = new Entry {
                Saved = new[] { "currentChunkX", "currentChunkZ" },
                Derived = new[] { "pawnNameGenerator", "random", "environment", "player", "eventProducer", "chunkSize", "locationScale",
                    "entityRepository", "gameConfig", "traitRandom", "regionMap" },
            },
            [typeof(ObjectiveTracker)] = new Entry {
                Saved = new[] { "currentIndex", "ambitionsGenerated", "currentProgress", "evaluatedOnce", "lastCompleted", "lastCompletedTick" },
                // the arc and the n-th ambition come from ObjectiveCatalog, rebuilt to the saved length
                Derived = new[] { "objectives", "ambitionFactory" },
            },
            [typeof(Objective)] = new Entry { Derived = new[] { "id", "title", "progress", "hint", "reward", "ambition" } },
            [typeof(ObjectiveReward)] = new Entry { Derived = new[] { "items" } },
            [typeof(ObjectiveProgress)] = saved("complete", "label", "current", "target", "secondLabel", "secondCurrent", "secondTarget"),
            [typeof(Chronicle)] = new Entry {
                Saved = new[] { "entries", "totalRecorded", "nationsFounded", "raidsToldToday", "raidDay" },
                Derived = new[] { "entityRepository", "nationRepository", "timeOfDay", "tickCounter", "seed", "capacity" },
            },
            [typeof(ChronicleEntry)] = saved("sequence", "eventType", "tick", "day", "partOfDay", "text", "nationIds", "entityIds"),
            [typeof(WorldEvents)] = new Entry {
                Saved = new[] { "random", "nextPlayerEventTick", "nextWorldEventTick", "numEvents", "numPlayerEvents", "lastPlayerEventId",
                    "pending", "effects" },
                // the events themselves are code, registered again as the Simulation is built
                Derived = new[] { "registry" },
            },
            [typeof(WorldEventRegistry)] = new Entry { Derived = new[] { "events" } },
            [typeof(WorldEventInstance)] = saved("eventId", "sequence", "startTick", "deadlineTick", "forPlayer", "nationId", "otherNationId",
                "settlementId", "entityId", "magnitude"),
            [typeof(WorldEffect)] = saved("kind", "nationId", "settlementId", "startTick", "endTick", "nextTick"),
            // stateless: everything about one happening is in its WorldEventInstance
            [typeof(DroughtEvent)] = new Entry(),
            [typeof(SicknessEvent)] = new Entry(),
            [typeof(WanderersEvent)] = new Entry(),
            [typeof(BanditCampEvent)] = new Entry(),
            [typeof(ClaimantEvent)] = new Entry(),
            [typeof(BountifulHarvestEvent)] = new Entry(),
            [typeof(LostTravellerEvent)] = new Entry(),
            [typeof(StorehouseFireEvent)] = new Entry(),
            [typeof(RareMerchantEvent)] = new Entry(),
            [typeof(EraTracker)] = saved("era", "reachedTicks", "outcome", "outcomeTick", "outcomeAcknowledged", "everHeldSettlement",
                "collapseSinceTick", "nationName", "peakMembers", "peakSettlements", "peakFunds", "history", "lastChronicleSequence",
                "ambitionEras"),
            [typeof(HistoryEntry)] = saved("tick", "day", "partOfDay", "text", "milestone"),
            [typeof(PawnBehaviorCalculator)] = new Entry {
                Derived = new[] { "random", "environment", "entityRepository", "nationRepository", "gameConfig", "tickCounter", "timeOfDay", "seasonCalendar" },
            },
            [typeof(PawnBehaviorExecutor)] = new Entry {
                Derived = new[] { "nationNameGenerator", "random", "environment", "nationRepository", "eventProducer", "entityRepository", "tickCounter", "seasonCalendar", "realm" },
            },
            // seasons and regions: no state of their own (docs/SAVES.md)
            [typeof(RegionMap)] = new Entry {
                Derived = new[] { "seed", "chunkLength" },
                // a memo of lookups already made: a loaded world fills it again as it asks
                Transient = new[] { "cache" },
            },
            [typeof(SeasonCalendar)] = new Entry { Derived = new[] { "timeOfDay" } },
            [typeof(SeasonalWorld)] = new Entry {
                Derived = new[] { "calendar", "regionMap", "environment", "entityRepository", "gameConfig", "player", "random" },
            },
            [typeof(WorldPalette)] = new Entry {
                Derived = new[] { "calendar", "regionMap" },
                // worked out afresh by every frame's beginFrame
                Transient = new[] { "seasonTint", "seasonAmount", "leaf" },
            },
            [typeof(NationResources)] = new Entry { Derived = new[] { "entityRepository", "regionMap" } },
            [typeof(Realm)] = new Entry {
                Saved = new[] { "lawRound", "laws", "wars", "truces", "lineage", "reignStartTick", "runOver", "notice", "noticeShown" },
                Derived = new[] { "nationRepository", "entityRepository", "eventProducer", "player", "diplomacy", "gameConfig", "tickCounter" },
            },
            [typeof(NationLaws)] = saved("nationId", "levels", "pending", "proclaimedAt", "lastTaxCollected", "totalTaxCollected", "deserters"),
            [typeof(Reign)] = saved("name", "nationName", "startTick", "endTick", "cause"),
            [typeof(SuccessionNotice)] = saved("previousName", "heirName", "nationName", "reignTicks", "reason", "lineEnded"),
            [typeof(LagPreventer)] = new Entry {
                Derived = new[] { "gameConfig", "tickCounter", "entityRepository", "environment" },
            },
        };

        /**
        * Event subclasses carry live references to whatever the event was
        * about. A save keeps only the event's type, description and date
        * (RestoredEvent), so their own fields are transient by design.
        */
        public static bool isEventPayload(Type type) {
            return typeof(Event).IsAssignableFrom(type) && type != typeof(Event);
        }

        public static bool isTransient(Type declaringType, string field) {
            if (isEventPayload(declaringType)) {
                return true;
            }
            Entry entry;
            return Manifest.TryGetValue(declaringType, out entry) && Array.IndexOf(entry.Transient, field) >= 0;
        }

        /**
        * Every Core type a Simulation can hold: the types of its fields, their
        * element types, and every Core subclass of each (so all entity and
        * event kinds count), followed transitively.
        */
        public static List<Type> reachableTypes() {
            Assembly core = typeof(Simulation).Assembly;
            List<Type> all = new List<Type>(core.GetTypes());
            List<Type> found = new List<Type>();
            HashSet<Type> seen = new HashSet<Type>();
            Queue<Type> queue = new Queue<Type>();
            queue.Enqueue(typeof(Simulation));
            while (queue.Count > 0) {
                Type type = queue.Dequeue();
                if (type == null || !seen.Add(type)) {
                    continue;
                }
                if (type.IsArray) {
                    queue.Enqueue(type.GetElementType());
                    continue;
                }
                if (type.IsGenericType) {
                    foreach (Type argument in type.GetGenericArguments()) {
                        queue.Enqueue(argument);
                    }
                }
                if (Nullable.GetUnderlyingType(type) != null) {
                    queue.Enqueue(Nullable.GetUnderlyingType(type));
                }
                if (type.Assembly != core || type.IsEnum) {
                    continue;
                }
                found.Add(type);
                foreach (Type candidate in all) {
                    if (candidate != type && type.IsAssignableFrom(candidate) && !candidate.IsInterface) {
                        queue.Enqueue(candidate);
                    }
                }
                if (type.BaseType != null) {
                    queue.Enqueue(type.BaseType);
                }
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                    queue.Enqueue(field.FieldType);
                }
            }
            return found;
        }

        public static List<string> declaredFields(Type type) {
            List<string> names = new List<string>();
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                names.Add(field.Name);
            }
            return names;
        }
    }
}
