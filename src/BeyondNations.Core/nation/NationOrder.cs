using System.Text.Json;
namespace beyondnations {

    /**
    * A leader's standing request to one member pawn: bring this many of an
    * item back to one of the nation's settlements. The pawn keeps living its
    * own life -- it gathers as it always does -- and whatever it carries of
    * the item is handed into the settlement's stockpile whenever it is inside
    * one of its nation's settlements, until the quota is met.
    *
    * The order is tied to the nation that gave it: a pawn that changes nation
    * drops it.
    */
    public class NationOrder {
        public const int DEFAULT_QUANTITY = 10;

        private readonly NationId nationId;
        private readonly ItemType itemType;
        private readonly int quantity;
        private int delivered;

        public NationOrder(NationId nationId, ItemType itemType, int quantity) {
            this.nationId = nationId;
            this.itemType = itemType;
            this.quantity = quantity;
        }

        public NationId getNationId() {
            return nationId;
        }

        public ItemType getItemType() {
            return itemType;
        }

        public int getQuantity() {
            return quantity;
        }

        public int getDelivered() {
            return delivered;
        }

        public int getRemaining() {
            return quantity - delivered;
        }

        public bool isComplete() {
            return delivered >= quantity;
        }

        public void recordDelivery(int amount) {
            delivered += amount;
        }

        /** "gather 10 wood" */
        public string describeTask() {
            return "gather " + quantity + " " + itemType.ToString().ToLower();
        }

        /** "gather 10 wood (4/10 delivered)" */
        public string describe() {
            return describeTask() + " (" + delivered + "/" + quantity + " delivered)";
        }

        // --- saving (docs/SAVES.md) ---

        internal void save(SaveWriter writer) {
            writer.write("nationId", nationId);
            writer.writeEnum("itemType", itemType);
            writer.write("quantity", quantity);
            writer.write("delivered", delivered);
        }

        internal static NationOrder load(JsonElement element, SaveReader reader) {
            NationOrder order = new NationOrder(reader.getNationId(element, "nationId"),
                SaveReader.getEnum<ItemType>(element, "itemType"), SaveReader.getInt(element, "quantity"));
            order.delivered = SaveReader.getInt(element, "delivered");
            return order;
        }
    }
}
