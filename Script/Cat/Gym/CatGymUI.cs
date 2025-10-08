using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

public class CatGymUI : MonoBehaviour
{
    public event Action<float> OnFishTimeChanged;
    public event Action OnFishEnhancementChanged;  // 물고기 강화 변경 이벤트

    [Header("구매 버튼들 (길이 7)")]
    public Button[] CatGymBuyBtn;

    [Header("상점 UI")]
    public Text[] Text_CatGymLevel;
    public Text[] Text_CatGymValue;
    public Text[] Text_CatGymNeedCoin;

    [Header("제한")]
    [Tooltip("3배 확률 Blocking")]
    public GameObject Block_Three;
    [Tooltip("냥이 최소 수치 강화 Blocking")]
    public GameObject Block_Bucket;

    void Awake()
    {
        WireUpButtons();
    }

    private void OnEnable()
    {
        UpdateCatGymUI();
        var coin = CatCoinManager.Instance;
        if (coin != null) coin.OnCatCoinChanged += HandleCoinChanged;
    }

    private void OnDisable()
    {
        var coin = CatCoinManager.Instance;
        if (coin != null) coin.OnCatCoinChanged -= HandleCoinChanged;
    }

    void WireUpButtons()
    {
        if (CatGymBuyBtn == null || CatGymBuyBtn.Length < 7)
        {
            Debug.LogError("[CatGymUI] CatGymBuyBtn 배열이 7개 미만입니다.");
            return;
        }

        // 안전하게 기존 리스너 제거 후 등록
        AddListenerSafe(0, () => UPGradeCatGymType(CatGymUPGradeManager.GymType.GetDoubleCatCoinProbability, 0));
        AddListenerSafe(1, () => UPGradeCatGymType(CatGymUPGradeManager.GymType.GetThreeCatCoinProbability, 1));
        AddListenerSafe(2, () => UPGradeCatGymType(CatGymUPGradeManager.GymType.IncreaseMaxCat, 2));
        AddListenerSafe(3, () => UPGradeCatGymType(CatGymUPGradeManager.GymType.IncreaseMaxFish, 3));
        AddListenerSafe(4, () => UPGradeCatGymType(CatGymUPGradeManager.GymType.DecreaseFishTime, 4));
        AddListenerSafe(5, () => UPGradeCatGymType(CatGymUPGradeManager.GymType.EnhancementFish, 5));
        AddListenerSafe(6, () => UPGradeCatGymType(CatGymUPGradeManager.GymType.EnhancementBucket, 6));
    }

    void AddListenerSafe(int index, UnityAction action)
    {
        if (index < 0 || index >= CatGymBuyBtn.Length || CatGymBuyBtn[index] == null) return;
        CatGymBuyBtn[index].onClick.RemoveAllListeners();
        CatGymBuyBtn[index].onClick.AddListener(action);
    }

    /// <summary>
    /// 업그레이드 처리(코인 차감 + 레벨업 + 부가 이벤트)
    /// </summary>
    private void UPGradeCatGymType(CatGymUPGradeManager.GymType type, int slot)
    {
        var coinManager = CatCoinManager.Instance;
        var gym = CatGymUPGradeManager.Instance;
        if (coinManager == null || gym == null) return;

        int cur = gym.CatGymLevel[slot];
        int maxLevel = gym.GetMaxLevel(type);
        if (cur >= maxLevel) { UpdateCatGymUI(); return; } // 이미 MAX

        long need = gym.GetCatUPGradeCost(type, cur);
        if (need < 0 || coinManager.HaveCatCoinCount < need) { UpdateCatGymUI(); return; }

        coinManager.RemoveCatCoin(need);
        gym.CatGymLevel[slot] = Mathf.Min(cur + 1, maxLevel);

        // 타입별 부가 효과
        if (type == CatGymUPGradeManager.GymType.EnhancementFish)
            OnFishEnhancementChanged?.Invoke();
        else if (type == CatGymUPGradeManager.GymType.IncreaseMaxCat)
            CatManager.Instance?.AddCatMaxCount(1);
        else if (type == CatGymUPGradeManager.GymType.IncreaseMaxFish)
            GameManager.Instance?.AddFishMaxCount(1);

        UpdateCatGymUI();
    }

