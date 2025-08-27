using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4.0f;
    public float sprintSpeed = 6.5f;
    [Tooltip("How quickly velocity reaches the target (higher = snappier).")]
    public float acceleration = 20f; // units/sec^2
    [Tooltip("Extra damping when there's no input (helps quick stops).")]
    public float idleDamping = 15f;

    [Header("Facing")]
    [Tooltip("Provide your SpriteRenderer for sprite flipping.")]
    public SpriteRenderer spriteRenderer;

    [Header("Input")]
    public KeyCode sprintKey = KeyCode.LeftShift;

    public KeyCode hideKey = KeyCode.C;
    
    [Header("Animation")]
    [Tooltip("Provide the Animator to set variables.")]
    [SerializeField] Animator animator;

    [Header("Collision")]
    [Tooltip("Layers that block player movement")]
    public LayerMask collisionLayers = -1;

    Rigidbody2D _rb;
    Collider2D[] _colliders;
    Vector2 _input;
    bool _wantsSprint;
    bool _wantToHide;
    Vector2 _velocity;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _colliders = GetComponents<Collider2D>();
    }

    void Update()
    {
        // Read input in Update
        float ix = Input.GetAxisRaw("Horizontal");
        float iy = Input.GetAxisRaw("Vertical");
        _input = Vector2.ClampMagnitude(new Vector2(ix, iy), 1f);

        _wantsSprint = Input.GetKey(sprintKey);
        _wantToHide = Input.GetKey(hideKey);
    }

    void FixedUpdate()
    {
        float targetSpeed = _wantsSprint ? sprintSpeed : walkSpeed;
        Vector2 targetVel = _input * targetSpeed;

        // Choose acceleration depending on input (snappier when moving, more damping when idle)
        float accel = _input.sqrMagnitude > 0.0001f ? acceleration : idleDamping;

        // Exponential smoothing toward target velocity
        float t = 1f - Mathf.Exp(-accel * Time.fixedDeltaTime);
        _velocity = Vector2.Lerp(_velocity, targetVel, t);

        // Move or Hide
        if (_wantToHide)
        {
            // If hiding, stop movement
            _velocity = Vector2.zero;
            animator.SetBool("IsHiding", true);
            animator.SetBool("IsMoving", false);
        }
        else
        {
            // Check for collisions before moving
            Vector2 deltaPosition = _velocity * Time.fixedDeltaTime;
            Vector2 newPosition = _rb.position + deltaPosition;
            
            // Only move if the path is clear
            if (CanMoveTo(newPosition))
            {
                _rb.MovePosition(newPosition);
                animator.SetBool("IsMoving", _input.sqrMagnitude > 0.01f);
                animator.SetBool("IsHiding", false);
            }
            else
            {
                animator.SetBool("IsMoving", false);
            }
        }

        // Always face movement direction
        FaceMovement();
    }

    bool CanMoveTo(Vector2 targetPosition)
    {
        // Check each collider on the player
        foreach (Collider2D playerCollider in _colliders)
        {
            if (!playerCollider.enabled) continue; // Skip disabled colliders
            
            // Calculate where this specific collider would be at the target position
            Vector2 colliderOffset = (Vector2)playerCollider.bounds.center - _rb.position;
            Vector2 targetColliderCenter = targetPosition + colliderOffset;
            
            // Check if there's a collision at the target position for this collider
            Collider2D hit = Physics2D.OverlapBox(
                targetColliderCenter, 
                playerCollider.bounds.size, 
                0f, 
                collisionLayers
            );
            
            // If we hit something that isn't one of our own colliders, movement is blocked
            if (hit != null && !IsOwnCollider(hit))
            {
                Debug.Log("Movement blocked by " + hit.name);
                return false;
            }
        }
        
        return true; // All colliders are clear
    }
    
    bool IsOwnCollider(Collider2D collider)
    {
        // Check if the collider belongs to this player
        foreach (Collider2D playerCollider in _colliders)
        {
            if (collider == playerCollider)
                return true;
        }
        return false;
    }

    void FaceMovement()
    {
        if (_velocity.sqrMagnitude < 0.0001f) return;

        if (spriteRenderer != null)
        {
            // Flip sprite based on horizontal movement direction
            // Face right when moving right (positive X), face left when moving left (negative X)
            if (Mathf.Abs(_velocity.x) > 0.001f)
            {
                bool faceRight = _velocity.x > 0f;
                spriteRenderer.flipX = !faceRight;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        
        if (other.gameObject.CompareTag("Cookie"))
        {
            // Consume the cookie
            Destroy(other.gameObject);
            GameManager.Instance.CollectCookie();
        }
        else if (other.gameObject.CompareTag("Exit"))
        {
            // Exit the level
            Debug.Log("Level Complete!");
            Destroy(gameObject);
            GameManager.Instance.ExitRoom();
        }
        
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        walkSpeed = Mathf.Max(0f, walkSpeed);
        sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
        acceleration = Mathf.Max(0f, acceleration);
        idleDamping = Mathf.Max(0f, idleDamping);
    }
#endif
}
