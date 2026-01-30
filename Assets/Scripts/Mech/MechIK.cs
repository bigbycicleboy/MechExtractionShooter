using Unity.Mathematics;
using UnityEngine;

public class QuadrupedMechIK : MonoBehaviour
{
    [System.Serializable]
    public class Leg
    {
        public Transform footTarget;
        public Transform footBone;
        public Transform kneeBone;
        public Transform kneeHint;
        
        [HideInInspector] public bool isMoving;
        [HideInInspector] public float lerpTime = 1f;
        [HideInInspector] public Vector3 startPos;
        [HideInInspector] public Vector3 endPos;
        [HideInInspector] public Quaternion startRot;
        [HideInInspector] public Quaternion endRot;
        [HideInInspector] public Vector3 defaultOffset;
    }
    
    [Header("Legs (Front Left, Front Right, Back Left, Back Right)")]
    [SerializeField] private Leg frontLeft;
    [SerializeField] private Leg frontRight;
    [SerializeField] private Leg backLeft;
    [SerializeField] private Leg backRight;
    
    [Header("Step Settings")]
    [SerializeField] private float stepHeight = 0.5f;
    [SerializeField] private float stepDistance = 1.5f;
    [SerializeField] private float stepSpeed = 8f;
    [SerializeField] private float bodyHeight = 2f;
    [SerializeField] private Vector3 footPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 footRotationOffset;
    
    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 3f;
    
    [Header("Body Tilt")]
    [SerializeField] private Transform bodyTransform;
    [SerializeField] private float tiltAmount = 0.1f;
    [SerializeField] private float tiltSpeed = 5f;
    
    [Header("Gait Settings")]
    [SerializeField] private bool useDiagonalGait = true; // true = trot, false = pace
    
    [Header("Surface Alignment")]
    [SerializeField] private float rotationSpeed = 10f;
    
    private Vector3 lastPosition;
    private Vector3 currentVelocity;
    private Leg[] allLegs;
    private int currentGaitPhase = 0;

    void Start()
    {
        lastPosition = transform.position;
        allLegs = new Leg[] { frontLeft, frontRight, backLeft, backRight };
        
        // Initialize and plant all feet on the ground
        foreach (var leg in allLegs)
        {
            if (leg != null && leg.footTarget != null && leg.footBone != null)
            {
                // Position target under the foot bone
                Vector3 footPos = leg.footBone.position;
                
                // Raycast down to find ground
                RaycastHit hit;
                if (Physics.Raycast(footPos + Vector3.up * 2f, Vector3.down, out hit, groundCheckDistance, groundLayer))
                {
                    leg.footTarget.position = hit.point;
                }
                else
                {
                    footPos.y = transform.position.y - bodyHeight;
                    leg.footTarget.position = footPos;
                }
                
                leg.footTarget.rotation = transform.rotation;

                leg.defaultOffset = transform.InverseTransformPoint(leg.footTarget.position);
            }
        }
    }    
    
    void Update()
    {
        CalculateVelocity();
        UpdateFootIK();
        UpdateKneeHints();
        UpdateBodyTilt();
    }

    void CalculateVelocity()
    {
        currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
    }

    void UpdateFootIK()
    {
        // Check if mech is moving
        bool isMoving = currentVelocity.magnitude > 0.1f;
        
        if (!isMoving)
        {
            // Plant all feet on ground when stationary
            foreach (var leg in allLegs)
            {
                PlantFoot(leg);
            }
            return;
        }
        
        // Check if any leg needs to step
        bool anyLegMoving = false;
        foreach (var leg in allLegs)
        {
            if (leg.isMoving)
            {
                anyLegMoving = true;
                break;
            }
        }
        
        // Start new step cycle if no legs are moving
        if (!anyLegMoving)
        {
            TryStartGaitCycle();
        }
        
        // Update all moving legs
        foreach (var leg in allLegs)
        {
            if (leg.isMoving)
            {
                UpdateStep(leg);
            }
        }
    }

