
using UnityEngine;

public class UR3JointTest : MonoBehaviour
{
    public ArticulationBody joint;

    [Header("Test Settings")]
    public float targetAngle = 30f;
    public float stiffness = 10000f;
    public float damping = 1000f;
    public float forceLimit = 10000f;

    void Start()
    {
        if (joint == null)
        {
            Debug.LogError("Assign a joint in the Inspector!");
            enabled = false;
            return;
        }

        Debug.Log("Joint type: " + joint.jointType);

        if (joint.jointType != ArticulationJointType.RevoluteJoint)
        {
            Debug.LogError("Expected a revolute joint.");
            enabled = false;
            return;
        }

        var drive = joint.xDrive;

        drive.stiffness = stiffness;
        drive.damping = damping;
        drive.forceLimit = forceLimit;

        // Clamp only when the joint uses limited motion.
        if (joint.twistLock == ArticulationDofLock.LimitedMotion)
        {
            targetAngle = Mathf.Clamp(
                targetAngle,
                drive.lowerLimit,
                drive.upperLimit
            );
        }

        drive.target = targetAngle;
        joint.xDrive = drive;

        Debug.Log("Commanded joint target: " + drive.target);
    }
}
