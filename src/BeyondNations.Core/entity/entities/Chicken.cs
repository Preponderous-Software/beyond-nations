using System.Numerics;

namespace beyondnations {

    public class Chicken : Entity {
        private static readonly Rgba Beige = new Rgba(1f, 0.9f, 0.8f);

        private RandomSource random;
        private int speed;
        private Vector3 wanderTarget;
        private float wanderTimer = 0f;
        private float wanderInterval = 3f;

        public Chicken(Vector3 position, RandomSource random) : base(EntityType.CHICKEN, "Chicken") {
            this.random = random;
            this.speed = random.range(5, 10);

            setPosition(position);
            setAppearance(new Appearance(PrimitiveKind.Capsule, new Vector3(0.5f, 0.5f, 0.5f), Beige));

            getInventory().addItem(ItemType.CHICKEN_MEAT, random.range(1, 3));
        }

        public int getSpeed() {
            return speed;
        }

        /**
        * Picks a new wander target every few seconds and sets a velocity toward
        * it. The integrator that applies that velocity arrives with #220; until
        * then this only decides where the chicken wants to go.
        */
        public void wander(float deltaTime) {
            wanderTimer += deltaTime;

            if (wanderTimer >= wanderInterval) {
                wanderTarget = getPosition() + new Vector3(random.range(-10f, 10f), 0, random.range(-10f, 10f));
                wanderTimer = 0f;
            }

            Vector3 direction = wanderTarget - getPosition();
            direction.Y = 0; // keep movement horizontal

            // Vertical velocity belongs to the movement integrator (#220), so
            // it is carried over here rather than overwritten.
            float verticalVelocity = getVelocity().Y;
            if (direction.Length() > 0.5f) {
                Vector3 horizontalVelocity = VectorMath.normalized(direction) * speed;
                setVelocity(new Vector3(horizontalVelocity.X, verticalVelocity, horizontalVelocity.Z));
            } else {
                setVelocity(new Vector3(0, verticalVelocity, 0));
            }
        }
    }
}
