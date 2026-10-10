using UnityEngine;
using UnityEngine.UI;
using Unity.VisualScripting; // template dependency: Object.GetComponent<> extension (kept from original code)

public class ApplicationController : SingletonMono<ApplicationController>
{
    private PlayerController player;
    private UIController ui;

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
        player = Instantiate<PlayerController>(Resources.Load<PlayerController>("PlayersShip"));

        if (enableTestSpawns)
        {
            SpawnTestObjects();
        }
    }

    /// <summary>
    /// TEST HARNESS (Q-T2-6 / Q-T3-4): spawns the two test enemies (Vulture +
    /// Falcon, each with its OWN movement SO asset), one bullet (Task 2
    /// movement test), and the immortal TestDummy target (damage visible on the
    /// HUD + console). Controlled by the enableTestSpawns bool — turn it OFF for
    /// normal play. Later this grows into the real spawn/wave system.
    /// </summary>
    private void SpawnTestObjects()
    {
        SpawnEnemy(vulturePrefab, vultureMovement, vultureSpawnPosition, "Vulture");
        SpawnEnemy(falconPrefab, falconMovement, falconSpawnPosition, "Falcon");

        if (testBulletPrefab != null)
        {
            var spawnedBullet = Instantiate(testBulletPrefab, testBulletSpawnPosition, Quaternion.identity);
            if (spawnedBullet == null)
            {
                Debug.LogError("[Test] Instantiate(testBulletPrefab) returned null.");
            }
            else
            {
                Bullet bullet = spawnedBullet.GetComponent<Bullet>();
                if (bullet == null)
                {
                    Debug.LogError("[Test] " + testBulletPrefab.name + " prefab root has no Bullet component. "
                        + "Ensure the Bullet script is on the ROOT GameObject of the prefab (not a child).");
                }
                else
                {
                    bullet.Init(testBulletMovement, 1);
                    Debug.Log("[Test] Spawned test bullet (movement test) — damage 1, movement: "
                        + (testBulletMovement != null ? testBulletMovement.name : "NONE ASSIGNED"));
                }
            }
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
