using System;
using System.Collections.Generic;
using UnityEngine;

namespace _2._Scripts.UI.MainMenu
{
    public class PanelsController : MonoBehaviour
    {
        [SerializeField] private UIPresenter[] uiPresenters;

        private Dictionary<string, UIPresenter> m_uiPresenterDictionary;

        private void Awake()
        {
            m_uiPresenterDictionary = new Dictionary<string, UIPresenter>();

            for (var l_i = 0; l_i < uiPresenters.Length; l_i++)
            {
                if (m_uiPresenterDictionary.TryAdd(uiPresenters[l_i].UIName, uiPresenters[l_i]))
                {
                    uiPresenters[l_i].Initialize();
                    continue;
                }

                MyLogger.LogError($"Failed to add {uiPresenters[l_i].UIName} to dictionary");
            }
        }

        private void OnDestroy()
        {
            for (var l_i = 0; l_i < uiPresenters.Length; l_i++)
            {
                if (m_uiPresenterDictionary.ContainsKey(uiPresenters[l_i].UIName))
                    uiPresenters[l_i].Shutdown();
            }
        }

        public void ShowUI(string p_uiName)
        {
            if (m_uiPresenterDictionary.TryGetValue(p_uiName, out var l_presenter))
            {
                l_presenter.Show();
            }
            else
            {
                MyLogger.LogError($"Failed to find UI {p_uiName}");
            }
        }

        public void HideUI(string p_uiName)
        {
            if (m_uiPresenterDictionary.TryGetValue(p_uiName, out var l_presenter))
            {
                l_presenter.Hide();
            }
            else
            {
                MyLogger.LogError($"Failed to find UI {p_uiName}");
            }
        }
    }
}