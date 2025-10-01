using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OpenPhone : MonoBehaviour
{
    [Tooltip("휴대폰 열기 버튼")]
    public Button Open_Phone;

    [Tooltip("폰 화면")]
    public GameObject Phone;
    // Start is called before the first frame update
    void Start()
    {
        Open_Phone.onClick.AddListener(Open);
    }

    /// <summary>
    /// 휴대폰 화면 열기
    /// </summary>
    private void Open()
    {
        Phone.SetActive(true);
    }
}
