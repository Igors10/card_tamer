# Ability System

How abilities work in Cardboarded and how to add new ones.

---

## The short version

An ability is **one ScriptableObject** (`AbilityObj`). Inside it there's an **Effects** list. Each entry is
a small C# class (an `AbilityEffect`) that you pick from a dropdown and set up in the Inspector.

You don't need a prefab, and you don't need to attach or wire up components.

```
Ability: Sting (AbilityObj)
  power: 5
  abilityDescription: Knocks out itself at the end of battle
  effects:
    ▼ Knock Self Out Effect        [Knock Self Out Effect ▾]
        trigger: OnOwnLineResolved
        customText: knocks itself out
        showPresentation: ✓
        oncePerRound: ☐
```

An ability can have **any number of effects**. For example, "+1 power on play" plus "knock self out after battle"
is just two entries in the list.

---

## Files

All of these are in `Assets/Scripts/Abilities/`:

| File | What it does |
|---|---|
| `AbilityEffect.cs` | Base class for every effect. Also defines the `EffectTrigger` enum. |
| `EffectContext.cs` | What an effect receives when it runs: who owns it, the opponent, fields, and helpers like `ChooseTarget`. |
| `AbilityRunner.cs` | Listens to game events and queues the matching effects. Created by `ExecuteManager`. |
| `EffectPresenter.cs` | Default juice played around every effect: sound, skill text, pop, VFX, pauses. |
| `EffectPickerAttribute.cs` + `Editor/EffectPickerDrawer.cs` | The dropdown in the Inspector that lists every effect class. |
| `Effects/*.cs` | The actual effects, one class per file. |

---

## How an effect gets triggered (the flow)

1. Something happens in the game, and `GameManager` fires one of its events:
   - `OnCardPlayed` is fired from `ExecuteManager.CardUseAbility`, after the entrance animation.
   - `OnCardPlayResolved` is fired from `ExecuteManager.CardUseAbility` once the played card's own OnPlay effects
     (like rolling) are resolved, so its starting power is final.
   - `OnDiceRolled` is fired from `D6.RollAnimation`.
   - `OnLineResolved` is fired from `BattleManager.InitBattleLine`, after the power counters resolve.
   - `Unit.KnockOut` doesn't use an event. It asks `AbilityRunner.QueueKnockOutEffects` directly, because it needs
     to know right away if the knock out is prevented.
2. `AbilityRunner` hears the event. It goes through **every unit on the board**, then each of its abilities,
   then each effect, and checks:
   - Does the effect's `trigger` match this event?
   - Is the unit still alive? Knocked-out (`stunned`) units never trigger.
   - If `oncePerRound` is on, has it already been used this round?
   - For `OnOwnLineResolved`: is the unit on the line that was just resolved?
   - Does `CanTrigger(ctx)` return true?
3. If all checks pass, `effect.Run(ctx)` is added to the right stack in `ExecuteManager`:

   | Trigger | Stack | Who resolves it |
   |---|---|---|
   | `OnPlay`, `OnAnyCardPlayed`, `AfterAnyCardPlayed` | `effectStack` | `CardUseAbility` → `TriggerEffects()` |
   | `OnDiceRolled` | `rollEffectStack` | `Unit.RollPower` → `TriggerEffects("onRoll")` |
   | `OnOwnLineResolved` | `battleOverEffectStack` | `BattleManager` → `TriggerEffects("onBattleEnd")` |
   | `OnWouldBeKnockedOut` | a local list | `Unit.KnockOut`, before the unit is knocked out |

4. `TriggerEffects` runs the queued effects **one at a time, in order**. Each `Run` does three things:
   `EffectPresenter.Show` → your `Execute` → `EffectPresenter.Hide`.
   Hide **always** runs, even if `Execute` stops early, so skill text never gets stuck on screen.

### Triggers

