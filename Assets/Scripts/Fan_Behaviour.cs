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
	private Animation heroAnimation;
	private Animator heroAnimator;

	bool TryBindHeroFromChildren()
	{
		foreach (Transform child in transform) {
			Animation childAnimation = child.GetComponent<Animation>();
			if (childAnimation == null) {
				childAnimation = child.GetComponentInChildren<Animation>();
			}

			Animator childAnimator = child.GetComponent<Animator>();
			if (childAnimator == null) {
				childAnimator = child.GetComponentInChildren<Animator>();
			}

			if (childAnimation != null || childAnimator != null) {
				hero = child;
				heroAnimation = childAnimation;
				heroAnimator = childAnimator;
				return true;
			}
		}

		return false;
	}

	bool AnimatorHasState(string stateName)
	{
		if (heroAnimator == null || heroAnimator.runtimeAnimatorController == null || string.IsNullOrEmpty(stateName)) {
			return false;
		}

		return heroAnimator.HasState(0, Animator.StringToHash("Base Layer." + stateName));
	}

	bool TryInitializeHero()
	{
		if (hero != null && (heroAnimation != null || heroAnimator != null)) return true;
		if (transform.childCount == 0) return false;

		return TryBindHeroFromChildren();
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
			if (heroAnimation != null) {
				heroAnimation.Stop();
				if (heroAnimation["Idle"] != null) {
					heroAnimation.CrossFade("Idle", 0.2f);
				}
			} else if (AnimatorHasState("Idle")) {
				heroAnimator.Play("Idle", 0, Random.Range(0.0f, 1.0f));
			}
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

		if (heroAnimation != null) {
			if(celebration_period){
				if(heroAnimation[play_animation] != null && !heroAnimation.IsPlaying(play_animation)){
					int random = Random.Range(0, 30);
					if (random == 0){
						heroAnimation.CrossFade(play_animation, 0.2f);
					}
				}
			} else if(heroAnimation["Idle"] != null && !heroAnimation.IsPlaying("Idle")) {
				heroAnimation.CrossFade("Idle",0.5f);
				heroAnimation["Idle"].time = Random.Range(0, heroAnimation["Idle"].length);
			}
			return;
		}

		if (heroAnimator != null) {
			if (celebration_period && AnimatorHasState(play_animation)) {
				int random = Random.Range(0, 30);
				if (random == 0 && !heroAnimator.GetCurrentAnimatorStateInfo(0).IsName(play_animation)) {
					heroAnimator.CrossFade(play_animation, 0.2f);
				}
			} else if (AnimatorHasState("Idle") && !heroAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) {
				heroAnimator.CrossFade("Idle", 0.2f);
			}
		}
		
	}
	
	public IEnumerator Celebrate()
	{
		if(!celebration_period && TryInitializeHero()) {
			if (heroAnimation != null && heroAnimation["Celebrate"] != null) {
				heroAnimation.CrossFade("Celebrate", 1f);
				yield return new WaitForSeconds(Random.Range(heroAnimation["Celebrate"].length*8f, heroAnimation["Celebrate"].length*16f));
				if (heroAnimation["Idle"] != null) {
					heroAnimation.CrossFade("Idle", 0.5f);
				}
			} else if (AnimatorHasState("Celebrate")) {
				heroAnimator.CrossFade("Celebrate", 0.2f);
				yield return new WaitForSeconds(Random.Range(2f, 4f));
				if (AnimatorHasState("Idle")) {
					heroAnimator.CrossFade("Idle", 0.2f);
				}
			}
		}
	}
}
