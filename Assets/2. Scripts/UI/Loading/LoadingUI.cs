using System;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] Slider progress;
    [SerializeField] CanvasGroup canvasGroup;

    public static LoadingUI Instance;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }
        
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) 
            Instance = null;
    }

    public void ShowImmediate()
    {
        if (canvasGroup)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = false;
        }
        gameObject.SetActive(true);
    }

    public void SetProgress(float v)
    {
        if (progress) progress.value = Mathf.Clamp01(v);
    }
}