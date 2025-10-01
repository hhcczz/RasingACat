using UnityEngine;
using UnityEngine.UI;

public class TossFish : MonoBehaviour
{
    [Header("Refs")]
    public Button btnTossFish;                // 양동이 버튼
    public RectTransform bucket;              // 양동이 RectTransform(= 이 스크립트 붙은 오브젝트 RT도 OK)
    public GameObject[] fishUiPrefab;           // UI용 물고기 프리팹 (Image + FishUiMover)
    public RectTransform playArea;            // 경계로 사용할 패널(없으면 Canvas의 전체 화면 사용)

    public GameObject WarningMaxCatTextBox;

    [Header("Motion")]
    public float speed = 1200f;               // px/sec (Canvas UI 기준)
    [Range(0f, 1.2f)] public float bounciness = 1f; // 1=완전 반사
    public bool randomDir = true;

    [Header("Spawn")]
    public GameObject explodeFxPrefab;          // 펑 이펙트 프리팹(UI)
    public GameObject catPrefab;                // 고양이 프리팹(UI, CatMover 포함)
    public Vector2 explodeDelayRange = new Vector2(1f, 2f);


    [Tooltip("물고기 바뀔때 같이 바뀌는 이미지")]
    public Image[] FishImg;

    [Tooltip("헬스장 UI")]

    public CatGymUI catGymUI;
    private readonly (int min, int max)[] fishToCatLevelRanges = new (int, int)[]
    {
        (0, 0),   // 0레벨 물고기
        (0, 1),   // 1레벨 물고기
        (0, 2),   // 2레벨 물고기
        (0, 3),   // 3레벨 물고기
        (0, 5),   // 4레벨 물고기
        (0, 7),  // 5레벨 물고기
        (0, 10),  // 6레벨 물고기
        (0, 13),  // 7레벨 물고기
        (0, 15),  // 8레벨 물고기
        (0, 18), // 9레벨 물고기
        (0, 21), // 10레벨 물고기
    };



    void Reset()
    {
        // 에디터에서 자동 채움 시도
        btnTossFish = GetComponent<Button>();
        bucket = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (btnTossFish) btnTossFish.onClick.AddListener(() => RunTossFish(false, 0));
        // 이벤트 구독
        if (catGymUI != null)
            catGymUI.OnFishEnhancementChanged += RefreshFishUI;

        // 시작 시 현재 강화레벨 반영
        RefreshFishUI();
    }
    private void RefreshFishUI()
    {
        int level = CatGymUPGradeManager.Instance.CatGymLevel[5];
        if (level < 0 || level >= fishUiPrefab.Length) return;

        var prefabImg = fishUiPrefab[level].GetComponent<Image>();
        if (prefabImg == null) return;

        foreach (var ui in FishImg)
        {
            if (ui != null)
                ui.sprite = prefabImg.sprite;
        }
    }
    /// <summary>
    /// 물고기 소환 함수
    /// pass = 물고기 차감 없이 소환하는 것 ( 나머지 구문은 검사함 )
    /// EX) 물고기 수는 검사 X, 최대 고양이 개수인지는 검사
    /// </summary>
    /// <param name="pass"></param>

