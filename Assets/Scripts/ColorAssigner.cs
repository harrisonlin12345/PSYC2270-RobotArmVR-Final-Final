
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ColorAssigner : MonoBehaviour
{
    [Header("Assignment Settings")]
    public float assignmentDelay = 2f;

    private readonly HashSet<ColoredBlock> overlapping =
        new HashSet<ColoredBlock>();

    private readonly Dictionary<ColoredBlock, Coroutine> pending =
        new Dictionary<ColoredBlock, Coroutine>();

    private void OnTriggerEnter(Collider other)
    {
        ColoredBlock block =
            other.GetComponentInParent<ColoredBlock>();

        if (block == null)
            return;

        overlapping.Add(block);

        if (block.assignedColor ==
                ColoredBlock.BlockColor.Unassigned &&
            !pending.ContainsKey(block))
        {
            pending[block] =
                StartCoroutine(AssignAfterDelay(block));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        ColoredBlock block =
            other.GetComponentInParent<ColoredBlock>();

        if (block == null)
            return;

        overlapping.Remove(block);

        if (pending.TryGetValue(block, out Coroutine routine))
        {
            StopCoroutine(routine);
            pending.Remove(block);
        }
    }

    private IEnumerator AssignAfterDelay(ColoredBlock block)
    {
        XRGrabInteractable grab =
            block.GetComponent<XRGrabInteractable>();

        // Wait until the block is released.
        while (block != null &&
               overlapping.Contains(block) &&
               grab != null &&
               grab.isSelected)
        {
            yield return null;
        }

        if (block == null || !overlapping.Contains(block))
        {
            pending.Remove(block);
            yield break;
        }

        // Require 2 continuous seconds on the assigner.
        float elapsed = 0f;

        while (elapsed < assignmentDelay)
        {
            if (block == null || !overlapping.Contains(block))
            {
                pending.Remove(block);
                yield break;
            }

            if (grab != null && grab.isSelected)
            {
                // Reset timer while being held.
                elapsed = 0f;
            }
            else
            {
                elapsed += Time.deltaTime;
            }

            yield return null;
        }

        int random = Random.Range(0, 3);

        ColoredBlock.BlockColor color =
            (ColoredBlock.BlockColor)(random + 1);

        block.AssignColor(color);

        pending.Remove(block);
    }
}
