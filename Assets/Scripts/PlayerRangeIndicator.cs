using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Draws a visible range ring around the player and shows/hides the
/// "Ernte verkaufen" button when a ProximityTarget enters or leaves range.
/// Debug +/- buttons let you adjust the radius directly on the device.
/// </summary>
public class PlayerRangeIndicator : MonoBehaviour
{
    [SerializeField] public  float rangeMetres   = 100f;
    [SerializeField] private float rangeStep     = 10f;
    [SerializeField] private int   segments      = 64;
    [SerializeField] private Color ringColor     = new Color(1f, 0.85f, 0f, 1f);
    [SerializeField] private float lineWidth     = 6f;

    private LineRenderer     ring;
    private Button           sellButton;
    private TextMeshProUGUI  rangeLabel;
    private float            lastRange;
    private ProximityTarget[] targets;

    private void Start()
    {
        BuildRing();
        BuildUI();
        targets   = FindObjectsByType<ProximityTarget>(FindObjectsSortMode.None);
        lastRange = rangeMetres;
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

        if (sellButton != null && sellButton.gameObject.activeSelf != inRange)
            sellButton.gameObject.SetActive(inRange);
    }

    // -------------------------------------------------------------------------
    // Ring
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // UI — sell button + debug radius controls
    // -------------------------------------------------------------------------

    private void BuildUI()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        BuildSellButton(canvas);
        BuildDebugControls(canvas);
    }

    private void BuildSellButton(Canvas canvas)
    {
        var btnGo = new GameObject("SellHarvestButton");
        btnGo.transform.SetParent(canvas.transform, false);

        var rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0.5f, 0f);
        rect.anchorMax        = new Vector2(0.5f, 0f);
        rect.pivot            = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 60f);
        rect.sizeDelta        = new Vector2(350f, 90f);

        btnGo.AddComponent<Image>().color = new Color(0.13f, 0.6f, 0.13f);
        sellButton = btnGo.AddComponent<Button>();

        AddLabel(btnGo, "Ernte verkaufen", 28f);
        btnGo.SetActive(false);
    }

    private void BuildDebugControls(Canvas canvas)
    {
        // Container anchored to top-right
        var container = new GameObject("DebugRangeControls");
        container.transform.SetParent(canvas.transform, false);

        var cr = container.AddComponent<RectTransform>();
        cr.anchorMin        = new Vector2(1f, 1f);
        cr.anchorMax        = new Vector2(1f, 1f);
        cr.pivot            = new Vector2(1f, 1f);
        cr.anchoredPosition = new Vector2(-10f, -10f);
        cr.sizeDelta        = new Vector2(180f, 120f);

        // Label showing current range
        var labelGo = new GameObject("RangeLabel");
        labelGo.transform.SetParent(container.transform, false);
        var lr = labelGo.AddComponent<RectTransform>();
        lr.anchorMin        = new Vector2(0f, 0.66f);
        lr.anchorMax        = new Vector2(1f, 1f);
        lr.offsetMin        = Vector2.zero;
        lr.offsetMax        = Vector2.zero;
        rangeLabel          = labelGo.AddComponent<TextMeshProUGUI>();
        rangeLabel.alignment = TextAlignmentOptions.Center;
        rangeLabel.color    = Color.white;
        rangeLabel.fontSize = 20f;

        // - button (left half)
        var minusGo = new GameObject("RangeMinus");
        minusGo.transform.SetParent(container.transform, false);
        var mr = minusGo.AddComponent<RectTransform>();
        mr.anchorMin        = new Vector2(0f, 0f);
        mr.anchorMax        = new Vector2(0.48f, 0.6f);
        mr.offsetMin        = Vector2.zero;
        mr.offsetMax        = Vector2.zero;
        minusGo.AddComponent<Image>().color = new Color(0.7f, 0.2f, 0.2f, 0.85f);
        var minusBtn = minusGo.AddComponent<Button>();
        minusBtn.onClick.AddListener(() => { rangeMetres = Mathf.Max(10f, rangeMetres - rangeStep); });
        AddLabel(minusGo, "−", 30f);

        // + button (right half)
        var plusGo = new GameObject("RangePlus");
        plusGo.transform.SetParent(container.transform, false);
        var pr = plusGo.AddComponent<RectTransform>();
        pr.anchorMin        = new Vector2(0.52f, 0f);
        pr.anchorMax        = new Vector2(1f,    0.6f);
        pr.offsetMin        = Vector2.zero;
        pr.offsetMax        = Vector2.zero;
        plusGo.AddComponent<Image>().color = new Color(0.2f, 0.55f, 0.2f, 0.85f);
        var plusBtn = plusGo.AddComponent<Button>();
        plusBtn.onClick.AddListener(() => { rangeMetres += rangeStep; });
        AddLabel(plusGo, "+", 30f);
    }

    private void UpdateRangeLabel()
    {
        if (rangeLabel != null)
            rangeLabel.text = $"Radius\n{rangeMetres:F0} m";
    }

    private static void AddLabel(GameObject parent, string text, float size)
    {
        var go   = new GameObject("Label");
        go.transform.SetParent(parent.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var tmp       = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        tmp.fontSize  = size;
    }
}
