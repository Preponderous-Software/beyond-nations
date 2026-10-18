using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    public class Settlement : Entity {
        private Rgba color;
        private NationId nationId;
        private string nationName;
        private List<EntityId> currentlyPresentEntities = new List<EntityId>();
        private Market market;

        public static readonly int WOOD_COST_TO_BUILD = 100;

        public Settlement(Vector3 position, NationId nationId, Rgba color, string nationName, RandomSource random) : base(EntityType.SETTLEMENT, "Settlement") {
            this.color = color;
            this.nationId = nationId;
            this.nationName = nationName;

            setPosition(position);
            setAppearance(new Appearance(PrimitiveKind.Cylinder, new Vector3(10, 5, 10), color));

            updateNameTagWithCurrentlyPresentEntities();
            market = new Market(4, random);
        }

        public List<EntityId> getCurrentlyPresentEntities() {
            return this.currentlyPresentEntities;
        }

        public void addCurrentlyPresentEntity(EntityId entityId) {
            this.currentlyPresentEntities.Add(entityId);
            updateNameTagWithCurrentlyPresentEntities();
        }

        public void removeCurrentlyPresentEntity(EntityId entityId) {
            this.currentlyPresentEntities.Remove(entityId);
            updateNameTagWithCurrentlyPresentEntities();
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

        private void updateNameTagWithCurrentlyPresentEntities() {
            string label = "Settlement of " + this.nationName + "\n\n";
            label += "(" + getCurrentlyPresentEntitiesCount() + ")";
            getAppearance().setLabel(label);
        }
    }
}
