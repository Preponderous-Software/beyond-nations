using System.Numerics;

namespace beyondnations {

    /**
    * Ground the world keeps for the player in the first minutes, so a new
    * player can do the first objectives where they stand.
    *
    *   - The grove: during the opening, pawns leave the trees and rocks within
    *     GROVE_RADIUS of the spawn alone. Pawns used to fell every tree near
    *     the spawn within about 40 game seconds, so a player who read the
    *     tips for half a minute met "Walk up to a tree" with no tree in
    *     sight (#351).
    *   - The would-be founder's ground: while the player leads a nation
    *     that has no settlement yet, in the opening, no pawn founds a
    *     settlement close enough to stop the player founding anywhere within
    *     FOUNDER_RADIUS of where they stand. Settlers otherwise filled the
    *     open ground around the player in the first in-game day (#433). It
    *     follows the player and starts only once they lead a nation: a fixed
    *     ring around the spawn, clear from the first tick, set the first
    *     nations on its rim, slowed the world's spread (fewer chunks, so
    *     fewer pawns) and left it short of shelter in its first winter.
    *   - The pointed-out site: when a refused founding names open ground
    *     (FoundSettlementCommand), no pawn founds close enough to it to shut
    *     it for HELD_SITE_SECONDS, the time it takes to walk there. Each
    *     hint used to be taken by a settler just before the player arrived,
    *     and the hint jumped 200 paces away, up to seven times (#433).
    *
    * The opening lasts OPENING_SECONDS of game time from the start of the
    * world, or until the player has a home settlement. Everything here is
    * derived from the tick count and the player, apart from the held site,
    * which lasts two minutes and is not saved.
    */
    public class ReservedGround {
        public const int OPENING_SECONDS = 600;
        public const float GROVE_RADIUS = 60f;
        public const float FOUNDER_RADIUS = 100f;
        public const int HELD_SITE_SECONDS = 120;
        /** How near the pointed-out site the player can stand and still find it open. */
        public const float HELD_SITE_RADIUS = 25f;

        /** Where every new world puts the player (Player's constructor). */
        public static readonly Vector3 Spawn = Vector3.Zero;

        private readonly Player player;
        private readonly TickCounter tickCounter;
        private Vector3? heldSite = null;
        private int heldUntilTick = 0;

        public ReservedGround(Player player, TickCounter tickCounter) {
            this.player = player;
            this.tickCounter = tickCounter;
        }

        /** Called once a tick: holds any site a refusal has just pointed out. */
        public void update() {
            Vector3? pointed = player.takePointedOutGround();
            if (pointed != null) {
                heldSite = pointed;
                heldUntilTick = tickCounter.getTotalTicks() + HELD_SITE_SECONDS * tickCounter.getTicksPerSecond();
            }
            if (heldSite != null && tickCounter.getTotalTicks() >= heldUntilTick) {
                heldSite = null;
            }
        }

        public bool isOpening() {
            return player.getHomeSettlementId() == null
                && tickCounter.getTotalTicks() < OPENING_SECONDS * tickCounter.getTicksPerSecond();
        }

        /** Whether the player is about to found: in the opening, in a nation, with no home settlement yet. */
        public bool isFounding() {
            return isOpening() && player.getNationId() != null;
        }

        /** The site being held for the player, or null. */
        public Vector3? getHeldSite() {
            return heldSite;
        }

        /** Whether pawns must leave this tree or rock alone for now: it is in the grove, during the opening. */
        public bool isGuarded(Entity entity) {
            if (entity == null || (entity.getType() != EntityType.TREE && entity.getType() != EntityType.ROCK)) {
                return false;
            }
            return isOpening() && Territory.groundDistance(entity.getPosition(), Spawn) <= GROVE_RADIUS;
        }

        /**
        * The ground distance from position to the nearest reserved ground the
        * player may want to found on; MaxValue when nothing is reserved. A pawn
        * founding here keeps a settlement at least this far from every point
        * of that ground.
        */
        public float getRoom(Vector3 position) {
            float room = float.MaxValue;
            if (isFounding()) {
                room = System.MathF.Min(room, Territory.groundDistance(position, player.getPosition()) - FOUNDER_RADIUS);
            }
            if (heldSite != null) {
                room = System.MathF.Min(room, Territory.groundDistance(position, heldSite.Value) - HELD_SITE_RADIUS);
            }
            return room;
        }

        /** Whether a pawn may found a settlement here: far enough from the reserved ground that the player still could. */
        public bool allowsPawnFounding(Vector3 position, float minDistance) {
            return getRoom(position) > minDistance;
        }
    }
}
