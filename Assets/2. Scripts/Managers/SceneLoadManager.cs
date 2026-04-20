using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager Instance { get; private set; }
    const float MinLoadingSeconds = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void LoadWithLoading(string gameplayScene, string loadingScene)
    {
        if (!Instance)
        {
            var go = new GameObject("~SceneLoadController");
            Instance = go.AddComponent<SceneLoadManager>();
            DontDestroyOnLoad(go);
        }
        Instance.StartCoroutine(Instance.Flow(gameplayScene, loadingScene));
    }

    IEnumerator Flow(string gameplayScene, string loadingScene)
    {
        var loadLoading = SceneManager.LoadSceneAsync(loadingScene, LoadSceneMode.Single);
        
        while (!loadLoading.isDone) 
            yield return null;
        
        var shownAt = Time.realtimeSinceStartup;

        while (!LoadingUI.Instance)
            yield return null;
        
        if (LoadingUI.Instance)
        {
            LoadingUI.Instance.gameObject.SetActive(true);
            LoadingUI.Instance.ShowImmediate();
            LoadingUI.Instance.SetProgress(0f);
        }
        
        var op = SceneManager.LoadSceneAsync(gameplayScene, LoadSceneMode.Additive);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            if (LoadingUI.Instance) LoadingUI.Instance.SetProgress(Mathf.Clamp01(op.progress / 0.9f));
            yield return null;
        }
        if (LoadingUI.Instance) LoadingUI.Instance.SetProgress(1f);
        
        float elapsed = Time.realtimeSinceStartup - shownAt;
        if (elapsed < MinLoadingSeconds)
            yield return new WaitForSecondsRealtime(MinLoadingSeconds - elapsed);
        
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        var gp = SceneManager.GetSceneByName(gameplayScene);
        if (gp.IsValid()) SceneManager.SetActiveScene(gp);
        
        yield return SceneManager.UnloadSceneAsync(loadingScene);

        yield return Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}
