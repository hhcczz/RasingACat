using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("플레이어 정보")]

    [Tooltip("가지고 있는 생선의 수")]
    [Min(0)] public int HaveFishCount = 5;
    [Tooltip("최대로 가질 수 있는 생선의 수")]
    [Min(0)] public int MaxFishCount = 5;

    /// <summary>
    /// 0 = BonusCoin, 1 = AutoMerge
    /// </summary>
    [Min(0)] public int[] AdLevel = new int[2];
    public int[] PlayAdTimeToCoin;
    public int[] PlayAdTimeToMerge;

    /// <summary>
    /// 0 = BonusCoin, 1 = AutoMerge
    /// </summary>
    public int[] RemainingTime = new int[2];

    public int TotalSummonedFishs = 0;

    public int OneDayBuffTime = 0;
    public bool OneDayBuffUsed = false;

    public int OneDayBuff_PlusMultipleCoin = 3;
    public int OneDayBuff_PlusMultipleGoldCoin = 3;
    public int OneDayBuff_DecreaseFishTime = 1;
    public int OneDayBuff_DecreaseAutoMerge = 10;
    public int OneDayBuff_DecreaseAutoSummons = 10;
    public int OneDayBuff_DecreaseRandomItemTime = 30;
    public bool OneDayBuff_FixToRandomItemProability = false;
    public float OneDayBuff_PlusJackPotMerge = 0.05f;
    public int OneDayBuff_PlusSummonsLevel = 1;

    public event Action<int, int> OnFishChanged;

    public GameObject Block;
    public AdUI _adui;
    public OneDayBuffUI onedayui;

    void Awake() { if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } }


    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMPitch(0.95f); 
            AudioManager.Instance.PlayBGM(0);          // bgmClips[0] 실행
        }
        else
        {
            // (선택) AudioManager 초기화 타이밍이 더 늦을 수 있을 때 대비
            StartCoroutine(WaitAndPlayBGM());
        }

        RemainingTime = new int[2];
        AdLevel = new int[2];
        for (int i = 0; i < AdLevel.Length; i++)
        {
            int index = i;
            AdLevel[index] = 0;
        }
        
        PlayAdTimeToCoin = new int[100];
        PlayAdTimeToMerge = new int[100];

        for(int i = 0; i < PlayAdTimeToCoin.Length; i++)
        {
            int index = i;

            PlayAdTimeToCoin[index] = 600 + 5 * i;
            PlayAdTimeToMerge[index] = 60 + 1 * i;
        }
        StartCoroutine(AdTimeCountdown());
        StartCoroutine(OneDayBuffCountdown());

        DisableBlock();
    }

    private IEnumerator WaitAndPlayBGM()
    {
        while (AudioManager.Instance == null) yield return null;
        AudioManager.Instance.SetBGMPitch(0.95f);
        AudioManager.Instance.PlayBGM(0);
    }
    // GameManager API

    /// <summary>
    /// 물고기를 추가하는 함수입니다.
    /// 추가에 성공하면 OnFishChanged로 방송 구독자들에게 뿌려줍니다.
    /// </summary>
    /// <param name="amount">물고기 개수(int)</param>
    /// <returns></returns>
    public int AddFish(int amount = 0)
    {
        if (amount <= 0)
        {
            Debug.LogError($"[GameManager] AddFish에 음수 혹은 0 입력 | {amount} ");
            return 0;
        }

        int space = Mathf.Max(0, MaxFishCount - HaveFishCount);
        int added = Mathf.Min(space, amount);

        HaveFishCount += added;
        OnFishChanged?.Invoke(HaveFishCount, MaxFishCount);

        if (added < amount)
            Debug.Log($"[GameManager] 용량 초과로 {amount - added}마리는 못 담았습니다. (현재 {HaveFishCount}/{MaxFishCount})");

        Debug.Log($"[GameManager] 생선 더하기 성공 | 실제 추가: {added}");
        
        return added;
    }

    /// <summary>
    /// 물고기를 제거하는 함수입니다.
    /// 제거에 성공하면 OnFishChanged로 방송 구독자들에게 뿌려줍니다.
    /// </summary>
    /// <param name="amount">물고기 개수(int)</param>
    /// <returns></returns>
    public int RemoveFish(int amount = 0)
    {
        if (amount <= 0)
        {
            Debug.LogError($"[GameManager] RemoveFish에 음수 혹은 0 입력 | {amount}");
            return 0;
        }

        int removed = Mathf.Min(HaveFishCount, amount);  // 보유량 기준
        HaveFishCount -= removed;

        OnFishChanged?.Invoke(HaveFishCount, MaxFishCount);

        if (removed < amount)
            Debug.Log($"[GameManager] 보유 수량 부족으로 {amount - removed}마리는 제거하지 못했습니다. (현재 {HaveFishCount}/{MaxFishCount})");

        Debug.Log($"[GameManager] 생선 빼기 성공 | 실제 제거: {removed}");
        TotalSummonedFishs++;
        return removed;
    }
    public void AddFishMaxCount(int delta)
    {
        MaxFishCount= Mathf.Max(1, MaxFishCount + delta);
        if (HaveFishCount > MaxFishCount)
            HaveFishCount = MaxFishCount;
        OnFishChanged?.Invoke(HaveFishCount, MaxFishCount);
    }


    public void EnableBlock() { Block.SetActive(true); }
    public void DisableBlock() { Block.SetActive(false); }

    public void AddAdTime(string type, int index = -1)
    {
        if (index == -1) return;
        if(type == "Coin") RemainingTime[index] += PlayAdTimeToCoin[AdLevel[index]];
        if(type == "Merge") RemainingTime[index] += PlayAdTimeToMerge[AdLevel[index]];
    }
    private IEnumerator AdTimeCountdown()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            for (int i = 0; i < RemainingTime.Length; i++)
            {
                int index = i;

                if (RemainingTime[index] > 0)
                {
                    RemainingTime[index]--;

                    _adui.RemainingTimeToCoin.text = Formatted.FormatTimeDouble(RemainingTime[0]);
                    _adui.RemainingTimeToMerge.text = Formatted.FormatTimeDouble(RemainingTime[1]);

                    _adui.RemainingTimeToCoinScreen.text = Formatted.FormatTimeDouble(RemainingTime[0]);
                    _adui.RemainingTimeToMergeScreen.text = Formatted.FormatTimeDouble(RemainingTime[1]);
                    _adui.ImgScreenRemaining[index].color = new Color(1, 1, 1, 1);
                }
                else
                {
                    _adui.ImgScreenRemaining[index].color = new Color(0, 0, 0, 1);
                }

            }
        }
    }

    private IEnumerator OneDayBuffCountdown()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if(OneDayBuffTime > 0)
            {
                OneDayBuffTime--;

                onedayui.RemainingTimeToOneDayBuff.text = Formatted.FormatTimeDouble(OneDayBuffTime);
                onedayui.ImgScreenRemaining.color = new Color(1, 1, 1, 1);
            }
            else
            {
                onedayui.ImgScreenRemaining.color = new Color(0, 0, 0, 1);
            }
            
        }
    }
}
