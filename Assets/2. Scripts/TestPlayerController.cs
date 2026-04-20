using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TestPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Physics")]
    [SerializeField] private bool usePhysicsMovement = true;

    private Rigidbody rb;
    private Vector3 movement;
    private bool isMoving;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Configure rigidbody for better movement
        rb.freezeRotation = true;
        rb.linearDamping = 5f; // Add some drag for better stopping

        // Ensure the player has the "Player" tag for AI detection
        if (!gameObject.CompareTag("Player"))
        {
            gameObject.tag = "Player";
            MyLogger.LogInfo("TestPlayerController: Set tag to 'Player' for AI detection");
        }
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        if (usePhysicsMovement)
        {
            MoveWithPhysics();
        }
        else
        {
            MoveWithTransform();
        }
    }

    private void HandleInput()
    {
        // Get input
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D or Arrow Keys
        float vertical = Input.GetAxisRaw("Vertical");     // W/S or Arrow Keys

        // Calculate movement direction
        movement = new Vector3(horizontal, 0f, vertical).normalized;
        isMoving = movement.magnitude > 0.1f;

        // Rotate to face movement direction
        if (isMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void MoveWithPhysics()
    {
        if (isMoving)
        {
            // Apply force for movement
            Vector3 force = movement * moveSpeed;
            rb.linearVelocity = new Vector3(force.x, rb.linearVelocity.y, force.z);
        }
    }

    private void MoveWithTransform()
    {
        if (isMoving)
        {
            // Direct transform movement
            Vector3 moveVector = movement * moveSpeed * Time.fixedDeltaTime;
            transform.position += moveVector;
        }
    }

    private void OnDrawGizmos()
    {
        // Draw movement direction in Scene view
        if (isMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, movement * 2f);
        }

        // Draw detection range visualization (helps with AI testing)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }

    // Public properties for AI to access
    public Vector3 Velocity => rb.linearVelocity;
    public bool IsMoving => isMoving;
    public Vector3 MovementDirection => movement;

    // Debug info
    [Header("Debug Info (Runtime)")]
    [SerializeField, HideInInspector] private float currentSpeed;
    [SerializeField, HideInInspector] private Vector3 currentVelocity;

    void LateUpdate()
    {
        // Update debug info
        currentSpeed = rb.linearVelocity.magnitude;
        currentVelocity = rb.linearVelocity;
    }
}
