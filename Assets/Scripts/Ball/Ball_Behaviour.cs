using UnityEngine;
using System.Collections;

public class Ball_Behaviour : MonoBehaviour
{
    protected bool game_restarted = true;
    protected bool animation_finished = true;
    protected bool rolling_eyes = false;
    protected bool tired_active = false;

    private int current_area = 7;
    private float last_animation_time = -999f;
    private const float MinAnimationInterval = 2f;
    private const float RollingEyesCooldown = 4f;
    private float last_rolling_eyes_time = -999f;

    protected GameObject last_player_touched;
    protected bool is_looking_somewhere;
    protected Animator animator;

    private const string ParamBlink = "Blink";
    private const string ParamTired = "Tired";
    private const string ParamRollingEyes = "RollingEyes";
    private const string ParamLookLeft = "LookLeft";
    private const string ParamLookRight = "LookRight";
    private const string ParamLookUp = "LookUp";
    private const string ParamLookDown = "LookDown";

    protected void Start()
    {
        is_looking_somewhere = false;
        animator = GetComponent<Animator>();

        GameObject[] center_planes = GameObject.FindGameObjectsWithTag("center-plane");
        GameObject center_circle_left = GameObject.FindGameObjectWithTag("center-circle-left");
        GameObject center_circle_right = GameObject.FindGameObjectWithTag("center-circle-right");

        for (int i = 0; i < center_planes.Length; i++)
        {
            Physics.IgnoreCollision(center_planes[i].GetComponent<Collider>(), transform.GetComponent<Collider>());
        }

        Physics.IgnoreCollision(center_circle_left.GetComponent<Collider>(), transform.GetComponent<Collider>());
        Physics.IgnoreCollision(center_circle_right.GetComponent<Collider>(), transform.GetComponent<Collider>());

        if (animator != null)
        {
            animator.Play("Idle", 0, 0f);
        }

        rolling_eyes = false;
        tired_active = false;
        animation_finished = true;
        last_animation_time = Time.time;
    }

    protected void Update()
    {
        if (animator == null) return;

        float now = Time.time;

        if (now - last_animation_time < MinAnimationInterval) return;
        if (rolling_eyes || !animation_finished) return;

        int rand = Random.Range(0, 100);

        if (rand < 1)
        {
            PlayTired(now);
        }
        else if (rand < 50)
        {
            PlayRandomLook(now);
        }
        else if (rand < 55)
        {
            TryBlink(now);
        }
    }

    private void PlayTired(float now)
    {
        ResetOneShotTriggers();
        animator.SetTrigger(ParamTired);

        tired_active = true;
        animation_finished = false;
        last_animation_time = now;

        StartCoroutine(ClearMainAnimationAfterDelay(2.5f, true));
    }

    private void PlayRandomLook(float now)
    {
        ResetOneShotTriggers();

        int dir = Random.Range(0, 4);
        switch (dir)
        {
            case 0: animator.SetTrigger(ParamLookLeft); break;
            case 1: animator.SetTrigger(ParamLookRight); break;
            case 2: animator.SetTrigger(ParamLookUp); break;
            case 3: animator.SetTrigger(ParamLookDown); break;
        }

        animation_finished = false;
        last_animation_time = now;

        StartCoroutine(ClearMainAnimationAfterDelay(2.5f, false));
    }

    private void TryBlink(float now)
    {
        if (rolling_eyes) return;
        if (tired_active) return;

        animator.ResetTrigger(ParamBlink);
        animator.SetTrigger(ParamBlink);
        last_animation_time = now;
    }

    private void ResetOneShotTriggers()
    {
        animator.ResetTrigger(ParamLookLeft);
        animator.ResetTrigger(ParamLookRight);
        animator.ResetTrigger(ParamLookUp);
        animator.ResetTrigger(ParamLookDown);
        animator.ResetTrigger(ParamTired);
        animator.ResetTrigger(ParamRollingEyes);
    }

    private IEnumerator ClearMainAnimationAfterDelay(float delay, bool clearTired)
    {
        yield return new WaitForSeconds(delay);
        animation_finished = true;

        if (clearTired)
            tired_active = false;
    }

    protected virtual void CourtCollision(Vector3 point)
    {
        Forcefield forcefield = GameObject.FindGameObjectWithTag("forcefield").GetComponent<Forcefield>();
        forcefield.BallCollition(point);

        float now = Time.time;
        if (now - last_rolling_eyes_time < RollingEyesCooldown) return;

        int random = Random.Range(0, 100);
        if (random <= 10 && !rolling_eyes && animator != null)
        {
            rolling_eyes = true;
            animation_finished = false;
            tired_active = false;
            last_rolling_eyes_time = now;
            last_animation_time = now;

            ResetOneShotTriggers();
            animator.SetTrigger(ParamRollingEyes);

            StartCoroutine(EndRollingEyesAfterDelay(5f));
        }
    }

    private IEnumerator EndRollingEyesAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        rolling_eyes = false;
        animation_finished = true;
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
            last_player_touched = collider.gameObject.transform.parent.gameObject;
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
            last_player_touched = collider.gameObject;
    }

    public void ReleasePlayers()
    {
        if (game_restarted)
        {
            GameObject gbo = GameObject.FindGameObjectWithTag("GameController");
            Game_Behaviour gb = gbo.GetComponent<Game_Behaviour>();
            gb.ReleasePlayers();
            game_restarted = false;
        }
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