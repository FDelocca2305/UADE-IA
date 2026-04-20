using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "GuardDetainState", menuName = "Main/FSM/Guard States/Detain State")]
    public class GuardDetainState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                guard.StateTimer = 0f;
                guard.ClearLeaderOverride();
                Debug.Log($"[DetainState] {guard.name} entered Detain state");
            }
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                if (guard.IsKnockedOut)
                {
                    ProcessKnockout(guard);
                }
                else if (guard.IsDetaining)
                {
                    ProcessDetain(guard);
                }
            }
        }

        public override void ExitState(IUseFsm p_model)
        {
            if (p_model is Guard guard)
            {
                guard.StopDetaining();
            }
        }

        private void ProcessKnockout(Guard guard)
        {
            guard.StateTimer += Time.deltaTime;
            if (guard.StateTimer >= guard.KnockoutRecoverTime)
            {
                guard.RecoverFromKnockout();
                Debug.Log($"[DetainState] {guard.name} recovered from knockout");
            }
        }

        private void ProcessDetain(Guard guard)
        {
            if (guard.DetainTarget == null)
            {
                guard.StopDetaining();
                return;
            }

            guard.StateTimer += Time.deltaTime;
            guard.DetainTarget.TryGetComponent(out BaseCharacter targetCharacter);

            Vector3 direction = guard.DetainTarget.position - guard.transform.position;
            direction.y = 0f;
            float distance = direction.magnitude;

            if (distance > guard.DetainRange)
            {
                float speed = guard.ChaseSpeed * guard.DetainMoveSpeedFactor;
                Vector3 steering = Game.AI.Steering.Steering.Arrive(
                    guard.transform.position,
                    guard.DetainTarget.position,
                    guard.CurrentVelocity,
                    speed,
                    guard.SlowingDistance);
                guard.ApplySteering(steering);
            }
            else
            {
                guard.FaceDirection(direction, guard.BaseRotationSpeed * 2f);
                guard.ApplyDetainDamage();
                guard.StopDetaining();
            }
        }
    }
}
