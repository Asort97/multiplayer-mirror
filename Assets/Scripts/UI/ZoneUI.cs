using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ZoneUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private Image timerFill;

    private void Update()
    {
        var zone = ZoneManager.Instance;
        if (zone == null) return;

        float timer = zone.PhaseTimer;
        int minutes = Mathf.FloorToInt(timer / 60f);
        int seconds = Mathf.FloorToInt(timer % 60f);

        if (timerText != null)
        {
            if (zone.IsShrinking)
                timerText.text = $"Зона сужается {minutes:00}:{seconds:00}";
            else
                timerText.text = $"Сужение через {minutes:00}:{seconds:00}";
        }

        if (phaseText != null)
        {
            int phase = zone.CurrentPhase + 1;
            int total = zone.TotalPhases;
            if (zone.CurrentPhase >= total)
                phaseText.text = "Финальная зона";
            else
                phaseText.text = $"Фаза {phase}/{total}";
        }

        if (timerFill != null)
        {
            float duration = zone.CurrentPhaseDuration;
            timerFill.fillAmount = duration > 0f ? zone.PhaseTimer / duration : 0f;
        }
    }
}
