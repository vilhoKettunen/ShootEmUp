using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting; // template dependency: Object.GetComponent<> extension (kept from original code)

/// <summary>
/// The application-level controller. Owns the top-level objects of a game
/// session: the player ship, the UI controller, and the object pool that
/// recycles bullets, enemies and collectables.
///
/// Responsibilities:
///   - Bootstraps the UI and the player ship when the game loads.
///   - Loads the pool configuration asset and pre-instantiates the pooled
///     prefabs so bullets/enemies/collectables can be recycled instead of
///     destroyed and re-created.
///   - Spawns the demo enemies + test bullet used for in-scene testing.
///   - Bridges the UI to the game (StartNewGame / QuitGame).
/// </summary>
public class ApplicationController : SingletonMono<ApplicationController>
{
    private PlayerController player;
    private UIController ui;

    // ---- Object pool (bullets, enemies, collectables are recycled through this) ----
    [HideInInspector] public SOPoolSpaceObjects PoolSpaceObjects;
    [HideInInspector] public ObjectPool ObjectPool = new ObjectPool();

    [Header("Test spawning")]
    [Tooltip("When ON, the controller spawns the test enemies + a bullet at game start. Toggle in the Inspector.")]
    [SerializeField] private bool enableTestSpawns = true;
    [SerializeField] private SOMovementParameters vultureMovement;
    [SerializeField] private SOMovementParameters falconMovement;
    [SerializeField] private SOMovementParameters testBulletMovement;
    [SerializeField] private GameObject testBulletPrefab;
    [SerializeField] private GameObject vulturePrefab;
    [SerializeField] private GameObject falconPrefab;
    [SerializeField] private GameObject testDummyPrefab;

    [Tooltip("One collectable pickup prefab per entry. Each prefab should carry a WeaponPickup script with its own weapon assigned (e.g. one MachineGun pickup, one Plasma pickup, ...). They are spawned from the pool — add each prefab to the SOPoolSpaceObjects asset first.")]
    [SerializeField] private List<GameObject> testCollectablePrefabs = new List<GameObject>();
    [Tooltip("Where the test pickups spawn (stacked horizontally, one row per pickup).")]
    [SerializeField] private Vector2 collectableSpawnPosition = new Vector2(0f, 2.5f);
    [SerializeField] private float collectableSpacing = 1.5f;

    [Tooltip("Optional: extra on-screen debug text (the HUD view already shows weapon/level/dummy HP).")]
    [SerializeField] private Text debugText;
    [SerializeField] private Vector2 vultureSpawnPosition = new Vector2(-2f, 4f);
    [SerializeField] private Vector2 falconSpawnPosition = new Vector2(2f, 4f);
    [SerializeField] private Vector2 testBulletSpawnPosition = new Vector2(0f, -4f);
    [SerializeField] private Vector2 testDummySpawnPosition = new Vector2(0f, 1.5f);

    /// <summary>
    /// Runs once when the application loads (before any scene content is
    /// ready). Makes sure the singleton Instance exists, then boots the game.
    /// </summary>
    [RuntimeInitializeOnLoadMethod]
    static void OnInit()
    {
        Instance.Init();
    }

    /// <summary>
    /// Bootstraps the game session:
    ///   1. Instantiates the UI prefab (which builds all its views) and hides
    ///      the HUD, since the main menu is on top at start.
    ///   2. Instantiates the player ship from its Resources prefab.
    ///   3. Loads the pool configuration and pre-instantiates the pooled objects.
    ///   4. Optionally spawns the demo enemies and test bullet.
    /// </summary>
    public void Init()
    {
        ui = Instantiate(Resources.Load("UI")).GetComponent<UIController>().Init();

        // The HUD is part of the UI prefab and defaults to VISIBLE, so it would
        // show behind the start menu. Hide it now (start-menu state); it is
        // re-shown in StartNewGame() once the actual game begins.
        UiViewHud.Instance?.Hide();

        player = Instantiate<PlayerController>(Resources.Load<PlayerController>("PlayersShip"));

        // Load the pool config asset + pre-instantiate each pooled prefab.
        InitObjectPool();

        if (enableTestSpawns)
        {
            SpawnTestObjects();
        }
    }

