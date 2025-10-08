using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RelicsUI : MonoBehaviour
{
    private enum _result { success, fail, broken };

    [Header("대장간 화면 UI")]
    public Image Relics_CoverImg;
    public Sprite[] Relics_CoverSprite;
    public Image[] Relics_RankImg;

    public Text Relics_Rank;
    public Text Relics_UpgradeCatCoinCost;
    public Text Relics_UpgradeCatGoldCoinCost;
    public Text Relics_SuccessProbability;
    public Text Relics_BreakProbability;

    public Text Relics_BonusMergeProbability;
    public Text Relics_BonusCatEggProbability;

    public Button Relics_StartUpgradeBtn;
    public Button Relics_ProtectedSwapBtn;
    public Text Relics_ProtectedText;

    public Sprite[] Relics_ProtectedSwapImg;

    [Header("결과 화면 UI")]
    public GameObject Relics_ResultPanel;
    public Button Relics_ResultCloseBtn;

    public Text Relics_ResultTitle;

    public Image Relics_ResultImgPrev;
    public Image Relics_ResultImgCurrent;

    public Text Relics_ResultLevelPrev;
    public Text Relics_ResultLevelCurrent;

    public Text Relics_ResultMerge;
    public Text Relics_ResultCatEgg;

    public GameObject Block;
    public Button PhoneCloseBtn;

    private int _lastPlayerLevel = 0;

    private bool _isProtected = false;

    private void OnEnable()
    {
        RefreshUI();
    }

    [System.Obsolete]
    private void Start()
    {
        Relics_ProtectedSwapBtn.onClick.AddListener(ChangeProtectedMod);
        Relics_StartUpgradeBtn.onClick.AddListener(() => Relics_StartUpgradeBtn.interactable = false);
        Relics_ResultCloseBtn.onClick.AddListener(CloseResult);

        _lastPlayerLevel = RelicsManager.Instance.Player_RelicsLv;
    }

    private void CloseResult()
    {
        PhoneCloseBtn.interactable = true;
        Relics_ResultPanel.SetActive(false);
        Block.SetActive(false);
    }

    private void RefreshUI()
    {
        var RI = RelicsManager.Instance;
        var cm = CatCoinManager.Instance;
        var _max = RI.maxLevel <= RI.Player_RelicsLv;

        if (RI == null || RI._needCost == null) return;

        // 유물 이미지
        Relics_CoverImg.sprite = Relics_CoverSprite[RI.Player_RelicsLv];

        // 유물 몇등급인지 시각적으로 보여주는 이미지
        for(int i = 0; i < Relics_RankImg.Length; i++)
        {
            int index = i;

            if (index < RelicsManager.Instance.Player_RelicsLv) Relics_RankImg[index].color = new Color(1, 1, 1, 1);
            else Relics_RankImg[index].color = new Color(1, 1, 1, 100f / 255f);
        }

        // 유물 몇등급인지 Text
        Relics_Rank.text = _max ? "Lv. <color=yellow>Max</color> -> Lv. <color=yellow>Max</color>" : $"Lv. <color=yellow>{RI.GetRelicsLevel()}</color> -> Lv.  <color=yellow>{RI.GetRelicsLevel(1)}</color>";

        // 소비 재화
        if (_max)
        {
            Relics_UpgradeCatCoinCost.text = "강화 최대치";
            Relics_UpgradeCatGoldCoinCost.text = "강화 최대치";
        }
        else
        {
            Relics_UpgradeCatCoinCost.text = _isProtected ? $": {Formatted.FormatKoreanNumber(RI.GetNeedCost().catcoin * 3)}" : $": {Formatted.FormatKoreanNumber(RI.GetNeedCost().catcoin)}";
            Relics_UpgradeCatGoldCoinCost.text = _isProtected ? $": {Formatted.FormatKoreanNumber(RI.GetNeedCost().catGoldcoin * 3)}" : $": {Formatted.FormatKoreanNumber(RI.GetNeedCost().catGoldcoin)}";
        }

        // 강화 확률
        if (_max)
        {
            Relics_SuccessProbability.text = "강화 최대치";
            Relics_BreakProbability.text = "강화 최대치";
        }
        else
        {
            Relics_SuccessProbability.text = $": {RI.SuccessProbability[RI.Player_RelicsLv]}%";
            Relics_BreakProbability.text = _isProtected ? ": 0%" : $": {RI.BreakProbability[RI.Player_RelicsLv]}%";
        }

        Image _img = Relics_ProtectedSwapBtn.GetComponent<Image>();
        if (RelicsManager.Instance.Player_RelicsLv < 10)
        {
            Relics_ProtectedSwapBtn.interactable = false;
            _img.sprite = Relics_ProtectedSwapImg[0];
            _img.color = new Color(1, 1, 1, 130f / 255f);
        }
        else
        {
            Relics_ProtectedSwapBtn.interactable = true;
            _img.color = new Color(1, 1, 1, 1);
        }

        // 강화 버튼 관리
        if (_isProtected && !_max)
        {
            Relics_StartUpgradeBtn.interactable = cm.HaveCatCoinCount >= RI.GetNeedCost().catcoin * 3  && cm.HaveCatGoldCoinCount >= RI.GetNeedCost().catGoldcoin * 3;
        }
        else if(!_isProtected && !_max)
        {
            Relics_StartUpgradeBtn.interactable = cm.HaveCatCoinCount >= RI.GetNeedCost().catcoin && cm.HaveCatGoldCoinCount >= RI.GetNeedCost().catGoldcoin;
        }
        else
        {
            Relics_StartUpgradeBtn.interactable = false;
        }

        // 강화 보너스 능력치
        if (_max)
        {
            Relics_BonusMergeProbability.text = $": <color=#00ED09>{RI.JackpotMergeProbability[26]:F2}</color>%";
            Relics_BonusCatEggProbability.text = $": <color=#00ED09>{RI.JackpotCatEggProbability[26]:F2}</color>%";
        }
        else
        {
            Relics_BonusMergeProbability.text = $": <color=#00ED09>{RI.JackpotMergeProbability[RI.Player_RelicsLv]:F2}</color>%";
            Relics_BonusCatEggProbability.text = $": <color=#00ED09>{RI.JackpotCatEggProbability[RI.Player_RelicsLv]:F2}</color>%";
        }
        
    }

    private void ChangeProtectedMod()
    {
        Image _img = Relics_ProtectedSwapBtn.GetComponent<Image>();
        _isProtected = !_isProtected;

        _img.sprite = _isProtected ? Relics_ProtectedSwapImg[1] : Relics_ProtectedSwapImg[0];
        Relics_ProtectedText.text = _isProtected ? "파괴방지 <color=#00ED09>ON</color>" : "파괴방지 <color=red>OFF</color>";

        RefreshUI();
    }

    [System.Obsolete]
    public void StartRelicsReinforce()
    {
        var RI = RelicsManager.Instance;
        var cm = CatCoinManager.Instance;

        var _needcatcoin = _isProtected ? RI.GetNeedCost().catcoin * 3 : RI.GetNeedCost().catcoin;
        var _needcatgoldcoin = _isProtected ? RI.GetNeedCost().catGoldcoin * 3 : RI.GetNeedCost().catGoldcoin;


        if (cm.HaveCatCoinCount < _needcatcoin &&
            cm.HaveCatGoldCoinCount < _needcatgoldcoin)
        {
            Debug.Log($"강화 반려 : {_needcatcoin} {_needcatgoldcoin}");
            return;
        }
        

        cm.RemoveCatCoin(_needcatcoin);
        if(_needcatgoldcoin != 0) cm.RemoveCatGoldCoin(_needcatgoldcoin);

        var _success_rand = Random.RandomRange(1, 101);
        var _break_rand = Random.RandomRange(1, 101);

        _lastPlayerLevel = RI.Player_RelicsLv;

        if (_break_rand < RI.BreakProbability[RI.Player_RelicsLv])
        {
            BreakRelics();
            ShowResultPanel(_result.broken);
            return;
        }
        if(_success_rand > RI.SuccessProbability[RI.Player_RelicsLv])
        {
            FailRelics();
            ShowResultPanel(_result.fail);
            return;
        }

        AudioManager.Instance.PlaySuccessSound();
        ShowResultPanel(_result.success);
        RI.AddRelicsLevel();

        
        RefreshUI();
    }

    private void BreakRelics()
    {
        if (_isProtected)
        {
            Debug.Log("방어 성공");
            return;
        }

        AudioManager.Instance.PlayBreakSound();

        RelicsManager.Instance.Player_RelicsLv = 0;
        RefreshUI();
        return;
    }

    private void FailRelics()
    {
        AudioManager.Instance.PlayFailSound();
        return;
    }
    
    private void ShowResultPanel(_result result)
    {
        var RI = RelicsManager.Instance;
        var mode = result;

        Relics_ResultImgPrev.sprite = Relics_CoverSprite[RI.Player_RelicsLv];

        if (mode == _result.success)
        {
            Relics_ResultTitle.text = "강화 <color=#00ED09>성공</color>";
            if (RI.Player_RelicsLv < 27)
            {
                Relics_ResultImgCurrent.sprite = Relics_CoverSprite[_lastPlayerLevel + 1];

                Relics_ResultLevelPrev.text = $"Lv. <color=yellow>{_lastPlayerLevel}</color>";
                Relics_ResultLevelCurrent.text = $"Lv. <color=yellow>{_lastPlayerLevel + 1}</color>";

                Relics_ResultMerge.text = $": <color=#00ED09>{RI.JackpotMergeProbability[RI.Player_RelicsLv]:F2}</color>% -> <color=#00ED09>{RI.JackpotMergeProbability[RI.Player_RelicsLv + 1]:F2}</color>%";
                Relics_ResultCatEgg.text = $": <color=#00ED09>{RI.JackpotCatEggProbability[RI.Player_RelicsLv]:F2}</color>% -> <color=#00ED09>{RI.JackpotCatEggProbability[RI.Player_RelicsLv + 1]:F2}</color>%";
            }
        }
        else if(mode == _result.fail)
        {
            Relics_ResultTitle.text = "강화 <color=#FF9999>실패</color>";
            Relics_ResultImgCurrent.sprite = Relics_CoverSprite[RI.Player_RelicsLv];

            Relics_ResultLevelPrev.text = $"Lv. <color=yellow>{_lastPlayerLevel}</color>";
            Relics_ResultLevelCurrent.text = $"Lv. <color=yellow>{_lastPlayerLevel}</color>";

            Relics_ResultMerge.text = $": <color=#00ED09>{RI.JackpotMergeProbability[RI.Player_RelicsLv]:F2}</color>% -> <color=#00ED09>{RI.JackpotMergeProbability[RI.Player_RelicsLv]:F2}</color>%";
            Relics_ResultCatEgg.text = $": <color=#00ED09>{RI.JackpotCatEggProbability[RI.Player_RelicsLv]:F2}</color>% -> <color=#00ED09>{RI.JackpotCatEggProbability[RI.Player_RelicsLv]:F2}</color>%";
        }
        else if(mode == _result.broken)
        {
            Relics_ResultTitle.text = "강화 <color=red>파괴</color>";
            Relics_ResultImgCurrent.sprite = Relics_CoverSprite[0];

            Relics_ResultLevelPrev.text = $"Lv. <color=yellow>{_lastPlayerLevel}</color>";
            Relics_ResultLevelCurrent.text = $"Lv. <color=yellow>0</color>";

            Relics_ResultMerge.text = $": <color=#00ED09>{RI.JackpotMergeProbability[_lastPlayerLevel]:F2}</color>% -> <color=#00ED09>{RI.JackpotMergeProbability[0]:F2}</color>%";
            Relics_ResultCatEgg.text = $": <color=#00ED09>{RI.JackpotCatEggProbability[_lastPlayerLevel]:F2}</color>% -> <color=#00ED09>{RI.JackpotCatEggProbability[0]:F2}</color>%";

        }
        

        Relics_ResultPanel.SetActive(true);
        Block.SetActive(true);
        Relics_StartUpgradeBtn.interactable = true;
    }
}
