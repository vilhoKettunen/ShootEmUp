# ShootEmUp — Project Code Overview & Gameplay Loop

This document walks through every script in the project, explains what it does,
and then ties everything together into a single narrative of the gameplay loop
— from the moment the game boots to a bullet leaving the screen and being
recycled.

---

## 1. High-level architecture

The project is a 2D vertical "shoot 'em up" built in Unity with C#. The code
is organised into a few clear layers:

```
                    ┌────────────────────────────────────────────┐
                    │              APPLICATION LAYER             │
                    │  ApplicationController  (singleton)        │
                    │  AudioController        (singleton)        │
                    │  UIController           (views)            │
                    └────────────────────────────────────────────┘
                                   │ owns / drives
                    ┌──────────────┴───────────────┐
                    │        GAME OBJECT LAYER      │
                    │  SpaceObject (abstract base)  │
                    │   ├─ Bullet                     │
                    │   ├─ Enemy (abstract)           │
                    │   │   ├─ EnemyVulture           │
                    │   │   ├─ EnemyFalcon            │
                    │   │   └─ TestDummy              │
                    │   ├─ PlayerController           │
                    │   ├─ Collectable (abstract)     │
                    │   └─ Obstacle                   │
                    └────────────────────────────────┘
                                   │ uses
                    ┌──────────────┴───────────────┐
                    │          LOGIC LAYER          │
                    │  ObjectPool     (recycling)   │
                    │  MovementPatterns (pure math) │
                    │  Weapon         (weapon state)│
                    └──────────────────────────────┘
                                   │ reads
                    ┌──────────────┴───────────────┐
                    │        DATA LAYER             │
                    │  SOWeaponSpecs    (asset)     │
                    │  SOMovementParameters (asset) │
                    │  SOPoolSpaceObjects (asset)   │
                    │  MusicPlayList    (asset)     │
                    └──────────────────────────────┘

                 SUPPORT: Helper/ (CameraHelper, SingletonMono, EasingFunctions)
```

Two ideas run through the whole codebase and are worth understanding before
reading any file:

1. **Data-driven behaviour.** *How something moves, how a weapon fires, what a
   bullet looks like* is stored in **ScriptableObjects** (data assets), not in
   hard-coded numbers. The same C# script can drive a Vulture, a Falcon, or any
   future enemy just by pointing it at a different data asset.
2. **Recycling over destroy/create.** Bullets, enemies and collectables are
   **pooled** (see `ObjectPool`). They are never `Destroy`ed — they are
   deactivated, reset, and reused. This avoids the GC spikes and allocation
   churn you get from instantiating a bullet every frame.

A third principle, applied to every collision handler: **"react to the other,
act on yourself."** When two objects touch, each object's own `OnCollision`
method decides what that contact means *for itself* — it never reaches in and
commands the other object. This keeps the collision logic local and prevents
double-processing.

---

## 2. File-by-file reference

### Application layer

| File | Class | Purpose |
|------|-------|---------|
| `ApplicationController.cs` | `ApplicationController` | The top-level singleton. Boots the UI, the player ship and the object pool, spawns the demo content, and bridges UI ↔ game (Start / Quit). |
| `AudioController.cs` | `AudioController` | Singleton that plays music and SFX through an `AudioMixer`, cross-fading two music sources. |
| `UIController.cs` | `UIController` | Instantiates and parents all UI views, and provides show/hide/query of views by type. |

### Game object layer

