
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    [Header("Block Settings")]
    public GameObject blockPrefab;
    public Transform spawnPoint;

    [Header("Spawn Settings")]
    public bool spawnOnStart = true;
    public float respawnDelay = 0.5f;

    private GameObject currentBlock;
    private int blockCounter = 0;
    private bool waitingToRespawn = false;

    void Start()
    {
        if (spawnOnStart)
        {
            SpawnNextBlock();
        }
    }

    public GameObject SpawnNextBlock()
    {
        if (blockPrefab == null || spawnPoint == null)
        {
            Debug.LogError(
                "Assign Block Prefab and Spawn Point!"
            );
            return null;
        }

        // Don't spawn another block while one
        // is still waiting to be picked up.
        if (currentBlock != null || waitingToRespawn)
        {
            return currentBlock;
        }

        currentBlock = Instantiate(
            blockPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        currentBlock.name = "WhiteBlock_" + blockCounter;
        blockCounter++;

        Debug.Log("Spawned " + currentBlock.name);

        return currentBlock;
    }

    // Call this after the robot attaches the block
    // to its gripper.
    public void BlockPickedUp(GameObject pickedUpBlock)
    {
        if (pickedUpBlock != currentBlock)
        {
            Debug.LogWarning(
                "The picked-up block is not the current spawn block."
            );
            return;
        }

        currentBlock = null;

        if (!waitingToRespawn)
        {
            StartCoroutine(RespawnAfterDelay());
        }
    }

    System.Collections.IEnumerator RespawnAfterDelay()
    {
        waitingToRespawn = true;

        yield return new WaitForSeconds(respawnDelay);

        waitingToRespawn = false;
        SpawnNextBlock();
    }

    public GameObject GetCurrentBlock()
    {
        return currentBlock;
    }
}
