using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    public class Settlement : Entity {
        private Rgba color;
        private NationId nationId;
        private string nationName;
        private List<EntityId> currentlyPresentEntities = new List<EntityId>();
        private Market market;
        private List<Building> buildings = new List<Building>();
        private int residentCount = 0;
        private long drawnLayoutSignature = long.MinValue;
        private float claimRadius = Territory.getBaseClaimRadius(SettlementTier.CAMP);
        private string siegeText = null;

        public static readonly int WOOD_COST_TO_BUILD = 50;

        public Settlement(Vector3 position, NationId nationId, Rgba color, string nationName, RandomSource random) : base(EntityType.SETTLEMENT, "Settlement") {
            this.color = color;
            this.nationId = nationId;
            this.nationName = nationName;

            setPosition(position);
            setAppearance(new Appearance());
            market = new Market(4, random);

            updateNameTag();
            refreshAppearance();
        }

        public List<EntityId> getCurrentlyPresentEntities() {
            return this.currentlyPresentEntities;
        }

        public void addCurrentlyPresentEntity(EntityId entityId) {
            this.currentlyPresentEntities.Add(entityId);
            updateNameTag();
        }

        public void removeCurrentlyPresentEntity(EntityId entityId) {
            this.currentlyPresentEntities.Remove(entityId);
            updateNameTag();
        }

        public int getCurrentlyPresentEntitiesCount() {
            return this.currentlyPresentEntities.Count;
        }

        public Rgba getColor() {
            return this.color;
        }

        public NationId getNationId() {
            return this.nationId;
        }

        public string getNationName() {
            return this.nationName;
        }

        /**
        * Hands the settlement to another nation (#176): its banner, awnings
        * and border posts take the new colour and its nametag the new name.
        * Who lives there and who owns which stall is the caller's business.
        */
        public void changeNation(Nation nation) {
            this.nationId = nation.getId();
            this.color = nation.getColor();
            this.nationName = nation.getName();
            this.siegeText = null;
            updateNameTag();
            drawnLayoutSignature = long.MinValue;
            refreshAppearance();
        }

        // territory -----------------------------------------------------------------------------------------

        /**
        * How far the settlement's claim reaches from its centre, as Territory
        * last worked it out: its tier's radius, cut back where it would run
        * into a neighbour's.
        */
        public float getClaimRadius() {
            return claimRadius;
        }

        public void setClaimRadius(float claimRadius) {
            this.claimRadius = claimRadius;
        }

        /** "Besieged by Xland (2/6)", or null when nobody is laying siege. */
        public string getSiegeText() {
            return siegeText;
        }

        public void setSiegeText(string siegeText) {
            if (this.siegeText == siegeText) {
                return;
            }
            this.siegeText = siegeText;
            updateNameTag();
        }

        public string getNameTagText() {
            return getAppearance().getLabel();
        }

        public Market getMarket() {
            return this.market;
        }

        public int getFunds() {
            return getInventory().getNumItems(ItemType.COIN);
        }

        public void addFunds(int amount) {
            getInventory().addItem(ItemType.COIN, amount);
        }

        public void removeFunds(int amount) {
            getInventory().removeItem(ItemType.COIN, amount);
        }

        // buildings -----------------------------------------------------------------------------------------

        public IReadOnlyList<Building> getBuildings() {
            return buildings;
        }

        public int getNumBuildings() {
            return buildings.Count;
        }

        public int getNumBuildings(BuildingType type) {
            int count = 0;
            foreach (Building building in buildings) {
                if (building.getType() == type) {
                    count++;
                }
            }
            return count;
        }

        /**
        * Whether another building of this type fits: under its own limit and
        * under the settlement's total. Says nothing about materials.
        */
        public bool canAddBuilding(BuildingType type) {
            return buildings.Count < BuildingCatalog.MAX_BUILDINGS
                && getNumBuildings(type) < BuildingCatalog.getLimit(type);
        }

        /**
        * Puts a building up in the next free slot and applies what it does. The
        * caller has already taken the materials. Returns false, changing
        * nothing, when there is no room for it.
        */
        public bool addBuilding(BuildingType type) {
            if (!canAddBuilding(type)) {
                return false;
            }
            buildings.Add(new Building(type, buildings.Count));
            if (type == BuildingType.STOREHOUSE) {
                market.increaseMaxNumStalls(BuildingCatalog.STOREHOUSE_STALLS);
            }
            updateNameTag();
            refreshAppearance();
            return true;
        }

        public SettlementTier getTier() {
            return SettlementTiers.fromBuildingCount(buildings.Count);
        }

        /**
        * How many pawns may call this settlement home: a base, and more for each
        * house.
        */
        public int getResidentCap() {
            return BuildingCatalog.BASE_RESIDENTS + BuildingCatalog.HOUSE_RESIDENTS * getNumBuildings(BuildingType.HOUSE);
        }

        /**
        * How many call it home, as last counted. The simulation recounts it
        * periodically (SettlementLife.countResidents); a pawn joining adds one
        * straight away so two joiners in one tick cannot both take the last
        * place.
        */
        public int getResidentCount() {
            return residentCount;
        }

        public void setResidentCount(int residentCount) {
            if (this.residentCount == residentCount) {
                return;
            }
            this.residentCount = residentCount;
            updateNameTag();
        }

        public bool hasRoomForResident() {
            return residentCount < getResidentCap();
        }

        /**
        * Each farm stocks one apple into the merchant stall holding the fewest,
        * so farms feed the market rather than piling up out of reach. With no
        * merchant there is nobody to sell the harvest and nothing is produced.
        * Returns how many apples were stocked.
        */
        public int harvestFarms() {
            int farms = getNumBuildings(BuildingType.FARM);
            int stocked = 0;
            for (int i = 0; i < farms; i++) {
                Stall emptiest = null;
                foreach (Stall stall in market.getStalls()) {
                    if (stall.getOwnerId() == null) {
                        continue;
                    }
                    if (emptiest == null || stall.getInventory().getNumItems(ItemType.APPLE) < emptiest.getInventory().getNumItems(ItemType.APPLE)) {
                        emptiest = stall;
                    }
                }
                if (emptiest == null) {
                    break;
                }
                emptiest.getInventory().addItem(ItemType.APPLE, 1);
                stocked++;
            }
            return stocked;
        }

        // drawing -------------------------------------------------------------------------------------------

        /**
        * Redraws the settlement if anything its look depends on has changed
        * since it was last drawn: stalls built, bought or stocked, buildings put
        * up. Cheap when nothing has changed, so the simulation calls it every
        * tick rather than every place a stall can change hands having to.
        */
        public void refreshAppearance() {
            long signature = SettlementLayout.computeSignature(this);
            if (signature == drawnLayoutSignature) {
                return;
            }
            drawnLayoutSignature = signature;
            SettlementLayout.build(this, getAppearance());
        }

        private void updateNameTag() {
            string label = SettlementTiers.getDisplayName(getTier()) + " of " + this.nationName + "\n";
            label += getCurrentlyPresentEntitiesCount() + " here, " + residentCount + "/" + getResidentCap() + " residents";
            if (siegeText != null) {
                label += "\n" + siegeText;
            }
            getAppearance().setLabel(label);
        }
    }
}
