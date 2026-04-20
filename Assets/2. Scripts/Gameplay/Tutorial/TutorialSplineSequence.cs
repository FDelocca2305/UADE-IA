using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Splines;
using UnityEngine.UI;
using System.Collections;

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(CinemachineCamera))]
[RequireComponent(typeof(CinemachineSplineDolly))]
public class TutorialSplineSequence : MonoBehaviour
{
    
    [Header("Targets y Splines")]
    public Transform lookAtTarget;
    public SplineContainer firstSpline;
    public SplineContainer nextSpline;

    [Header("UI del Tutorial")]
    public GameObject tutorialUIRoot;
    public Image blackScreen;
    public float fadeCloseSeconds = 0.15f;
    public float fadeOpenSeconds  = 0.5f;

    [Header("Canvases de gameplay a ocultar mientras corre")]
    public GameObject[] canvasToDisable;

    [Header("Tiempos")]
    public float firstLegSeconds = 15f;
    public float secondLegSeconds = 7f;

    public enum EasingType { Linear, EaseIn, EaseOut, EaseInOut, SmoothStep }
    public EasingType easingType = EasingType.Linear;

    [Header("Prioridades")]
    public int startPriority = 50;
    public int endPriority   = 9;

    [Header("Debug")]  
    public bool debugLog = false;
    
    [Header("Gameplay Camera Follow")]
    [SerializeField] private CameraFollowController followController;

    CinemachineCamera cam;
    CinemachineSplineDolly dolly;
    CinemachineRotationComposer rot;

    Coroutine seq;
    bool driving;
    float targetPosKnot;
    bool alreadySeenTutorial;
    bool skipping;

    void Awake()
    {
        alreadySeenTutorial = TutorialSeenService.HasSeen();
        if (alreadySeenTutorial)
        {
            this.gameObject.SetActive(false);
        }
        
        cam   = GetComponent<CinemachineCamera>();
        dolly = GetComponent<CinemachineSplineDolly>();
        rot   = GetComponent<CinemachineRotationComposer>();

        if (rot) rot.enabled = false;
        cam.LookAt = null;

        if (firstSpline) dolly.Spline = firstSpline;
        
        if (blackScreen) blackScreen.fillAmount = 0f;
        if (tutorialUIRoot) tutorialUIRoot.SetActive(true);
        
        if (followController) followController.enabled = false;
        cam.enabled = true;
    }

    void Start()
    {
        if (!alreadySeenTutorial)
        {
            cam.Priority.Value = startPriority;
            SetGameplayCanvasActive(false);
            PlayTutorial();
        }
        else
        {
            if (tutorialUIRoot) tutorialUIRoot.SetActive(false);
        }
    }

    void SetGameplayCanvasActive(bool active)
    {
        foreach (var go in canvasToDisable)
            if (go) go.SetActive(active);
    }
    
    public void OnSkipButton()
    {
        TutorialSeenService.SetSeen();
        if (!skipping) StartCoroutine(SkipRoutine());
    }

    public void PlayTutorial()
    {
        if (seq != null) StopCoroutine(seq);
        seq = StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        if (!ValidateSpline(dolly.Spline, "primer")) yield break;

        float endA = Mathf.Min(4f, MaxKnotInclusive(dolly.Spline));
        yield return MoveKnotRange(0f, endA, Mathf.Max(0.01f, firstLegSeconds));
        
        if (lookAtTarget)
        {
            cam.LookAt = lookAtTarget;
            if (rot) rot.enabled = true;
        }
        
        if (nextSpline)
        {
            dolly.Spline = nextSpline;
            targetPosKnot = 0f;
            yield return null;

            if (!ValidateSpline(nextSpline, "segundo")) yield break;
            
            float toB = 0f;
            yield return MoveKnotRange(4f, toB, Mathf.Max(0.01f, secondLegSeconds));
        }

        EndTutorialAndReturnToGameplay();
    }
    
