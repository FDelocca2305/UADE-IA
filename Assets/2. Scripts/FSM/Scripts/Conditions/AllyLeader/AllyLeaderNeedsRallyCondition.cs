using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "AllyLeaderNeedsRallyCondition", menuName = "Main/FSM/AllyLeader Conditions/Needs Rally")]
public class AllyLeaderNeedsRallyCondition : StateCondition
{
    [SerializeField] private int minimumAliveAllies = 3;

    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is AllyLeader leader)
        {
            return leader.GetAliveAlliesCount() < minimumAliveAllies;
        }

        return false;
    }
}
