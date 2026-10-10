# AGENTS.md — Shoot 'em up Course Project: Rules, Criteria & Instructions

> This file is the **binding instruction set** for working in the ShootEmUp Unity
> project. It contains the course grading rules (strict), the game design spec, and
> the workflow rules for the AI assistant.
>
> **The owner of this project is a student who is learning from the work.**
> The AI assistant must **teach** while coding — see "Teaching Mode" below.

---

## 1. Project Overview

- **Type:** 2D shoot 'em up (vertically or horizontally scrolling), C#
- **Engine:** Unity 6.5 (6000.5.6f1), URP 2D — all work starts from the course's
  fixed project template; do not deviate from it
- **Solution:** `ShootEmUp.sln` → single `Assembly-CSharp` project
- **Deliverables:**
  - Mid-course: a working build containing all features assigned up to that point
  - End of course: (relevant) source code files assembled into one PDF, submitted to Learnin
  - Code review presentation + peer review at the end

### Game Design Spec (from course assignment)

- No separate levels/scenes. Enemies arrive in **scripted waves**; the game ends on
  player death, or continues harder (e.g. faster) after the player defeats the
  **final boss**, which appears after **5–10 enemy waves**.
- **Lives:** player starts with 3; extra lives obtainable; max 5.
- **Weapons:** 3 weapon types (plus bombs), each with 5 efficiency levels.
- **Power-ups:** dropped by enemies. Collecting the *same* weapon type raises its
  level; collecting a *different* type switches weapons but **keeps the current level**.
- **Death:** collision with enemy, enemy bullet, or obstacle kills the player; on
  death the weapon resets to the starting type and power.
- **Enemies:** at least 5 distinct basic types, clearly different in movement
  pattern, speed, shooting direction and durability.
- **Boss:** exactly one boss enemy, different enough that it is justified to write a
  `BossEnemy` class **extending the base `Enemy` class**.
- **UI:** in-game HUD showing lives, current weapon + its level, bombs, score.
  Plus: start menu, Game Over view, Pause view.
- **Feedback:** sounds, music and visual effects for shooting, ship destruction and
  power-up pickup (quality can be minimal; they must exist and be logically placed).
- **Code quality goal:** easily extendable, dynamic, reusable, clear, and as
  independent from the Unity editor as possible (portability to other platforms/engines).

---

## 2. Course Grading Rules (STRICT — apply to all code)

These rules affect the grade. Every piece of code written in this project must
satisfy **all** of them. When a proposed change would break one, stop and flag it.

### 2.1 Fixed controller classes
> FI: *Projektissa on valmiina joitain luokkia kuten: ApplicationController,
> PlayerController, InputController, UIController, AudioController (ja näihin
> liittyviä luokkia). Nämä luokat pysyvät projektissa alusta loppuun samannimisinä
> ja mitään tarpeettomia luokkia ei lisätä hoitamaan niihin kuuluvia
> toiminnallisuuksia ja/tai niiden toiminnallisuus ei "valu" mihinkään muihin luokkiin.*

- The classes **ApplicationController, PlayerController, InputController,
  UIController, AudioController** (and their related classes) already exist in the
  template and **must keep their names from start to finish**. Do not rename them.
- **Do not add unnecessary classes** to implement functionality that belongs to
  these controllers.
- The functionality of these controllers must **not "leak" into other classes** —
  e.g. enemy code must not contain UI or audio logic.

**Note:** the course rules list `InputController`, but it does not exist in the
project template yet (verified by code search). Confirmed with the teacher:
**it will be delivered later in the course when it is relevant.** Until then, do
NOT put input-handling logic into other classes (e.g. keep it out of
`PlayerController`).

### 2.2 One script per interactive object
> FI: *Pelin interaktiivisilla objekteilla on vain yksi skripti (lukuun ottamatta
> Unityn valmiita komponentteja).*

- Every interactive game object (player, enemy, bullet, power-up, etc.) has
  **exactly one custom script** on it. Unity's built-in components
  (Collider, Rigidbody2D, SpriteRenderer, …) are allowed.