| File | Class | Purpose |
|------|-------|---------|
| `GameObjects/SpaceObject.cs` | `SpaceObject` (abstract) | Base for everything in the world. Owns active/alive state, the pool `Init`/`ResetObject` hooks, the shared `Die()` ritual, and the Unity trigger adapter that funnels into the virtual `OnCollision`. |
| `GameObjects/IDamageable.cs` | `IDamageable` (interface) | Capability marker: "this object *can* be damaged" (`TakeDamage`). Lets bullets target any damageable without a tag. |
| `GameObjects/Bullet.cs` | `Bullet` | A projectile for **both** player and enemies. Moves via `MovementPatterns`, routes damage by owner, and recycles itself when it hits a valid target, leaves the screen, or times out. |
| `GameObjects/Enemy.cs` | `Enemy` (abstract) | Base enemy: has health, a coroutine firing loop, and virtual `Move`. Recycles itself to the pool on death. |
| `GameObjects/EnemyVulture.cs` | `EnemyVulture` | Concrete enemy that drifts with a **Sine** wiggle (from its data asset). |
| `GameObjects/EnemyFalcon.cs` | `EnemyFalcon` | Concrete enemy that moves with an **Eased** approach (from its data asset). |
| `GameObjects/PlayerController.cs` | `PlayerController` | The player ship: input, weapon state, fire-rate timing, hit response and the test keyboard hooks. |
| `GameObjects/Collectable.cs` | `Collectable` (abstract) | Base for power-ups / drops. Handles the pickup ritual and pool recycling. |
| `GameObjects/Obstacle.cs` | `Obstacle` | Indestructible hazard. Passive — it never takes damage or commands others. |
| `GameObjects/TestDummy.cs` | `TestDummy` | Stationary **immortal** target for testing weapon damage, reporting HP to the HUD. |
| `GameObjects/PoolEntry.cs` | `PoolEntry` | One row in the pool config: a prefab + how many to pre-instantiate. |

### Logic layer

| File | Class | Purpose |
|------|-------|---------|
| `Logic/ObjectPool.cs` | `ObjectPool` | Generic, plain-C# pool keyed by prefab. `Register` / `Get<T>` / `Return<T>`. |
| `Logic/MovementPatterns.cs` | `MovementPatterns` (static) | Pure, stateless math for the four movement patterns (Straight, Sine, Target, Eased). |
| `Logic/Weapon.cs` | `Weapon` | Plain-C# weapon state: a `SOWeaponSpecs` data asset + the current level, exposing read-only firing values. |

### Data layer (ScriptableObjects)

| File | Class | Purpose |
|------|-------|---------|
| `ScriptableObjects/SOWeaponSpecs.cs` | `SOWeaponSpecs` | Defines a weapon: name, bullet prefab, bullet movement, sound, fire interval, max level, damage table. |
| `ScriptableObjects/SOMovementParameters.cs` | `SOMovementParameters` | Defines a movement: pattern, direction, speed, amplitude, frequency, duration, target offset. |
| `ScriptableObjects/SOPoolSpaceObjects.cs` | `SOPoolSpaceObjects` | The list of pool entries (which prefabs to pool, and how many). |
| `AudioScripts/MusicPlayList.cs` | `MusicPlayList` / `MusicTrack` | Named list of audio clips (soundtrack or SFX). |

### Support / helpers

| File | Class | Purpose |
|------|-------|---------|
| `Helper/SingletonMono.cs` | `SingletonMono<T>` | Thread-safe MonoBehaviour singleton that creates itself if missing and survives scene loads. |
| `Helper/CameraHelper.cs` | `CameraHelper` (static) | "Is this sprite on screen?" and "where to spawn across the top of the camera?" |
| `Helper/EassingFunctions.cs` | `Kryz.Tweening.EasingFunctions` (static) | Library of easing curves (linear, quad, cubic, sine, expo, circ, elastic, back, bounce) used by the `Eased` movement pattern. |

### UI

| File | Class | Purpose |
|------|-------|---------|
| `UI/UiElement.cs` | `UiElement`, `UiLayerRoot` | Describes a UI view + which canvas layer it belongs to. |
| `UI/UiView.cs` | `UiView` (base) | Show/hide machinery for any UI view, with an optional toggle child. |
| `UI/UiViewsList.cs` | `UiViewsList` | The list of views the `UIController` instantiates. |
| `UI/UiViewsListItem.cs` | `UiViewsListItem` | One entry: view prefab + its layer. |
| `UI/UiMainMenuButton.cs` | `UiMainMenuButton` | Tags a main-menu button with its purpose (NewGame, Quit, ...). |
| `UI/Views/UiViewHud.cs` | `UiViewHud` | In-game HUD: weapon name, level, HP. Exposed via a static `Instance`. |
| `UI/Views/UiViewMainMenu.cs` | `UiViewMainMenu` | Main menu: New Game and Quit buttons. |

---

## 3. How each system works

### 3.1 Bootstrapping (`ApplicationController`)

