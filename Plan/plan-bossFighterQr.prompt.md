# Plan: Boss Fighter QR — Game Logic Foundation

Build the game loop, boss system, and card system for a QR-code-driven board game companion. Uses a **state machine** for the 7-phase loop, **ScriptableObjects** for data-driven boss/card definitions, and **C# events** for reactive triggers. The existing `QRCodeReader` gets extended with an event that feeds into the game loop.

---

### Folder Structure
```
Assets/Scripts/
  Core/         GameManager, PhaseStateMachine, IGamePhase, enums
  Core/Phases/  7 concrete phase classes
  Boss/         BossData (SO), BossController (runtime)
  Cards/        CardData (SO), CardRegistry (QR→card lookup)
  UI/           GameUI (phase display, HP bar, shields, action log)
Assets/Data/
  Cards/        CardData .asset files
  Bosses/       BossData .asset files
```

---

### Steps

**Phase A — Enums & Data Layer** *(no dependencies)*

1. Create `Enums.cs` — `GamePhase` (7 values), `DamageType` (Melee/Ranged/Magic), `StatusEffectType` (None/Poison), `HeroType`, `ClassType` as extensible placeholders
2. Create `CardData.cs` (ScriptableObject) — `cardName`, `qrId`, `heroType`, `classType`, `damageType`, `damage`, `damageEnhancement`, `shield`
3. Create `BossData.cs` (ScriptableObject) — `bossName`, `maxHP`, `initialShields` (per damage type), plus serializable lists for: `BossAttack` (damage, type, status effect), `BossReaction` (damage threshold → retaliation), `BossHPTrigger` (HP threshold → attack bonus), `BossTimeTrigger` (round → damage to all), `BossShieldTrigger` (shield type → damage on destroy)

**Phase B — Game Loop State Machine** *depends on A*

4. Create `IGamePhase` interface — `Enter()`, `Tick()`, `Exit()`, `IsComplete`
5. Create 7 phase classes: `PlanningPhase` (boss picks attacks), `ShieldPhase` (assign shields), `ActionPhase` (QR scan active, manual end), `AttackPhase` (boss deals damage, auto), `StatusPhase` (tick effects, auto), `DropCardsPhase` (manual), `DrawCardsPhase` (manual)
6. Create `GameManager` — owns `BossController`, `CardRegistry`, round counter, `AdvancePhase()` cycling; fires `OnPhaseChanged` and `OnActionLog` events; checks win condition after each phase

**Phase C — Boss Runtime** *depends on A, parallel with D*

7. Create `BossController` (plain C# class) — runtime state (currentHP, shields, attackBonus), `TakeDamage()` that reduces shields first then HP, evaluates reactions/HP triggers/shield triggers, fires events (`OnHPChanged`, `OnShieldDestroyed`, `OnBossAttack`)

**Phase D — Card System & QR Integration** *depends on A, parallel with C*

8. Create `CardRegistry` — maps `qrId` strings to `CardData` via dictionary
9. Extend `QRCodeReader` — add `event Action<string> OnQRCodeScanned`, fire on decode with 2-second deduplication cooldown
10. Wire together: `GameManager` subscribes to QR event → looks up card → if in Action Phase, calls `BossController.TakeDamage()` and logs the result

**Phase E — Basic UI Toolkit** *depends on B, C*

11. Create `GameUI` — UI Toolkit canvas with boss name, HP bar (slider + text), shield values per type, current phase label, round counter, scrollable action log, "Next Phase" button for manual phases
12. Create `BossFight.unity` scene — Canvas + GameManager + QRCodeReader on single manager object

**Phase F — Test Data & Verification** *depends on all*

13. Create sample assets: 1 boss ("Goblin King", 50 HP, shields 5/3/4, 2 attacks, 1 reaction, 1 HP trigger, 1 time trigger, 1 shield trigger) + 3-4 test cards (melee/ranged/magic/shield)
14. Generate QR codes for test card IDs and run through a full round cycle

---

### Relevant Files
- `Assets/Scripts/QRCodeReader.cs` — add `OnQRCodeScanned` event + deduplication
- `Assets/Scripts/Core/Enums.cs` — new, all shared enums
- `Assets/Scripts/Core/IGamePhase.cs` — new, phase interface
- `Assets/Scripts/Core/Phases/*.cs` — new, 7 phase classes
- `Assets/Scripts/Core/GameManager.cs` — new, orchestrator
- `Assets/Scripts/Boss/BossData.cs` — new, boss definition SO with nested serializable trigger classes
- `Assets/Scripts/Boss/BossController.cs` — new, runtime boss logic + events
- `Assets/Scripts/Cards/CardData.cs` — new, card definition SO
- `Assets/Scripts/Cards/CardRegistry.cs` — new, QR→card lookup
- `Assets/Scripts/UI/GameUI.cs` — new, HUD

### Verification
1. Play → phases cycle through all 7 and loop, round counter increments
2. Scan test QR during Action Phase → boss takes correct shield/HP damage in UI + log
3. Scan QR outside Action Phase → ignored with log
4. Deal ≥ threshold damage → reaction retaliation logged
5. Boss HP drops below threshold → attack bonus increases
6. Reach trigger round → time-trigger damage logged
7. Destroy a shield → shield-trigger damage logged
8. Boss HP reaches 0 → game-over state

### Further Considerations
1. **Hero/Class enums** are left as placeholders — should I add specific values from your board game now, or leave extensible?
2. **Boss attack targeting** is uniform ("all players"). Could later add a targeting enum for specific-player attacks.
3. **Card combos** — currently one scan = one resolve. A "pending cards" buffer could enable multi-card combos later.
