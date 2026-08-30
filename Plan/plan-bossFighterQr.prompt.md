# Plan: Boss Fighter QR — Companion App for the Physical Board Game

## Overall Goal

Build a **digital companion app** for the Boss Fighters QR board game. The app drives the fight: it runs the 7-phase round loop, plans and executes boss attacks, manages boss shields, resolves scanned cards via QR codes, and tells the players exactly what to do at each step. It deliberately does **not** track hero health points — per the official rules, hero HP lives on physical Health counters because too many effects modify it; the app only instructs players how to adjust their counters.

The fight ends when the boss's life reaches 0 (players win) or any one hero's HP reaches 0 (players lose; handled physically, app only shows a defeat affordance).

---

## Core Design Decisions (from the rulebook)

1. **7 phases per round, in order**: Planning → Shield → Action → Attack → Status → Discard → Draw.
2. **Phase 2 (Shield) is skipped in round 1**; from round 2 on, the boss receives its shields.
3. **Action phase**: players take turns clockwise, each with 3 actions (dots above their portrait). Round 1 starts with a player chosen by tapping their portrait (future UI work).
4. **Every phase transition is confirmed** via an instruction popup (the rulebook's "check mark") — the app waits for acknowledgement before continuing. The Action phase popup only opens the phase; the phase ends when all actions are spent.
5. **Cards have 1+ effects, resolved in order**: Attack (Melee/Ranged/Magic), Support (only works if an Attack of the same damage type was already played this round by any player), Protection (reduces a chosen hero's boss attack value), Lightning (take 1 additional action), Heal, Draw, and Special (free-text star effect).
6. **Hero/Class gating**: a card may only be played by a player whose hero type and class type match the card; `All` is a wildcard. Rejected cards consume no action and do not pass the turn.
7. **Hero HP, hand cards, draw/discard piles, and status tokens are physical**. The app displays instructions for them (e.g. "draw 2 cards", "gain 8 HP") instead of simulating them.
8. **Boss abilities** come in 5 types: attacks, HP-threshold triggers, reactions to player actions, recurring (round-based) abilities, and shield effects. `BossData` models all five as serializable lists.
9. **Shields absorb typed damage first**; overflow hits boss HP. Shields refill each Shield phase from round 2 onward.

---

## Architecture (current)

```
Assets/Scripts/
  Core/
    Enums.cs              GamePhase, DamageType, StatusEffectType, HeroType, ClassType
    IGamePhase.cs         Phase contract: Enter/Tick/Exit/IsComplete
    PhaseStateMachine.cs  Ordered phase cycle + round counter + events
    GameDialogs.cs        Popup state (phase instructions + messages), survives UI reloads
    Player.cs             Serializable player: number, hero type, class type, action budget
    GameManager.cs        Orchestrator: wiring, phase side effects, card play, logging
    Phases/
      PopupPhase.cs       Base for acknowledgement-gated phases + 5 concrete phases
      ActionPhase.cs      Round-robin turn order over GameManager's player roster
  Boss/
    BossData.cs           SO: HP, shields, attacks, reactions, HP/time/shield triggers
    BossController.cs     Runtime boss state + TakeDamage + all trigger evaluation + events
  Cards/
    CardData.cs           SO: identity (name/description/qrId/hero/class) + ordered effects
    CardEffects.cs        Serializable effect classes: Attack/Support/Protection/Lightning/Heal/Draw/Special
    CardDatabase.cs       SO catalog of all CardData assets (Inspector-managed list)
    CardRegistry.cs       Runtime qrId → CardData dictionary built from the database
    CardEffectResolver.cs Resolves a card's effect list in order; one handler per effect type
  UI/
    GameHUD.cs            UI Toolkit HUD: boss HP/shields, planned attack, action dots,
                          card test field, phase + message popups
  QRCodeReader.cs         Webcam QR decoding (ZXing), dedup cooldown, toggleable preview/logs
Assets/Editor/
  CardDataEditor.cs       Custom CardData inspector: effect add/remove UI + QR scan-to-qrId
Assets/Data/
  Cards/                  CardData .assets + CardDatabase.asset
  Bosses/                 BossData .assets
```

### Design patterns in use
- **State machine** for the round loop (`IGamePhase` + `PhaseStateMachine`).
- **Data-driven design** via ScriptableObjects (`BossData`, `CardData`, `CardDatabase`).
- **C# events** for all reactive flows (boss triggers, phase changes, dialog requests, QR scans) — UI never polls game state.
- **Service extraction (SOLID)**: `GameManager` orchestrates only; dialog state lives in `GameDialogs`, effect rules in `CardEffectResolver`, lookups in `CardRegistry`. New effect types require one data class + one handler method.
- **Polymorphic serialized effects** via `[SerializeReference]`; the custom editor makes them authorable.

---

## Completed Milestones

- **A — Data layer**: enums, `CardData`, `BossData` with all five boss ability types.
- **B — Game loop**: 7-phase state machine, concrete popup-gated phases, round counter.
- **C — Boss runtime**: shields-first damage, reactions, HP/time/shield triggers, planned attacks.
- **D — Cards & QR**: database + registry, QR event with 2 s dedup, scan-to-play wiring, hero/class eligibility, ordered multi-effect resolution (attack/support rules implemented).
- **E — UI Toolkit**: HUD (HP bar, shields, planned attack), action dots, per-phase instruction popups with configurable text, generic message popup, manual phase button, QR test input.
- **F — Tooling**: custom `CardData` inspector with typed effect list editing and scan-to-assign QR ID.

---

## Next Steps (mapped to the rulebook)

1. **Player portraits row** — show all players, planned boss attack value per portrait, and per-player action dots; tap a portrait to pick the round's starting player.
2. **Per-player boss attacks** — boss attacks currently target "all"; add targeting so Protection can reduce a specific portrait's attack value.
3. **Protection target selection** — popup to choose the hero a Protection card shields.
4. **Status phase resolution** — report poison damage per token (tokens stay physical); model poison-on-boss from cards like Poison Blade.
5. **Discard/Draw phase instructions** — include hand-limit reminder text (hand limit = hero + class hand card values; needs those values on `Player`).
6. **Round-1 special case** — skip Shield phase in round 1 per the rules.
7. **Win/lose flow** — boss defeat screen; skull button for conceding when a hero drops to 0 HP.
8. **Sample content** — full training-fight boss ("Goblin King" stand-in for The Prince) and starter card set with QR codes.

### Explicitly out of scope (by design)
- Hero HP tracking in-app (physical counters per the rulebook).
- Hand/draw/discard pile simulation (physical cards; app only instructs).
- Undo (rulebook: no undo button, it would interfere with boss mechanics).

### Verification Checklist
1. Phases cycle 1→7 and loop; each non-Action phase blocks until its popup is closed; round counter increments.
2. Action popup dismisses without advancing; phase ends only when all players' actions are spent.
3. Valid QR scan in Action phase → effects resolve in order, boss UI updates, description popup pauses scanning until OK.
4. Support without prior matching attack → message popup, effect skipped.
5. Wrong hero/class → rejection popup, no action spent, no player switch.
6. QR scan outside Action phase → ignored with log.
7. Reaction / HP / time / shield triggers each log and fire their events at the right moment.
8. Boss HP reaches 0 → game-over state.
