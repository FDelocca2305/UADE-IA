using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "LeaderIdleState", menuName = "Main/FSM/Leader States/Idle")]
    public class LeaderIdleState : State
    {
        public override void EnterState(IUseFsm p_model)
        {
            // No-op idle for leader coordination FSM
        }

        public override void ExecuteState(IUseFsm p_model)
        {
            // Leader waits for requests; transition driven by conditions on StateData
        }

        public override void ExitState(IUseFsm p_model)
        {
            // Nothing to clean up
        }
    }
}
