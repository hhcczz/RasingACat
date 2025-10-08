using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayInfoUI : MonoBehaviour
{
    [Header("플레이 정보 오브젝트 연결")]
    [Tooltip("고양이 사진")]
    public Image Cat;
    [Tooltip("고양이 최대 마리 수")]
    public Text MaxCat;
    [Tooltip("고양이 최대 레벨")]
    public Text CatLv;
    [Tooltip("고양이 소환 마리 수")]
    public Text CatSummonAmount;
    [Tooltip("물고기 최대 마리 수")]
    public Text MaxFish;
    [Tooltip("물고기 최대 레벨")]
    public Text FishLv;
    [Tooltip("물고기 소환 마리 수")]
    public Text FishSummonAmount;
    [Tooltip("획득 코인 개수")]
    public Text CoinAmount;
    [Tooltip("두배 확률")]
    public Text DoubleCoinProbability;
    [Tooltip("세배 확률")]
    public Text ThreeCoinProbability;
    [Tooltip("추가 코인 퍼센트")]
    public Text PlusCoin;
    [Tooltip("획득 황금 코인 개수")]
    public Text GoldCoinAmount;

    // Start is called before the first frame update
    void Start()
    {
        InitialUI();
    }

    private void OnEnable()
    {
        InitialUI();
    }

    private void InitialUI()
    {
        //TODO : Image Cat
        Cat.sprite = CatManager.Instance.catPrefabsByLevel[CatManager.Instance.GetHighestDiscoveredLevel()].GetComponent<Image>().sprite;


        // 최대 고양이 마리 수
        MaxCat.text = $"최대 고양이 마리 수  \t: \t<color=cyan>{CatManager.Instance.catMaxCount}마리</color>";

        // 최대 고양이 레벨
        CatLv.text = $"최대 고양이 레벨   \t: \t<color=cyan>Lv.{GetHighCatLevel()}</color>";

        // 고양이 소환 횟수
        CatSummonAmount.text = $"고양이 소환 횟수  \t\t: \t<color=cyan>{CatManager.Instance.TotalSummonedCats:N0}회</color>";

        // 최대 물고기 레벨
        MaxFish.text = $"최대 물고기 마리 수  \t: \t<color=cyan>{GameManager.Instance.MaxFishCount}마리</color>";

        // 최대 물고기 종류
        FishLv.text = $"최대 물고기 종류      \t: \t<color=cyan>{CatGymUPGradeManager.Instance.EnhancementFishValue[CatGymUPGradeManager.Instance.CatGymLevel[5]]}</color>";

        // 물고기 소환 횟수
        FishSummonAmount.text = $"물고기 소환 횟수  \t\t: \t<color=cyan>{GameManager.Instance.TotalSummonedFishs:N0}회</color>";

        // 획득 코인 개수
        CoinAmount.text = $"획득 코인 개수  \t\t\t: \t<color=cyan>{Formatted.FormatKoreanNumber(CatCoinManager.Instance.TotalSummonedCoins)}</color>";

        // 2배 코인 확률
        DoubleCoinProbability.text = $"2배 코인 확률  \t\t\t: \t<color=cyan>{CatGymUPGradeManager.Instance.GetDoubleCatCoinProbabilityValue[CatGymUPGradeManager.Instance.CatGymLevel[0]]:F1}%</color>";

        // 3배 코인 확률
        ThreeCoinProbability.text = $"3배 코인 확률  \t\t\t: \t<color=cyan>{CatGymUPGradeManager.Instance.GetThreeCatCoinProbabilityValue[CatGymUPGradeManager.Instance.CatGymLevel[1]]:F1}%</color>";

        // 추가 코인 배율
        PlusCoin.text = $"추가 코인 획득량  \t\t: \t<color=cyan>{CatSuppliesManager.Instance.GetSuppliesBonus() - 100:N0}%</color>";

        // 황금 코인 획득 개수
        GoldCoinAmount.text = $"황금 코인 획득 개수  \t\t: \t<color=cyan>{CatCoinManager.Instance.TotalSummonedGoldCoins:N0}개</color>";

    }

    // 발견된 고양이 중 가장 높은 레벨 찾기
    private int GetHighCatLevel()
    {
        int highCatLevel = 0;
        var discovered = CatManager.Instance.IsCatDiscovered;

        for (int i = 0; i < discovered.Length; i++)
        {
            if (discovered[i])  // 해당 레벨 고양이가 발견된 경우
            {
                if (i > highCatLevel)
                    highCatLevel = i;  // 최대 레벨 갱신
            }
        }

        return highCatLevel;
    }
}
