using UnityEngine;
using UnityEngine.UI;

public class CatNum : MonoBehaviour
{
    public Text Text_MaxCat;  // 인스펙터에 직접 할당해도 되고 자동 검색도 가능

    void Start()
    {
        Text_MaxCat.text = $"최대 냥이 수 : <color=#5FFF00>{CatManager.Instance.catHaveCount}/{CatManager.Instance.catMaxCount}</color>";
        CatManager.Instance.OnCatCountChanged += UpdateTitle;
    }

    void UpdateTitle(int have, int max)
    {
        if (!Text_MaxCat) return;
        Text_MaxCat.text = $"최대 냥이 수 : <color=#5FFF00>{have}/{max}</color>";
    }
}