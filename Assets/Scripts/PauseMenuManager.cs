// using UnityEngine;

// public class PauseMenuManager : MonoBehaviour
// {
//     public static PauseMenuManager Instance;
//     public GameObject pauseMenuPrefab;
//     private GameObject pauseMenuInstance;

//     void Awake()
//     {
//         // Ensure only one exists
//         if (Instance != null && Instance != this)
//         {
//             Destroy(gameObject);
//             return;
//         }
//         Instance = this;
//         DontDestroyOnLoad(gameObject);

//         // Spawn the Pause Menu once
//         if (pauseMenuPrefab != null && pauseMenuInstance == null)
//         {
//             pauseMenuInstance = Instantiate(pauseMenuPrefab);
//             pauseMenuInstance.SetActive(false); // Start disabled
//             DontDestroyOnLoad(pauseMenuInstance);
//         }
//     }

//     void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.Escape))
//         {
//             TogglePause();
//         }

//         // Optional controller support
//         if (Input.GetButtonDown("Start") || Input.GetButtonDown("Pause"))
//         {
//             TogglePause();
//         }
//     }

//     public void TogglePause()
//     {
//         if (pauseMenuInstance == null) return;

//         bool currentlyActive = pauseMenuInstance.activeSelf;
//         pauseMenuInstance.SetActive(!currentlyActive);

//         Time.timeScale = currentlyActive ? 1f : 0f;
//         PauseMenu.isPaused = !currentlyActive;
//     }
// }



using UnityEngine;

/// <summary>
/// Legacy PauseMenuManager. Currently unused; kept only so you can restore
/// the old global pause-prefab system later if you want.
/// Pause is now driven by PlayerController (CombatStyleMenu button) +
/// GameManager-spawned PausePopupUI / PauseMenu.
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // Intentionally left blank.
    }
}