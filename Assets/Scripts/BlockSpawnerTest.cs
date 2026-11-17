
using UnityEngine;

public class BlockSpawnerTest : MonoBehaviour
{
    public BlockSpawner spawner;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            GameObject block = spawner.GetCurrentBlock();

            if (block != null)
            {
                // Simulate the robot taking the block
                spawner.BlockPickedUp(block);

                // Move it away from the spawn point
                block.transform.position +=
                    Vector3.up * 0.5f;

                Debug.Log("Simulated robot pickup");
            }
        }
    }
}
