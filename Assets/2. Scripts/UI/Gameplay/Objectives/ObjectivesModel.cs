using System.Collections.Generic;
using UnityEngine;

namespace _2._Scripts.UI.Gameplay.Objectives
{
    public class ObjectivesModel : UIModel
    {
        private readonly List<string> m_objectives = new();

        public IReadOnlyList<string> Objectives => m_objectives;

        public void SetObjectives(IEnumerable<string> objectives)
        {
            m_objectives.Clear();
            if (objectives == null)
            {
                return;
            }

            m_objectives.AddRange(objectives);
        }
    }
}