    /// <summary>
    /// Loads the SOPoolSpaceObjects asset from a Resources folder and
    /// registers every pool entry with the ObjectPool. Adding a new poolable
    /// prefab is a data change in the asset only — no code change needed.
    /// </summary>
    private void InitObjectPool()
    {
        PoolSpaceObjects = Resources.Load<SOPoolSpaceObjects>("PoolSpaceObjects");
        if (PoolSpaceObjects == null)
        {
            Debug.LogError("[ApplicationController] 'PoolSpaceObjects' SOPoolSpaceObjects asset not found in a Resources folder. " +
                "Create it: Create → Game → Pool Space Objects, and put it in Assets/Resources/. " +
                "Bullets/enemies/collectables will NOT be pooled until this is fixed.");
            return;
        }

        foreach (PoolEntry entry in PoolSpaceObjects.PoolObjects)
        {
            if (entry.prefab == null)
            {
                Debug.LogWarning("[ApplicationController] A PoolEntry has no prefab assigned — skipped.");
                continue;
            }
            ObjectPool.Register(entry.prefab, entry.initialSize);
        }
        Debug.Log("[ApplicationController] ObjectPool registered " + PoolSpaceObjects.PoolObjects.Count + " prefab type(s).");
    }

    /// <summary>
    /// Spawns the demo content used to test the game: two enemies (Vulture +
    /// Falcon, each with its OWN movement data asset), one bullet (to test
    /// movement), and optionally a stationary test target. Controlled by the
    /// enableTestSpawns flag — turn it OFF for normal play. This is the
    /// starting point for the real spawn/wave system.
    ///
    /// The test bullet is spawned through the ObjectPool — demonstrating the
    /// recycling system. The enemies are instantiated directly because they
    /// need their movement data injected, which the pool path does not do.
    /// </summary>
    private void SpawnTestObjects()
    {
        SpawnEnemy(vulturePrefab, vultureMovement, vultureSpawnPosition, "Vulture");
        SpawnEnemy(falconPrefab, falconMovement, falconSpawnPosition, "Falcon");

        if (testBulletPrefab != null)
        {
            // Get the bullet from the pool instead of Instantiate.
            // The pool keys on the GameObject prefab and resolves the ROOT Bullet
            // component internally (the SO entry and this field must reference
            // the SAME prefab asset for the pool to find it).
            Bullet bullet = ObjectPool.Get<Bullet>(testBulletPrefab);
            if (bullet == null)
            {
                Debug.LogError("[Test] ObjectPool.Get<Bullet> returned null — is the test bullet prefab in PoolSpaceObjects?");
                return;
            }

            bullet.transform.position = testBulletSpawnPosition;

            // Note: the bullet prefab already carries its own movement asset
            // (see Bullet.movementParameters), so the test bullet moves with
            // whatever SOMovementParameters is assigned on the prefab.

            bullet.SetOwnerAndDamage(player, 1);

            Debug.Log("[Test] Spawned test bullet (movement test) — damage 1, movement: "
                + (bullet.MovementParameters != null ? bullet.MovementParameters.name : "NONE ASSIGNED"));
        }

        // Spawn a row of test weapon pickups (one per prefab in the list).
        SpawnTestCollectables();

        if (debugText != null)
        {
            debugText.text = "TEST MODE: [1-5] switch weapon  E level up  Q level down";
        }
    }

