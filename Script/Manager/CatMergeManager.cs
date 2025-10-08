using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class CatMergeManager : MonoBehaviour
{
    public static CatMergeManager Instance { get; private set; }
    public bool IsMerging { get; private set; } = false;

    [Header("Global")]
    public bool mergeEnabled = true;           // 토글용 On/Off
    public GameObject mergeFxPrefab;           // (선택) 합성 FX UI 프리팹

    [Header("고양이 첫 합성 관련")]
    [Tooltip("고양이 합성 상자")]
    public GameObject _catMergeBox;
    [Tooltip("고양이 사진")]
    public Image _catMergeImage;
    [Tooltip("고양이 애니메이션")]
    public Animator _catMergeAnimator;
    [Tooltip("고양이 이름")]
    public Text _catMergeNameText;
    [Tooltip("고양이 코인 개수")]
    public Text _catMergeValueText;
    [Tooltip("고양이 설명")]
    public Text _catMergeDesc;
    [Tooltip("고양이 합성 상자 닫기 버튼")]
    public Button _catMergeCloseBtn;

    [Header("Jackpot Merge")]
    [Tooltip("0.01% = 0.0001f")]
    public GameObject jackpotFxPrefab;                     // 잭팟 전용 FX 

    [HideInInspector]
    public string[] _catMergeDescList;

    private float autoMergeMoveTime = 0.2f;   // 자동 이동 시간
    private AnimationCurve autoMergeEase = AnimationCurve.EaseInOut(0, 0, 1, 1); // 부드러운 보간

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (_catMergeCloseBtn)
            _catMergeCloseBtn.onClick.AddListener(() => CloseMergeBox());
         CatManager.Instance.OnCatDiscovered += OpenMergeBox; // UI 등에서 구독 가능

        // TODO : 글 채워넣기
        _catMergeDescList = new string[30] { 
            "자랑스러운 첫 번째 나의 고양이\n수상한 마을에서 보내온 첫 번째 고양이다.",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
            "나중에 글 채워넣기",
        };
    }

    private void CloseMergeBox()
    {
        if (_catMergeBox) _catMergeBox.SetActive(false);

        GameManager.Instance.DisableBlock();
    }

    /// <summary>
    /// 고양이 레벨에 맞게 MergeBox UI에 데이터 채운 뒤 표시.
    /// level = 방금 처음으로 달성한 합성 결과 레벨
    /// catMergeMaxLevel 은 "다음에 처음 달성해야 할 목표 레벨"로 level+1 로 갱신한다.
    /// </summary>
    private void OpenMergeBox(int level)
    {
        if (!_catMergeBox) return;

        var cm = CatManager.Instance;
        if (!cm) return;

        // 안전한 인덱싱
        if (level < 0) return;

        // 이미지 & 애니메이터 프리팹에서 직접 따오기
        if (level < cm.catPrefabsByLevel.Length)
        {
            var prefab = cm.catPrefabsByLevel[level];
            if (prefab)
            {
                // 이미지
                var img = prefab.GetComponent<Image>();
                if (_catMergeImage && img) _catMergeImage.sprite = img.sprite;

                // 애니메이터: 컨트롤러/아바타를 UI 박스의 애니메이터에 적용해야 함
                var srcAnim = prefab.GetComponent<Animator>();
                if (srcAnim && _catMergeAnimator)
                {
                    // 컨트롤러/아바타 교체
                    _catMergeAnimator.runtimeAnimatorController = srcAnim.runtimeAnimatorController;
                    _catMergeAnimator.avatar = srcAnim.avatar;

                    // 파라미터/트리거 초기화 & 첫 프레임부터 재생
                    _catMergeAnimator.Rebind();
                    _catMergeAnimator.Update(0f);
                    // 기본 레이어 0의 기본 상태부터 재생 (원하면 특정 스테이트 이름으로 Play)
                    _catMergeAnimator.Play(0, -1, 0f);
                }
            }
        }

        // 이름
        if (_catMergeNameText && cm.catNamesByLevel != null &&
            level < cm.catNamesByLevel.Length)
        {
            _catMergeNameText.text = $"이름 : <color=cyan>{cm.catNamesByLevel[level]}</color>";
        }

        // 코인/초
        if (_catMergeValueText && cm.coinPerSecondByLevel != null &&
            level < cm.coinPerSecondByLevel.Length)
        {
            _catMergeValueText.text =
                $"코인 생성 개수 : <color=cyan>{cm.coinPerSecondByLevel[level]}</color>";
        }

        // 설명
        if (_catMergeDesc)
        {
            string desc = (level >= 0 && level < _catMergeDescList.Length)
                ? _catMergeDescList[level]
                : "설명 없음";
            _catMergeDesc.text = desc;
        }
        GameManager.Instance.EnableBlock();
        // 박스 표시
        _catMergeBox.SetActive(true);

        // 다음 목표 레벨로 갱신 (처음 달성 감지 임계값)
        cm.MarkDiscovered(level);

    }

    public void SetMergeEnabled(bool on) => mergeEnabled = on;

    public bool CanMerge(int aLevel, int bLevel)
    {
        if (!mergeEnabled) return false;
        return CatManager.Instance ? CatManager.Instance.CanMerge(aLevel, bLevel) : (aLevel == bLevel);
    }

    /// <summary>
    /// 두 고양이(CatDragHandler)를 합성해 결과 프리팹으로 교체.
    /// parentForResult: 결과가 붙을 부모(보통 PlayArea)
    /// worldPos: 결과 생성 위치
    /// </summary>
    public GameObject DoMerge(CatDragHandler a, CatDragHandler b, RectTransform parentForResult, Vector3 worldPos)
    {
        if (!a || !b || a == b) return null;
        if (!CanMerge(a.level, b.level)) return null;

        var cm = CatManager.Instance;

        // ── 잭팟 여부 & 스텝 결정 ──
        float bonus = 0f;
        if (GameManager.Instance.OneDayBuffTime > 0) bonus = 0.0005f;
        bool isJackpot = Random.value < Mathf.Clamp01(RelicsManager.Instance.GetAbilityValue(RelicsManager.Type.Merge) + bonus);

        // 현재 레벨 기준 남은 헤드룸(최대 4까지만 허용)
        int cur = a.level;
        int headroom = Mathf.Clamp(cm.catPrefabsByLevel.Length - cur, 1, 4); // 1~4

        int step;
        if (isJackpot)
        {
            // 잭팟이면 +2 ~ +4 사이, 단 헤드룸이 2 미만이면 +1로 폴백
            step = (headroom >= 2) ? Random.Range(2, headroom + 1) : 1; // upper exclusive 아님: +1해서 포함
        }
        else
        {
            step = 1;
        }

        // 업그레이드 규칙 적용
        int next = CatManager.Instance
            ? CatManager.Instance.GetUpgradeTargetByStep(cur, step)
            : cur + step;

        // 최종 안전 클램프 (매니저 클램프 + 레벨캡 30 동시 보장)
        if (CatManager.Instance) next = CatManager.Instance.ClampLevel(next);
        next = Mathf.Min(next, cm.catPrefabsByLevel.Length);

        // 결과 프리팹
        if (!cm || !cm.TryGetPrefab(next, out var resultPrefab) || resultPrefab == null)
        {
            Debug.LogError($"[CatMergeManager] 레벨 {next} 프리팹 없음");
            return null;
        }

        // 결과 생성
        var result = Instantiate(resultPrefab, parentForResult);
        var rt = result.GetComponent<RectTransform>();
        if (rt) { rt.position = worldPos; rt.localScale = Vector3.one; }
        else { result.transform.position = worldPos; }

        // 결과 CatDragHandler 레벨/PlayArea 주입
        var dh = result.GetComponent<CatDragHandler>();
        if (dh)
        {
            dh.level = cm ? cm.ClampLevel(next) : next;
            if (parentForResult) dh.playArea = parentForResult;
        }

        // 이동 컴포넌트가 있다면 PlayArea 주입
        var mover = result.GetComponent<CatMover>();
        if (mover && parentForResult) mover.SetPlayArea(parentForResult);

        // 일반 합성 FX
        if (mergeFxPrefab && parentForResult)
        {
            var fx = Instantiate(mergeFxPrefab, parentForResult);
            var fxRt = fx.GetComponent<RectTransform>();
            if (fxRt) { fxRt.position = worldPos; fxRt.localScale = Vector3.one; }
            else fx.transform.position = worldPos;
        }

        // 잭팟 FX만 추가 (사운드는 기존과 동일)
        if (isJackpot && parentForResult && jackpotFxPrefab)
        {
            var jfx = Instantiate(jackpotFxPrefab, parentForResult);
            var jrt = jfx.GetComponent<RectTransform>();
            if (jrt) { jrt.position = worldPos; jrt.localScale = Vector3.one; }
            else jfx.transform.position = worldPos;
        }

        // 울음소리(기본)
        if (AudioManager.Instance) AudioManager.Instance.PlaySFX(SfxKey.MeowRandom);

        // 원본 제거 및 보유 수 갱신 (2 → 1 이므로 -1)
        Destroy(a.gameObject);
        Destroy(b.gameObject);
        if (cm) cm.AddCatHaveCount(-1);

        // 처음 달성 레벨이면 박스 오픈
        if (cm && !cm.IsCatDiscovered[next])
        {
            cm.MarkDiscovered(next);
            OpenMergeBox(next);
        }

        return result;
    }

    /// <summary>
    /// 소환되어 있는 고양이 중, 같은 레벨 2마리를 찾아
    /// 한쪽을 다른 쪽으로 이동시켜 자동으로 합성.
    /// (한 번 호출에 한 쌍만 처리)
    /// </summary>
    public bool ForceMerge()
    {
        if (IsMerging) return false;

        var all = FindObjectsOfType<CatDragHandler>(includeInactive: false);
        if (all == null || all.Length < 2) return false;

        if (!TryFindBestPair(all, sameParentOnly: true, out var a, out var b))
            if (!TryFindBestPair(all, sameParentOnly: false, out a, out b))
                return false;

        var parent = a.playArea ? a.playArea : (b.playArea ? b.playArea : null);
        var targetPos = GetWorldPos(a);

        IsMerging = true;
        StartCoroutine(MoveAndDoMerge(b.gameObject, targetPos, parent, () =>
        {
            DoMerge(a, b, parent, targetPos);
            IsMerging = false;
        }));

        return true; // 이번 프레임에 합성 시작함
    }

    /// <summary>
    /// 가능한 모든 쌍(같은 레벨)을 찾아 가장 가까운 쌍을 선정.
    /// sameParentOnly=true면 같은 부모(RectTransform)인 경우만.
    /// </summary>
    private bool TryFindBestPair(CatDragHandler[] all, bool sameParentOnly, out CatDragHandler bestA, out CatDragHandler bestB)
    {
        bestA = null; bestB = null;
        float bestDistSqr = float.MaxValue;

        // 레벨별로 그룹핑 (LINQ)
        var groups = all
            .Where(x => x != null && x.gameObject.activeInHierarchy)
            .GroupBy(x => x.level);

        foreach (var g in groups)
        {
            // 같은 레벨이 2마리 이상 있어야 후보
            var list = g.ToList();
            if (list.Count < 2) continue;

            for (int i = 0; i < list.Count; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    var A = list[i];
                    var B = list[j];
                    if (!A || !B) continue;

                    if (sameParentOnly && A.playArea != B.playArea) continue;

                    float d = (GetWorldPos(A) - GetWorldPos(B)).sqrMagnitude;
                    if (d < bestDistSqr)
                    {
                        bestDistSqr = d;
                        bestA = A; bestB = B;
                    }
                }
            }
        }

        return bestA && bestB;
    }

    /// <summary>
    /// RectTransform/Transform 구분 없이 월드 좌표 얻기
    /// </summary>
    private Vector3 GetWorldPos(MonoBehaviour mb)
    {
        if (!mb) return Vector3.zero;
        var rt = mb.GetComponent<RectTransform>();
        return rt ? (Vector3)rt.position : mb.transform.position;
    }

    /// <summary>
    /// 부드럽게 이동한 뒤 콜백 실행
    /// </summary>
    private System.Collections.IEnumerator MoveAndDoMerge(GameObject moverGo, Vector3 toWorldPos, RectTransform parentForResult, System.Action onArrived)
    {
        if (!moverGo)
        {
            onArrived?.Invoke();
            yield break;
        }

        // 이동 대상의 RectTransform/Transform 처리
        var rt = moverGo.GetComponent<RectTransform>();
        Vector3 from = rt ? (Vector3)rt.position : moverGo.transform.position;

        float t = 0f;
        float dur = Mathf.Max(0.01f, autoMergeMoveTime);

        while (t < dur && moverGo)
        {
            t += Time.deltaTime;
            float k = autoMergeEase != null ? autoMergeEase.Evaluate(t / dur) : (t / dur);
            Vector3 p = Vector3.Lerp(from, toWorldPos, k);

            if (rt) rt.position = p; else moverGo.transform.position = p;
            yield return null;
        }

        // 도착 스냅
        if (moverGo)
        {
            if (rt) rt.position = toWorldPos; else moverGo.transform.position = toWorldPos;
        }

        onArrived?.Invoke();
    }
}