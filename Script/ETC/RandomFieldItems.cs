using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

/// <summary>
/// 부모 RectTransform 영역(+여유 범위) 안에 아이템을 주기적으로 스폰.
/// - maxSpawnItems 초과 시 스폰 X
/// - weights(가중치) 기반으로 prefabToSpawn 중 하나를 선택
/// - 각 아이템에는 스폰된 인덱스를 기록 (FieldItem.index)
/// </summary>
public class RandomFieldItems : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("생성할 프리팹들 (UI라면 RectTransform 포함 권장)")]
    public GameObject[] prefabToSpawn;

    [Tooltip("각 프리팹의 가중치(확률). 합계가 100일 필요는 없음. 배열 길이는 prefabToSpawn과 동일해야 합니다.")]
    [SerializeField] private float[] weights;

    [Tooltip("부모 RectTransform 가로/세로에 더해줄 여유 범위(px)")]
    public float extraRange = 100f;

    [Tooltip("동시에 존재 가능한 최대 개수")]
    public int maxSpawnItems = 3;

    [Header("Targets / Prefabs")]
    public RectTransform FishArriveCenter;
    public RectTransform CatCoinArriveCenter;
    public RectTransform CatGoldCoinArriveCenter;
    public RectTransform CatEggBucketArriveCenter;

    [Header("Explode FX (펑 전용)")]
    public GameObject explodeFxPrefab;      // 펑 이펙트 프리팹(UI용)
    public float explodeFxDuration = 0.6f; // 이펙트 수명

    [Header("날아가는 객체 이미지")]
    public TossFish _tossfish;
    public GameObject CatCoin;
    public GameObject CatGoldCoin;
    public GameObject CatEggCommon;
    public GameObject CatEggRare;
    public GameObject CatEggUnique;

    private RectTransform rectTransform;
    private readonly List<GameObject> alive = new List<GameObject>();

    [Header("Control")]
    [SerializeField] private bool useSelfTimer = false; // 내부 스폰 루프 사용할지 여부

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        if (prefabToSpawn == null || prefabToSpawn.Length == 0)
        {
            Debug.LogWarning("[RandomFieldItems] prefabToSpawn 이 비어 있습니다!");
            enabled = false;
            return;
        }

        EnsureWeightsLength();

        // 내부 타이머로 스폰할지 선택
        if (useSelfTimer)
            StartCoroutine(SpawnLoop());
    }

    void EnsureWeightsLength()
    {
        if (weights == null || weights.Length != prefabToSpawn.Length)
        {
            var def = new float[prefabToSpawn.Length];
            // 기본 확률 테이블 (필요에 맞게 조정)
            // 예: 물고기바구니, 코인, 골드코인, 알(커먼/레어/유니크)
            float[] fallback = new float[] { 49f, 40f, 0.1f, 10.6f, 0.29f, 0.01f };

            for (int i = 0; i < def.Length; i++)
                def[i] = i < fallback.Length ? fallback[i] : 1f;

            weights = def;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float baseSec = Mathf.Max(0.1f,
                CatGymDrugManager.Instance
                    ? CatGymDrugManager.Instance.GetCurrentAbility(CatGymDrugManager.DrugType.Chur)
                    : 60f);

            // 원데이 버프 반영
            if (GameManager.Instance && GameManager.Instance.OneDayBuffTime > 0)
                baseSec -= MathF.Max(0.1f, GameManager.Instance.OneDayBuff_DecreaseRandomItemTime);

            yield return new WaitForSeconds(baseSec);

            TrySpawn();
        }
    }

    // 외부(슬라이더 등)에서 호출할 때 사용할 안전 래퍼
    public void TriggerSpawn()
    {
        TrySpawn();
    }

    // 필요하면 public으로 바꿔도 OK
    private void TrySpawn()
    {
        CleanupAlive();
        if (alive.Count >= maxSpawnItems) return;
        if (!rectTransform) return;

        float halfWidth = rectTransform.rect.width * 0.5f + (extraRange * 0.5f);
        float halfHeight = rectTransform.rect.height * 0.5f + extraRange;

        Vector3 localPos;
        int safety = 100;
        do
        {
            float randX = UnityEngine.Random.Range(-halfWidth, halfWidth);
            float randY = UnityEngine.Random.Range(-halfHeight, halfHeight);
            localPos = new Vector3(randX, randY, 0f);
            safety--;
        }
        while (Mathf.Abs(localPos.x) <= 200f && Mathf.Abs(localPos.y) <= 200f && safety > 0);

        Vector3 worldPos = rectTransform.TransformPoint(localPos);

        int pickedIndex = PickIndexByWeights(weights);
        if (pickedIndex < 0 || pickedIndex >= prefabToSpawn.Length) pickedIndex = 0;

        GameObject prefab = prefabToSpawn[pickedIndex];
        if (!prefab) return;

        GameObject go = Instantiate(prefab, worldPos, Quaternion.identity, rectTransform);

        RectTransform childRT = go.GetComponent<RectTransform>();
        if (childRT)
        {
            childRT.anchoredPosition3D = localPos;
            childRT.localScale = Vector3.one;
            childRT.localRotation = Quaternion.identity;
        }

        FieldItem carrier = go.GetComponent<FieldItem>();
        if (!carrier) carrier = go.AddComponent<FieldItem>();
        carrier.index = pickedIndex;
        carrier.spawner = this;
        carrier.spawnParent = go.transform as RectTransform;

        alive.Add(go);
    }

