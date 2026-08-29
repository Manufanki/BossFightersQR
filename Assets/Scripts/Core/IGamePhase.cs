// A single step of the 7-phase round loop, driven by PhaseStateMachine.
public interface IGamePhase
{
    GamePhase PhaseId { get; }
    bool IsComplete { get; }

    void Enter(GameManager gameManager);
    void Tick(GameManager gameManager);
    void Exit(GameManager gameManager);
}
