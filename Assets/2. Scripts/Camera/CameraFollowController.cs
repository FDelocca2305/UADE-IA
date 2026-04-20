using UnityEngine;

public class CameraFollowController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private CameraFollowDataSO cameraData;

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Collision Avoidance")]
    [SerializeField] private LayerMask wallMask = ~0;
    [SerializeField] private float sphereRadius = 0.25f;
    [SerializeField] private float collisionPadding = 0.08f;
    [SerializeField] private float rayOriginHeight = 1.2f;

    [Header("Confiner por MÚLTIPLES colliders")]
    [SerializeField] private Collider[] confineVolumes;
    [SerializeField] private float confinePadding = 0.05f;
    [SerializeField] private bool preferVolumeThatContainsTarget = true;

    private Vector3 velocity = Vector3.zero;
    private Vector3 previousVelocity = Vector3.zero;
    private bool isFirstFrame = true;

    void Start()
    {
        if (!target || !cameraData) return;
        transform.rotation = Quaternion.Euler(cameraData.fixedRotationAngles);
        transform.position = ComputeDesiredCameraPosition();
    }

    void LateUpdate()
    {
        if (!target || !cameraData) return;
        UpdatePosition();
        UpdateRotation();
    }

    Vector3 ComputeDesiredCameraPosition()
    {
        // Posición ideal por offset + rot fija
        Quaternion camRot = Quaternion.Euler(cameraData.fixedRotationAngles);
        Vector3 forward = camRot * Vector3.forward;
        float distance = Mathf.Abs(cameraData.offset.z);
        Vector3 ideal = target.position - forward * distance;

        // 1) Evitar atravesar paredes
        Vector3 origin = target.position + Vector3.up * rayOriginHeight;
        Vector3 dir = ideal - origin;
        float dist = dir.magnitude;
        if (dist > 0.0001f)
        {
            dir /= dist;
            if (Physics.SphereCast(origin, sphereRadius, dir, out var hit, dist, wallMask, QueryTriggerInteraction.Ignore))
                ideal = hit.point + hit.normal * collisionPadding;
        }

        // 2) Confinamiento dentro de varios colliders (unión) o “sala actual”
        if (confineVolumes != null && confineVolumes.Length > 0)
            ideal = ClampToVolumes(ideal, confineVolumes, confinePadding, preferVolumeThatContainsTarget ? target.position : (Vector3?)null);

        return ideal;
    }

    void UpdatePosition()
    {
        Vector3 desired = ComputeDesiredCameraPosition();

        Vector3 newPosition = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref velocity,
            cameraData.smoothTime,
            cameraData.maxFollowSpeed,
            Time.deltaTime
        );

        if (!isFirstFrame)
        {
            Vector3 acceleration = (velocity - previousVelocity) / Mathf.Max(Time.deltaTime, 0.0001f);
            if (acceleration.magnitude > cameraData.maxAcceleration)
            {
                acceleration = acceleration.normalized * cameraData.maxAcceleration;
                velocity = previousVelocity + acceleration * Time.deltaTime;
                newPosition = transform.position + velocity * Time.deltaTime;
            }
        }

        previousVelocity = velocity;
        isFirstFrame = false;

        float distanceFromDesired = Vector3.Distance(newPosition, desired);
        if (distanceFromDesired > cameraData.maxDistanceFromIdeal)
        {
            Vector3 dirToDesired = (desired - newPosition).normalized;
            newPosition = desired - dirToDesired * cameraData.maxDistanceFromIdeal;
            velocity = (newPosition - transform.position) / Mathf.Max(Time.deltaTime, 0.0001f);
        }

        transform.position = newPosition;
    }

    void UpdateRotation()
    {
        transform.rotation = Quaternion.Euler(cameraData.fixedRotationAngles);
    }

    public void SnapToTarget()
    {
        if (!target || !cameraData) return;
        velocity = Vector3.zero;
        previousVelocity = Vector3.zero;
        isFirstFrame = true;

        transform.position = ComputeDesiredCameraPosition();
        transform.rotation = Quaternion.Euler(cameraData.fixedRotationAngles);
    }

    // ================== Confinamiento ==================

    static bool IsInside(Collider col, Vector3 p)
    {
        // ClosestPoint devuelve p si está dentro (o muy cerca de la superficie)
        return (col.ClosestPoint(p) - p).sqrMagnitude < 1e-6f;
    }

    static Vector3 ClampToVolumes(Vector3 pos, Collider[] volumes, float padding, Vector3? preferPoint)
    {
        // 1) Si pedimos “sala actual”, intenta usar el volumen que contiene al target
        if (preferPoint.HasValue)
        {
            for (int i = 0; i < volumes.Length; i++)
            {
                var c = volumes[i];
                if (!c) continue;
                if (IsInside(c, preferPoint.Value))
                    return ClampToSingleVolume(pos, c, padding);
            }
        }

        // 2) Unión de volúmenes: si está dentro de cualquiera, lo dejamos
        for (int i = 0; i < volumes.Length; i++)
        {
            var c = volumes[i];
            if (!c) continue;
            if (IsInside(c, pos)) return pos;
        }

        // 3) Si está fuera de todos, elegimos el punto más cercano entre todos
        float best = float.PositiveInfinity;
        Vector3 bestPoint = pos;
        Collider bestCol = null;

        for (int i = 0; i < volumes.Length; i++)
        {
            var c = volumes[i];
            if (!c) continue;
            Vector3 cp = c.ClosestPoint(pos);
            float d = (cp - pos).sqrMagnitude;
            if (d < best) { best = d; bestPoint = cp; bestCol = c; }
        }

        if (bestCol)
        {
            // Empujamos un poquito hacia adentro del volumen elegido
            Vector3 inwardDir = (bestPoint - pos); // de fuera -> superficie
            if (inwardDir.sqrMagnitude > 1e-6f) bestPoint += inwardDir.normalized * padding;
            // Mantener altura de cámara
            bestPoint.y = pos.y;
        }

        return bestPoint;
    }

    static Vector3 ClampToSingleVolume(Vector3 pos, Collider col, float padding)
    {
        // Si ya está dentro, podemos devolver tal cual (o empujar hacia adentro si querés padding interno)
        if (IsInside(col, pos)) return pos;

        Vector3 cp = col.ClosestPoint(pos);
        Vector3 inwardDir = (cp - pos);
        if (inwardDir.sqrMagnitude > 1e-6f) cp += inwardDir.normalized * padding;
        cp.y = pos.y;
        return cp;
    }
}
