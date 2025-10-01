using UnityEngine;

public class AnimationDestroyed : MonoBehaviour
{
    /// <summary>
    /// 애니메이션 Event에서 호출하면 자기 자신을 파괴합니다.
    /// </summary>
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}