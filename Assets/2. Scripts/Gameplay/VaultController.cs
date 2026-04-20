using System;
using System.Collections;
using Services;
using Services.MicroServices.EventsServices;
using StaticClass;
using UnityEngine;

namespace _2._Scripts.Gameplay
{
    public class VaultController : MonoBehaviour
    {
        [SerializeField] private Transform vaultDoorTransform;
        [SerializeField] private float timeToForceOpen = 2f;
        [SerializeField] private float openAnimationTime = 2f;
        [SerializeField] private float angleToOpen = 120f;
        
        private Coroutine m_animationCoroutine;
        private float m_forceOpenTimer;
        private bool m_isDoorOpen;
        [SerializeField] private GameObject[] uiToHideOnOpen;
        
        private static IEventService EventService => ServiceLocator.Get<IEventService>();

        private void Awake()
        {
            m_forceOpenTimer = timeToForceOpen;
        }

        private void OpenDoor()
        {
            if (m_animationCoroutine != null || m_isDoorOpen) 
                return;
            
            EventService.DispatchEvent(EventsDefinition.OPEN_VAULT);
            m_isDoorOpen = true;
            m_animationCoroutine = StartCoroutine(OpenDoorCoroutine());
        }

        private IEnumerator OpenDoorCoroutine()
        {
            var l_timer = 0f;
            var l_rotation = vaultDoorTransform.rotation;
            var l_rotationFinal = Quaternion.Euler(0f, -angleToOpen, 0f);
            
            while (l_timer < openAnimationTime) 
            {
                l_timer += Time.deltaTime;
                vaultDoorTransform.rotation = Quaternion.Slerp(l_rotation, l_rotationFinal, l_timer / openAnimationTime);
                yield return null;
            }
            
            EventService.DispatchEvent(EventsDefinition.END_OPEN_VAULT);
            HideVaultUI();
            m_animationCoroutine = null;
        }

        private void OnTriggerEnter(Collider p_other)
        {
            if (!p_other.CompareTag("Player")) 
                return;
            
            m_forceOpenTimer = timeToForceOpen;
            EventService.DispatchEvent(EventsDefinition.START_OPEN_VAULT);
        }

        private void OnTriggerStay(Collider p_other)
        {
            if (m_isDoorOpen)
                return;

            if (!p_other.CompareTag("Player")) 
                return;
            
            m_forceOpenTimer -= Time.deltaTime;
            EventService.DispatchEvent(new OnUpdateVaultEvent(m_forceOpenTimer));
            if (m_forceOpenTimer <= 0f)
            {
                OpenDoor();
            }
        }

        private void OnTriggerExit(Collider p_other)
        {
            if (m_isDoorOpen)
                return;

            if (!p_other.CompareTag("Player")) 
                return;
            
            m_forceOpenTimer = timeToForceOpen;
            EventService.DispatchEvent(EventsDefinition.END_OPEN_VAULT);
        }

        private void HideVaultUI()
        {
            if (uiToHideOnOpen == null) return;

            for (int i = 0; i < uiToHideOnOpen.Length; i++)
            {
                if (uiToHideOnOpen[i])
                {
                    uiToHideOnOpen[i].SetActive(false);
                }
            }
        }
    }

    public struct OnUpdateVaultEvent : ICustomEventData
    {
        public readonly float Seconds;

        public OnUpdateVaultEvent(float p_seconds)
        {
            Seconds = p_seconds;
        }
    }
}
