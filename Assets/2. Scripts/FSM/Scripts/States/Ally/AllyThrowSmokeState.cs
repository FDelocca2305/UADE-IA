using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "AllyThrowSmokeState", menuName = "Main/FSM/Ally States/Throw Smoke")]
    public class AllyThrowSmokeState : State
    {
        [SerializeField] private float dwellTime = 0.25f;
        private bool m_smokeDeployed;

        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Ally ally)
            {
                ally.StateTimer = 0f;
                m_smokeDeployed = TryDeploySmoke(ally, ally.transform.position);
                ally.StopMovement();
                MyLogger.LogDebug($"Ally {ally.name}: Enter ThrowSmoke (deployed={m_smokeDeployed})");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is not Ally ally) return;

            ally.StateTimer += Time.deltaTime;
            if (ally.StateTimer >= dwellTime)
            {
                // Nothing else to do; exit condition should move us on.
                return;
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Ally ally)
            {
                MyLogger.LogDebug($"Ally {ally.name}: Exit ThrowSmoke");
            }
        }

        private bool TryDeploySmoke(Ally ally, Vector3 position)
        {
            var data = ally.AllyData;
            if (data == null) return false;

            float cooldown = data.smokeCooldown;
            if (Time.time < ally.LastSmokeTime + cooldown)
                return false;

            ally.LastSmokeTime = Time.time;

            var prefab = data.smokePrefab;
            float lifetime = data.smokeLifetime;
            float scale = data.smokeScale;
            string obstacleLayerName = data.smokeObstacleLayerName;

            int obstacleLayer = LayerMask.NameToLayer(obstacleLayerName);
            if (ally.SmokeInstance != null)
            {
                Object.Destroy(ally.SmokeInstance);
            }

            GameObject smokeInstance;
            if (prefab != null)
            {
                smokeInstance = Object.Instantiate(prefab, position, Quaternion.identity);
                smokeInstance.transform.localScale *= scale;
            }
            else
            {
                smokeInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                smokeInstance.transform.position = position;
                smokeInstance.transform.localScale = Vector3.one * scale;

                var renderer = smokeInstance.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
            }

            smokeInstance.layer = obstacleLayer;

            var collider = smokeInstance.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = false;
                var selfColliders = ally.GetComponentsInChildren<Collider>();
                foreach (var selfCol in selfColliders)
                {
                    if (selfCol != null && selfCol != collider)
                    {
                        Physics.IgnoreCollision(collider, selfCol, true);
                    }
                }
            }

            ally.SmokeInstance = smokeInstance;
            Object.Destroy(smokeInstance, lifetime);
            return true;
        }
    }
}
