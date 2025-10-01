using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class CatCoinManager : MonoBehaviour
{
    public static CatCoinManager Instance;

    public Sprite[] CatCoinSprite;

    public event Action<long> OnCatCoinChanged;
    public event Action<long> OnCatGoldCoinChanged;

    [Header("일반 코인")]
    [Tooltip("플레이어가 소유하고 있는 고양이 코인 개수")]
    [Min(0)] public long HaveCatCoinCount = 0;
    [Tooltip("플레이어가 소유할 수 있는 최대 코인 개수")]
    [Min(0)] public long MaxCatCoinCount = 99999999999; // 99억9999만9999

    [Header("황금 코인")]
    [Tooltip("플레이어가 소유하고 있는 황금 고양이 코인 개수")]
    [Min(0)] public long HaveCatGoldCoinCount = 0;
    [Tooltip("플레이어가 소유할 수 있는 최대 황금 코인 개수")]
    [Min(0)] public long MaxCatGoldCoinCount = 99999999; // 9999만9999

    [Tooltip("황금코인 획득 확률")]
    public double getCatGoldCoinProbability = 0.01d;

    [Header("고양이 알 획득 확률")]
    [Min(0)] public double _catEggProbability = 0.01d;

    void Awake() { if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } }


    // 일반코인
    public long TotalSummonedCoins = 0;
    //황금코인
    public long TotalSummonedGoldCoins = 0;


    /// <summary>
    /// 고양이 코인을 더하는 함수입니다.
    /// </summary>
    /// <param name="amount">고양이 코인 개수(long)</param>
    /// <returns></returns>
    public long AddCatCoin(long amount = 0)
    {
        if (amount <= 0)
        {
            Debug.LogError($"[CatCoinManager] AddCatCoin에 음수 혹은 0 입력 | {amount}");
            return 0;
        }

        long space = Math.Max(0, MaxCatCoinCount - HaveCatCoinCount);
        long added = Math.Min(space, amount);

        HaveCatCoinCount += added;
        OnCatCoinChanged?.Invoke(HaveCatCoinCount);

        if (added < amount)
            Debug.Log($"[CatCoinManager] 용량 초과로 {amount - added}는 추가 실패. (현재 {HaveCatCoinCount}/{MaxCatCoinCount})");

        Debug.Log($"[CatCoinManager] 고양이 코인 추가 성공 | 실제 추가: {added}");
        TotalSummonedCoins += added;
        return added;
    }

    /// <summary>
    /// 고양이 코인을 제거하는 함수입니다.
    /// </summary>
    /// <param name="amount">고양이 코인 개수(long)</param>
    /// <returns></returns>
    public float RemoveCatCoin(long amount = 0)
    {
        if (amount <= 0)
        {
            Debug.LogError($"[CatCoinManager] RemoveCatCoin에 음수 혹은 0 입력 | {amount} ");
            return 0;
        }

        long removed = Math.Min(HaveCatCoinCount, amount);  // 보유량 기준
        HaveCatCoinCount -= removed;

        OnCatCoinChanged?.Invoke(HaveCatCoinCount);

        if (removed < amount)
            Debug.Log($"[CatCoinManager] 용량 초과로 {amount - removed}는 제거 실패. (현재 {HaveCatCoinCount}/{MaxCatCoinCount})");

        Debug.Log($"[CatCoinManager] 고양이 코인 제거 성공 | 실제 추가: {removed}");

        return removed;
    }

    /// <summary>
    /// 고양이 코인 배수 판정.
    /// - CatGymLevel[0] : 더블 확률
    /// - CatGymLevel[1] : 트리플 확률
    /// 반환: 최종 배수 (1, 2, 3)
    /// </summary>
    public int MultipleGetCatCoin()
    {
        var gym = CatGymUPGradeManager.Instance;
        if (gym == null || gym.CatGymLevel == null)
            return 1;

        // --- 트리플 확률 가져오기 ---
        float p3 = 0f;
        if (gym.GetThreeCatCoinProbabilityValue != null && gym.CatGymLevel.Length > 1)
        {
            int lv3 = Mathf.Clamp(gym.CatGymLevel[1], 0, gym.GetThreeCatCoinProbabilityValue.Length - 1);
            // 예: 0.5 == 0.5%
            p3 = Mathf.Clamp01(gym.GetThreeCatCoinProbabilityValue[lv3] * 0.01f);
        }

        // --- 더블 확률 가져오기 ---
        float p2 = 0f;
        if (gym.GetDoubleCatCoinProbabilityValue != null && gym.CatGymLevel.Length > 0)
        {
            int lv2 = Mathf.Clamp(gym.CatGymLevel[0], 0, gym.GetDoubleCatCoinProbabilityValue.Length - 1);
            p2 = Mathf.Clamp01(gym.GetDoubleCatCoinProbabilityValue[lv2] * 0.01f);
        }

        // --- 판정: 트리플 우선, 실패 시 더블 ---
        if (UnityEngine.Random.value < p3) return 3;
        if (UnityEngine.Random.value < p2) return 2;
        return 1;


    }

    public bool GetCatEggForCoinTick()
    {
        return UnityEngine.Random.value < _catEggProbability;
    }
    public bool GetCatGoldCoinForCoinTick()
    {
        return UnityEngine.Random.value < getCatGoldCoinProbability;
    }

    /// <summary>
    /// 고양이 황금 코인을 더하는 함수입니다.
    /// </summary>
    /// <param name="amount">고양이 황금 코인 개수(long)</param>
    /// <returns></returns>
    public long AddCatGoldCoin(long amount = 0)
    {
        if (amount <= 0)
        {
            Debug.LogError($"[CatCoinManager] AddCatGoldCoin에 음수 혹은 0 입력 | {amount}");
            return 0;
        }

        long space = Math.Max(0, MaxCatGoldCoinCount - HaveCatGoldCoinCount);
        long added = Math.Min(space, amount);

        HaveCatGoldCoinCount += added;
        OnCatGoldCoinChanged?.Invoke(HaveCatGoldCoinCount);

        if (added < amount)
            Debug.Log($"[CatCoinManager] 용량 초과로 {amount - added}는 추가 실패. (현재 {HaveCatGoldCoinCount}/{MaxCatGoldCoinCount})");

        Debug.Log($"[CatCoinManager] 고양이 골드 코인 추가 성공 | 실제 추가: {added}");
        TotalSummonedGoldCoins += added;
        return added;
    }

    public float RemoveCatGoldCoin(long amount = 0)
    {
        if (amount <= 0)
        {
            Debug.LogError($"[CatCoinManager] RemoveCatGoldCoin에 음수 혹은 0 입력 | {amount} ");
            return 0;
        }

        long removed = Math.Min(HaveCatGoldCoinCount, amount);  // 보유량 기준
        HaveCatGoldCoinCount -= removed;

        OnCatGoldCoinChanged?.Invoke(HaveCatGoldCoinCount);

        if (removed < amount)
            Debug.Log($"[CatCoinManager] 용량 초과로 {amount - removed}는 제거 실패. (현재 {HaveCatGoldCoinCount}/{MaxCatGoldCoinCount})");

        Debug.Log($"[CatCoinManager] 황금 고양이 코인 제거 성공 | 실제 추가: {removed}");

        return removed;
    }
}