`ApplicationController` is a `SingletonMono<ApplicationController>`. Because of
`[RuntimeInitializeOnLoadMethod] OnInit()`, it forces its own `Instance` to be
created the moment the game starts (before scenes are even loaded), then calls
`Init()`:

1. Loads the `"UI"` prefab from `Resources` and instantiates it, then calls
   `UIController.Init()` — which in turn instantiates **every** view in the
   prefab's `UiViewsList` and parents each under its canvas layer.
2. Hides the HUD (it defaults to visible, and would otherwise show behind the
   main menu).
3. Instantiates the player ship from the `"PlayersShip"` resource.
4. Calls `InitObjectPool()`: loads the `SOPoolSpaceObjects` asset and, for each
   row, calls `ObjectPool.Register(prefab, initialSize)`.
5. If `enableTestSpawns` is on, calls `SpawnTestObjects()` to place the demo
   Vulture, Falcon and a test bullet.

`StartNewGame()` (called by the main menu) simply re-shows the HUD and pushes
the player's starting HP into it. `QuitGame()` stops play mode in the Editor
or exits the player build.

### 3.2 The object pool (`ObjectPool`)

`ObjectPool` is deliberately **plain C#** — no `MonoBehaviour`, no Unity
lifecycle. It holds two dictionaries:

- `pools`: `GameObject → Queue<SpaceObject>` (each prefab maps to a queue of
  ready instances).
- `objectPrefabs`: `SpaceObject → GameObject` (each instance maps back to the
  prefab it came from).

- **`Register(prefab, initialSize)`** — instantiates `initialSize` copies of the
  prefab, deactivates each, and enqueues them.
- **`Get<T>(prefab)`** — dequeues a ready instance (or instantiates a fresh one
  if the queue is empty), reactivates it, calls the object's `Init()` hook, and
  returns it typed as `T`.
- **`Return<T>(obj)`** — calls the object's `ResetObject()` hook, deactivates
  it, and enqueues it to the back.

Because the pool only ever talks to the abstract `SpaceObject` (through its
`Init`/`ResetObject` hooks), it never needs to know whether it's handing out a
bullet, an enemy or a collectable — adding a new poolable type is a **data**
change (a new row in `SOPoolSpaceObjects`), not a code change.

### 3.3 Movement (`MovementPatterns` + `SOMovementParameters`)

`SOMovementParameters` is pure data. It picks one of four patterns and holds
the numbers that pattern needs:

- **Straight** — constant velocity along `Direction`.
- **Sine** — straight motion plus a perpendicular sine wave (wiggle) defined by
  `Amplitude` and `Frequency`.
- **Target** — travel from spawn to `spawn + TargetOffset`, then hold.
- **Eased** — same as Target, but the progress is shaped by an easing curve
  (it uses `Kryz.Tweening.EasingFunctions.OutCubic`).

`MovementPatterns` is a **static, pure** class: given the asset, an elapsed
time `t`, and the spawn origin, it returns the current absolute position.
`GetPosition(asset, t, origin)` dispatches to the right function. Being pure
(no `MonoBehaviour`, no `Time`, no `Transform`), the same math is trivially
unit-testable and shared by bullets *and* enemies with zero duplication.

Objects (bullet, vulture, falcon) keep an `elapsed` counter and a cached
`spawnOrigin`, then each frame do a single line:

```csharp
transform.position = MovementPatterns.GetPosition(movementParameters, elapsed, spawnOrigin);
```

### 3.4 Weapons (`Weapon` + `SOWeaponSpecs`)

Two-layer separation:

- **`SOWeaponSpecs`** = *what the weapon is* (designer data): name, bullet
  prefab, bullet movement asset, shoot sound, fire interval, max level, and a
  per-level damage table (`GetDamageForLevel(level)`).
- **`Weapon`** = *where the player is on that weapon* (runtime state): the
  current level. It wraps a `SOWeaponSpecs` and a level, and exposes the
  read-only values the firing code needs (`FireInterval`, `CurrentDamage`,
  `BulletPrefab`, `SpawnOffset`, `ShootAudioClip`, ...).

The **level is global to the player**, not to a weapon. `PlayerController`
owns `currentLevel` and hands it to every `Weapon` it constructs, so switching
weapons keeps the player's level (clamped to the new weapon's max).

