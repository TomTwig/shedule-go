using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Draws a visible range ring around the player and fires a sell button
/// visible/hidden event whenever a ProximityTarget enters or leaves range.
/// </summary>
public class PlayerRangeIndicator : MonoBehaviour
{
    [SerializeField] public  float rangeMetres  = 100f;
    [SerializeField] private int   segments     = 64;
    [SerializeField] private Color ringColor    = new Color(1f, 0.85f, 0f, 1f);
    [SerializeField] private float lineWidth    = 6f;

    private LineRenderer  ring;
    private Button        sellButton;
    private float         lastRange;
    private ProximityTarget[] targets;

    private void Start()
    {
        BuildRing();
        BuildSellButton();
        targets   = FindObjectsByType<ProximityTarget>(FindObjectsSortMode.None);
        lastRange = rangeMetres;
    }

    private void Update()
    {
        if (Mathf.Abs(rangeMetres - lastRange) > 0.01f)
        {
            lastRange = rangeMetres;
            UpdateRingGeometry();
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

    private void BuildRing()
    {
        ring = gameObject.AddComponent<LineRenderer>();
        ring.loop             = true;
        ring.widthMultiplier  = lineWidth;
        ring.useWorldSpace    = false;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows   = false;

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

    private void BuildSellButton()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        var btnGo = new GameObject("SellHarvestButton");
        btnGo.transform.SetParent(canvas.transform, false);

        var rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0.5f, 0f);
        rect.anchorMax        = new Vector2(0.5f, 0f);
        rect.pivot            = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 60f);
        rect.sizeDelta        = new Vector2(350f, 90f);

        var img   = btnGo.AddComponent<Image>();
        img.color = new Color(0.13f, 0.6f, 0.13f);

        sellButton = btnGo.AddComponent<Button>();

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label       = labelGo.AddComponent<TextMeshProUGUI>();
        label.text      = "Ernte verkaufen";
        label.alignment = TextAlignmentOptions.Center;
        label.color     = Color.white;
        label.fontSize  = 28f;

        btnGo.SetActive(false);
    }
}