    void TryStartGaitCycle()
    {
        // Determine which legs should move based on gait pattern
        Leg[] legsToMove = null;
        
        if (useDiagonalGait)
        {
            // Diagonal gait (trot): FL+BR, then FR+BL
            if (currentGaitPhase == 0)
            {
                // Check if front-left or back-right need stepping
                if (ShouldStep(frontLeft) || ShouldStep(backRight))
                {
                    legsToMove = new Leg[] { frontLeft, backRight };
                    currentGaitPhase = 1;
                }
            }
            else
            {
                // Check if front-right or back-left need stepping
                if (ShouldStep(frontRight) || ShouldStep(backLeft))
                {
                    legsToMove = new Leg[] { frontRight, backLeft };
                    currentGaitPhase = 0;
                }
            }
        }
        else
        {
            // Lateral gait (pace): FL+FR, then BL+BR
            if (currentGaitPhase == 0)
            {
                if (ShouldStep(frontLeft) || ShouldStep(frontRight))
                {
                    legsToMove = new Leg[] { frontLeft, frontRight };
                    currentGaitPhase = 1;
                }
            }
            else
            {
                if (ShouldStep(backLeft) || ShouldStep(backRight))
                {
                    legsToMove = new Leg[] { backLeft, backRight };
                    currentGaitPhase = 0;
                }
            }
        }
        
        // Start stepping for selected legs
        if (legsToMove != null)
        {
            foreach (var leg in legsToMove)
            {
                StartStep(leg);
            }
        }
    }

    bool ShouldStep(Leg leg)
    {
        if (leg == null || leg.footTarget == null) return false;
        
        // Calculate ideal foot position
        Vector3 idealPos = CalculateFootTarget(leg, out _);
        float distance = Vector3.Distance(leg.footTarget.position, idealPos);
        
        return distance > stepDistance;
    }

    void StartStep(Leg leg)
    {
        if (leg == null || leg.footTarget == null) return;
        
        leg.isMoving = true;
        leg.lerpTime = 0f;
        leg.startPos = leg.footTarget.position;
        leg.startRot = leg.footTarget.rotation;
        
        Quaternion endRotation;
        leg.endPos = CalculateFootTarget(leg, out endRotation);
        leg.endRot = endRotation;
    }

    void UpdateStep(Leg leg)
    {
        leg.lerpTime += Time.deltaTime * stepSpeed;
        
        if (leg.lerpTime >= 1f)
        {
            // When landing, make sure we're above ground
            leg.footTarget.position = leg.endPos;
            leg.footTarget.rotation = leg.endRot;
            leg.isMoving = false;
            leg.lerpTime = 1f;
            return;
        }
        
        // Arc trajectory for stepping
        Vector3 currentPos = Vector3.Lerp(leg.startPos, leg.endPos, leg.lerpTime);
        
        // Make sure the arc goes HIGH enough to clear the ground
        float arc = Mathf.Sin(leg.lerpTime * Mathf.PI) * stepHeight;
        currentPos.y += arc;
        
        // Clamp to never go below the end position's Y
        currentPos.y = Mathf.Max(currentPos.y, leg.endPos.y);
        
        leg.footTarget.position = currentPos;
        leg.footTarget.rotation = Quaternion.Slerp(leg.startRot, leg.endRot, leg.lerpTime);
    }

    Vector3 CalculateFootTarget(Leg leg, out Quaternion targetRotation)
    {
        targetRotation = Quaternion.identity;
        
        if (leg == null || leg.footBone == null)
        {
            return Vector3.zero;
        }
        
        Vector3 targetPos = transform.TransformPoint(leg.defaultOffset);
        Vector3 anticipatedPos = targetPos + currentVelocity.normalized * 0.3f;
        
        // Convert local offset to world space
        Vector3 worldOffset = transform.TransformDirection(footPositionOffset);
        
        RaycastHit hit;
        if (Physics.Raycast(anticipatedPos + Vector3.up * 2f, Vector3.down, out hit, groundCheckDistance, groundLayer))
        {
            targetRotation = transform.rotation * Quaternion.Euler(footRotationOffset);
            
            // Apply local offset in world space
            return hit.point + Vector3.up * 0.15f + worldOffset;
        }
        
        anticipatedPos.y = transform.position.y - bodyHeight + 0.15f;
        targetRotation = transform.rotation * Quaternion.Euler(footRotationOffset);
        return anticipatedPos + worldOffset;
    }

