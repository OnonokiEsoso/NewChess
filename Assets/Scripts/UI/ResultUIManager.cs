using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultUIManager : MonoBehaviour
{
    [Header("Game")]
    [SerializeField] private GameManager gameManager;

    [Header("Round Result")]
    [SerializeField] private GameObject roundResultPanel;
    [SerializeField] private TMP_Text roundTitleText;
    [SerializeField] private TMP_Text roundWinnerText;
    [SerializeField] private TMP_Text roundScoreText;
    [SerializeField] private TMP_Text economyText;
    [SerializeField] private Button continueButton;

    [Header("Match Result")]
    [SerializeField] private GameObject matchResultPanel;
    [SerializeField] private TMP_Text matchTitleText;
    [SerializeField] private TMP_Text matchWinnerText;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
    }

    private void OnEnable()
    {
        if (gameManager != null)
        {
            gameManager.PhaseChanged += HandlePhaseChanged;
            gameManager.RoundEnded += RefreshRoundResult;
            gameManager.MatchEnded += RefreshMatchResult;
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ContinueToNextRound);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartMatch);
        }
    }

    private void Start()
    {
        RefreshForCurrentPhase();
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.PhaseChanged -= HandlePhaseChanged;
            gameManager.RoundEnded -= RefreshRoundResult;
            gameManager.MatchEnded -= RefreshMatchResult;
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ContinueToNextRound);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartMatch);
        }
    }

    public void ContinueToNextRound()
    {
        gameManager?.ContinueToNextRound();
    }

    public void RestartMatch()
    {
        gameManager?.RestartMatch();
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        RefreshForCurrentPhase();
    }

    private void RefreshForCurrentPhase()
    {
        bool showRoundResult =
            gameManager != null && gameManager.CurrentPhase == GamePhase.RoundResult;
        bool showMatchResult =
            gameManager != null && gameManager.CurrentPhase == GamePhase.GameOver;

        if (roundResultPanel != null)
        {
            roundResultPanel.SetActive(showRoundResult);
        }

        if (matchResultPanel != null)
        {
            matchResultPanel.SetActive(showMatchResult);
        }

        if (showRoundResult)
        {
            RefreshRoundResult();
        }

        if (showMatchResult)
        {
            RefreshMatchResult();
        }
    }

    private void RefreshRoundResult()
    {
        if (gameManager == null)
        {
            return;
        }

        if (roundTitleText != null)
        {
            roundTitleText.text = $"Round {gameManager.LastCompletedRound} Result";
        }

        if (roundWinnerText != null)
        {
            roundWinnerText.text = $"Winner: {GetSideName(gameManager.LastRoundWinner)}";
        }

        if (roundScoreText != null)
        {
            roundScoreText.text =
                $"Score\nWhite {gameManager.GetState(PieceSide.Player).RoundWins}" +
                $" - {gameManager.GetState(PieceSide.Enemy).RoundWins} Black";
        }

        if (economyText != null)
        {
            economyText.text =
                "Next Round Points\n" +
                $"White: {gameManager.GetProjectedNextRoundPoints(PieceSide.Player)}pt\n" +
                $"Black: {gameManager.GetProjectedNextRoundPoints(PieceSide.Enemy)}pt";
        }
    }

    private void RefreshMatchResult()
    {
        if (gameManager == null)
        {
            return;
        }

        if (matchTitleText != null)
        {
            matchTitleText.text = "Match Over";
        }

        if (matchWinnerText != null)
        {
            matchWinnerText.text = $"Winner: {GetSideName(gameManager.MatchWinner)}";
        }

        if (finalScoreText != null)
        {
            finalScoreText.text =
                "Final Score\n" +
                $"White {gameManager.GetState(PieceSide.Player).RoundWins}" +
                $" - {gameManager.GetState(PieceSide.Enemy).RoundWins} Black";
        }
    }

    private static string GetSideName(PieceSide side)
    {
        return side == PieceSide.Player ? "White" : "Black";
    }
}
