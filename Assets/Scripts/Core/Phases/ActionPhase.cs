using System.Collections.Generic;

// Round-robin turn order for the Action Phase: each player takes one action, then play passes
// to the next player. The phase completes once every player has used all of their actions.
public class ActionPhase : IGamePhase
{
    private readonly int _actionsPerPlayer;
    private readonly string _instructionText;
    private readonly IReadOnlyList<Player> _players;
    private int _currentPlayerIndex;

    public GamePhase PhaseId => GamePhase.Action;
    public bool IsComplete { get; private set; }
    private Player PhaseCurrentPlayer => _players[_currentPlayerIndex];

    public ActionPhase(IReadOnlyList<Player> players, int actionsPerPlayer, string instructionText)
    {
        if (players == null || players.Count == 0)
            throw new System.ArgumentException("ActionPhase requires at least one player.", nameof(players));

        _actionsPerPlayer = actionsPerPlayer;
        _instructionText = instructionText;
        _players = players;
    }

    public void Enter(GameManager gameManager)
    {
        IsComplete = false;
        _currentPlayerIndex = 0;

        foreach (Player player in _players)
            player.ResetActions(_actionsPerPlayer);

        gameManager.SetCurrentPlayer(PhaseCurrentPlayer);
        gameManager.Log($"Entering phase: {PhaseId}. Player {PhaseCurrentPlayer.PlayerNumber}'s turn ({PhaseCurrentPlayer.ActionsRemaining} actions left).");
        gameManager.ShowPhasePopup(PhaseId, _instructionText);
    }

    public void Tick(GameManager gameManager)
    {
    }

    public void Exit(GameManager gameManager)
    {
    }

    // Consumes one action for the current player, then passes the turn to the next player with
    // actions remaining. Call this once per scanned card (or manual "Use Action" trigger).
    public void PerformAction(GameManager gameManager)
    {
        if (IsComplete)
            return;

        Player currentPlayer = gameManager.CurrentPlayer;
        currentPlayer.UseAction();
        gameManager.Log($"Player {currentPlayer.PlayerNumber} used an action ({currentPlayer.ActionsRemaining} left).");

        AdvanceToNextPlayer(gameManager);
    }

    private void AdvanceToNextPlayer(GameManager gameManager)
    {
        if (AllPlayersDone())
        {
            IsComplete = true;
            gameManager.Log("All players have used their actions.");
            return;
        }

        do
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
        }
        while (!PhaseCurrentPlayer.HasActionsRemaining);

        gameManager.SetCurrentPlayer(PhaseCurrentPlayer);
        gameManager.Log($"Player {PhaseCurrentPlayer.PlayerNumber}'s turn ({PhaseCurrentPlayer.ActionsRemaining} actions left).");
    }

    private bool AllPlayersDone()
    {
        foreach (Player player in _players)
        {
            if (player.HasActionsRemaining)
                return false;
        }

        return true;
    }
}
