using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody))]
public class StayOnWalker : MonoBehaviourPun
{
    [Header("Walker Settings")]
    [SerializeField] private Transform walker;
    [SerializeField] private bool autoFindWalker = true;
    [SerializeField] private float groundCheckDistance = 1.5f;
    [SerializeField] private LayerMask walkerLayer;

    [Header("Alignment")]
    [SerializeField] private bool alignRotationToWalker = true;
    [SerializeField] private bool yawOnly = true;

    [Header("Anti-Slip")]
    [SerializeField] private float velocityCompensation = 1f;

    private bool isOnWalker;
    private Rigidbody rb;

    private Vector3 lastWalkerPos;
    private Quaternion lastWalkerRot;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine)
            return;

        if (!other.CompareTag("Walker"))
            return;

        isOnWalker = true;

        if (walker == null && autoFindWalker)
            walker = other.transform;

        lastWalkerPos = walker.position;
        lastWalkerRot = walker.rotation;
    }

    void OnTriggerExit(Collider other)
    {
        if (!photonView.IsMine)
            return;

        if (!other.CompareTag("Walker"))
            return;

        ExitWalker();
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine || !isOnWalker || walker == null)
            return;

        if (!IsStillOnWalker())
        {
            ExitWalker();
            return;
        }

        // ---- POSITION COMPENSATION ----
        Vector3 walkerDelta = walker.position - lastWalkerPos;

        // ---- ROTATION COMPENSATION (NO SLIDING) ----
        Quaternion walkerRotDelta = walker.rotation * Quaternion.Inverse(lastWalkerRot);

        Vector3 relativePos = rb.position - walker.position;
        relativePos = walkerRotDelta * relativePos;

        Vector3 targetPos = walker.position + relativePos + walkerDelta;

        // ---- VELOCITY COMPENSATION (START / STOP FIX) ----
        Vector3 walkerVelocity = walkerDelta / Time.fixedDeltaTime;
        rb.linearVelocity += walkerVelocity * velocityCompensation;

        rb.MovePosition(targetPos);

        // ---- ROTATION ALIGNMENT ----
        if (alignRotationToWalker)
        {
            Quaternion targetRot = rb.rotation;

            if (yawOnly)
            {
                float y = walker.rotation.eulerAngles.y;
                targetRot = Quaternion.Euler(0f, y, 0f);
            }
            else
            {
                targetRot = walkerRotDelta * rb.rotation;
            }

            rb.MoveRotation(targetRot);
        }

        lastWalkerPos = walker.position;
        lastWalkerRot = walker.rotation;
    }

    bool IsStillOnWalker()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);
        return Physics.Raycast(ray, groundCheckDistance, walkerLayer);
    }

    void ExitWalker()
    {
        isOnWalker = false;
        walker = null;
    }

    public bool IsOnWalker() => isOnWalker;
}
