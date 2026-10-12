# Weapon Pickup — Walkthrough & Test Setup

This document walks through, step by step, how a **weapon pickup** works in this
project, how to build the prefab, how to wire it into the `ApplicationController`
test-spawn system, and — most importantly — **what the debug logs will show** so
you can verify the intended behaviour before the real logic is implemented.

It also documents the collectable *design* (what SHOULD happen) with debug logs
as the verification, which is what is expected at this stage.

---

## 1. What is a weapon pickup?

A **weapon pickup** is a `Collectable` that, when the player touches it, grants
the player a specific **weapon** (a `SOWeaponSpecs` data asset).

There are two possible outcomes, and the outcome depends on *which weapon the
player is currently holding*:

| Situation | What would happen | Level behaviour |
|-----------|-------------------|-----------------|
| Picked-up weapon **== current** weapon | **Level up** the current weapon by 1 | Level **+1** (clamped to that weapon's max) |
| Picked-up weapon **!= current** weapon | **Switch** to the picked-up weapon | Level is **kept** (the player's *global* level, clamped to the new weapon's max) — it does **not** reset to 1 |

The key design point is that **the weapon level is GLOBAL to the player**, not
stored per weapon. So:

- Picking up the **same** weapon raises that global level.
- Picking up a **different** weapon switches to it **while keeping the same
  global level** (clamped to the new weapon's max), so your progress isn't lost.

The player's own number keys (`[1-5]` to switch, `E`/`Q` to level up/down) use
the *same* global level, so pickups and the temp keyboard controls stay
consistent with each other.

> **Status:** The actual switch/level-up calls are **not** implemented yet.
> `WeaponPickup.Collect()` and `PlayerController.Collect()` only **log what
> would happen**. That is the intended state right now — the logs are the
> verification that the data (which weapon, which level, which damage) flows
> correctly.

---

## 2. The classes involved

| Class | File | Role |
|-------|------|------|
| `Collectable` | `GameObjects/Collectable.cs` | Abstract base: owns the pickup ritual, pool recycling, and the player-touch handler. |
| `WeaponPickup` | `GameObjects/WeaponPickup.cs` | **Concrete** collectable that carries a `SOWeaponSpecs` payload and logs the level-up vs. switch outcome. |
| `PlayerController` | `PlayerController.cs` | The player side of the pickup: logs its current weapon/level when it touches a pickup. |
| `SOWeaponSpecs` | `ScriptableObjects/SOWeaponSpecs.cs` | The weapon data the pickup grants (name, damage table, fire interval, ...). |
| `ObjectPool` | `Logic/ObjectPool.cs` | Recycles the pickup prefab instead of destroying it. |

The flow when the player touches a pickup is:

```
Player touches pickup (colliders overlap)
   │
   ├─ SpaceObject.OnTriggerEnter2D fires on BOTH objects
   │
   ├─ Pickup side:  Collectable.OnCollision(player)
   │       ├─ player.Collect(this)   →  PlayerController.Collect (player-side log)
   │       └─ this.Collect()         →  WeaponPickup.Collect  (outcome log + pool Return)
   │
   └─ Player side:  PlayerController.OnCollision(pickup)  →  does nothing
                     (the pickup owns its own side — avoids a double pool-return)
```

---

## 3. Step-by-step: building the pickup prefab in Unity

Follow these steps in the Unity Editor.

### 3.1 Create the GameObject

1. In the **Hierarchy**, create a new empty GameObject and name it e.g.
   `WeaponPickup_MachineGun`.
2. Add a **Sprite Renderer** and assign a sprite (any icon — a gun, a star, etc.).
   This is what the player will see.
3. Add a **Box Collider 2D** (or Circle Collider 2D).
4. **Important:** on the collider, tick **Is Trigger** — pickups are picked up
   on contact (a trigger), not by physically pushing things around.

### 3.2 Attach the script

5. Add a **Weapon Pickup** component to the GameObject (Add Component →
   `WeaponPickup`).
6. In the **Payload → Weapon Specs** field, drag in the weapon this pickup
   represents — e.g. `Assets/ScriptableObjects/Weapons/MachineGun`.

   This single field is the whole "identity" of the pickup. The rest of the
   class is generic.

### 3.3 Save it as a Prefab

7. Drag the GameObject from the Hierarchy into a folder in the Project window
   (e.g. `Assets/Prefabs/Collectable/`). Unity saves it as `WeaponPickup_MachineGun.prefab`.
8. Delete the GameObject from the Hierarchy (keep the prefab in the Project).

### 3.4 Make one pickup per weapon

Repeat 3.1–3.3 for each weapon you want to test, each with its own weapon
asset, e.g.:

- `WeaponPickup_MachineGun`  → `MachineGun.asset`
- `WeaponPickup_Plasma`      → `Plasma.asset`
- `WeaponPickup_Spread`      → `Spread.asset`
- `WeaponPickup_Orbital`     → `Orbital.asset`

You now have one prefab per weapon type.

> **Why one prefab per weapon?** Each pickup needs to point at a *different*
> `SOWeaponSpecs`, and the data lives on the prefab. This is the same
> data-driven approach the enemies use (each enemy prefab carries its own
> movement asset).

---

## 4. Wire the pickups into the object pool

Pickups are recycled through the `ObjectPool`, so each pickup prefab must be
registered in the pool config asset.

1. Select **Assets/Resources/PoolSpaceObjects** in the Project window.
2. In the Inspector, under **Pool Objects**, click the **+** button.
3. Assign one pickup prefab to `Prefab` and set an `Initial Size` (e.g. `5`).
4. Repeat for every pickup prefab.

> The `ApplicationController` reads this asset at boot (`InitObjectPool`) and
> pre-instantiates each entry. If a pickup prefab is **not** in this list,
> `ObjectPool.Get<Collectable>` returns `null` and the test spawn logs an error.

---

## 5. Wire the pickups into the ApplicationController test spawn

The `ApplicationController` already has the test-spawn plumbing. To include the
pickups:

1. Select the **ApplicationController** singleton in the Hierarchy
   (it's auto-created, named `(singleton) ApplicationController`).
2. Under **Test spawning**, in the **Test Collectable Prefabs** list, click **+**
   and assign each of your pickup prefabs (MachineGun pickup, Plasma pickup, ...).
3. Set **Collectable Spawn Position** (where the row of pickups starts, e.g.
   `0, 2.5`) and **Collectable Spacing** (horizontal gap between pickups, e.g.
   `1.5`).
4. Make sure **Enable Test Spawns** is ON.

At game start, `SpawnTestCollectables()` pulls one of each pickup **from the
pool** and lays them out in a horizontal row. Fly the ship into any of them to
trigger the pickup.

---

## 6. What the debug logs will show

When the player touches a pickup, **two** log groups are printed (one from the
player, one from the pickup). Here is exactly what to expect.

### 6.1 Picking up the SAME weapon (level up)

Say the player is holding **MachineGun at level 2** and touches a
**MachineGun** pickup:

```
[Player] Picked up collectable: WeaponPickup (payload: LEVEL UP MachineGun from Lv.2 to Lv.3)
[Player] My current weapon: MachineGun Lv.2 → picked up: MachineGun
[WeaponPickup] ════════════════════════════════════════════
[WeaponPickup] Collected weapon pickup: 'MachineGun'
[WeaponPickup] Same weapon as current → would LEVEL UP
[WeaponPickup]   Weapon:   MachineGun
[WeaponPickup]   Level:    2 → 3
[WeaponPickup]   Damage:   14 → 19
[WeaponPickup]   (Future: player.IncreaseLevel() or player.SetLevel(3))
[WeaponPickup] Recycling pickup to pool.
[WeaponPickup] ════════════════════════════════════════════
```

If the pickup is at **max level**, the log shows the clamp instead of a bump:

```
[WeaponPickup] Same weapon as current → would LEVEL UP
[WeaponPickup]   Level:    5 → 5  (MAX — clamped)
```

### 6.2 Picking up a DIFFERENT weapon (switch, keep level)

Say the player is holding **MachineGun at level 3** and touches a **Plasma**
pickup:

```
[Player] Picked up collectable: WeaponPickup (payload: SWITCH to Plasma at Lv.3 (keeping global level 3))
[Player] My current weapon: MachineGun Lv.3 → picked up: Plasma
[WeaponPickup] ════════════════════════════════════════════
[WeaponPickup] Collected weapon pickup: 'Plasma'
[WeaponPickup] Different weapon → would SWITCH (keep global level)
[WeaponPickup]   Current:  MachineGun Lv.3
[WeaponPickup]   New:      Plasma Lv.3
[WeaponPickup]   Damage:   25
[WeaponPickup]   Fire interval: 0.25s
[WeaponPickup]   (Future: player.SetWeapon(weaponSpecs) — level is kept automatically)
[WeaponPickup] Recycling pickup to pool.
[WeaponPickup] ════════════════════════════════════════════
```

If the new weapon's `maxLevel` is **lower** than the player's global level, the
log shows the clamp:

```
[WeaponPickup]   New:      Plasma Lv.2  (clamped from 3)
```

### 6.3 Reading the logs

- **`[Player] ...`** lines = the player's perspective (what I had, what I picked up).
- **`[WeaponPickup] ...`** lines = the pickup's perspective (what WOULD happen,
  with the exact level and damage numbers, then the pool recycle).
- The damage numbers come straight from the weapon's `damagePerLevel` table via
  `SOWeaponSpecs.GetDamageForLevel(level)`, so the logs prove the level→damage
  lookup is wired correctly.
- **"Recycling pickup to pool"** confirms the object was returned to the
  `ObjectPool` (not destroyed).

---

## 7. How to test the level-up vs. switch behaviour

Use the temp keyboard controls together with the pickups:

1. **Start a game.** The player starts with its `defaultWeapon` (check the
   `PlayersShip` prefab) at level 1.
2. **Press `2`** (or any number) to switch to a weapon that matches one of the
   spawned pickups.
3. **Fly into the matching pickup** → you should see the **LEVEL UP** log.
4. **Press `E`** to level up a few more times, then fly into the matching
   pickup again → the level keeps climbing until it hits the weapon's max,
   after which the log shows **"(MAX — clamped)"**.
5. **Press a different number** to switch to another weapon (note: your level
   is *kept*). Then fly into *that* weapon's pickup → **LEVEL UP** again.
6. Now fly into a pickup for a weapon you are **not** holding → you should see
   the **SWITCH** log, and the level is carried over (or clamped down if the
   new weapon's max is lower).

Because the level is global, the number-key switches and the pickup switches
should always agree on the level value.

---

## 8. What changes when the real logic lands

When the feature is implemented (a later step), the only change is inside the
two log methods:

- In `PlayerController.Collect`, where it currently says
  *"Later: apply the collectable's payload"*, the `if (collectable is
  WeaponPickup pickup)` block will call:
  ```csharp
  if (pickup.WeaponSpecs == CurrentWeapon?.Specs)
      IncreaseLevel();                       // same weapon → level up
  else
      SetWeapon(pickup.WeaponSpecs);         // different → switch (level kept)
  ```
- `WeaponPickup.Collect()` keeps its detailed log (now describing what *did*
  happen instead of what *would* happen) and its pool `Return`.

Everything else — the prefab, the pool entry, the test spawn, the collision
plumbing — already works and stays as-is.

---

## 9. Quick checklist

- [ ] Each weapon pickup prefab has: Sprite Renderer, Box/Circle Collider 2D
      (**Is Trigger ON**), and a `WeaponPickup` component with a `Weapon Specs`
      asset assigned.
- [ ] Each pickup prefab is a row in **Assets/Resources/PoolSpaceObjects**
      (so the pool can recycle it).
- [ ] The `ApplicationController` → **Test Collectable Prefabs** list has your
      pickup prefabs, and **Enable Test Spawns** is ON.
- [ ] Flying into a pickup prints the `[Player]` + `[WeaponPickup]` log groups
      shown in section 6.
- [ ] Same-weapon pickup → **LEVEL UP** log; different-weapon pickup →
      **SWITCH** log with the level carried over.
