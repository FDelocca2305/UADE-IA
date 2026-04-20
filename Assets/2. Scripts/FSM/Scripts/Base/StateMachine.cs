using System;
using System.Collections.Generic;
using System.Linq;
using Scripts.FSM.Models;
using UnityEngine.Assertions;

namespace Scripts.FSM.Base.StateMachine
{
    public class StateMachine
    {
        private List<StateData> m_allStatesData;
        private StateData m_currentState;
        private int m_currentStateConditionsAmount;

        private readonly Dictionary<Type, StateData> m_statesDictionary = new();
        private IUseFsm m_model;
        
        public StateMachine(List<StateData>  p_allStatesData)
        {
            m_allStatesData = p_allStatesData;
        }

        public StateMachine(List<StateData>  p_allStatesData, IUseFsm p_model)
        {
            m_allStatesData = p_allStatesData;
            InitializeStateMachine(p_model);
        }

        public void InitializeStateMachine(IUseFsm p_model)
        {
            m_model = p_model;

            Assert.IsNotNull(m_allStatesData);

            InitializedStatesCheck(m_allStatesData);
            InitializeStates(m_allStatesData);

            m_currentState = m_allStatesData[0];
            m_currentState.State.EnterState(m_model);
            m_currentStateConditionsAmount = m_currentState.StateConditions.Count;
        }

        private void InitializeStates(IReadOnlyList<StateData> p_enemyStatesData)
        {
            var l_statesCount = p_enemyStatesData.Count;

            for (var l_i = 0; l_i < l_statesCount; l_i++)
            {
                var l_type = p_enemyStatesData[l_i].State.GetType();

                if (m_statesDictionary.ContainsKey(l_type))
                    continue;

                m_statesDictionary.Add(l_type, p_enemyStatesData[l_i]);
            }
        }

        #region InitializationCheck

        private static void InitializedStatesCheck(IReadOnlyList<StateData> p_enemyStatesData)
        {
            if (p_enemyStatesData.Count < 1)
            {
                throw new Exception($"FSM {p_enemyStatesData} has no states assigned");
            }

            for (var l_i = 0; l_i < p_enemyStatesData.Count; l_i++)
            {
                var l_currState = p_enemyStatesData[l_i];

                if (l_currState == null)
                {
                    throw new Exception($"State in position {l_i} is null");
                }

                if (l_currState.ExitStates.Count != l_currState.StateConditions.Count)
                {
                    throw new Exception($"State {l_currState} doesn't have the same amount of exits and conditions");
                }

                if (l_currState.ExitStates.Any(p_exitState => p_exitState == null))
                {
                    throw new Exception($"State {l_currState} has an invalid exit state");
                }

                if (l_currState.StateConditions.Any(p_condition => p_condition == null))
                {
                    throw new Exception($"State {l_currState} has an invalid exit condition");
                }
            }
        }

        #endregion InitializationCheck

        public void RunStateMachine()
        {
            if (m_currentState == null) return;

            for (var l_i = 0; l_i < m_currentStateConditionsAmount; l_i++)
            {
                if (!m_currentState.StateConditions[l_i].CompleteCondition(m_model))
                    continue;

                ChangeState(m_currentState.ExitStates[l_i].State.GetType());
                return;
            }

            m_currentState.State.ExecuteState(m_model);
        }

        private void ChangeState(Type p_newStateType)
        {
            if (!m_statesDictionary.TryGetValue(p_newStateType, out var l_newStateData))
                return;

            m_currentState.State.ExitState(m_model);
            m_currentState = l_newStateData;
            m_currentState.State.EnterState(m_model);
            m_currentStateConditionsAmount = m_currentState.StateConditions.Count;
        }

        public StateData GetCurrentState() => m_currentState;

        public List<StateData> GetAllStates() => m_allStatesData;

        public void AddState(StateData p_data) => m_allStatesData.Add(p_data);
        
        /// <summary>
        /// Reset the FSM to its initial state (first element) and re-enter it.
        /// </summary>
        public void ResetStateMachine()
        {
            if (m_allStatesData == null || m_allStatesData.Count == 0 || m_model == null)
                return;

            if (m_currentState != null)
            {
                m_currentState.State.ExitState(m_model);
            }

            m_currentState = m_allStatesData[0];
            m_currentState.State.EnterState(m_model);
            m_currentStateConditionsAmount = m_currentState.StateConditions.Count;
        }
        
        /// <summary>
        /// Change state by state name (for external requests like Decision Tree)
        /// </summary>
        /// <param name="stateName">Name of the state to change to</param>
        /// <returns>True if state change was successful</returns>
        public bool ChangeStateByName(string stateName)
        {
            if (string.IsNullOrEmpty(stateName) || m_allStatesData == null)
                return false;

            // Find state by name
            var targetState = m_allStatesData.FirstOrDefault(state => 
                state.State != null && 
                state.State.StateName != null && 
                state.State.StateName.Equals(stateName, StringComparison.OrdinalIgnoreCase));

            if (targetState?.State == null)
                return false;

            // Change to the found state
            ChangeState(targetState.State.GetType());
            return true;
        }
        
        public void ClearStates()
        {
            if (m_allStatesData == null)
                m_allStatesData = new List<StateData>();
            else
                m_allStatesData.Clear();
    
            m_currentState = null;
        }
    }
}
