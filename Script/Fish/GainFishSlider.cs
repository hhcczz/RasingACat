using UnityEngine;
using UnityEngine.UI;

public class GainFishSlider : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider slider;

    [Header("게이지 설정")]
    [SerializeField, Min(0.1f)] private float durationSeconds = 5f;
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private float _t = 0f;                  // 0~durationSeconds
    private bool _pausedByFull = false;     // 인벤토리 꽉 차서 정지

    CatGymUI _gymUI; // 구독용 참조

    void Reset()
    {
        if (!slider) slider = GetComponent<Slider>();
    }

    void Awake()
    {
        if (!slider) slider = GetComponent<Slider>();
        if (slider)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = 0f;
        }
    }
    private void Start()
    {
        // 초기값 1회 반영
        UpdateFishTime();

        // CatGymUI 찾고 이벤트 구독
        _gymUI = FindObjectOfType<CatGymUI>(includeInactive: true);
        if (_gymUI != null)
            _gymUI.OnFishTimeChanged += HandleFishTimeChanged;
    }

    void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnFishChanged += HandleFishChanged;
    }

    void OnDisable()
    {
        if (_gymUI != null)
            _gymUI.OnFishTimeChanged -= HandleFishTimeChanged;

        if (GameManager.Instance != null)
            GameManager.Instance.OnFishChanged -= HandleFishChanged;
    }

    private void HandleFishTimeChanged(float newSeconds)
    {
        durationSeconds = Mathf.Max(0.1f, newSeconds);
        // 옵션) 진행도 리셋하고 싶으면 다음 두 줄 추가
        // _t = 0f;
        // if (slider) slider.value = 0f;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (!slider || gm == null) return;

        bool isFull = gm.HaveFishCount >= gm.MaxFishCount;

        // --- 풀 상태에선 무조건 1에서 멈춤 ---
        if (isFull)
        {
            _pausedByFull = true;     // 풀로 멈춘 상태
            _t = durationSeconds;     // 내부 타이머도 1에 고정
            slider.value = 1f;
            return;
        }

        // --- 방금 풀에서 벗어났다면(예: 소환으로 5/5→4/5), 자동 보충 없이 0부터 다시 시작 ---
        if (_pausedByFull && !isFull)
        {
            _pausedByFull = false;
            _t = 0f;                  // 다음 프레임부터 0→1로 진행
            // 슬라이더 값은 아래 진행 계산으로 자연스럽게 갱신
        }

        // --- 0→1 채움 ---
        _t += Time.deltaTime;

        // (FPS 드랍 대비) 이번 프레임에서 끝난 사이클 수 계산
        float dur = Mathf.Max(0.0001f, durationSeconds);
        int cycles = Mathf.FloorToInt(_t / dur);
        if (cycles > 0)
        {
            // 규칙: 1→0으로 리셋되는 순간에만 AddFish(1) 시도
            //      여러 사이클이 한 프레임에 끝날 수 있으므로 그 횟수만큼 처리
            for (int i = 0; i < cycles; i++)
            {
                _t -= dur;

                // 혹시 사이 중간에 누군가가 풀로 만들어버렸다면 즉시 1에서 멈춤
                if (gm.HaveFishCount >= gm.MaxFishCount)
                {
                    _pausedByFull = true;
                    _t = dur;
                    slider.value = 1f;
                    return;
                }

                // 리셋 타이밍에만 +1
                int added = gm.AddFish(1);

                // 공간 없어서 실패했다면(이 프레임 사이 풀로 됨) 1에서 멈춤
                if (added == 0 || gm.HaveFishCount >= gm.MaxFishCount)
                {
                    _pausedByFull = true;
                    _t = dur;
                    slider.value = 1f;
                    return;
                }
            }
        }

        float progress = Mathf.Clamp01(_t / dur);
        slider.value = easeCurve.Evaluate(progress);
    }

    // 소환 등으로 수량이 바뀌어도 여기서는 Value/슬라이더를 직접 건드리지 않음
    private void HandleFishChanged(int have, int max)
    {
        // 의도치 않은 자동 보충/사이클 허용을 막기 위해 비워둠
        // (게이지 제어는 Update에서만 수행)
    }

    public void UpdateFishTime()
    {
        var gym = CatGymUPGradeManager.Instance;
        if (gym == null) return;

        int idx = Mathf.Clamp(gym.CatGymLevel[4], 0, gym.DecreaseFishTimeValue.Length - 1);
        durationSeconds = Mathf.Max(0.1f, gym.DecreaseFishTimeValue[idx]);
    }
}