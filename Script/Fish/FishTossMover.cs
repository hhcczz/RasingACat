using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class FishTossMover : MonoBehaviour
{
    RectTransform rt;
    Canvas rootCanvas;

    RectTransform boundArea;     // 경계 패널(없으면 캔버스 전체)
    Vector2 velocity;            // px/sec

    [Header("Physics")]
    [Range(0f, 1.2f)] public float bounciness = 0.9f;
    [Range(0f, 10f)] public float linearDrag = 1.2f;
    [Range(0f, 1f)] public float wallFriction = 0.15f;
    public float minSpeed = 15f;

    [Header("Visual")]
    public bool rotateWithVelocity = true;

    [Header("Spawn Settings")]
    [Tooltip("펑! 이펙트 프리팹(UI용, RectTransform/Animator 권장)")]
    public GameObject explodeFxPrefab;
    [Tooltip("고양이/이펙트를 붙일 부모(보통 PlayArea). 비워두면 현재 부모 사용")]
    public RectTransform spawnParent;
    [Tooltip("펑까지의 지연 시간(초) 랜덤 범위)")]
    public Vector2 explodeDelayRange = new Vector2(1f, 2f);
    [Tooltip("펑 후에 고양이가 나올 때 살짝 위로 튕기는 속도(px/s)")]
    public float catInitialUpSpeed = 350f;
    [Tooltip("소환할 고양이 레벨")]
    private int spawnLevel = 0; // 소환할 고양이 레벨

    bool scheduled; // 한 번만 예약

    public void SetSpawnContext(
        RectTransform spawnParent,
        GameObject explodeFxPrefab,
        Vector2 explodeDelayRange,
        int catLevel)
    {
        this.spawnParent = spawnParent;         // 고양이/FX가 붙을 부모 = PlayArea (씬 인스턴스)
        this.explodeFxPrefab = explodeFxPrefab;
        this.explodeDelayRange = explodeDelayRange;
        this.spawnLevel = catLevel;   // 내부에서 기록해두기
    }

    // ───────── Init ─────────
    public void Init(RectTransform playArea, float speed, float bounce, Vector2? dir = null)
    {
        if (!rt) rt = GetComponent<RectTransform>();
        boundArea = playArea;
        bounciness = Mathf.Clamp(bounce, 0f, 1.2f);
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>(true);

        Vector2 d = (dir.HasValue ? dir.Value : Random.insideUnitCircle).normalized;
        if (d == Vector2.zero) d = Vector2.right;
        velocity = d * speed;

        // 부모 기본값
        if (!spawnParent) spawnParent = boundArea ? boundArea : rt.parent as RectTransform;

        // 폭발/소환 예약(중복 방지)
        if (!scheduled)
        {
            scheduled = true;
            float delay = Random.Range(explodeDelayRange.x, explodeDelayRange.y);
            Invoke(nameof(ExplodeAndSpawnCat), delay);
        }
    }

    void Awake()
    {
        if (!rt) rt = GetComponent<RectTransform>();
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>(true);
        if (!spawnParent) spawnParent = rt.parent as RectTransform;
    }

    void Update()
    {
        if (!rt) return;

        float dt = Time.unscaledDeltaTime;

        // 1) 공기저항(선형 감속)
        float dragK = Mathf.Clamp01(linearDrag * dt);
        velocity *= (1f - dragK);

        if (velocity.sqrMagnitude < minSpeed * minSpeed)
            velocity = Vector2.zero;

        // 2) 이동(월드 좌표)
        transform.position += (Vector3)(velocity * dt);

        // 3) 경계 체크 + 반사 + 벽 마찰
        Rect worldBounds = GetWorldBounds(boundArea);
        Rect myWorldRect = GetWorldRect(rt);
        Vector3 pos = transform.position;

        // 좌우
        if (myWorldRect.xMin <= worldBounds.xMin && velocity.x < 0f)
        {
            float pen = worldBounds.xMin - myWorldRect.xMin;
            pos.x += pen;
            velocity.x = -velocity.x * bounciness;
            velocity.y *= (1f - wallFriction);
        }
        else if (myWorldRect.xMax >= worldBounds.xMax && velocity.x > 0f)
        {
            float pen = myWorldRect.xMax - worldBounds.xMax;
            pos.x -= pen;
            velocity.x = -velocity.x * bounciness;
            velocity.y *= (1f - wallFriction);
        }

        // 상하
        if (myWorldRect.yMin <= worldBounds.yMin && velocity.y < 0f)
        {
            float pen = worldBounds.yMin - myWorldRect.yMin;
            pos.y += pen;
            velocity.y = -velocity.y * bounciness;
            velocity.x *= (1f - wallFriction);
        }
        else if (myWorldRect.yMax >= worldBounds.yMax && velocity.y > 0f)
        {
            float pen = myWorldRect.yMax - worldBounds.yMax;
            pos.y -= pen;
            velocity.y = -velocity.y * bounciness;
            velocity.x *= (1f - wallFriction);
        }

        transform.position = pos;

        // 4) 비주얼 회전
        if (rotateWithVelocity && velocity != Vector2.zero)
        {
            float ang = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            rt.rotation = Quaternion.Euler(0, 0, ang);
        }
    }

    // ───────── 폭발 + 고양이 소환 ─────────
    void ExplodeAndSpawnCat()
    {
        // 1) FX
        if (explodeFxPrefab)
        {
            var fx = Instantiate(explodeFxPrefab, spawnParent ? spawnParent : rt.parent);
            var fxRt = fx.GetComponent<RectTransform>();
            if (fxRt) { fxRt.position = rt.position; fxRt.localScale = Vector3.one; }
            else fx.transform.position = transform.position;
            Destroy(fx, 0.8f);
        }

        // 2) CatManager 통해서 고양이 프리팹 가져오기
        var cm = CatManager.Instance;
        if (cm && cm.TryGetPrefab(spawnLevel, out var prefab))
        {
            cm.MarkDiscovered(spawnLevel); // 발견 처리

            var cat = Instantiate(prefab, spawnParent ? spawnParent : rt.parent);
            var catRt = cat.GetComponent<RectTransform>();
            if (catRt)
            {
                catRt.position = rt.position;
                catRt.localScale = Vector3.one;
            }
            else
            {
                cat.transform.position = transform.position;
            }

            // 소리
            AudioManager.Instance.PlaySFX(SfxKey.MeowRandom);

            // CatMover 연출
            var mover = cat.GetComponent<CatMover>();
            if (mover && catRt)
            {
                catRt.anchoredPosition += new Vector2(0f, 6f);
            }
        }

        // 3) 자기 자신 제거
        Destroy(gameObject);
    }

    // ───────── Utils ─────────
    Rect GetWorldBounds(RectTransform area)
    {
        if (area) return GetWorldRect(area);
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>(true);
        var cam = rootCanvas ? rootCanvas.worldCamera : Camera.main;
        Rect pr = rootCanvas ? rootCanvas.pixelRect : new Rect(0, 0, Screen.width, Screen.height);
        Vector3 min = cam ? cam.ScreenToWorldPoint(new Vector3(pr.xMin, pr.yMin, 0)) : new Vector3(pr.xMin, pr.yMin, 0);
        Vector3 max = cam ? cam.ScreenToWorldPoint(new Vector3(pr.xMax, pr.yMax, 0)) : new Vector3(pr.xMax, pr.yMax, 0);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    static Rect GetWorldRect(RectTransform target)
    {
        Vector3[] c = new Vector3[4];
        target.GetWorldCorners(c);
        return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
    }
}
