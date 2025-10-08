using System;
using UnityEngine;
using UnityEngine.UI;

public class CheckInManager : MonoBehaviour
{
    // 28일 출석표
    private bool[] DoCheckIn;
    private int DayCount = 0; // 현재까지 수령한 일수(0-based로 i를 인자로 쓰기 좋음)

    // 서버 설정 전 임시 변수 TODO : 서버 연동되면 대체
    [Header("임시: 오늘 날짜 인덱스(0-base)")]
    public int _currntDay = 3; // ex) 0=1일차, 1=2일차 ...

    [Header("UI")]
    public GameObject CheckInPanel;
    public Button Btn_CheckInOpen;
    public Button Btn_CheckInOut;  // ← 닫기 버튼
    public Button Btn_CheckIn;     // ← 수령 버튼
    public Text Text_CheckIn;

    [Tooltip("블럭 배경(수령 전 가림) 길이=28")]
    public GameObject[] BlockImg;
    [Tooltip("수령 확인 문구/아이콘 (체크마크 등) 길이=28")]
    public GameObject[] Receipt;

    private void Start()
    {
        // 데이터 초기화
        if (DoCheckIn == null || DoCheckIn.Length != 28)
            DoCheckIn = new bool[28];

        // (선택) 첫 칸을 디폴트로 오픈하고 싶다면 사용. 확실치 않으면 주석 유지。
        // if (!DoCheckIn[0]) DoCheckIn[0] = true;  // ← 이건 "수령 완료"로 표시되어 버리니 주의

        // 버튼 연결
        if (Btn_CheckInOpen != null) Btn_CheckInOpen.onClick.AddListener(OpenCheckInPanel);
        if (Btn_CheckInOut != null) Btn_CheckInOut.onClick.AddListener(CloseCheckInPanel);
        if (Btn_CheckIn != null) Btn_CheckIn.onClick.AddListener(CheckIn);

        RefreshUI();
    }

    private void OpenCheckInPanel()
    {
        if (CheckInPanel) CheckInPanel.SetActive(true);
        GameManager.Instance.EnableBlock();
        RefreshUI();
    }

    private void CloseCheckInPanel()
    {
        if (CheckInPanel) CheckInPanel.SetActive(false);
        GameManager.Instance.DisableBlock();
    }

    /// <summary>
    /// i번째 출석 보상 수령 처리
    /// </summary>
    private void ReceiveCheckInGift(int i)
    {
        if (i < 0 || i >= DoCheckIn.Length) return;
        if (DoCheckIn[i]) return; // 이미 수령

        Give(i);                  // 보상 지급
        DoCheckIn[i] = true;      // 수령 완료
    }

    /// <summary>
    /// i번째 보상 실제 지급
    /// </summary>
    private void Give(int i = -1)
    {
        if (i < 0) return;

        switch (i)
        {
            case 0:
                CatCoinManager.Instance?.AddCatGoldCoin(3);
                break;
            case 1:
                CatCoinManager.Instance?.AddCatGoldCoin(3);
                break;
            // TODO: 나머지 2~27구간 보상 채워주세요.
            default:
                // 예시: 기본 보상
                CatCoinManager.Instance?.AddCatGoldCoin(1);
                break;
        }
    }

    /// <summary>
    /// 오늘자 출석 처리(현재 임시: _currntDay 사용)
    /// </summary>
    private void CheckIn()
    {
        // 오늘까지 도달한 일수 안에서 아직 수령하지 않은 가장 첫날 보상 찾기
        int target = -1;
        for (int i = 0; i <= _currntDay && i < DoCheckIn.Length; i++)
        {
            if (!DoCheckIn[i]) // 아직 수령 안 한 날 발견
            {
                target = i;
                break;
            }
        }

        if (target == -1)
        {
            // 받을 보상이 없음
            Debug.Log("수령할 출석 보상이 없습니다.");
            RefreshUI();
            return;
        }

        // 보상 지급
        ReceiveCheckInGift(target);

        // 누적 일수 갱신
        DayCount = Mathf.Max(DayCount, target + 1);

        RefreshUI();
    }

    private void RefreshUI()
    {
        for (int i = 0; i < DoCheckIn.Length; i++)
        {
            bool reached = (i <= _currntDay);     // 날짜 도달 여부
            bool received = DoCheckIn[i];          // 이미 수령했는지

            bool blockOn = (!reached) || received; // 도달X or 이미 받음 → Block True

            if (BlockImg != null && i < BlockImg.Length && BlockImg[i] != null)
                BlockImg[i].SetActive(blockOn);

            if (Receipt != null && i < Receipt.Length && Receipt[i] != null)
                Receipt[i].SetActive(received);
        }

        // 출석 가능한 날 있는지 검사
        bool hasClaimable = false;
        for (int i = 0; i <= _currntDay && i < DoCheckIn.Length; i++)
        {
            if (!DoCheckIn[i]) { hasClaimable = true; break; }
        }

        // 버튼 및 텍스트 갱신
        if (Btn_CheckIn != null) Btn_CheckIn.interactable = hasClaimable;

        if (Text_CheckIn != null)
        {
            if (hasClaimable)
            {
                Text_CheckIn.text = "출석하기";
                ColorUtility.TryParseHtmlString("#49FF00", out var green);
                Text_CheckIn.color = green;
            }
            else
            {
                Text_CheckIn.text = "출석완료";
                ColorUtility.TryParseHtmlString("#FF5665", out var red);
                Text_CheckIn.color = red;
            }
        }
    }

    // (선택) 개발 편의용: 강제로 특정 날짜를 체크 처리하고 UI 갱신
    [ContextMenu("Debug/Fill to Current Day")]
    private void DebugFillToCurrent()
    {
        for (int i = 0; i <= Mathf.Clamp(_currntDay, 0, 27); i++)
            DoCheckIn[i] = true;
        DayCount = Mathf.Clamp(_currntDay + 1, 0, 28);
        RefreshUI();
    }
}