    void PlantFoot(Leg leg)
    {
        if (leg == null || leg.footTarget == null || leg.footBone == null) return;
        
        Vector3 desiredPos = leg.footBone.position;
        
        // Convert local offset to world space
        Vector3 worldOffset = transform.TransformDirection(footPositionOffset);
        
        RaycastHit hit;
        if (Physics.Raycast(desiredPos + Vector3.up * 2f, Vector3.down, out hit, groundCheckDistance, groundLayer))
        {
            Vector3 footPos = hit.point + Vector3.up * 0.1f + worldOffset;
            leg.footTarget.position = Vector3.Lerp(leg.footTarget.position, footPos, Time.deltaTime * 5f);
            
            Quaternion targetRotation = transform.rotation * Quaternion.Euler(180f, 0f, 0f);
            leg.footTarget.rotation = Quaternion.Slerp(leg.footTarget.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
    
    void UpdateKneeHints()
    {
        UpdateKneeHint(frontLeft, true, -1);
        UpdateKneeHint(frontRight, true, 1);
        UpdateKneeHint(backLeft, false, -1);
        UpdateKneeHint(backRight, false, 1);
    }

    void UpdateKneeHint(Leg leg, bool isFront, float sideMultiplier)
    {
        if (leg == null || leg.kneeHint == null || leg.kneeBone == null) return;
        
        float forwardOffset = isFront ? 0.5f : -0.5f;
        Vector3 kneePos = leg.kneeBone.position + transform.forward * forwardOffset + transform.right * (0.3f * sideMultiplier);
        leg.kneeHint.position = kneePos;
    }

    void UpdateBodyTilt()
    {
        if (bodyTransform == null) return;
        
        // Tilt body based on velocity
        Vector3 localVel = transform.InverseTransformDirection(currentVelocity);
        
        // Forward/backward tilt
        float pitchTilt = -localVel.z * tiltAmount;
        // Side tilt
        float rollTilt = -localVel.x * tiltAmount;
        
        Quaternion targetRotation = Quaternion.Euler(pitchTilt, 0f, rollTilt);
        bodyTransform.localRotation = Quaternion.Slerp(bodyTransform.localRotation, targetRotation, Time.deltaTime * tiltSpeed);
    }

    void OnDrawGizmosSelected()
    {
        // Visualize foot targets with different colors
        if (frontLeft?.footTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(frontLeft.footTarget.position, 0.1f);
            DrawOrientationGizmo(frontLeft.footTarget);
        }
        
        if (frontRight?.footTarget != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(frontRight.footTarget.position, 0.1f);
            DrawOrientationGizmo(frontRight.footTarget);
        }
        
        if (backLeft?.footTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(backLeft.footTarget.position, 0.1f);
            DrawOrientationGizmo(backLeft.footTarget);
        }
        
        if (backRight?.footTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(backRight.footTarget.position, 0.1f);
            DrawOrientationGizmo(backRight.footTarget);
        }
        
        // Visualize step distance
        Gizmos.color = Color.cyan;
        if (transform != null)
        {
            Gizmos.DrawWireSphere(transform.position, stepDistance);
        }
    }
    
    void DrawOrientationGizmo(Transform t)
    {
        if (t == null) return;
        
        // Draw orientation arrows
        Gizmos.color = Color.red;
        Gizmos.DrawLine(t.position, t.position + t.right * 0.2f);
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(t.position, t.position + t.up * 0.2f);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(t.position, t.position + t.forward * 0.2f);
    }
    
    // Public getter for other scripts to access velocity
    public Vector3 GetVelocity()
    {
        return currentVelocity;
    }
}