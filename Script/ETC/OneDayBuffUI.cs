using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OneDayBuffUI : MonoBehaviour
{
    [Header("OneDayBuffUI")]
    public Button OpenOneDayBuff;
    public Button CloseOneDayBuff;

    public GameObject OneDayBuffPanel;

    public Button UseOneDayBuff;

    public Text UseOneDayBuffText;

    [Header("OtherUI")]
    public Image ImgScreenRemaining;
    public Text RemainingTimeToOneDayBuff;

    private void Start()
    {
        OpenOneDayBuff.onClick.AddListener(OpenOneDayBuffUI);
        CloseOneDayBuff.onClick.AddListener(CloseOneDayBuffUI);

        UseOneDayBuff.onClick.AddListener(UsingOneDayBuff);
    }

    private void OpenOneDayBuffUI()
    {
        RefreshUI();
        OneDayBuffPanel.SetActive(true);
        GameManager.Instance.Block.SetActive(true);
    }

    private void RefreshUI()
    {
        UseOneDayBuff.interactable = !GameManager.Instance.OneDayBuffUsed;
        if (GameManager.Instance.OneDayBuffUsed)
        {
            UseOneDayBuff.interactable = false;
            UseOneDayBuffText.text = "사용 완료";
        }
        else
        {
            UseOneDayBuff.interactable = true;
            UseOneDayBuffText.text = "사용 가능";
        }
    }

    private void CloseOneDayBuffUI()
    {
        OneDayBuffPanel.SetActive(false);
        GameManager.Instance.Block.SetActive(false);
    }

    private void UsingOneDayBuff()
    {
        GameManager.Instance.OneDayBuffTime = 1200;
        GameManager.Instance.OneDayBuffUsed = true;

        RefreshUI();
    }

}
