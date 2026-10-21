using System.Numerics;

namespace beyondnations {

    public class Rock : Entity {
        /**
        * Stone a rock gives up. Buildings cost 5-30 stone, so at 3 per rock
        * and one or two rocks a chunk, stone was the bottleneck of every
        * settlement; see WorldGenerator for the density and the regrowth.
        */
        public const int STONE_YIELD = 5;

        public Rock(Vector3 position) : base(EntityType.ROCK, "Rock") {
            setPosition(position);
            setAppearance(new Appearance(PrimitiveKind.Cube, Vector3.One, Rgba.Gray));

            getInventory().addItem(ItemType.STONE, STONE_YIELD);
        }
    }
}
