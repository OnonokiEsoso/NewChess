using System;
using UnityEngine;

[System.Serializable]
public class PlayerRoundState
{
    [SerializeField] private int roundWins;
    [SerializeField] private int currentPoints;
    [SerializeField] private int carryOverPoints;
    [SerializeField] private int nextRoundCaptureBonus;

    public int RoundWins => roundWins;
    public int CurrentPoints => currentPoints;
    public int CarryOverPoints => carryOverPoints;
    public int NextRoundCaptureBonus => nextRoundCaptureBonus;

    public void ResetAll()
    {
        roundWins = 0;
        currentPoints = 0;
        carryOverPoints = 0;
        nextRoundCaptureBonus = 0;
    }

    public void StartRound(int basePoints)
    {
        currentPoints = basePoints + carryOverPoints + nextRoundCaptureBonus;
        carryOverPoints = 0;
        nextRoundCaptureBonus = 0;
    }

    public bool TrySpendPoints(int amount)
    {
        if (amount < 0 || currentPoints < amount)
        {
            return false;
        }

        currentPoints -= amount;
        return true;
    }

    public void RefundPoints(int amount)
    {
        if (amount > 0)
        {
            currentPoints += amount;
        }
    }

    public void AddCaptureBonus(int amount)
    {
        if (amount > 0)
        {
            nextRoundCaptureBonus += amount;
        }
    }

    public void StoreCarryOver(int maxCarryOver)
    {
        carryOverPoints = Mathf.Clamp(currentPoints, 0, maxCarryOver);
    }

    public void AddRoundWin()
    {
        roundWins++;
    }
}

public class GameManager : MonoBehaviour
{
    [Header("Game Phase")]
    [SerializeField] private GamePhase currentPhase = GamePhase.Preparation;

    [Header("White")]
    [SerializeField] private bool whiteRandomCpu;

    [Header("Black")]
    [SerializeField] private bool blackRandomCpu;

    [Header("CPU Settings")]
    [SerializeField] private float cpuMoveDelay = 0.6f;

    [Header("Match Rules")]
    [SerializeField] private int maxRounds = 3;
    [SerializeField] private int roundsToWin = 2;

    [Header("Round Economy")]
    [SerializeField] private int basePointsPerRound = 30;
    [SerializeField] private int maxCarryOverPoints = 10;
    [SerializeField] private int captureBonusPerPiece = 1;

    [Header("Runtime State")]
    [SerializeField] private int currentRound = 1;
    [SerializeField] private PlayerRoundState whiteState = new PlayerRoundState();
    [SerializeField] private PlayerRoundState blackState = new PlayerRoundState();

    public float CpuMoveDelay => cpuMoveDelay;
    public int CurrentRound => currentRound;
    public int MaxRounds => maxRounds;
    public int RoundsToWin => roundsToWin;
    public int BasePointsPerRound => basePointsPerRound;
    public int MaxCarryOverPoints => maxCarryOverPoints;
    public int CaptureBonusPerPiece => captureBonusPerPiece;
    public GamePhase CurrentPhase => currentPhase;
    public event Action<GamePhase> PhaseChanged;

    public bool IsMatchOver =>
        whiteState.RoundWins >= roundsToWin ||
        blackState.RoundWins >= roundsToWin ||
        currentRound > maxRounds;

    private void Awake()
    {
        StartNewMatch();
    }

    public bool IsRandomCpu(PieceSide side)
    {
        return side == PieceSide.Player
            ? whiteRandomCpu
            : blackRandomCpu;
    }

    public PlayerRoundState GetState(PieceSide side)
    {
        return side == PieceSide.Player ? whiteState : blackState;
    }

    public int GetCurrentPoints(PieceSide side)
    {
        return GetState(side).CurrentPoints;
    }

    public bool TrySpendPoints(PieceSide side, int amount)
    {
        return GetState(side).TrySpendPoints(amount);
    }

    public void RefundPoints(PieceSide side, int amount)
    {
        GetState(side).RefundPoints(amount);
    }

    public void RegisterCapture(PieceSide capturingSide)
    {
        GetState(capturingSide).AddCaptureBonus(captureBonusPerPiece);
    }

    public void StartNewMatch()
    {
        currentRound = 1;
        whiteState.ResetAll();
        blackState.ResetAll();
        StartCurrentRound();
    }

    public void StartCurrentRound()
    {
        whiteState.StartRound(basePointsPerRound);
        blackState.StartRound(basePointsPerRound);
        SetPhase(GamePhase.Preparation);

        Debug.Log(
            $"Round {currentRound} start | White: {whiteState.CurrentPoints}pt | Black: {blackState.CurrentPoints}pt"
        );
    }

    public void StartBattle()
    {
        if (currentPhase == GamePhase.Preparation)
        {
            SetPhase(GamePhase.Battle);
        }
    }

    public void SetGameOver()
    {
        SetPhase(GamePhase.GameOver);
    }

    private void SetPhase(GamePhase phase)
    {
        if (currentPhase == phase)
        {
            return;
        }

        currentPhase = phase;
        PhaseChanged?.Invoke(currentPhase);
    }

    public void EndRound(PieceSide winner)
    {
        if (currentPhase != GamePhase.Battle || IsMatchOver)
        {
            return;
        }

        GetState(winner).AddRoundWin();

        // Whatever was deliberately left unspent during preparation can carry forward,
        // capped independently from capture bonuses.
        whiteState.StoreCarryOver(maxCarryOverPoints);
        blackState.StoreCarryOver(maxCarryOverPoints);

        Debug.Log(
            $"Round {currentRound} winner: {winner} | Score White {whiteState.RoundWins} - {blackState.RoundWins} Black"
        );

        bool reachedWinTarget =
            whiteState.RoundWins >= roundsToWin ||
            blackState.RoundWins >= roundsToWin;
        bool reachedRoundLimit = currentRound >= maxRounds;

        if (reachedWinTarget || reachedRoundLimit)
        {
            SetPhase(GamePhase.GameOver);
            PieceSide matchWinner = GetMatchWinner(winner);
            Debug.Log(
                $"Match Over! Winner: {matchWinner} | Final Score White {whiteState.RoundWins} - {blackState.RoundWins} Black"
            );
            return;
        }

        currentRound++;
        StartCurrentRound();
    }

    private PieceSide GetMatchWinner(PieceSide latestRoundWinner)
    {
        if (whiteState.RoundWins > blackState.RoundWins)
        {
            return PieceSide.Player;
        }

        if (blackState.RoundWins > whiteState.RoundWins)
        {
            return PieceSide.Enemy;
        }

        // A tie is not expected with the current best-of-three rules, but this keeps
        // the result deterministic if match settings are changed later.
        return latestRoundWinner;
    }
}
