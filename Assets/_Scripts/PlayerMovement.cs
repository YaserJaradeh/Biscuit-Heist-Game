using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
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

    Rigidbody2D _rb;
    Vector2 _input;
    bool _wantsSprint;
    bool _wantToHide;
    Vector2 _velocity;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        // Recommended Rigidbody2D settings for top-down:
        // Body Type: Dynamic
        // Gravity Scale: 0
        // Interpolate: Interpolate
        // Collision Detection: Continuous
        // Freeze Z Rotation if you rotate via FlipX
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
            if (animator)
            {
                animator.SetBool("IsHiding", true);
                animator.SetBool("IsMoving", false);
            }
        }
        else
        {
            _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);
            if (animator)
            {
                animator.SetBool("IsMoving", _input.sqrMagnitude > 0.01f);
                animator.SetBool("IsHiding", false);
            }
        }

        // Always face movement direction
        FaceMovement();
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
