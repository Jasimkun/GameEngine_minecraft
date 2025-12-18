using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 죽었을 때 씬 재시작용

public class PlayerController2 : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHP = 20;
    public int currentHP;
    private bool isDead = false;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpPower = 5f;
    public float gravity = -9.81f;
    public float mouseSensitivity = 3f;

    [Header("Light Settings")]
    public Light playerLight;
    public bool isLightOn = true;

    Animator anim;

    float xRotation = 0f;
    CharacterController controller;
    Transform cam;
    Vector3 velocity;
    bool isGrounded;

    private bool isCursorLocked = true;

    // 지속 데미지 코루틴 중복 방지용
    private Coroutine dotCoroutine;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>()?.transform;
        }

        if (playerLight == null)
        {
            playerLight = GetComponentInChildren<Light>();
        }
    }

    private void Start()
    {
        anim = GetComponent<Animator>();
        currentHP = maxHP; // 체력 초기화

        if (playerLight != null)
        {
            playerLight.enabled = isLightOn;
        }
    }

    void Update()
    {
        if (isDead) return; // 죽으면 조작 불가

        HandleCursorLock();
        HandleMove();
        HandleLook();
        HandleLight();

        // 애니메이션 (ControllPlayer 로직 통합)
        UpdateAnimation();
    }

    // === 🔻 [추가] 데미지 처리 로직 ===
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHP -= damage;
        Debug.Log($"Player HP: {currentHP}");

        // 여기에 피격 효과음이나 화면 붉어짐 효과를 넣을 수 있습니다.

        if (currentHP <= 0)
        {
            Die();
        }
    }

    // === 🔻 [추가] 지속 데미지(DoT) 로직 ===
    public void StartDamageOverTime(int damagePerTick, float duration, float interval)
    {
        if (dotCoroutine != null) StopCoroutine(dotCoroutine);
        dotCoroutine = StartCoroutine(DoTRoutine(damagePerTick, duration, interval));
    }

    IEnumerator DoTRoutine(int damage, float duration, float interval)
    {
        float timer = 0f;
        while (timer < duration && !isDead)
        {
            yield return new WaitForSeconds(interval);
            TakeDamage(damage);
            timer += interval;
        }
        dotCoroutine = null;
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Player Died!");

        // 커서 잠금 해제
        SetCursorLock(false);

        // 예시: 현재 씬 재시작 (원하는 로직으로 변경 가능)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    // ==================================

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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetCursorLock(false);
        }
        else if (!isCursorLocked && (Input.GetMouseButtonDown(0)))
        {
            SetCursorLock(true);
        }
    }

    private void SetCursorLock(bool lockState)
    {
        isCursorLocked = lockState;
        if (lockState)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void UpdateAnimation()
    {
        if (anim == null) return;

        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        float moveVertical = Input.GetAxisRaw("Vertical");

        if (moveHorizontal != 0 || moveVertical != 0)
        {
            anim.SetInteger("Walk", 1);
        }
        else
        {
            anim.SetInteger("Walk", 0);
        }
    }

    void HandleLight()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            isLightOn = !isLightOn;
            if (playerLight != null)
            {
                playerLight.enabled = isLightOn;
            }
        }
    }
}