using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdUI : MonoBehaviour
{
    public Button OpenADUI;
    public Button CloseADUI;

    public GameObject AdBox;

    public Button PlayBonusCoin;
    public Button PlayAutoCatMerge;

    public Text TimeBonusCoin;
    public Text TimeAutoCatMerge;

    public Text RemainingTimeToCoin;
    public Text RemainingTimeToMerge;

    public Text RemainingTimeToCoinScreen;
    public Text RemainingTimeToMergeScreen;

    public Image[] ImgScreenRemaining;

    public Text[] Level;

    public AutoMerge _auto;

    enum AdType { coin, merge }

    // Start is called before the first frame update
    void Start()
    {
        OpenADUI.onClick.AddListener(OpenUI);
        CloseADUI.onClick.AddListener(CloseUI);

        PlayBonusCoin.onClick.AddListener(() => PlayToAD(AdType.coin));
        PlayAutoCatMerge.onClick.AddListener(() => PlayToAD(AdType.merge));
    }

    private void PlayToAD(AdType _type)
    {
        // TODO : 광고 조건 달기

        if (_type == AdType.coin) 
        {
            GameManager.Instance.AdLevel[0] += 1;
            GameManager.Instance.AddAdTime("Coin", 0);
        }
        else if(_type == AdType.merge)
        {
            GameManager.Instance.AdLevel[1] += 1;
            GameManager.Instance.AddAdTime("Merge", 1);
            _auto.StartAutoMerge();
        }
        RefreshUI();
    }

    private void RefreshUI()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // RemainingTime
        if (gm.RemainingTime != null && gm.RemainingTime.Length >= 2)
        {
            RemainingTimeToCoin.text = Formatted.FormatTimeDouble(gm.RemainingTime[0]);
            RemainingTimeToMerge.text = Formatted.FormatTimeDouble(gm.RemainingTime[1]);

            RemainingTimeToCoinScreen.text = Formatted.FormatTimeDouble(gm.RemainingTime[0]);
            RemainingTimeToMergeScreen.text = Formatted.FormatTimeDouble(gm.RemainingTime[1]);
        }

        // AdLevel → 안전 인덱스 보정
        int levelCoin = (gm.AdLevel != null && gm.AdLevel.Length > 0) ? Mathf.Clamp(gm.AdLevel[0], 0, gm.PlayAdTimeToCoin.Length - 1) : 0;
        int levelMerge = (gm.AdLevel != null && gm.AdLevel.Length > 1) ? Mathf.Clamp(gm.AdLevel[1], 0, gm.PlayAdTimeToMerge.Length - 1) : 0;

        // PlayAdTime 배열 체크 후 출력
        if (gm.PlayAdTimeToCoin != null && gm.PlayAdTimeToCoin.Length > 0)
            TimeBonusCoin.text = $"충전 : <color=cyan>{Formatted.FormatTimeDouble(gm.PlayAdTimeToCoin[levelCoin])}</color>";

        if (gm.PlayAdTimeToMerge != null && gm.PlayAdTimeToMerge.Length > 0)
            TimeAutoCatMerge.text = $"충전 : <color=cyan>{Formatted.FormatTimeDouble(gm.PlayAdTimeToMerge[levelMerge])}</color>";

        Level[0].text = $"Lv. <color=yellow>{GameManager.Instance.AdLevel[0]}</color>";
        Level[1].text = $"Lv. <color=yellow>{GameManager.Instance.AdLevel[1]}</color>";
    }

    private void OpenUI()
    {
        GameManager.Instance.EnableBlock();
        AdBox.SetActive(true);
        RefreshUI();
    }

    private void CloseUI()
    {
        GameManager.Instance.DisableBlock();
        AdBox.SetActive(false);
        RefreshUI();
    }


}
