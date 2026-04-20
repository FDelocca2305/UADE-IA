using Scripts.FSM.Models;
using UnityEngine;
using Game.AI.Steering;
using UnityEngine.ProBuilder.Shapes;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "CivilianFleeState", menuName = "Main/FSM/Civilian States/Flee State")]
    public class CivilianFleeState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Civilian civilian)
            {
                civilian.SafeTimer = 0f;
                civilian.SetCurrentMaxSpeed(civilian.FleeSpeed);

                // 1. LIMPIAR RUTA VIEJA (Esto arregla tu bug)
                // Fuerza a que RecomputeFleePathIfNeeded crea que no hay camino
                civilian.ClearFleePath();

                // A*: asegurar init (no hace alloc si ya estaba)
                civilian.EnsureFleePathfindingInitialized();

                //Debug.Log($"------------------------- Civilian scaping using A* -------------------------------");

                if (civilian.EnableDebugLogs)
                    MyLogger.LogInfo($"Civilian {civilian.name}: Entered Flee State - Fleeing at speed {civilian.FleeSpeed:F1}");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is Civilian civilian)
            {
                PerformFleeMovement(civilian);
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Civilian civilian && civilian.EnableDebugLogs)
            {
                MyLogger.LogInfo($"Civilian {civilian.name}: Exited Flee State");
            }
        }
        private void PerformFleeMovement(Civilian civilian)
        {
            // 1) Intentar A* por nodos
            if (civilian.HasFleeGraph)
            {
                civilian.RecomputeFleePathIfNeeded(Time.time);

                if (civilian.HasFleePath)
                {
                    var steering = civilian.TickFleePathSteering();

                    // USAR EL METODO ESPECIALIZADO
                    civilian.ApplySteeringFleeOriginal(steering); // <- CAMBIO AQUI
                    //civilian.ApplySteering(steering);
                    if (civilian.FleePathReachedEnd())
                    {
                        civilian.SafeTimer += Time.deltaTime;
                    }
                    else
                    {
                        civilian.SafeTimer = 0f;
                    }

                    return;
                }
            }

            // 2) Fallback: huida directa
            if (civilian.Player != null)
            {
                Vector3 steering = Steering.Flee(
                    civilian.transform.position,
                    civilian.Player.position,
                    civilian.CurrentVelocity,
                    civilian.FleeSpeed
                );
                civilian.ApplySteeringDebug(steering); // Fallback sin obstacle avoidance
            }
        }

    }
}