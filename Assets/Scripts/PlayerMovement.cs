using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float rotateSpeed = 720f;


    private Vector2 moveInput;
    private PlayerController playerControls;
    private Rigidbody rb;

    private bool isBlocking = false;
    public bool isSprinting { get; private set; }  // Public getter, private setter

    [Header("Attack Settings")]
    public float punchDamage = 10f; // Damage dealt by punch
    public Transform punchPoint; // Position of punch (where we want to check for collisions)
    public float punchRange = 1f; // How far the punch can reach
    public LayerMask enemyLayer; // Layer of enemies to detect

    void Awake()
    {
        playerControls = new PlayerController();
        rb = GetComponent<Rigidbody>();

        // Subscribe to actions
        playerControls.Player.Punch.performed += ctx => Punch();
        playerControls.Player.Block.performed += ctx => StartBlocking();
        playerControls.Player.Block.canceled += ctx => StopBlocking();
    }

    void OnEnable()
    {
        playerControls.Player.Enable();
    }

    void OnDisable()
    {
        playerControls.Player.Disable();
    }

    void FixedUpdate()
    {
        if (isBlocking)
        {
            // Optional: Don't move while blocking
            return;
        }

        moveInput = playerControls.Player.Move.ReadValue<Vector2>();

        float h = moveInput.x;
        float v = moveInput.y;
        Vector3 moveDir = new Vector3(h, 0f, v).normalized;

        isSprinting = Keyboard.current.leftShiftKey.isPressed;
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        if (moveDir != Vector3.zero)
        {
            rb.MovePosition(transform.position + moveDir * currentSpeed * Time.fixedDeltaTime);

            Quaternion toRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotateSpeed * Time.fixedDeltaTime);
        }
    }

    void Punch()
    {
        if (!isBlocking)
        {
            Debug.Log("Player punches!");

            // Check for enemy within punch range
            //Collider[] hitEnemies = Physics.OverlapSphere(punchPoint.position, punchRange);

            //Collider[] hitEnemies = Physics.OverlapSphere(punchPoint.position, punchRange, enemyLayer);
            Collider[] hitEnemies = Physics.OverlapSphere(punchPoint.position, punchRange, enemyLayer);
            Debug.Log($"Enemies hit: {hitEnemies.Length}");

            foreach (Collider enemy in hitEnemies)
            {
                if (enemy.CompareTag("Enemy"))
                {
                    // Deal damage to enemy
                    HealthSystem enemyHealth = enemy.GetComponent<HealthSystem>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.TakeDamage(punchDamage, enemyHealth.isBlocking); // ✅ Fixed line
                    }
                }
            }
        }
    }

    void StartBlocking()
    {
        isBlocking = true;
        Debug.Log("Player is blocking!");
        // TODO: Trigger block animation or effects here
    }

    void StopBlocking()
    {
        isBlocking = false;
        Debug.Log("Player stopped blocking!");
        // TODO: Reset animation or effects
    }
    void OnDrawGizmosSelected()
    {
        if (punchPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(punchPoint.position, punchRange);
        }
    }

}