private int PickIndexByWeights(float[] w)
    {
        if (w == null || w.Length == 0) return 0;

        float total = 0f;
        for (int i = 0; i < w.Length; i++)
            total += Mathf.Max(0f, w[i]);

        if (total <= 0f) return 0;

        float r = UnityEngine.Random.Range(0f, total);
        float acc = 0f;
        for (int i = 0; i < w.Length; i++)
        {
            acc += Mathf.Max(0f, w[i]);
            if (r <= acc) return i;
        }
        return w.Length - 1;
    }

    private void CleanupAlive()
    {
        for (int i = alive.Count - 1; i >= 0; i--)
            if (alive[i] == null) alive.RemoveAt(i);
    }
}

/// <summary>
/// 스폰된 아이템에 붙는 간단한 인덱스 보관 + 클릭 상호작용 예시
/// </summary>
public class FieldItem : MonoBehaviour, IPointerClickHandler
{
    public int index;

    [Header("References")]
    public RandomFieldItems spawner;
    public RectTransform spawnParent;

    [Header("Settings")]
    public int FlyCount = 5;
    public float fishFlyTime = 0.6f;
    public float spawnJitter = 10f;
    public float spawnDelay = 0.1f;


    bool collected;

    private enum ItemTypes { FishBucket, CatCoin, CatGoldCoin, EggBucketCommon, EggBucketRare, EggBucketUnique };

    void Awake()
    {
        if (spawnParent == null)
            spawnParent = transform as RectTransform;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (collected) return;
        collected = true;

        

        // 본체 이미지만 투명 처리
        var img = GetComponent<Image>();
        if (img != null) { var c = img.color; c.a = 0f; img.color = c; }

        // 펑 이펙트만 나오고 끝 (보상 지급 X)
        if (ShouldExplode())
        {
            StartCoroutine(OnlyExplodeAndVanish());
            return;
        }
        AudioManager.Instance.PlayButtonClick();
        // 여기서부터는 보상 지급 루틴
        ItemTypes type = index switch
        {
            0 => ItemTypes.FishBucket,
            1 => ItemTypes.CatCoin,
            2 => ItemTypes.CatGoldCoin,
            3 => ItemTypes.EggBucketCommon,
            4 => ItemTypes.EggBucketRare,
            5 => ItemTypes.EggBucketUnique,
            _ => ItemTypes.FishBucket
        };

        StartCoroutine(PlayCollectSequence(type));
    }

    // 버프 중이면 100%, 아니면 50% 확률로 펑
    bool ShouldExplode()
    {
        if (GameManager.Instance && GameManager.Instance.OneDayBuffTime > 0) return false;
        return UnityEngine.Random.value < 0.5f;
    }

    IEnumerator OnlyExplodeAndVanish()
    {
        // 펑 이펙트 재생 (보상 X)
        if (spawner.explodeFxPrefab)
        {
            AudioManager.Instance.PlayBreakSound();
            var parent = spawnParent ? (Transform)spawnParent.parent : transform.parent;
            var fx = Instantiate(spawner.explodeFxPrefab, parent);
            var fxRt = fx.GetComponent<RectTransform>();
            if (fxRt)
            {
                fxRt.position = transform.position;
                fxRt.localScale = Vector3.one;
                fxRt.localRotation = Quaternion.identity;
            }
            else
            {
                fx.transform.position = transform.position;
            }
            Destroy(fx, spawner.explodeFxDuration);
        }

        // 약간의 딜레이 후 자기 자신 제거
        yield return new WaitForSeconds(spawner.explodeFxDuration * 0.9f);
        Destroy(gameObject);
    }


