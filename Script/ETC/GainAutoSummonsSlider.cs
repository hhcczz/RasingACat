using UnityEngine;
using UnityEngine.UI;

public class GainAutoSummonsSlider : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider slider;
    public Button OnAndOff;
    public Text OnAndOffText;

    [Header("게이지 설정")]
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private float _t = 0f; // 0 ~ durationSeconds

    public TossFish _tossfish;
    private bool _Auto = false;

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
        _Auto = false;

        OnAndOffText.text = _Auto ? "<color=#68FF7E>ON</color>" : "<color=#C0C0C0>OFF</color>";
        
    }

    private void Start()
    {
        OnAndOff.onClick.AddListener(ChangeState);
    }

    private void ChangeState()
    {
        _Auto = !_Auto;
        OnAndOffText.text = _Auto ? "<color=#68FF7E>ON</color>" : "<color=#C0C0C0>OFF</color>";
    }

    void Update()
    {
        if (!slider) return;

        if (_Auto) _t += Time.deltaTime;
        else return;
        
        float duration = Mathf.Max(0.1f, CatGymDrugManager.Instance.GetCurrentAbility(CatGymDrugManager.DrugType.Megician));
        if (GameManager.Instance.OneDayBuffTime > 0) Mathf.Max(0.1f, duration -= GameManager.Instance.OneDayBuff_DecreaseAutoSummons);
        float normalized = Mathf.Clamp01(_t / duration);

        slider.value = easeCurve.Evaluate(normalized);

        // 1 이상 되면 실행
        if (normalized >= 1f)
        {
            RunAutoSummons();

            // 다시 0부터 시작
            _t = 0f;
            slider.value = 0f;
        }
    }

    void RunAutoSummons()
    {
        Debug.Log("RunAutoMerge 실행!");
        _tossfish.RunTossFish();
    }
}