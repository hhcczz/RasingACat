using UnityEngine;
using UnityEngine.UI;

public class CrystalShopManager : MonoBehaviour
{
    [Header("구매 버튼들 (각 상품 1회만 구매 가능)")]
    [SerializeField] private Button[] purchaseButtons;
    public static CrystalShopManager Instance { get; private set; }

    // 실행 중 구매 여부 저장 (임시)
    private bool[] purchasedFlags;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        purchasedFlags = new bool[purchaseButtons.Length];

        for (int i = 0; i < purchaseButtons.Length; i++)
        {
            int idx = i;
            if (purchaseButtons[i] != null)
                purchaseButtons[i].onClick.AddListener(() => TryPurchase(idx));
        }
    }

    private void TryPurchase(int index)
    {
        if (index < 0 || index >= purchaseButtons.Length) return;

        Button btn = purchaseButtons[index];
        if (btn == null || !btn.interactable) return; // 이미 구매됨

        Debug.Log($"상품 {index} 구매 완료!");

        // 버튼 비활성화 + 텍스트 변경
        btn.interactable = false;
        var txt = btn.GetComponentInChildren<Text>();
        if (txt != null) txt.text = "구매완료";

        // 구매 상태 기록
        purchasedFlags[index] = true;

        // ★ 상위 상품 구매 시, 하위 배수 버튼 전부 비활성화
        // 예: index==3(5배)면 0,1,2 전부 비활성화
        DisableLowerTiers(index, markLowerAsIncluded: true, changeLabel: true);
    }

    /// <summary>
    /// 상위 상품 구매 시 하위 배수 버튼을 전부 비활성화.
    /// - markLowerAsIncluded: 하위 배수도 '보유(포함)' 처리할지 여부
    /// - changeLabel: 버튼 텍스트를 "포함됨"으로 바꿀지 여부
    /// </summary>
    private void DisableLowerTiers(int purchasedIndex, bool markLowerAsIncluded = true, bool changeLabel = true)
    {
        if (purchaseButtons == null) return;

        for (int i = 0; i < purchasedIndex; i++)
        {
            var lowBtn = purchaseButtons[i];
            if (lowBtn == null) continue;

            // 이미 구매완료라면 건너뜀
            if (!lowBtn.interactable) continue;

            lowBtn.interactable = false;

            if (changeLabel)
            {
                var txt = lowBtn.GetComponentInChildren<Text>();
                if (txt != null) txt.text = "포함됨";
            }

            // 논리적으로도 하위 혜택 포함 처리하고 싶다면 true
            if (markLowerAsIncluded && purchasedFlags != null && i < purchasedFlags.Length)
                purchasedFlags[i] = true;
        }
    }

    // ───────── 구매 여부 확인 API ─────────
    public bool IsPurchased(int index)
    {
        if (purchasedFlags == null || index < 0 || index >= purchasedFlags.Length) return false;
        return purchasedFlags[index];
    }
}