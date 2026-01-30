using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

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

    private ModuleData selectedModule;
    private GameObject ghostModule;
    private SnapPoint hoveredSnap;

    public List<Module> allModules = new();

    bool triggeredStartSpawn;

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

        if (ghostModule != null)
            Destroy(ghostModule);
    }

    // -------------------------
    // GHOST
    // -------------------------
    void UpdateGhost()
    {
        hoveredSnap = null;

        Ray ray = buildCamera.ScreenPointToRay(Input.mousePosition);

        // Snap point check
        if (Physics.Raycast(ray, out RaycastHit snapHit, 100f, snapLayer))
        {
            SnapPoint snap = snapHit.collider.GetComponent<SnapPoint>();
            if (snap != null)
            {
                SnapPoint ghostSnap = GetBestGhostSnap(selectedModule, snap);
                if (ghostSnap == null)
                    return;

                // 1. Rotate ghost so snaps face each other
                Quaternion targetRotation =
                    Quaternion.LookRotation(
                        -snap.transform.forward,
                        snap.transform.up
                    );

                ghostModule.transform.rotation =
                    targetRotation *
                    Quaternion.Inverse(ghostSnap.transform.localRotation);

                // 2. Move ghost so snap points overlap
                Vector3 offset =
                    ghostModule.transform.position - ghostSnap.transform.position;

                ghostModule.transform.position =
                    snap.transform.position + offset;
                    
                bool valid = CanAttach(selectedModule, snap);
                TintGhost(valid ? Color.cyan : Color.red);

                if (valid)
                    hoveredSnap = snap;

                return;
            }
        }

        // Fallback: free placement position
        if (Physics.Raycast(ray, out RaycastHit groundHit, 100f, groundLayer))
        {
            ghostModule.transform.localPosition = transform.InverseTransformPoint(groundHit.point);
            ghostModule.transform.localRotation = Quaternion.identity;
        }

        Plane plane = new Plane(Vector3.up, transform.position);
        if (plane.Raycast(ray, out float enter))
        {
            Vector3 worldPoint = ray.GetPoint(enter);
            ghostModule.transform.localPosition =
                transform.InverseTransformPoint(worldPoint);
        }

        TintGhost(Color.red);
    }

    void TintGhost(Color color)
    {
        foreach (Renderer r in ghostModule.GetComponentsInChildren<Renderer>())
        {
            foreach (Material m in r.materials)
                m.color = new Color(color.r, color.g, color.b, 0.4f);
        }
    }

    // -------------------------
    // PLACEMENT
    // -------------------------
    void TryPlace()
    {
        if (hoveredSnap == null)
            return;

        if (allModules.Count >= maxTotalModules)
            return;

        PlaceModule(selectedModule, hoveredSnap);
    }

    bool CanAttach(ModuleData data, SnapPoint snap)
    {
        if (snap.occupied)
            return false;

        if (!snap.accepts.Contains(data.category))
            return false;

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

        SnapPoint ghostSnap = obj.GetComponentInChildren<SnapPoint>();

        // Match rotation
        Quaternion targetRotation =
            Quaternion.LookRotation(
                -targetSnap.transform.forward,
                targetSnap.transform.up
            );

        obj.transform.rotation =
            targetRotation *
            Quaternion.Inverse(ghostSnap.transform.localRotation);

        // Match position
        Vector3 offset =
            obj.transform.position - ghostSnap.transform.position;

        obj.transform.position =
            targetSnap.transform.position + offset;

        // Finalize
        targetSnap.occupied = true;
        targetSnap.attachedModule = module;

        module.AttachTo(targetSnap);
        allModules.Add(module);
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

    SnapPoint GetBestGhostSnap(ModuleData data, SnapPoint targetSnap)
    {
        foreach (SnapPoint ghostSnap in ghostModule.GetComponentsInChildren<SnapPoint>())
        {
            if (targetSnap.accepts.Contains(data.category))
                return ghostSnap;
        }

        return null;
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