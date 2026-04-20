using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    public abstract class StateCondition : ScriptableObject
    {
        public abstract bool CompleteCondition(IUseFsm p_model);
    }
}