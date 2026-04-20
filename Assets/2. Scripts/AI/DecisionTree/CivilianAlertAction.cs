// TODO(MIN_SCOPE): Template for civilian Decision Tree "Alert" action
// This file should be implemented when the civilian Decision Tree system is ready

/*
Example implementation for civilian DT Alert action:

using UnityEngine;

public class CivilianAlertAction : IDecisionTreeAction
{
    public void Execute(IAIContext context)
    {
        var blackboard = ServiceLocator.Get<IBlackboard>();
        if (blackboard != null)
        {
            // Minimum scope: Set global alert when civilian triggers alarm
            blackboard.SetValue(BlackboardKeys.GLOBAL_ALERT, true);

            Logger.LogInfo("Civilian triggered GLOBAL_ALERT");
        }
    }
}
*/