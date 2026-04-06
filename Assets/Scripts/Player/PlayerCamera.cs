using UnityEngine;
using Mirror;

public class PlayerCamera : NetworkBehaviour
{
    public override void OnStartLocalPlayer()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = transform;
        }
    }
}

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    [SerializeField] private float smoothSpeed = 8f;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = new Vector3(target.position.x, target.position.y, -10f);
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }
}
