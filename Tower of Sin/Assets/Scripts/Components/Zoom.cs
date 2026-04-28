using UnityEngine;

[ExecuteInEditMode]
public class Zoom : MonoBehaviour
{
    private Camera cam; // renamed

    public float defaultFOV = 60;
    public float maxZoomFOV = 15;

    [Range(0, 1)]
    public float currentZoom;

    public float sensitivity = 1;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam)
        {
            defaultFOV = cam.fieldOfView;
        }
    }

    void Update()
    {
        currentZoom += Input.mouseScrollDelta.y * sensitivity * 0.05f;
        currentZoom = Mathf.Clamp01(currentZoom);

        if (cam != null)
            cam.fieldOfView = Mathf.Lerp(defaultFOV, maxZoomFOV, currentZoom);
    }
}
