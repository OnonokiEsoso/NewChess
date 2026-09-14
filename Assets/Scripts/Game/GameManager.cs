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
    [Header("White")]
    [SerializeField] private bool whiteRandomCpu;

    [Header("Black")]
    [SerializeField] private bool blackRandomCpu = true;

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

        Debug.Log(
            $"Round {currentRound} start | White: {whiteState.CurrentPoints}pt | Black: {blackState.CurrentPoints}pt"
        );
    }

    public void EndRound(PieceSide winner)
    {
        if (IsMatchOver)
        {
            return;
        }

        GetState(winner).AddRoundWin();

        whiteState.StoreCarryOver(maxCarryOverPoints);
        blackState.StoreCarryOver(maxCarryOverPoints);

        Debug.Log(
            $"Round {currentRound} winner: {winner} | Score White {whiteState.RoundWins} - {blackState.RoundWins} Black"
        );

        if (whiteState.RoundWins >= roundsToWin || blackState.RoundWins >= roundsToWin)
        {
            Debug.Log($"Match Over! Winner: {winner}");
            return;
        }

        currentRound++;

        if (currentRound <= maxRounds)
        {
            StartCurrentRound();
        }
    }
}
