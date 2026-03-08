using UnityEngine;
using System.Collections;

public class Player_Behaviour : MonoBehaviour {
	
	private float POWER_COOLDOWN = 12f;
	public float ACCELERATION = 0.065f;
	public float MAX_ANIMATION_SPEED = 2f;
	public float SHOOT_VELOCITY = 9;
	private int MIN_GOALS_REWARD = 3;  // minimum amount of goals to receive the best goal scorer reward
	public Transform player_mesh;
	
	public float increase_speed = 0.1f;
	private float power_cooldown;

	public Vector3 velocity = Vector3.zero;
	public Vector3 normalized_velocity = Vector3.zero;
	public bool gamepad = false;

	public int gamepad_num = 0;
	public bool move = true;

	public double debug_hit_remaining_time = 0;
	public double debug_hit_time = 30;
	
	public AudioClip ball_sound;
	
	public Material normal_team_1_material;
	public Material shoot_team_1_material;
	
	public GameObject player_controller_prefab;

	protected Vector3 direction = Vector3.zero;
	protected bool ball_collision = false;
	protected Material shoot_material;
	protected Material normal_material;

	protected bool colliding_with_ball = false;
	protected Collider ball_collider;
	protected Transform player_base;
	protected Transform power_bar;
	public Transform power_bar_fill;
	protected Transform dash_smoke;
	protected Transform player_indicator_container;
	public Vector3 viewport_dash_pos;
	protected Transform shoot_effect;

	protected GameObject ball;

	protected bool debug_key_pressed = false;
	protected bool debug_mode = false;
	protected bool hit = false;
	protected Vector3 last_ball_position;
	
	protected bool is_adding_speed = false;
	protected float animation_speed = 1f;
	private Coroutine reactionResetCoroutine;
	private float reactionEndsAt = -1f;

	public PlayerController.Commands commands;
	
	private float PLAYER_ARROW_SIZE = 0.015f;
	private float INIT_PLAYER_STAR_SIZE = 0.5f;
	private float STAR_SHRINK_SPEED = 1f;
	private float star_size;
	
    private float PLAYER_ARROW_HEIGHT = 0.02f;
	
	
	private int goals_scored = 0;
	private bool best_score = false;
	private bool is_animating_star = false;

	protected Hero hero;
	
	private CameraMovement main_camera;

	private float power_activated_timer;
	private float POWER_ACTIVATED_TIMER;

	private bool is_cooldown_over = true;
	private bool is_power_activated_timer_over = false;

	protected bool is_ai = false;

	public int team = 1;

	void VerifyShoot()
	{
		if(colliding_with_ball && !ball_collision && move){

			if(!ball)
				ball = GameObject.FindGameObjectWithTag("ball");

			Vector3 direction = ball.transform.position - transform.position;
			direction.Normalize();

			if((!gamepad && commands.shoot != 0)){

				ball_collider.GetComponent<Rigidbody>().linearVelocity += direction * SHOOT_VELOCITY;
				ball_collision = true;
				colliding_with_ball = false;
				debug_hit_remaining_time = debug_hit_time;
				AudioSource.PlayClipAtPoint(ball_sound, transform.position);
				shoot_effect.GetComponent<ParticleSystem>().Play();
				Ball_Behaviour bb = ball.GetComponent<Ball_Behaviour>();
				bb.ReleasePlayers();
				bb.SetLastPlayerTouched(this.gameObject);

			}
		}
	}

	public Hero GetHero()
	{
		return hero;
	}

	public bool IsCollidingWithBall()
	{
		return colliding_with_ball;
	}
	
	protected virtual void VerifyPower()
	{
		hero.UsePower(commands);
	}

	public void resetPowerBar()
	{
		power_cooldown = POWER_COOLDOWN+Time.time;
		is_cooldown_over = false;
	}

	public void setDashCooldown(float power_cooldown)
	{
		POWER_COOLDOWN = power_cooldown;
	}
	
