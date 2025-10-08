using UnityEngine;
using UnityEngine.UI;

public class GainAutoMergeSlider : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider slider;
    public Button OnAndOff;
    public Text OnAndOffText;

    [Header("게이지 설정")]
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private float _t = 0f; // 0 ~ durationSeconds

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

    void Update()
    {
        if (!slider) return;

        if (_Auto) _t += Time.deltaTime;
        else return;

        float duration = Mathf.Max(0.1f, CatGymDrugManager.Instance.GetCurrentAbility(CatGymDrugManager.DrugType.Factorian));
        if (GameManager.Instance.OneDayBuffTime > 0) Mathf.Max(0.1f, duration -= GameManager.Instance.OneDayBuff_DecreaseAutoMerge);
        float normalized = Mathf.Clamp01(_t / duration);

        slider.value = easeCurve.Evaluate(normalized);

        if (normalized >= 1f)
        {
            RunAutoMerge();
            _t = 0f;
            slider.value = 0f;
        }
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

    void RunAutoMerge()
    {
        Debug.Log("RunAutoMerge 실행!");
        CatMergeManager.Instance.ForceMerge();
    }
}