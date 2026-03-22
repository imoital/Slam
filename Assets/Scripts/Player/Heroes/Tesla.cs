using UnityEngine;

public class Tesla : Hero
{
    private Transform magnet;

    private GameObject ball;
    private Rigidbody ballRb;
    private Rigidbody playerRb;

    private Collider[] teslaColliders;
    private Collider[] ballColliders;

    private bool isUsingPower = false;
    private bool isBallCaptured = false;
    private bool isBallSettling = false;
    private bool lastPowerPressed = false;

    private Vector2 pullAnchorSmoothXZ;
    private bool pullAnchorSmoothActive;

    private Vector3 pullDirSmooth;
    private bool pullDirSmoothActive;

    private int holdSurfaceColliderIndex = -1;

    private const float POWER_DURATION = 2f;
    private const float POWER_COOLDOWN = 1f;

    // Pull tuning
    private const float MAGNET_RANGE = 1f;
    private const float MAX_PULL_SPEED = 18f;
    private const float PULL_ANCHOR_SMOOTH_HZ = 9f;
    private const float PULL_DIR_SMOOTH_HZ = 7f;
    private const float PULL_DIR_ORBIT_SPEED_REF = 6f;
    private const float PULL_DIR_ORBIT_BOOST = 5f;
    private const float HOLD_SURFACE_SWITCH_MARGIN = 0.09f;
    private const float PULL_VEL_RESPONSE = 11f;
    private const float ORBIT_FEEDFORWARD = 0.92f;
    private const float PULL_MAX_ACCEL = 30f;
    private const float PULL_SPIN_DAMP_HZ = 10f;

    // Capture / hold tuning
    private const float CAPTURE_DISTANCE = 0.12f;
    private const float LOCK_DISTANCE = 0.04f;
    private const float SETTLE_SPEED = 20f;
    private const float RELEASE_VELOCITY_MULTIPLIER = 1f;
    private const float HOLD_VISUAL_GAP = 0.05f;

    public Tesla(Player_Behaviour player)
    {
        hero_prefab = Resources.Load<GameObject>("Heroes/Tesla/Tesla");
        this.player = player;

        playerRb = player.GetComponent<Rigidbody>();
        player.setDashCooldown(POWER_COOLDOWN);
    }

    public override void Start()
    {
        AI.RegisterHero(this);

        magnet = FindDeepChild(player.transform.Find("Mesh"), "Magnet");

        if (magnet != null)
        {
            ParticleSystem ps = magnet.GetComponent<ParticleSystem>();
            if (ps != null)
                ps.Stop();
        }

        team = player.team;

        CacheBall();
        teslaColliders = player.GetComponentsInChildren<Collider>(true);
    }

    public override void UsePower(PlayerController.Commands commands)
    {
        CacheBall();

        bool powerPressed = commands.dash > 0.5f;

        // Rising edge only
        if (powerPressed && !lastPowerPressed)
        {
            if (!isUsingPower && player.IsCooldownOver())
            {
                StartPower();
            }
            else if (isUsingPower)
            {
                StopPower();
            }
        }

        lastPowerPressed = powerPressed;

        if (!isUsingPower)
            return;

        if (player.IsPowerTimerOver())
        {
            StopPower();
            return;
        }

        if (ballRb == null)
            return;

        if (isBallCaptured)
        {
            HoldCapturedBall();
        }
        else
        {
            ApplyMagnetPull();
            TryCaptureBall();
        }
    }

    private void StartPower()
    {
        isUsingPower = true;
        isBallCaptured = false;
        isBallSettling = false;

        CacheBall();
        SetBallCollisionWithTesla(true);
        pullAnchorSmoothActive = false;
        pullDirSmoothActive = false;
        holdSurfaceColliderIndex = -1;

        power_cooldown = Time.time + POWER_COOLDOWN;
        player.setPowerActivatedTimer(POWER_DURATION);

        // If Tesla is already pressing into the ball when power starts,
        // skip settle and latch it immediately.
        if (ballRb != null && player.IsCollidingWithBall() && IsBallInFront())
        {
            CaptureBallImmediate();
        }

        EmmitPowerFX("power_up");
    }