### 3.5 Collision & damage routing

Every object in the world derives from `SpaceObject`, which owns the **only**
Unity trigger adapter: `OnTriggerEnter2D`. It applies the shared guards (both
objects must be `IsActive` **and** `IsAlive`) and then calls the virtual
`OnCollision(other)`. Each subclass overrides `OnCollision` to decide what the
contact means **for itself**:

- **`Bullet.OnCollision`** — the heart of combat:
  - If it's a **player** shot, it damages any `IDamageable` that isn't the
    player (friendly-fire guard).
  - If it's an **enemy** shot, it damages the player only.
  - It ignores its own owner (so a bullet doesn't self-hit the frame it spawns).
  - Only when it hits a *valid* target does it return itself to the pool.
    Hitting a non-target (obstacle, own ship) lets it pass through.
- **`PlayerController.OnCollision`** — touching an `Enemy` or `Obstacle` deals
  contact damage to the player. It deliberately does *nothing* for collectables
  and bullets — those handle their own side (avoids a double pool-return).
- **`Collectable.OnCollision`** — if it touches the player, it tells the player
  `Collect(this)` (the player logs what it gained) and then recycles *itself*.
- **`Enemy.OnCollision` / `Obstacle.OnCollision`** — intentionally empty, because
  their hit response is driven by the other object's handler.

`IDamageable` is the capability interface that makes this clean: a bullet checks
`other is IDamageable d` and calls `d.TakeDamage(Damage)` — no tags, no
`switch` on type strings. `Obstacle` and `Collectable` simply don't implement
it, so bullets never try to damage them.

### 3.6 Firing: player vs enemy

**Player** (`PlayerController`): fire is *held* input. While the fire key is
down, `Update` calls `FireAtInterval()` every frame, and a `fireTimer` cooldown
gates it so the rate never exceeds the weapon's `FireInterval`. When the
cooldown expires, `FireCurrentWeapon()` pulls a bullet from the pool, places it
at the weapon's `SpawnOffset`, sets owner = player and damage = current level's
damage, and plays the shoot sound.

**Enemy** (`Enemy`): firing is a **coroutine loop** (`FireRoutine`).
`StartFiring()` starts it; it waits `fireInterval`, calls `Shoot()`, and repeats
while the enemy is alive, active and firing. `Shoot()` pulls a pooled bullet,
places it along the enemy's aim direction, and sets owner = the enemy.
`Die()` and `ResetObject()` both call `StopFiring()` so a dead/recycled enemy
never keeps a live coroutine (a classic Unity leak).

### 3.7 Death & recycling

- **`SpaceObject.Die()`** sets `IsAlive = false`. Subclasses extend it:
  - **Enemy** also returns itself to the pool.
  - **PlayerController** logs death (respawn / game-over are a later feature).
  - **TestDummy** overrides it to a no-op (it's immortal).
- **`SpaceObject.Despawn()`** is the non-pooled removal path (deactivate +
  `Destroy`).
- Pooled objects never `Destroy` — they go through the pool's `Return` →
  `ResetObject` → deactivate → enqueue cycle.

### 3.8 Audio (`AudioController`)

A singleton that loads an `AudioMixer` and two music `AudioSource`s plus an SFX
`AudioSource`. Music cross-fades by switching `AudioMixerSnapshot`s
(`music1FullVolume` / `music2FullVolume` / `noMusic`), so one track can fade out
while another fades in. Tracks are looked up **by name** from a `MusicPlayList`
asset. The game uses it today for the weapon shoot sound
(`AudioController.Instance.PlaySoundEffect(clip)`).

### 3.9 UI (`UIController`, `UiView`, views)

`UIController` instantiates every view in the `UiViewsList` and parents each
under its canvas layer. It provides:
- `ShowUiView(view)` — hide the others (or just the self-hiding ones), show one.
- `ShowUiView<T>()` / `GetUiView<T>()` — find a view by concrete type.
- `HideAllUiViews()`.

`UiView` is the show/hide base class. `UiViewHud` (weapon, level, HP) and
`UiViewMainMenu` (New Game / Quit) are the two concrete views. Game code pushes
HUD values via the static `UiViewHud.Instance` (`UpdateWeapon`, `UpdateLevel`,
`UpdateHealth`) — the HUD never polls the game.

---

## 4. The gameplay loop, end to end

Here is one complete "second" of gameplay, traced through the code:

**1. Boot (once).**
`ApplicationController.OnInit` → `Init` instantiates the UI (main menu visible,
HUD hidden), the player ship, and the pool (bullets, enemies, collectables
pre-instantiated and idle). If test spawns are on, a Vulture, a Falcon and a
test bullet appear on screen.

**2. Player starts a game.**
The main menu's New Game button (`UiViewMainMenu.HandleNewGameButton`) hides the
menu and calls `ApplicationController.StartNewGame()`, which re-shows the HUD
and pushes the player's HP into it.

**3. Player moves.**
`PlayerController.Update` reads the `Move` input action and translates the ship
by `input * moveSpeed * deltaTime`.

**4. Player fires.**
Holding the fire key sets `isFiringHeld`. Each frame `FireAtInterval()` ticks
the `fireTimer` down; when it hits zero it fires:
- `FireCurrentWeapon()` asks the **pool** for a `Bullet` (`ObjectPool.Get`).
- The bullet is placed at the weapon's `SpawnOffset` above the ship.
- `SetOwnerAndDamage(player, currentDamage)` marks it a *player* bullet and sets
  its damage from the weapon's level table.
- The shoot SFX plays.

**5. Bullet flies.**
`Bullet.Update` advances `elapsed` and sets its position via
`MovementPatterns.GetPosition(...)` (a single line, whatever pattern its asset
defines). It's checking every frame whether it left the camera view
(`CameraHelper.SpriteBoundsInCameraView`) or ran out of its `Duration` — if so it
recycles itself to the pool.

**6. Enemy moves and fires.**
In parallel, each enemy's `Update` moves it via the same `MovementPatterns`
machinery (Sine for the Vulture, Eased for the Falcon). Each enemy's
`FireRoutine` coroutine, on its own `fireInterval`, pulls a pooled bullet, sets
owner = enemy, and releases it downward.

**7. A hit happens.**
When a bullet's collider overlaps a target, Unity fires
`OnTriggerEnter2D` on **both** objects. The shared guard confirms both are active
and alive, then each side's `OnCollision` runs:
- A **player** bullet touching an `Enemy` → the bullet calls
  `enemy.TakeDamage(damage)` and returns to the pool.
- An **enemy** bullet touching the **player** → the bullet calls
  `player.TakeDamage(damage)` and returns to the pool.
- Contact with an `Enemy`/`Obstacle` → the **player's** own `OnCollision` deals
  contact damage to the player.

**8. An enemy dies.**
`Enemy.TakeDamage` decrements health; at 0 it calls `Die()`, which stops its
firing coroutine, marks it dead, and returns it to the pool (it will be
`ResetObject`ed — health restored, firing stopped — the next time it's `Get`
again). The HUD updates as damage values change.

**9. Player collects / takes damage / dies.**
Picking up a `Collectable` triggers the collectable's `OnCollision` → the player
logs the gain and the collectable recycles itself. The player's HP is pushed to
the HUD on every hit. At 0 HP the player's `Die()` runs (a clean extension point
for respawn / game-over).

**10. Recycle, repeat.**
Every bullet, enemy and collectable that leaves the play area is deactivated and
queued back into its pool instead of destroyed — so step 5/6 of the *next*
second reuses the very same GameObjects, keeping the frame rate smooth.

---

## 5. Where to extend the game

- **New enemy type** → add a `SOMovementParameters` asset, a prefab with the
  `Enemy` (or a new subclass) component, and a row in `SOPoolSpaceObjects`.
  No C# required.
- **New weapon** → add a `SOWeaponSpecs` asset and drop it into the player's
  `weaponSpecs` list (test hook `[1..5]`).
- **Real waves/spawning** → replace `SpawnTestObjects` with a spawner that
  uses `CameraHelper.GetSpawnPositionAbove` + the pool.
- **Respawn / lives / game-over** → build on the `Die()` hooks and the HUD.
- **Obstacle scrolling** → give `Obstacle.Move()` a movement asset like the
  other objects.
