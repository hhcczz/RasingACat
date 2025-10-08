using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CatSummonsUI : MonoBehaviour
{
    public Image[] CatSummonsThumbnail;
    public Button[] CatSummonsButton;
    public Text[] CatSummonsName;
    public Text[] CatSummonsDesc;
    public Text[] CatSummonsAmount;

    public GameObject WarningMaxCatTextBox;

    public TossFish tossfish;
    private void Start()
    {
        for(int i = 0; i < CatSummonsButton.Length; i++)
        {
            int index = i;

            CatSummonsButton[index].onClick.AddListener(() => SummonsCat(index));
        }
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        for (int i = 0; i < CatSummonsThumbnail.Length; i++)
        {
            int index = i;

            bool discovered = false;
            discovered = CatManager.Instance.IsCatDiscovered[index];

            CatSummonsThumbnail[index].sprite = CatManager.Instance.catPrefabsByLevel[index].GetComponent<Image>().sprite;
            CatSummonsThumbnail[index].color = discovered ? Color.white : Color.black; // #FFFFFF / #000000

            CatSummonsName[index].text = discovered ? CatManager.Instance.catNamesByLevel[index] : "???";
            CatSummonsDesc[index].text = discovered ? CatMergeManager.Instance._catMergeDescList[index] : "미 발견 고양이";

            CatSummonsButton[index].interactable = CatSummonsManager.Instance._haveSummonsCat[index] >= 1;

            CatSummonsAmount[index].text = $"보유 : {CatSummonsManager.Instance._haveSummonsCat[index]}마리";
        }
    }

    //TODO : 소환작업하기
    private void SummonsCat(int index = -1)
    {
        if (index == -1) return;

        if (CatManager.Instance.catMaxCount <= CatManager.Instance.catHaveCount)
        {
            if (!WarningMaxCatTextBox.activeSelf)  // 올바른 상태 체크
            {
                WarningMaxCatTextBox.SetActive(true);
                Invoke(nameof(HideWarningBox), 3f); // 3초 후 실행
            }
            return;
        }

        if (CatSummonsManager.Instance.TryConsumeTicketByTier(index, 1)) 
        {
            tossfish.RunTossFish(true, index);
        }

        RefreshUI();
    }

    private void HideWarningBox()
    {
        WarningMaxCatTextBox.SetActive(false);
    }

}
