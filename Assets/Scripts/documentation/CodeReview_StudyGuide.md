# Code Review Study Guide — Design Principles

This document walks through the four graded pieces of code, explains **what each
method/field does**, **why it was designed that way**, and gives you **ready-made
explanations** for the code review, matched to the grading criteria.

---

## 1 / ObjectPool.cs — pooling, dictionaries, queues, generics, object lifecycle

### The big idea
In a space shooter, bullets are created and destroyed many times per second.
Calling `Instantiate`/`Destroy` constantly is expensive (allocation + GC).
A **pool** solves this: objects are created **once**, then **recycled**
(reactivated when needed, deactivated when done) instead of destroyed.

### The two data structures (the heart of the class)

```csharp
private readonly Dictionary<GameObject, Queue<SpaceObject>> pools;
private readonly Dictionary<SpaceObject, GameObject> objectPrefabs;
```

- **`pools`**: `prefab → queue of its free instances`.
  - `Dictionary` is used because lookups by prefab must be **O(1)** — we go
    straight from "I want a bullet" to "here is the bullet queue".
  - `Queue` (FIFO) is used for the free instances: we hand out the **oldest**
    free object (`Dequeue` = front) and put returned ones at the **back**
    (`Enqueue`). FIFO gives predictable, fair reuse order. A `List`/`Stack`
    would work too, but a queue reads naturally: "in the back, out the front".
- **`objectPrefabs`**: the **reverse map**, `instance → prefab`.
  - Needed for `Return<T>()`: when a bullet is returned, the pool must find
    *which* queue to put it back in. Without this map we'd have to scan every
    queue — with it, it's one O(1) lookup.

This is the answer to "why two dictionaries?": one map answers
*"which queue serves this prefab?"*, the other answers
*"which prefab did this instance come from?"* — both directions must be fast.

### The generic constraint `where T : SpaceObject`

```csharp
public T Get<T>(GameObject prefab) where T : SpaceObject
```

- `T` is a **static-type hint**: the caller says *"give me a Bullet"* (or Enemy,
  Collectable…). The pool itself only ever deals with the base type
  `SpaceObject` — it never references any concrete class.
- The constraint `where T : SpaceObject` guarantees at **compile time** that T
  is a SpaceObject-derived type, so `so.Init()` / `so.ResetObject()` always
  exist. You cannot call `Get<int>(...)` — the compiler rejects it.
- The pool is a plain C# class (no MonoBehaviour) — it's a **service**, owned by
  the `ApplicationController` singleton.

### Lifecycle — the full story of one pooled object

**Register** (once at start, per prefab):
1. Validate: the prefab's ROOT must have a `SpaceObject`-derived component,
   otherwise log an error and refuse (fail fast with a helpful message).
