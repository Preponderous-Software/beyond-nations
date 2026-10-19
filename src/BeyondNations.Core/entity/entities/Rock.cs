using System.Numerics;

namespace beyondnations {

    public class Rock : Entity {
        /** Stone a rock gives up. Rocks are scarce (one or two per chunk). */
        public const int STONE_YIELD = 3;

        public Rock(Vector3 position) : base(EntityType.ROCK, "Rock") {
            setPosition(position);
            setAppearance(new Appearance(PrimitiveKind.Cube, Vector3.One, Rgba.Gray));

            getInventory().addItem(ItemType.STONE, STONE_YIELD);
        }
    }
}
