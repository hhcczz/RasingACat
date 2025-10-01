using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FishNum : MonoBehaviour
{
    public Text Text_FishCounts;

    // Start is called before the first frame update
    void Start()
    {
        Text_FishCounts.text = $"{GameManager.Instance.HaveFishCount} / {GameManager.Instance.MaxFishCount}";
        GameManager.Instance.OnFishChanged += UpdateFishUI;
    }


    /// <summary>
    /// 물고기 개수가 변경될 때 마다 UI를 업데이트 시켜줍니다.
    /// </summary>
    /// <param name="current">현재 증가 값</param>
    /// <param name="max">최대 증가 값</param>

    void UpdateFishUI(int current, int max)
    {
        if (Text_FishCounts)
            Text_FishCounts.text = $"{current} / {max}";
        else Debug.LogError("[FishNum] 현재 Text가 연결 안되어있습니다.");
    }
}
