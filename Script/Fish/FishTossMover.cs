using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class FishTossMover : MonoBehaviour
{
    RectTransform rt;
    Canvas rootCanvas;

    RectTransform boundArea;     // 경계 패널(없으면 캔버스 전체)
    Vector2 velocity;            // px/sec

    [Header("Physics")]
    [Range(0f, 1f)] private float bounciness = 1f;
    [Range(0f, 10f)] private float linearDrag = 1f;
    [Range(0f, 1f)] private float wallFriction = 0.15f;
    private readonly float minSpeed = 10f;

    [Header("Visual")]
    public bool rotateWithVelocity = true;
    public bool useUnscaledTime = true;   // 옵션으로 TimeScale 제어

    [Header("Spawn Settings")]
    public GameObject explodeFxPrefab;
    public RectTransform spawnParent;
    public Vector2 explodeDelayRange = new Vector2(1f, 2f);
    public float catInitialUpSpeed = 350f;
    private int spawnLevel = 0; // 소환할 고양이 레벨

    Coroutine explodeCo; // 코루틴 핸들
    bool scheduled;

    // ───────── Init ─────────
    public void SetSpawnContext(
        RectTransform spawnParent,
        GameObject explodeFxPrefab,
        Vector2 explodeDelayRange,
        int catLevel)
    {
        this.spawnParent = spawnParent;
        this.explodeFxPrefab = explodeFxPrefab;
        this.explodeDelayRange = explodeDelayRange;
        this.spawnLevel = catLevel;
    }

    public void Init(RectTransform playArea, float speed, float bounce, Vector2? dir = null)
    {
        if (!rt) rt = GetComponent<RectTransform>();
        boundArea = playArea;
        bounciness = Mathf.Clamp(bounce, 0f, 1f);
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>(true);

        Vector2 d = (dir.HasValue ? dir.Value : Random.insideUnitCircle).normalized;
        if (d == Vector2.zero) d = Vector2.right;
        velocity = d * speed;

        if (!spawnParent) spawnParent = boundArea ? boundArea : rt.parent as RectTransform;

        // 폭발/소환 예약
        if (!scheduled)
        {
            scheduled = true;
            float delay = Random.Range(explodeDelayRange.x, explodeDelayRange.y);
            explodeCo = StartCoroutine(ExplodeAfterDelay(delay));
        }
    }

    IEnumerator ExplodeAfterDelay(float t)
    {
        if (useUnscaledTime) yield return new WaitForSecondsRealtime(t);
        else yield return new WaitForSeconds(t);
        ExplodeAndSpawnCat();
    }

    void OnDisable()
    {
        if (explodeCo != null) StopCoroutine(explodeCo);
    }

    void Awake()
    {
        if (!rt) rt = GetComponent<RectTransform>();
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>(true);
        if (!spawnParent) spawnParent = rt.parent as RectTransform;
    }

    // ───────── Update ─────────
    readonly Vector3[] _selfCorners = new Vector3[4];
    readonly Vector3[] _areaCorners = new Vector3[4];

    void Update()
    {
        if (!rt) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // 1) 공기저항
        float dragK = Mathf.Clamp01(linearDrag * dt);
        velocity *= (1f - dragK);

        if (velocity.sqrMagnitude < minSpeed * minSpeed)
            velocity = Vector2.zero;

        // 2) 이동
        transform.position += (Vector3)(velocity * dt);

        // 3) 경계 체크
        Rect worldBounds = GetWorldBounds(boundArea);
        Rect myWorldRect = GetWorldRect(rt, _selfCorners);
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

        // 4) 회전
        if (rotateWithVelocity && velocity.sqrMagnitude > 1f)
        {
            float ang = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            rt.rotation = Quaternion.Euler(0, 0, ang);
        }
    }

    // ───────── 폭발 + 소환 ─────────
    void ExplodeAndSpawnCat()
    {
        // FX
        if (explodeFxPrefab)
        {
            var fx = Instantiate(explodeFxPrefab, spawnParent ? spawnParent : rt.parent);
            var fxRt = fx.GetComponent<RectTransform>();
            if (fxRt) { fxRt.position = rt.position; fxRt.localScale = Vector3.one; }
            else fx.transform.position = transform.position;
            Destroy(fx, 0.8f);
        }

        // CatManager
        var cm = CatManager.Instance;
        if (cm && cm.TryGetPrefab(spawnLevel, out var prefab))
        {
            cm.MarkDiscovered(spawnLevel);

            var cat = Instantiate(prefab, spawnParent ? spawnParent : rt.parent);
            var catRt = cat.GetComponent<RectTransform>();
            if (catRt)
            {
                catRt.position = rt.position;
                catRt.localScale = Vector3.one;
            }
            else cat.transform.position = transform.position;

            AudioManager.Instance.PlaySFX(SfxKey.MeowRandom);

            var mover = cat.GetComponent<CatMover>();
            if (mover && catRt)
            {
                catRt.anchoredPosition += new Vector2(0f, 6f);
            }
        }

        Destroy(gameObject);
    }

    // ───────── Utils ─────────
    Rect GetWorldBounds(RectTransform area)
    {
        if (area) return GetWorldRect(area, _areaCorners);
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>(true);
        var cam = rootCanvas ? rootCanvas.worldCamera : Camera.main;
        Rect pr = rootCanvas ? rootCanvas.pixelRect : new Rect(0, 0, Screen.width, Screen.height);
        Vector3 min = cam ? cam.ScreenToWorldPoint(new Vector3(pr.xMin, pr.yMin, 0)) : new Vector3(pr.xMin, pr.yMin, 0);
        Vector3 max = cam ? cam.ScreenToWorldPoint(new Vector3(pr.xMax, pr.yMax, 0)) : new Vector3(pr.xMax, pr.yMax, 0);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    static Rect GetWorldRect(RectTransform target, Vector3[] corners)
    {
        target.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }
}