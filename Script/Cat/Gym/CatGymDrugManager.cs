using System;
using UnityEngine;

public class CatGymDrugManager : MonoBehaviour
{
    public static CatGymDrugManager Instance { get; private set; }

    public enum DrugType { Flower, Chur, Megician, Factorian, EnergyDrink, Egg }

    [SerializeField] private int[] level = new int[Enum.GetValues(typeof(DrugType)).Length];
    [SerializeField] private int[] maxLevel = new int[Enum.GetValues(typeof(DrugType)).Length];

    // 능력 테이블
    private int[] flowerAddByLv;
    private int[] churSecByLv;
    private int[] magicianSecByLv;
    private int[] factorianSecByLv;
    private int[] energyDrinkSecByLv;
    private string[] eggGrades = { "커먼 고양이", "레어 고양이", "유니크 고양이" };

    [SerializeField] private int energyDrinkStartSec = 1200; // 20분
    [SerializeField] private int energyDrinkStepSec = 60;   // 레벨당 +1분

    // 쿨타임 시작값(최대) & 감소 스텝(레벨당)
    [SerializeField] private int churBaseSec = 60;
    [SerializeField] private int magBaseSec = 30;
    [SerializeField] private int facBaseSec = 30;

    [SerializeField] private int churReduceStep = 1; // 레벨당 -1초
    [SerializeField] private int magReduceStep = 1;
    [SerializeField] private int facReduceStep = 1;

    //  가격 테이블(타입별): 인덱스 = 레벨, 값 = 그 레벨의 기준가
    // 업그레이드 비용 = priceByType[type][currentLevel + 1]
    private int[][] priceByType;

