using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CatCoinNum : MonoBehaviour
{
    public Text Text_CatCoinCounts;


    // Start is called before the first frame update
    void Start()
    {
        Text_CatCoinCounts.text = Formatted.FormatKoreanNumber(CatCoinManager.Instance.HaveCatCoinCount);
        CatCoinManager.Instance.OnCatCoinChanged += UpdateCatCoinUI;
    }

    /// <summary>
    /// 고양이 코인 개수가 변경될 때 마다 UI를 업데이트 시켜줍니다.
    /// </summary>
    /// <param name="current">현재 증가 값</param>
    /// <param name="max">최대 증가 값</param>

    void UpdateCatCoinUI(long current)
    {
        if (Text_CatCoinCounts)
            Text_CatCoinCounts.text = Formatted.FormatKoreanNumber(CatCoinManager.Instance.HaveCatCoinCount);
        else Debug.LogError("[CatCoinNum] 현재 Text가 연결 안되어있습니다.");
    }
}
