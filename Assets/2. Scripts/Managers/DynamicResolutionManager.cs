using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Gestor de Dynamic Resolution para mobile (URP, Unity 6).
/// - Requiere URP Dynamic Resolution activado en la cámara.
/// - Ajusta la resolución interna en función de CPU/GPU frame time.
/// - Pensado para Android/iOS (idealmente Vulkan/Metal).
/// </summary>
[DisallowMultipleComponent]
public class MobileDynamicResolution : MonoBehaviour
{
    [Header("Objetivo de rendimiento")]
    [Tooltip("FPS objetivo en el device (ej: 60 para móviles modernos, 30 para low-end).")]
    public int targetFPS = 60;

    [Tooltip("Margen de FPS por debajo del objetivo antes de bajar la resolución.")]
    public float lowerFpsMargin = 5f;

    [Tooltip("Margen de FPS por encima del objetivo antes de subir la resolución.")]
    public float upperFpsMargin = 10f;

    [Header("Escala de resolución")]
    [Range(0.5f, 1.0f)]
    [Tooltip("Escala mínima de resolución render (valor más bajo permitido).")]
    public float minScale = 0.6f;

    [Range(0.5f, 1.0f)]
    [Tooltip("Escala máxima de resolución render (normalmente 1.0).")]
    public float maxScale = 1.0f;

    [Tooltip("Cuánto sube/baja la escala cada vez que ajusta.")]
    [Range(0.01f, 0.2f)]
    public float scaleStep = 0.05f;

    [Header("Frecuencia de ajuste")]
    [Tooltip("Cada cuántos segundos vuelve a evaluar el rendimiento.")]
    [Range(0.2f, 3f)]
    public float evaluationInterval = 0.5f;

    private float _currentScale;
    private float _timer;
    private readonly FrameTiming[] _frameTimings = new FrameTiming[1];

    private void Awake()
    {
        _currentScale = Mathf.Clamp(maxScale, minScale, 1.0f);

        // Fijar targetFrameRate acorde al objetivo (no obligatorio, pero recomendado)
        Application.targetFrameRate = targetFPS;

#if !UNITY_EDITOR
        QualitySettings.vSyncCount = 0; // En mobile suele ser mejor controlar por targetFrameRate
#endif

        ApplyScale();
    }

    private void Update()
    {
        _timer += Time.unscaledDeltaTime;
        if (_timer < evaluationInterval)
            return;

        _timer = 0f;
        AdjustScaleBasedOnPerformance();
    }

    private void AdjustScaleBasedOnPerformance()
    {
        // Capturamos los timings del frame
        FrameTimingManager.CaptureFrameTimings();
        uint count = FrameTimingManager.GetLatestTimings(1, _frameTimings);
        if (count == 0)
            return;

        // Tiempo de CPU y GPU en milisegundos
        double cpuMs = _frameTimings[0].cpuFrameTime;
        double gpuMs = _frameTimings[0].gpuFrameTime;

        // Consideramos el peor de los dos (el cuello de botella)
        double frameMs = Mathf.Max((float)cpuMs, (float)gpuMs);
        if (frameMs <= 0.0)
            return;

        float fps = 1000f / (float)frameMs;

        // Lógica de auto-escalado con histéresis
        float minFps = targetFPS - lowerFpsMargin;
        float maxFps = targetFPS + upperFpsMargin;

        if (fps < minFps && _currentScale > minScale)
        {
            // Vamos justos de rendimiento → bajamos resolución
            _currentScale = Mathf.Max(minScale, _currentScale - scaleStep);
            ApplyScale();

        }
        else if (fps > maxFps && _currentScale < maxScale)
        {
            // Tenemos margen de sobra → subimos resolución
            _currentScale = Mathf.Min(maxScale, _currentScale + scaleStep);
            ApplyScale();

        }
    }

    private void ApplyScale()
    {
        ScalableBufferManager.ResizeBuffers(_currentScale, _currentScale);
    }
}
