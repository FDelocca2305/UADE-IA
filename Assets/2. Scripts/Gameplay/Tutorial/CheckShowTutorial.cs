using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class CheckShowTutorial : MonoBehaviour
{
    void Awake()
    {
        if (TutorialSeenService.HasSeen())
        {
            this.gameObject.SetActive(false);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Reset Tutorial Seen")]
    private void ResetTutorialSeen()
    {
        TutorialSeenService.Reset();
    }
#endif
}
