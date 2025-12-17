using UnityEngine;

public class DeviceVariantSpawner : MonoBehaviour
{
    public enum DeviceKind { Phone, Tablet }

    [Header("Prefabs to Spawn (NO SetActive)")]
    [SerializeField] private GameObject phonePrefab;
    [SerializeField] private GameObject tabletPrefab;

    [Header("Spawn Options")]
    [SerializeField] private Transform spawnParent;         // 비우면 이 스크립트가 붙은 오브젝트 아래에 생성
    [SerializeField] private bool spawnAtThisTransform = true;

    [Header("Detection Tuning")]
    [SerializeField] private float tabletMinInches = 6.8f;  // 대충 6.8~7.0 이상이면 태블릿으로 보는 게 무난
    [SerializeField] private float fallbackAspectTabletMax = 1.55f; // DPI 못 믿을 때(0) 대비: 화면비가 너무 "덜 길쭉"하면 태블릿 취급

    [Header("Editor Override")]
    [SerializeField] private bool overrideInEditor = false;
    [SerializeField] private DeviceKind editorKind = DeviceKind.Phone;

    private GameObject _spawned;

    public DeviceKind Kind { get; private set; }

    private void Awake()
    {
        Kind = DetectDeviceKind();
        SpawnVariant(Kind);
    }

    private DeviceKind DetectDeviceKind()
    {
#if UNITY_EDITOR
        if (overrideInEditor) return editorKind;
#endif
        // 1) DPI 기반 대각선(inch) 계산 (가장 깔끔)
        float dpi = Screen.dpi;

        // 일부 기기에서 dpi가 0이거나 이상한 값이 들어오는 경우가 있어 fallback 준비
        if (dpi >= 50f && dpi <= 1000f)
        {
            float wInch = Screen.width / dpi;
            float hInch = Screen.height / dpi;
            float diagonal = Mathf.Sqrt(wInch * wInch + hInch * hInch);

            return (diagonal >= tabletMinInches) ? DeviceKind.Tablet : DeviceKind.Phone;
        }

        // 2) fallback: 화면비(가로/세로 or 세로/가로)로 대략 구분
        float aspect = (float)Mathf.Max(Screen.width, Screen.height) / Mathf.Min(Screen.width, Screen.height);
        // 폰은 보통 더 길쭉(1.7~2.3), 태블릿은 상대적으로 덜 길쭉(1.3~1.6)
        return (aspect <= fallbackAspectTabletMax) ? DeviceKind.Tablet : DeviceKind.Phone;
    }

    private void SpawnVariant(DeviceKind kind)
    {
        GameObject prefab = (kind == DeviceKind.Tablet) ? tabletPrefab : phonePrefab;
        if (prefab == null)
        {
            Debug.LogError($"[DeviceVariantSpawner] Prefab is missing for {kind}.");
            return;
        }

        Transform parent = spawnParent != null ? spawnParent : transform;

        Vector3 pos = spawnAtThisTransform ? transform.position : parent.position;
        Quaternion rot = spawnAtThisTransform ? transform.rotation : parent.rotation;

        _spawned = Instantiate(prefab, pos, rot, parent);
        _spawned.name = $"{prefab.name} (Spawned:{kind})";
    }

    public GameObject GetSpawnedInstance() => _spawned;

}
