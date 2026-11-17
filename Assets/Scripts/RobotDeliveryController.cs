
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RobotDeliveryController : MonoBehaviour
{
    public enum MotionMode
    {
        SlowStraight,
        SlowCurved,
        FastStraight,
        FastCurved
    }

    [Header("Experimental Condition")]
    public MotionMode motionMode = MotionMode.SlowStraight;

    [Header("Robot Joints")]
    public ArticulationBody[] joints = new ArticulationBody[6];

    [Header("Block Settings")]
    public GameObject blockPrefab;
    public Transform gripperAttachPoint;

    public Vector3 blockWorldScale =
        new Vector3(0.06f, 0.06f, 0.06f);

    [Header("Joint Poses (degrees)")]
    public float[] pickupPose =
        { -90f, 20f, -150f, 0f, 20f, -90f };

    public float[] handoffPose =
        { 0f, -30f, 45f, 0f, 20f, 0f };

    [Header("Speed Settings")]
    public float slowSpeed = 15f;
    public float fastSpeed = 45f;
    public float acceleration = 60f;

    [Header("Curved Motion Settings")]
    public float curveAmplitude = 15f;

    [Header("Movement")]
    public float arrivalTolerance = 4f;
    public float movementTimeout = 60f;

    [Header("Drive Settings")]
    public float stiffness = 10000f;
    public float damping = 1000f;
    public float forceLimit = 10000f;

    [Header("Timing")]
    public float pickupPause = 0.5f;
    public float settlingDelay = 0.3f;
    public float returnDelay = 1f;

    private float[] commandedAngles;
    private float[] commandedSpeeds;

    private GameObject heldBlock;
    private Rigidbody heldBlockRigidbody;

    private bool blockGrabbed;
    private bool handoffReady;

    private void Start()
    {
        Debug.Log("ROBOT DELIVERY CONTROLLER STARTED");

        if (joints == null || joints.Length != 6 ||
            pickupPose == null || pickupPose.Length != 6 ||
            handoffPose == null || handoffPose.Length != 6 ||
            blockPrefab == null || gripperAttachPoint == null)
        {
            Debug.LogError(
                "RobotDeliveryController: Missing references."
            );

            enabled = false;
            return;
        }

        commandedAngles = new float[6];
        commandedSpeeds = new float[6];

        for (int i = 0; i < 6; i++)
        {
            if (joints[i] == null ||
                joints[i].jointType !=
                    ArticulationJointType.RevoluteJoint)
            {
                Debug.LogError(
                    "Invalid robot joint: " + i
                );

                enabled = false;
                return;
            }

            ArticulationDrive drive = joints[i].xDrive;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            joints[i].xDrive = drive;
        }

        SyncCommandedAngles();
        StartCoroutine(DeliverySequence());
    }

    // =========================================
    // JOINT SYNCHRONIZATION
    // =========================================

    private void SyncCommandedAngles()
    {
        for (int i = 0; i < 6; i++)
        {
            commandedAngles[i] =
                joints[i].jointPosition[0] * Mathf.Rad2Deg;

            commandedSpeeds[i] = 0f;
        }
    }

    private float GetTarget(int index, float requested)
    {
        ArticulationDrive drive = joints[index].xDrive;

        if (joints[index].twistLock ==
            ArticulationDofLock.LimitedMotion)
        {
            return Mathf.Clamp(
                requested,
                drive.lowerLimit,
                drive.upperLimit
            );
        }

        return requested;
    }

    // =========================================
    // DELIVERY SEQUENCE
    // =========================================

    private IEnumerator DeliverySequence()
    {
        while (true)
        {
            blockGrabbed = false;
            handoffReady = false;

            SyncCommandedAngles();

            bool reached = false;

            Debug.Log("Moving to pickup position.");

            // Straight modes use base-first pickup.
            // Curved modes retain their original motion.

            if (IsStraightMode())
            {
                yield return MoveToPickupBaseFirst(
                    result => reached = result
                );
            }
            else
            {
                yield return MoveWithPattern(
                    pickupPose,
                    result => reached = result
                );
            }

            if (!reached)
            {
                Debug.LogWarning(
                    "Robot could not reach pickup pose."
                );

                LogJointErrors(pickupPose);
                yield break;
            }

            Debug.Log("Robot reached pickup pose.");

            yield return new WaitForSeconds(settlingDelay);
            yield return new WaitForSeconds(pickupPause);

            SpawnBlockInGripper();

            if (heldBlock == null)
            {
                Debug.LogError(
                    "Failed to spawn white block."
                );

                yield break;
            }

            SyncCommandedAngles();

            reached = false;

            Debug.Log("Moving to handoff position.");

            // Handoff uses the original movement
            // behavior for all four modes.

            yield return MoveWithPattern(
                handoffPose,
                result => reached = result
            );

            if (!reached)
            {
                Debug.LogWarning(
                    "Robot could not reach handoff pose."
                );

                LogJointErrors(handoffPose);
                yield break;
            }

            handoffReady = true;

            XRGrabInteractable grab =
                heldBlock.GetComponent<XRGrabInteractable>();

            if (grab == null)
            {
                Debug.LogError(
                    "Spawned block has no XR Grab Interactable."
                );

                yield break;
            }

            grab.enabled = true;

            Debug.Log("Block is now grabbable.");
            Debug.Log(
                "Robot reached handoff pose. Waiting for grab."
            );

            yield return new WaitUntil(
                () => blockGrabbed
            );

            yield return new WaitForSeconds(returnDelay);

            SyncCommandedAngles();

            Debug.Log("Returning to pickup position.");
        }
    }

    // =========================================
    // STRAIGHT PICKUP: BASE FIRST
    // =========================================

    private bool IsStraightMode()
    {
        return motionMode == MotionMode.SlowStraight ||
               motionMode == MotionMode.FastStraight;
    }

    private IEnumerator MoveToPickupBaseFirst(
        Action<bool> onComplete)
    {
        float speed =
            motionMode == MotionMode.SlowStraight
                ? slowSpeed
                : fastSpeed;

        // Stage 1:
        // Keep joints 1-5 at their current angles.
        // Rotate only the base to its pickup angle.

        float[] basePose = new float[6];

        for (int i = 0; i < 6; i++)
        {
            basePose[i] =
                joints[i].jointPosition[0] * Mathf.Rad2Deg;
        }

        basePose[0] = pickupPose[0];

        Debug.Log(
            "Pickup stage 1: Rotating base."
        );

        bool baseReached = false;

        yield return MoveToPose(
            basePose,
            speed,
            result => baseReached = result
        );

        if (!baseReached)
        {
            Debug.LogWarning(
                "Base rotation failed."
            );

            onComplete?.Invoke(false);
            yield break;
        }

        Debug.Log(
            "Pickup stage 1 complete."
        );

        // Reset commanded positions after stage 1.

        SyncCommandedAngles();

        // Stage 2:
        // Keep the base at its pickup angle.
        // Move joints 1-5 to the pickup pose.

        Debug.Log(
            "Pickup stage 2: Bending down."
        );

        bool bendReached = false;

        yield return MoveToPose(
            pickupPose,
            speed,
            result => bendReached = result
        );

        if (!bendReached)
        {
            Debug.LogWarning(
                "Pickup bending movement failed."
            );

            onComplete?.Invoke(false);
            yield break;
        }

        Debug.Log(
            "Pickup stage 2 complete."
        );

        onComplete?.Invoke(true);
    }

    // =========================================
    // BLOCK FOLLOWS GRIPPER
    // =========================================

    private void FixedUpdate()
    {
        if (heldBlock == null ||
            heldBlockRigidbody == null ||
            blockGrabbed)
        {
            return;
        }

        heldBlockRigidbody.MovePosition(
            gripperAttachPoint.position
        );

        heldBlockRigidbody.MoveRotation(
            gripperAttachPoint.rotation
        );
    }

    // =========================================
    // MOVEMENT MODE SELECTION
    // =========================================

    private IEnumerator MoveWithPattern(
        float[] destination,
        Action<bool> onComplete)
    {
        bool curved =
            motionMode == MotionMode.SlowCurved ||
            motionMode == MotionMode.FastCurved;

        float speed =
            motionMode == MotionMode.SlowStraight ||
            motionMode == MotionMode.SlowCurved
                ? slowSpeed
                : fastSpeed;

        if (!curved)
        {
            yield return MoveToPose(
                destination,
                speed,
                onComplete
            );

            yield break;
        }

        // Original continuous curved movement.

        float[] start =
            (float[])commandedAngles.Clone();

        float maxDistance = 0f;

        for (int i = 0; i < 6; i++)
        {
            maxDistance = Mathf.Max(
                maxDistance,
                Mathf.Abs(destination[i] - start[i])
            );
        }

        float duration = Mathf.Max(
            maxDistance / Mathf.Max(speed, 0.01f),
            0.1f
        );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            float smoothT = Mathf.SmoothStep(
                0f, 1f, t
            );

            for (int i = 0; i < 6; i++)
            {
                float linear = Mathf.Lerp(
                    start[i],
                    destination[i],
                    smoothT
                );

                float offset =
                    Mathf.Sin(Mathf.PI * smoothT) *
                    curveAmplitude *
                    ((i % 2 == 0) ? 1f : -1f);

                float target = GetTarget(
                    i,
                    linear + offset
                );

                ArticulationDrive drive = joints[i].xDrive;

                drive.target = target;
                joints[i].xDrive = drive;

                commandedAngles[i] = target;
            }

            yield return new WaitForFixedUpdate();
        }

        // Set exact final targets.

        for (int i = 0; i < 6; i++)
        {
            float target = GetTarget(
                i,
                destination[i]
            );

            ArticulationDrive drive = joints[i].xDrive;
            drive.target = target;
            joints[i].xDrive = drive;

            commandedAngles[i] = target;
        }

        float settleTime = 0f;

        while (!ActualJointsNear(destination) &&
               settleTime < movementTimeout)
        {
            settleTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        bool success = ActualJointsNear(destination);

        if (!success)
        {
            Debug.LogWarning(
                "Curved movement timed out."
            );
        }

        onComplete?.Invoke(success);
    }

    // =========================================
    // ORIGINAL STRAIGHT MOVEMENT
    // =========================================

    private IEnumerator MoveToPose(
        float[] pose,
        float speed,
        Action<bool> onComplete)
    {
        float elapsed = 0f;

        for (int i = 0; i < commandedSpeeds.Length; i++)
        {
            commandedSpeeds[i] = 0f;
        }

        while (elapsed < movementTimeout)
        {
            bool commandFinished = true;
            float dt = Time.fixedDeltaTime;

            for (int i = 0; i < 6; i++)
            {
                ArticulationDrive drive = joints[i].xDrive;

                float target = GetTarget(
                    i,
                    pose[i]
                );

                float remaining = Mathf.Abs(
                    target - commandedAngles[i]
                );

                float safeAcceleration =
                    Mathf.Max(acceleration, 0.01f);

                float desiredSpeed = Mathf.Min(
                    Mathf.Max(speed, 0.01f),
                    Mathf.Sqrt(
                        2f * safeAcceleration * remaining
                    )
                );

                commandedSpeeds[i] = Mathf.MoveTowards(
                    commandedSpeeds[i],
                    desiredSpeed,
                    safeAcceleration * dt
                );

                commandedAngles[i] = Mathf.MoveTowards(
                    commandedAngles[i],
                    target,
                    commandedSpeeds[i] * dt
                );

                drive.target = commandedAngles[i];
                joints[i].xDrive = drive;

                if (Mathf.Abs(
                    target - commandedAngles[i]) > 0.1f)
                {
                    commandFinished = false;
                }
            }

            if (commandFinished && ActualJointsNear(pose))
            {
                onComplete?.Invoke(true);
                yield break;
            }

            elapsed += dt;
            yield return new WaitForFixedUpdate();
        }

        Debug.LogWarning(
            "Straight movement timed out."
        );

        onComplete?.Invoke(false);
    }

    // =========================================
    // CHECK ACTUAL JOINT POSITIONS
    // =========================================

    private bool ActualJointsNear(float[] pose)
    {
        for (int i = 0; i < 6; i++)
        {
            float target = GetTarget(
                i,
                pose[i]
            );

            float actual =
                joints[i].jointPosition[0] * Mathf.Rad2Deg;

            if (Mathf.Abs(actual - target) >
                arrivalTolerance)
            {
                return false;
            }
        }

        return true;
    }

    // =========================================
    // DEBUG JOINT ERRORS
    // =========================================

    private void LogJointErrors(float[] pose)
    {
        for (int i = 0; i < 6; i++)
        {
            float target = GetTarget(
                i,
                pose[i]
            );

            float actual =
                joints[i].jointPosition[0] * Mathf.Rad2Deg;

            Debug.Log(
                $"Joint {i}: Target={target:F1}, " +
                $"Actual={actual:F1}, " +
                $"Error={Mathf.Abs(actual - target):F1}"
            );
        }
    }

    // =========================================
    // SPAWN BLOCK OUTSIDE ROBOT HIERARCHY
    // =========================================

    private void SpawnBlockInGripper()
    {
        if (heldBlock != null)
            return;

        heldBlock = Instantiate(
            blockPrefab,
            gripperAttachPoint.position,
            gripperAttachPoint.rotation
        );

        heldBlock.name = "RobotWhiteBlock";
        heldBlock.transform.localScale = blockWorldScale;

        heldBlockRigidbody =
            heldBlock.GetComponent<Rigidbody>();

        if (heldBlockRigidbody == null)
        {
            Debug.LogError(
                "WhiteBlock prefab needs a Rigidbody."
            );

            Destroy(heldBlock);
            heldBlock = null;
            return;
        }

        heldBlockRigidbody.isKinematic = true;
        heldBlockRigidbody.useGravity = false;
        heldBlockRigidbody.linearVelocity = Vector3.zero;
        heldBlockRigidbody.angularVelocity = Vector3.zero;

        foreach (Collider col in
            heldBlock.GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
        }

        BlockGrabHandler handler =
            heldBlock.GetComponent<BlockGrabHandler>();

        if (handler != null)
        {
            handler.Initialize(this);
        }
        else
        {
            Debug.LogError(
                "WhiteBlock prefab is missing BlockGrabHandler."
            );
        }

        XRGrabInteractable grab =
            heldBlock.GetComponent<XRGrabInteractable>();

        if (grab != null)
        {
            grab.enabled = false;
        }
        else
        {
            Debug.LogError(
                "WhiteBlock prefab is missing XR Grab Interactable."
            );
        }

        Debug.Log(
            "Robot block spawned outside robot hierarchy."
        );
    }

    // =========================================
    // PARTICIPANT GRAB EVENT
    // =========================================

    public void OnBlockGrabbed(GameObject block)
    {
        if (block != heldBlock || !handoffReady)
            return;

        blockGrabbed = true;
        handoffReady = false;

        heldBlock = null;
        heldBlockRigidbody = null;

        Debug.Log(
            "Participant grabbed block. " +
            "Robot will return after 1 second."
        );
    }
}
