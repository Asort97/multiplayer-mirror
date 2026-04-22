using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ZoneUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private Image timerFill;

    private const string WaitingForMatchStartText = "Зона активируется после старта матча";

    private void Update()
    {
        var zone = ZoneManager.Instance;
        if (zone == null) return;

        if (MatchManager.Instance != null && !MatchManager.Instance.HasStarted)
        {
            if (timerText != null)
                timerText.text = WaitingForMatchStartText;

            if (phaseText != null)
                phaseText.text = "Фаза --/--";

            if (timerFill != null)
                timerFill.fillAmount = 0f;

            return;
        }

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
