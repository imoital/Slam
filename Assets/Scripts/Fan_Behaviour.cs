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

	// State name candidates for Animator (FBX/Blender often use "Armature|Idle" etc.)
	static string[] AnimatorStateCandidates(string logicalName)
	{
		return new string[] {
			logicalName,
			"Armature|" + logicalName,
			logicalName + " 0"
		};
	}

	// Resolve the actual Animator state name (for Play/CrossFade). Returns null if not found.
	public static string GetResolvedAnimatorStateName(Animator animator, string logicalName)
	{
		if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(logicalName))
			return null;

		foreach (string candidate in AnimatorStateCandidates(logicalName)) {
			int fullHash = Animator.StringToHash("Base Layer." + candidate);
			if (animator.HasState(0, fullHash))
				return "Base Layer." + candidate;
			int bareHash = Animator.StringToHash(candidate);
			if (animator.HasState(0, bareHash))
				return candidate;
		}
		return null;
	}

	bool AnimatorHasState(string stateName)
	{
		return GetResolvedAnimatorStateName(heroAnimator, stateName) != null;
	}

	// Check if current state matches the resolved state name (e.g. "Base Layer.Armature|Idle" or "Idle").
	static bool IsAnimatorInState(AnimatorStateInfo stateInfo, string resolvedStateName)
	{
		if (string.IsNullOrEmpty(resolvedStateName)) return false;
		if (stateInfo.IsName(resolvedStateName)) return true;
		string shortName = resolvedStateName.StartsWith("Base Layer.") ? resolvedStateName.Substring("Base Layer.".Length) : resolvedStateName;
		return stateInfo.IsName(shortName);
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
			} else {
				string idleState = GetResolvedAnimatorStateName(heroAnimator, "Idle");
				if (idleState != null) {
					heroAnimator.Play(idleState, 0, Random.Range(0.0f, 1.0f));
				}
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
			string playState = GetResolvedAnimatorStateName(heroAnimator, play_animation);
			string idleState = GetResolvedAnimatorStateName(heroAnimator, "Idle");
			string celebrateState = GetResolvedAnimatorStateName(heroAnimator, "Celebrate");
			AnimatorStateInfo stateInfo = heroAnimator.GetCurrentAnimatorStateInfo(0);
			if (celebration_period && playState != null) {
				int random = Random.Range(0, 30);
				if (random == 0 && !IsAnimatorInState(stateInfo, playState)) {
					heroAnimator.CrossFade(playState, 0.2f);
				}
			} else if (idleState != null && !IsAnimatorInState(stateInfo, idleState)) {
				// Don't force Idle while in Celebrate - the Celebrate() coroutine will transition back.
				bool inRandomCelebrate = celebrateState != null && IsAnimatorInState(stateInfo, celebrateState);
				if (!inRandomCelebrate) {
					heroAnimator.CrossFade(idleState, 0.2f);
				}
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
			} else {
				string celebrateState = GetResolvedAnimatorStateName(heroAnimator, "Celebrate");
				string idleState = GetResolvedAnimatorStateName(heroAnimator, "Idle");
				if (celebrateState != null) {
					heroAnimator.CrossFade(celebrateState, 0.2f);
					yield return new WaitForSeconds(Random.Range(2f, 4f));
					if (idleState != null) {
						heroAnimator.CrossFade(idleState, 0.2f);
					}
				}
			}
		}
	}
}
