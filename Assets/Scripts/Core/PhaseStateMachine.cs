using System;
using System.Collections.Generic;

// Cycles through an ordered list of IGamePhase, looping back to the first phase after the
// last one completes, and incrementing Round each time the loop wraps.
public class PhaseStateMachine
{
    private readonly List<IGamePhase> _phases;
    private int _currentIndex;

    public IGamePhase CurrentPhase => _phases[_currentIndex];
    public int Round { get; private set; } = 1;

    public event Action<IGamePhase> OnPhaseEntered;
    public event Action<int> OnRoundChanged;

    public PhaseStateMachine(List<IGamePhase> phases)
    {
        if (phases == null || phases.Count == 0)
            throw new ArgumentException("PhaseStateMachine requires at least one phase.", nameof(phases));

        _phases = phases;
    }

    public void Start(GameManager gameManager)
    {
        _currentIndex = 0;
        Round = 1;
        OnRoundChanged?.Invoke(Round);
        EnterCurrentPhase(gameManager);
    }

    public void Tick(GameManager gameManager)
    {
        CurrentPhase.Tick(gameManager);

        if (CurrentPhase.IsComplete)
            Advance(gameManager);
    }

    private void Advance(GameManager gameManager)
    {
        CurrentPhase.Exit(gameManager);

        bool wrapsToNewRound = _currentIndex == _phases.Count - 1;
        _currentIndex = (_currentIndex + 1) % _phases.Count;

        if (wrapsToNewRound)
        {
            Round++;
            OnRoundChanged?.Invoke(Round);
        }

        EnterCurrentPhase(gameManager);
    }

    private void EnterCurrentPhase(GameManager gameManager)
    {
        CurrentPhase.Enter(gameManager);
        OnPhaseEntered?.Invoke(CurrentPhase);
    }
}
