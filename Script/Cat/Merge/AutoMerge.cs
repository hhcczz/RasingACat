using System.Collections;
using UnityEngine;

public class AutoMerge : MonoBehaviour
{
    public TossFish _tossfish;
    Coroutine autoMergeRoutine;

    /// <summary>
    /// 자동 합성 시작 (조건 충족시 호출)
    /// </summary>
    public void StartAutoMerge()
    {
        if (autoMergeRoutine == null)
            autoMergeRoutine = StartCoroutine(AutoMergeLoop());
    }

    /// <summary>
    /// 자동 합성 중단 (조건 깨질 때 호출)
    /// </summary>
    public void StopAutoMerge()
    {
        if (autoMergeRoutine != null)
        {
            StopCoroutine(autoMergeRoutine);
            autoMergeRoutine = null;
        }
    }

    private IEnumerator AutoMergeLoop()
    {
        while (true)
        {
            // 광고 자동합성 끝나면 종료
            if (GameManager.Instance.RemainingTime[1] < 1)
            {
                StopAutoMerge();
                yield break;
            }

            // 1) 합칠 수 있는 동안 연속으로 합성 (애니메이션 완료까지 기다렸다가 다음)
            int safety = 0;
            while (safety++ < 100)
            {
                // 이미 합성 중이면 애니 끝날 때까지 대기
                while (CatMergeManager.Instance.IsMerging)
                    yield return null;

                // 합성 시도 (없으면 break)
                bool started = CatMergeManager.Instance.ForceMerge();
                if (!started) break;

                // 합성 애니메이션이 끝나고 IsMerging=false 될 때까지 대기
                while (CatMergeManager.Instance.IsMerging)
                    yield return null;

                // 다음 연쇄 합성으로 바로 이어짐
                yield return null;
            }

            // 2) 더 이상 합칠 쌍이 없고 보드 여유 & 물고기 있으면 1마리 소환
            if (GameManager.Instance.HaveFishCount > 0 &&
                CatManager.Instance.catHaveCount < CatManager.Instance.catMaxCount)
            {
                _tossfish.RunTossFish(false, 0);
            }

            // 3) 다음 틱까지 대기
            yield return new WaitForSeconds(0.2f);
        }
    }
}