using System.Numerics;

namespace beyondnations {

    /**
    * Replaces Rigidbody for #220. Unity's physics engine used to own gravity,
    * ground contact and the jump impulse; none of that survives the move away
    * from Unity, so this is the whole of what replaces it.
    *
    * The ground is a flat plane of cube tiles with no slope (see #220), so a
    * single scalar height per entity is enough to describe "resting on the
    * ground" -- there is no terrain to raycast against. Callers pass that
    * height in rather than the integrator guessing it, because it differs per
    * entity: it is however high off the tile surface that entity's collider
    * used to sit in Unity (a chicken's capsule is shorter than a pawn's, whose
    * is shorter than the player's).
    *
    * Integration is semi-implicit Euler: velocity is updated by gravity first,
    * then position is advanced by the updated velocity. It is the same
    * integration order Unity's own Rigidbody uses, and it is what keeps a
    * dropped entity's fall from gaining energy over many steps the way plain
    * (explicit) Euler can.
    *
    * No physics engine -- BepuPhysics v2 or otherwise -- is introduced here, by
    * design (#220).
    */
    public static class MovementIntegrator {

        /**
        * Matches UnityEngine.Physics.gravity.y, the default every Rigidbody in
        * the Unity build fell under.
        */
        public const float Gravity = -9.81f;

        /**
        * Matches the historical `AddForce(Vector3.up * 10, ForceMode.Impulse)`.
        * Impulse mode adds force/mass directly to velocity, and the Rigidbodies
        * this replaces used Unity's default mass of 1, so the impulse and this
        * velocity are the same number.
        */
        public const float JumpSpeed = 10f;

        /**
        * Advances one entity by one fixed step: gravity accumulates into
        * vertical velocity, velocity moves the position, and the position is
        * then clamped so the entity never ends up below the tile surface.
        * Horizontal velocity (X/Z) is left exactly as the caller set it --
        * this function only ever touches Y.
        *
        * groundHeight is the Y this entity rests at when standing on the tile
        * plane (see class remarks). Landing zeroes vertical velocity, the same
        * way a Rigidbody's velocity stops accumulating once a collider is
        * resting on the ground.
        */
        public static void step(Entity entity, float deltaTime, float groundHeight) {
            Vector3 velocity = entity.getVelocity();
            velocity.Y += Gravity * deltaTime;

            Vector3 position = entity.getPosition();
            position += velocity * deltaTime;

            if (position.Y <= groundHeight) {
                position.Y = groundHeight;
                if (velocity.Y < 0f) {
                    velocity.Y = 0f;
                }
            }

            entity.setPosition(position);
            entity.setVelocity(velocity);
        }

        /**
        * Applies the jump impulse. Callers are expected to have already
        * checked groundedness -- this function does not, so that it stays
        * usable in tests without needing a real ground check.
        */
        public static void jump(Entity entity) {
            Vector3 velocity = entity.getVelocity();
            velocity.Y = JumpSpeed;
            entity.setVelocity(velocity);
        }
    }
}
