using System.Numerics;

namespace beyondnations {

    /**
    * A chunk is a 2D array of locations.
    * It is a part of the environment.
    */
    public class Chunk {
        private RandomSource random;
        private ChunkId id;
        private int size;
        private Location[,] locations;
        private Vector3 position;
        private int xpos;
        private int zpos;
        private string name;

        public Chunk(int xpos, int zpos, int size, int locationScale, RandomSource random) {
            this.random = random;
            this.id = new ChunkId();
            this.size = size;
            this.locations = new Location[size, size];
            this.xpos = xpos;
            this.zpos = zpos;
            this.name = "Chunk_" + xpos + "_" + zpos;
            this.position = calculatePosition(locationScale);
            generateLocations(locationScale);
        }

        public ChunkId getId() {
            return id;
        }

        public string getName() {
            return name;
        }

        public int getSize() {
            return size;
        }

        public int getX() {
            return xpos;
        }

        public int getZ() {
            return zpos;
        }

        public Vector3 getPosition() {
            return position;
        }

        public Location[,] getLocations() {
            return locations;
        }

        public Location getLocation(int x, int z) {
            return locations[x, z];
        }

        public int getLocationScale() {
            return locations[0, 0].getScale();
        }

        public Location getRandomLocation() {
            int x = random.range(0, size);
            int z = random.range(0, size);
            return locations[x, z];
        }

        private Vector3 calculatePosition(int locationScale) {
            int lengthOfChunk = size * locationScale;
            int x = xpos * lengthOfChunk;
            int z = zpos * lengthOfChunk;
            return new Vector3(x, 0, z);
        }

        private void generateLocations(int locationScale) {
            for (int x = 0; x < size; x++) {
                for (int z = 0; z < size; z++) {
                    locations[x, z] = new Location(xpos * size + x, zpos * size + z, locationScale, random);
                }
            }
        }
    }
}