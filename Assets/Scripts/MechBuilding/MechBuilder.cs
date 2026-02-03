using UnityEngine;
using System.Collections.Generic;

public class MechBuilder : MonoBehaviour
{
    public bool buildModeActive;

    [Header("References")]
    public Camera buildCamera;
    public LayerMask snapLayer;
    public LayerMask groundLayer;
    public GameObject builderUI;

    [Header("Limits")]
    public int maxTotalModules = 50;

    [Header("Snap Settings")]
    public float snapSearchRadius = 2f; // How far to search for snap points
    public float snapDetectionRadius = 0.5f; // How close mouse needs to be to trigger snap
    public float complementarySnapBonus = 10f; // Priority bonus for complementary snaps

    private ModuleData selectedModule;
    private GameObject ghostModule;
    private SnapPoint hoveredSnap;
    private SnapPoint bestGhostSnap;
    private Vector3 currentGhostRotation;

    public List<Module> allModules = new();

    void Update()
    {
        if (buildCamera == null)
        {
            buildCamera = Camera.main;
            return;
        }

        if (Input.GetKeyDown(KeyCode.B))
            ToggleBuildMode();

        if (!buildModeActive || selectedModule == null)
            return;

        UpdateGhost();

        if (Input.GetMouseButtonDown(0))
            TryPlace();

        if (Input.GetMouseButtonDown(1))
            CancelPlacement();

        // Fixed rotation input - shift modifier must be checked first
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (Input.GetKey(KeyCode.LeftShift))
                currentGhostRotation += new Vector3(0f, -90f, 0f);
            else
                currentGhostRotation += new Vector3(0f, 90f, 0f);
        }
    }

    // -------------------------
    // BUILD MODE
    // -------------------------

    void ToggleBuildMode()
    {
        buildModeActive = !buildModeActive;

        buildCamera.GetComponent<BuildCamera>().enabled = buildModeActive;
        buildCamera.GetComponent<PlayerCameraController>().enabled = !buildModeActive;

        builderUI.SetActive(buildModeActive);

        if (!buildModeActive)
            CancelPlacement();
    }

    // -------------------------
    // UI API
    // -------------------------

    public void SelectModule(ModuleData data)
    {
        selectedModule = data;

        if (ghostModule != null)
            Destroy(ghostModule);

        ghostModule = Instantiate(data.prefab);
        ghostModule.transform.SetParent(transform);
        ghostModule.transform.localPosition = Vector3.zero;

        SetGhostState(ghostModule, true);
    }

    public void CancelPlacement()
    {
        selectedModule = null;
        hoveredSnap = null;
        bestGhostSnap = null;

        if (ghostModule != null)
            Destroy(ghostModule);
    }

    // -------------------------
    // GHOST
    // -------------------------

    void UpdateGhost()
    {
        hoveredSnap = null;
        bestGhostSnap = null;

        Ray ray = buildCamera.ScreenPointToRay(Input.mousePosition);

        // Try to find nearby snap points using sphere cast
        SnapPoint targetSnap = FindNearestSnapPoint(ray);
        
        if (targetSnap != null)
        {
            SnapPoint ghostSnap = GetBestGhostSnap(selectedModule, targetSnap);
            if (ghostSnap != null)
            {
                bestGhostSnap = ghostSnap;
                
                // Rotate ghost so snaps face each other
                Quaternion targetRotation = Quaternion.LookRotation(
                    -targetSnap.transform.forward,
                    targetSnap.transform.up
                );

                ghostModule.transform.rotation =
                    targetRotation *
                    Quaternion.Inverse(ghostSnap.transform.localRotation) *
                    Quaternion.Euler(currentGhostRotation);

                // Move ghost so snap points overlap EXACTLY
                Vector3 offset = ghostModule.transform.position - ghostSnap.transform.position;
                ghostModule.transform.position = targetSnap.transform.position + offset;

                bool valid = CanAttach(selectedModule, targetSnap);
                TintGhost(valid ? Color.cyan : Color.red);

                if (valid)
                    hoveredSnap = targetSnap;

                return;
            }
        }

        // Fallback: free placement on ground
        if (Physics.Raycast(ray, out RaycastHit groundHit, 100f, groundLayer))
        {
            ghostModule.transform.localPosition =
                transform.InverseTransformPoint(groundHit.point);
            ghostModule.transform.localRotation = Quaternion.Euler(currentGhostRotation);
            TintGhost(Color.yellow);
            return;
        }

        // Final fallback: plane at mech base
        Plane plane = new Plane(Vector3.up, transform.position);
        if (plane.Raycast(ray, out float enter))
        {
            Vector3 worldPoint = ray.GetPoint(enter);
            ghostModule.transform.localPosition =
                transform.InverseTransformPoint(worldPoint);
            ghostModule.transform.localRotation = Quaternion.Euler(currentGhostRotation);
        }

        TintGhost(Color.red);
    }

    SnapPoint FindNearestSnapPoint(Ray ray)
    {
        // First try direct raycast
        if (Physics.Raycast(ray, out RaycastHit snapHit, 100f, snapLayer))
        {
            SnapPoint snap = snapHit.collider.GetComponent<SnapPoint>();
            if (snap != null && !snap.occupied)
                return snap;
        }

        // If direct hit fails, search for nearby snap points
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(hit.point, snapSearchRadius, snapLayer);
            
            SnapPoint closestSnap = null;
            float closestDistance = float.MaxValue;

            foreach (Collider col in nearbyColliders)
            {
                SnapPoint snap = col.GetComponent<SnapPoint>();
                if (snap == null || snap.occupied)
                    continue;

                float distance = Vector3.Distance(hit.point, snap.transform.position);
                
                // Weight distance by how aligned the snap is with the camera ray
                Vector3 toSnap = (snap.transform.position - hit.point).normalized;
                float alignment = Vector3.Dot(ray.direction, toSnap);
                float weightedDistance = distance * (2f - alignment); // Prefer snaps along ray direction

                if (weightedDistance < closestDistance && distance < snapDetectionRadius)
                {
                    closestDistance = weightedDistance;
                    closestSnap = snap;
                }
            }

            return closestSnap;
        }

        return null;
    }

    void TintGhost(Color color)
    {
        foreach (Renderer r in ghostModule.GetComponentsInChildren<Renderer>())
        {
            foreach (Material m in r.materials)
            {
                m.color = new Color(color.r, color.g, color.b, 0.4f);
            }
        }
    }

    // -------------------------
    // PLACEMENT
    // -------------------------

    void TryPlace()
    {
        if (hoveredSnap == null) return;
        if (allModules.Count >= maxTotalModules) return;

        PlaceModule(selectedModule, hoveredSnap);
    }

    bool CanAttach(ModuleData data, SnapPoint snap)
    {
        if (snap.occupied) return false;
        if (!snap.accepts.Contains(data.category)) return false;

        Module parent = snap.GetComponentInParent<Module>();
        if (parent != null && parent.depth >= data.maxChainDepth)
            return false;

        return true;
    }

    void PlaceModule(ModuleData data, SnapPoint targetSnap)
    {
        GameObject obj = Instantiate(data.prefab, transform);
        Module module = obj.GetComponent<Module>();
        module.data = data;

        // Use the same ghost snap we found during preview
        SnapPoint ghostSnap = bestGhostSnap != null ? 
            FindMatchingSnapInModule(obj, bestGhostSnap) : 
            obj.GetComponentInChildren<SnapPoint>();

        // Match rotation
        Quaternion targetRotation = Quaternion.LookRotation(
            -targetSnap.transform.forward,
            targetSnap.transform.up
        );

        obj.transform.rotation =
            targetRotation *
            Quaternion.Inverse(ghostSnap.transform.localRotation) *
            Quaternion.Euler(currentGhostRotation);

        // Match position EXACTLY
        Vector3 offset = obj.transform.position - ghostSnap.transform.position;
        obj.transform.position = targetSnap.transform.position + offset;

        // Finalize
        targetSnap.occupied = true;
        targetSnap.attachedModule = module;

        module.AttachTo(targetSnap);
        allModules.Add(module);
    }

    SnapPoint FindMatchingSnapInModule(GameObject module, SnapPoint ghostSnap)
    {
        // Find the snap point in the new module that matches the ghost snap's local position
        SnapPoint[] snaps = module.GetComponentsInChildren<SnapPoint>();
        
        float closestDist = float.MaxValue;
        SnapPoint match = null;

        foreach (SnapPoint snap in snaps)
        {
            float dist = Vector3.Distance(snap.transform.localPosition, ghostSnap.transform.localPosition);
            if (dist < closestDist)
            {
                closestDist = dist;
                match = snap;
            }
        }

        return match != null ? match : snaps.Length > 0 ? snaps[0] : null;
    }

    public void SpawnStartingModule(ModuleData data, Vector3 localPos, Quaternion localRot)
    {
        GameObject obj = Instantiate(data.prefab, transform);
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = localRot;

        Module module = obj.GetComponent<Module>();
        module.data = data;
        module.depth = 0;

        allModules.Add(module);
    }

    // -------------------------
    // UTIL
    // -------------------------

    void SetGhostState(GameObject obj, bool ghost)
    {
        foreach (Collider c in obj.GetComponentsInChildren<Collider>())
            c.enabled = !ghost;
    }

    /// <summary>
    /// Determines if two snap types are complementary (e.g., top↔bottom, left↔right)
    /// </summary>
    bool AreComplementarySnapTypes(SnapType typeA, SnapType typeB)
    {
        switch (typeA)
        {
            case SnapType.Top:
                return typeB == SnapType.Bottom;
            case SnapType.Bottom:
                return typeB == SnapType.Top;
            case SnapType.Left:
                return typeB == SnapType.Right;
            case SnapType.Right:
                return typeB == SnapType.Left;
            case SnapType.Front:
                return typeB == SnapType.Back;
            case SnapType.Back:
                return typeB == SnapType.Front;
            default:
                return false;
        }
    }

    SnapPoint GetBestGhostSnap(ModuleData data, SnapPoint targetSnap)
    {
        SnapPoint[] ghostSnaps = ghostModule.GetComponentsInChildren<SnapPoint>();
        SnapPoint bestSnap = null;
        float bestScore = float.MaxValue;

        foreach (SnapPoint ghostSnap in ghostSnaps)
        {
            // Check compatibility: the ghost snap must accept the module's category
            if (!ghostSnap.accepts.Contains(data.category))
                continue;

            // Calculate how close this snap point would be after alignment
            Vector3 directionToTarget = targetSnap.transform.position - ghostSnap.transform.position;
            float distance = directionToTarget.magnitude;
            
            // Consider alignment of normals (prefer snaps facing the right direction)
            float normalAlignment = Vector3.Dot(ghostSnap.transform.forward, -targetSnap.transform.forward);
            
            // Base score: combine distance and alignment (lower is better)
            float score = distance * (2f - normalAlignment);

            // PRIORITY BOOST: Check if snap types are complementary
            if (AreComplementarySnapTypes(ghostSnap.snapType, targetSnap.snapType))
            {
                // Heavily prioritize complementary snaps by reducing their score
                score /= complementarySnapBonus;
            }

            if (score < bestScore)
            {
                bestScore = score;
                bestSnap = ghostSnap;
            }
        }

        return bestSnap;
    }

    public void FinishBuild()
    {
        buildModeActive = false;

        buildCamera.GetComponent<BuildCamera>().enabled = false;
        buildCamera.GetComponent<PlayerCameraController>().enabled = true;

        builderUI.SetActive(false);
        GetComponent<Rigidbody>().useGravity = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        CancelPlacement();
    }
}