- Consequence: an object's full behavior lives in that single class; extend
  behavior via **inheritance** (e.g. `BossEnemy : Enemy`), not by stacking scripts.

### 2.3 No tags — use types
> FI: *Pelissä ei käytetä laisinkaan tageja, vaan asiat erotetaan toisistaan tyypeillä.*

- **Unity tags are forbidden entirely** (`CompareTag`, tag strings, etc.).
- Distinguish objects by their **C# type** (e.g. `if (collider.GetComponent<Enemy>() != null)`,
  `is` / `as` checks, generic constraints, typed events).
- Rationale: types are checked at compile time, IDE-friendly, and portable.

### 2.4 MonoBehaviour only when necessary
> FI: *Vain ne luokat periytyvät MonoBehaviour-luokasta, joiden ehdottomasti tarvitsee.*

- A class inherits `MonoBehaviour` **only if it must** (needs Unity lifecycle
  callbacks, needs to be a component, needs coroutines, etc.).
- Plain logic (weapon stats, wave definitions, scoring rules, easing curves…)
  must be in **plain C# classes** — this is what makes the code editor-independent
  and portable.
- Rationale: pure C# classes are unit-testable and don't drag in the engine.

### 2.5 Scene independence
> FI: *Pelin voi käynnistää täysin virheettömästi mistä tahansa Unity
> scene-tiedostosta. … millään toiminnallisella osalla ei ole riippuvuksia näistä
> skripteistä.*

- The game must **start completely error-free from ANY Unity scene file**.
- A scene may contain decorative scripted content (background animations, etc.),
  but **no functional part may depend on scene-specific scripts**.
- Practical rule: never rely on an object "existing in the scene"; look up or
  create dependencies explicitly (via the controllers / DI-style references).

### 2.6 No repeated reference fetching
> FI: *Komponenttiviittauksia ei haeta toistuvasti pelin aikana, jos viittaus
> voidaan alustaa kerran.*

- Component references (`GetComponent`, `Find`, etc.) must be resolved **once**
  (e.g. in `Awake`/`Start` or constructor) and cached in a field, **unless a
  reference genuinely changes at runtime**.
