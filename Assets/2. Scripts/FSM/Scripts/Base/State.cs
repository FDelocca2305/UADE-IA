using Scripts.FSM.Models;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    public abstract class State : ScriptableObject
    {
        [field: SerializeField] public string StateName { get; private set; }

        public abstract void EnterState(IUseFsm p_model);

        public abstract void ExecuteState(IUseFsm p_model);

        public abstract void ExitState(IUseFsm p_model);
    }
}