using System.Collections.Generic;

// Owns the Action phase playing order: round-robin turns, interruption turns granted by
// extra-turn effects, and consuming player actions. The active player itself is tracked
// by GameManager; this class only decides who plays next.
public class ActionPhase : IGamePhase
{
    private readonly int _actionsPerPlayer;
    private readonly string _instructionText;
    private const string StartPlayerPrompt = "Choose a start player for this round.";
    private readonly IReadOnlyList<Player> _players;
    private int _roundRobinIndex;
    private Player _interruptionTarget;
    private Player _interruptionSource;

    public GamePhase PhaseId => GamePhase.Action;
    public bool IsComplete { get; private set; }
    public bool IsInterruptionActive => _interruptionTarget != null;

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
        _roundRobinIndex = 0;
        _interruptionTarget = null;
        _interruptionSource = null;

        foreach (Player player in _players)
            player.StartRound(_actionsPerPlayer);

        // Wait for the players to choose who starts; the notification closes on a panel click.
        gameManager.BeginActionPhase(StartPlayerPrompt);
    }

    public void Tick(GameManager gameManager)
    {
    }

    public void Exit(GameManager gameManager)
    {
    }

    public void PerformAction(GameManager gameManager)
    {
        PerformAction(gameManager, false);
    }

    // Consumes one action; grantExtraAction keeps the turn on the current player (Lightning).
    public void PerformAction(GameManager gameManager, bool grantExtraAction)
    {
        if (IsComplete)
            return;

        Player currentPlayer = gameManager.CurrentPlayer;
        currentPlayer.UseAction();
        gameManager.Log($"Player {currentPlayer.PlayerNumber} used an action ({currentPlayer.ActionsRemaining} left).");

        if (grantExtraAction && currentPlayer.HasActionsRemaining)
        {
            gameManager.Log($"Player {currentPlayer.PlayerNumber} takes an additional action ({currentPlayer.ActionsRemaining} left).");
            return;
        }

        AdvanceToNextPlayer(gameManager);
    }

    // Starts an interruption: the target gets one granted action and becomes active now.
    // The round-robin position is untouched; play resumes after the source later.
    public void BeginInterruption(GameManager gameManager, Player target, Player sourcePlayer)
    {
        _interruptionTarget = target;
        _interruptionSource = sourcePlayer;
        target.AddActions(1);
        gameManager.SetCurrentPlayer(target);
        gameManager.Log($"Player {target.PlayerNumber} takes an interruption turn.");
    }

    // Ends the interruption: consumes the target's granted action and returns control
    // to the source player, whose card can then finish resolving.
    public void CompleteInterruption(GameManager gameManager)
    {
        Player target = _interruptionTarget;
        _interruptionTarget = null;
        target.UseAction();
        gameManager.Log($"Player {target.PlayerNumber} finished their interruption action.");
        gameManager.SetCurrentPlayer(_interruptionSource);
    }

    private void AdvanceToNextPlayer(GameManager gameManager)
    {
        // During an interruption hand-back, the next player is the one after the source.
        int baseIndex = _interruptionSource != null ? IndexOf(_interruptionSource) : _roundRobinIndex;
        if (_interruptionSource != null && gameManager.CurrentPlayer == _interruptionSource)
            _interruptionSource = null;

        if (AllPlayersDone())
        {
            IsComplete = true;
            gameManager.Log("All players have used their actions.");
            return;
        }

        int next = baseIndex;
        do
        {
            next = (next + 1) % _players.Count;
        }
        while (!_players[next].HasActionsRemaining);

        _roundRobinIndex = next;
        gameManager.SetCurrentPlayer(_players[_roundRobinIndex]);
        gameManager.Log($"Player {_players[_roundRobinIndex].PlayerNumber}'s turn ({_players[_roundRobinIndex].ActionsRemaining} actions left).");
    }

    private int IndexOf(Player player)
    {
        for (int i = 0; i < _players.Count; i++)
        {
            if (_players[i] == player)
                return i;
        }
        return 0;
    }

    // Sets the player who starts the round (chosen by clicking their panel at phase start).
    public void SetStartingPlayer(GameManager gameManager, Player player)
    {
        _roundRobinIndex = IndexOf(player);
        gameManager.SetCurrentPlayer(player);
        gameManager.Log($"Player {player.PlayerNumber} starts the round ({player.ActionsRemaining} actions left).");
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
