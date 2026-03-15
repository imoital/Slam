using UnityEngine;
using System.Collections;

public class Ball_Behaviour : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected Animator animator;

    [Header("Main action timing")]
    [SerializeField] private float minMainActionInterval = 2f;
    [SerializeField] private float lookDuration = 2.5f;
    [SerializeField] private float rollingEyesDuration = 5f;
    [SerializeField] private float rollingEyesCooldown = 4f;

    [Header("Blink timing")]
    [SerializeField] private float minBlinkInterval = 2.5f;
    [SerializeField] private float maxBlinkInterval = 5f;
    [SerializeField] private float blinkDurationFallback = 0.18f;
    [SerializeField] private float tiredDurationFallback = 2.5f;

    protected bool game_restarted = true;
    protected bool animation_finished = true;
    protected bool rolling_eyes = false;
    protected bool is_looking_somewhere = false;

    private int current_area = 7;
    protected GameObject last_player_touched;

    private float nextMainActionTime;
    private float nextBlinkTime;
    private float lastRollingEyesTime = float.NegativeInfinity;

    private Coroutine baseReturnRoutine;
    private Coroutine eyelidReturnRoutine;
    private Coroutine rollingEyesRoutine;

    private int baseLayer = -1;
    private int eyelidsLayer = -1;

    private AnimationClip blinkEyelidClip;
    private AnimationClip tiredEyelidClip;
    private Collider myCollider;
    private Forcefield forcefield;

    private enum EyelidState
    {
        Idle,
        Blinking,
        Tired
    }

    private EyelidState eyelidState = EyelidState.Idle;
    private bool IsTiredActive => eyelidState == EyelidState.Tired;

    // State names must match your Animator exactly
    private static readonly int BaseIdleHash = Animator.StringToHash("Base.Idle");
    private static readonly int BaseLookLeftHash = Animator.StringToHash("Base.look_left");
    private static readonly int BaseLookRightHash = Animator.StringToHash("Base.look_right");
    private static readonly int BaseLookUpHash = Animator.StringToHash("Base.look_up");
    private static readonly int BaseLookDownHash = Animator.StringToHash("Base.look_down");
    private static readonly int BaseBlinkPupilHash = Animator.StringToHash("Base.BlinkPupil");
    private static readonly int BaseRollingEyesHash = Animator.StringToHash("Base.Rolling Eyes");

    private static readonly int EyelidsIdleHash = Animator.StringToHash("Eyelids.Idle");
    private static readonly int EyelidsBlinkHash = Animator.StringToHash("Eyelids.BlinkEyelid");
    private static readonly int EyelidsTiredHash = Animator.StringToHash("Eyelids.Tired");

    private const string BlinkClipName = "BlinkEyelid";
    private const string TiredClipName = "TiredEyelid";

    protected void Start()
    {
        if (!InitializeAnimator()) return;
        if (!InitializeLayers()) return;
        if (!InitializeCollider()) return;

        CacheReferences();
        CacheAnimationClips();
        IgnoreCenterCollisions();

        ResetAnimationState();

        nextMainActionTime = Time.time + minMainActionInterval;
        nextBlinkTime = Time.time + Random.Range(minBlinkInterval, maxBlinkInterval);
    }

    protected void Update()
    {
        if (animator == null) return;

        float now = Time.time;

        HandleBlink(now);
        HandleMainActions(now);
    }

    private bool InitializeAnimator()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (animator != null) return true;

        Debug.LogError("Ball_Behaviour: Animator not found on " + gameObject.name, this);
        enabled = false;
        return false;
    }

    private bool InitializeLayers()
    {
        baseLayer = animator.GetLayerIndex("Base");
        eyelidsLayer = animator.GetLayerIndex("Eyelids");

        if (baseLayer >= 0 && eyelidsLayer >= 0) return true;

        Debug.LogError(
            "Ball_Behaviour: Required animator layers 'Base' and/or 'Eyelids' not found on " + gameObject.name,
            this
        );
        enabled = false;
        return false;
    }

    private bool InitializeCollider()
    {
        myCollider = GetComponent<Collider>();

        if (myCollider != null) return true;

        Debug.LogError("Ball_Behaviour: Collider not found on " + gameObject.name, this);
        enabled = false;
        return false;
    }

    private void CacheReferences()
    {
        GameObject forcefieldObject = GameObject.FindGameObjectWithTag("forcefield");
        if (forcefieldObject != null)
        {
            forcefield = forcefieldObject.GetComponent<Forcefield>();
        }
    }

    private void CacheAnimationClips()
    {
        blinkEyelidClip = FindClipByName(BlinkClipName);
        tiredEyelidClip = FindClipByName(TiredClipName);

        if (blinkEyelidClip == null)
        {
            Debug.LogWarning("Ball_Behaviour: Blink clip not found. Falling back to blinkDurationFallback.", this);
        }

        if (tiredEyelidClip == null)
        {
            Debug.LogWarning("Ball_Behaviour: Tired clip not found. Falling back to tiredDurationFallback.", this);
        }
    }

    private void IgnoreCenterCollisions()
    {
        GameObject[] centerPlanes = GameObject.FindGameObjectsWithTag("center-plane");
        GameObject centerCircleLeft = GameObject.FindGameObjectWithTag("center-circle-left");
        GameObject centerCircleRight = GameObject.FindGameObjectWithTag("center-circle-right");

        for (int i = 0; i < centerPlanes.Length; i++)
        {
            Collider planeCollider = centerPlanes[i].GetComponent<Collider>();
            if (planeCollider != null)
            {
                Physics.IgnoreCollision(planeCollider, myCollider);
            }
        }

        if (centerCircleLeft != null)
        {
            Collider leftCollider = centerCircleLeft.GetComponent<Collider>();
            if (leftCollider != null)
            {
                Physics.IgnoreCollision(leftCollider, myCollider);
            }
        }

        if (centerCircleRight != null)
        {
            Collider rightCollider = centerCircleRight.GetComponent<Collider>();
            if (rightCollider != null)
            {
                Physics.IgnoreCollision(rightCollider, myCollider);
            }
        }
    }

    private void ResetAnimationState()
    {
        PlayBase(BaseIdleHash);
        PlayEyelids(EyelidsIdleHash);

        rolling_eyes = false;
        is_looking_somewhere = false;
        animation_finished = true;
        eyelidState = EyelidState.Idle;
    }

    private void HandleMainActions(float now)
    {
        if (rolling_eyes || !animation_finished) return;
        if (now < nextMainActionTime) return;

        nextMainActionTime = now + minMainActionInterval;

        int random = Random.Range(0, 100);

        if (random < 1)
        {
            PlayTired();
        }
        else if (random < 50)
        {
            PlayRandomLook();
        }
    }

    private void HandleBlink(float now)
    {
        if (now < nextBlinkTime) return;

        nextBlinkTime = now + Random.Range(minBlinkInterval, maxBlinkInterval);

        if (rolling_eyes) return;
        if (eyelidState != EyelidState.Idle) return;

        PlayBlink();
    }

    private void PlayRandomLook()
    {
        StopRoutine(ref baseReturnRoutine);

        int direction = Random.Range(0, 4);
        switch (direction)
        {
            case 0:
                PlayBase(BaseLookLeftHash);
                break;
            case 1:
                PlayBase(BaseLookRightHash);
                break;
            case 2:
                PlayBase(BaseLookUpHash);
                break;
            case 3:
                PlayBase(BaseLookDownHash);
                break;
        }

        is_looking_somewhere = true;
        animation_finished = false;

        baseReturnRoutine = StartCoroutine(ReturnBaseToIdleAfter(lookDuration, clearMainLock: true));
    }

    private void PlayTired()
    {
        if (eyelidState != EyelidState.Idle) return;

        StopRoutine(ref eyelidReturnRoutine);

        eyelidState = EyelidState.Tired;
        animation_finished = false;

        PlayEyelids(EyelidsTiredHash);

        float tiredDuration = GetTiredDuration();
        eyelidReturnRoutine = StartCoroutine(ReturnTiredToIdleAfter(tiredDuration));
    }

    private void PlayBlink()
    {
        if (eyelidState != EyelidState.Idle) return;

        StopRoutine(ref eyelidReturnRoutine);

        eyelidState = EyelidState.Blinking;

        float blinkDuration = GetBlinkDuration();

        PlayEyelids(EyelidsBlinkHash);
        eyelidReturnRoutine = StartCoroutine(FinishBlinkAfter(blinkDuration));

        if (!is_looking_somewhere && !rolling_eyes)
        {
            StopRoutine(ref baseReturnRoutine);
            PlayBase(BaseBlinkPupilHash);
            baseReturnRoutine = StartCoroutine(ReturnBaseToIdleAfter(blinkDuration, clearMainLock: false));
        }
    }

    private float GetBlinkDuration()
    {
        return GetClipDuration(blinkEyelidClip, blinkDurationFallback);
    }

    private float GetTiredDuration()
    {
        return GetClipDuration(tiredEyelidClip, tiredDurationFallback);
    }

    private float GetClipDuration(AnimationClip clip, float fallback)
    {
        if (clip != null && clip.length > 0f)
        {
            return clip.length;
        }

        return fallback;
    }

    private AnimationClip FindClipByName(string clipName)
    {
        if (animator == null) return null;

        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller == null) return null;

        AnimationClip[] clips = controller.animationClips;
        if (clips == null || clips.Length == 0) return null;

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && string.Equals(clip.name, clipName, System.StringComparison.Ordinal))
            {
                return clip;
            }
        }

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && string.Equals(clip.name, clipName, System.StringComparison.OrdinalIgnoreCase))
            {
                return clip;
            }
        }

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && clip.name.IndexOf(clipName, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return clip;
            }
        }

        return null;
    }

    protected virtual void CourtCollision(Vector3 point)
    {
        if (forcefield != null)
        {
            forcefield.BallCollition(point);
        }

        float now = Time.time;
        if (now - lastRollingEyesTime < rollingEyesCooldown) return;
        if (rolling_eyes) return;
        if (animator == null) return;

        // 25% chance to play rolling eyes on each eligible wall hit
        if (Random.value >= 0.25f) return;

        rolling_eyes = true;
        animation_finished = false;
        is_looking_somewhere = false;

        lastRollingEyesTime = now;
        nextMainActionTime = now + minMainActionInterval;

        StopRoutine(ref baseReturnRoutine);
        StopRoutine(ref rollingEyesRoutine);

        PlayBase(BaseRollingEyesHash);
        rollingEyesRoutine = StartCoroutine(EndRollingEyesAfterDelay(rollingEyesDuration));
    }

    private IEnumerator ReturnBaseToIdleAfter(float delay, bool clearMainLock)
    {
        yield return new WaitForSeconds(delay);

        PlayBase(BaseIdleHash);
        is_looking_somewhere = false;

        if (clearMainLock && !rolling_eyes && !IsTiredActive)
        {
            animation_finished = true;
        }

        baseReturnRoutine = null;
    }

    private IEnumerator FinishBlinkAfter(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (eyelidState != EyelidState.Blinking)
        {
            eyelidReturnRoutine = null;
            yield break;
        }

        PlayEyelids(EyelidsIdleHash);
        eyelidState = EyelidState.Idle;

        eyelidReturnRoutine = null;
    }

    private IEnumerator ReturnTiredToIdleAfter(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (eyelidState != EyelidState.Tired)
        {
            eyelidReturnRoutine = null;
            yield break;
        }

        eyelidState = EyelidState.Idle;
        PlayEyelids(EyelidsIdleHash);

        if (!rolling_eyes)
        {
            animation_finished = true;
        }

        eyelidReturnRoutine = null;
    }

    private IEnumerator EndRollingEyesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        rolling_eyes = false;
        PlayBase(BaseIdleHash);

        if (!IsTiredActive)
        {
            animation_finished = true;
        }

        rollingEyesRoutine = null;
    }

    private void PlayBase(int stateHash)
    {
        animator.Play(stateHash, baseLayer, 0f);
    }

    private void PlayEyelids(int stateHash)
    {
        animator.Play(stateHash, eyelidsLayer, 0f);
    }

    private void StopRoutine(ref Coroutine routine)
    {
        if (routine == null) return;

        StopCoroutine(routine);
        routine = null;
    }

    public void GameHasRestarted()
    {
        game_restarted = true;
    }

    void OnCollisionEnter(Collision collider)
    {
        if (collider.gameObject.CompareTag("forcefield"))
        {
            CourtCollision(collider.contacts[0].point);
        }
        else
        {
            ReleasePlayers();
        }
    }

    protected void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag("colliderShoot"))
        {
            last_player_touched = collider.gameObject.transform.parent.gameObject;
        }
    }

    public GameObject GetLastPlayerTouched()
    {
        return last_player_touched;
    }

    public void SetLastPlayerTouched(GameObject player)
    {
        last_player_touched = player;
    }

    public void OnCollisionExit(Collision collider)
    {
        if (collider.gameObject.CompareTag("Player"))
        {
            last_player_touched = collider.gameObject;
        }
    }

    public void ReleasePlayers()
    {
        if (!game_restarted) return;

        GameObject gameController = GameObject.FindGameObjectWithTag("GameController");
        Game_Behaviour gameBehaviour = gameController.GetComponent<Game_Behaviour>();
        gameBehaviour.ReleasePlayers();
        game_restarted = false;
    }

    public void SetCurrentArea(int area)
    {
        current_area = area;
    }

    public int GetCurrentArea()
    {
        return current_area;
    }
}
