using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UICoinFlyer : MonoBehaviour
{
    public RectTransform target;      // 도착 지점
    public float duration = 0.6f;     // 비행 시간
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    RectTransform rt;
    Vector3 startPos;
    float t;

    public void Init(Vector3 startWorldPos, RectTransform target, float duration)
    {
        rt = rt ? rt : GetComponent<RectTransform>();
        this.target = target;
        this.duration = Mathf.Max(0.01f, duration);

        rt.position = startWorldPos;
        startPos = startWorldPos;
        t = 0f;
    }

    void Awake() { rt = GetComponent<RectTransform>(); }

    void Update()
    {
        if (target == null) { Destroy(gameObject); return; }

        t += Time.unscaledDeltaTime;
        float p = Mathf.Clamp01(t / duration);
        float k = ease != null ? ease.Evaluate(p) : p;

        Vector3 endPos = target.position;
        rt.position = Vector3.LerpUnclamped(startPos, endPos, k);

        if (p >= 1f) Destroy(gameObject);   // 도착하면 삭제
    }
}