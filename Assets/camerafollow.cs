using UnityEngine;

public class camerafollow : MonoBehaviour
{
    [Header("目標設定")]
    public Transform target; // 這裡要拖曳你的飛機物件

    [Header("跟隨設定")]
    public Vector3 offset = new Vector3(0f, 3f, -10f); // 攝影機相對於飛機的距離（預設在飛機後方 10 公尺、上方 3 公尺）
    public float smoothSpeed = 10f; // 跟隨的滑順度，數值越大跟越緊

    void LateUpdate()
    {
        if (target == null) return;

        // 計算攝影機應該去的位置（目標位置 + 偏移量）
        Vector3 desiredPosition = target.position + target.TransformDirection(offset);

        // 使用 Lerp 讓攝影機移動時帶有一點點緩衝滑順感
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // 讓攝影機永遠盯著飛機看
        transform.LookAt(target.position + Vector3.up * 1f);
    }
}