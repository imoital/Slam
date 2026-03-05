using UnityEngine;
using System.Collections;

public class Ball_Behaviour : MonoBehaviour {

	protected bool game_restarted = true;
	protected bool animation_finished = true;
	protected bool rolling_eyes = false;

	private int current_area = 7;

	string[] animationsType1;
	string[] animationsType2;

	protected GameObject last_player_touched;

	protected bool is_looking_somewhere;

	protected Animator animator;

	// Animator parameter names (must match your Animator Controller)
	const string ParamBlink = "Blink";
	const string ParamTired = "Tired";
	const string ParamRollingEyes = "RollingEyes";
	const string ParamRandomIdle = "RandomIdle";

	public void GameHasRestarted()
	{
		game_restarted = true;
	}

	void OnCollisionEnter(Collision collider)
	{
		if (collider.gameObject.tag == "forcefield") {
			CourtCollision(collider.contacts[0].point);
		} else {
			ReleasePlayers();
		}
	}

	protected virtual void CourtCollision(Vector3 point)
	{
		Forcefield forcefield = GameObject.FindGameObjectWithTag("forcefield").GetComponent<Forcefield>();
		forcefield.BallCollition(point);
		int random = Random.Range(0, 100);
		if (random <= 10) {
			if (!rolling_eyes && animator != null) {
				StopAllCoroutines();
				rolling_eyes = true;
				animation_finished = true;
				animator.SetTrigger(ParamRollingEyes);
				StartCoroutine(SetRollingEyesFalseAfterDelay(5f));
			}
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

	protected void OnTriggerEnter(Collider collider)
	{
		if (collider.gameObject.tag == "colliderShoot")
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
		if (collider.gameObject.tag == "Player")
			last_player_touched = collider.gameObject;
	}

	IEnumerator SetAnimationFinishedAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
		animation_finished = true;
	}

	IEnumerator SetRollingEyesFalseAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
		rolling_eyes = false;
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

protected void Start()
	{
		is_looking_somewhere = false;
		animator = GetComponentInChildren<Animator>();

		animationsType1 = new string[] { "look_left", "look_right", "look_up", "look_down" };
		animationsType2 = new string[] { "Default" };

		GameObject[] center_planes = GameObject.FindGameObjectsWithTag("center-plane");
		GameObject center_circle_left = GameObject.FindGameObjectWithTag("center-circle-left");
		GameObject center_circle_rigth = GameObject.FindGameObjectWithTag("center-circle-right");

		for (int i = 0; i < center_planes.Length; i++)
			Physics.IgnoreCollision(center_planes[i].GetComponent<Collider>(), transform.GetComponent<Collider>());
		Physics.IgnoreCollision(center_circle_left.GetComponent<Collider>(), transform.GetComponent<Collider>());
		Physics.IgnoreCollision(center_circle_rigth.GetComponent<Collider>(), transform.GetComponent<Collider>());

		//if (animator != null)
	//	{
			// Ensure we start in the Default state at time 0
			animator.Play("Default", 0, 0f);
		//}

		animation_finished = true;
	}

	protected void Update()
	{
		//if (animator == null) return;

		if (!rolling_eyes && animation_finished) {
			int rand = Random.Range(0, 1000);
			if (rand < 1) {
				animator.SetTrigger(ParamTired);
				animation_finished = false;
				StartCoroutine(SetAnimationFinishedAfterDelay(2.5f));
			} else if (rand < 10) {
				animator.SetInteger(ParamRandomIdle, Random.Range(0, 4));
				animation_finished = false;
				StartCoroutine(SetAnimationFinishedAfterDelay(2.5f));
			}
		}

		if (Random.Range(0, 100) == 0)
			animator.SetTrigger(ParamBlink);
	}
}