- Rationale: `GetComponent`/`Find` in `Update` is a performance smell and usually
  a design smell (the object shouldn't be "finding" things every frame).

### 2.7 Clear separation of responsibilities
> FI: *Luokkien tulee noudattaa selkeää vastuujakoa - Esim. PlayerController-luokka
> ei käsittele ääniä, inputtia ja pelin tilaa.*

- Each class has **one clear responsibility**. Example: `PlayerController` does
  NOT handle audio, input, or game state — those belong to `AudioController`,
  `InputController`, `ApplicationController` respectively.
- When in doubt, ask: "which controller owns this concern?" and put it there.

### 2.8 Minimize Inspector usage
> FI: *Inspectorin käyttö minimoidaan (suuri osa objekteista instantioidaan
> prefabeista tarvittaessa) ja [SerializeField]-attribuuttia sekä public-tason
> muuttujia käytetään vain, jos niitä ehdottomasti tarvitaan.*

- **Minimize Inspector configuration**: most objects should be **instantiated from
  prefabs at runtime** as needed, not hand-placed in scenes.
- Use `[SerializeField]` and `public` fields **only when truly necessary**.
- Default visibility is `private`; expose to the Inspector only with an explicit,
  justified reason (e.g. a prefab reference that must be assigned once).

---

## 3. Code Map (current hand-written scripts)

| Area | Files |
|---|---|
| Core | `Assets/Scripts/ApplicationController.cs`, `Assets/Scripts/PlayerController.cs`, `Assets/Scripts/UIController.cs` |
| UI system | `Assets/Scripts/UI/UiElement.cs`, `UiView.cs`, `UiViewsList.cs`, `UiViewsListItem.cs`, `UiMainMenuButton.cs`, `Views/UiViewMainMenu.cs` |
| Audio | `Assets/Scripts/AudioScripts/AudioController.cs`, `MusicPlayList.cs` |
| Helpers | `Assets/Scripts/Helper/SingletonMono.cs`, `EassingFunctions.cs` |
| Generated | `Assets/Settings/InputSystem_Actions.cs` — Unity-generated, do not edit by hand |

Naming conventions observed in the template (keep them):
- UI classes use a `Ui` prefix (e.g. `UiView`, `UiElement`).
- Managers use a `Controller` suffix.
- Generic helpers live in `Assets/Scripts/Helper/`.
- Use the existing `SingletonMono` helper for singleton MonoBehaviours rather
  than inventing new singleton patterns.

---

## 4. Workflow Rules for the AI Assistant

1. **Read this file and the relevant source before editing.**
2. **Teaching Mode (IMPORTANT — the user is a student):**
   - Before writing code, briefly explain the *concept* and *why* it's the right
     approach in Unity/C# (e.g. what `MonoBehaviour` lifecycle does, why caching a
     reference matters, what inheritance buys us).
   - After writing a snippet, walk through the important lines and point out which
     course rule each part satisfies (e.g. "this field is `private` +
     `[SerializeField]` per rule 2.8; the type check per rule 2.3").
   - When suggesting an action, say what the user should do in Unity and *why*
     (e.g. "create the prefab instead of placing the object in the scene, per 2.8").
   - Keep explanations concise but complete; prefer teaching over silent fixes.
3. **Rule compliance check:** before proposing any change, verify it satisfies
   rules 2.1–2.8. If a conflict exists, surface it and offer the compliant option.
4. **No tags** in any code (2.3); **no per-frame `GetComponent`/`Find`** (2.6).
5. Match existing code style (indentation, naming, comment density).
6. Prefer minimal, targeted changes over refactors unless the exercise asks.
7. **Update this file** when new conventions, class additions, or architecture
   decisions are made (e.g. when `InputController` or `Enemy`/`BossEnemy` are
   created, add them to the Code Map).

### Task Progress Log
| Task | Status | Docs |
|---|---|---|
| 1. SpaceObject base class + Player/Enemy/Obstacle/Collectable | 📝 in progress (design stage) | `Assets/Scripts/documentation/task-01-spaceobject/` |
8. **Documentation (mandatory):** for every task, create and maintain the
   three task documents per §5 in `Assets/Scripts/documentation/` (`.txt` files):
   `dev-actions.txt`, `code-changes-and-why.txt`, `learned-lessons-and-concepts.txt`.

---

## 5. Documentation Rules (mandatory for every task)

For **every course task**, create a task-specific folder under
`Assets/Scripts/documentation/` named `task-NN-short-task-name/`
(e.g. `task-01-spaceobject`), containing **exactly three** plain text (`.txt`) files:

| File | What it is for |
|---|---|
| `dev-actions.txt` | **What to do**: a concrete checklist — what has been done, what is still pending (code or Unity actions), in what order, plus open questions for the teacher. The "what do I do next?" view. |
| `code-changes-and-why.txt` | **What changed and why**: every code change or proposed design, with the "why" and which course rule (§2.1–2.8) it satisfies. The "do I understand this?" view. |
| `learned-lessons-and-concepts.txt` | **What to learn**: the underlying C#/Unity concepts explained in more detail (inheritance, virtual/override, encapsulation, collision system, …). The "how does it actually work?" view. |

Rules:
- Write the docs **during** the task, not as an afterthought; tick items off
  in `dev-actions.txt` as they are completed.
- Docs are in English; reference course rules by number (§2.1–§2.8).
- **Line length limit: 158 characters per line.** VS 2022 (Unity mode) shows `.txt`
  files without word-wrap, so every line must fit within 158 chars: wrap prose at
  word boundaries, and never use wide tables — use lists or per-item sections
  instead so each line stays short.
- Docs are **plain text (`.txt`)**, NOT Markdown: VS 2022 in Unity mode handles
  `.txt` natively but not `.md`. Unity ignores them, so they never affect the build.