    public event Action OnChanged;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureMaxDefaults();
        BuildTables();
        BuildPrices(); //  가격 테이블 생성
    }

    private void EnsureMaxDefaults()
    {
        int n = level.Length;
        if (maxLevel == null || maxLevel.Length != n) maxLevel = new int[n];

        for (int i = 0; i < n; i++)
        {
            if (maxLevel[i] <= 0)
            {
                maxLevel[i] = (DrugType)i switch
                {
                    DrugType.Egg => 2,
                    DrugType.EnergyDrink => 20,
                    DrugType.Flower => 100,
                    DrugType.Chur => 60,
                    DrugType.Megician => 40,
                    DrugType.Factorian => 40,
                    _ => 50,
                };
            }
            level[i] = Mathf.Clamp(level[i], 0, maxLevel[i]);
        }

        // Egg 최대레벨을 등급 테이블에 맞춰 보정
        maxLevel[(int)DrugType.Egg] = Mathf.Min(maxLevel[(int)DrugType.Egg], eggGrades.Length - 1);
        level[(int)DrugType.Egg] = Mathf.Clamp(level[(int)DrugType.Egg], 0, maxLevel[(int)DrugType.Egg]);
    }

    // BuildTables() 내부 수정
    private void BuildTables()
    {
        flowerAddByLv = BuildLinear(maxLevel[(int)DrugType.Flower] + 1, 0, 1); 

        churSecByLv = BuildLinear(maxLevel[(int)DrugType.Chur] + 1, 0, churReduceStep);
        magicianSecByLv = BuildLinear(maxLevel[(int)DrugType.Megician] + 1, 0, magReduceStep);
        factorianSecByLv = BuildLinear(maxLevel[(int)DrugType.Factorian] + 1, 0, facReduceStep);

        // 에너지드링크: 20분 시작, 레벨당 +1분
        int maxED = GetMaxLevel(DrugType.EnergyDrink);
        energyDrinkSecByLv = BuildLinear(maxED + 1, energyDrinkStartSec, energyDrinkStepSec);
    }

    //  가격: 단순히 0,1,2,3,… 로 생성 (업그레이드 비용은 next 레벨 인덱스 값을 사용)
    private void BuildPrices()
    {
        int n = level.Length;
        priceByType = new int[n][];

        for (int i = 0; i < n; i++)
        {
            int len = maxLevel[i] + 1;              // 0..max
            var arr = new int[Mathf.Max(1, len)];
            for (int lv = 0; lv < arr.Length; lv++)
                arr[lv] = lv;                        // 0,1,2,3,...

            priceByType[i] = arr;
        }
    }

    private int[] BuildLinear(int length, int start, int step)
    {
        length = Mathf.Max(1, length);
        var arr = new int[length];
        for (int i = 0; i < length; i++) arr[i] = start + step * i;
        return arr;
    }

    // ── Public API ──────────────────────────────────────────────────────────────
    public int GetLevel(DrugType type) => Mathf.Clamp(level[(int)type], 0, maxLevel[(int)type]);
    public int GetMaxLevel(DrugType type) => maxLevel[(int)type];
    public bool IsMax(DrugType type) => GetLevel(type) >= GetMaxLevel(type);

    //  다음 레벨 업그레이드 가격 (MAX면 -1)
    public int GetNextPrice(DrugType type)
    {
        int i = (int)type;
        int cur = GetLevel(type);
        int max = GetMaxLevel(type);
        if (cur >= max) return -1;

        var table = priceByType != null && i < priceByType.Length ? priceByType[i] : null;
        if (table == null) return -1;

        int nextIdx = cur + 1;
        if (nextIdx < 0 || nextIdx >= table.Length) return -1;
        return table[nextIdx];
    }

    public string GetAbilityText(DrugType type)
    {
        int curLv = GetLevel(type);
        int maxLv = GetMaxLevel(type);
        bool isMax = curLv >= maxLv;

        switch (type)
        {
            case DrugType.Flower:
                {
                    int cur = Safe(flowerAddByLv, curLv);
                    int nxt = Safe(flowerAddByLv, Mathf.Min(curLv + 1, maxLv));
                    return isMax
                        ? $"황금 코인 추가 획득 : <color=cyan>+{cur} (MAX)</color>"
                        : $"황금 코인 추가 획득 : <color=cyan>+{cur} → +{nxt}</color>";
                }
            case DrugType.Chur:
                {
                    int curReduce = Safe(churSecByLv, curLv);
                    int nxtReduce = Safe(churSecByLv, Mathf.Min(curLv + 1, maxLv));
                    int curShow = Mathf.Max(0, churBaseSec - curReduce);
                    int nxtShow = Mathf.Max(0, churBaseSec - nxtReduce);
                    return isMax ? $"아이템 소환 : <color=cyan>{curShow}초 (MAX)</color>" : $"아이템 소환 : <color=cyan>{curShow}초 → {nxtShow}초</color>";
                }

            case DrugType.Megician:
                {
                    int curReduce = Safe(magicianSecByLv, curLv);
                    int nxtReduce = Safe(magicianSecByLv, Mathf.Min(curLv + 1, maxLv));
                    int curShow = Mathf.Max(0, magBaseSec - curReduce);
                    int nxtShow = Mathf.Max(0, magBaseSec - nxtReduce);
                    return isMax ? $"자동 소환 : <color=cyan>{curShow}초 (MAX)</color>" : $"자동 소환 : <color=cyan>{curShow}초 → {nxtShow}초</color>";
                }

            case DrugType.Factorian:
                {
                    int curReduce = Safe(factorianSecByLv, curLv);
                    int nxtReduce = Safe(factorianSecByLv, Mathf.Min(curLv + 1, maxLv));
                    int curShow = Mathf.Max(0, facBaseSec - curReduce);
                    int nxtShow = Mathf.Max(0, facBaseSec - nxtReduce);
                    return isMax ? $"자동 합성 : <color=cyan>{curShow}초 (MAX)</color>" : $"자동 합성 : <color=cyan>{curShow}초 → {nxtShow}초</color>";
                }
            case DrugType.EnergyDrink:
                {
                    int cur = Safe(energyDrinkSecByLv, curLv);
                    int nxt = Safe(energyDrinkSecByLv, Mathf.Min(curLv + 1, maxLv));
                    string cs = Formatted.FormatTimeDouble(cur);
                    string ns = Formatted.FormatTimeDouble(nxt);
                    return isMax ? $"시간 : <color=cyan>{cs} (MAX)</color>" : $"시간 : <color=cyan>{cs} → {ns}</color>";
                }
            case DrugType.Egg:
                {
                    string cur = Safe(eggGrades, curLv);
                    string nxt = (curLv + 1 <= maxLv) ? Safe(eggGrades, curLv + 1) : string.Empty;
                    return (isMax || string.IsNullOrEmpty(nxt)) ? $"<color=cyan>{cur} (MAX)</color>" : $"<color=cyan>{cur} → {nxt}</color>";
                }
        }
        return string.Empty;
    }

    // 업그레이드(임시: 가격 차감 검증 없음)
    public bool TryUpgrade(DrugType type)
    {
        int i = (int)type;
        if (level[i] >= maxLevel[i]) return false;

        // TODO: 여기서 GetNextPrice(type) 사용해 코인/재화 차감 검증 추가
        level[i]++;

        OnChanged?.Invoke();
        return true;
    }

    public void SetLevel(DrugType type, int value)
    {
        int i = (int)type;
        level[i] = Mathf.Clamp(value, 0, maxLevel[i]);
        OnChanged?.Invoke();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private T Safe<T>(T[] arr, int idx)
    {
        if (arr == null || idx < 0 || idx >= arr.Length) return default;
        return arr[idx];
    }
    public int GetCurrentAbility(DrugType type)
    {
        int curLv = GetLevel(type);

        return type switch
        {
            DrugType.Flower => Safe(flowerAddByLv, curLv),                 // 황금 코인 추가 획득량
            DrugType.Chur => Mathf.Max(0, churBaseSec - Safe(churSecByLv, curLv)),   // 아이템 소환 쿨타임
            DrugType.Megician => Mathf.Max(0, magBaseSec - Safe(magicianSecByLv, curLv)),// 자동 소환 쿨타임
            DrugType.Factorian => Mathf.Max(0, facBaseSec - Safe(factorianSecByLv, curLv)),// 자동 합성 쿨타임
            DrugType.EnergyDrink => Safe(energyDrinkSecByLv, curLv),            // 지속 시간
            DrugType.Egg => curLv,                                      // 단순 레벨 or 등급 (필요시 eggGrades[curLv] 사용)
            _ => 0,
        };
    }
}