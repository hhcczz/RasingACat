using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전구 버튼으로 여는 5단계 튜토리얼 패널(버튼 1개 + 나가기).
/// - GameObject 활성/비활성 제어
/// - primaryButton: 단계별 라벨/동작 변경
///   (Intro: 넘기기 → 1~4: 완료하기 → 5: 튜토리얼 종료 → End: 종료하기)
/// - exitButton: 패널만 닫기(진행도 유지, 다음에 다시 열면 이어서 표시)
/// - 완료 시 전구 제거(1회성), PlayerPrefs로 진행도/완료 저장
/// - 보상은 GiveSpecialCoinReward(step) 스텁으로 남김(나중에 실제 지급 로직 연결)
/// </summary>
public class TutorialPanelController : MonoBehaviour
{
    [Header("Entry")]
    [Tooltip("튜토리얼 해보기 전구 버튼(종료 시 제거)")]
    public GameObject bulbButtonGO;

    [Header("Panel Root")]
    [Tooltip("튜토리얼 패널 루트(전체 컨테이너)")]
    public GameObject tutorialPanelRoot;

    [Header("Texts")]
    public Text titleText;        // 상단 타이틀/단계명
    public Text bodyText;         // 본문 안내

    [Header("Primary Button (단계 진행 1개)")]
    public Button primaryButton;      // 단계 진행용 단일 버튼
    public Text primaryButtonLabel;

    [Header("Exit Button (패널만 닫기)")]
    public Button exitButton;         // 언제든 패널만 닫기

    [Header("Optional Visual Steps (True/False)")]
    [Tooltip("인덱스 매핑: 0=Intro, 1~5=단계, 6=End (비워도 동작)")]
    public GameObject[] visualSteps;

    // 내부 상태
    private const int INTRO = 0;
    private const int LAST_STEP = 5;  // 튜토리얼 마지막 단계(5)
    private const int END_SCREEN = 6; // 완료 후 엔드 문구
    private int currentStep = 0;      // 0=Intro, 1~5=튜토리얼 단계, 6=EndScreen

    // PlayerPrefs 키
    private const string PP_TUTORIAL_DONE = "pp.tutorial.done.once_1btn";
    private const string PP_TUTORIAL_PROGRESS = "pp.tutorial.progress_1btn"; // 0~6

#if UNITY_EDITOR
    [Header("Editor Only")]
    [Tooltip("에디터에서 플레이할 때마다 저장 초기화(개발 편의용)")]
    public bool resetOnPlayInEditor = false;
#endif

