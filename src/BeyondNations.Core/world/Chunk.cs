using System.Text.Json;
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

        // --- saving (docs/SAVES.md) ---

        /**
        * A chunk is its coordinates and size, from which its position, name
        * and every location's position, scale and name follow; and the parts
        * that were rolled or assigned: its id, each location's id and colour.
        */
        internal void save(SaveWriter writer) {
            writer.write("id", id.getGuid());
            writer.writeStream("random", random);
            writer.write("x", xpos);
            writer.write("z", zpos);
            writer.write("size", size);
            writer.write("locationScale", getLocationScale());
            writer.beginArray("locations");
            for (int x = 0; x < size; x++) {
                for (int z = 0; z < size; z++) {
                    writer.beginObject();
                    locations[x, z].save(writer);
                    writer.endObject();
                }
            }
            writer.endArray();
        }

        internal static Chunk load(JsonElement element, SaveReader reader) {
            int size = SaveReader.getInt(element, "size");
            if (size <= 0 || size > 1024) {
                throw new SaveFormatException("a chunk of size " + size);
            }
            RandomSource random = reader.stream(SaveReader.property(element, "random"));
            // The constructor rolls each location's colour; those draws come
            // off a scratch stream, and the saved colours replace them.
            Chunk chunk = new Chunk(SaveReader.getInt(element, "x"), SaveReader.getInt(element, "z"), size,
                SaveReader.getInt(element, "locationScale"), new RandomSource(0));
            chunk.random = random;
            chunk.id = new ChunkId(SaveReader.getGuid(element, "id"));
            JsonElement saved = SaveReader.property(element, "locations");
            if (saved.GetArrayLength() != size * size) {
                throw new SaveFormatException("a chunk with " + saved.GetArrayLength() + " locations, not " + (size * size));
            }
            int index = 0;
            for (int x = 0; x < size; x++) {
                for (int z = 0; z < size; z++) {
                    chunk.locations[x, z].load(saved[index++], reader);
                }
            }
            return chunk;
        }
    }
}
