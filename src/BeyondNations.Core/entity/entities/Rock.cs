using System.Numerics;

namespace beyondnations {

    public class Rock : Entity {

        public Rock(Vector3 position) : base(EntityType.ROCK, "Rock") {
            setPosition(position);
            setAppearance(new Appearance(PrimitiveKind.Cube, Vector3.One, Rgba.Gray));

            getInventory().addItem(ItemType.STONE, 1);
        }
    }
}
