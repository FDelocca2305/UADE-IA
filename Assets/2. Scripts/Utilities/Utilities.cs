using UnityEngine;

public static class Utilities
{
    public static Vector3 NoY(this Vector3 v3)
    {
        v3.y = 0;
        return v3;
    }
}
