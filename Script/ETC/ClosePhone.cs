using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClosePhone : MonoBehaviour
{
    [Tooltip("ÈÞ´ëÆù ´Ý±â ¹öÆ°")]
    public Button Close_Phone;

    [Tooltip("Æù È­¸é")]
    public GameObject Phone;
    // Start is called before the first frame update
    void Start()
    {
        Close_Phone.onClick.AddListener(Close);
    }

    /// <summary>
    /// ÈÞ´ëÆù È­¸é ´Ý±â
    /// </summary>
    private void Close()
    {
        Phone.SetActive(false);
    }
}
