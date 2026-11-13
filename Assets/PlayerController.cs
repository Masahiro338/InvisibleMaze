using UnityEngine;

/// <summary>
/// 一人称視点（FPS）のプレイヤー操作。
/// WASD移動 / マウス視点 / Spaceジャンプ / Shiftダッシュ / 重力・接地判定。
/// 壁との衝突は CharacterController.Move が処理するため、すり抜けない。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("移動")]
    [Tooltip("歩く速さ (m/s)")]
    public float walkSpeed = 4f;
    [Tooltip("Shift押下中の速さ (m/s)")]
    public float dashSpeed = 7f;

    [Header("ジャンプ・重力")]
    [Tooltip("ジャンプの高さ (m)")]
    public float jumpHeight = 0.8f;
    [Tooltip("重力加速度 (負の値)")]
    public float gravity = -19.62f;

    [Header("視点")]
    [Tooltip("プレイヤーの子にしたカメラ。未設定なら子オブジェクトから自動取得")]
    public Transform playerCamera;
    public float mouseSensitivity = 2f;
    [Tooltip("上下の視点の最大角度")]
    public float maxLookAngle = 80f;

    private CharacterController controller;
    private float xRotation = 0f;
    private float verticalVelocity = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // カメラ未設定なら、子にあるカメラを探す
        if (playerCamera == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) playerCamera = cam.transform;
        }
        if (playerCamera == null)
        {
            Debug.LogWarning("PlayerController: カメラが見つかりません。Main Camera を Player の子にしてください。", this);
        }

        LockCursor(true);
    }

    void Update()
    {
        HandleCursor();
        HandleLook();
        HandleMove();
    }

    // ---- 視点 ----
    void HandleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 左右はプレイヤー本体を回転
        transform.Rotate(Vector3.up * mouseX);

        // 上下はカメラのみ回転
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    // ---- 移動・ジャンプ・重力 ----
    void HandleMove()
    {
        bool grounded = controller.isGrounded;

        // 接地中は下向き速度をリセット（少し押し付けて接地判定を安定させる）
        if (grounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        // WASD 入力
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        move = Vector3.ClampMagnitude(move, 1f);

        // ダッシュ
        bool dashing = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float speed = dashing ? dashSpeed : walkSpeed;

        // ジャンプ（接地中のみ）
        if (grounded && Input.GetKeyDown(KeyCode.Space))
        {
            // v = sqrt(h * -2 * g)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 重力
        verticalVelocity += gravity * Time.deltaTime;

        // 水平移動と垂直移動をまとめて1回で Move（isGrounded が安定する）
        Vector3 velocity = move * speed + Vector3.up * verticalVelocity;
        CollisionFlags flags = controller.Move(velocity * Time.deltaTime);

        // 天井に頭をぶつけたら上昇を止める
        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }
    }

    // ---- カーソル ----
    void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LockCursor(false);
        }
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor(true);
        }
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
