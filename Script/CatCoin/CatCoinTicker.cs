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

        var cats = FindObjectsOfType<CatDragHandler>(includeInactive: false);

        int count = CatManager.Instance.GetCatCounts();
        if (count == 0)
        {
            lastCatCount = 0;
            lastAddedCoins = 0;
            return;
        }

        long bonusPerCat = 0;
        long totalCoins = 0;

        foreach (var cat in cats)
        {
            if (!cat || !cat.gameObject.activeInHierarchy) continue;

            int lvl = CatManager.Instance.ClampLevel(cat.level);
            float perCycle = CatManager.Instance.GetCoinPerSecond(lvl);
            long baseGainThisCycle = Mathf.Max(0, Mathf.FloorToInt(perCycle));

            long gain = baseGainThisCycle + bonusPerCat;
            if (gain < 0) gain = 0;

            int multiple = CatCoinManager.Instance.MultipleGetCatCoin();
            gain *= multiple;
            if(GameManager.Instance.RemainingTime[0] > 0) gain *= 3;

            totalCoins += gain;

            if (coinPrefab && coinTarget && gain > 0)
                SpawnVisual(cat.GetComponent<RectTransform>(), multiple);
        }

        // 소비재 보너스(%)
        totalCoins = (totalCoins * CatSuppliesManager.Instance.GetSuppliesBonus()) / 100;

        lastCatCount = count;
        CatCoinManager.Instance.AddCatCoin(totalCoins);
        lastAddedCoins = totalCoins;

        if (coinCounterAnimator != null)
            coinCounterAnimator.AnimateTo(CatCoinManager.Instance.HaveCatCoinCount);
    }

    void SpawnVisual(RectTransform catRt, int multiple = 1)
    {
        if (!catRt) return;

        var parent = spawnParent ? spawnParent : catRt.root as RectTransform;
        Vector3 start = catRt.position + (Vector3)(Random.insideUnitCircle * spawnJitter);

        // 1) 일반 코인: 항상 생성 (배수 스프라이트 반영)
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
                    int idx = Mathf.Clamp(multiple - 1, 0, sprites.Length - 1);
                    img.sprite = sprites[idx];
                }
            });

        // 2) 골드 코인: 확률 당첨 시 → 비주얼 + 코인지급 콜백
        bool goldHit = (GoldCoinPrefab && GoldCoinTarget && CatCoinManager.Instance.GetCatGoldCoinForCoinTick());
        if (goldHit)
        {
            SpawnOne(
                prefab: GoldCoinPrefab,
                parent: parent,
                startWorldPos: start,
                target: GoldCoinTarget,
                travelTime: coinTravelTime);

            if (GameManager.Instance.RemainingTime[0] > 0)
                CatCoinManager.Instance.AddCatCoin(3);
            else
                CatCoinManager.Instance.AddCatGoldCoin(1);
        }

        // 3) 고양이 알: 확률 당첨 시 → 비주얼 + 코인지급/아이템 지급 콜백
        bool eggHit = (CatEggPrefab && CatEggTarget && CatCoinManager.Instance.GetCatEggForCoinTick());
        if (eggHit)
        {
            SpawnOne(
                prefab: CatEggPrefab,
                parent: parent,
                startWorldPos: start,
                target: CatEggTarget,
                travelTime: coinTravelTime);


            CatSummonsManager.Instance.AddTicket(CatSummonsManager.SummonTier.Common);
        }

        Debug.Log($"코인 시각효과 스폰: 배수 {multiple}, 골드Hit={goldHit}, 알Hit={eggHit}");
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