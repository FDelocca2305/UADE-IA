using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.LogicGates
{
    [CreateAssetMenu(fileName = "NegateCondition", menuName = "Main/FSM/LogicGates/NEGATE")]
    public class NegateCondition : StateCondition
    {
        [SerializeField] private StateCondition condition;

        public override bool CompleteCondition(IUseFsm p_model)
        {
            return !condition.CompleteCondition(p_model);
        }
    }
}