    public void RunTossFish(bool pass = false, int forceIndex = -1)
    {
        // 최대 고양이 수를 넘으면 반려시킴
        if (CatManager.Instance.catHaveCount >= CatManager.Instance.catMaxCount)
        {
            if (!WarningMaxCatTextBox.activeSelf && !pass)  // 올바른 상태 체크
            {
                WarningMaxCatTextBox.SetActive(true);
                Invoke(nameof(HideWarningBox), 3f); // 3초 후 실행
            }

            Debug.Log("[TossFish] 소환할 수 있는 최대 고양이 수에 도달했습니다.");
            return;
        }

        // 인벤토리 차감 성공 시만 소환
        if (GameManager.Instance && GameManager.Instance.RemoveFish(1) <= 0 && !pass)
            return;

        // 강화 레벨에 맞는 프리팹 선택
        GameObject fishPrefab = GetFishPrefabByEnhancementLevel();
        if (fishPrefab == null)
        {
            Debug.LogError("[TossFish] 유효한 물고기 프리팹을 찾지 못했습니다.");
            return;
        }

        // 소리 재생
        AudioManager.Instance.PlaySFX(SfxKey.FishSpawn);

        // 반드시 '양동이'의 자식으로 생성
        var go = Instantiate(fishPrefab, bucket);
        var rt = go.GetComponent<RectTransform>();
        if (!rt) { Debug.LogError("[TossFishUI] 프리팹에 RectTransform이 없습니다."); return; }

        // 시작 위치: 양동이 기준 중앙(원하면 아이콘 아래 등으로 오프셋)
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        // 이동 세팅
        var mover = go.GetComponent<FishTossMover>();
        if (!mover) mover = go.AddComponent<FishTossMover>();

        // 고양이 스폰레벨 결정
        int spawnLevel = GetSpawnLevelFromFish(pass, forceIndex);

        // 추가: FX/고양이/부모/딜레이를 런타임 주입
        mover.SetSpawnContext(playArea, explodeFxPrefab, explodeDelayRange, spawnLevel);

        mover.Init(
            playArea,
            speed,
            bounciness,
            randomDir ? (Vector2?)null : Vector2.right
        );

        CatManager.Instance.AddCatHaveCount(+1); // 고양이 한 마리 추가

    }
    private void HideWarningBox()
    {
        WarningMaxCatTextBox.SetActive(false);
    }
    /// <summary>
    /// Gym 강화 레벨에 맞는 물고기 프리팹을 반환.
    /// - CatGymLevel[5] 를 프리팹 배열 인덱스로 사용 (클램프 안전 처리)
    /// - EnhancementFishValue가 ‘표시용 수치’라면 여기서 굳이 쓰지 않고, 인덱스는 CatGymLevel[5]만 사용.
    ///   (정말 EnhancementFishValue로 매핑해야 한다면 주석의 대안 코드 참고)
    /// </summary>
    private GameObject GetFishPrefabByEnhancementLevel()
    {
        if (fishUiPrefab == null || fishUiPrefab.Length == 0) return null;

        int levelIdx = 0;

        if (CatGymUPGradeManager.Instance != null &&
            CatGymUPGradeManager.Instance.CatGymLevel != null &&
            CatGymUPGradeManager.Instance.CatGymLevel.Length > 5)
        {
            levelIdx = CatGymUPGradeManager.Instance.CatGymLevel[5]; // 5 = EnhancementFish
        }

        // 인덱스 안전 범위
        levelIdx = Mathf.Clamp(levelIdx, 0, fishUiPrefab.Length - 1);

        // 만약 EnhancementFishValue → 프리팹 인덱스 매핑이 필요한 구조라면(예: 0,10,20,30…),
        //    아래처럼 값을 인덱스로 변환하는 함수를 따로 두세요.
        // int value = CatGymUPGradeManager.Instance.EnhancementFishValue[levelIdx];
        // levelIdx = MapFishValueToPrefabIndex(value);

        return fishUiPrefab[levelIdx];
    }
    private int GetSpawnLevelFromFish(bool pass = false, int index = 0)
    {
        var gym = CatGymUPGradeManager.Instance;
        if (gym == null || gym.CatGymLevel.Length <= 5) return 0;

        int fishLevel = Mathf.Clamp(gym.CatGymLevel[5], 0, fishToCatLevelRanges.Length - 1);
        var (min, max) = fishToCatLevelRanges[fishLevel];

        // inclusive Random
        // TODO : Debuging Mode -> Real Mode        현재 : 리얼 모드
        int result = Random.Range(min + CatGymUPGradeManager.Instance.CatGymLevel[6], max + 1);
        //int result = Random.Range(min, CatManager.Instance.catNamesByLevel.Length);
        if (pass) result = index;
        // CatManager 최대레벨 보정
        return CatManager.Instance.ClampLevel(result);
    }
}