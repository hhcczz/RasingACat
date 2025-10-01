using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CatGymUPGradeManager : MonoBehaviour
{
    // ===== Singleton =====
    public static CatGymUPGradeManager Instance { get; private set; }

    // 업그레이드 종류 (UI 슬롯과 매칭)
    public enum GymType
    {
        GetDoubleCatCoinProbability, // 0: 고양이 코인 2배 확률
        GetThreeCatCoinProbability,  // 1: 고양이 코인 3배 확률
        IncreaseMaxCat,              // 2: 고양이 최대 수 증가
        IncreaseMaxFish,             // 3: 물고기 최대 수 증가
        DecreaseFishTime,            // 4: 물고기 충전 시간 감소
        EnhancementFish,             // 5: 물고기 강화 (종류 증가)
        EnhancementBucket            // 6: 물고기 통 강화 (최소등급 올리기)
    }

    // ===== 기본(폴백) 맥스 =====
    // ※ 실제 사용 맥스는:  min(오버라이드>0? 오버라이드 : 아래 기본, "테이블길이-1")
    private const int DEF_MAX_DOUBLE = 160; // 0.5% * 160 = 80%
    private const int DEF_MAX_TRIPLE = 400; // 0.2% * 400 = 80%
    private const int DEF_MAX_CAT = 20;
    private const int DEF_MAX_FISH = 20;
    private const int DEF_MAX_FISHTIME = 20;  // 표시는 21칸(초기 포함)
    private const int DEF_MAX_EFISH = 9;   // 10종(0~9)
    private const int DEF_MAX_EBUCKET = 9;   // 10종(0~9)

    [Header("CatGym 업그레이드 현황 (현재 레벨)")]
    [Min(0)] public int[] CatGymLevel;

    [Header("맥스 레벨 오버라이드 (0 = 자동)")]
    [Tooltip("0이면 테이블 길이 기준 자동. >0이면 그 값으로 강제하되 테이블 길이를 넘으면 절삭.")]
    public int[] MaxLevelOverride = new int[7]; // 인덱스 = GymType

    [Header("효과 테이블 (레벨별 값)")]
    public float[] GetDoubleCatCoinProbabilityValue; // 0~80 (%)
    public float[] GetThreeCatCoinProbabilityValue;  // 0~80 (%)
    public int[] IncreaseMaxCatValue;              // 고양이 최대치
    public int[] IncreaseMaxFishValue;             // 물고기 최대치
    public float[] DecreaseFishTimeValue;            // 충전 시간(초)
    public string[] EnhancementFishValue;            // 물고기 이름(레벨별)
    public string[] EnhancementBucketValue;          // 물고기 통 이름(레벨별)

    public Dictionary<GymType, long[]> costTable;

    // ===== Unity lifecycle =====
    void Awake()
    {
        if (Application.isPlaying)
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Instance = this; }
    }

    void Reset() => InitialSetting();

    private void Start() => InitialSetting();

    private void OnValidate()
    {
        // 에디터에서 값 변경 시에도 레벨을 맥스에 맞춰 정리
        ClampAllLevelsToMax();
    }

    // ===== 초기 세팅 =====
    [ContextMenu("Initial Setting (Tables & Levels)")]
    public void InitialSetting()
    {
        // --- 효과 테이블 초기화 (기존 규칙 유지) ---
        GetDoubleCatCoinProbabilityValue = new float[DEF_MAX_DOUBLE + 1]; // 0..160
        for (int i = 0; i < GetDoubleCatCoinProbabilityValue.Length; i++)
            GetDoubleCatCoinProbabilityValue[i] = Mathf.Clamp(0.5f * i, 0f, 80f);

        GetThreeCatCoinProbabilityValue = new float[DEF_MAX_TRIPLE + 1]; // 0..400
        for (int i = 0; i < GetThreeCatCoinProbabilityValue.Length; i++)
            GetThreeCatCoinProbabilityValue[i] = Mathf.Clamp(0.2f * i, 0f, 80f);

        IncreaseMaxCatValue = new int[DEF_MAX_CAT + 1];   // 0..20
        IncreaseMaxFishValue = new int[DEF_MAX_FISH + 1];  // 0..20
        for (int i = 0; i < IncreaseMaxCatValue.Length; i++) IncreaseMaxCatValue[i] = 5 + i;
        for (int i = 0; i < IncreaseMaxFishValue.Length; i++) IncreaseMaxFishValue[i] = 5 + i;

        DecreaseFishTimeValue = new float[DEF_MAX_FISHTIME + 1]; // 0..20 (표시는 21칸)
        for (int i = 0; i < DecreaseFishTimeValue.Length; i++)
            DecreaseFishTimeValue[i] = 5.0f - 0.1f * i; // 5.0 → ... → 3.0

        // 물고기 종류(기본 10종)
        EnhancementFishValue = new[]
        {
            "멸치", "흰동가리", "꽃게", "복어", "서전피시",
            "천사물고기", "배스", "메기", "금붕어", "무지개 송어",
        };

        // 물고기 통 이름(기본 10)
        EnhancementBucketValue = new string[10];
        BuildEnhancementBucketFromCats();

        // 슬롯 개수(7개) 맞춰 레벨 배열 준비
        if (CatGymLevel == null || CatGymLevel.Length != 7)
            CatGymLevel = new int[7];

        // 비용 테이블
        InitialCostSetting();

        // 레벨을 맥스에 맞춰 정리
        ClampAllLevelsToMax();
    }

    // ===== 맥스 레벨 계산/사용 =====

    /// <summary>
    /// 타입별 “실제 사용” 맥스 레벨.
    /// - 우선순위: MaxLevelOverride > 0 ? 그 값 : 기본값(DEF_*)
    /// - 최종값은 반드시 테이블 길이를 넘지 않도록 (values.Length - 1) 로 절삭
    /// </summary>
    public int GetMaxLevel(GymType type)
    {
        int overrideVal = (MaxLevelOverride != null && MaxLevelOverride.Length == 7)
            ? MaxLevelOverride[(int)type] : 0;

        int baseMax = type switch
        {
            GymType.GetDoubleCatCoinProbability => DEF_MAX_DOUBLE,
            GymType.GetThreeCatCoinProbability => DEF_MAX_TRIPLE,
            GymType.IncreaseMaxCat => DEF_MAX_CAT,
            GymType.IncreaseMaxFish => DEF_MAX_FISH,
            GymType.DecreaseFishTime => DEF_MAX_FISHTIME,
            GymType.EnhancementFish => DEF_MAX_EFISH,
            GymType.EnhancementBucket => DEF_MAX_EBUCKET,
            _ => 0
        };

        int desired = (overrideVal > 0) ? overrideVal : baseMax;

        // 실제 테이블 길이에 맞춰 안전 절삭
        int tableMax = GetTableMaxLevel(type); // values.Length - 1 (없으면 0)
        return Mathf.Clamp(desired, 0, tableMax);
    }

    /// <summary>
    /// 해당 타입의 “값 테이블”이 허용하는 최대 인덱스(= Length - 1)
    /// </summary>
    private int GetTableMaxLevel(GymType type)
    {
        int len = type switch
        {
            GymType.GetDoubleCatCoinProbability => GetDoubleCatCoinProbabilityValue?.Length ?? 0,
            GymType.GetThreeCatCoinProbability => GetThreeCatCoinProbabilityValue?.Length ?? 0,
            GymType.IncreaseMaxCat => IncreaseMaxCatValue?.Length ?? 0,
            GymType.IncreaseMaxFish => IncreaseMaxFishValue?.Length ?? 0,
            GymType.DecreaseFishTime => DecreaseFishTimeValue?.Length ?? 0,
            GymType.EnhancementFish => EnhancementFishValue?.Length ?? 0,
            GymType.EnhancementBucket => EnhancementBucketValue?.Length ?? 0,
            _ => 0
        };
        return Mathf.Max(0, len - 1);
    }

    public bool IsMax(GymType type, int level) => level >= GetMaxLevel(type);

    public void ClampAllLevelsToMax()
    {
        if (CatGymLevel == null || CatGymLevel.Length != 7) return;
        for (int i = 0; i < CatGymLevel.Length; i++)
        {
            int max = GetMaxLevel((GymType)i);
            CatGymLevel[i] = Mathf.Clamp(CatGymLevel[i], 0, max);
        }
    }

    // ===== 비용 설정/조회 유틸 =====
    public void SetCost(GymType type, params long[] cost)
    {
        costTable ??= new Dictionary<GymType, long[]>();
        costTable[type] = cost ?? Array.Empty<long>();
    }

    public long GetCatUPGradeCost(GymType type, int curLv)
    {
        if (costTable == null || !costTable.TryGetValue(type, out var arr) || arr == null) return -1;
        // 맥스 레벨이면 더 이상 업그레이드 없음
        if (curLv >= GetMaxLevel(type)) return -1;
        // 비용 테이블 길이 밖이면 업그레이드 불가 처리
        if (curLv < 0 || curLv >= arr.Length) return -1;
        return arr[curLv]; // “현재 레벨 → 다음 레벨” 비용
    }

    public long GetCatUPGradeCost(int typeIndex, int curLv)
    {
        var max = Enum.GetValues(typeof(GymType)).Length;
        if (typeIndex < 0 || typeIndex >= max) return -1;
        return GetCatUPGradeCost((GymType)typeIndex, curLv);
    }

    // ===== 유틸 =====
    public float GetDoubleProbPercentByLevel(int levelIndex)
    {
        if (GetDoubleCatCoinProbabilityValue == null || GetDoubleCatCoinProbabilityValue.Length == 0) return 0f;
        int idx = Mathf.Clamp(levelIndex, 0, GetDoubleCatCoinProbabilityValue.Length - 1);
        return GetDoubleCatCoinProbabilityValue[idx];
    }

    public float GetDoubleProb01ByLevel(int levelIndex)
        => Mathf.Clamp01(GetDoubleProbPercentByLevel(levelIndex) / 100f);

    // 물고기 통 이름 생성
    private void BuildEnhancementBucketFromCats()
    {
        if (EnhancementBucketValue == null || EnhancementBucketValue.Length != 10)
            EnhancementBucketValue = new string[10];

        if (CatManager.Instance == null)
        {
            for (int i = 0; i < 10; i++)
                EnhancementBucketValue[i] = $"Bucket {i + 1}";
            return;
        }

        for (int i = 0; i < 10; i++)
        {
            string name = CatManager.Instance.GetCatName(i);
            EnhancementBucketValue[i] = string.IsNullOrEmpty(name)
                ? $"Bucket {i + 1}"
                : name.Replace("고양이", "").Trim();
#if UNITY_EDITOR
            Debug.Log($"고양이 이름 : {EnhancementBucketValue[i]}");
#endif
        }
    }

    private void InitialCostSetting()
    {
        costTable = new();

        // 2배/3배 확률: 시드 + 자동 증가
        SetDoubleCatCoinCost();
        SetThreeCatCoinCost();

        // 고양이 최대 마릿 수 증가 (업그레이드 가능 횟수 20)
        SetCost(GymType.IncreaseMaxCat,
            100, 300, 1000, 2500, 7500, 10000, 20000, 40000, 80000, 100000,
            300000, 500000, 700000, 1000000, 1500000, 2000000, 2500000, 3000000, 5000000, 200000000);

        // 물고기 최대 마릿 수 증가 (업그레이드 가능 횟수 20)
        SetCost(GymType.IncreaseMaxFish,
            100, 300, 1000, 2500, 7500, 10000, 20000, 40000, 80000, 100000,
            300000, 500000, 700000, 1000000, 1500000, 2000000, 2500000, 3000000, 5000000, 200000000);

        // 물고기 시간 감소 (업그레이드 가능 횟수 20)
        SetCost(GymType.DecreaseFishTime,
            100, 300, 1000, 2500, 7500, 10000, 20000, 40000, 80000, 100000,
            300000, 500000, 700000, 1000000, 1500000, 2000000, 2500000, 3000000, 3000000, 5000000, 200000000);

        // 물고기 타입 (업그레이드 가능 횟수 10)
        SetCost(GymType.EnhancementFish,
            500, 1500, 5000, 25000, 125000, 625000, 3325000, 8000000, 10000000, 200000000);

        // 물고기 통 업그레이드 (업그레이드 가능 횟수 10)
        SetCost(GymType.EnhancementBucket,
            2500000, 4500000, 6500000, 8500000, 12000000, 15000000, 18000000, 24000000, 30000000, 400000000);
    }

    private void SetDoubleCatCoinCost()
    {
        // 1) 시드(초기 5개) 세팅
        SetCost(GymType.GetDoubleCatCoinProbability, 10, 20, 40, 60, 100);

        // 2) 목표 길이 = “실사용 맥스 레벨” (비용 배열은 ‘업그레이드 횟수’만큼 = maxLevel 개)
        int targetLen = GetMaxLevel(GymType.GetDoubleCatCoinProbability);
        if (targetLen <= 0) targetLen = 1;

        // 3) 현 배열 가져오기/확장
        var has = costTable.TryGetValue(GymType.GetDoubleCatCoinProbability, out var costArr) && costArr != null;
        costArr = has ? CopyResize(costArr, targetLen) : new long[targetLen];

        // 4) 증가 파라미터
        var alpha = 1.025f; // 완만 증가
        var rho = 2.1f;   // 10마다(20이상) 가속

        // 5) 인덱스 5부터 자동 생성
        for (int n = 5; n < costArr.Length; n++)
        {
            double prev1 = (n - 1 >= 0) ? costArr[n - 1] : 0;
            costArr[n] = (n % 10 == 0 && n >= 20) ? (long)(prev1 * alpha * rho)
                                                   : (long)(prev1 * alpha);
        }

        costTable[GymType.GetDoubleCatCoinProbability] = costArr;
    }

    private void SetThreeCatCoinCost()
    {
        // 1) 시드(초기 5개) 세팅
        SetCost(GymType.GetThreeCatCoinProbability, 100, 200, 400, 600, 1000);

        // 2) 목표 길이 = “실사용 맥스 레벨”
        int targetLen = GetMaxLevel(GymType.GetThreeCatCoinProbability);
        if (targetLen <= 0) targetLen = 1;

        // 3) 현 배열 가져오기/확장
        var has = costTable.TryGetValue(GymType.GetThreeCatCoinProbability, out var costArr) && costArr != null;
        costArr = has ? CopyResize(costArr, targetLen) : new long[targetLen];

        // 4) 증가 파라미터
        var alpha = 1.035f;
        var rho = 2.65f;

        // 5) 인덱스 5부터 자동 생성
        for (int n = 5; n < costArr.Length; n++)
        {
            double prev1 = (n - 1 >= 0) ? costArr[n - 1] : 0;
            costArr[n] = (n % 10 == 0 && n >= 20) ? (long)(prev1 * alpha * rho)
                                                   : (long)(prev1 * alpha);
        }

        costTable[GymType.GetThreeCatCoinProbability] = costArr;
    }

    private long[] CopyResize(long[] src, int newLen)
    {
        var dst = new long[newLen];
        if (src != null) Array.Copy(src, dst, Math.Min(src.Length, newLen));
        return dst;
    }
}