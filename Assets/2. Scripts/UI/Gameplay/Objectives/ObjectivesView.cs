using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _2._Scripts.UI.Gameplay.Objectives
{
    public class ObjectivesView : UIView
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text objectivesText;
        [SerializeField] private Button dismissButton;
        [SerializeField] private string defaultTitle = "Objetivos";
        [SerializeField] private CanvasGroup canvasGroup;
        [Header("Auto-dismiss")]
        [SerializeField] private float autoDismissDelay = 4f;
        [SerializeField] private float fadeDuration = 0.35f;
        [SerializeField] private Transform animatedRoot;

        public event Action OnDismissRequested;

        private Coroutine m_fadeCoroutine;
        private bool m_dismissed;

        public void SetTitle(string title)
        {
            if (!titleText)
            {
                return;
            }

            titleText.text = string.IsNullOrEmpty(title) ? defaultTitle : title;
        }

        public void SetObjectives(IEnumerable<string> objectives)
        {
            if (!objectivesText)
            {
                return;
            }

            objectivesText.text = objectives == null ? string.Empty : string.Join("\n", objectives);
        }

        public override void Show()
        {
            base.Show();
            m_dismissed = false;
            CancelFade();
            ResetVisuals();
            StartAutoDismiss();
            if (dismissButton)
            {
                dismissButton.onClick.AddListener(OnDismiss);
            }
        }

        public override void Hide()
        {
            base.Hide();
            CancelFade();
            if (dismissButton)
            {
                dismissButton.onClick.RemoveListener(OnDismiss);
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();
            if (dismissButton)
            {
                dismissButton.onClick.RemoveListener(OnDismiss);
            }
        }

        private void OnDismiss()
        {
            if (m_dismissed)
                return;

            m_dismissed = true;
            CancelFade();
            OnDismissRequested?.Invoke();
        }

        private void ResetVisuals()
        {
            if (canvasGroup)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            if (animatedRoot)
            {
                animatedRoot.localScale = Vector3.one;
            }
        }

        private void StartAutoDismiss()
        {
            if (m_dismissed)
                return;

            m_fadeCoroutine = StartCoroutine(DismissAfterDelay());
        }

        private void CancelFade()
        {
            if (m_fadeCoroutine != null)
            {
                StopCoroutine(m_fadeCoroutine);
                m_fadeCoroutine = null;
            }
        }

        private System.Collections.IEnumerator DismissAfterDelay()
        {
            // Wait before starting fade out.
            yield return new WaitForSecondsRealtime(autoDismissDelay);

            if (m_dismissed)
                yield break;

            float elapsed = 0f;
            var startScale = animatedRoot ? animatedRoot.localScale : Vector3.one;
            var endScale = startScale * 0.9f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                if (canvasGroup)
                {
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                }

                if (animatedRoot)
                {
                    animatedRoot.localScale = Vector3.Lerp(startScale, endScale, t);
                }

                yield return null;
            }

            if (canvasGroup)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }

            OnDismiss();
        }
    }
}
