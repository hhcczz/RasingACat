using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class CatMover : MonoBehaviour
{
    public RectTransform playArea;
    public float moveSpeed = 220f;
    public float arriveDistance = 8f;
    public Vector2 pauseTimeRange = new Vector2(1.5f, 8.0f);
    public Animator animator;

    [Header("Flip")]
    public bool flipByDirection = true;
    public float flipDeadzone = 0.01f;

    [Header("UI Refs")]
    public RectTransform textName;   // 자식 "Text_Name"의 RectTransform (자동 탐색)

    RectTransform rt;
    Vector2 target;
    float pauseTimer;
    bool initialized;

    public bool isPaused = false;

    // 최적화용 캐시
    bool wasMoving = false;
    int lastDirection = 1; // 이전 이동 방향(+1 or -1)

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        if (!animator) animator = GetComponent<Animator>();
        if (!playArea) EnsurePlayArea();

        // Text_Name 자동 찾기
        if (!textName)
        {
            var t = transform.Find("Text_Name");
            if (t) textName = t as RectTransform;
        }
    }

    void OnEnable() { TryInit(); }
    void Start() { TryInit(); }

    void TryInit()
    {
        if (initialized) return;
        EnsurePlayArea();
        if (!playArea)
        {
            Debug.LogError("[CatMover] playArea가 비어 있습니다.", this);
            enabled = false;
            return;
        }
        PickNewTarget();
        initialized = true;

        // 시작 시에도 텍스트 방향 보정
        FixTextFlip();
    }

    void EnsurePlayArea()
    {
        if (playArea) return;
        var p = transform.parent as RectTransform;
        if (p) playArea = p;
        if (!playArea)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas)
            {
                var found = canvas.transform.Find("MainPanel/PlayArea");
                if (found) playArea = found as RectTransform;
            }
        }
    }

    public void SetPlayArea(RectTransform area)
    {
        playArea = area;
        if (!initialized && isActiveAndEnabled) TryInit();
    }

    void Update()
    {
        if (!initialized) return;
        if (isPaused) return;

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            SetMoving(false);
            return;
        }

        Vector2 pos = rt.anchoredPosition;
        Vector2 dir = target - pos;
        float dist = dir.magnitude;

        if (dist <= arriveDistance)
        {
            pauseTimer = Random.Range(pauseTimeRange.x, pauseTimeRange.y);
            PickNewTarget();
            SetMoving(false);
            return;
        }

        Vector2 step = dir.normalized * moveSpeed * Time.deltaTime;
        rt.anchoredPosition = pos + step;

        // ←→ 진행 방향에 따라 좌우 반전 (텍스트는 보정)
        if (flipByDirection && Mathf.Abs(step.x) > flipDeadzone)
        {
            int newDir = (step.x >= 0f ? 1 : -1);
            if (newDir != lastDirection) // 방향이 바뀌었을 때만 반전 처리
            {
                Vector3 s = rt.localScale;
                s.x = Mathf.Abs(s.x) * newDir;
                rt.localScale = s;

                FixTextFlip();
                lastDirection = newDir;
            }
        }

        SetMoving(true);
    }

    void PickNewTarget()
    {
        if (!playArea) return;
        Vector2 half = playArea.rect.size * 0.5f;
        target = new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
    }

    public void SetPause(bool pause) => isPaused = pause;

    // ───────── Animator 최적화 ─────────
    void SetMoving(bool moving)
    {
        if (animator && moving != wasMoving)
        {
            animator.SetBool("IsMoving", moving);
            wasMoving = moving;
        }
    }

    // ───────── 텍스트 반전 보정 ─────────
    void FixTextFlip()
    {
        if (!textName) return;

        var ts = textName.localScale;
        float parentSign = Mathf.Sign(rt.localScale.x);
        if (parentSign == 0f) parentSign = 1f; // 안전망

        ts.x = Mathf.Abs(ts.x) * parentSign;
        textName.localScale = ts;
    }
}