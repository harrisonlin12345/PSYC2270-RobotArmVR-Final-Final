
using UnityEngine;

public class ColoredBlock : MonoBehaviour
{
    public enum BlockColor
    {
        Unassigned,
        Red,
        Blue,
        Green
    }

    public BlockColor assignedColor = BlockColor.Unassigned;

    public void AssignColor(BlockColor newColor)
    {
        if (assignedColor != BlockColor.Unassigned)
            return;

        assignedColor = newColor;

        Color displayColor = Color.white;

        switch (newColor)
        {
            case BlockColor.Red:
                displayColor = Color.red;
                break;

            case BlockColor.Blue:
                displayColor = Color.blue;
                break;

            case BlockColor.Green:
                displayColor = Color.green;
                break;
        }

        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            foreach (Material mat in rend.materials)
            {
                mat.color = displayColor;
            }
        }

        Debug.Log("Block assigned: " + newColor);
    }
}
