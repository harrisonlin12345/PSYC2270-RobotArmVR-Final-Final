
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SortingZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public ColoredBlock.BlockColor zoneColor;

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

        if (!pending.ContainsKey(block))
        {
            pending[block] =
                StartCoroutine(CheckBlock(block));
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

    private IEnumerator CheckBlock(ColoredBlock block)
    {
        XRGrabInteractable grab =
            block.GetComponent<XRGrabInteractable>();

        // Wait until the participant releases the block.
        while (block != null &&
               overlapping.Contains(block) &&
               grab != null &&
               grab.isSelected)
        {
            yield return null;
        }

        // Allow physics to settle after release.
        yield return new WaitForSeconds(0.15f);

        if (block == null || !overlapping.Contains(block))
        {
            pending.Remove(block);
            yield break;
        }

        if (grab != null && grab.isSelected)
        {
            pending.Remove(block);
            yield break;
        }

        if (block.assignedColor == zoneColor)
        {
            Debug.Log(
                "Correct sorting: " + zoneColor
            );

            overlapping.Remove(block);
            pending.Remove(block);

            Destroy(block.gameObject);
        }
        else
        {
            Debug.Log(
                "Incorrect sorting. Block is " +
                block.assignedColor +
                ", zone is " + zoneColor
            );

            pending.Remove(block);
        }
    }
}
