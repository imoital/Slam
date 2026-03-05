using UnityEngine;
using System.Collections;

public class Fan_Behaviour : MonoBehaviour {
	
	public int team;
	private GameObject center;
	private GameObject ball;
	private bool celebration_period = false;
	private string play_animation;
	private bool was_sad;
	private Transform hero;

	bool TryInitializeHero()
	{
		if (hero != null) return true;
		if (transform.childCount == 0) return false;

		hero = transform.GetChild(0);
		return hero != null && hero.GetComponent<Animation>() != null;
	}
	
	void Awake()
	{
		NotificationCenter.DefaultCenter.AddObserver(this, "StopCelebration");
		NotificationCenter.DefaultCenter.AddObserver(this, "ChangeReaction");
		//hero.animation.Stop();
	}

	void StopCelebration()
	{
		celebration_period = false;
	}

	public void HeroStarted(GameObject center)
	{
		this.center = center;
		if (TryInitializeHero()) {
			hero.GetComponent<Animation>().Stop();
		}
	}
	
	void ChangeReaction(NotificationCenter.Notification notification)
	{
		if ((int)notification.data["team"] == team) {
			play_animation = (string)notification.data["reaction"];
			celebration_period = true;
			was_sad = false;
		}
	}
	
	void UpdateRotation()
	{
		GameObject look_to = ball;
		if(!ball){
			ball = GameObject.FindGameObjectWithTag("ball");

			if(!ball) {
				look_to = center;
			}

		} else {
			var rotation = Quaternion.LookRotation(transform.position - look_to.transform.position);
		    transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 1000);
		}
	}
	
	// Update is called once per frame
	void Update () {
		UpdateRotation();
		if (!TryInitializeHero()) return;

		Animation heroAnimation = hero.GetComponent<Animation>();
		if (heroAnimation == null) return;

		if(celebration_period){
			if(!heroAnimation.IsPlaying(play_animation)){
				int random = Random.Range(0, 30);
				if (random == 0){
					heroAnimation.CrossFade(play_animation, 0.2f);
				}
			}
		} else if(!heroAnimation.IsPlaying("Idle")) {
			heroAnimation.CrossFade("Idle",0.5f);
			heroAnimation["Idle"].time = Random.Range(0, heroAnimation["Idle"].length);
		}
		
	}
	
	public IEnumerator Celebrate()
	{
		if(!celebration_period) {
			hero.GetComponent<Animation>().CrossFade("Celebrate", 1f);
			yield return new WaitForSeconds(Random.Range(hero.GetComponent<Animation>()["Celebrate"].length*8f, hero.GetComponent<Animation>()["Celebrate"].length*16f));
			hero.GetComponent<Animation>().CrossFade("Idle", 0.5f);
		}
	}
}
