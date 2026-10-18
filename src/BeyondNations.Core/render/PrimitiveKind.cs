namespace beyondnations {

    /**
    * The shapes the simulation knows how to be drawn as.
    *
    * Everything in Beyond Nations is a coloured primitive, so this list is the
    * whole vocabulary: ground tiles, rocks and tree leaves are cubes, trunks and
    * settlements are cylinders, pawns and chickens are capsules, and item drops
    * are spheres.
    *
    * This is deliberately not UnityEngine.PrimitiveType. It is a request for a
    * shape, not a handle on one, and carries no graphics resource with it.
    */
    public enum PrimitiveKind {
        Cube,
        Capsule,
        Cylinder,
        Sphere
    }
}
