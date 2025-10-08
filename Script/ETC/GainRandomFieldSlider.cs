using UnityEngine;
using UnityEngine.UI;

public class GainRandomFieldSlider : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider slider;

    [Header("게이지 설정")]
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("스폰 매니저")]
    [SerializeField] private RandomFieldItems spawner;

    private float _t = 0f;   // 진행 타이머
    private bool auto = true; // 항상 자동 실행 (On/Off 버튼 없음)

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

    void Update()
    {
        if (!slider || !auto) return;

        // 1) 기본 지속시간
        float duration = 60f;
        var drug = CatGymDrugManager.Instance;
        if (drug != null)
            duration = Mathf.Max(0.1f, drug.GetCurrentAbility(CatGymDrugManager.DrugType.Chur));

        // 2) 원데이 버프 적용
        var gm = GameManager.Instance;
        if (gm != null && gm.OneDayBuffTime > 0)
            duration = Mathf.Max(0.1f, duration - gm.OneDayBuff_DecreaseRandomItemTime);

        // 3) 타이머 증가 + 슬라이더 갱신
        _t += Time.deltaTime;
        float normalized = Mathf.Clamp01(_t / duration);
        slider.value = easeCurve.Evaluate(normalized);

        // 4) 다 차면 실행
        if (normalized >= 1f)
        {
            RunSpawn();
            _t = 0f;
            slider.value = 0f;
        }
    }

    private void RunSpawn()
    {
        if (spawner != null)
        {
            spawner.SendMessage("TrySpawn", SendMessageOptions.DontRequireReceiver);
            Debug.Log("랜덤 필드 아이템 스폰 실행!");
        }
    }
}