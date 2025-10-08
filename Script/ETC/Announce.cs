using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Announce : MonoBehaviour
{
    [Header("UI")]
    public Text AnnounceText;

    [Header("설정")]
    [SerializeField] private float scrollSpeed = 100f;    // 텍스트 이동 속도(px/sec)
    [SerializeField] private float resetDelay = 1f;       // 문구 바뀔 때 잠시 멈추는 시간
    [SerializeField] private float startX = 1500f;         // 시작 위치
    [SerializeField] private float endX = -1500f;          // 끝 위치

    private string[] AnnounceDesc;
    private int currentIndex = 0;
    private RectTransform rect;

    void Start()
    {
        rect = AnnounceText.GetComponent<RectTransform>();

        AnnounceDesc = new string[]
        {
            "하루에 한 번 <color=cyan>필살 버프</color>를 이용해보세요. 매우 강력한 버프가 주어집니다.",
            "귀여운 고양이를 합성해 다른 고양이들을 획득해보세요.",
            "고양이 대박 합성에는 다른 이펙트가 출력됩니다. 운을 시험해보세요."
        };

        StartCoroutine(ScrollAnnounce());
    }

    private IEnumerator ScrollAnnounce()
    {
        while (true)
        {
            // 문구 세팅 및 시작 위치 리셋
            AnnounceText.text = AnnounceDesc[currentIndex];
            rect.anchoredPosition = new Vector2(startX, rect.anchoredPosition.y);

            // 왼쪽으로 이동
            while (rect.anchoredPosition.x > endX)
            {
                rect.anchoredPosition -= new Vector2(scrollSpeed * Time.deltaTime, 0);
                yield return null;
            }

            // 다음 문구로 전환
            currentIndex = (currentIndex + 1) % AnnounceDesc.Length;
            yield return new WaitForSeconds(resetDelay);
        }
    }
}