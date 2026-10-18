using System.Numerics;

namespace beyondnations {

    /**
    * Vector helpers that preserve UnityEngine.Vector3 semantics on top of
    * System.Numerics.Vector3.
    *
    * The two structs are near enough to identical that most of the migration is
    * a rename, but normalisation is a genuine behavioural difference and not a
    * naming one:
    *
    *   - UnityEngine's `normalized` returns the zero vector when the magnitude
    *     is too small to normalise safely
    *   - System.Numerics' `Normalize` divides by that magnitude regardless and
    *     yields NaN
    *
    * Two call sites normalise a direction that can legitimately be zero: a pawn
    * standing exactly on its target, and a wander offset that lands on zero. On
    * Unity both produced a zero velocity; through System.Numerics both would
    * produce a NaN velocity and corrupt the entity's position permanently.
    *
    * Depends on nothing but the base class library, as #212 requires.
    */
    public static class VectorMath {

        /**
        * The magnitude below which a vector is treated as un-normalisable.
        * This is the same threshold UnityEngine.Vector3 uses.
        */
        public const float NormalizeEpsilon = 1e-5f;

        /**
        * Returns the unit vector in the same direction, or the zero vector if
        * the input is too small to normalise. Equivalent to Unity's
        * `Vector3.normalized`.
        */
        public static Vector3 normalized(Vector3 vector) {
            float length = vector.Length();
            if (length < NormalizeEpsilon) {
                return Vector3.Zero;
            }
            return vector / length;
        }
    }
}
