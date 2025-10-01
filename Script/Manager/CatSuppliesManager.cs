using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatSuppliesManager : MonoBehaviour
{
    public static CatSuppliesManager Instance;
    void Awake() { if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); } }
    // DB
    public string[] _catSuppliesNameData;
    public string[] _catSuppliesAbilityData;
    public long[] _catSuppliesPriceData;
    public bool[] _catSuppliesBuyItemList;
    public int[] _catSuppliesRealData;

    private void Start()
    {
        _catSuppliesBuyItemList = new bool[100];

        _catSuppliesRealData = new int[]
        {
            20, 20, 20, 20, 20, 20,
            30, 30, 30, 30, 30, 30,
            40, 40, 40, 40, 40, 40,
            50, 50, 50, 50, 50, 50
        };
        InitialSettingData();
    }

    public int GetSuppliesBonus()
    {
        int bonus = 100;
        for (int i = 0; i < _catSuppliesBuyItemList.Length; i++)
        {
            int index = i;
            bonus += _catSuppliesBuyItemList[index] ? _catSuppliesRealData[index] : 0;
        }
        return bonus;
    }
    private void InitialSettingData()
    {
        // 불필요한 이중할당 없이 바로 리터럴로 세팅
        _catSuppliesNameData = new string[]
        {
            "고양이 실뭉치", "고양이 케이지", "장난감 쥐",
            "고양이 가방", "고양이 명화", "고양이 샴푸",
            "고양이 표지판", "푸른 고양이", "고양이 메달",
            "고양이 담요", "고양이 통조림"," 애착 식물",
            "고양이 어항", "고양이 산책줄", "고양이 카메라",
            "애착 생선", "고양이 핸드백", "고양이 목걸이",
            "고양이 어항", "고양이 노트북", "식빵 고양이",
            "고양이 캣잎", "고양이 모래 주걱", "고양이 상비약"
        };

        _catSuppliesAbilityData = new string[]
        {
            "고양이 코인 증가\n<color=#00FFA9>20%</color>",
            "고양이 코인 증가\n<color=#00FFA9>20%</color>",
            "고양이 코인 증가\n<color=#00FFA9>20%</color>",
            "고양이 코인 증가\n<color=#00FFA9>20%</color>",
            "고양이 코인 증가\n<color=#00FFA9>20%</color>",
            "고양이 코인 증가\n<color=#00FFA9>20%</color>",

            "고양이 코인 증가\n<color=#00FFA9>30%</color>",
            "고양이 코인 증가\n<color=#00FFA9>30%</color>",
            "고양이 코인 증가\n<color=#00FFA9>30%</color>",
            "고양이 코인 증가\n<color=#00FFA9>30%</color>",
            "고양이 코인 증가\n<color=#00FFA9>30%</color>",
            "고양이 코인 증가\n<color=#00FFA9>30%</color>",

            "고양이 코인 증가\n<color=#00FFA9>40%</color>",
            "고양이 코인 증가\n<color=#00FFA9>40%</color>",
            "고양이 코인 증가\n<color=#00FFA9>40%</color>",
            "고양이 코인 증가\n<color=#00FFA9>40%</color>",
            "고양이 코인 증가\n<color=#00FFA9>40%</color>",
            "고양이 코인 증가\n<color=#00FFA9>40%</color>",

            "고양이 코인 증가\n<color=#00FFA9>50%</color>",
            "고양이 코인 증가\n<color=#00FFA9>50%</color>",
            "고양이 코인 증가\n<color=#00FFA9>50%</color>",
            "고양이 코인 증가\n<color=#00FFA9>50%</color>",
            "고양이 코인 증가\n<color=#00FFA9>50%</color>",
            "고양이 코인 증가\n<color=#00FFA9>50%</color>",
        };

        _catSuppliesPriceData = new long[]
        {
            10000,      20000,      30000,     40000,       50000,      60000,
            120000,     220000,     320000,    420000,      520000,     6200000,
            1020000,    1320000,    1620000,   1920000,     2220000,    2520000,
            4000000,    6000000,    8000000,   10000000,    12000000,   14000000,

        };
    }
}