    IEnumerator PlayCollectSequence(ItemTypes type)
    {
        if (spawner == null || spawner._tossfish == null || spawner.FishArriveCenter == null)
        {
            Debug.LogWarning("[FieldItem] Missing refs (spawner/_tossfish/ArriveCenter).");
            Destroy(gameObject); // 안전상 제거
            yield break;
        }

        // 객체 날리기 (스태거)
        if(type == ItemTypes.FishBucket)
        {
            int level = CatGymUPGradeManager.Instance.CatGymLevel[5];
            GameObject Prefab = spawner._tossfish.fishUiPrefab[level];
            if (Prefab == null)
            {
                Debug.LogWarning("[FieldItem] fishPrefab is null for level " + level);
                Destroy(gameObject);
                yield break;
            }
            yield return StartCoroutine(SpawnAndFlyFishes(Prefab, spawner.FishArriveCenter, FlyCount, fishFlyTime));
        }
        else if(type == ItemTypes.CatCoin)
        {
            GameObject Prefab = spawner.CatCoin;
            if (Prefab == null)
            {
                Destroy(gameObject);
                yield break;
            }
            yield return StartCoroutine(SpawnAndFlyFishes(Prefab, spawner.CatCoinArriveCenter, FlyCount, fishFlyTime));
        }
        else if (type == ItemTypes.CatGoldCoin)
        {
            GameObject Prefab = spawner.CatGoldCoin;
            if (Prefab == null)
            {
                Destroy(gameObject);
                yield break;
            }
            yield return StartCoroutine(SpawnAndFlyFishes(Prefab, spawner.CatGoldCoinArriveCenter, FlyCount, fishFlyTime));
        }

        else if (type == ItemTypes.EggBucketCommon)
        {
            GameObject Prefab = spawner.CatEggCommon;
            if (Prefab == null)
            {
                Destroy(gameObject);
                yield break;
            }
            yield return StartCoroutine(SpawnAndFlyFishes(Prefab, spawner.CatEggBucketArriveCenter, FlyCount, fishFlyTime));
        }

        else if (type == ItemTypes.EggBucketRare)
        {
            GameObject Prefab = spawner.CatEggRare;
            if (Prefab == null)
            {
                Destroy(gameObject);
                yield break;
            }
            yield return StartCoroutine(SpawnAndFlyFishes(Prefab, spawner.CatEggBucketArriveCenter, FlyCount, fishFlyTime));
        }


        else if (type == ItemTypes.EggBucketUnique)
        {
            GameObject Prefab = spawner.CatEggUnique;
            if (Prefab == null)
            {
                Destroy(gameObject);
                yield break;
            }
            yield return StartCoroutine(SpawnAndFlyFishes(Prefab, spawner.CatEggBucketArriveCenter, FlyCount, fishFlyTime));
        }



        // 부모 제거
        Destroy(gameObject);
    }

    IEnumerator SpawnAndFlyFishes(GameObject fishPrefab, RectTransform target, int count, float duration)
    {
        if (spawnParent == null) yield break;

        int finished = 0; // 완료 카운터

        for (int i = 0; i < count; i++)
        {
            // 물고기 생성
            var go = Instantiate(fishPrefab, spawnParent);
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();

            // 시작 위치(로컬) + 지터
            Vector2 startLocal = new Vector2(
                UnityEngine.Random.Range(-spawnJitter, spawnJitter),
                UnityEngine.Random.Range(-spawnJitter, spawnJitter)
            );
            rt.anchoredPosition = startLocal;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            // 개별 비행 코루틴 시작
            StartCoroutine(FlyOne(rt, startLocal, target, duration, () => { finished++; }));

            // 0.1초 간격으로 다음 물고기 생성
            yield return new WaitForSeconds(spawnDelay);
        }

        // 모든 물고기 비행이 끝날 때까지 대기
        while (finished < count) yield return null;
    }

    IEnumerator FlyOne(RectTransform rt, Vector2 startLocal, RectTransform target, float duration, Action onDone)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - p, 3f); // easeOutCubic

            // 목표(타깃) 월드 → spawnParent 로컬 변환 (타깃이 움직이면 매 프레임 추적)
            Vector2 targetLocal = WorldToLocalInParent(spawnParent, target.position);

            // 고정된 시작점(startLocal) → 현재 목표점(targetLocal) 보간
            rt.anchoredPosition = Vector2.Lerp(startLocal, targetLocal, eased);

            yield return null;
        }

        if (rt != null) Destroy(rt.gameObject);
        onDone?.Invoke();
    }

    private Vector2 WorldToLocalInParent(RectTransform parent, Vector3 worldPos)
    {
        Camera cam = null;
        var canvas = parent.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, cam, out var localPoint);
        return localPoint;
    }
}