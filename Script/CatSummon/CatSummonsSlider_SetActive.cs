using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CatSummonsSlider_SetActive : MonoBehaviour
{
    [Header("Assign (필수)")]
    [SerializeField] private ScrollRect scrollRect;   // Scroll View의 ScrollRect
    [SerializeField] private RectTransform content;   // Viewport 밑 Content

    [Header("표시 규칙")]
    [SerializeField] private int maxVisible = 9;      // 화면에 보일 개수
    [SerializeField] private int edgeBuffer = 1;      // 위/아래 버퍼 1칸(총 2)

    // 내부
    private readonly List<RectTransform> items = new List<RectTransform>();
    private VerticalLayoutGroup vlg;
    private float itemBodyHeight;     // 아이템 자체 높이
    private float stride;             // 한 칸 간격 = itemBodyHeight + spacing
    private float padTop;             // VLG padding top
    private int dataCount;

    void Start()
    {
        if (!scrollRect || !content)
        {
            Debug.LogError("[SliderLayout] scrollRect/content 미할당");
            enabled = false; return;
        }

        vlg = content.GetComponent<VerticalLayoutGroup>();
        if (!vlg)
        {
            Debug.LogError("[SliderLayout] Content에 VerticalLayoutGroup이 필요합니다.");
            enabled = false; return;
        }

        // 자식 수집 + CanvasGroup 보장
        items.Clear();
        for (int i = 0; i < content.childCount; i++)
        {
            var rt = content.GetChild(i) as RectTransform;
            if (!rt) continue;
            items.Add(rt);

            var cg = rt.GetComponent<CanvasGroup>();
            if (!cg) cg = rt.gameObject.AddComponent<CanvasGroup>();
            // 초기엔 전부 보여주도록
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        dataCount = items.Count;
        if (dataCount == 0)
        {
            Debug.LogWarning("[SliderLayout] Content에 자식이 없습니다.");
            enabled = false; return;
        }

        // 첫 아이템 기준 높이 측정 (VLG가 Height를 Control하지 않아도 rect.height는 나옴)
        var first = items[0];
        itemBodyHeight = first.rect.height;
        if (itemBodyHeight <= 0f) itemBodyHeight = Mathf.Abs(first.sizeDelta.y);
        if (itemBodyHeight <= 0f) itemBodyHeight = 100f; // fallback

        padTop = vlg.padding.top;
        stride = itemBodyHeight + vlg.spacing;

        // 스크롤 이벤트 연결
        scrollRect.onValueChanged.AddListener(_ => Refresh());
        Refresh();
    }

    private void Refresh()
    {
        // 뷰포트 높이 → 실제 보이는 칸 수 계산
        var viewport = scrollRect.viewport ? scrollRect.viewport : content.parent as RectTransform;
        float viewportH = viewport ? viewport.rect.height : maxVisible * stride;
        int visibleCount = Mathf.Max(1, Mathf.CeilToInt((viewportH + 0.5f) / stride));

        // 현재 스크롤 위치 → 최상단 인덱스 (패딩 고려)
        float scrollY = content.anchoredPosition.y;
        if (scrollY < 0f) scrollY = 0f;
        float yFromFirstItem = Mathf.Max(0f, scrollY - padTop);

        int rawFirst = Mathf.FloorToInt(yFromFirstItem / stride);
        int maxFirst = Mathf.Max(0, dataCount - visibleCount);
        int firstIndex = Mathf.Clamp(rawFirst, 0, maxFirst);

        int start = Mathf.Max(0, firstIndex - edgeBuffer);
        int end = Mathf.Min(dataCount - 1, firstIndex + visibleCount - 1 + edgeBuffer);

        // 보이기/가리기 (SetActive 금지!)
        for (int i = 0; i < dataCount; i++)
        {
            var cg = items[i].GetComponent<CanvasGroup>();
            bool visible = (i >= start && i <= end);

            // 바뀐 경우에만 적용(오버헤드 줄임)
            if (visible && cg.alpha != 1f)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
            else if (!visible && cg.alpha != 0f)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }
    }
}
