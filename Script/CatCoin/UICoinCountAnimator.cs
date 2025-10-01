using UnityEngine;
using UnityEngine.UI;
// using TMPro; // TMP 쓰면 이걸 열고 Text -> TMP_Text로 변경

public class UICoinCountAnimator : MonoBehaviour
{
    [Header("UI")]
    public Text coinText; // TMP 쓰면 TMP_Text coinText;

    [Header("Animation")]
    public float duration = 0.6f;
    public bool useUnscaledTime = true;

    Coroutine animCo;
    long displayedValue = 0; // 현재 표시 중인 값(뷰)

    void Awake()
    {
        if (!coinText) coinText = GetComponent<Text>();
    }

    public void SetInstant(long value)
    {
        displayedValue = value;
        coinText.text = Format(value);
    }

    public void AnimateTo(long target)
    {
        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(AnimateRoutine(displayedValue, target, duration));
    }

    System.Collections.IEnumerator AnimateRoutine(long from, long to, float d)
    {
        if (d <= 0f) d = 0.01f;
        float t = 0f;

        while (t < d)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float p = Mathf.Clamp01(t / d);

            // 이징: easeOutExpo
            float eased = 1f - Mathf.Pow(2f, -10f * p);

            long val = (long)Mathf.Lerp(from, to, eased);
            if (val != displayedValue)
            {
                displayedValue = val;
                coinText.text = Format(val);
            }
            yield return null;
        }

        displayedValue = to;
        coinText.text = Format(to);
        animCo = null;
    }

    string Format(long v)
    {
        // 1,234,567 형식 또는 한글 단위로 하고 싶으면 커스터마이즈
        return Formatted.FormatKoreanNumber(v); // 이미 쓰는 포맷터 있으면 이걸로
    }
}