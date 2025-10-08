using UnityEngine;
using System;
using System.Collections.Generic;

public class CatManager : MonoBehaviour
{
    // ───────── Singleton ─────────
    public static CatManager Instance { get; private set; }

    public readonly List<CatDragHandler> ActiveCats = new List<CatDragHandler>();
    public IReadOnlyList<CatDragHandler> ActiveCatsRO => ActiveCats;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ValidateLengths();
    }
    public int GetCatCounts() => ActiveCats.Count;

    public void Register(CatDragHandler cat)
    {
        if (!cat) return;
        if (!ActiveCats.Contains(cat)) ActiveCats.Add(cat);
    }

    public void Unregister(CatDragHandler cat)
    {
        if (!cat) return;
        ActiveCats.Remove(cat);
    }


    private void Start()
    {
        coinPerSecondByLevel[0] = 1;
        coinPerSecondByLevel[1] = 3;
        coinPerSecondByLevel[2] = 6;
        coinPerSecondByLevel[3] = 10;
        coinPerSecondByLevel[4] = 15;

        var alpha = 1.1f;
        var rho = 1.2f;

        // seeds: a[1]=1, a[2]=2, a[3]=3, a[4]=4 등
        for (int n = 5; n < catPrefabsByLevel.Length; n++)
        {
            double delta = Math.Floor(Math.Sqrt(alpha * Math.Pow(rho, n)));
            coinPerSecondByLevel[n] = coinPerSecondByLevel[n - 1] + coinPerSecondByLevel[n - 4] + (int)delta;
        }
    }

    // ───────── Data: Prefab / Stats / Names ─────────
    [Header("레벨별 프리팹 (0레벨부터 순서대로)")]
    public GameObject[] catPrefabsByLevel;

    [Header("레벨별 초당 코인 생산량 (Paw Stamp / sec)")]
    public long[] coinPerSecondByLevel;

    [Header("레벨별 고양이 이름 (0레벨부터 순서대로)")]
    public string[] catNamesByLevel;

    [Header("현재 고양이 보유 수")]
    [Min(0)] public int catHaveCount = 0;

    [Header("최대 고양이 보유 수")]
    [Min(1)] public int catMaxCount = 5;

    [Header("발견 여부 (레벨별)")]
    public bool[] IsCatDiscovered;   // 고양이 레벨별 발견 여부
    public event Action<int> OnCatDiscovered; // (선택) 발견 이벤트

    // 이벤트: 현재/최대
    public event Action<int, int> OnCatCountChanged;

    public int TotalSummonedCats = 0;



    // ───────── Properties ─────────
    public int MaxLevel => Mathf.Max(0, (catPrefabsByLevel?.Length ?? 1) - 1);
    public bool IsValidLevel(int level) => level >= 0 && level < (catPrefabsByLevel?.Length ?? 0);
    public int ClampLevel(int level) => Mathf.Clamp(level, 0, MaxLevel);

    // ───────── Query: Prefab / Stats / Name ─────────
    public GameObject GetPrefab(int level)
    {
        if (!IsValidLevel(level))
        {
            Debug.LogWarning($"[CatManager] 잘못된 레벨 요청: {level}");
            return null;
        }
        return catPrefabsByLevel[level];
    }

    public bool TryGetPrefab(int level, out GameObject prefab)
    {
        prefab = GetPrefab(level);
        return prefab != null;
    }

    public long GetCoinPerSecond(int level)
    {
        if (coinPerSecondByLevel != null &&
            level >= 0 && level < coinPerSecondByLevel.Length)
            return Math.Max(0, coinPerSecondByLevel[level]);
        return 0;
    }

    public float GetCoinsForDelta(int level, float deltaSeconds)
        => GetCoinPerSecond(level) * Mathf.Max(0f, deltaSeconds);

    public string GetCatName(int level)
    {
        if (catNamesByLevel != null &&
            level >= 0 && level < catNamesByLevel.Length &&
            !string.IsNullOrEmpty(catNamesByLevel[level]))
            return catNamesByLevel[level];

        // 기본 fallback
        return $"Cat Lv.{ClampLevel(level)}";
    }

    public void SetCatName(int level, string name)
    {
        if (catPrefabsByLevel == null) return;
        if (catNamesByLevel == null || catNamesByLevel.Length != catPrefabsByLevel.Length)
            ResizeAndCopyNames();

        level = ClampLevel(level);
        catNamesByLevel[level] = name;
    }

    public void SetCatNames(string[] names) // 일괄 세팅용
    {
        if (catPrefabsByLevel == null || names == null) return;
        int n = catPrefabsByLevel.Length;
        if (catNamesByLevel == null || catNamesByLevel.Length != n)
            catNamesByLevel = new string[n];

        for (int i = 0; i < n; i++)
            catNamesByLevel[i] = i < names.Length && !string.IsNullOrEmpty(names[i])
                ? names[i]
                : DefaultName(i);
    }

    // ───────── Upgrade API ─────────
    public int GetUpgradeTargetByStep(int currentLevel, int step)
        => ClampLevel(currentLevel + Mathf.Max(0, step));

    public int GetUpgradeTargetTo(int currentLevel, int targetLevel, bool allowDowngrade = false)
    {
        int target = ClampLevel(targetLevel);
        if (!allowDowngrade && target < currentLevel) return currentLevel;
        return target;
    }

    public bool CanMerge(int aLevel, int bLevel) => aLevel == bLevel;

    // ───────── Spawn Utilities ─────────
    public GameObject SpawnCat(int level, RectTransform parent, Vector3 worldPos)
    {
        if (!TryGetPrefab(level, out var prefab)) return null;

        var go = Instantiate(prefab, parent);
        var rt = go.GetComponent<RectTransform>();
        if (rt) { rt.position = worldPos; rt.localScale = Vector3.one; }
        else { go.transform.position = worldPos; }

        MarkDiscovered(level);

        return go;
    }

    // ───────── Validation ─────────
    void OnValidate() => ValidateLengths();

    void ValidateLengths()
    {
        if (catPrefabsByLevel == null) return;
        int n = catPrefabsByLevel.Length;

        // coinPerSecondByLevel 길이 맞추기(부족분 0)
        if (coinPerSecondByLevel == null || coinPerSecondByLevel.Length != n)
        {
            var arr = new long[n];
            if (coinPerSecondByLevel != null)
                for (int i = 0; i < Mathf.Min(coinPerSecondByLevel.Length, n); i++)
                    arr[i] = coinPerSecondByLevel[i];
            coinPerSecondByLevel = arr;
        }

        // 발견 여부 배열 길이 맞추기
        if (IsCatDiscovered == null || IsCatDiscovered.Length != n)
        {
            var arr = new bool[n];
            if (IsCatDiscovered != null)
                for (int i = 0; i < Mathf.Min(IsCatDiscovered.Length, n); i++)
                    arr[i] = IsCatDiscovered[i];
            IsCatDiscovered = arr;
        }

        // catNamesByLevel 길이 맞추기(부족분 기본 이름)
        if (catNamesByLevel == null || catNamesByLevel.Length != n)
        {
            var old = catNamesByLevel;
            catNamesByLevel = new string[n];
            int copy = old != null ? Mathf.Min(old.Length, n) : 0;
            for (int i = 0; i < copy; i++)
                catNamesByLevel[i] = string.IsNullOrEmpty(old[i]) ? DefaultName(i) : old[i];
            for (int i = copy; i < n; i++)
                catNamesByLevel[i] = DefaultName(i);
        }
        else
        {
            // 비어있는 항목만 기본 이름으로 채움
            for (int i = 0; i < catNamesByLevel.Length; i++)
                if (string.IsNullOrEmpty(catNamesByLevel[i]))
                    catNamesByLevel[i] = DefaultName(i);
        }
    }

    void ResizeAndCopyNames()
    {
        if (catPrefabsByLevel == null) return;
        int n = catPrefabsByLevel.Length;
        var arr = new string[n];
        if (catNamesByLevel != null)
        {
            for (int i = 0; i < Mathf.Min(catNamesByLevel.Length, n); i++)
                arr[i] = string.IsNullOrEmpty(catNamesByLevel[i]) ? DefaultName(i) : catNamesByLevel[i];
        }
        for (int i = 0; i < n; i++)
            if (string.IsNullOrEmpty(arr[i])) arr[i] = DefaultName(i);
        catNamesByLevel = arr;
    }

    string DefaultName(int level) => $"Cat Lv.{level}";

    /*public int GetCatCounts()
    {
        var cats = FindObjectsOfType<CatDragHandler>(includeInactive: false);
        var count = 0;
        if (cats.Length == 0) return 0;
        foreach (var cat in cats)
        {
            if (!cat || !cat.gameObject.activeInHierarchy) continue;
            count++;
        }

        return count;
    }*/

    // 고양이 보유 수 증가/감소 → 방송
    public void AddCatHaveCount(int delta)
    {
        catHaveCount = Mathf.Clamp(catHaveCount + delta, 0, catMaxCount);
        TotalSummonedCats++;
        BroadcastCatCount();
    }

    public void AddCatMaxCount(int delta)
    {
        catMaxCount = Mathf.Max(1, catMaxCount + delta);
        if (catHaveCount > catMaxCount)
            catHaveCount = catMaxCount;
        BroadcastCatCount();
    }

    public void MarkDiscovered(int level)
    {
        if (!IsValidLevel(level)) return;

        if (!IsCatDiscovered[level])
        {
            IsCatDiscovered[level] = true;
            Debug.Log($"[CatManager] 고양이 발견! Lv.{level} - {GetCatName(level)}");

            OnCatDiscovered?.Invoke(level); // UI 등에서 구독 가능
        }
    }

    public void BroadcastCatCount()
    {
        OnCatCountChanged?.Invoke(catHaveCount, catMaxCount);
    }

    /// <summary>
    /// 발견된 고양이 중 가장 높은 등급(인덱스) 반환.
    /// 발견된 고양이가 없으면 0 반환.
    /// </summary>
    /// <returns></returns>
    public int GetHighestDiscoveredLevel()
    {
        if (IsCatDiscovered == null || IsCatDiscovered.Length == 0)
            return 0;

        for (int i = IsCatDiscovered.Length - 1; i >= 0; i--)
        {
            if (IsCatDiscovered[i]) return i;
        }
        return 0;
    }
}