2. Check for duplicate registration (warn, don't double-create).
3. Instantiate `initialSize` (default 10) copies, `SetActive(false)` each,
   enqueue them, and record each in `objectPrefabs`.

**Get<T>** (take one out):
1. `TryGetValue` → find the queue (O(1)).
2. If queue not empty → `Dequeue()` a recycled instance.
3. If empty → **grow on demand**: instantiate a fresh one (the pool never
   starves the game), record it in `objectPrefabs`.
4. `SetActive(true)`, then call the object's **`Init()` hook**.
5. `T typed = so as T` — a safe downcast; if the prefab actually carries a
   different component, log a precise error instead of throwing.

**Return<T>** (give one back):
1. Reverse lookup `objectPrefabs` → find the right queue.
2. Call the object's **`ResetObject()` hook** (it cleans its own state).
3. `SetActive(false)`, then `Enqueue` to the back.

### Why the hooks `Init()` / `ResetObject()` instead of the pool reaching in?
The pool is **generic** — it has no idea what a Bullet or Enemy needs to
reset. So the *base class* `SpaceObject` defines two **virtual** hooks, and each
subclass overrides them to reset **its own** state (Bullet resets elapsed,
owner, damage…). This is the **Template Method** idea: the pool defines the
*lifecycle skeleton*, subclasses fill in the specifics. **Adding a new poolable
type requires zero changes to ObjectPool** — that's the extensibility point.

### Key C# concepts used (say these in review)
- `readonly` fields → the two dictionaries are fixed for the object's life.
- `TryGetValue` vs indexer → non-throwing lookup + explicit error message.
- Generic method with constraint → type-safe, compile-time-checked polymorphism.
- `as` + null check → safe downcast (unlike a hard cast which throws).
- Queue FIFO semantics → Dequeue front / Enqueue back.

### Talking points for the review
- "A dictionary gives O(1) lookup from prefab to queue; the second dictionary
  is the reverse map so Return() is also O(1)."
- "A queue fits because we reuse free objects in order; front = oldest free,
  back = most recently returned."
- "`Get<T>()` is generic so the pool stays type-agnostic; the constraint
  `T : SpaceObject` lets the pool call Init()/ResetObject() safely."
- "The pool grows dynamically when a queue is empty, so we never block the
  game, and it never needs a fixed maximum."
- "Init()/ResetObject() keep the lifecycle logic inside each type — the pool
  never touches subclass internals."

---

## 2 / MovementPatterns.cs — static methods, pure functions, parameterization

### The big idea
Movement math (straight, sine wiggle, move-to-target, eased) is **stateless
math**: give it parameters + time, get a position back. So it lives in a
**static class** with **pure functions** — no MonoBehaviour, no GameObjects,
no `Time.time` read internally. Only `Vector2` from UnityEngine, and even that
is just a math struct.

### Why pure functions matter
A **pure function** has no side effects and no hidden inputs — output depends
*only* on its arguments. Notice **time is passed in** as `float t`, never read
from `Time.time`. Consequences:
- **Deterministic & testable**: `Straight(p, 2f)` always returns the same
  vector — you can unit-test it in plain C# with no Unity editor.
- **Reusable**: enemies AND bullets call the *same* functions; the class "never
  knows which object asked".
- **No side effects**: the class never modifies any object; callers assign the
  returned value themselves.

### The parameter object: SOMovementParameters
Instead of many arguments (`direction, speed, amplitude, frequency, …`), all
movement data is packed in one **ScriptableObject data asset**:

```csharp
public static Vector2 Sine(SOMovementParameters p, float t)
```

- One asset per variant ("VultureMovement", "PlayerBullet_MG"…) → designers
  tune behavior in the **Inspector, not in code**.
- Same script, different data = different behavior → **variation lives in
  data, not in code branches** (no `if (enemy == vulture)` anywhere).
- Adding a new pattern = add one enum value + one static method; every caller
  gets it automatically via `GetPosition`.

### The four patterns, one by one

**Straight** — constant velocity:
```csharp
return p.Direction * (p.Speed * t);
```
A **delta** (offset from spawn): direction × speed × elapsed time.

**Sine** — straight motion + perpendicular wiggle:
```csharp
Vector2 perpendicular = new Vector2(-p.Direction.y, p.Direction.x);
float wave = p.Amplitude * MathF.Sin(2f * MathF.PI * p.Frequency * t);
return straight + perpendicular * wave;
```
- Rotating the direction vector 90° gives the perpendicular axis.
- `2π · Frequency · t` → Frequency is *full cycles per second*.
- **Backward compatible**: Amplitude = 0 degenerates exactly to Straight.

**MoveToTarget** — travel to a point, then hold:
```csharp
float travelTime = distance / p.Speed;
float k = Math.Clamp(t / travelTime, 0f, 1f);
return Vector2.Lerp(origin, target, k);
```
- `k` = progress 0→1; `Clamp` guarantees it never overshoots the destination.
- Returns an **absolute** position (includes origin).

**Eased** — same as Target but progress goes through an easing curve:
```csharp
float eased = Kryz.Tweening.EasingFunctions.OutCubic(k);
return Vector2.Lerp(origin, target, eased);
```
- This is the **consumer of the Kryzarel easing set**: `OutCubic` = starts
  fast, ends slow. Because easing maps 0→0 and 1→1, there's still no
  overshoot. Shows the class is **extensible** with the existing easing utility.

**GetPosition** — the dispatcher:
```csharp
switch (p.Pattern) { ... }
```
- One entry point for callers:
  `transform.position = MovementPatterns.GetPosition(so, elapsed, origin);`
- **Normalizes the two return conventions**: Straight/Sine return a *delta*
  (so it adds `origin`), Target/Eased return *absolute* (passed through).
  Callers never need to know which convention a pattern uses.

### The delta vs. absolute convention (worth mentioning in review)
Straight/Sine answer "how far from the start" — easy to compose mathematically.
Target/Eased answer "where am I" — natural for point-to-point travel.
`GetPosition` hides this difference so the outside world sees only absolute
positions.

### Key concepts (say these in review)
- **Static class** = namespace of related stateless functions; cannot be
  instantiated, no hidden state.
- **Pure functions** = same input → same output, no side effects, time
  injected → unit-testable without Unity.
- **Parameter object** (SOMovementParameters) = data-driven behavior,
  designer-tunable, eliminates per-behavior subclasses.
- **Dispatcher pattern** = one switch on an enum → easy to extend.
- **Easing** = remap linear progress through a curve (OutCubic: fast→slow).

---

## 3 / Weapon.cs + SOWeaponSpecs.cs — data vs. state, constructor, properties, visibility

### The core principle: separate *definition* from *runtime state*

| | SOWeaponSpecs (ScriptableObject) | Weapon (plain C# class) |
|---|---|---|
| What | **WHAT** the weapon is — static, designer-tuned, one asset per weapon type | **WHERE the player is** on that weapon — its current level |
| Who edits | Level designers in the Inspector | The game code at runtime |
| Lifetime | Asset, lives forever | Cheap, created/destroyed on every weapon switch |

Why two types? The level *changes*; the definition *doesn't*. If the level
lived inside the ScriptableObject, every player/instance would mutate the
**shared asset**. Keeping state in a separate plain class means the data asset
stays immutable and shared, while state is per-weapon-instance.

### SOWeaponSpecs — the data asset
- `[CreateAssetMenu(...)]` → right-click menu to create the asset.
- All fields `[SerializeField] private` — serialized for the Inspector but
  **not publicly writable**.
- Exposed through **get-only properties** (`=> field`):
  `WeaponName`, `BulletPrefab`, `BulletMovement`, `SpawnOffset`, `FireInterval`,
  `MaxLevel`, `BaseDamage`, `DamagePerLevel` — read-only from outside.
- One method, `GetDamageForLevel(int level)`:
  ```csharp
  int index = Mathf.Clamp(level, 1, table.Length) - 1;
  return table[index];
  ```
  1-based level → 0-based array index, **clamped** so out-of-range levels never
  throw. "Data and code never fight about edge cases."

### Weapon — the state wrapper
```csharp
private readonly SOWeaponSpecs specs;
private readonly int level;
```
- **Constructor** does the only mutation:
  ```csharp
  public Weapon(SOWeaponSpecs specs, int level = MinLevel)
  {
      this.specs = specs;
      this.level = Mathf.Clamp(level, MinLevel, specs.MaxLevel);
  }
  ```
  - `level = MinLevel` is an **optional parameter** → `new Weapon(specs)` works.
  - Clamping here means an invalid level is **rejected at the door** — after
    construction the object is always in a valid state.
  - `readonly` fields → neither can ever be reassigned after construction.

- **Visibility strategy** (explain this clearly in review):
  - Fields: `private readonly` — encapsulated, immutable.
  - Properties: `public` but **get-only** (`=>`) — callers can *read*
    anything (`CurrentDamage`, `FireInterval`, `BulletPrefab`, …) but can
    never *write* anything.
  - There are **no setters anywhere** → the class is effectively immutable.
- **Immutability by copy**, not by mutation:
  ```csharp
  public Weapon IncreaseLevel() => new Weapon(specs, level + 1);
  public Weapon ResetLevel()   => new Weapon(specs, MinLevel);
  ```
  Leveling up returns a **new Weapon** wrapping the *same* specs asset.
  This is safe (no shared mutable state) and cheap (two small fields).
- **`CurrentDamage`** ties the two halves together:
  `specs.GetDamageForLevel(level)` — state (level) + data (damage table)
  → the answer the firing code needs.
- **Responsibility split**: the `fireInterval` number comes from the asset,
  but the actual fire *timer* is owned by `PlayerController`. The weapon
  supplies data; the player owns game timing.

### Key concepts (say these in review)
- **Data/state separation** = immutable definition (SO) + small state object
  (Weapon) → no shared-mutable-asset bugs, easy to test.
- **Immutability** = `readonly` fields + get-only properties; changes happen
  by creating a new instance.
- **Constructor validation** (clamp) = objects can never be invalid.
- **Encapsulation** = private fields, public read-only surface.
- **Extensibility** = a new weapon = a new asset, *zero* new C# classes.

---

## 4 / Bullet.cs — inheritance, type checks, collisions, responsibility split

### The core principle: one class, identity by data — not by subclass or tag
The SAME `Bullet` class serves both the player and enemies. What makes a bullet
"the player's" or "the enemy's" is not a tag or a subclass — it's the **owner
reference** (`SpaceObject owner`) + damage, set at spawn time:

```csharp
Bullet b = pool.Get<Bullet>(prefab);
b.transform.position = spawnPos;
b.SetOwnerAndDamage(shooter, damage);
```

### Inheritance — what Bullet gets from SpaceObject
`Bullet : SpaceObject` inherits:
1. **Active/life state** — `IsActive`, `IsAlive`.
2. **Collision plumbing** — `OnTriggerEnter2D` adapter + guards, and the
   **virtual `OnCollision(SpaceObject other)` hook** Bullet overrides.
   The base class decides *when* collisions are valid (both active + both
   alive); the subclass decides *what it means for itself*.
3. **Pool hooks** — `Init()` / `ResetObject()`, which the pool calls and
   Bullet overrides to reset its own state (elapsed, owner, damage).
4. The "react to the other, act on yourself" collision philosophy.

This is the **Template Method** pattern again: SpaceObject defines the
skeleton (guards, adapter, hooks); Bullet fills in bullet-specific behavior.

### SetOwnerAndDamage — chaining + origin caching
```csharp
public Bullet SetOwnerAndDamage(SpaceObject owner, int damage)
{
    this.owner = owner;
    Damage = damage;
    spawnOrigin = transform.position;   // re-cache the FINAL spawn point
    return this;
}
```
- **Returns `this`** → fluent chaining after `pool.Get<Bullet>(...)`.
- It **re-caches** `spawnOrigin`, so the convention is: Get → set position →
  SetOwnerAndDamage. The cached origin is always the spawner's final intent.
- `Damage` has a **private setter** (`public int Damage { get; private set; }`)
  — only Bullet's own methods can change it.

### The pool lifecycle hooks in Bullet
- **`Init()`** (pool Get): resets `elapsed = 0`, caches default origin,
  restores `Damage = defaultDamage` (the prefab's value), sets
  `initialized = true`. Movement parameters come **from the prefab itself**
  (serialized field) — the prefab *carries* its own config.
- **`ResetObject()`** (pool Return): clears **`owner = null`** (so a recycled
  bullet never keeps its previous shooter's identity — a real bug class this
  prevents), resets elapsed/damage, clears the initialized flag.

### OnCollision — type-based hit routing (the key method)
```csharp
if (other == owner) return;                       // no self-hit at spawn

bool playerShot = owner is PlayerController;
bool enemyShot  = owner is Enemy;

if (playerShot && other is IDamageable d && !(other is PlayerController))
    d.TakeDamage(Damage);                          // hit any enemy/collectable
else if (enemyShot && other is PlayerController p && !(other is Enemy))
    p.TakeDamage(Damage);                          // enemy shots only hit player

if (hitValidTarget)
    ApplicationController.Instance.ObjectPool.Return(this);
```

Explain these pieces in review:
- **`is` pattern matching** (`other is IDamageable d`) — checks the type AND
  binds it to `d` in one step. No `TryGetComponent`, no tags: damageability is
  a **capability (interface)**, not a label.
- **Friendly-fire guard** — `!(other is PlayerController)` stops player bullets
  from hitting the player; `!(other is Enemy)` stops enemy bullets from
  damaging other enemies.
- **Self-hit prevention** — a bullet spawns overlapping its shooter; the
  `other == owner` check skips it (reference equality, O(1)).
- **Selective consumption** — the bullet is only returned to the pool when it
  hit a *valid* target. Overlapping non-targets (own shooter, friendlies) is
  ignored and the bullet keeps flying. This is the "bullets pass through
  friendlies" behavior with no special-casing.
- **No PlayerBullet / EnemyBullet subclasses** — the same code handles both
  sides by inspecting the owner's type. That's the whole point of the
  criterion "no unnecessary separate implementations".

### Update — movement + end-of-life
```csharp
transform.position = MovementPatterns.GetPosition(movementParameters, elapsed, spawnOrigin);
```
- **One line** of movement — delegates to the pure math class (criterion 2
  pays off here: enemies and bullets share the same math).
- Three end-of-life triggers, all funnel to `ObjectPool.Return(this)`:
  1. Touches a valid target (collision).
  2. Leaves the camera view (`SpriteBoundsInCameraView`).
  3. Movement duration elapses (`Duration > 0 && elapsed >= Duration`).
- Guards: `if (!initialized || !IsActive) return;` — never acts while
  recycled/inactive.

### Key concepts (say these in review)
- **Inheritance for shared plumbing** (base owns state, guards, hooks) +
  **virtual methods** for subclass specifics.
- **Interface as capability**: `IDamageable` — "can be damaged" is a
  capability; `is IDamageable` routes damage without tags or hard-coded
  types.
- **Type-based routing via owner** — one class, behavior chosen by the owner's
  type at runtime; no subclasses, no tags.
- **Encapsulated mutation** — `Damage { get; private set; }`, owner set only
  through the public API.
- **Consistent lifecycle** — same pool, same hooks, same return path for both
  player and enemy bullets.

---

## How the four pieces fit together (the one-paragraph story)

A designer creates **data assets** (`SOWeaponSpecs`, `SOMovementParameters`) —
no code needed per weapon/movement variant. `Weapon` wraps a specs asset with
the player's level. When firing, the spawner asks the **ObjectPool** for a
`Bullet` via the generic `Get<Bullet>()`; the pool dequeues a recycled
instance, activates it, and calls its `Init()` hook. The spawner positions it
and calls `SetOwnerAndDamage(shooter, damage)`. Each frame the bullet moves via
the **pure, shared MovementPatterns** math and checks its end-of-life
conditions. On a valid hit it routes damage by **owner type**
(`is PlayerController` / `is Enemy` / `is IDamageable`) and returns itself to
the pool via `Return()`, which resets its state through the `ResetObject()`
hook — ready for the next shot.

## Cheat sheet: the criteria keywords and where they appear

| Criterion keyword | Where to point |
|---|---|
| Dictionary purpose | `ObjectPool.pools` (prefab→queue) + `objectPrefabs` (reverse map) |
| Queue usage | free-instance FIFO: `Dequeue` front / `Enqueue` back |
| Object lifecycle | `Init()` hook on Get, `ResetObject()` + `SetActive(false)` on Return |
| `Get<T>()` + constraint | compile-time guarantee T is SpaceObject → can call Init/Reset safely |
| Static methods / pure functions | `MovementPatterns` — no state, time passed in, testable |
| Parameterization | `SOMovementParameters` data asset injected into every caller |
| Data vs state separation | `SOWeaponSpecs` (immutable asset) vs `Weapon` (level state) |
| Constructor / properties / visibility | `Weapon` ctor clamps level; `private readonly` fields; get-only props |
| Inheritance / type checks | `Bullet : SpaceObject`; `is PlayerController`, `is IDamageable d` |
| No separate player/enemy bullets | single `Bullet`, owner-based routing in `OnCollision` |