	public void GoalScored()
	{
		goals_scored++;
		if(goals_scored >= MIN_GOALS_REWARD) {
			if (best_score == false) {
				star_size = INIT_PLAYER_STAR_SIZE;
				is_animating_star = true;
			}
			best_score = true;
			GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
			foreach(GameObject player in players){
				if(player != this.gameObject){
					Player_Behaviour pb = (Player_Behaviour)player.GetComponent<Player_Behaviour>();
					if(pb.DoYouHaveMoreGoals(this.gameObject, goals_scored)) {
						best_score = false;
						break;
					}
				}
			}
		}
	}
	
	bool DoYouHaveMoreGoals(GameObject player, int new_player_score)
	{
		if(new_player_score <= goals_scored) {
			if(new_player_score == goals_scored)
				best_score = false;
			return true;
		} else {
			best_score = false;
			return false;
		}
	}
	
	public int getGoalsScored()
	{
		return goals_scored;
	}
	
	protected void OnTriggerEnter (Collider collider) 
	{
	    if(collider.gameObject.tag == "ball") {
//			colliding_with_ball = true;
			ball_collider = collider;
		}
	}
	
	protected void OnTriggerStay (Collider collider)
	{
		if(collider.gameObject.tag == "ball") {
			colliding_with_ball = true;
			ball_collider = collider;
		}
	}

	protected void OnTriggerExit (Collider collider)
	{
	    if(collider.gameObject.tag == "ball") {
			colliding_with_ball = false;
		}
	}

	void IncreaseSpeed() 
	{
		if(move) {
			direction.x = commands.vertical_direction;
			direction.z = commands.horizontal_direction;
			direction.Normalize();
			
			if(direction == Vector3.zero)
				is_adding_speed = false;
			else
				is_adding_speed = true;
	
			GetComponent<Rigidbody>().linearVelocity += direction*ACCELERATION;
		}
	}

