using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "LoseSightCondition", menuName = "Main/FSM/Civilian Conditions/Lose Sight")]
public class CivilianLoseSightCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Civilian civilian)
        {
            // Delegate the decision to the Civilian using Single Responsibility principle
            // The Civilian owns all the logic for determining when pursuit should abort
            bool shouldAbort = civilian.ShouldAbortPursuit();
            
            if (shouldAbort && civilian.EnableDebugLogs)
            {
                MyLogger.LogInfo($"Civilian {civilian.name}: LoseSightCondition triggered abort - " +
                    $"LoseSight timer: {civilian.PursuitLoseSightTimer:F2}s, Grace: {civilian.AttackLoseSightGrace:F2}s");
            }
            
            return shouldAbort;
        }
        return false;
    }
}