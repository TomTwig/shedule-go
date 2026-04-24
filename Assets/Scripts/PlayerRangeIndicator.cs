using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Draws a visible range ring around the player and shows/hides the
/// sell button when a ProximityTarget enters or leaves range.
/// UI elements are assigned via Inspector references — create them
/// manually in the Canvas hierarchy and drag them in.
/// </summary>
public class PlayerRangeIndicator : MonoBehaviour
{
    [SerializeField] public float rangeMetres = 100f;
    [SerializeField] private float rangeStep  = 10f;
    [SerializeField] private int   segments   = 64;
    [SerializeField] private Color ringColor  = new Color(1f, 0.85f, 0f, 1f);
    [SerializeField] private float lineWidth  = 6f;

    [Header("UI References")]
    [Tooltip("Root GameObject of the sell button — will be shown/hidden.")]
    [SerializeField] private GameObject      sellButtonRoot;
    [Tooltip("Text field that displays the current radius value.")]
    [SerializeField] private TextMeshProUGUI rangeLabel;
    [Tooltip("Button that decreases the radius.")]
    [SerializeField] private Button          minusButton;
    [Tooltip("Button that increases the radius.")]
    [SerializeField] private Button          plusButton;

    private LineRenderer      ring;
    private float             lastRange;
    private ProximityTarget[] targets;

    private void Start()
    {
        BuildRing();

        targets   = FindObjectsByType<ProximityTarget>(FindObjectsSortMode.None);
        lastRange = rangeMetres;

        if (sellButtonRoot != null) sellButtonRoot.SetActive(false);

        if (minusButton != null)
            minusButton.onClick.AddListener(() =>
            {
                rangeMetres = Mathf.Max(10f, rangeMetres - rangeStep);
            });

        if (plusButton != null)
            plusButton.onClick.AddListener(() => rangeMetres += rangeStep);

        UpdateRangeLabel();
    }

    private void Update()
    {
        if (Mathf.Abs(rangeMetres - lastRange) > 0.01f)
        {
            lastRange = rangeMetres;
            UpdateRingGeometry();
            UpdateRangeLabel();
        }

        bool inRange = false;
        foreach (var t in targets)
        {
            if (t != null && t.DistanceToPlayer <= rangeMetres)
            {
                inRange = true;
                break;
            }
        }

        if (sellButtonRoot != null && sellButtonRoot.activeSelf != inRange)
            sellButtonRoot.SetActive(inRange);
    }

    private void BuildRing()
    {
        ring = gameObject.AddComponent<LineRenderer>();
        ring.loop              = true;
        ring.widthMultiplier   = lineWidth;
        ring.useWorldSpace     = false;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows    = false;

        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Sprites/Default")
                  ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", ringColor);
        else                               mat.color = ringColor;
        ring.material = mat;

        UpdateRingGeometry();
    }

    private void UpdateRingGeometry()
    {
        ring.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 2f * Mathf.PI / segments;
            ring.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * rangeMetres,
                1f,
                Mathf.Sin(angle) * rangeMetres));
        }
    }

    private void UpdateRangeLabel()
    {
        if (rangeLabel != null)
            rangeLabel.text = $"Radius\n{rangeMetres:F0} m";
    }
}
