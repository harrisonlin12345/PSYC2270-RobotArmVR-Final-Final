
using UnityEngine;

public class RobotMotionController : MonoBehaviour
{
    public enum MotionPattern
    {
        BackAndForth,
        Waypoints,
        Oscillating,
        Randomized
    }

    [Header("Robot Joints")]
    public ArticulationBody[] joints = new ArticulationBody[6];

    [Header("Motion Settings")]
    public MotionPattern pattern = MotionPattern.BackAndForth;
    public float speed = 20f;          // Degrees per second
    public float acceleration = 50f;   // Degrees per second squared
    public bool loop = true;

    [Header("Timing")]
    public float holdTime = 1f;

    [Header("Drive Settings")]
    public float stiffness = 10000f;
    public float damping = 1000f;
    public float forceLimit = 10000f;

    [Header("Joint Poses (degrees)")]
    public float[] poseA = { 0, -30, 45, 0, 20, 0 };
    public float[] poseB = { 30, -50, 70, 20, 40, 0 };
    public float[] poseC = { -30, -20, 60, -20, 30, 0 };

    private float[][] poses;
    private float[] currentTargets;
    private float[] currentSpeeds;

    private int targetIndex = 0;
    private float elapsed = 0f;
    private float holdTimer = 0f;
    private bool finished = false;

    void Start()
    {
        if (joints == null || joints.Length != 6 ||
            poseA == null || poseA.Length != 6 ||
            poseB == null || poseB.Length != 6 ||
            poseC == null || poseC.Length != 6)
        {
            Debug.LogError(
                "Expected six joints and six angles per pose."
            );
            enabled = false;
            return;
        }

        poses = new float[][] { poseA, poseB, poseC };

        currentTargets = new float[6];
        currentSpeeds = new float[6];

        for (int i = 0; i < 6; i++)
        {
            if (joints[i] == null ||
                joints[i].jointType !=
                ArticulationJointType.RevoluteJoint)
            {
                Debug.LogError(
                    "Joint " + i +
                    " is missing or not revolute."
                );
                enabled = false;
                return;
            }

            var drive = joints[i].xDrive;

            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;

            joints[i].xDrive = drive;

            currentTargets[i] =
                joints[i].jointPosition[0] * Mathf.Rad2Deg;
        }
    }

    void FixedUpdate()
    {
        if (finished)
            return;

        // Continuous sinusoidal motion
        if (pattern == MotionPattern.Oscillating)
        {
            elapsed += Time.fixedDeltaTime;

            float[] target = new float[6];

            float frequency =
                Mathf.Max(speed, 0.1f) / 360f;

            float blend = 0.5f + 0.5f *
                Mathf.Sin(
                    2f * Mathf.PI * frequency * elapsed
                );

            for (int i = 0; i < 6; i++)
            {
                target[i] = Mathf.Lerp(
                    poseA[i],
                    poseB[i],
                    blend
                );
            }

            ApplyTargets(target);

            return;
        }

        // Move toward the current destination
        float[] destination = poses[targetIndex];

        MoveTowardsPose(destination);

        // Check whether the commanded movement has finished
        if (CommandFinished(destination))
        {
            holdTimer += Time.fixedDeltaTime;

            if (holdTimer >= holdTime)
            {
                holdTimer = 0f;
                AdvanceToNextPose();
            }
        }
        else
        {
            holdTimer = 0f;
        }
    }

    void MoveTowardsPose(float[] destination)
    {
        float dt = Time.fixedDeltaTime;

        for (int i = 0; i < 6; i++)
        {
            float target = GetLimitedTarget(
                i, destination[i]
            );

            float remaining = Mathf.Abs(
                target - currentTargets[i]
            );

            float safeAcceleration =
                Mathf.Max(acceleration, 0.01f);

            // Reduce commanded speed near the target
            float desiredSpeed = Mathf.Min(
                Mathf.Max(speed, 0f),
                Mathf.Sqrt(
                    2f * safeAcceleration * remaining
                )
            );

            currentSpeeds[i] = Mathf.MoveTowards(
                currentSpeeds[i],
                desiredSpeed,
                safeAcceleration * dt
            );

            currentTargets[i] = Mathf.MoveTowards(
                currentTargets[i],
                target,
                currentSpeeds[i] * dt
            );
        }

        ApplyTargets(currentTargets);
    }

    float GetLimitedTarget(int index, float angle)
    {
        var drive = joints[index].xDrive;

        if (joints[index].twistLock ==
            ArticulationDofLock.LimitedMotion)
        {
            return Mathf.Clamp(
                angle,
                drive.lowerLimit,
                drive.upperLimit
            );
        }

        return angle;
    }

    void ApplyTargets(float[] targets)
    {
        for (int i = 0; i < 6; i++)
        {
            var drive = joints[i].xDrive;

            drive.target = GetLimitedTarget(
                i, targets[i]
            );

            joints[i].xDrive = drive;
        }
    }

    bool CommandFinished(float[] destination)
    {
        for (int i = 0; i < 6; i++)
        {
            float target = GetLimitedTarget(
                i, destination[i]
            );

            if (Mathf.Abs(
                currentTargets[i] - target
            ) > 0.1f)
            {
                return false;
            }
        }

        return true;
    }

    void AdvanceToNextPose()
    {
        // Reset commanded speed before the next movement
        for (int i = 0; i < currentSpeeds.Length; i++)
        {
            currentSpeeds[i] = 0f;
        }

        switch (pattern)
        {
            case MotionPattern.BackAndForth:

                if (targetIndex == 0)
                {
                    targetIndex = 1;
                }
                else
                {
                    targetIndex = 0;

                    if (!loop)
                        finished = true;
                }

                break;

            case MotionPattern.Waypoints:

                targetIndex++;

                if (targetIndex >= poses.Length)
                {
                    targetIndex = 0;

                    if (!loop)
                        finished = true;
                }

                break;

            case MotionPattern.Randomized:

                if (!loop)
                {
                    finished = true;
                    break;
                }

                int next = Random.Range(
                    0, poses.Length - 1
                );

                if (next >= targetIndex)
                    next++;

                targetIndex = next;

                break;
        }
    }
}
