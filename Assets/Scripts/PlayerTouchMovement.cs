using System;
using System.Collections;
using _2._Scripts.UI.MainMenu;
using Services;
using Services.MicroServices.AudioService;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem;
using ETouch = UnityEngine.InputSystem.EnhancedTouch;

public class PlayerTouchMovement : MonoBehaviour
{
    [SerializeField]
    private Vector2 JoystickSize = new Vector2(300, 300);
    [SerializeField]
    private FloatingJoystick Joystick;
    [SerializeField]
    private NavMeshAgent Player;

    [SerializeField] private string objectivesPanelName = "Objectives";
    [SerializeField] private GameObject dragTutorial;
    [SerializeField] private GameObject shootTutorial;
    [SerializeField] private PanelsController panelsController;

    [Header("Audio")]
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField] private float footstepInterval = 0.5f;
    
    private Finger MovementFinger;
    private Vector2 MovementAmount;
    
    [SerializeField]
    private MainCharacter mainCharacter;

    private Finger TapFinger;
    
    bool dragClosed, shootClosed, objectivesShown;
    bool shouldAutoShowObjectives;
    
    private Finger ShootingFinger;
    private Vector2 ShootingStartPosition;
    private Vector2 ShootingAmount;
    private bool isContinuousShooting = false;
    private bool hasBurstFired = false;
    int _speedHash;
    private Coroutine continuousShootingCoroutine;
    private InputAction moveAction;

    [Header("Shooting Settings")]
    [SerializeField] private FloatingJoystick ShootingJoystick;
    [SerializeField] private float continuousShootingInterval = 0.3f;
    [SerializeField] private int burstShotCount = 5;
    [SerializeField] private float burstShotInterval = 0.25f;
    [SerializeField]
    private float continuousShootingDelay = 0.3f;
    private Coroutine delayedContinuousCoroutine;
    
    [Header("Animator")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private string speedParam = "speed";
    [SerializeField] private float speedDampTime = 0.1f;
    [SerializeField] private bool normalizeSpeed = true;
    [SerializeField] private float maxSpeedForParam = 3.5f;

    private static AudioService AudioService => AudioService.Instance;
    private AudioConfig m_audioConfig;
    private float lastFootstepTime;
    private bool wasMovingLastFrame;

    private void Awake()
    {
        SetupMoveAction();

        if (TutorialSeenService.HasSeen())
        {
            dragTutorial.SetActive(false);
            shootTutorial.SetActive(false);
            shouldAutoShowObjectives = true;
        }
        
        dragClosed = shootClosed = objectivesShown = false;
        m_audioConfig = AudioService.GetConfig();
        _speedHash = Animator.StringToHash(speedParam);

        // Si ya se vio el tutorial, aseguramos mostrar los objetivos (luego de que UI se inicialice).
        if (shouldAutoShowObjectives)
            StartCoroutine(ShowObjectivesNextFrame());
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        ETouch.Touch.onFingerDown += HandleFingerDown;
        ETouch.Touch.onFingerUp += HandleLoseFinger;
        ETouch.Touch.onFingerMove += HandleFingerMove;
        moveAction?.Enable();
    }

    private void OnDisable()
    {
        ETouch.Touch.onFingerDown -= HandleFingerDown;
        ETouch.Touch.onFingerUp -= HandleLoseFinger;
        ETouch.Touch.onFingerMove -= HandleFingerMove;
        EnhancedTouchSupport.Disable();
        moveAction?.Disable();
    }

    private void HandleFingerMove(Finger MovedFinger)
    {
        if (MovedFinger == MovementFinger)
        {
            Vector2 knobPosition;
            float maxMovement = JoystickSize.x / 2f;
            ETouch.Touch currentTouch = MovedFinger.currentTouch;
            if (!IsValidVector2(currentTouch.screenPosition))
            {
                return;
            }

            if (Vector2.Distance(
                    currentTouch.screenPosition,
                    Joystick.RectTransform.anchoredPosition
                ) > maxMovement)
            {
                knobPosition = (
                    currentTouch.screenPosition - Joystick.RectTransform.anchoredPosition
                    ).normalized
                    * maxMovement;
            }
            else
            {
                knobPosition = currentTouch.screenPosition - Joystick.RectTransform.anchoredPosition;
            }

            Joystick.Knob.anchoredPosition = knobPosition;
            MovementAmount = knobPosition / maxMovement;

            if (!dragClosed && dragTutorial && dragTutorial.activeSelf)
                StartCoroutine(CloseAfter(dragTutorial, 2f, () => { dragClosed = true; TryShowObjectives(); }));
        }
        else if (MovedFinger == ShootingFinger)
        {
            // Handle shooting joystick movement
            Vector2 knobPosition;
            float maxMovement = JoystickSize.x / 2f;
            ETouch.Touch currentTouch = MovedFinger.currentTouch;
            
            if (!IsValidVector2(currentTouch.screenPosition))
            {
                return;
            }

            // Calculate knob position relative to start position
            Vector2 dragVector = currentTouch.screenPosition - ShootingStartPosition;
            
            if (dragVector.magnitude > maxMovement)
            {
                knobPosition = dragVector.normalized * maxMovement;
            }
            else
            {
                knobPosition = dragVector;
            }

            // Clamp knob position to stay within joystick bounds
            knobPosition = Vector2.ClampMagnitude(knobPosition, maxMovement);
            
            ShootingJoystick.Knob.anchoredPosition = knobPosition;
            ShootingAmount = knobPosition / maxMovement;
            
            // If we have significant drag, trigger burst shooting
            if (dragVector.magnitude > 30f && mainCharacter != null)
            {
                // Cancel delayed continuous shooting if it was scheduled
                if (delayedContinuousCoroutine != null)
                {
                    StopCoroutine(delayedContinuousCoroutine);
                    delayedContinuousCoroutine = null;
                }
                
                // Stop any active continuous shooting
                if (isContinuousShooting && continuousShootingCoroutine != null)
                {
                    StopCoroutine(continuousShootingCoroutine);
                    isContinuousShooting = false;
                }
                
                // Only fire burst if we haven't already or if we're still dragging
                if (!hasBurstFired || dragVector.magnitude > 50f)
                {
                    Vector3 shootDirection = new Vector3(dragVector.normalized.x, 0, dragVector.normalized.y);
                    if (!hasBurstFired)
                    {
                        // First burst
                        StartCoroutine(ShootBurst(shootDirection));
                        hasBurstFired = true;
                    }
                }
            }
        }
    }

    private void HandleLoseFinger(Finger LostFinger)
    {
        if (LostFinger == MovementFinger)
        {
            MovementFinger = null;
            Joystick.Knob.anchoredPosition = Vector2.zero;
            Joystick.gameObject.SetActive(false);
            MovementAmount = Vector2.zero;
        }
        else if (LostFinger == ShootingFinger)
        {
            // Cancel delayed continuous shooting
            if (delayedContinuousCoroutine != null)
            {
                StopCoroutine(delayedContinuousCoroutine);
                delayedContinuousCoroutine = null;
            }
            
            // Stop continuous shooting
            if (isContinuousShooting && continuousShootingCoroutine != null)
            {
                StopCoroutine(continuousShootingCoroutine);
                isContinuousShooting = false;
            }
            
            // If no burst was fired and no continuous shooting was active, fire a single shot
            if (!hasBurstFired && !isContinuousShooting && mainCharacter != null)
            {
                Vector3 shootDirection = mainCharacter.transform.forward;
                mainCharacter.Shoot(shootDirection);
            }
            
            // Reset shooting joystick
            ShootingJoystick.Knob.anchoredPosition = Vector2.zero;
            ShootingJoystick.gameObject.SetActive(false);
            ShootingFinger = null;
            ShootingAmount = Vector2.zero;
            hasBurstFired = false;

            if (!shootClosed && shootTutorial && shootTutorial.activeSelf)
                StartCoroutine(CloseAfter(shootTutorial, 0f, () => { shootClosed = true; TryShowObjectives(); }));
        }
    }

    private void HandleFingerDown(Finger TouchedFinger)
    {
        float halfScreenWidth = Screen.width / 2f;

        if (!TryGetScreenPosition(TouchedFinger, out Vector2 screenPosition))
        {
            return;
        }

        if (MovementFinger == null && screenPosition.x <= halfScreenWidth)
        {
            MovementFinger = TouchedFinger;
            MovementAmount = Vector2.zero;
            Joystick.gameObject.SetActive(true);
            Joystick.RectTransform.sizeDelta = JoystickSize;
            Joystick.RectTransform.anchoredPosition = ClampStartPosition(screenPosition);
        }
        else if (screenPosition.x > halfScreenWidth)
        {
            ShootingFinger = TouchedFinger;
            ShootingStartPosition = ClampShootingPosition(screenPosition);
            ShootingAmount = Vector2.zero;
            hasBurstFired = false;
            
            ShootingJoystick.gameObject.SetActive(true);
            ShootingJoystick.RectTransform.sizeDelta = JoystickSize;
            ShootingJoystick.RectTransform.anchoredPosition = ShootingStartPosition;
            ShootingJoystick.Knob.anchoredPosition = Vector2.zero;
            
            if (mainCharacter != null)
            {
                delayedContinuousCoroutine = StartCoroutine(DelayedContinuousShooting());
            }
        }
    }

    private Vector2 ClampStartPosition(Vector2 StartPosition)
    {
        if (StartPosition.x < JoystickSize.x / 2)
        {
            StartPosition.x = JoystickSize.x / 2;
        }

        if (StartPosition.y < JoystickSize.y / 2)
        {
            StartPosition.y = JoystickSize.y / 2;
        }
        else if (StartPosition.y > Screen.height - JoystickSize.y / 2)
        {
            StartPosition.y = Screen.height - JoystickSize.y / 2;
        }

        return StartPosition;
    }

    private void Update()
    {
        if (!Player || !Player.enabled || !Player.isOnNavMesh) return;

        if (MovementFinger == null && moveAction != null)
        {
            var keyboardInput = moveAction.ReadValue<Vector2>();
            if (keyboardInput.sqrMagnitude > 0.01f)
            {
                MovementAmount = Vector2.ClampMagnitude(keyboardInput, 1f);
            }
            else
            {
                MovementAmount = Vector2.zero;
            }
        }

        Vector3 scaledMovement = Player.speed * Time.deltaTime * new Vector3(
            MovementAmount.x,
            0,
            MovementAmount.y
        );

        Player.transform.LookAt(Player.transform.position + scaledMovement, Vector3.up);
        Player.Move(scaledMovement);

        // Handle footstep sounds
        bool isMoving = scaledMovement.magnitude > 0.01f;

        if (isMoving)
        {
            if (!wasMovingLastFrame && footstepAudioSource != null && m_audioConfig != null)
            {
                // Just started moving, play footsteps in loop
                footstepAudioSource.clip = m_audioConfig.concreteFootstepsSFX;
                footstepAudioSource.loop = true;
                if (!footstepAudioSource.isPlaying)
                {
                    footstepAudioSource.Play();
                }
            }
        }
        else
        {
            if (wasMovingLastFrame && footstepAudioSource != null)
            {
                // Just stopped moving, stop footsteps
                footstepAudioSource.Stop();
            }
        }

        wasMovingLastFrame = isMoving;

        
        if (characterAnimator)
        {
            float worldSpeed = (new Vector3(MovementAmount.x, 0, MovementAmount.y).magnitude) * Player.speed;

            float paramValue = normalizeSpeed
                ? Mathf.InverseLerp(0f, Mathf.Max(0.01f, maxSpeedForParam), worldSpeed)
                : worldSpeed;

            characterAnimator.SetFloat(_speedHash, paramValue, speedDampTime, Time.deltaTime);
        }
    }

    private bool TryGetScreenPosition(Finger finger, out Vector2 screenPosition)
    {
        screenPosition = Vector2.zero;

        if (finger == null)
        {
            return false;
        }

        if (IsValidVector2(finger.screenPosition))
        {
            screenPosition = finger.screenPosition;
            return true;
        }

        var touch = finger.currentTouch;
        bool touchValid = touch.touchId >= 0;
        if (touchValid)
        {
            if (IsValidVector2(touch.screenPosition))
            {
                screenPosition = touch.screenPosition;
                return true;
            }

            if (IsValidVector2(touch.startScreenPosition))
            {
                screenPosition = touch.startScreenPosition;
                return true;
            }
        }

        return false;
    }

    private bool IsValidVector2(Vector2 value)
    {
        return IsFinite(value.x) && IsFinite(value.y);
    }

    private bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
    
    IEnumerator CloseAfter(GameObject go, float seconds, System.Action onClosed)
    {
        if (!go) yield break;
        if (seconds > 0f) yield return new WaitForSecondsRealtime(seconds);
        go.SetActive(false);
        onClosed?.Invoke();
    }
    
    void TryShowObjectives()
    {
        if (objectivesShown) return;

        bool dragDone  = (dragTutorial  == null) || !dragTutorial.activeInHierarchy || dragClosed;
        bool shootDone = (shootTutorial == null) || !shootTutorial.activeInHierarchy || shootClosed;

        if (dragDone && shootDone)
        {
            objectivesShown = true;
            if (panelsController)
                panelsController.ShowUI(objectivesPanelName);
        }
    }

    private IEnumerator ShowObjectivesNextFrame()
    {
        // Esperamos un frame para que PanelsController inicialice su estado/UI.
        yield return null;
        TryShowObjectives();
    }
    
    private Vector2 ClampShootingPosition(Vector2 screenPosition)
    {
        float minX = Screen.width / 2 + JoystickSize.x / 2;
        float maxX = Screen.width - JoystickSize.x / 2;
        float minY = JoystickSize.y / 2;
        float maxY = Screen.height - JoystickSize.y / 2;

        screenPosition.x = Mathf.Clamp(screenPosition.x, minX, maxX);
        screenPosition.y = Mathf.Clamp(screenPosition.y, minY, maxY);

        return screenPosition;
    }

    private IEnumerator ShootContinuously()
    {
        while (isContinuousShooting && mainCharacter != null)
        {
            Vector3 shootDirection = mainCharacter.transform.forward;
            mainCharacter.Shoot(shootDirection);
            yield return new WaitForSeconds(continuousShootingInterval);
        }
    }

    private IEnumerator ShootBurst(Vector3 direction)
    {
        if (mainCharacter == null) yield break;
        
        if (direction.magnitude > 0.1f)
        {
            direction.Normalize();
        }
        else
        {
            direction = mainCharacter.transform.forward;
        }
        
        for (int i = 0; i < burstShotCount; i++)
        {
            if (mainCharacter != null)
            {
                mainCharacter.Shoot(direction);
            }
            yield return new WaitForSeconds(burstShotInterval);
        }
    }
    
    private IEnumerator DelayedContinuousShooting()
    {
        yield return new WaitForSeconds(continuousShootingDelay);
        
        if (ShootingFinger != null && !hasBurstFired && mainCharacter != null)
        {
            isContinuousShooting = true;
            continuousShootingCoroutine = StartCoroutine(ShootContinuously());
        }
    }

    private void SetupMoveAction()
    {
        if (moveAction != null)
            return;

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");
    }
}
