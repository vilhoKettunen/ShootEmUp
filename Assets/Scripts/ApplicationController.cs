using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting; // template dependency: Object.GetComponent<> extension (kept from original code)

public class ApplicationController : SingletonMono<ApplicationController>
{
    private PlayerController player;
    private UIController ui;

    // ---- Task 4 additions (pool) ----
    [HideInInspector] public SOPoolSpaceObjects PoolSpaceObjects;
    [HideInInspector] public ObjectPool ObjectPool = new ObjectPool();

    [Header("Test spawning (Q-T2-6)")]
    [Tooltip("When ON, the controller spawns the test enemies + a bullet at game start. Toggle in the Inspector.")]
    [SerializeField] private bool enableTestSpawns = true;
    [SerializeField] private SOMovementParameters vultureMovement;
    [SerializeField] private SOMovementParameters falconMovement;
    [SerializeField] private SOMovementParameters testBulletMovement;
    [SerializeField] private GameObject testBulletPrefab;
    [SerializeField] private GameObject vulturePrefab;
    [SerializeField] private GameObject falconPrefab;
    [SerializeField] private GameObject testDummyPrefab;
    [Tooltip("Optional: extra on-screen debug text (the HUD view already shows weapon/level/dummy HP).")]
    [SerializeField] private Text debugText;
    [SerializeField] private Vector2 vultureSpawnPosition = new Vector2(-2f, 4f);
    [SerializeField] private Vector2 falconSpawnPosition = new Vector2(2f, 4f);
    [SerializeField] private Vector2 testBulletSpawnPosition = new Vector2(0f, -4f);
    [SerializeField] private Vector2 testDummySpawnPosition = new Vector2(0f, 1.5f);

    [RuntimeInitializeOnLoadMethod]
    static void OnInit()
    {
        Instance.Init();
    }

    public void Init()
    {
        ui = Instantiate(Resources.Load("UI")).GetComponent<UIController>().Init();

        // The HUD is part of the UI prefab and defaults to VISIBLE, so it would
        // show behind the start menu. Hide it now (start-menu state); it is
        // re-shown in StartNewGame() once the actual game begins.
        UiViewHud.Instance?.Hide();

        player = Instantiate<PlayerController>(Resources.Load<PlayerController>("PlayersShip"));

        // ---- Task 4: load the pool config asset + pre-instantiate each entry ----
        InitObjectPool();

        if (enableTestSpawns)
        {
            SpawnTestObjects();
        }
    }

    /// <summary>
    /// (Task 4) Load the SOPoolSpaceObjects asset from a Resources folder and
    /// register every pool entry. One line per entry, no per-type code branches
    /// (rule 2.8: all pool tuning lives in the SO asset).
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
    /// TEST HARNESS (Q-T2-6 / Q-T3-4): spawns the two test enemies (Vulture +
    /// Falcon, each with its OWN movement SO asset), one bullet (Task 2
    /// movement test), and the immortal TestDummy target (damage visible on the
    /// HUD + console). Controlled by the enableTestSpawns bool — turn it OFF for
    /// normal play. Later this grows into the real spawn/wave system.
    ///
    /// (Task 4) The test bullet is now spawned through the ObjectPool — that's
    /// the whole point of the pool. The enemies are still spawned via
    /// Instantiate because they need their movement SO injected (the old
    /// Task 2 API: Init(SOMovementParameters)); the test harness is the only
    /// place that path still runs.
    /// </summary>
    private void SpawnTestObjects()
    {
        SpawnEnemy(vulturePrefab, vultureMovement, vultureSpawnPosition, "Vulture");
        SpawnEnemy(falconPrefab, falconMovement, falconSpawnPosition, "Falcon");

        if (testBulletPrefab != null)
        {
            // (Task 4) Get the bullet from the pool instead of Instantiate.
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
            // Optional: override with the test harness's movement SO.
            if (testBulletMovement != null)
            {
                // The old Task 2 injection API — works on any Bullet-derived class
                // that exposes Init(SO); the base Bullet just reads its own field.
            }
            bullet.SetOwnerAndDamage(player, 1);

            Debug.Log("[Test] Spawned test bullet (movement test) — damage 1, movement: "
                + (bullet.MovementParameters != null ? bullet.MovementParameters.name : "NONE ASSIGNED"));
        }

        if (testDummyPrefab != null)
        {
            Instantiate(testDummyPrefab, testDummySpawnPosition, Quaternion.identity);
            Debug.Log("[Test] Spawned immortal TestDummy at " + testDummySpawnPosition);
        }

        if (debugText != null)
        {
            debugText.text = "TEST MODE: [1-5] switch weapon  E level up  Q level down";
        }
    }

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

        Debug.Log("[Test] Spawned " + label + " at " + position
            + " (pattern: " + (movement != null ? movement.Pattern : "NONE")
            + ", speed: " + (movement != null ? movement.Speed : 0) + ")");
    }

    public void StartNewGame()
    {
        // We've left the start menu — reveal the in-game HUD (it was hidden in
        // Init() so it doesn't show behind the menu).
        UiViewHud.Instance?.Show();
    }

    public void QuitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}
