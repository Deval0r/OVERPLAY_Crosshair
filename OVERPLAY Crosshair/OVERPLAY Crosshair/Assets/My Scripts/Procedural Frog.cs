using UnityEngine;

public class ProceduralFrog : MonoBehaviour
{
    [Header("Frog Parts")]
    public Transform head;
    public Transform body;
    public Transform headParent; // Parent object containing head colliders
    public Transform frontLeftLeg;
    public Transform frontRightLeg;
    public Transform backLeftLeg;
    public Transform backRightLeg;

    [Header("Body Follow Settings")]
    public float bodyFollowSpeed = 15f; // Increased from 5f
    public float bodySquishAmount = 0.2f;
    public float bodySquishSpeed = 5f;
    public float bodyOffsetDistance = 0.5f; // Distance body tries to stay from head
    public float bodyFollowDistance = 1f; // Distance threshold for dynamic speed
    public float fastLerpThreshold = 2.5f; // Distance at which to lerp much faster
    public float fastLerpMultiplier = 5f; // How much faster to lerp when above threshold
    public float headVelocityLerpMultiplier = 1.5f; // Multiplier for lerp speed based on head velocity
    public float bodyTrailThreshold = 0.5f; // Head velocity above which body trails velocity
    public float bodyTrailLerp = 8f; // How fast the body rotates to trail
    public float bodyDownwardBias = 0.7f; // 0 = no bias, 1 = always downward when settled
    public float bodyDownwardBiasVelocity = 1.0f; // Head velocity below which downward bias is strongest

    [Header("Jump Settings")]
    public float jumpForce = 5f;
    public float jumpCooldown = 2f;
    public float jumpRandomness = 1f;
    public float gravity = 9.8f;

    [Header("Head Ground Level")]
    public bool enableHeadGroundLevel = true;
    public float headGroundLevel = -0.5f; // Y level where head tries to rest
    public float headGroundForce = 10f; // Force applied when below ground level
    public float headGroundDamping = 0.8f; // Damping when above ground level

    [Header("Collision Settings")]
    public bool ignoreHeadBodyCollision = true;

    [Header("Limb Settings")]
    public float limbBaseDistance = 0.5f;
    public float limbWiggleAmount = 0.1f;
    public float limbWiggleSpeed = 2f;
    public float limbLength = 0.7f;
    public Vector3 frontLeftLegOffset = Vector3.zero;
    public Vector3 frontRightLegOffset = Vector3.zero;
    public Vector3 backLeftLegOffset = Vector3.zero;
    public Vector3 backRightLegOffset = Vector3.zero;



    private Vector3 bodyDefaultScale;
    private float jumpTimer;
    private bool isJumping;
    private Vector3 jumpVelocity;
    private Vector3 frogBasePosition;
    private Vector3 lastHeadPosition;
    private float bodyAngle = -90f; // Start facing downward

    // (Removed: Drag logic and direct manipulation of head)

    void Start()
    {
        if (body != null)
            bodyDefaultScale = body.localScale;
        jumpTimer = Random.Range(jumpCooldown, jumpCooldown + jumpRandomness);
        if (head != null)
        {
            frogBasePosition = head.position;
            lastHeadPosition = head.position;
        }
        
        // Set up collision ignoring between head and body
        SetupCollisionIgnoring();
    }

    void SetupCollisionIgnoring()
    {
        if (!ignoreHeadBodyCollision) return;
        
        if (body != null)
        {
            Collider2D bodyCollider = body.GetComponent<Collider2D>();
            
            // Get colliders from head parent if assigned, otherwise from head
            Transform colliderSource = headParent != null ? headParent : head;
            
            if (colliderSource != null && bodyCollider != null)
            {
                // Get all Collider2D components from the source and all its children
                Collider2D[] headColliders = colliderSource.GetComponentsInChildren<Collider2D>();
                
                if (headColliders.Length > 0)
                {
                    foreach (Collider2D headCollider in headColliders)
                    {
                        Physics2D.IgnoreCollision(headCollider, bodyCollider, true);
                    }

                }

            }

        }
    }

    void Update()
    {
        HandleHeadGroundLevel();
        HandleBodyFollowAndSquish();
        HandleLimbPositioning();
        HandleJumping();
    }

    void HandleHeadGroundLevel()
    {
        if (!enableHeadGroundLevel || head == null) return;

        Rigidbody2D headRb = head.GetComponent<Rigidbody2D>();
        if (headRb == null) return;

        // Apply upward force when head is below ground level
        if (head.position.y < headGroundLevel)
        {
            float forceMultiplier = Mathf.Clamp01((headGroundLevel - head.position.y) / 1f); // Normalize force based on how far below
            Vector2 upwardForce = Vector2.up * headGroundForce * forceMultiplier;
            headRb.AddForce(upwardForce, ForceMode2D.Force);
        }
        // Apply damping when above ground level to prevent excessive bouncing
        else if (head.position.y > headGroundLevel + 0.1f && headRb.linearVelocity.y > 0)
        {
            headRb.linearVelocity = new Vector2(headRb.linearVelocity.x, headRb.linearVelocity.y * headGroundDamping);
        }
    }