    /// <summary>
    /// 고양이 헬스장 UI 갱신
    /// 0 => 고양이 코인 2배 확률
    /// 1 => 고양이 코인 3배 확률
    /// 2 => 고양이 최대 수
    /// 3 => 물고기 최대 수
    /// 4 => 물고기 시간 감소
    /// 5 => 물고기 강화
    /// 6 => 물고기 통 강화
    /// </summary>
    private void UpdateCatGymUI()
    {
        var gym = CatGymUPGradeManager.Instance;
        var coin = CatCoinManager.Instance;
        if (gym == null || coin == null) return;

        for (int i = 0; i < gym.CatGymLevel.Length; i++)
        {
            var type = (CatGymUPGradeManager.GymType)i;

            int curLv = gym.CatGymLevel[i];
            int maxLv = gym.GetMaxLevel(type);
            bool isMax = (curLv >= maxLv);

            // ── 레벨 텍스트: 항상 "Lv. 현재/최대" ──
            if (Text_CatGymLevel != null && i < Text_CatGymLevel.Length && Text_CatGymLevel[i] != null)
                Text_CatGymLevel[i].text = $"Lv. {curLv}/{maxLv}";

            // ── 필요 코인 텍스트 ──
            if (Text_CatGymNeedCoin != null && i < Text_CatGymNeedCoin.Length && Text_CatGymNeedCoin[i] != null)
            {
                string display;
                if (isMax) display = "MAX";
                else
                {
                    long need = gym.GetCatUPGradeCost(type, curLv);
                    display = (need < 0) ? "MAX" : Formatted.FormatKoreanNumber(need);
                }

                var label = Text_CatGymNeedCoin[i];
                label.text = display;
                label.fontSize = GetFontSizeByLen(display);
            }

            // ── 밸류 텍스트 (MAX면 "A (MAX)", 아니면 "A → B") ──
            if (Text_CatGymValue != null && i < Text_CatGymValue.Length && Text_CatGymValue[i] != null)
            {
                int last = maxLv;                 // GetMaxLevel이 테이블 길이 고려
                int a = Mathf.Clamp(curLv, 0, last);
                int b = isMax ? a : Mathf.Clamp(curLv + 1, 0, last);

                switch (i)
                {
                    case 0: // 2배 확률
                        {
                            float va = gym.GetDoubleCatCoinProbabilityValue[a];
                            float vb = gym.GetDoubleCatCoinProbabilityValue[b];
                            Text_CatGymValue[i].text = isMax
                                ? $"{va:F1}% (MAX)"
                                : $"{va:F1}% → {vb:F1}%";
                            break;
                        }
                    case 1: // 3배 확률
                        {
                            float va = gym.GetThreeCatCoinProbabilityValue[a];
                            float vb = gym.GetThreeCatCoinProbabilityValue[b];
                            Text_CatGymValue[i].text = isMax
                                ? $"{va:F1}% (MAX)"
                                : $"{va:F1}% → {vb:F1}%";
                            break;
                        }
                    case 2: // 고양이 최대 수
                        {
                            int va = gym.IncreaseMaxCatValue[a];
                            int vb = gym.IncreaseMaxCatValue[b];
                            Text_CatGymValue[i].text = isMax
                                ? $"{va} (MAX)"
                                : $"{va} → {vb}";
                            break;
                        }
                    case 3: // 물고기 최대 수
                        {
                            int va = gym.IncreaseMaxFishValue[a];
                            int vb = gym.IncreaseMaxFishValue[b];
                            Text_CatGymValue[i].text = isMax
                                ? $"{va} (MAX)"
                                : $"{va} → {vb}";
                            break;
                        }
                    case 4: // 물고기 시간 감소
                        {
                            float va = gym.DecreaseFishTimeValue[a];
                            float vb = gym.DecreaseFishTimeValue[b];
                            Text_CatGymValue[i].text = isMax
                                ? $"{va:F1} (MAX)"
                                : $"{va:F1} → {vb:F1}";
                            break;
                        }
                    case 5: // 물고기 종류
                        {
                            string va = gym.EnhancementFishValue[a];
                            string vb = gym.EnhancementFishValue[b];
                            Text_CatGymValue[i].text = isMax
                                ? $"{va} (MAX)"
                                : $"{va} → {vb}";
                            break;
                        }
                    case 6: // 최소 냥이 버킷
                        {
                            string va = gym.EnhancementBucketValue[a];
                            string vb = gym.EnhancementBucketValue[b];
                            Text_CatGymValue[i].text = isMax
                                ? $"최소 냥이 등장 : {va} (MAX)"
                                : $"최소 냥이 등장 : {va} → {vb}";
                            break;
                        }
                }
            }

            // ── 버튼 상태 ──
            if (CatGymBuyBtn != null && i < CatGymBuyBtn.Length && CatGymBuyBtn[i] != null)
            {
                var btn = CatGymBuyBtn[i];
                var colors = btn.colors;

                if (isMax)
                {
                    btn.interactable = false;
                    Color maxColor = Color.gray;
                    colors.normalColor = colors.highlightedColor = colors.pressedColor =
                    colors.selectedColor = colors.disabledColor = maxColor;
                }
                else
                {
                    long need = gym.GetCatUPGradeCost(type, curLv);
                    bool canBuy = (need >= 0) && (CatCoinManager.Instance.HaveCatCoinCount >= need);
                    btn.interactable = canBuy;

                    ColorUtility.TryParseHtmlString("#00FFA5", out Color buyable);
                    ColorUtility.TryParseHtmlString("#FFFFFF", out Color notBuy);
                    Color targetColor = canBuy ? buyable : notBuy;

                    colors.normalColor = colors.highlightedColor = colors.pressedColor =
                    colors.selectedColor = colors.disabledColor = targetColor;
                }
                btn.colors = colors;
            }
        }

        if (Block_Three) Block_Three.SetActive(gym.CatGymLevel[0] <= 99);
        if (Block_Bucket) Block_Bucket.SetActive(gym.CatGymLevel[5] <= 8);
    }

    private void HandleCoinChanged(long have)
    {
        // 코인 수가 변할 때마다 버튼/텍스트 갱신
        UpdateCatGymUI();
    }

    // (참고) 필요시 사용할 수 있는 헬퍼들
    private (int cur, int next, bool isMax) GetCurNext(int curLv, int maxLv)
    {
        int cur = Mathf.Clamp(curLv, 0, Mathf.Max(0, maxLv));
        bool isMax = (cur >= maxLv);
        int next = isMax ? cur : cur + 1;
        return (cur, next, isMax);
    }

    // 길이 계산: 콤마(,)는 제외
    int VisualLen(string s) => string.IsNullOrEmpty(s) ? 0 : s.Replace(",", "").Length;

    int GetFontSizeByLen(string s)
    {
        int len = VisualLen(s);
        if (len <= 6) return 30;
        if (len >= 7) return 25;
        return 25; // 8글자 이상
    }
}