    /// <summary>
    /// Spawns one instance of each test collectable prefab, laid out in a row
    /// (spacing = collectableSpacing), starting at collectableSpawnPosition.
    ///
    /// Each pickup is taken FROM THE POOL (ObjectPool.Get&lt;Collectable&gt;) —
    /// this both demonstrates recycling and lets you fly the ship into one to
    /// test the collect logs (see WeaponPickup.Collect for the detailed output).
    ///
    /// Each prefab should carry a WeaponPickup script with a specific
    /// SOWeaponSpecs assigned (e.g. "MachineGun", "Plasma", "Spread") so the
    /// logs show the correct level-up vs. switch outcome for each weapon.
    /// </summary>
    private void SpawnTestCollectables()
    {
        if (testCollectablePrefabs == null || testCollectablePrefabs.Count == 0)
        {
            return;
        }

        Debug.Log("[Test] Spawning " + testCollectablePrefabs.Count + " test weapon pickup(s) ...");

        int index = 0;
        foreach (GameObject prefab in testCollectablePrefabs)
        {
            if (prefab == null)
            {
                index++;
                continue;
            }

            // Get from the pool (the prefab must be in SOPoolSpaceObjects).
            Collectable pickup = ObjectPool.Get<Collectable>(prefab);
            if (pickup == null)
            {
                Debug.LogError("[Test] ObjectPool.Get<Collectable> returned null for '" + prefab.name +
                    "' — is it in PoolSpaceObjects?");
                index++;
                continue;
            }

            // Lay the pickups out in a horizontal row.
            Vector2 spawnPos = collectableSpawnPosition + new Vector2(index * collectableSpacing, 0f);
            pickup.transform.position = spawnPos;

            string weaponName = (pickup as WeaponPickup)?.WeaponSpecs?.WeaponName ?? "(no weapon assigned)";
            Debug.Log("[Test]   Pickup " + (index + 1) + ": '" + weaponName + "' at " + spawnPos
                + " — fly into it to see the collect log.");

            index++;
        }
    }

    /// <summary>
    /// Instantiates one demo enemy, injects its movement data asset and
    /// starts its firing loop. The Enemy component may sit on a child of the
    /// prefab, so it is resolved with GetComponentInChildren.
    /// </summary>
    private void SpawnEnemy(GameObject enemyPrefab, SOMovementParameters movement, Vector2 position, string label)
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("[Test] " + label + " prefab not assigned — skipped.");
            return;
        }

        // The Enemy component lives on a child GameObject of the prefab (see the
        // prefab structure), so the Inspector field is a GameObject and we resolve
        // the component at runtime. GetComponentInChildren also covers the case
        // where the component is on the root.
        Enemy enemy = enemyPrefab.GetComponentInChildren<Enemy>();
        if (enemy == null)
        {
            Debug.LogError("[Test] " + label + " prefab '" + enemyPrefab.name + "' has no Enemy component — skipped.");
            return;
        }

        GameObject spawned = Instantiate(enemyPrefab, position, Quaternion.identity);
        Enemy spawnedEnemy = spawned.GetComponentInChildren<Enemy>();
        if (movement == null)
        {
            Debug.LogWarning("[Test] " + label + " has no SOMovementParameters assigned — it will not move.");
        }

        if (spawnedEnemy is EnemyVulture vulture)
        {
            vulture.Init(movement);
        }
        else if (spawnedEnemy is EnemyFalcon falcon)
        {
            falcon.Init(movement);
        }

        // The Instantiate + Init(movement) path above only sets up
        // movement — it does NOT start the firing loop (that usually starts in
        // the pool Init() hook). Start it explicitly so enemies spawned here can shoot.
        spawnedEnemy.StartFiring();

        Debug.Log("[Test] Spawned " + label + " at " + position
            + " (pattern: " + (movement != null ? movement.Pattern : "NONE")
            + ", speed: " + (movement != null ? movement.Speed : 0) + ")");
    }

    /// <summary>
    /// Called when the player starts a new game from the main menu:
    /// reveals the in-game HUD and pushes the player's initial HP so the
    /// health display is correct from the first frame.
    /// </summary>
    public void StartNewGame()
    {
        // We've left the start menu — reveal the in-game HUD (it was hidden in
        // Init() so it doesn't show behind the menu).
        UiViewHud.Instance?.Show();

        // Push the player's initial HP so the HUD shows the correct value on start.
        if (player != null)
        {
            UiViewHud.Instance?.UpdateHealth(player.HitPoints);
        }
    }

    /// <summary>
    /// Quits the application: stops play mode in the Editor, or exits the
    /// player build on other platforms.
    /// </summary>
    public void QuitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}
