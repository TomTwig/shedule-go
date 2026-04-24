using UnityEngine;
using TMPro;

public class CountdownActivator : MonoBehaviour
{
    [Header("Timer Settings")]
    public float startTime = 7f;

    [Header("UI")]
    public TextMeshProUGUI countdownText;

    [Header("Target")]
    public GameObject objectToActivate;

    private float currentTime;
    private bool isRunning = true;

    void Start()
    {
        currentTime = startTime;

        // Sicherheit: Objekt am Anfang deaktivieren
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(false);
        }

        UpdateText();
    }

    void Update()
    {
        if (!isRunning) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0)
        {
            currentTime = 0;
            isRunning = false;

            UpdateText();

            ActivateObject();

            gameObject.SetActive(false);
        }
        else
        {
            UpdateText();
        }
    }

    void UpdateText()
    {
        if (countdownText != null)
        {
            // Rundet auf ganze Sekunden (z.B. 6,5 → 7)
            countdownText.text = Mathf.Ceil(currentTime).ToString();
        }
    }

    void ActivateObject()
    {
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
        }
    }
}