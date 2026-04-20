using UnityEngine;
using Mirror;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Camera cam;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnStartLocalPlayer()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        if (moveInput.sqrMagnitude > 0.01f)
            GameAudioManager.StartNamedLoop("footsteps");
        else
            GameAudioManager.StopCurrentLoop();

        RotateTowardsMouse();
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        rb.linearVelocity = moveInput.normalized * moveSpeed;
    }

    private void RotateTowardsMouse()
    {
        if (cam == null) return;

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mouseWorldPos - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnDisable()
    {
        if (isLocalPlayer)
            GameAudioManager.StopCurrentLoop();
    }
}
