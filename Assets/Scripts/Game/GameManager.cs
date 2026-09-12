using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("White")]
    [SerializeField] private bool whiteRandomCpu;

    [Header("Black")]
    [SerializeField] private bool blackRandomCpu = true;

    [Header("CPU Settings")]
    [SerializeField] private float cpuMoveDelay = 0.6f;

    public float CpuMoveDelay => cpuMoveDelay;

    public bool IsRandomCpu(PieceSide side)
    {
        return side == PieceSide.Player
            ? whiteRandomCpu
            : blackRandomCpu;
    }
}