	void UpdateRotation()
	{
		if(!ball)
			ball = GameObject.FindGameObjectWithTag("ball");
		var rotation = Quaternion.LookRotation(ball.transform.position - transform.position);
	    transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 1000);
	}
	
	void UpdateAnimationSpeed()
	{
		if(is_adding_speed) {
			if(animation_speed < MAX_ANIMATION_SPEED)
				animation_speed += increase_speed;
			else if(animation_speed > MAX_ANIMATION_SPEED)
				animation_speed = MAX_ANIMATION_SPEED;
		} else {
			if(animation_speed > 1f)
				animation_speed -= increase_speed;
			else if (animation_speed < 1f)
				animation_speed = 1f;
		}

		if (IsTeslaHero()) {
			Animator teslaAnimator = GetTeslaAnimator();
			if (teslaAnimator != null) {
				teslaAnimator.speed = animation_speed;
			}
			return;
		}

		Animation legacyAnimation = GetLegacyAnimation();
		if (legacyAnimation != null && legacyAnimation["Idle"] != null) {
			legacyAnimation["Idle"].speed = animation_speed;
		}
	}
	
	public void ChangeAnimation(string animation_to_play)
	{
		ScheduleReactionReset(animation_to_play);

		if (IsTeslaHero()) {
			PlayTeslaState(animation_to_play, 0.3f);
			if (animation_to_play == "Celebrate" && hero.GetType() == typeof(AI)) {
				AI ai = (AI)hero;
				ai.GoalScored();
			}
			return;
		}

		Animation legacyAnimation = GetLegacyAnimation();
		if (legacyAnimation == null) return;

		switch (animation_to_play){
			case "Idle":
				legacyAnimation.CrossFade("Idle", 0.3f);
				break;
			case "Celebrate":
				legacyAnimation.CrossFade("Celebrate", 0.3f);
				if (hero.GetType() == typeof(AI)) {
					AI ai = (AI)hero;
					ai.GoalScored();
				}
					
				break;
			case "Sad":
				legacyAnimation.CrossFade("Sad", 0.3f);
				break;
		}
	}
	
	protected void Start() 
	{	
		player_mesh = transform.Find("Mesh");
		star_size = INIT_PLAYER_STAR_SIZE;
		NotificationCenter.DefaultCenter.AddObserver(this, "DoYouHaveMoreGoals");
		
		GameObject court_walls = GameObject.FindGameObjectWithTag("court_walls");
		GameObject main_camera_object = GameObject.FindGameObjectWithTag("MainCamera");
		main_camera = main_camera_object.GetComponent<CameraMovement>();
		GameObject[] goal_detection = GameObject.FindGameObjectsWithTag("goal_detection");
		GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
		player_base = ResolvePlayerBase(player_mesh);
		power_bar = transform.Find("Power_Bar");
		power_bar_fill = power_bar.Find("Power_Fill");
//		dash_smoke = transform.Find("Dash_Smoke");
		shoot_effect = transform.Find("Shoot_Effect");
		Transform base_collider = transform.Find("Collider");
		Transform shoot_collider = transform.Find("ColliderShoot");
		Transform colliderAIPossessionCenter = transform.Find("ColliderAIPossession/ColliderAIPossessionCenter");
		Transform colliderAIPossessionLeft = transform.Find("ColliderAIPossession/ColliderAIPossessionLeft");
		Transform colliderAIPossessionRight = transform.Find("ColliderAIPossession/ColliderAIPossessionRight");
		Transform court_collider = court_walls.transform.Find("forcefield");
		Physics.IgnoreCollision(court_collider.GetComponent<Collider>(), base_collider.GetComponent<Collider>());
		Physics.IgnoreCollision(court_collider.GetComponent<Collider>(), shoot_collider.GetComponent<Collider>());
		Physics.IgnoreCollision(court_collider.GetComponent<Collider>(), colliderAIPossessionCenter.GetComponent<Collider>());
		Physics.IgnoreCollision(court_collider.GetComponent<Collider>(), colliderAIPossessionLeft.GetComponent<Collider>());
		Physics.IgnoreCollision(court_collider.GetComponent<Collider>(), colliderAIPossessionRight.GetComponent<Collider>());

		for(int i = 0; i < goal_detection.Length; i++) {
			Physics.IgnoreCollision(goal_detection[i].GetComponent<Collider>(), base_collider.GetComponent<Collider>());
		}

		for (int i = 0; i < players.Length; i++) {
			Transform other_player_mesh = players[i].transform.Find("Mesh");
			Transform other_player_shoot_collider = players[i].transform.Find("ColliderShoot");
			if(other_player_mesh == null || other_player_shoot_collider == null) {
				continue;
			}
			if(other_player_shoot_collider.GetComponent<Collider>() != shoot_collider.GetComponent<Collider>()) {
				Physics.IgnoreCollision(other_player_shoot_collider.GetComponent<Collider>(), shoot_collider.GetComponent<Collider>());
				Physics.IgnoreCollision(other_player_shoot_collider.GetComponent<Collider>(), base_collider.GetComponent<Collider>());
			}
		}
		
		normal_material = normal_team_1_material;
		shoot_material = shoot_team_1_material;

		if (IsTeslaHero()) {
			PlayTeslaState("Idle", 0f);
		} else {
			Animation legacyAnimation = GetLegacyAnimation();
			if (legacyAnimation != null && legacyAnimation["Idle"] != null) {
				legacyAnimation["Idle"].time = Random.Range(0.0f, legacyAnimation["Idle"].length);
			}
		}
	}
	
	protected 
	void Awake()
	{
		player_indicator_container = transform.Find("Player_Indicator_Container");
	}

	void DebugMode() 
	{
		if(!ball)
			ball = GameObject.FindGameObjectWithTag("ball");

		Vector3 direction;
		if(hit)
			direction = last_ball_position - transform.position;
		else 
			direction = ball.transform.position - transform.position;

		direction.Normalize();

		Debug.DrawLine(transform.position, transform.position + direction*3, Color.yellow);	
	}
	
	private void UpdatePlayerIndicator()
	{
		//player_indicator_container.rotation = Quaternion.LookRotation(Camera.main.transform.position - player_indicator_container.position);
		
	}
	
	public Texture indicator_arrow;
	public Texture dash_bar_outline;
	public Texture dash_bar_green;
	public Texture dash_bar_red;
	public Texture dash_arrow_full;
	public Texture dash_arrow_fill;
	public Texture star;
	private Texture dash_arrow_texture;
	private float current_power_value;
	private float fill_percent;

	void OnGUI()
	{
		float x=0;
		float y=0;
		float width=0;
		float height=0;
		
		Vector2 player_indicator_position = Camera.main.WorldToViewportPoint(power_bar.position);
		float player_x = (float)System.Math.Round((double)player_indicator_position.x, 3);
		float player_y = (float)System.Math.Round((double)player_indicator_position.y, 3);
		
		if (player_x < 0 && player_y < 0) {
		
			y = Screen.height*0.99f - Screen.height*PLAYER_ARROW_SIZE;
			width = Screen.width*PLAYER_ARROW_SIZE;
			height = Screen.width*PLAYER_ARROW_SIZE;
			GUIUtility.RotateAroundPivot(45f, new Vector2(x+Screen.width*PLAYER_ARROW_SIZE/2f, y+Screen.width*PLAYER_ARROW_SIZE/2f));
		
		} else if (player_x < 0 && player_y > 1) {
		
			width = Screen.width*PLAYER_ARROW_SIZE;
			height = Screen.width*PLAYER_ARROW_SIZE;
			GUIUtility.RotateAroundPivot(135f, new Vector2(x+Screen.width*PLAYER_ARROW_SIZE/2f, y+Screen.width*PLAYER_ARROW_SIZE/2f));
			
		} else if (player_x > 1 && player_y < 0) {
			y = Screen.height*0.99f - Screen.height*PLAYER_ARROW_SIZE;
			x = Screen.width - Screen.height*PLAYER_ARROW_HEIGHT;
			width = Screen.width*PLAYER_ARROW_SIZE;
			height = Screen.width*PLAYER_ARROW_SIZE;
			GUIUtility.RotateAroundPivot(-45f, new Vector2(x+Screen.width*PLAYER_ARROW_SIZE/2f, y+Screen.width*PLAYER_ARROW_SIZE/2f));
			
		} else if (player_x > 1 && player_y > 1) {
			x= Screen.width - Screen.height*PLAYER_ARROW_HEIGHT;
			width = Screen.width*PLAYER_ARROW_SIZE;
			height = Screen.width*PLAYER_ARROW_SIZE;
			GUIUtility.RotateAroundPivot(-135f, new Vector2(x+Screen.width*PLAYER_ARROW_SIZE/2f, y+Screen.width*PLAYER_ARROW_SIZE/2f));
			
		}
			
		else if (player_x < 0) {
			x = Screen.height*PLAYER_ARROW_HEIGHT;
			y = (1 - player_y) * Screen.height - Screen.width*PLAYER_ARROW_SIZE/2f - Screen.height*PLAYER_ARROW_HEIGHT;
			width = Screen.width*PLAYER_ARROW_SIZE;
			height = Screen.width*PLAYER_ARROW_SIZE;
			GUIUtility.RotateAroundPivot(90f, new Vector2(x, y));
			
		
		} else if (player_x > 1) {
			x = Screen.width - Screen.height*PLAYER_ARROW_HEIGHT;
			y = (1 - player_y)* Screen.height - Screen.width*PLAYER_ARROW_SIZE/2f - Screen.height*PLAYER_ARROW_HEIGHT;
			width = Screen.width*PLAYER_ARROW_SIZE;
			height = Screen.width*PLAYER_ARROW_SIZE;
			GUIUtility.RotateAroundPivot(-90f, new Vector2(x+Screen.width*PLAYER_ARROW_SIZE/2f, y+Screen.width*PLAYER_ARROW_SIZE/2f));
			
		} else if (player_y > 1) {
			x = player_x * Screen.width - Screen.width*PLAYER_ARROW_SIZE/2f;
			y = 0;
			width = Screen.width*PLAYER_ARROW_SIZE;
			height = Screen.width*PLAYER_ARROW_SIZE;
			GUIUtility.RotateAroundPivot(180f, new Vector2(x+Screen.width*PLAYER_ARROW_SIZE/2f, y+Screen.width*PLAYER_ARROW_SIZE/2f));
		
		} else if (player_y < 0) {
			x = (player_x * Screen.width - Screen.width*PLAYER_ARROW_SIZE/2f);
			y = Screen.height*0.99f - Screen.height*PLAYER_ARROW_SIZE;
			width = Screen.width*PLAYER_ARROW_SIZE;
			height = Screen.width*PLAYER_ARROW_SIZE;
		}
		
		else {
			x = (player_x * Screen.width - Screen.width*PLAYER_ARROW_SIZE/2f);
			y = (1 - player_y) * Screen.height - Screen.width*PLAYER_ARROW_SIZE/2f - Screen.height*PLAYER_ARROW_HEIGHT;
			width = Screen.width*PLAYER_ARROW_SIZE;
			height = Screen.width*PLAYER_ARROW_SIZE;
		}
		
		y = y - Screen.height*0.03f;
//		if(best_score){
//			x = x - 0.004f*Screen.width;
//			y = y - 0.006f*Screen.height;
//			width = width + 0.007f*Screen.width;
//			height = height + 0.012f*Screen.height;
//		}
		
		GUI.DrawTexture( new Rect(x - 0.001f*Screen.width, y - 0.001f*Screen.height, width + 0.002f*Screen.width, height), indicator_arrow, ScaleMode.ScaleToFit, true);
		if(best_score) {
			if (is_animating_star == true)
				AnimateStar();
			if (main_camera.is_shaking)
				main_camera.ShakeCamera();
			GUI.DrawTexture( new Rect(
					x + 0.0016f*Screen.width-(Screen.width*star_size*0.5f), 
					y + 0.001f*Screen.height-(Screen.height*star_size*0.5f), 
					width - 0.0036f*Screen.width+(star_size*Screen.width), 
					height - 0.0108f*Screen.height+(star_size*Screen.height)), 
				star, 
				ScaleMode.ScaleToFit, 
				true);
		}

		if (POWER_ACTIVATED_TIMER == 0f) {
			dash_arrow_texture = dash_arrow_fill;
			is_cooldown_over = false;
			current_power_value = POWER_COOLDOWN - (power_cooldown-Time.time);

		} else {
			dash_arrow_texture = dash_arrow_full;
			current_power_value = power_activated_timer - Time.time;

		}

		if (current_power_value < 0) {
			current_power_value = 0;
			is_power_activated_timer_over = true;

		}else if (current_power_value > POWER_COOLDOWN) {
			current_power_value = POWER_COOLDOWN;
			dash_arrow_texture = dash_arrow_full;
			is_cooldown_over = true;
		}

		if (POWER_ACTIVATED_TIMER == 0f) {
			fill_percent = current_power_value/POWER_COOLDOWN;
		} else {
			fill_percent = current_power_value/POWER_ACTIVATED_TIMER;
		}
		

		float dash_arrow_width = 0.005f * Screen.width;
		float dash_arrow_height = 0.004f * Screen.height;
		
		
		GUI.BeginGroup(new Rect (x-(dash_arrow_width/2f), y - (dash_arrow_height/2f) + height*(1f-fill_percent), width + dash_arrow_width, height*fill_percent + dash_arrow_height - 0.002f*Screen.height));
		GUI.DrawTexture(new Rect(0, -(1f-fill_percent)*height, width + dash_arrow_width, height + dash_arrow_height), dash_arrow_texture);
		GUI.EndGroup();
	}

	public bool IsCooldownOver()
	{
		return is_cooldown_over;
	}

	public bool IsPowerTimerOver()
	{
		return is_power_activated_timer_over;
	}

	public void SetIsAI(bool is_ai)
	{
		this.is_ai = is_ai;
	}

	public void setPowerActivatedTimer(float value)
	{
		power_activated_timer = value + Time.time;
		POWER_ACTIVATED_TIMER = value;
		is_power_activated_timer_over = false;
	}
	
	private void AnimateStar()
	{	
		if (star_size <= 0.05) {
			star_size = 0;
			main_camera.ShakeCamera();
			is_animating_star = false;
		}
		else
			star_size -= STAR_SHRINK_SPEED * Time.deltaTime;
	}

	// Update is called once per frame
	protected void FixedUpdate () 
	{
		/* TODO: Uma forma mais inteligente de fazer isto */
		if (player_base == null)
			player_base = ResolvePlayerBase(transform.Find("Mesh"));
		/***************************************************/
		
		if (power_bar_fill == null)
			power_bar_fill = transform.Find("Dash_Fill");
		
		power_bar.rotation = Quaternion.Euler(0,180f,0);
		viewport_dash_pos = Camera.main.WorldToViewportPoint(power_bar.position);

//		Debug.Log(Camera.main.WorldToViewportPoint(dash_bar.position));
		
		if(ball_collision && commands.shoot == 0) {
			ball_collision = false;
		}
		
		if(move) {
			Renderer baseRenderer = player_base != null ? player_base.GetComponent<Renderer>() : null;
			if (baseRenderer != null) {
				if(!ball_collision && commands.shoot != 0)
					baseRenderer.material = shoot_material;
				else
					baseRenderer.material = normal_material;
			}
		}
		
		IncreaseSpeed();
		VerifyPower();
		VerifyShoot();
		MaintainDefaultAnimation();
		UpdatePlayerIndicator();
		UpdateRotation();
		UpdateAnimationSpeed();

		if(Input.GetKeyDown(KeyCode.F1) && !debug_key_pressed) {
			debug_mode = !debug_mode;
			debug_key_pressed = true;
		}

		if(Input.GetKeyUp(KeyCode.F1) && debug_key_pressed)
			debug_key_pressed = false;

		if(debug_hit_remaining_time == 0)
			hit = false;
		else {
			hit = true;
			if(debug_hit_remaining_time == debug_hit_time) {
				if(!ball)
					ball = GameObject.FindGameObjectWithTag("ball");
				last_ball_position = ball.transform.position;
			}
			debug_hit_remaining_time--;
		}

		if(debug_mode)
			DebugMode();
	}

	void ScheduleReactionReset(string animationToPlay)
	{
		if (reactionResetCoroutine != null) {
			StopCoroutine(reactionResetCoroutine);
			reactionResetCoroutine = null;
		}

		if (animationToPlay == "Idle") {
			reactionEndsAt = -1f;
			return;
		}

		float resetDelay = GetAnimationLength(animationToPlay);
		if (resetDelay <= 0f) {
			resetDelay = 1.2f;
		}

		reactionEndsAt = Time.time + resetDelay;
		reactionResetCoroutine = StartCoroutine(ReturnToIdleAfterDelay(resetDelay));
	}

	IEnumerator ReturnToIdleAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
		ChangeAnimation("Idle");
		reactionResetCoroutine = null;
	}

	void MaintainDefaultAnimation()
	{
		if (reactionEndsAt > 0f && Time.time < reactionEndsAt) {
			return;
		}

		if (IsTeslaHero()) {
			Animator animator = GetTeslaAnimator();
			if (animator != null) {
				if (!IsCurrentTeslaState("Idle")) {
					PlayTeslaState("Idle", 0.15f);
				}
			}
			return;
		}

		Animation legacyAnimation = GetLegacyAnimation();
		if (legacyAnimation != null && legacyAnimation["Idle"] != null && !legacyAnimation.IsPlaying("Idle")) {
			legacyAnimation.CrossFade("Idle", 0.15f);
		}
	}

	float GetAnimationLength(string animationName)
	{
		if (IsTeslaHero()) {
			Animator animator = GetTeslaAnimator();
			if (animator != null && animator.runtimeAnimatorController != null) {
				AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
				for (int i = 0; i < clips.Length; i++) {
					if (clips[i] != null && MatchesTeslaAnimationName(clips[i].name, animationName)) {
						return clips[i].length;
					}
				}
			}
		}

		Animation legacyAnimation = GetLegacyAnimation();
		if (legacyAnimation != null && legacyAnimation[animationName] != null) {
			return legacyAnimation[animationName].length;
		}

		return 0f;
	}

	protected Animation GetLegacyAnimation()
	{
		if (player_mesh != null) {
			Animation playerMeshAnimation = player_mesh.GetComponent<Animation>();
			if (playerMeshAnimation != null) {
				return playerMeshAnimation;
			}
		}

		return GetComponent<Animation>();
	}

	protected bool IsTeslaHero()
	{
		return GetTeslaAnimator() != null;
	}

	protected Animator GetTeslaAnimator()
	{
		if (player_mesh == null) return null;

		Animator animator = player_mesh.GetComponent<Animator>();
		if (animator == null) {
			animator = player_mesh.GetComponentInChildren<Animator>();
		}

		if (animator != null && animator.runtimeAnimatorController != null) {
			return animator;
		}

		return null;
	}

	protected void PlayTeslaState(string animationToPlay, float transitionDuration)
	{
		Animator animator = GetTeslaAnimator();
		if (animator == null) return;

		string stateName = ResolveTeslaStateName(animator, animationToPlay);
		if (string.IsNullOrEmpty(stateName)) return;

		int stateHash = Animator.StringToHash(stateName);

		if (transitionDuration > 0f) {
			animator.CrossFade(stateHash, transitionDuration);
		} else {
			animator.Play(stateHash, 0, 0f);
		}
	}

	protected Transform ResolvePlayerBase(Transform meshRoot)
	{
		if (meshRoot == null) return null;

		Transform directBase = meshRoot.Find("Base");
		if (directBase != null) return directBase;

		return FindDeepChild(meshRoot, "Base");
	}

	protected Transform FindDeepChild(Transform parent, string childName)
	{
		if (parent == null) return null;

		for (int i = 0; i < parent.childCount; i++) {
			Transform child = parent.GetChild(i);
			if (child.name == childName) {
				return child;
			}

			Transform nestedChild = FindDeepChild(child, childName);
			if (nestedChild != null) {
				return nestedChild;
			}
		}

		return null;
	}

	bool IsCurrentTeslaState(string animationToPlay)
	{
		Animator animator = GetTeslaAnimator();
		if (animator == null) return false;

		AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
		string resolvedStateName = ResolveTeslaStateName(animator, animationToPlay);
		if (string.IsNullOrEmpty(resolvedStateName)) return false;

		string[] candidates = BuildTeslaStateCandidates(animationToPlay);
		for (int i = 0; i < candidates.Length; i++) {
			string candidate = candidates[i];
			if (resolvedStateName == candidate || resolvedStateName.EndsWith("." + candidate)) {
				if (stateInfo.IsName(candidate) || stateInfo.IsName("Base Layer." + candidate)) {
					return true;
				}
			}
		}

		return false;
	}

	string ResolveTeslaStateName(Animator animator, string animationToPlay)
	{
		string[] candidates = BuildTeslaStateCandidates(animationToPlay);
		for (int i = 0; i < candidates.Length; i++) {
			string candidate = candidates[i];
			int fullHash = Animator.StringToHash("Base Layer." + candidate);
			if (animator.HasState(0, fullHash)) {
				return "Base Layer." + candidate;
			}

			int bareHash = Animator.StringToHash(candidate);
			if (animator.HasState(0, bareHash)) {
				return candidate;
			}
		}

		return null;
	}

	string[] BuildTeslaStateCandidates(string animationToPlay)
	{
		return new string[] {
			animationToPlay,
			"Armature|" + animationToPlay,
			animationToPlay + " 0"
		};
	}

	bool MatchesTeslaAnimationName(string clipName, string animationName)
	{
		if (string.IsNullOrEmpty(clipName)) return false;
		if (clipName == animationName) return true;
		if (clipName.EndsWith("|" + animationName)) return true;
		return false;
	}
}