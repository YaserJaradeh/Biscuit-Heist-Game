using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FollowPlayer : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    [Tooltip("Optional: If provided, we use velocity for lookahead. Otherwise, derive from target motion.")]
    public Rigidbody2D targetRb;

    [Header("Follow Smoothing")]
    [Tooltip("How smoothly the camera follows the target (0 = instant, 1 = very slow).")]
    [Range(0f, 0.95f)] public float followSmoothing = 0.1f;

    [Header("Lookahead")]
    [Tooltip("Scale for velocity-based lookahead.")]
    public float lookaheadDistance = 1.5f;
    [Tooltip("Time to ease into/out of lookahead.")]
    public float lookaheadSmoothing = 0.25f;
    [Tooltip("Clamp for lookahead (world units).")]
    public Vector2 lookaheadClamp = new Vector2(2.5f, 1.8f);

    [Header("Camera Bounds")]
    [Tooltip("Optional: Box Collider 2D that defines camera movement bounds. Camera will be clamped within these bounds.")]
    public BoxCollider2D boundingBox;

    [Header("Zoom (optional)")]
    public bool enableSpeedZoom = false;
    [Tooltip("Orthographic size at rest.")]
    public float baseOrthoSize = 5f;
    [Tooltip("Extra size added at maxSpeedForZoom.")]
    public float zoomOutExtra = 1.5f;
    [Tooltip("Speed at which zoom reaches max extra.")]
    public float maxSpeedForZoom = 12f;
    [Tooltip("Zoom smoothing time.")]
    public float zoomSmoothTime = 0.25f;

    [Header("Settle Animation")]
    [Tooltip("Small damped bounce when the target stops quickly.")]
    public bool settleOnStops = true;
    [Tooltip("Threshold for 'hard stop' detection.")]
    public float stopSpeedThreshold = 8f;
    [Tooltip("Settle amplitude (units).")]
    public float settleAmplitude = 0.25f;
    [Tooltip("Settle decay rate.")]
    public float settleDamp = 9f;
    [Tooltip("Settle oscillation frequency (Hz).")]
    public float settleFrequency = 6f;

    [Header("Screen Shake")]
    [Tooltip("Base shake amplitude (units). Call Shake() to use.")]
    public float defaultShakeAmplitude = 0.25f;
    [Tooltip("Decay per second for shakes.")]
    public float shakeDecay = 3f;

    Camera _cam;
    Vector2 _smoothedLookahead;     // eased lookahead
    Vector3 _lastTargetPos;
    bool _hadLastPos;
    Vector2 _lastVelocity;          // for velocity smoothing when no Rigidbody2D

    // Settle anim state
    float _settleTime;
    float _settleStrength;          // current amplitude
    Vector2 _settleDir;

    // Shake state
    float _shakeStrength;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _cam.orthographic = true;
        if (baseOrthoSize <= 0f) baseOrthoSize = _cam.orthographicSize;
        if (!targetRb) targetRb = target.GetComponent<Rigidbody2D>();
    }

    void LateUpdate()
    {
        if (!target) return;

        // --- Derive target position & velocity ---
        Vector3 tpos = target.position;
        Vector2 vel = Vector2.zero;

        if (targetRb)
        {
            vel = targetRb.linearVelocity;
        }
        else if (_hadLastPos)
        {
            Vector2 raw = (tpos - _lastTargetPos) / Mathf.Max(Time.deltaTime, 1e-6f);
            // Smooth velocity to avoid jitter when no Rigidbody2D is available
            vel = Vector2.Lerp(_lastVelocity, raw, 0.6f);
            _lastVelocity = vel;
        }

        // --- Lookahead (velocity-based) ---
        Vector2 desiredLookahead = vel * lookaheadDistance;
        desiredLookahead = new Vector2(
            Mathf.Clamp(desiredLookahead.x, -lookaheadClamp.x, lookaheadClamp.x),
            Mathf.Clamp(desiredLookahead.y, -lookaheadClamp.y, lookaheadClamp.y)
        );

        float laT = 1f - Mathf.Exp(-Mathf.Max(0.01f, 1f / Mathf.Max(lookaheadSmoothing, 0.0001f)) * Time.deltaTime);
        _smoothedLookahead = Vector2.Lerp(_smoothedLookahead, desiredLookahead, laT);

        // --- Direct follow with lookahead ---
        Vector3 camPos = transform.position;
        Vector2 camXY = new Vector2(camPos.x, camPos.y);
        Vector2 targetWithLook = new Vector2(tpos.x, tpos.y) + _smoothedLookahead;
        Vector2 desiredCam = targetWithLook;

        // --- Settle animation on hard stops ---
        if (settleOnStops)
        {
            // detect hard stop: high speed → very low speed in a short time
            float spd = vel.magnitude;
            if (spd < 0.1f && _hadLastPos)
            {
                // compute previous speed estimate
                Vector2 prevVel = (Vector2)((_lastTargetPos - tpos) / Mathf.Max(Time.deltaTime, 1e-6f)) * -1f;
                if (prevVel.magnitude > stopSpeedThreshold)
                {
                    _settleTime = 0f;
                    _settleStrength = settleAmplitude;
                    _settleDir = prevVel.normalized; // nudge in travel direction
                }
            }

            // decay settle over time
            if (_settleStrength > 0.0001f)
            {
                _settleTime += Time.deltaTime;
                float oscillation = Mathf.Sin(_settleTime * Mathf.PI * 2f * settleFrequency);
                Vector2 settleOffset = _settleDir * oscillation * _settleStrength;
                desiredCam += settleOffset;
                _settleStrength = Mathf.MoveTowards(_settleStrength, 0f, settleDamp * Time.deltaTime);
            }
        }

        Vector2 current = camXY;
        
        // --- Smooth follow with gentle damping ---
        float smoothT = 1f - Mathf.Pow(followSmoothing, Time.deltaTime * 60f); // Frame-rate independent
        current = Vector2.Lerp(current, desiredCam, smoothT);

        // --- Apply camera bounds clamping ---
        if (boundingBox != null)
        {
            current = ClampCameraToBounds(current);
        }

        // --- Screen shake (applied last) ---
        Vector2 shake = Vector2.zero;
        if (_shakeStrength > 0.0001f)
        {
            shake = new Vector2(
                (Mathf.PerlinNoise(Time.time * 19.7f, 1.23f) - 0.5f),
                (Mathf.PerlinNoise(Time.time * 23.1f, 7.89f) - 0.5f)
            ) * (_shakeStrength * 2f);
            _shakeStrength = Mathf.MoveTowards(_shakeStrength, 0f, shakeDecay * Time.deltaTime);
        }

        Vector3 finalPos = new Vector3(current.x + shake.x, current.y + shake.y, camPos.z);
        transform.position = finalPos;

        // --- Speed-based zoom ---
        if (enableSpeedZoom)
        {
            float speedRatio = Mathf.Clamp01(vel.magnitude / Mathf.Max(0.01f, maxSpeedForZoom));
            float targetSize = baseOrthoSize + speedRatio * zoomOutExtra;
            
            // Use simpler, more reliable smoothing
            float zoomSpeed = 1f / Mathf.Max(0.01f, zoomSmoothTime);
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, targetSize, zoomSpeed * Time.deltaTime);
        }

        _lastTargetPos = tpos;
        _hadLastPos = true;
    }

    /// <summary>
    /// Clamps the camera position to stay within the bounds of the specified BoxCollider2D.
    /// Takes into account the camera's orthographic size to ensure the camera view doesn't go outside the bounds.
    /// </summary>
    /// <param name="cameraPosition">The desired camera position</param>
    /// <returns>The clamped camera position</returns>
    Vector2 ClampCameraToBounds(Vector2 cameraPosition)
    {
        if (boundingBox == null) return cameraPosition;

        // Get the bounds of the BoxCollider2D
        Bounds bounds = boundingBox.bounds;
        
        // Calculate camera view extents based on orthographic size and aspect ratio
        float cameraHeight = _cam.orthographicSize * 2f;
        float cameraWidth = cameraHeight * _cam.aspect;
        
        // Calculate the half extents of the camera view
        float halfCameraWidth = cameraWidth * 0.5f;
        float halfCameraHeight = cameraHeight * 0.5f;
        
        // Calculate the min and max positions the camera center can be at
        float minX = bounds.min.x + halfCameraWidth;
        float maxX = bounds.max.x - halfCameraWidth;
        float minY = bounds.min.y + halfCameraHeight;
        float maxY = bounds.max.y - halfCameraHeight;
        
        // If the bounding area is smaller than the camera view, center the camera
        if (minX > maxX) 
        {
            float centerX = (bounds.min.x + bounds.max.x) * 0.5f;
            cameraPosition.x = centerX;
        }
        else
        {
            cameraPosition.x = Mathf.Clamp(cameraPosition.x, minX, maxX);
        }
        
        if (minY > maxY) 
        {
            float centerY = (bounds.min.y + bounds.max.y) * 0.5f;
            cameraPosition.y = centerY;
        }
        else
        {
            cameraPosition.y = Mathf.Clamp(cameraPosition.y, minY, maxY);
        }
        
        return cameraPosition;
    }

    /// <summary>
    /// Triggers a short screenshake. Amplitude is in world units.
    /// </summary>
    public void Shake(float amplitude = -1f)
    {
        _shakeStrength = Mathf.Max(_shakeStrength, amplitude > 0f ? amplitude : defaultShakeAmplitude);
    }
}