    private void StopPower()
    {
        if (isBallCaptured)
            ReleaseCapturedBall();
        else
            SetBallCollisionWithTesla(false);

        isUsingPower = false;
        isBallCaptured = false;
        isBallSettling = false;
        pullAnchorSmoothActive = false;
        pullDirSmoothActive = false;
        holdSurfaceColliderIndex = -1;

        EmmitPowerFX("power_down");
        player.setPowerActivatedTimer(0f);
        player.resetPowerBar();
    }

    private void ApplyMagnetPull()
    {
        Vector3 attractPoint = GetHoldPoint();
        Vector3 toTarget = attractPoint - ballRb.position;
        toTarget.y = 0f;

        float sqrDistance = toTarget.sqrMagnitude;
        if (sqrDistance > MAGNET_RANGE * MAGNET_RANGE)
            return;

        float distance = Mathf.Sqrt(sqrDistance);
        if (distance < 0.001f)
            return;

        Vector3 dir = toTarget / distance;
        dir.y = 0f;

        Vector3 playerHoriz = Vector3.zero;
        if (playerRb != null)
        {
            playerHoriz = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
            if (magnet != null)
            {
                Vector3 magnetOffset = magnet.position - playerRb.worldCenterOfMass;
                Vector3 spinCarry = Vector3.Cross(playerRb.angularVelocity, magnetOffset);
                spinCarry.y = 0f;
                playerHoriz += spinCarry;
            }
        }

        Vector3 orbitTangentRaw = playerHoriz - Vector3.Project(playerHoriz, dir);
        float orbitBoost = Mathf.Clamp01(orbitTangentRaw.magnitude / PULL_DIR_ORBIT_SPEED_REF) * PULL_DIR_ORBIT_BOOST;

        if (!pullDirSmoothActive)
        {
            pullDirSmooth = dir;
            pullDirSmoothActive = true;
        }
        else
        {
            float dirHz = PULL_DIR_SMOOTH_HZ + orbitBoost;
            float dirA = 1f - Mathf.Exp(-dirHz * Time.deltaTime);
            pullDirSmooth = Vector3.Slerp(pullDirSmooth, dir, dirA);
            pullDirSmooth.y = 0f;
            if (pullDirSmooth.sqrMagnitude > 0.0001f)
                pullDirSmooth.Normalize();
            else
                pullDirSmooth = dir;
        }

        float t = 1f - Mathf.Clamp01(distance / MAGNET_RANGE);
        float desiredSpeed = Mathf.Lerp(MAX_PULL_SPEED * 0.28f, MAX_PULL_SPEED, t);

        Vector3 alongPull = Vector3.Project(playerHoriz, pullDirSmooth);
        Vector3 tangent = playerHoriz - alongPull;

        Vector3 v = ballRb.linearVelocity;
        Vector3 horizVel = new Vector3(v.x, 0f, v.z);
        Vector3 desiredVel = pullDirSmooth * desiredSpeed + tangent * ORBIT_FEEDFORWARD;
        Vector3 accel = (desiredVel - horizVel) * PULL_VEL_RESPONSE;

        if (accel.sqrMagnitude > PULL_MAX_ACCEL * PULL_MAX_ACCEL)
            accel = accel.normalized * PULL_MAX_ACCEL;

        ballRb.AddForce(accel, ForceMode.Acceleration);
        ClampBallHorizontalSpeed();

        float spin = 1f - Mathf.Exp(-PULL_SPIN_DAMP_HZ * Time.deltaTime);
        ballRb.angularVelocity = Vector3.Lerp(ballRb.angularVelocity, Vector3.zero, spin);
    }

    private void TryCaptureBall()
    {
        Vector3 holdPoint = GetHoldPoint();
        Vector3 delta = holdPoint - ballRb.position;
        delta.y = 0f;

        bool closeEnough = delta.sqrMagnitude <= CAPTURE_DISTANCE * CAPTURE_DISTANCE;
        bool touching = player.IsCollidingWithBall();
        bool inFront = IsBallInFront();

        if (touching && inFront)
        {
            CaptureBallImmediate();
            return;
        }

        if (closeEnough)
        {
            CaptureBall();
        }
    }

