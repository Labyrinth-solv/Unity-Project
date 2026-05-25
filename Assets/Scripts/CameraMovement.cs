using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private float cameraSpeed = 10f;
    [SerializeField] private float rotationStep = 90f;
    [SerializeField] private float zoomSpeed = 8f;
    [SerializeField] private float minFieldOfView = 20f;
    [SerializeField] private float maxFieldOfView = 70f;

    private Camera targetCamera;
    private float pitchAngle;
    private float yawAngle;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        Vector3 eulerAngles = transform.eulerAngles;
        pitchAngle = eulerAngles.x;
        yawAngle = eulerAngles.y;
    }

    private void Update()
    {
        HandleRotation();
        HandleZoom();
        HandleMovement();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 direction = right * horizontal + forward * vertical;

        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        transform.position += direction * cameraSpeed * Time.deltaTime;
    }

    private void HandleRotation()
    {
        bool isCtrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (!isCtrlHeld || !Input.GetKeyDown(KeyCode.R))
        {
            return;
        }

        yawAngle += rotationStep;
        if (TryGetViewCenterOnGround(out Vector3 pivot))
        {
            transform.RotateAround(pivot, Vector3.up, rotationStep);
        }
        else
        {
            transform.rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
        }
    }

    private void HandleZoom()
    {
        if (targetCamera == null)
        {
            return;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f))
        {
            return;
        }

        targetCamera.fieldOfView = Mathf.Clamp(
            targetCamera.fieldOfView - scroll * zoomSpeed,
            minFieldOfView,
            maxFieldOfView);
    }

    private bool TryGetViewCenterOnGround(out Vector3 point)
    {
        point = Vector3.zero;

        if (targetCamera == null)
        {
            return false;
        }

        Ray ray = targetCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (!groundPlane.Raycast(ray, out float enter))
        {
            return false;
        }

        point = ray.GetPoint(enter);
        return true;
    }
}
