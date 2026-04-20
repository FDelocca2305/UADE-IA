using UnityEngine;

public class PathFollowerAgent
{
    // Tunings
    public float waypointReachDist = 0.6f;   // subi un poco el reach (0.6–0.8 ayuda mucho)
    public float slowingDistance = 1.2f;   // 1.0–1.5 tipico
    public int stuckFrameThreshold = 60;//20;   // frames sin mejorar -> avanzar de todas formas
    public float progressEpsilon = 0.001f; // tolerancia para detectar “no mejora”

    // Internos
    int _len = 0, _cursor = 0;
    float _lastSqDist = float.PositiveInfinity;
    int _stuckFrames = 0;

    public int CurrentIndex => _cursor;  // para debug
    public void ReseedCursor(int idx)
    {
        _cursor = Mathf.Clamp(idx, 0, Mathf.Max(0, _len - 1));
        _lastSqDist = float.PositiveInfinity;
        _stuckFrames = 0;
    }

    public int BuildWorldPath(GraphAsset g, int[] idxPath, int idxLen, Vector3[] worldOut)
    {
        _len = idxLen;
        _cursor = 0;
        _lastSqDist = float.PositiveInfinity;
        _stuckFrames = 0;

        for (int i = 0; i < idxLen; i++)
            worldOut[i] = g.nodePositions[idxPath[i]];

        return _len;
    }

    public Vector3 Tick(Vector3 pos, Vector3 vel, float maxSpeed, Vector3[] worldPath)
    {
        if (_len <= 0 || _cursor >= _len) return Vector3.zero;

        Vector3 target = worldPath[_cursor];
        Vector3 toWp = target - pos;
        toWp.y = 0f;
        float sqDist = toWp.sqrMagnitude;

        // REACH DINAMICO: mas tolerante si vamos rapido, mas estricto si vamos lento
        float speedFactor = Mathf.Clamp01(vel.magnitude / maxSpeed);
        float dynamicReach = waypointReachDist * (1.0f + speedFactor * 0.5f); // 1.0x a 1.5x reach
        float reachR2 = dynamicReach * dynamicReach;

        // 1) ¿Llegue al waypoint?
        if (sqDist <= reachR2)
        {
            if (_cursor < _len - 1)
            {
                _cursor++;
                _lastSqDist = float.PositiveInfinity;
                _stuckFrames = 0;
                target = worldPath[_cursor];
                toWp = (target - pos);
                toWp.y = 0f;
                sqDist = toWp.sqrMagnitude;
                MyLogger.LogInfo($"PathFollower: ok Reached WP {_cursor - 1}, moving to {_cursor}");
            }
            else
            {
                return Vector3.zero;
            }
        }

        // 2) Overshoot ULTRA conservador
        // Solo si estamos MUY cerca Y claramente pasamos de largo
        float overshootDist = dynamicReach * 3.0f; // 3x reach
        float overshootSqDist = overshootDist * overshootDist;

        if (_cursor < _len - 1 &&
            sqDist < overshootSqDist &&
            vel.sqrMagnitude > 1.0f && // Velocidad significativa
            Vector3.Dot(toWp, vel) < -0.5f) // MUY claramente hacia atras
        {
            _cursor++;
            _lastSqDist = float.PositiveInfinity;
            _stuckFrames = 0;
            target = worldPath[_cursor];
            toWp = (target - pos);
            toWp.y = 0f;
            sqDist = toWp.sqrMagnitude;
            MyLogger.LogInfo($"PathFollower: Overshot WP {_cursor - 1}, moving to {_cursor}");
        }

        // 3) Anti-stuck MUY tolerante
        if (sqDist > _lastSqDist - progressEpsilon)
        {
            _stuckFrames++;

            // Solo avanzar si REALMENTE estamos atorados (velocidad casi 0)
            bool reallyStuck = vel.magnitude < maxSpeed * 0.1f; // < 10% velocidad

            if (reallyStuck && _stuckFrames >= stuckFrameThreshold && _cursor < _len - 1)
            {
                _cursor++;
                _lastSqDist = float.PositiveInfinity;
                _stuckFrames = 0;
                target = worldPath[_cursor];
                toWp = (target - pos);
                toWp.y = 0f;
                sqDist = toWp.sqrMagnitude;
                MyLogger.LogWarning($"PathFollower: STUCK {_stuckFrames} frames, forcing advance to {_cursor}");
            }
        }
        else
        {
            _stuckFrames = 0;
            _lastSqDist = sqDist;
        }

        // 4) Steering
        Vector3 steer;
        float slowR2 = slowingDistance * slowingDistance;

        if (sqDist > slowR2)
        {
            steer = Game.AI.Steering.Steering.Seek(pos, target, vel, maxSpeed);
        }
        else
        {
            steer = Game.AI.Steering.Steering.Arrive(pos, target, vel, maxSpeed, slowingDistance);
        }

        steer.y = 0f;
        return steer;
    }

    public bool ReachedEnd => _cursor >= _len - 1;
}
