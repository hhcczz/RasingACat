using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public class CatDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum FailDropMode { ReturnToStart, StayWhereDropped, SnapBelowTarget }

    [Header("Level")]
    [Min(0)] public int level = 0;

    [Header("Layer / Parent")]
    public RectTransform playArea;

    [Header("Fail Behavior")]
    public FailDropMode failDrop = FailDropMode.StayWhereDropped;
    public float snapBelowOffsetY = 60f;
    public float edgeMargin = 24f;

    [Header("Title")]
    [Tooltip("자식 중 이름 오브젝트의 이름")]
    public string titleChildName = "Text_Name";
    [Tooltip("자동으로 찾은 Text(없으면 자동 탐색)")]
    public Text titleText;   // UnityEngine.UI.Text 사용

    RectTransform rt;
    Canvas rootCanvas;
    CanvasGroup cg;
    Vector2 startAnchored;
    Transform startParent;

    Vector2 lastLocalDrop;
    CatMover mover;

    [HideInInspector] public RectTransform cachedRectTransform;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        mover = GetComponent<CatMover>();
        rootCanvas = GetComponentInParent<Canvas>();
        cachedRectTransform = GetComponent<RectTransform>();
        if (!playArea) playArea = rt.parent as RectTransform;

        // 레벨 보정
        if (CatManager.Instance) level = CatManager.Instance.ClampLevel(level);

        // Title Text 자동 탐색 & 초기 세팅
        EnsureTitleRef();
        RefreshTitle();
    }

    void OnEnable()
    {
        CatManager.Instance?.Register(this);
    }

    void OnDisable()
    {
        CatManager.Instance?.Unregister(this);
    }

#if UNITY_EDITOR
    // 에디터에서 값 바뀔 때도 미리 미리 반영
    void OnValidate()
    {
        if (CatManager.Instance) level = CatManager.Instance.ClampLevel(level);
        EnsureTitleRef();
        RefreshTitle();
    }
#endif

    // 외부에서 레벨 변경 시 이름도 함께 갱신하고 싶을 때 사용
    public void SetLevel(int newLevel, bool refreshName = true)
    {
        level = CatManager.Instance ? CatManager.Instance.ClampLevel(newLevel) : Mathf.Max(0, newLevel);
        if (refreshName) RefreshTitle();
    }

    void EnsureTitleRef()
    {
        if (titleText && titleText.gameObject) return;

        // 1) 이름으로 직접 찾기
        if (!string.IsNullOrEmpty(titleChildName))
        {
            var t = transform.Find(titleChildName);
            if (t) titleText = t.GetComponent<Text>();
        }

        // 2) 못 찾았으면 자식들 중 첫 Text
        if (!titleText)
            titleText = GetComponentInChildren<Text>(includeInactive: true);
    }

    void RefreshTitle()
    {
        if (!titleText) return;
        string nameToShow = CatManager.Instance ? CatManager.Instance.GetCatName(level) : $"Cat Lv.{level}";
        titleText.text = nameToShow;
    }

    // ───────── drag logic (기존 그대로) ─────────
    public void OnBeginDrag(PointerEventData eventData)
    {
        startParent = rt.parent;
        startAnchored = rt.anchoredPosition;
        rt.SetParent(playArea, worldPositionStays: true);
        if (mover) mover.SetPause(true);
        cg.blocksRaycasts = false;
        cg.alpha = 0.9f;
        CacheLocalFromPointer(eventData, out lastLocalDrop);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CacheLocalFromPointer(eventData, out lastLocalDrop)) return;
        rt.anchoredPosition = ClampToPlayArea(lastLocalDrop);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        cg.blocksRaycasts = true;
        cg.alpha = 1f;
        if (mover) mover.SetPause(false);

        var me = this;
        var target = RaycastForCat(eventData);

        if (me && target && CatMergeManager.Instance &&
            CatMergeManager.Instance.CanMerge(me.level, target.level))
        {
            Vector3 world = target.transform.position;
            CatMergeManager.Instance.DoMerge(me, target, playArea, world);
            return;
        }

        switch (failDrop)
        {
            case FailDropMode.ReturnToStart:
                rt.SetParent(startParent, false);
                rt.anchoredPosition = startAnchored;
                break;
            case FailDropMode.StayWhereDropped:
                rt.SetParent(playArea, true);
                rt.anchoredPosition = ClampToPlayArea(lastLocalDrop);
                break;
            case FailDropMode.SnapBelowTarget:
                if (target != null)
                {
                    var targetRT = target.GetComponent<RectTransform>();
                    Vector2 basePos = targetRT ? targetRT.anchoredPosition
                        : (Vector2)playArea.InverseTransformPoint(target.transform.position);
                    Vector2 snapped = basePos + new Vector2(0f, -snapBelowOffsetY);
                    rt.SetParent(playArea, true);
                    rt.anchoredPosition = ClampToPlayArea(snapped);
                }
                else
                {
                    rt.SetParent(playArea, true);
                    rt.anchoredPosition = ClampToPlayArea(lastLocalDrop);
                }
                break;
        }
    }

    // helpers
    bool CacheLocalFromPointer(PointerEventData eventData, out Vector2 local)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            playArea, eventData.position, rootCanvas ? rootCanvas.worldCamera : null, out local);
    }

    Vector2 ClampToPlayArea(Vector2 pos)
    {
        if (!playArea) return pos;
        Vector2 half = playArea.rect.size * 0.5f;
        pos.x = Mathf.Clamp(pos.x, -half.x + edgeMargin, half.x - edgeMargin);
        pos.y = Mathf.Clamp(pos.y, -half.y + edgeMargin, half.y - edgeMargin);
        return pos;
    }

    CatDragHandler RaycastForCat(PointerEventData eventData)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            if (r.gameObject == gameObject) continue;
            var cu = r.gameObject.GetComponentInParent<CatDragHandler>();
            if (cu) return cu;
        }
        return null;
    }
}
