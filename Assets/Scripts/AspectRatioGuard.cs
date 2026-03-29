using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class AspectRatioGuard : MonoBehaviour
{
    [Header("Reference Camera Framing")]
    [Tooltip("Use the orthographic size that gives the correct look on your reference screen.")]
    [SerializeField] private float referenceOrthographicSize = 5f;

    [Header("Behavior")]
    [SerializeField] private bool updateContinuously = true;
    [SerializeField] private bool logDebug = false;

    private Camera cam;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        ApplyCameraFraming(true);
    }

    private void Start()
    {
        ApplyCameraFraming(true);
    }

    private void Update()
    {
        if (!updateContinuously)
            return;

        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            ApplyCameraFraming(false);
    }

    public void ApplyCameraFraming(bool force)
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam == null)
            return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        // Always use the full screen. Do NOT crop with camera rect.
        cam.rect = new Rect(0f, 0f, 1f, 1f);

        // Keep the intended vertical framing consistent.
        if (cam.orthographic)
            cam.orthographicSize = referenceOrthographicSize;

        if (logDebug)
        {
            Debug.Log(
                $"[AspectRatioGuard] Screen={Screen.width}x{Screen.height}, " +
                $"Full rect applied, OrthoSize={cam.orthographicSize}"
            );
        }
    }
}