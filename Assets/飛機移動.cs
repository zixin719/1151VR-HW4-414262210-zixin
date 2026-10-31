using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class 飛機移動 : MonoBehaviour
{
    [Header("移動設定")]
    public float forwardSpeed = 10f;
    public float moveSpeed = 8f;
    public float xLimit = 8f;
    public float yLimit = 4.5f;

    [Header("手機觸控設定")]
    public float touchSensitivity = 100f; // 數值越小越靈敏（代表滑動多少像素達到最大移動量）
    private Vector2 touchStartPos;

    [Header("過關設定")]
    public float winZPosition = 210f; // 勝利的 Z 軸座標目標

    [Header("音效設定")]
    public AudioSource engineAudio;
    public AudioSource sfxAudio;
    public AudioClip crashClip;
    public AudioClip winClip;
    public AudioClip milestoneClip;

    private bool isPlaying = false;
    private bool hasGameStarted = false; // 記錄遊戲是否已經開始過
    private float startPositionZ;
    private Vector3 startPosition;
    private Quaternion startRotation; // 用來記錄飛機最初的旋轉角度
    private float nextMilestoneZ;
    public float milestoneInterval = 50f;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation; // 記住一開始的角度
        startPositionZ = transform.position.z;
        nextMilestoneZ = startPositionZ + milestoneInterval;

        isPlaying = false;
        hasGameStarted = false;
        if (engineAudio != null) engineAudio.Stop();
    }

    void Update()
    {
        // 1. 檢測開始或重新遊玩（支援 空白鍵、手機觸控點擊、滑鼠點擊）
        bool isSpacePressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);
        bool isTouchTapped = (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) ||
                              (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (isSpacePressed || isTouchTapped)
        {
            if (!hasGameStarted)
            {
                // 遊戲第一次啟動
                StartGame();
                hasGameStarted = true;
            }
            else if (!isPlaying)
            {
                // 遊戲結束或過關後重新遊玩
                RestartGame();
            }
        }

        if (!isPlaying) return;

        // 2. 自動向前移動
        transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime);

        // 3. 讀取移動輸入（同時支援 鍵盤 與 手機滑動/滑鼠拖曳）
        float moveX = 0f;
        float moveY = 0f;

        // A. 鍵盤控制 (WASD / 方向鍵)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed) moveX = -1f;
            if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed) moveX = 1f;
            if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed) moveY = -1f;
            if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed) moveY = 1f;
        }

        // B. 手機觸控拖曳控制
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                // 剛按下時記錄起始位置
                touchStartPos = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            else
            {
                // 計算滑動距離差
                Vector2 currentTouchPos = Touchscreen.current.primaryTouch.position.ReadValue();
                Vector2 offset = currentTouchPos - touchStartPos;
                moveX = Mathf.Clamp(offset.x / touchSensitivity, -1f, 1f);
                moveY = Mathf.Clamp(offset.y / touchSensitivity, -1f, 1f);
            }
        }
        // C. 電腦上用滑鼠模擬拖曳（方便在 Unity 編輯器測試）
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                touchStartPos = Mouse.current.position.ReadValue();
            }
            else
            {
                Vector2 currentMousePos = Mouse.current.position.ReadValue();
                Vector2 offset = currentMousePos - touchStartPos;
                moveX = Mathf.Clamp(offset.x / touchSensitivity, -1f, 1f);
                moveY = Mathf.Clamp(offset.y / touchSensitivity, -1f, 1f);
            }
        }

        // 套用移動
        Vector3 moveDir = new Vector3(moveX, moveY, 0f).normalized;
        transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);

        // 限制移動範圍
        Vector3 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, -xLimit, xLimit);
        clampedPos.y = Mathf.Clamp(clampedPos.y, -yLimit, yLimit);
        transform.position = clampedPos;

        // 4. 檢查是否到達勝利位置
        if (transform.position.z >= winZPosition)
        {
            WinGame();
            return;
        }

        // 5. 檢查飛行距離里程碑音效
        if (transform.position.z >= nextMilestoneZ)
        {
            PlayMilestoneSound();
            nextMilestoneZ += milestoneInterval;
        }
    }

    void StartGame()
    {
        isPlaying = true;
        if (engineAudio != null && !engineAudio.isPlaying)
        {
            engineAudio.Play();
        }
    }

    void PlayMilestoneSound()
    {
        if (sfxAudio != null && milestoneClip != null)
        {
            sfxAudio.PlayOneShot(milestoneClip);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isPlaying) return;

        // 撞到障礙物判定失敗
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            GameOver();
        }
    }

    void GameOver()
    {
        isPlaying = false;
        if (engineAudio != null) engineAudio.Stop();
        if (sfxAudio != null && crashClip != null)
        {
            sfxAudio.PlayOneShot(crashClip);
        }
        Debug.Log("遊戲結束！撞到障礙物。點擊螢幕重新遊玩。");
    }

    void WinGame()
    {
        isPlaying = false;
        if (engineAudio != null) engineAudio.Stop();
        if (sfxAudio != null && winClip != null)
        {
            sfxAudio.PlayOneShot(winClip);
        }
        Debug.Log("恭喜過關！點擊螢幕再次挑戰。");
    }

    void RestartGame()
    {
        // 1. 將飛機位置瞬間移回遊戲剛開始的起點
        transform.position = startPosition;

        // 2. 將飛機旋轉角度重置回正面
        transform.rotation = startRotation;

        // 3. 如果飛機有掛載 Rigidbody（物理元件），把速度歸零防止慣性
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 4. 重置里程碑目標
        startPositionZ = transform.position.z;
        nextMilestoneZ = startPositionZ + milestoneInterval;

        // 5. 重新開始遊戲並播放引擎音效
        StartGame();

        Debug.Log("重新開始遊戲！");
    }
}