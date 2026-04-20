using UnityEngine;

namespace FlockingSystem
{
    /// <summary>
    /// Extension methods for Vector3 to support flocking calculations.
    /// </summary>
    public static class Vector3Extensions
    {
        /// <summary>
        /// Returns a copy of the vector with Y component set to zero.
        /// Useful for 2D flocking on the XZ plane.
        /// </summary>
        public static Vector3 NoY(this Vector3 vector)
        {
            return new Vector3(vector.x, 0f, vector.z);
        }

        /// <summary>
        /// Returns a copy of the vector with Y component set to a specific value.
        /// </summary>
        public static Vector3 WithY(this Vector3 vector, float y)
        {
            return new Vector3(vector.x, y, vector.z);
        }
    }
}