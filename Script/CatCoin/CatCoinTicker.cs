using UnityEngine;
using UnityEngine.UI;

public class CatCoinTicker : MonoBehaviour
{
    [Header("Tick")]
    public float tickInterval = 2f;

    [Header("Visual Coin")]
    public GameObject coinPrefab;              // Image + UICoinFlyer
    public RectTransform coinTarget;           // HUD 코인 아이콘 RectTransform
    public RectTransform spawnParent;          // 비주얼 부모(없으면 최상위 Canvas)
    public float coinTravelTime = 0.75f;       // 비행 시간
    public float spawnJitter = 6f;             // 발밑 산란(px)

    [Header("Visual Gold Coin")]
    public GameObject GoldCoinPrefab;
    public RectTransform GoldCoinTarget;

    [Header("Visual Cat Egg")]
    public GameObject CatEggPrefab;
    public RectTransform CatEggTarget;

    [Header("Debug / Stats")]
    public int lastCatCount = 0;
    public float lastAddedCoins = 0f;

    [Header("UI Counter")]
    public UICoinCountAnimator coinCounterAnimator;

    float timer;

    [HideInInspector] public RectTransform cachedRectTransform;
    void Awake() { cachedRectTransform = GetComponent<RectTransform>(); }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < tickInterval) return;
        timer -= tickInterval;
        DoTick();
    }

    void DoTick()
    {
        if (CatManager.Instance == null || CatCoinManager.Instance == null) return;

        //  캐시된 리스트 사용
        var cats = CatManager.Instance.ActiveCatsRO;
        int count = cats?.Count ?? 0;
        if (count == 0)
        {
            lastCatCount = 0;
            lastAddedCoins = 0;
            return;
        }

        // 반복 호출 캐시
        int multiple = CatCoinManager.Instance.MultipleGetCatCoin();
        bool inBoost = (GameManager.Instance.RemainingTime != null
                        && GameManager.Instance.RemainingTime.Length > 0
                        && GameManager.Instance.RemainingTime[0] > 0);

        long totalCoins = 0;

        for (int i = 0; i < count; i++)
        {
            var cat = cats[i];
            if (!cat || !cat.gameObject.activeInHierarchy) continue;

            int lvl = CatManager.Instance.ClampLevel(cat.level);
            long baseGainThisCycle = Mathf.Max(0, Mathf.FloorToInt(CatManager.Instance.GetCoinPerSecond(lvl)));

            long gain = baseGainThisCycle * multiple;
            if (inBoost) gain *= 3;

            if (gain <= 0) continue;

            totalCoins += gain;

            // 캐시된 RectTransform 사용
            var rt = cat.cachedRectTransform != null ? cat.cachedRectTransform
                                                     : cat.GetComponent<RectTransform>();

            if (coinPrefab && coinTarget)
                SpawnVisual(rt, multiple, inBoost); // inBoost 전달(선택)
        }

        if(GameManager.Instance.OneDayBuffTime > 0) totalCoins *= GameManager.Instance.OneDayBuff_PlusMultipleCoin;

        // 보너스도 루프 밖에서 한 번만
        int suppliesBonus = CatSuppliesManager.Instance.GetSuppliesBonus(); // 100=기본, 130=+30%라면 그대로 OK
        totalCoins = (totalCoins * suppliesBonus) / 100;

        lastCatCount = count;
        CatCoinManager.Instance.AddCatCoin(totalCoins);
        lastAddedCoins = totalCoins;

        coinCounterAnimator?.AnimateTo(CatCoinManager.Instance.HaveCatCoinCount);
    }

    void SpawnVisual(RectTransform catRt, int multiple = 1, bool inBoost = false)
    {
        if (!catRt) return;

        var parent = spawnParent ? spawnParent : catRt.root as RectTransform;
        Vector3 start = catRt.position + (Vector3)(Random.insideUnitCircle * spawnJitter);

        // 1) 일반 코인
        SpawnOne(
            prefab: coinPrefab,
            parent: parent,
            startWorldPos: start,
            target: coinTarget,
            travelTime: coinTravelTime,
            onSpawn: go =>
            {
                var img = go.GetComponent<Image>();
                var sprites = CatCoinManager.Instance.CatCoinSprite;
                if (img != null && sprites != null && sprites.Length > 0)
                {
            // multiple: 1~5 → idx: 0~4
            int idx = Mathf.Clamp(multiple - 1, 0, sprites.Length - 1);
                    img.sprite = sprites[idx];
                }
            });

        // 2) 골드 코인
        if (GoldCoinPrefab && GoldCoinTarget && CatCoinManager.Instance.GetCatGoldCoinForCoinTick())
        {
            SpawnOne(GoldCoinPrefab, parent, start, GoldCoinTarget, coinTravelTime, null);
            if (inBoost)
            {
                if (GameManager.Instance.OneDayBuffTime > 0) CatCoinManager.Instance.AddCatGoldCoin(9);
                else CatCoinManager.Instance.AddCatGoldCoin(3);
            }
            else
            {
                if (GameManager.Instance.OneDayBuffTime > 0) CatCoinManager.Instance.AddCatGoldCoin(3);
                else CatCoinManager.Instance.AddCatGoldCoin(1);
            }
        }

        // 3) 고양이 알
        if (CatEggPrefab && CatEggTarget && CatCoinManager.Instance.GetCatEggForCoinTick())
        {
            SpawnOne(CatEggPrefab, parent, start, CatEggTarget, coinTravelTime, null);
            CatSummonsManager.Instance.AddTicket(CatSummonsManager.SummonTier.Common);
        }

        // 로그는 개발시에만
        // #if UNITY_EDITOR
        // Debug.Log($"코인 시각효과 스폰: 배수 {multiple}, inBoost={inBoost}");
        // #endif
    }

    void SpawnOne(GameObject prefab, RectTransform parent, Vector3 startWorldPos,
                  RectTransform target, float travelTime, System.Action<GameObject> onSpawn = null)
    {
        if (!prefab || !parent) return;

        var go = Instantiate(prefab, parent);
        onSpawn?.Invoke(go);

        var rt = go.GetComponent<RectTransform>();
        var flyer = go.GetComponent<UICoinFlyer>();

        if (flyer && target)
        {
            flyer.Init(startWorldPos, target, travelTime);
        }
        else
        {
            if (rt) rt.position = startWorldPos;
            Destroy(go, travelTime);
        }
    }

}