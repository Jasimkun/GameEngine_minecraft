using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController2 : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpPower = 5f;
    public float gravity = -9.81f;
    public float mouseSensitivity = 3f;

    float xRotation = 0f;
    CharacterController controller;
    Transform cam;
    Vector3 velocity;
    bool isGrounded;

    private bool isCursorLocked = true;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>()?.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandleCursorLock();
        HandleMove();
        HandleLook();
    }

    void HandleMove()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = transform.right * h + transform.forward * v;
        controller.Move(move * moveSpeed * Time.deltaTime);
        if (Input.GetButtonDown("Jump") && isGrounded)
            velocity.y = Mathf.Sqrt(jumpPower * -2f * gravity);
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        if (cam != null)
            cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void HandleCursorLock()
    {
        // 1. ESC 키를 누르면 커서 해제
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLock(false);
        }
        // 2. 커서가 해제된 상태에서 마우스 왼쪽 버튼(GetMouseButtonDown(0) 대신 마우스 버튼 일반 사용)을 클릭하면 다시 잠금
        else if (!isCursorLocked && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Escape)))
        {
            SetCursorLock(true);
        }
    }

    private void SetCursorLock(bool lockState)
    {
        isCursorLocked = lockState;

        if (lockState)
        {
            // 커서 잠금 및 숨기기 (게임 플레이 모드)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            // 커서 해제 및 보이기 (UI 메뉴 또는 일시 정지 모드)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
