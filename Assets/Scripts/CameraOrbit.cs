using UnityEngine;

// Otáčení kamery: pravé tlačítko myši, klávesy 1/2 po 90°.
public class CameraOrbit : MonoBehaviour
{
    public static CameraOrbit I;
    public Transform pivot;
    public float yaw = 38f;
    public float pitch = 42f;
    public float distance = 17.5f;
    public float targetYaw = 38f;
    public bool autoOrbit;

    public Vector3Int ForwardAxis { get; private set; }
    public Vector3Int RightAxis { get; private set; }

    void Awake()
    {
        I = this;
        targetYaw = yaw;
    }

    public void BumpYaw(float deg)
    {
        autoOrbit = false;
        targetYaw += deg;
    }

    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        if (Input.GetMouseButton(1))
        {
            autoOrbit = false;
            yaw += Input.GetAxis("Mouse X") * 9f;
            pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 7f, 16f, 76f);
            targetYaw = yaw;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            BumpYaw(-90f);
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            BumpYaw(90f);

        if (autoOrbit && !Input.GetMouseButton(1))
        {
            targetYaw += 10f * dt;
            yaw = targetYaw;
        }
        else
        {
            yaw = Mathf.LerpAngle(yaw, targetYaw, 1f - Mathf.Exp(-8f * dt));
        }

        Vector3 look = pivot != null ? pivot.position : new Vector3(0f, 5.5f, 0f);
        var rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = look + rot * new Vector3(0f, 0f, -distance);
        transform.LookAt(look + Vector3.up * 0.4f);
        UpdateAxes();
    }

    void UpdateAxes()
    {
        Vector3 f = transform.forward; f.y = 0f;
        if (f.sqrMagnitude < 0.0001f) f = -Vector3.forward;
        f.Normalize();
        Vector3 r = transform.right; r.y = 0f;
        if (r.sqrMagnitude < 0.0001f) r = Vector3.right;
        r.Normalize();
        ForwardAxis = Snap(f);
        RightAxis = Snap(r);
    }

    static Vector3Int Snap(Vector3 v)
    {
        if (Mathf.Abs(v.x) >= Mathf.Abs(v.z))
            return new Vector3Int(v.x >= 0f ? 1 : -1, 0, 0);
        return new Vector3Int(0, 0, v.z >= 0f ? 1 : -1);
    }
}