    private void CaptureBall()
    {
        isBallCaptured = true;
        isBallSettling = true;

        SetBallCollisionWithTesla(true);
        pullAnchorSmoothActive = false;
        pullDirSmoothActive = false;
        holdSurfaceColliderIndex = -1;

        // Clear shove momentum on XZ, preserve Y if needed
        ballRb.linearVelocity = new Vector3(0f, ballRb.linearVelocity.y, 0f);
        ballRb.angularVelocity = Vector3.zero;
    }

    private void CaptureBallImmediate()
    {
        isBallCaptured = true;
        isBallSettling = false;

        SetBallCollisionWithTesla(true);
        pullAnchorSmoothActive = false;
        pullDirSmoothActive = false;
        holdSurfaceColliderIndex = -1;

        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        Vector3 holdPoint = ComputeHoldPoint();
        ballRb.position = holdPoint;
    }

    private void HoldCapturedBall()
    {
        Vector3 holdPoint = GetHoldPoint();
        Vector3 toHold = holdPoint - ballRb.position;
        toHold.y = 0f;

        float distance = toHold.magnitude;

        if (isBallSettling)
        {
            if (distance <= LOCK_DISTANCE)
            {
                isBallSettling = false;
                ballRb.linearVelocity = Vector3.zero;
                ballRb.angularVelocity = Vector3.zero;
                ballRb.MovePosition(holdPoint);
                return;
            }

            Vector3 desiredVelocity = toHold.normalized * SETTLE_SPEED;

            if (playerRb != null)
            {
                Vector3 playerHorizontal = new Vector3(
                    playerRb.linearVelocity.x,
                    0f,
                    playerRb.linearVelocity.z
                );

                desiredVelocity += playerHorizontal;
            }

            ballRb.linearVelocity = new Vector3(
                desiredVelocity.x,
                ballRb.linearVelocity.y,
                desiredVelocity.z
            );

            ballRb.angularVelocity = Vector3.zero;
            return;
        }

        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
        ballRb.MovePosition(holdPoint);
    }

    private void ReleaseCapturedBall()
    {
        SetBallCollisionWithTesla(false);

        if (ballRb != null && playerRb != null)
        {
            Vector3 playerVelocity = playerRb.linearVelocity;
            Vector3 currentBallVelocity = ballRb.linearVelocity;

            ballRb.linearVelocity = new Vector3(
                playerVelocity.x * RELEASE_VELOCITY_MULTIPLIER,
                currentBallVelocity.y,
                playerVelocity.z * RELEASE_VELOCITY_MULTIPLIER
            );
        }
    }

    private bool IsBallInFront()
    {
        if (ballRb == null)
            return false;

        Vector3 toBall = ballRb.position - player.transform.position;
        toBall.y = 0f;

        if (toBall.sqrMagnitude < 0.0001f)
            return true;

        Vector3 forward = player.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        else
            forward.Normalize();

        return Vector3.Dot(toBall.normalized, forward) > 0.15f;
    }

    private float GetBallRadius()
    {
        if (ballColliders != null)
        {
            for (int i = 0; i < ballColliders.Length; i++)
            {
                Collider col = ballColliders[i];
                if (col == null || !col.enabled || col.isTrigger)
                    continue;

                Bounds b = col.bounds;
                return Mathf.Max(b.extents.x, b.extents.z);
            }
        }

        return 0.25f;
    }

    private Vector3 GetHoldPoint()
    {
        Vector3 raw = ComputeHoldPoint();

        if (isBallCaptured || !isUsingPower)
            return raw;

        float a = 1f - Mathf.Exp(-PULL_ANCHOR_SMOOTH_HZ * Time.deltaTime);
        Vector2 targetXZ = new Vector2(raw.x, raw.z);

        if (!pullAnchorSmoothActive)
        {
            pullAnchorSmoothXZ = targetXZ;
            pullAnchorSmoothActive = true;
        }
        else
        {
            pullAnchorSmoothXZ = Vector2.Lerp(pullAnchorSmoothXZ, targetXZ, a);
        }

        return new Vector3(pullAnchorSmoothXZ.x, raw.y, pullAnchorSmoothXZ.y);
    }

