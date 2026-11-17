
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class BlockGrabHandler : MonoBehaviour
{
    private RobotDeliveryController robot;
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;

    private bool hasBeenGrabbed = false;

    private void Awake()
    {
        grabInteractable =
            GetComponent<XRGrabInteractable>();

        rb = GetComponent<Rigidbody>();

        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    public void Initialize(RobotDeliveryController controller)
    {
        robot = controller;
        hasBeenGrabbed = false;
    }

    // =========================================
    // WHEN PARTICIPANT GRABS BLOCK
    // =========================================

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        Debug.Log(
            "XR SELECT ENTERED: " + gameObject.name
        );

        if (hasBeenGrabbed)
            return;

        hasBeenGrabbed = true;

        // Ensure the block is not parented to
        // any part of the robot.

        transform.SetParent(null, true);

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (robot != null)
        {
            robot.OnBlockGrabbed(gameObject);
        }

        Debug.Log("Block grabbed by participant.");
    }

    // =========================================
    // WHEN PARTICIPANT RELEASES BLOCK
    // =========================================

    private void OnReleased(SelectExitEventArgs args)
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        Debug.Log("Block released. Gravity enabled.");
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }
}