    IEnumerator SkipRoutine()
    {
        skipping = true;
        
        yield return FadeBlack(0f, 1f, fadeCloseSeconds);
        
        if (seq != null) StopCoroutine(seq);
        EndTutorialAndReturnToGameplay();
        
        yield return FadeBlack(1f, 0f, fadeOpenSeconds);
        
        if (tutorialUIRoot) tutorialUIRoot.SetActive(false);
        SetGameplayCanvasActive(true);

        skipping = false;
    }

    void EndTutorialAndReturnToGameplay()
    {
        driving = false;
        if (rot) rot.enabled = false;
        cam.LookAt = null;
        
        if (followController)
        {
            followController.enabled = true;
            followController.SnapToTarget();   // <- clave para evitar la espera
        }
        
        if (cam) cam.enabled = false;
        alreadySeenTutorial = true;
        if (!skipping)
        {
            SetGameplayCanvasActive(true);
            if (tutorialUIRoot) tutorialUIRoot.SetActive(false);
        }
        TutorialSeenService.SetSeen();
    }
    
    IEnumerator MoveKnotRange(float fromKnot, float toKnot, float seconds)
    {
        driving = true;
        targetPosKnot = fromKnot;
        
        yield return null;
        int guard = 0;
        while (Time.deltaTime == 0f && Time.unscaledDeltaTime == 0f && guard++ < 120)
            yield return null;

        float elapsed = 0f;
        while (elapsed < seconds && !skipping)
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) dt = Time.unscaledDeltaTime;

            elapsed += dt;
            float u = Mathf.Clamp01(elapsed / seconds);
            float w = EvaluateEasing(u);

            targetPosKnot = Mathf.Lerp(fromKnot, toKnot, w);

            if (debugLog)
                MyLogger.LogInfo($"[TSS] target={targetPosKnot:0.###} actual={dolly.CameraPosition:0.###}");

            yield return null;
        }

        targetPosKnot = toKnot;
    }

    IEnumerator FadeBlack(float from, float to, float seconds)
    {
        if (!blackScreen || seconds <= 0f) yield break;
        
        if (tutorialUIRoot && !tutorialUIRoot.activeSelf) tutorialUIRoot.SetActive(true);

        float t = 0f;
        blackScreen.fillAmount = from;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / seconds);
            blackScreen.fillAmount = Mathf.Lerp(from, to, u);
            yield return null;
        }
        blackScreen.fillAmount = to;
    }

    float EvaluateEasing(float u)
    {
        switch (easingType)
        {
            default:
            case EasingType.Linear:     return u;
            case EasingType.SmoothStep: return u * u * (3f - 2f * u);
            case EasingType.EaseIn:     return u * u;
            case EasingType.EaseOut:    return 1f - (1f - u) * (1f - u);
            case EasingType.EaseInOut:  return (u < 0.5f) ? 2f * u * u : 1f - Mathf.Pow(-2f * u + 2f, 2f) / 2f;
        }
    }

    void LateUpdate()
    {
        if (!driving) return;

        dolly.AutomaticDolly.Enabled = false;
        dolly.PositionUnits = PathIndexUnit.Knot;
        dolly.Damping.Enabled = false;

        dolly.CameraPosition = targetPosKnot;
    }

    float MaxKnotInclusive(SplineContainer sc)
    {
        var s = sc.Spline;
        return s.Closed ? s.Count : Mathf.Max(0, s.Count - 1);
    }

    bool ValidateSpline(SplineContainer sc, string label)
    {
        if (!sc) { MyLogger.LogError($"[TSS] {label} spline = NULL"); return false; }
        int c = sc.Spline.Count;
        if (c < 5) { MyLogger.LogError($"[TSS] {label} spline necesita >= 5 knots (tiene {c})"); return false; }
        return true;
    }
    
    public static void ResetTutorialSeen()
    {
        TutorialSeenService.Reset();
    }
}
