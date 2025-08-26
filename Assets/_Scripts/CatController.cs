using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CatController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3.0f;
    [Tooltip("How quickly velocity reaches the target (higher = snappier).")]
    public float acceleration = 15f;
    [Tooltip("Extra damping when there's no input (helps quick stops).")]
    public float idleDamping = 10f;
    [Tooltip("Distance to target before considering it reached.")]
    public float arrivalDistance = 0.5f;

    [Header("AI Behavior")]
    [Tooltip("Time to wait at each point of interest before moving to the next.")]
    public float waitTime = 2f;
    [Tooltip("Points of interest that the cat will move between.")]
    public List<Transform> pointsOfInterest = new List<Transform>();
    [Tooltip("Should the cat randomly choose the next point or go in order?")]
    public bool randomSelection = true;

    [Header("Facing")]
    [Tooltip("Provide your SpriteRenderer for sprite flipping.")]
    public SpriteRenderer spriteRenderer;

    [Header("Animation")]
    [Tooltip("Provide the Animator to set variables.")]
    [SerializeField] Animator animator;

    Rigidbody2D _rb;
    Vector2 _velocity;
    Transform _currentTarget;
    int _currentTargetIndex = 0;
    float _waitTimer = 0f;
    bool _isWaiting = false;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Start with the first point of interest if available
        if (pointsOfInterest.Count > 0)
        {
            SetNextTarget();
        }
    }

    void Update()
    {
        HandleAI();
    }

    void FixedUpdate()
    {
        if (_isWaiting)
        {
            // Stop movement while waiting
            StopMovement();
        }
        else if (_currentTarget != null)
        {
            // Move towards current target
            MoveTowardsTarget();
        }
        else
        {
            // No target, stop movement
            StopMovement();
        }

        // Always face movement direction
        FaceMovement();
    }

    void HandleAI()
    {
        if (pointsOfInterest.Count == 0) return;

        if (_isWaiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _isWaiting = false;
                SetNextTarget();
            }
        }
        else if (_currentTarget != null)
        {
            // Check if we've reached the current target
            float distanceToTarget = Vector2.Distance(transform.position, _currentTarget.position);
            if (distanceToTarget <= arrivalDistance)
            {
                // Reached target, start waiting
                _isWaiting = true;
                _waitTimer = waitTime;
                
                if (animator)
                {
                    animator.SetBool("IsSeeking", false);
                }
            }
        }
    }

    void SetNextTarget()
    {
        if (pointsOfInterest.Count == 0) return;

        if (randomSelection)
        {
            // Choose a random point that's different from current target
            Transform newTarget;
            do
            {
                _currentTargetIndex = Random.Range(0, pointsOfInterest.Count);
                newTarget = pointsOfInterest[_currentTargetIndex];
            } while (newTarget == _currentTarget && pointsOfInterest.Count > 1);
            
            _currentTarget = newTarget;
        }
        else
        {
            // Go to next point in order
            _currentTargetIndex = (_currentTargetIndex + 1) % pointsOfInterest.Count;
            _currentTarget = pointsOfInterest[_currentTargetIndex];
        }

        if (animator)
        {
            animator.SetBool("IsSeeking", true);
        }
    }

    void MoveTowardsTarget()
    {
        if (_currentTarget == null) return;

        Vector2 direction = ((Vector2)_currentTarget.position - _rb.position).normalized;
        Vector2 targetVel = direction * walkSpeed;

        // Exponential smoothing toward target velocity
        float t = 1f - Mathf.Exp(-acceleration * Time.fixedDeltaTime);
        _velocity = Vector2.Lerp(_velocity, targetVel, t);

        _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);
    }

    void StopMovement()
    {
        // Apply damping to smoothly stop
        float t = 1f - Mathf.Exp(-idleDamping * Time.fixedDeltaTime);
        _velocity = Vector2.Lerp(_velocity, Vector2.zero, t);
        
        _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);
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

    public void AddPointOfInterest(Transform point)
    {
        if (point != null && !pointsOfInterest.Contains(point))
        {
            pointsOfInterest.Add(point);
        }
    }

    public void RemovePointOfInterest(Transform point)
    {
        if (pointsOfInterest.Contains(point))
        {
            pointsOfInterest.Remove(point);
            
            // If we removed the current target, find a new one
            if (_currentTarget == point)
            {
                if (pointsOfInterest.Count > 0)
                {
                    SetNextTarget();
                }
                else
                {
                    _currentTarget = null;
                    if (animator)
                    {
                        animator.SetBool("IsSeeking", false);
                    }
                }
            }
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        walkSpeed = Mathf.Max(0f, walkSpeed);
        acceleration = Mathf.Max(0f, acceleration);
        idleDamping = Mathf.Max(0f, idleDamping);
        arrivalDistance = Mathf.Max(0.1f, arrivalDistance);
        waitTime = Mathf.Max(0f, waitTime);
    }

    void OnDrawGizmosSelected()
    {
        // Draw lines to points of interest
        Gizmos.color = Color.cyan;
        foreach (Transform point in pointsOfInterest)
        {
            if (point != null)
            {
                Gizmos.DrawLine(transform.position, point.position);
                Gizmos.DrawWireSphere(point.position, 0.2f);
            }
        }

        // Highlight current target
        if (_currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_currentTarget.position, 0.3f);
            Gizmos.DrawLine(transform.position, _currentTarget.position);
        }

        // Draw arrival distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, arrivalDistance);
    }
#endif
}