    private void Awake()
    {
#if UNITY_EDITOR
        if (resetOnPlayInEditor)
        {
            PlayerPrefs.DeleteKey(PP_TUTORIAL_PROGRESS);
            PlayerPrefs.DeleteKey(PP_TUTORIAL_DONE);
            PlayerPrefs.Save();
            Debug.Log("[Tutorial] Reset on Play (Editor option) → cleared PlayerPrefs.");
        }
#endif
        bool done = PlayerPrefs.GetInt(PP_TUTORIAL_DONE, 0) == 1;

        // 완료했으면 전구 제거, 패널 꺼두기
        SafeSetActive(bulbButtonGO, !done);
        SafeSetActive(tutorialPanelRoot, false);

        // 미완료 상태라면 저장된 진행도 로드 (없으면 0: Intro)
        if (!done)
            currentStep = Mathf.Clamp(PlayerPrefs.GetInt(PP_TUTORIAL_PROGRESS, 0), INTRO, END_SCREEN);

        // 버튼 이벤트 바인딩
        if (primaryButton)
        {
            primaryButton.onClick.RemoveAllListeners();
            primaryButton.onClick.AddListener(OnPrimaryButtonClicked);
        }
        if (exitButton)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExitClicked);
        }
    }

    // 전구 버튼 OnClick에서 연결
    public void OpenTutorialPanelFromBulb()
    {
        int done = PlayerPrefs.GetInt(PP_TUTORIAL_DONE, 0);
        int saved = PlayerPrefs.GetInt(PP_TUTORIAL_PROGRESS, 0);
        Debug.Log($"[Tutorial] Open requested. DONE={done}, savedStep={saved}");

        if (done == 1)
        {
            Debug.LogWarning("[Tutorial] Already done → bulb hidden, panel not opening.");
            return;
        }

        // 저장된 단계로 열기
        currentStep = Mathf.Clamp(saved, INTRO, END_SCREEN);
        SafeSetActive(tutorialPanelRoot, true);
        Debug.Log($"[Tutorial] Panel opened. currentStep={currentStep}");
        RenderStep();
    }

    // ─────────────────────────────────────────
    // 버튼 핸들러
    // ─────────────────────────────────────────
    private void OnPrimaryButtonClicked()
    {
        // 단계별 동작
        if (currentStep == INTRO)
        {
            // Intro → Step1
            currentStep = 1;
            SaveProgress();
            RenderStep();
            return;
        }

        if (currentStep >= 1 && currentStep <= 4)
        {
            // Step1~4: 완료하기
            GiveSpecialCoinReward(currentStep); // TODO: 실제 지급 로직 연결
            currentStep++; // 다음 단계
            SaveProgress();
            RenderStep();
            return;
        }

        if (currentStep == LAST_STEP)
        {
            GiveSpecialCoinReward(currentStep);
            // 완료는 아직 찍지 않는다. END 화면으로만 이동
            currentStep = END_SCREEN;
            SaveProgress();      // END_SCREEN 저장 → 나가기 눌러도 다시 열면 END 화면으로 복귀
            RenderStep();
            return;
        }

        if (currentStep == END_SCREEN)
        {
            // 여기서만 최종 완료로 마킹
            MarkTutorialDone();
            SafeSetActive(tutorialPanelRoot, false);
            SafeSetActive(bulbButtonGO, false);
            return;
        }
    }

    private void OnExitClicked()
    {
        // 패널만 닫기 (진행도 유지)
        SafeSetActive(tutorialPanelRoot, false);
        Debug.Log($"[Tutorial] Exit clicked. Keep progress at step={currentStep}");
        // 다음에 전구를 누르면 이어서 currentStep으로 Render됨
    }

    // ─────────────────────────────────────────
    // 렌더링
    // ─────────────────────────────────────────
    private void RenderStep()
    {
        Debug.Log($"[Tutorial] RenderStep: {currentStep}");
        SafeSetActive(tutorialPanelRoot, true);

        switch (currentStep)
        {
            case INTRO: // 0
                SetTexts(
                    "튜토리얼 안내",
                    "튜토리얼은 총 5단계로 이루어져있습니다.\n\n아래 버튼을 눌러 시작하세요."
                );
                SetPrimaryLabel("넘기기");
                break;

            case 1:
                SetTexts(
                    "1단계 : 고양이 소환해보기",
                    "중앙에 있는 물고기 통을 <color=yellow>클릭</color>해\n\n고양이를 한 번 소환해보세요."
                );
                SetPrimaryLabel("완료하기");
                break;

            case 2:
                SetTexts(
                    "2단계 : 고양이 5마리 소환해보기",
                    "중앙에 있는 물고기 통을 클릭해\n\n고양이 <color=yellow>5마리</color>를 한 번 소환해보세요."
                );
                SetPrimaryLabel("완료하기");
                break;

            case 3:
                SetTexts(
                    "3단계 : 고양이 합성해보기",
                    "고양이 한 마리를 들어서 다른 고양이 위에 올려보세요.\n\n" +
                    "<color=yellow>삼색 고양이</color>가 나오면 성공입니다."
                );
                SetPrimaryLabel("완료하기");
                break;

            case 4:
                SetTexts(
                    "4단계 : 고양이 코인 업그레이드 해보기",
                    "<size=45>핸드폰 클릭 → 고양이 헬스장 클릭 → <color=yellow>고양이 코인 업그레이드</color> 클릭</size>"
                );
                SetPrimaryLabel("완료하기");
                break;

            case 5:
                SetTexts(
                    "5단계 : 갈색 고양이 소환해보기",
                    "고양이를 합성하다 보면 갈색 고양이가 나옵니다.\n\n" +
                    "<color=yellow>갈색 고양이</color>를 획득해보세요!"
                );
                SetPrimaryLabel("튜토리얼 종료");
                break;

            case END_SCREEN:
                SetTexts(
                    "튜토리얼 완료!",
                    "이제부터 다양한 고양이를 획득해\n\n<color=yellow>고양이 도감</color>을 전부 채워보세요!" +
                    "\n버튼을 누르면 튜토리얼이 종료됩니다!"
                );
                SetPrimaryLabel("종료하기");
                break;
        }

        // 단계별 비주얼 오브젝트 토글(선택)
        if (visualSteps != null && visualSteps.Length > 0)
        {
            for (int i = 0; i < visualSteps.Length; i++)
                SafeSetActive(visualSteps[i], i == currentStep);
        }
    }

    // ─────────────────────────────────────────
    // 저장/유틸
    // ─────────────────────────────────────────
    private void SaveProgress()
    {
        PlayerPrefs.SetInt(PP_TUTORIAL_PROGRESS, currentStep);
        PlayerPrefs.Save();
        Debug.Log($"[Tutorial] Progress saved: step={currentStep}");
    }

    private void MarkTutorialDone()
    {
        PlayerPrefs.SetInt(PP_TUTORIAL_DONE, 1);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] Marked as DONE (one-time).");
    }

    private void SetTexts(string title, string body)
    {
        if (titleText) titleText.text = title;
        if (bodyText) bodyText.text = body;
    }

    private void SetPrimaryLabel(string label)
    {
        if (primaryButtonLabel) primaryButtonLabel.text = label;
    }

    private void SafeSetActive(GameObject go, bool active)
    {
        if (!go) return;
        if (go.activeSelf != active) go.SetActive(active);
    }

    /// <summary>
    /// 특별 코인 지급(스텁). 나중에 실제 지급 로직으로 교체하세요.
    /// </summary>
    private void GiveSpecialCoinReward(int step)
    {
        // TODO: CatCoinManager.Instance.AddCatCoin(amount);
        Debug.Log($"[Tutorial/Reward] Step{step} 완료 보상 지급(스텁).");
    }

    // (선택) 에디터 메뉴에서 진행도/완료 플래그 초기화
    [ContextMenu("Reset Tutorial Progress (Not Done)")]
    private void ResetProgressNotDone()
    {
        PlayerPrefs.DeleteKey(PP_TUTORIAL_PROGRESS);
        PlayerPrefs.DeleteKey(PP_TUTORIAL_DONE);
        PlayerPrefs.Save();
        currentStep = INTRO;
        SafeSetActive(bulbButtonGO, true);
        SafeSetActive(tutorialPanelRoot, false);
        Debug.Log("[Tutorial] ResetProgressNotDone executed.");
    }
}
