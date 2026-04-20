using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Scripts.FSM.Base.StateMachine
{
    [CreateAssetMenu(fileName = "StateData", menuName = "Main/FSM/StateData", order = 0)]
    public class StateData : ScriptableObject
    {
        [field: SerializeField] public State State { get; private set; }

        [field: SerializeField] public List<StateCondition> StateConditions { get; private set; }
        [field: SerializeField] public List<StateData> ExitStates { get; private set; }
    }
}