| Trigger | When |
|---|---|
| `OnPlay` | The card with this ability was just played. |
| `OnAnyCardPlayed` | Any card was played (yours or the opponent's, **including this one**) while this unit is on the board. Use `ctx.triggeringCard` to see which card. |
| `OnDiceRolled` | Any unit finished rolling a die. Use `ctx.triggeringCard` (who rolled) and `ctx.diceResult`. |
| `OnOwnLineResolved` | The battle on this unit's line just ended. |
| `AfterAnyCardPlayed` | Any card was played and its own OnPlay effects are done, so `ctx.triggeringCard.startingPower` is final (base power + rolled dice). Use this for "when a unit enters play with X power". |
| `OnWouldBeKnockedOut` | This unit is about to be knocked out. If one of the effects returns `PreventsKnockOut => true`, the unit stays. |

---

## Common settings (every effect has these)

| Field | Meaning |
|---|---|
| `trigger` | When the effect fires (see the table above). |
| `customText` | Text that pops up above the unit. Leave it empty to show the ability description instead. Rich text tags like `<color=red>+3</color>` work. |
| `showPresentation` | Turn this off for silent or passive effects (no sound, text, pop or pause). |
| `oncePerRound` | The effect fires at most once per round for each unit. |

---

## Adding a new ability

### Case A: an effect that already exists
1. Right-click in the Project window → **Create → Ability**.
2. Set `power` and `abilityDescription`.
3. In **Effects**, press **+**, then **pick the effect type from the dropdown** and set its fields.
4. Add the ability to `BasicAbilities` (in `ScrObjects/List objects`) so it can show up in the workshop.

> ⚠ After pressing **+**, always choose the type from the dropdown, even if the new element already shows one.
> Unity sometimes copies the previous element *by reference*, which means two entries would share the same settings.
> Picking from the dropdown always creates a fresh copy.

### Case B: you need a new effect
Create a file in `Assets/Scripts/Abilities/Effects/`. Here's a complete example:

```csharp
using System.Collections;

/// <summary>
/// Heals the owner when played
/// </summary>
[System.Serializable]                       // required, otherwise it won't show up / save
public class HealOwnerEffect : AbilityEffect
{
    public int amount = 1;                  // public fields show up in the Inspector

    protected override IEnumerator Execute(EffectContext ctx)
    {
        ctx.Owner.health += amount;
        ctx.Owner.playerUI.Refresh();
        yield break;                        // needed when nothing is waited on
    }
}
```

That's all. Once the code compiles, **Heal Owner Effect** shows up in the dropdown.

Rules:
- The class needs `[System.Serializable]` and a parameterless constructor (the default one is fine).
- Don't keep runtime state in fields like `bool used`. One AbilityObj is **shared by every card** that has the
  ability, so all of those cards would share that bool. Use `ctx.memory` instead (see below).
- To change the default trigger for a new effect, set it in a constructor:
  ```csharp
  public KnockSelfOutEffect() { trigger = EffectTrigger.OnOwnLineResolved; }
  ```

### Skipping when there's nothing to do: `CanTrigger`
If the effect shouldn't fire at all, override `CanTrigger`. When it returns false, the effect is skipped entirely
and no text or VFX is shown:

```csharp
public override bool CanTrigger(EffectContext ctx) => !ctx.Fields.IsFieldEmpty(ctx.OpposingField);
```

Conditions like "when a [4] is rolled by an opponent" also go here:

```csharp
public override bool CanTrigger(EffectContext ctx) =>
    ctx.diceResult == neededNumber && ctx.triggeringCard.player == ctx.Opponent;
```

`yield break` inside `Execute` is also safe. The presentation still closes properly.

---

## EffectContext cheat sheet

| Member | What it is |
|---|---|
| `ctx.self` | The unit that owns this effect. |
| `ctx.ability` | The `Ability` component (gives you `abilityData` and `abilityColor`). |
| `ctx.triggeringCard` | The card that caused the trigger (the played card, or the one that rolled). Can be null. |
| `ctx.diceResult` | The rolled number (only for `OnDiceRolled`). |
| `ctx.Owner` / `ctx.Opponent` | The players. |
| `ctx.Fields` | Shortcut to `FieldManager`. |
| `ctx.FieldID` | Index (0-3) of the unit's line. |
| `ctx.OwnField` / `ctx.OpposingField` | This unit's field / the enemy field on the same line. |
| `ctx.IsOnCurrentBattleLine` | True while the battle is on this unit's line. |
| `ctx.EnemyUnits` / `ctx.AllyUnits` / `ctx.GetUnits(TargetSide)` | Lists of units on the board. |
| `yield return ctx.ChooseTarget(list, hint)` then `ctx.chosenTarget` | Highlights the options and waits for the player to click one (the AI picks randomly). `chosenTarget` is null if the list was empty. |
| `ctx.memory` | A `Dictionary<string, int>` for this unit + effect. Cleared at the end of each round. |

Example of using memory, for a "buff once when the field becomes empty" effect:
```csharp
bool applied = ctx.memory.ContainsKey("applied");
if (!applied) { ctx.self.card.GainPower(2); ctx.memory["applied"] = 1; }
```

### Useful game functions to call from effects
- `unit.card.GainPower(amount)`: change power (use a negative number for a debuff). Refreshes visuals and field colors.
- `yield return unit.KnockOut()`: knock a unit out and wait for it. To not wait, use
  `unit.StartCoroutine(unit.KnockOut())`.
- `yield return ctx.self.RollPower()`: roll the unit's die (this also fires `OnDiceRolled` effects).
- `ctx.Fields.MoveUnit(unit, field, slot)`: move a unit (slot 0 = front, 1 = back).
- `field.roundsBlocked += n; field.RefreshFieldVisuals();`: block a field.
- `player.TakeDamage(n)`: damage a player.

---

## Juice / presentation

Everything that happens around an effect is in `EffectPresenter.cs`:
- `Show`: plays `EffectSFX`, brings the unit to the front, shows the skill text, pops the sprite, plays `cardEffectVFX`,
  then waits `PauseBeforeEffect` (1s).
- `Hide`: waits `PauseAfterEffect` (1s), turns off the VFX, fades the text and puts the unit back in its normal layer.

Change it here and every ability that uses the new system gets the change.

Juice that belongs to one specific effect (a special particle, a sound) goes inside that effect's `Execute`,
e.g. `ParticleManager.instance.SpawnVFX(...)` or `AudioManager.instance.PlaySFX(...)`.

---

## Effects that exist right now

| Effect | Used by | Fields |
|---|---|---|
| `RollPowerEffect` | Roll | none. Rolls the die and gains the rolled power. |
| `PushOpposingEnemyEffect` | Avalanche | none. Pushes the front enemy on the opposing field to a free adjacent field (random side if both are free). |
| `ModifyTargetPowerEffect` | DebuffTarget | `targets` (Enemies/Allies/Anyone), `amount`, `targetHint` |
| `KnockSelfOutEffect` | Sting | none. Default trigger is `OnOwnLineResolved`. |
| `SpawnedWithPowerEffect` | Fangs, Turbo | `neededPower`, `spawnedBy` (Enemies/Allies/Anyone), `receiver` (SpawnedUnit/Self), `amount`. Default trigger is `AfterAnyCardPlayed`. Ignores this unit entering play. |
| `BlockOpposingFieldEffect` | Inkstain | `rounds`. Blocks the opposing enemy field. |
| `PreventKnockOutEffect` | Exoskeleton | none. Default trigger is `OnWouldBeKnockedOut`. The presentation plays every time a knock out is blocked. |
