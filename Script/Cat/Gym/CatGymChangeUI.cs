using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CatGymChangeUI : MonoBehaviour
{
    [Header("Pages / Viewports / Contents (size=2)")]
    [SerializeField] private GameObject[] pages = new GameObject[2];            // Viewport_Page1, Viewport_Page2 부모 GO (각 페이지 루트)
    [SerializeField] private RectTransform[] viewports = new RectTransform[2];  // Viewport_Page1, Viewport_Page2
    [SerializeField] private RectTransform[] contents = new RectTransform[2];   // 각 Viewport 밑의 Content

    [Header("Buttons (size=2)")]
    [SerializeField] private Button[] pageButtons = new Button[2];

    [Header("ScrollRect (공용 1개)")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Colors")]
    [SerializeField] private Color selectedColor = new Color(0.00f, 0.78f, 0.66f); // 민트
    [SerializeField] private Color unselectedColor = Color.white;

    [Header("Initial")]
    [SerializeField, Range(0, 1)] private int defaultPageIndex = 0;
    [SerializeField] private bool resetToTopOnSwitch = true;

    int currentIndex = -1;
    Coroutine rebuildRoutine;

    void Awake()
    {
        pageButtons[0]?.onClick.AddListener(() => SetPage(0));
        pageButtons[1]?.onClick.AddListener(() => SetPage(1));
    }

    void OnEnable()
    {
        SetPage(defaultPageIndex);
    }

    public void SetPage(int index)
    {
        if (index == currentIndex) return;

        if (!ValidateArrays()) { Debug.LogWarning("[CatGymChangeUI] 배열 세팅 확인 필요"); return; }
        if (scrollRect == null) { Debug.LogWarning("[CatGymChangeUI] ScrollRect 미지정"); return; }

        // 페이지 On/Off
        for (int i = 0; i < pages.Length; i++)
            if (pages[i]) pages[i].SetActive(i == index);

        // 버튼 색상
        for (int i = 0; i < pageButtons.Length; i++)
            if (pageButtons[i]?.targetGraphic)
                pageButtons[i].targetGraphic.color = (i == index) ? selectedColor : unselectedColor;

        // Viewport / Content 교체
        var vp = viewports[index];
        var ct = contents[index];

        if (vp == null || ct == null) { Debug.LogWarning("[CatGymChangeUI] Viewport/Content 누락"); return; }

        scrollRect.viewport = vp;   // ★ 페이지별 Viewport 연결
        scrollRect.content = ct;   // ★ 해당 Viewport의 Content 연결

        // 앵커/피벗 무난 세팅 (혹시 꼬였을 경우 대비)
        EnsureStretchToViewport(ct, vp);

        // 레이아웃 강제 갱신 + 스크롤 위치 초기화
        if (rebuildRoutine != null) StopCoroutine(rebuildRoutine);
        rebuildRoutine = StartCoroutine(RebuildLayoutAndResetScroll(ct));

        currentIndex = index;
    }

    IEnumerator RebuildLayoutAndResetScroll(RectTransform content)
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        yield return null; // VLG/CSF가 프레임 끝에 사이즈 반영

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        if (resetToTopOnSwitch && scrollRect != null)
        {
            if (scrollRect.vertical) scrollRect.verticalNormalizedPosition = 1f; // 맨 위
            if (scrollRect.horizontal) scrollRect.horizontalNormalizedPosition = 0f; // 맨 왼쪽
        }

        // 살짝 더 안정화(선택): 다음 프레임에도 한 번 더
        yield return null;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        rebuildRoutine = null;
    }

    bool ValidateArrays()
    {
        return pages != null && viewports != null && contents != null &&
               pageButtons != null &&
               pages.Length == 2 && viewports.Length == 2 && contents.Length == 2 && pageButtons.Length == 2;
    }

    void EnsureStretchToViewport(RectTransform content, RectTransform viewport)
    {
        // 일반적으로 Content는 Viewport 내부에서 Stretch(좌0,우1,상1,하0) 세팅이 안전
        if (content && viewport)
        {
            content.SetParent(viewport, worldPositionStays: false);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            // 가로는 Viewport 폭에 맞춰지고, 세로는 VLG/CSF가 height를 늘림
        }
    }
}