    private Vector3 ComputeHoldPoint()
    {
        Vector3 forward = player.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        else
            forward.Normalize();

        Vector3 origin = magnet != null ? magnet.position : player.transform.position;
        Vector3 probePoint = origin + forward * 3f;

        Vector3 frontSurfacePoint = origin;
        bool foundSurface = false;
        float bestForwardDot = float.NegativeInfinity;
        int bestColliderIndex = -1;

        if (teslaColliders != null)
        {
            for (int i = 0; i < teslaColliders.Length; i++)
            {
                Collider col = teslaColliders[i];
                if (col == null || !col.enabled || col.isTrigger)
                    continue;

                Vector3 p = col.ClosestPoint(probePoint);
                Vector3 fromOrigin = p - origin;
                fromOrigin.y = 0f;

                float forwardDot = Vector3.Dot(fromOrigin, forward);

                if (!foundSurface || forwardDot > bestForwardDot)
                {
                    foundSurface = true;
                    bestForwardDot = forwardDot;
                    frontSurfacePoint = p;
                    bestColliderIndex = i;
                }
            }

            if (foundSurface && bestColliderIndex >= 0)
            {
                if (holdSurfaceColliderIndex >= 0 && holdSurfaceColliderIndex != bestColliderIndex)
                {
                    Collider prevCol = teslaColliders[holdSurfaceColliderIndex];
                    if (prevCol != null && prevCol.enabled && !prevCol.isTrigger)
                    {
                        Vector3 pPrev = prevCol.ClosestPoint(probePoint);
                        Vector3 fromPrev = pPrev - origin;
                        fromPrev.y = 0f;
                        float prevDot = Vector3.Dot(fromPrev, forward);

                        if (prevDot >= bestForwardDot - HOLD_SURFACE_SWITCH_MARGIN)
                        {
                            frontSurfacePoint = pPrev;
                        }
                        else
                        {
                            holdSurfaceColliderIndex = bestColliderIndex;
                        }
                    }
                    else
                    {
                        holdSurfaceColliderIndex = bestColliderIndex;
                    }
                }
                else
                {
                    holdSurfaceColliderIndex = bestColliderIndex;
                }
            }
        }

        float ballRadius = GetBallRadius();
        Vector3 holdPoint = frontSurfacePoint + forward * (ballRadius + HOLD_VISUAL_GAP);

        if (ballRb != null)
            holdPoint.y = ballRb.position.y;
        else if (ball != null)
            holdPoint.y = ball.transform.position.y;

        return holdPoint;
    }

    private void ClampBallHorizontalSpeed()
    {
        Vector3 v = ballRb.linearVelocity;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);

        if (horizontal.magnitude > MAX_PULL_SPEED)
        {
            horizontal = horizontal.normalized * MAX_PULL_SPEED;
            ballRb.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);
        }
    }

    private void SetBallCollisionWithTesla(bool ignore)
    {
        CacheBall();

        if (teslaColliders == null || teslaColliders.Length == 0)
            teslaColliders = player.GetComponentsInChildren<Collider>(true);

        if (ball != null && (ballColliders == null || ballColliders.Length == 0))
            ballColliders = ball.GetComponentsInChildren<Collider>(true);

        if (teslaColliders == null || ballColliders == null)
            return;

        for (int i = 0; i < teslaColliders.Length; i++)
        {
            Collider teslaCol = teslaColliders[i];
            if (teslaCol == null || !teslaCol.enabled || teslaCol.isTrigger)
                continue;

            for (int j = 0; j < ballColliders.Length; j++)
            {
                Collider ballCol = ballColliders[j];
                if (ballCol == null || !ballCol.enabled || ballCol.isTrigger)
                    continue;

                Physics.IgnoreCollision(teslaCol, ballCol, ignore);
            }
        }
    }

    private void CacheBall()
    {
        if (ball != null && ballRb != null)
            return;

        ball = GameObject.FindWithTag("ball");

        if (ball != null)
        {
            ballRb = ball.GetComponent<Rigidbody>();
            ballColliders = ball.GetComponentsInChildren<Collider>(true);
        }
    }

    public override void EmmitPowerFX(string type = "none")
    {
        if (magnet == null)
            return;

        ParticleSystem ps = magnet.GetComponent<ParticleSystem>();
        if (ps == null)
            return;

        if (type == "power_up")
            ps.Play();
        else if (type == "power_down")
            ps.Stop();
    }

    public override void Update()
    {
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null) return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == childName)
                return child;

            Transform nestedChild = FindDeepChild(child, childName);
            if (nestedChild != null)
                return nestedChild;
        }

        return null;
    }
}