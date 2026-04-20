using Scripts.FSM.Base.StateMachine;
using Scripts.FSM.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "ChooseAttackPathCondition", menuName = "Main/FSM/Civilian Conditions/Choose Attack Path")]
public class CivilianChooseAttackPathCondition : StateCondition
{
    public override bool CompleteCondition(IUseFsm p_model)
    {
        if (p_model is Civilian civilian)
        {
            // Only trigger if we can see the player
            if (!civilian.HasLoS()) return false;


            // IA Roulette Wheel Selection
            // Roulette decision: calculate if we should choose attack path
            float r = Random.value;
            float total = civilian.EscapeWeight + civilian.AttackWeight;
            bool chooseAttack = (r >= (civilian.EscapeWeight / Mathf.Max(0.0001f, total)));

            if (civilian.EnableDebugLogs)
            {
                MyLogger.LogInfo($"Civilian {civilian.name}: Roulette decision - Roll: {r:F3}, " +
                    $"Escape Weight: {civilian.EscapeWeight}, Attack Weight: {civilian.AttackWeight}, " +
                    $"Choose: {(chooseAttack ? "ATTACK" : "ESCAPE")}");
            }

            return chooseAttack;
        }
        return false;
    }
}