using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class CatSummonsManager : MonoBehaviour
{
    public static CatSummonsManager Instance { get; private set; }

    [Header("무슨 고양이 가지고 있는지(보유 개수, 인덱스별)")]
    public int[] _haveSummonsCat; // 각 고양이 보유 개수

    [Header("소환권 수량 (로드/하우스/엠퍼러)")]
    public int _haveCommonSummonsCat = 0;
    public int _haveRareSummonsCat = 0;
    public int _haveUniqueSummonsCat = 0;

    [Header("Summons UI")]
    public Button OpenSummonsPanel;
    public Button CloseSummonsPanel;
    public GameObject SummonsPanel;

    public Text Text_CommonAmount;
    public Text Text_RareAmount;
    public Text Text_UniqueAmount;

    public Button[] Btn_Summons; // [0]=Common, [1]=Rare, [2]=Unique
    public GameObject Panel_Summons;

    [Header("Result UI")]
    public Image Img_SummonsResult;
    public Text Text_SummonsResult;
    public Animator Ani_SummonsResult; // 호환용(미사용시 null)
    public GameObject Panel_Result;
    [SerializeField] private Animator resultAnimator; // 결과 패널(씬 오브젝트)의 Animator

    private bool _isRolling = false;

    // === Scroll (VLG/CSF 켠 상태 전용) ===
    [Header("Scroll (VLG/CSF ON)")]
    [SerializeField] private ScrollRect scrollRect;     // Scroll View의 ScrollRect
    [SerializeField] private RectTransform content;     // Viewport 밑 Content (VerticalLayoutGroup + ContentSizeFitter 켜짐)
    [SerializeField] private int maxVisible = 9;        // 화면에 보이는 목표 개수
    [SerializeField] private int edgeBuffer = 1;        // 위/아래 버퍼 1칸(총 2)

    private readonly List<RectTransform> _items = new List<RectTransform>();
    private VerticalLayoutGroup _vlg;
    private float _itemBodyHeight;                      // 아이템 하나 높이
    private float _stride;                              // 한 칸 간격 = _itemBodyHeight + spacing
    private float _padTop;
    private int _dataCount;
    // =====================================

    #region Unity Lifecycle
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (CatManager.Instance == null)
        {
            Debug.LogError("[Summons] CatManager.Instance가 null 입니다. 씬 세팅을 확인하세요.");
            enabled = false;
            return;
        }
        
        // --- Scroll 롤링 초기화 (VLG/CSF ON) ---
        if (scrollRect != null && content != null)
        {
            _vlg = content.GetComponent<VerticalLayoutGroup>();
            if (_vlg != null)
            {
                // (중요) 레이아웃 한 번 강제 갱신 후 측정
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);

                _items.Clear();
                for (int i = 0; i < content.childCount; i++)
                {
                    var rt = content.GetChild(i) as RectTransform;
                    if (!rt) continue;
                    _items.Add(rt);

                    // SetActive는 쓰지 않고 CanvasGroup으로만 숨김/표시
                    var cg = rt.GetComponent<CanvasGroup>();
                    if (!cg) cg = rt.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }

                _dataCount = _items.Count;

                // 첫 아이템 기준 높이/간격 측정
                var first = _items[0];
                _itemBodyHeight = first.rect.height;
                if (_itemBodyHeight <= 0f) _itemBodyHeight = Mathf.Abs(first.sizeDelta.y);
                if (_itemBodyHeight <= 0f) _itemBodyHeight = 100f; // fallback

                _padTop = _vlg.padding.top;
                _stride = _itemBodyHeight + _vlg.spacing;

                // 스크롤 이벤트로 롤링
                scrollRect.onValueChanged.AddListener(_ => SliderUI());

                // 초기 1회 적용
                SliderUI();
            }
            else
            {
                Debug.LogWarning("[CatSummonsManager] Content에 VerticalLayoutGroup이 필요합니다.");
            }
        }

        // 보유 고양이 배열 초기화 (이름 배열 길이 기준)
        _haveSummonsCat = new int[CatManager.Instance.catNamesByLevel.Length];

        if (OpenSummonsPanel) OpenSummonsPanel.onClick.AddListener(OpenUI);
        if (CloseSummonsPanel) CloseSummonsPanel.onClick.AddListener(CloseUI);

        if (Btn_Summons == null || Btn_Summons.Length < 3)
        {
            Debug.LogError("[Summons] Btn_Summons가 3개 미만입니다. 인스펙터 연결을 확인하세요.");
        }
        else
        {
            // 버튼 리스너 연결
            Btn_Summons[0].onClick.AddListener(() => SummonsCat((int)SummonTier.Common));
            Btn_Summons[1].onClick.AddListener(() => SummonsCat((int)SummonTier.Rare));
            Btn_Summons[2].onClick.AddListener(() => SummonsCat((int)SummonTier.Unique));
        }

        RefreshUI();
        if (Panel_Summons) Panel_Summons.SetActive(true);
        if (Panel_Result) Panel_Result.SetActive(false);
    }
    #endregion

    #region UI Open/Close
    private void OpenUI()
    {
        RefreshUI();
        if (SummonsPanel) SummonsPanel.SetActive(true);
        GameManager.Instance.EnableBlock();
    }

    private void CloseUI()
    {
        if (SummonsPanel) SummonsPanel.SetActive(false);
        if (Panel_Summons) Panel_Summons.SetActive(true);
        if (Panel_Result) Panel_Result.SetActive(false);

        GameManager.Instance.DisableBlock();
    }

    private void RefreshUI()
    {
        if (Text_CommonAmount) Text_CommonAmount.text = $"보유 개수 : {_haveCommonSummonsCat}";
        if (Text_RareAmount) Text_RareAmount.text = $"보유 개수 : {_haveRareSummonsCat}";
        if (Text_UniqueAmount) Text_UniqueAmount.text = $"보유 개수 : {_haveUniqueSummonsCat}";

        if (Btn_Summons != null && Btn_Summons.Length >= 3)
        {
            Btn_Summons[0].interactable = _haveCommonSummonsCat >= 1 && !_isRolling;
            Btn_Summons[1].interactable = _haveRareSummonsCat >= 1 && !_isRolling;
            Btn_Summons[2].interactable = _haveUniqueSummonsCat >= 1 && !_isRolling;
        }
    }

    // TODO: 아래위로 10개까지만 보여주고, 넘어가면 자동 비활/활성. (스크롤 풀링)
    private void SliderUI()
    {
        // VLG/CSF 켠 상태에서 "maxVisible + 위/아래 edgeBuffer"만 보이도록 CanvasGroup으로 표시/숨김
        if (scrollRect == null || content == null || _vlg == null || _items.Count == 0) return;

        // 뷰포트 높이 → 실제 보이는 칸 수(동적 계산; 뷰포트가 딱 9칸이 아닐 수 있음)
        var viewport = scrollRect.viewport ? scrollRect.viewport : content.parent as RectTransform;
        float viewportH = viewport ? viewport.rect.height : maxVisible * _stride;
        int visibleCount = Mathf.Max(1, Mathf.CeilToInt((viewportH + 0.5f) / _stride));

        // 현재 스크롤 위치 → 최상단 인덱스 (패딩 고려)
        float scrollY = content.anchoredPosition.y;
        if (scrollY < 0f) scrollY = 0f;
        float yFromFirstItem = Mathf.Max(0f, scrollY - _padTop);

        int rawFirst = Mathf.FloorToInt(yFromFirstItem / _stride);
        int maxFirst = Mathf.Max(0, _dataCount - visibleCount);
        int firstIndex = Mathf.Clamp(rawFirst, 0, maxFirst);

        int start = Mathf.Max(0, firstIndex - edgeBuffer);
        int end = Mathf.Min(_dataCount - 1, firstIndex + visibleCount - 1 + edgeBuffer);

        // 보이기/가리기 (SetActive X, CanvasGroup만 사용)
        for (int i = 0; i < _dataCount; i++)
        {
            var cg = _items[i].GetComponent<CanvasGroup>();
            bool visible = (i >= start && i <= end);

            if (visible)
            {
                if (cg.alpha != 1f)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }
            else
            {
                if (cg.alpha != 0f)
                {
                    cg.alpha = 0f;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }
            }
        }
    }
    #endregion

    #region Summon Core
    public enum SummonTier { Common = 1, Rare = 2, Unique = 3 }

    private bool IsTicketEnough(SummonTier tier)
    {
        switch (tier)
        {
            case SummonTier.Common: return _haveCommonSummonsCat >= 1;
            case SummonTier.Rare: return _haveRareSummonsCat >= 1;
            case SummonTier.Unique: return _haveUniqueSummonsCat >= 1;
        }
        return false;
    }

    private void ConsumeTicket(SummonTier tier)
    {
        switch (tier)
        {
            case SummonTier.Common: _haveCommonSummonsCat--; break;
            case SummonTier.Rare: _haveRareSummonsCat--; break;
            case SummonTier.Unique: _haveUniqueSummonsCat--; break;
        }
    }

    public void AddTicket(SummonTier tier)
    {
        switch (tier)
        {
            case SummonTier.Common: _haveCommonSummonsCat++; break;
            case SummonTier.Rare: _haveRareSummonsCat++; break;
            case SummonTier.Unique: _haveUniqueSummonsCat++; break;
        }
    }

    /// <summary>
    /// 고양이 소환하기
    /// index = 1 : Common | 2 : Rare | 3 : Unique
    /// </summary>
    private void SummonsCat(int index = -1)
    {
        if (_isRolling) return; // 중복 방지
        if (index == -1) { Debug.LogWarning("[Summons] index가 -1 입니다."); return; }
        if (CatManager.Instance == null) { Debug.LogError("[Summons] CatManager.Instance가 null"); return; }

        var prefabs = CatManager.Instance.catPrefabsByLevel;
        var names = CatManager.Instance.catNamesByLevel;

        if (prefabs == null || names == null)
        {
            Debug.LogError("[Summons] 고양이 데이터 배열이 null");
            return;
        }
        if (prefabs.Length != names.Length)
        {
            Debug.LogError($"[Summons] 데이터 불일치: prefabLen={prefabs.Length}, nameLen={names.Length}");
            return;
        }
        if (prefabs.Length == 0)
        {
            Debug.LogError("[Summons] 고양이 데이터가 비어 있습니다.");
            return;
        }

        var tier = (SummonTier)index;
        if (!IsTicketEnough(tier))
        {
            Debug.LogWarning($"[Summons] {tier} 소환권이 부족합니다.");
            return;
        }

        _isRolling = true;
        SetAllButtons(false);

        if (Panel_Summons) Panel_Summons.SetActive(false);
        if (Panel_Result) Panel_Result.SetActive(true);

        int selected = -1;

        if (tier == SummonTier.Common)
        {
            // 0~5
            int[] weights = { 30, 20, 20, 10, 10, 10 };
            int usable = Mathf.Min(weights.Length, prefabs.Length, names.Length);
            if (usable < 1 || !TrySelectByWeights(weights, usable, out int local)) goto FAIL;
            selected = local; // 0..usable-1
        }
        else if (tier == SummonTier.Rare)
        {
            // 4~15 (12개)
            int minIdx = 4;
            int maxIdx = 15;

            if (minIdx >= prefabs.Length)
            {
                Debug.LogError($"[Summons-Rare] minIdx({minIdx})가 데이터 길이({prefabs.Length})를 초과");
                goto FAIL;
            }
            maxIdx = Mathf.Min(maxIdx, prefabs.Length - 1);
            if (maxIdx < minIdx)
            {
                Debug.LogError($"[Summons-Rare] 범위가 유효하지 않음: {minIdx}~{maxIdx}");
                goto FAIL;
            }

            int[] weightsFull = { 30, 20, 10, 10, 6, 6, 5, 3, 3, 3, 3, 1 };
            int needed = (maxIdx - minIdx + 1);
            int[] weights = new int[needed];
            for (int i = 0; i < needed; i++)
                weights[i] = (i < weightsFull.Length) ? Mathf.Max(0, weightsFull[i]) : 1;

            if (!TrySelectByWeights(weights, weights.Length, out int offset)) goto FAIL;
            selected = minIdx + offset;
        }
        else if (tier == SummonTier.Unique)
        {
            // 3) index == 3 : 9~30 (포함) 중 가중치
            int minIdx = 9;
            int maxIdx = 30;

            if (minIdx >= prefabs.Length)
            {
                Debug.LogError($"[Summons-Unique] minIdx({minIdx})가 데이터 길이({prefabs.Length})를 초과");
                goto FAIL;
            }
            maxIdx = Mathf.Min(maxIdx, prefabs.Length - 1);
            if (maxIdx < minIdx)
            {
                Debug.LogError($"[Summons-Unique] 범위가 유효하지 않음: {minIdx}~{maxIdx}");
                goto FAIL;
            }

            int[] weightsFull = {
                18,10,8,8,7,5,5,        // 9~15
                4,4,4,3,3,3,3,3,3,2,    // 16~25
                2,2,1,1,1,              // 26~30
            };

            int needed = (maxIdx - minIdx + 1);
            int[] weights = new int[needed];
            for (int i = 0; i < needed; i++)
                weights[i] = (i < weightsFull.Length) ? Mathf.Max(0, weightsFull[i]) : 1;

            if (!TrySelectByWeights(weights, weights.Length, out int offset)) goto FAIL;
            selected = minIdx + offset;
        }
        else
        {
            Debug.LogWarning($"[Summons] 지원하지 않는 index: {index}");
            goto FAIL;
        }

        if (!TryApplyResult(selected, prefabs, names)) goto FAIL;

        // 보유 고양이 증가
        if (!AddOwnedCat(selected, 1)) goto FAIL;

        // 소환권 소모
        ConsumeTicket(tier);

        Debug.Log($"[Summons-{tier}] 뽑힌 고양이: {names[selected]} (Index:{selected})");

        RefreshUI();
        _isRolling = false;
        SetAllButtons(true);
        return;

    FAIL:
        _isRolling = false;
        SetAllButtons(true);
        Debug.LogError("[Summons] 소환 처리 실패");
    }
    #endregion

    #region Random, Result, Anim
    /// <summary>
    /// 가중치 배열(길이 = usableLength)에서 하나를 뽑아 인덱스를 반환.
    /// 합계 0, 음수 가중치 등 비정상이면 false.
    /// </summary>
    private bool TrySelectByWeights(int[] weights, int usableLength, out int selected)
    {
        selected = -1;
        if (weights == null || usableLength <= 0) return false;

        int total = 0;
        for (int i = 0; i < usableLength; i++)
        {
            int w = Mathf.Max(0, weights[i]); // 음수 방지
            weights[i] = w;
            total += w;
        }
        if (total <= 0) return false;

        int rand = UnityEngine.Random.Range(0, total); // [0, total)
        int cumulative = 0;
        for (int i = 0; i < usableLength; i++)
        {
            cumulative += weights[i];
            if (rand < cumulative)
            {
                selected = i;
                return true;
            }
        }
        // 이론상 도달 불가지만, 안전망
        selected = usableLength - 1;
        return true;
    }

    /// <summary>
    /// 선택된 인덱스의 프리팹/이름을 UI와 애니메이터에 안전하게 적용.
    /// 컴포넌트가 없으면 에러 로그 후 false.
    /// </summary>
    private bool TryApplyResult(int selected, GameObject[] prefabs, string[] names)
    {
        if (selected < 0 || selected >= prefabs.Length)
        {
            Debug.LogError($"[Summons] selected({selected}) 범위 초과. len={prefabs.Length}");
            return false;
        }

        var go = prefabs[selected];
        if (go == null)
        {
            Debug.LogError($"[Summons] prefabs[{selected}] 가 null");
            return false;
        }

        // 이미지/애니메이터는 자식에 있을 수도 있으니 InChildren으로 안전 탐색
        var imgComp = go.GetComponentInChildren<Image>(true);
        var aniComp = go.GetComponentInChildren<Animator>(true);

        if (Img_SummonsResult == null)
        {
            Debug.LogError("[Summons] Img_SummonsResult UI 참조가 null");
            return false;
        }
        if (Text_SummonsResult == null)
        {
            Debug.LogError("[Summons] Text_SummonsResult UI 참조가 null");
            return false;
        }

        if (imgComp == null)
        {
            Debug.LogError($"[Summons] 선택 프리팹에 Image 컴포넌트가 없습니다. index={selected}, name={(selected < names.Length ? names[selected] : "N/A")}");
            return false;
        }
        // UI 스프라이트/텍스트 반영
        Img_SummonsResult.sprite = imgComp.sprite;
        Text_SummonsResult.text = (selected < names.Length) ? names[selected] : $"Cat_{selected}";

        // ==== 결과 패널 애니메이터 적용 ====
        if (aniComp == null || aniComp.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"[Summons] 선택 프리팹에 Animator/Controller 없음. index={selected}, name={(selected < names.Length ? names[selected] : "N/A")}");
            if (resultAnimator != null) resultAnimator.runtimeAnimatorController = null; // 잔상 방지
            Ani_SummonsResult = aniComp; // 호환용
            return true;
        }

        if (resultAnimator != null)
        {
            resultAnimator.runtimeAnimatorController = aniComp.runtimeAnimatorController;
            resultAnimator.updateMode = AnimatorUpdateMode.Normal; // 필요 시 UnscaledTime 등으로 변경
            resultAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (HasParameter(resultAnimator, "Play", AnimatorControllerParameterType.Trigger))
                resultAnimator.SetTrigger("Play");
            else
                resultAnimator.Play(0, 0, 0f);
        }
        else
        {
            Ani_SummonsResult = aniComp; // 호환용
            Debug.LogWarning("[Summons] resultAnimator가 할당되지 않아 컨트롤러 교체 대신 참조만 저장했습니다. (인스펙터에 결과 패널 Animator 연결 권장)");
        }

        return true;
    }
    public bool TryConsumeTicketByTier(int tierOrIndex, int amount = 1)
    {
        if (amount <= 0) return false;
        if (tierOrIndex < 0) return false;

        _haveSummonsCat[tierOrIndex]--;
        RefreshUI();
        return true;
    }
    private bool HasParameter(Animator anim, string name, AnimatorControllerParameterType type)
    {
        if (anim == null) return false;
        foreach (var p in anim.parameters)
            if (p.type == type && p.name == name) return true;
        return false;
    }
    #endregion

    #region Owned Cats & Legacy Methods
    private bool AddOwnedCat(int catIndex, int amount = 1)
    {
        if (_haveSummonsCat == null || catIndex < 0 || catIndex >= _haveSummonsCat.Length)
        {
            Debug.LogError($"[CatSummonsManager] AddOwnedCat 범위 오류 index={catIndex}");
            return false;
        }
        if (amount <= 0)
        {
            Debug.LogError($"[CatSummonsManager] AddOwnedCat 잘못된 amount={amount}");
            return false;
        }

        _haveSummonsCat[catIndex] += amount;
        Debug.Log($"[CatSummonsManager] 보유 고양이 증가 | {CatManager.Instance.catNamesByLevel[catIndex]} +{amount}");

        CatManager.Instance.TotalSummonedCats += amount;
        return true;
    }

    /// <summary>
    /// [레거시] 소환권을 '고양이별'로 더하던 함수 이름. 현재 설계에서는 사용하지 않음.
    /// 다른 곳에서 호출 중일 수 있어 남겨두고 경고만 표시.
    /// </summary>
    [Obsolete("AddSummonsCatTicket는 레거시입니다. 티켓은 Common/Rare/Unique 카운터로만 관리하세요.")]
    public bool AddSummonsCatTicket(int index = 0, int amount = 0)
    {
        Debug.LogWarning("[CatSummonsManager] AddSummonsCatTicket는 레거시 입니다. 현재 설계에서는 사용하지 않습니다.");
        if (amount <= 0)
        {
            Debug.LogError($"[CatSummonsManager] AddSummonsCatTicket에 잘못된 값 | {amount}");
            return false;
        }
        // 예전과의 하위 호환: 보유 고양이로 취급
        return AddOwnedCat(index, amount);
    }

    
    #endregion

    #region Helpers
    private void SetAllButtons(bool enabled)
    {
        if (Btn_Summons == null) return;
        foreach (var b in Btn_Summons)
            if (b) b.interactable = enabled;
        RefreshUI(); // 티켓 수량 기반으로 다시 맞춤
    }
    #endregion
}
