using UnityEngine;

namespace Scripts.FSM.Models
{
    public interface IUseFsm
    {
        public Transform GetModelTransform();

        public void UpdateFsm();

        public void SetTargetTransform(Transform p_target);

        public Transform GetTargetTransform();
    }
}