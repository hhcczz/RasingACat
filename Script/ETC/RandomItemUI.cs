using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomItemUI : MonoBehaviour
{
    public GameObject RandomItemPanel;
    public Button OpenBtn;
    public Button CloseBtn;

    public Text Desc;

    // Start is called before the first frame update
    void Start()
    {
        OpenBtn.onClick.AddListener(OpenUI);
        CloseBtn.onClick.AddListener(CloseUI);
    }

    private void OpenUI()
    {
        RefreshUI();
        GameManager.Instance.Block.SetActive(true);
        RandomItemPanel.SetActive(true);
    }
    
    private void CloseUI()
    {
        GameManager.Instance.Block.SetActive(false);
        RandomItemPanel.SetActive(false);
    }

    private void RefreshUI()
    {
        Desc.text = GameManager.Instance.OneDayBuffTime > 0 ? 
            $"<color=cyan>{CatGymDrugManager.Instance.GetCurrentAbility(CatGymDrugManager.DrugType.Chur) - GameManager.Instance.OneDayBuff_DecreaseRandomItemTime}√ </color>\n <color=cyan>100%</color>" 
            : $"<color=cyan>{CatGymDrugManager.Instance.GetCurrentAbility(CatGymDrugManager.DrugType.Chur)}√ </color>\n<color=cyan>50%</color>";
    }
}
