using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CatGoldCoinNum : MonoBehaviour
{
    public Text Text_CatGoldCoinCounts;


    // Start is called before the first frame update
    void Start()
    {
        Text_CatGoldCoinCounts.text = CatCoinManager.Instance.HaveCatGoldCoinCount.ToString("N0");
        CatCoinManager.Instance.OnCatGoldCoinChanged += UpdateCatCoinUI;
    }

    /// <summary>
    /// 고양이 황금 코인 개수가 변경될 때 마다 UI를 업데이트 시켜줍니다.
    /// </summary>
    /// <param name="current">현재 증가 값</param>
    /// <param name="max">최대 증가 값</param>

    void UpdateCatCoinUI(long current)
    {
        if (Text_CatGoldCoinCounts)
            Text_CatGoldCoinCounts.text = CatCoinManager.Instance.HaveCatGoldCoinCount.ToString("N0");
        else Debug.LogError("[CatGoldCoinNum] 현재 Text가 연결 안되어있습니다.");
    }
}
