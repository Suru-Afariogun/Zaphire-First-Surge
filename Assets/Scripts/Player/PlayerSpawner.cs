// using UnityEngine;

// public class PlayerSpawner : MonoBehaviour
// {
//     public Transform spawnPoint;
//     public GameObject playerPrefab;

//     void Start()
//     {
//         Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
//     }
// }

using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public Transform spawnPoint; // assign in Inspector

    void Start()
    {
        // Check if GameManager exists
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null! Make sure a GameManager GameObject exists in the scene.");
            return;
        }

        GameObject prefab = GameManager.Instance.GetSelectedCombatStylePrefab();

        if (prefab != null)
        {
            Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        }
        else
        {
            Debug.LogError("No combat style prefab found to spawn! Check GameManager's CombatStylePrefabs array.");
        }
    }
}
