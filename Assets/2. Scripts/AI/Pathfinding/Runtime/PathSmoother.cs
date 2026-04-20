using UnityEngine;

/// <summary>
/// Algoritmo de String Pulling para suavizar caminos.
/// Elimina waypoints innecesarios verificando línea de visión.
/// </summary>
public static class PathSmoother
{
    /// <summary>
    /// Suaviza el camino intentando conectar nodos distantes si hay visibilidad.
    /// </summary>
    public static int SmoothPath(
        Vector3[] originalPath,
        int pathLength,
        Vector3[] smoothedPath,
        Vector3 startPos,
        LayerMask obstacleMask,
        float checkRadius = 0.3f)
    {
        if (pathLength < 2)
        {
            if (pathLength == 1) smoothedPath[0] = originalPath[0];
            return pathLength;
        }

        int smoothedCount = 0;

        // 1. El primer punto siempre es donde estamos o el inicio del path
        // PathFollower maneja el movimiento, asi que mantenemos el primer nodo del path
        smoothedPath[smoothedCount++] = originalPath[0];

        int currentIdx = 0; // Indice del nodo en originalPath desde el que estamos mirando

        // Mientras no lleguemos al final
        while (currentIdx < pathLength - 1)
        {
            // Buscamos el nodo mas lejano visible
            int farthestVisible = currentIdx + 1;

            // Iteramos desde el final hacia el actual para encontrar el salto mas grande posible
            // Limitamos la busqueda a 5 nodos adelante para no hacer demasiados raycasts en mobile
            int maxLookAhead = Mathf.Min(pathLength - 1, currentIdx + 5);

            for (int testIdx = maxLookAhead; testIdx > currentIdx + 1; testIdx--)
            {
                Vector3 from = smoothedPath[smoothedCount - 1];
                Vector3 to = originalPath[testIdx];

                if (HasLineOfSight(from, to, obstacleMask, checkRadius))
                {
                    farthestVisible = testIdx;
                    break;
                }
            }

            // Agregamos ese nodo al path suavizado
            smoothedPath[smoothedCount++] = originalPath[farthestVisible];
            currentIdx = farthestVisible;
        }

        return smoothedCount;
    }

    /// <summary>
    /// Versión ligera: Solo chequea saltando nodos (1, 3, 5...)
    /// Ideal para Mobile gama baja.
    /// </summary>
    public static int SmoothPathSimple(
        Vector3[] originalPath,
        int pathLength,
        Vector3[] smoothedPath,
        Vector3 startPos,
        LayerMask obstacleMask,
        float checkRadius = 0.3f,
        int skipStride = 2)
    {
        if (pathLength <= 2)
        {
            for (int k = 0; k < pathLength; k++) smoothedPath[k] = originalPath[k];
            return pathLength;
        }

        int smoothedCount = 0;
        smoothedPath[smoothedCount++] = originalPath[0];

        int i = 0;
        while (i < pathLength - 1)
        {
            // Intentar saltar N waypoints
            int targetIdx = Mathf.Min(i + skipStride, pathLength - 1);

            // Si ya es el siguiente, no gastar raycast
            if (targetIdx == i + 1)
            {
                smoothedPath[smoothedCount++] = originalPath[targetIdx];
                i = targetIdx;
                continue;
            }

            Vector3 from = smoothedPath[smoothedCount - 1];
            Vector3 to = originalPath[targetIdx];

            if (HasLineOfSight(from, to, obstacleMask, checkRadius))
            {
                smoothedPath[smoothedCount++] = originalPath[targetIdx];
                i = targetIdx;
            }
            else
            {
                // Si falla, avanzamos solo uno
                i++;
                smoothedPath[smoothedCount++] = originalPath[i];
            }
        }

        return smoothedCount;
    }

    private static bool HasLineOfSight(Vector3 from, Vector3 to, LayerMask obstacleMask, float radius)
    {
        // Forzar plano XZ
        from.y = 0f; to.y = 0f;
        Vector3 dir = to - from;
        float dist = dir.magnitude;

        if (dist < 0.01f) return true;

        // Levantamos un poco el origen para no chocar con el suelo
        Vector3 origin = from + Vector3.up * 0.5f;

        // SphereCast es mejor que Raycast para que el NPC no corte esquinas muy pegado a la pared
        return !Physics.SphereCast(origin, radius, dir.normalized, out _, dist, obstacleMask, QueryTriggerInteraction.Ignore);
    }
}