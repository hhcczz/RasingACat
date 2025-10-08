using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image fadeImage;          // 페이드용 부모 이미지
    [SerializeField] private Button startButton;       // 시작 버튼
    [SerializeField] private float fadeDuration = 1f;  // 페이드 시간(초)

    private List<Image> allImages = new List<Image>();
    private List<Text> allTexts = new List<Text>();
    private List<Outline> allOutlines = new List<Outline>();

    private Color[] originalImageColors;
    private Color[] originalTextColors;
    private Color[] originalOutlineColors;

    void Awake()
    {
        if (fadeImage == null)
            fadeImage = GetComponent<Image>();

        // 부모 + 자식의 모든 Image/Text/Outline 컴포넌트 수집
        allImages.AddRange(GetComponentsInChildren<Image>(includeInactive: true));
        allTexts.AddRange(GetComponentsInChildren<Text>(includeInactive: true));
        allOutlines.AddRange(GetComponentsInChildren<Outline>(includeInactive: true));

        // 각 원본 색상 저장
        originalImageColors = new Color[allImages.Count];
        for (int i = 0; i < allImages.Count; i++)
        {
            originalImageColors[i] = allImages[i].color;
        }

        originalTextColors = new Color[allTexts.Count];
        for (int i = 0; i < allTexts.Count; i++)
        {
            originalTextColors[i] = allTexts[i].color;
        }

        originalOutlineColors = new Color[allOutlines.Count];
        for (int i = 0; i < allOutlines.Count; i++)
        {
            originalOutlineColors[i] = allOutlines[i].effectColor;
        }
    }

    // 버튼 클릭 시 호출
    public void OnStartButton()
    {
        if (startButton != null)
            startButton.interactable = false; // 중복 클릭 방지

        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);

            // 이미지 알파 변경
            for (int i = 0; i < allImages.Count; i++)
            {
                Color c = originalImageColors[i];
                c.a = alpha;
                allImages[i].color = c;
            }

            // 텍스트 알파 변경
            for (int i = 0; i < allTexts.Count; i++)
            {
                Color c = originalTextColors[i];
                c.a = alpha;
                allTexts[i].color = c;
            }

            // Outline 알파 변경
            for (int i = 0; i < allOutlines.Count; i++)
            {
                Color c = originalOutlineColors[i];
                c.a = alpha;
                allOutlines[i].effectColor = c;
            }

            yield return null;
        }

        // 완전히 투명 처리
        for (int i = 0; i < allImages.Count; i++)
        {
            Color c = originalImageColors[i];
            c.a = 0f;
            allImages[i].color = c;
        }
        for (int i = 0; i < allTexts.Count; i++)
        {
            Color c = originalTextColors[i];
            c.a = 0f;
            allTexts[i].color = c;
        }
        for (int i = 0; i < allOutlines.Count; i++)
        {
            Color c = originalOutlineColors[i];
            c.a = 0f;
            allOutlines[i].effectColor = c;
        }

        gameObject.SetActive(false); // 페이드 완료 후 비활성화
    }
}