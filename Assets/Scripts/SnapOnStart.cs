// SnapOnStart.cs
using UnityEngine;

[DisallowMultipleComponent]
public class SnapOnStart : MonoBehaviour
{
    [SerializeField] private GridSnapper snapper;

    [Tooltip("If true, snaps in Awake. If false, snaps in Start.")]
    [SerializeField] private bool snapInAwake = true;

    private void Awake()
    {
        if (snapper == null) snapper = GetComponent<GridSnapper>();
        if (snapInAwake) DoSnap();
    }

    private void Start()
    {
        if (!snapInAwake) DoSnap();
    }

    private void DoSnap()
    {
        if (snapper == null) return;

        // Ensure snapping is enabled for the initial placement
        snapper.SetSnappingEnabled(true);
        snapper.SnapNow();
    }
}