    void HandleBodyFollowAndSquish()
    {
        if (body == null || head == null) 
        {
            return;
        }

        // Calculate head velocity
        float headVelocity = ((head.position - lastHeadPosition) / Time.deltaTime).magnitude;
        Vector3 headVelocityVec = (head.position - lastHeadPosition) / Time.deltaTime;
        lastHeadPosition = head.position;

        // Compute direction from body to head
        Vector3 toHead = head.position - body.position;
        float dist = toHead.magnitude;

        // --- Simple X-velocity based rotation ---
        // Calculate rotation based only on head's X velocity
        float xVelocity = headVelocityVec.x;
        float deadZone = 0.1f;
        
        if (Mathf.Abs(xVelocity) < deadZone)
        {
            // At rest, point downward (south = 180°)
            bodyAngle = Mathf.LerpAngle(bodyAngle, 180f, Time.deltaTime * 3f);
        }
        else
        {
            // Moving left (negative X) = rotate toward east (90°)
            // Moving right (positive X) = rotate toward west (270°)
            float targetAngle = xVelocity > 0 ? 270f : 90f;
            float velocityStrength = Mathf.Clamp01(Mathf.Abs(xVelocity) * 0.5f);
            float blendedAngle = Mathf.LerpAngle(180f, targetAngle, velocityStrength);
            bodyAngle = Mathf.LerpAngle(bodyAngle, blendedAngle, Time.deltaTime * 3f);
        }

        // Target position follows the body's rotation (coupled approach)
        Vector3 bodyDir = Quaternion.Euler(0, 0, -bodyAngle) * Vector3.down;
        Vector3 targetPos = head.position + bodyDir * bodyOffsetDistance;



        // Lerp speed increases with head velocity
        float dynamicFollowSpeed = bodyFollowSpeed * (1f + headVelocity * headVelocityLerpMultiplier);
        body.position = Vector3.Lerp(body.position, targetPos, Time.deltaTime * dynamicFollowSpeed);

        body.rotation = Quaternion.Euler(0, 0, -bodyAngle + 180f);

        // Squish effect based on distance
        float squish = 1 + Mathf.Sin(Time.time * bodySquishSpeed) * bodySquishAmount + Mathf.Clamp((dist - bodyOffsetDistance) * 0.5f, -bodySquishAmount, bodySquishAmount);
        body.localScale = new Vector3(bodyDefaultScale.x * (2 - squish), bodyDefaultScale.y * squish, bodyDefaultScale.z);
    }

    void HandleLimbPositioning()
    {
        if (body == null) return;
        float t = Time.time * limbWiggleSpeed;
        // Calculate base positions for limbs relative to body
        Vector3 up = body.up;
        Vector3 right = body.right;
        
        // Front legs
        if (frontLeftLeg != null)
        {
            Vector3 basePos = body.position + up * limbBaseDistance + right * -limbBaseDistance + frontLeftLegOffset;
            frontLeftLeg.position = basePos + up * Mathf.Sin(t) * limbWiggleAmount + right * Mathf.Cos(t) * limbWiggleAmount * 0.5f;
            frontLeftLeg.rotation = Quaternion.LookRotation(Vector3.forward, up + right * -0.5f);
        }
        
        if (frontRightLeg != null)
        {
            Vector3 basePos = body.position + up * limbBaseDistance + right * limbBaseDistance + frontRightLegOffset;
            frontRightLeg.position = basePos + up * Mathf.Sin(t + 1) * limbWiggleAmount + right * Mathf.Cos(t + 1) * limbWiggleAmount * 0.5f;
            frontRightLeg.rotation = Quaternion.LookRotation(Vector3.forward, up + right * 0.5f);
        }
        
        // Back legs
        if (backLeftLeg != null)
        {
            Vector3 basePos = body.position + up * -limbBaseDistance + right * -limbBaseDistance * 1.2f + backLeftLegOffset;
            backLeftLeg.position = basePos + up * Mathf.Sin(t + 2) * limbWiggleAmount + right * Mathf.Cos(t + 2) * limbWiggleAmount * 0.5f;
            backLeftLeg.rotation = Quaternion.LookRotation(Vector3.forward, up * -1 + right * -0.7f);
        }
        
        if (backRightLeg != null)
        {
            Vector3 basePos = body.position + up * -limbBaseDistance + right * limbBaseDistance * 1.2f + backRightLegOffset;
            backRightLeg.position = basePos + up * Mathf.Sin(t + 3) * limbWiggleAmount + right * Mathf.Cos(t + 3) * limbWiggleAmount * 0.5f;
            backRightLeg.rotation = Quaternion.LookRotation(Vector3.forward, up * -1 + right * 0.7f);
        }
    }

    void HandleJumping()
    {
        if (head == null || body == null) return;
        Rigidbody2D headRb = head.GetComponent<Rigidbody2D>();
        if (!isJumping)
        {
            jumpTimer -= Time.deltaTime;
            if (jumpTimer <= 0 && headRb != null)
            {
                // Start jump: apply force to head
                isJumping = true;
                Vector2 jumpDir = Vector2.up + Vector2.right * Random.Range(-0.2f, 0.2f);
                headRb.AddForce(jumpDir.normalized * jumpForce, ForceMode2D.Impulse);
                jumpTimer = Random.Range(jumpCooldown, jumpCooldown + jumpRandomness);
            }
        }
        else
        {
            // Check if landed (simple: y <= base y and falling)
            if (head.position.y <= frogBasePosition.y && headRb != null && headRb.linearVelocity.y <= 0.01f)
            {
                isJumping = false;
                // Optionally, reset y to base position
                // Vector3 pos = head.position;
                // head.position = new Vector3(pos.x, frogBasePosition.y, pos.z);
            }
        }
    }
}
