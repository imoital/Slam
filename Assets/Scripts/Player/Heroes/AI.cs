using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AI : Hero {

	// Static replacement for removed AIManager
	private static List<Hero> allHeroes = new List<Hero>();
	private const float POSSESSION_THRESHOLD = 1.25f;

	public static void RegisterHero(Hero h) {
		if (h != null && !allHeroes.Contains(h)) allHeroes.Add(h);
	}

	private static GameObject GetBall() {
		GameObject b = GameObject.FindGameObjectWithTag("ball");
		return b != null ? b : null;
	}

	private static Vector3 GetRedTeamGoalPosition() {
		GameObject g = GameObject.Find("RedGoal");
		if (g == null) g = GameObject.Find("GoalRed");
		return g != null ? g.transform.position : new Vector3(0f, 0f, -10f);
	}

	private static Vector3 GetBlueTeamGoalPosition() {
		GameObject g = GameObject.Find("BlueGoal");
		if (g == null) g = GameObject.Find("GoalBlue");
		return g != null ? g.transform.position : new Vector3(0f, 0f, 10f);
	}

	private static float GoalWidth() { return 2f; }

	private static int AreaToFlank(int area) {
		if (area < 0 || area > 17) return GlobalConstants.MID_FLANK;
		return area % 3; // 0=BOTTOM, 1=MID, 2=TOP
	}

	private static Vector3 GetPitchAreaCoords(int index) { return PitchArea.GetPosition(index); }

	private static List<Hero> GetPlayerListFromArea(int index) { return PitchArea.GetHeroesInArea(index); }

	private static List<Hero> GetPlayersInPossession() {
		List<Hero> inPossession = new List<Hero>();
		GameObject ballObj = GetBall();
		if (ballObj == null) return inPossession;
		Vector3 ballPos = ballObj.transform.position;
		foreach (Hero h in allHeroes) {
			float dist = Vector3.Distance(h.GetPosition(), ballPos);
			if (dist < POSSESSION_THRESHOLD) inPossession.Add(h);
		}
		return inPossession;
	}

	private static Hero GetHeroCloserToBall(int team) {
		GameObject ballObj = GetBall();
		if (ballObj == null) return null;
		Vector3 ballPos = ballObj.transform.position;
		Hero closest = null;
		float minDist = float.MaxValue;
		foreach (Hero h in allHeroes) {
			if (h.GetTeam() != team) continue;
			float d = Vector3.Distance(h.GetPosition(), ballPos);
			if (d < minDist) { minDist = d; closest = h; }
		}
		return closest;
	}

	private static int GetTeamInPossession() {
		List<Hero> pos = GetPlayersInPossession();
		if (pos.Count == 0) return 0;
		return pos[0].GetTeam();
	}

	private bool HeroHasBall() {
		if (ball == null) return false;
		return Vector3.Distance(player.transform.position, ball.transform.position) < POSSESSION_THRESHOLD;
	}

	private static List<Hero>[] GetFlankHeroes(int flank) {
		List<Hero>[] byDepth = new List<Hero>[] { new List<Hero>(), new List<Hero>(), new List<Hero>(), new List<Hero>(), new List<Hero>(), new List<Hero>() };
		foreach (Hero h in allHeroes) {
			int a = h.GetCurrentArea();
			if (a < 0 || a > 17) continue;
			if (AreaToFlank(a) != flank) continue;
			int depth = 0;
			for (int i = 0; i <= a; i++) if (AreaToFlank(i) == flank) depth++;
			if (depth >= 1 && depth <= 6) byDepth[depth - 1].Add(h);
		}
		return byDepth;
	}

	private static Vector3 IsTeammateAloneInFlanks(AI ai) {
		int team = ai.team;
		Vector3 result = new Vector3(-1, -1, -1);
		for (int flank = 0; flank <= 2; flank++) {
			List<Hero>[] byDepth = GetFlankHeroes(flank);
			int ourCount = 0, theirCount = 0;
			for (int d = 0; d < 6; d++)
				foreach (Hero h in byDepth[d]) {
					if (h.GetTeam() == team) ourCount++; else theirCount++;
				}
			if (ourCount == 1 && theirCount == 0) result[flank] = team;
			else if (theirCount == 1 && ourCount == 0) result[flank] = team == GlobalConstants.RED ? GlobalConstants.BLUE : GlobalConstants.RED;
		}
		return result;
	}

	private static List<Hero>[] GetTopFlankHeroes() { return GetFlankHeroes(GlobalConstants.TOP_FLANK); }
	private static List<Hero>[] GetMidFlankHeroes() { return GetFlankHeroes(GlobalConstants.MID_FLANK); }
	private static List<Hero>[] GetBottomFlankHeroes() { return GetFlankHeroes(GlobalConstants.BOTTOM_FLANK); }

//	private PlayerController controller;
	Local_Player local_player;
	private GameObject ball;
	private Ball_Behaviour ball_behaviour;

	private const int BOTTOM_FLANK = 0, MID_FLANK = 1, TOP_FLANK = 2, LEFT = 3, RIGHT = 4;

	private const int MOVE_UP = 1, MOVE_DOWN = -1, MOVE_LEFT = 1, MOVE_RIGHT = -1;

	private const int UP = 1, DOWN = 2;

	private const int ABOVE = 1, BELOW = 2;

	private const int MAX_DEPTH = 6;

	//private const int RED = 1, BLUE = 2;

//	private bool has_ball = false;

	private bool is_clockwise_rotation = true;

	private bool touched_ball = false;

	//distance from which the player is considered to be in possession
	private float possession_distance_threshold = 1.25f;

	private Transform sphere;

	private int goto_area = 0;

	private int key = 0;

	Transform colliderAIPossessionCenter;
	Transform colliderAIPossessionLeft;
	Transform colliderAIPossessionRight;
	Transform player_collider;

	private int emotion;


	private struct Beliefs 
	{
		public Vector3 own_goal_position;
		public Vector3 opponent_goal_position;
		public float goal_width;
		public bool opponent_has_ball;
		public bool teammate_has_ball;
		public bool is_in_scoring_depth;
		public int team;
		public bool has_ball;
		public bool is_obstructed_path;
		public float distance_to_ball;
		public int team_in_possession;
		public Hero hero_in_possession;
		public Hero teammate_closer_to_ball;
		public Hero opponent_closer_to_ball;
		//public List<int> opponents_in_the_way;
	}

	private enum Desires 
	{
		PASS,
		SCORE,
		DRIBBLE,
		TACKLE,
		MOVE_TO_AREA,
		TAKE_POSSESSION
		
	}

	private enum Expectations
	{
		PASS,
		SCORE,
		DEFEND,
		NULL
	}

	Beliefs beliefs;
	Expectations expectation;

	// The desire to which the agent has commited will be the intention
	Desires desire; 

	public AI(Player_Behaviour player)
	{
		hero_prefab = Resources.Load<GameObject>("Heroes/Sam");
		this.player = player;
		this.local_player = (Local_Player)player;
		player.SetIsAI(true);
		is_ai = true;
		ball = GameObject.FindGameObjectWithTag("ball");

		ball_behaviour = ball.GetComponent<Ball_Behaviour>();

		colliderAIPossessionCenter = player.transform.Find("ColliderAIPossession/ColliderAIPossessionCenter");
		colliderAIPossessionLeft = player.transform.Find("ColliderAIPossession/ColliderAIPossessionLeft");
		colliderAIPossessionRight = player.transform.Find("ColliderAIPossession/ColliderAIPossessionRight");
		player_collider = player.transform.Find("Collider");
		colliderAIPossessionCenter.gameObject.SetActive(true);
	}
	// Use this for initialization
	public override void Start () 
	{
		if (!allHeroes.Contains(this)) allHeroes.Add(this);
		this.team = player.team;
		beliefs.team = player.team;
		if (beliefs.team == GlobalConstants.RED) {
			beliefs.own_goal_position = GetRedTeamGoalPosition();
			beliefs.opponent_goal_position = GetBlueTeamGoalPosition();
		} else if (beliefs.team == GlobalConstants.BLUE) {
			beliefs.own_goal_position = GetBlueTeamGoalPosition();
			beliefs.opponent_goal_position = GetRedTeamGoalPosition();
			Debug.Log(this.GetTeam() +  " CORRECT TEAM " + GlobalConstants.BLUE);
		}
		beliefs.goal_width = GoalWidth();

		beliefs.distance_to_ball = 0;

	//	beliefs.teammate_going_for_ball = false;
	//	beliefs.opponent_going_for_ball = false;

		desire = Desires.TAKE_POSSESSION;

		expectation = Expectations.DEFEND;
	//	Debug.Log(expectation);
	}


	public override void Update() 
	{
		
		if (Input.GetKeyDown("0"))
		    key = 0;
		else if (Input.GetKeyDown("1"))
			key = 1;
		else if (Input.GetKeyDown("2"))
			key = 2;
		if (Input.GetKeyDown("3"))
			key = 3;
		if (Input.GetKeyDown("4"))
			key = 4;
		if (Input.GetKeyDown("5"))
			key = 5;

		ResetControllers();
	
		UpdateBeliefs();
		UpdatePossession();

//		if(expectation == Expectations.PASS && beliefs.teammate_has_ball)
//			Debug.Log("happy");
//		else
//			Debug.Log("Sad");
		
		//if (expectation == Expectations.SCORE && beliefs.teammate_has_ball)

		if (beliefs.has_ball) {
			if (GetPlayersInPossession().Count > 1) {
				//Shoot();
				Score();
			} else GoOpenFlank();
			if (beliefs.is_in_scoring_depth) {
				if(!Score()) { //returns false if can't find a clear opening to shoot to score
					Pass();
					expectation = Expectations.PASS; //If can't score will try to shoot
				} else {
					expectation = Expectations.SCORE;
				}
			} else
				Pass();
		} else {
			if (GetPlayersInPossession().Count > 1 || (beliefs.teammate_closer_to_ball != null && !beliefs.teammate_closer_to_ball.Equals(this))) {
				Defend();
			}
		}

		if (beliefs.team_in_possession == 0 || beliefs.team_in_possession != team) {
			if (beliefs.teammate_closer_to_ball != null && beliefs.teammate_closer_to_ball.Equals(this)) {
				GoToBall();
			}
		} else if (!beliefs.has_ball && beliefs.team_in_possession == team) {
			Unmark ();
		//	expectation = Actions.PASS;
		}

	//	Debug.Log(expectation);
		
	}

	private bool GoOpenFlank()
	{
		Vector3 flanks = IsTeammateAloneInFlanks(this);
		int current_flank = AreaToFlank(current_area);
		if (current_flank == GlobalConstants.TOP_FLANK) {
			if (flanks.x == -1) {
				DribbleToArea(DepthToArea(GlobalConstants.TOP_FLANK ,AreaToDepth(current_area)+1));
				//return true;
			}
		} else if (current_flank == GlobalConstants.MID_FLANK) {
			if (flanks.y == -1) {
				DribbleToArea(DepthToArea(GlobalConstants.MID_FLANK, AreaToDepth(current_area)+1));
			//	return true;
			}
		} else if (current_flank == GlobalConstants.BOTTOM_FLANK) {
			if (flanks.z == -1) {
				DribbleToArea(DepthToArea(GlobalConstants.BOTTOM_FLANK, AreaToDepth(current_area)+1));
				//return true;
			}
		}
	//	Debug.Log(DepthToArea(GlobalConstants.TOP_FLANK ,AreaToDepth(current_area)+1));
		return false;

	}


	private bool DisputingBall()
	{
		if (GetPlayersInPossession().Count > 1)
			return true;
		else return false;
	}

	private void Defend()
	{
		if (beliefs.teammate_closer_to_ball == null) return;
		int current_depth = AreaToDepth(beliefs.teammate_closer_to_ball.GetCurrentArea());
		
		if (team == GlobalConstants.RED)
			current_depth += 2;
		else
			current_depth -= 2;
		
		int new_area = DepthToArea(GlobalConstants.MID_FLANK, current_depth);

		//Debug.Log(current_depth);

		GoToArea(new_area);
		
	}

	private void UpdateScoringDepth()
	{
		int team = beliefs.team;

		if (team == GlobalConstants.RED) {
			if (AreaToDepth(current_area) <= 2) {
				beliefs.is_in_scoring_depth = true;
			} else {
			beliefs.is_in_scoring_depth = false;
			}
		} else {
			if (AreaToDepth(current_area) >= 5) {
				beliefs.is_in_scoring_depth = true;
			} else {
				beliefs.is_in_scoring_depth = false;
			}
		}
	}

	private bool Score()
	{

		RotateAroundBall(beliefs.opponent_goal_position);

		int layer_mask = 1 << 29 | 1 << 28 | 1 << 27;
		Vector3 ball_vector = new Vector3 (ball.transform.position.x, -0.1f, ball.transform.position.z);
	
		RaycastHit goal_hit;
		RaycastHit shoot_hit;

		Vector3 goal_pos = new Vector3(beliefs.opponent_goal_position.x, -0.1f, beliefs.opponent_goal_position.z);
		Ray goal_ray = new Ray(ball_vector, beliefs.opponent_goal_position - ball_vector);
		Ray shoot_ray = new Ray(ball_vector, -1*(goal_pos - ball_vector));
		
		if(Physics.Raycast(goal_ray, out goal_hit, Mathf.Infinity, layer_mask)) {
			if (goal_hit.collider.CompareTag("goal_detection")) {
				if (colliderAIPossessionCenter.GetComponent<Collider>().Raycast(shoot_ray, out shoot_hit, Mathf.Infinity)) {
					Shoot();
					return true;
				}
			} else {
				if (beliefs.is_in_scoring_depth == false) {
					Shoot(); // tries to clear the ball
					return false; // path obstructed
				}
			}
		}

		Debug.DrawRay(ball_vector, goal_pos - ball_vector);
		Debug.DrawRay(ball_vector, -1*(goal_pos - ball_vector));
		return true; // keep trying
	}

	private void Shoot()
	{
		local_player.player_controller.commands.shoot = 1;
	}

	private void Pass()
	{
		Vector3 flanks = IsTeammateAloneInFlanks(this);

		if (flanks.x == team) {
			PassToFlank(GetTopFlankHeroes());
		}
		else if (flanks.y == team) {
			PassToFlank(GetMidFlankHeroes());
		}
		else if (flanks.z == team) {
			PassToFlank(GetBottomFlankHeroes());
		}
		
	}

	private void PassToFlank(List<Hero>[] flank_heroes)
	{

		for (int i = 0; i < 6; i++) {
			foreach(Hero hero in flank_heroes[i]) {
				if (!hero.Equals(this)) {
					PassTeammate(hero);
					return;
					//Debug.Log(hero);
				}


			}
		}

	}

	private void PassTeammate(Hero hero)
	{

		int layer_mask = 1 << 28;
		Vector3 ball_vector = new Vector3 (ball.transform.position.x, -0.1f, ball.transform.position.z);

		RaycastHit teammate_hit;
		RaycastHit shoot_hit;

		Vector3 player_pos = new Vector3(hero.GetPosition().x, -0.1f, hero.GetPosition().z);
		Ray teammate_ray = new Ray(ball_vector, player_pos - ball_vector);
		Ray shoot_ray = new Ray(ball_vector, -1*(player_pos - ball_vector));

		RotateAroundBall(hero.GetPosition());

		if(Physics.Raycast(teammate_ray, out teammate_hit, Mathf.Infinity, layer_mask)) {
			if (teammate_hit.collider.CompareTag("colliderShoot")) {
				if (colliderAIPossessionCenter.GetComponent<Collider>().Raycast(shoot_ray, out shoot_hit, Mathf.Infinity)) {
					Shoot();
					//Debug.Log("PASS!!!");

				}
			}
		}
		//Debug.Log(player_pos - ball_vector);
		Debug.DrawRay(ball_vector, player_pos - ball_vector);
		Debug.DrawRay(ball_vector, -1*(player_pos - ball_vector));
	}



//	// The vector it returns will be (T,F,F) if the Top flank has at least a teammate and
//	// no opponent in the flank, and the Mid and Bottom flanks have at least one opponent
//	private Vector3 IsTeammateAloneInFlank()
//	{
//		List<Hero>[] top_flank = ai_manager.GetTopFlankHeroes();
//		List<Hero>[] top_flank = ai_manager.GetTopFlankHeroes();
//		List<Hero>[] top_flank = ai_manager.GetTopFlankHeroes();
//	}

	private bool IsInArea(int index)
	{
		if (current_area == index)
			return true;
		else return false;
	}



	private void UpdateBeliefs()
	{
		UpdatePossession();
		UpdateScoringDepth();
		beliefs.teammate_closer_to_ball = GetHeroCloserToBall(team);
		if (team == GlobalConstants.RED)
			beliefs.opponent_closer_to_ball = GetHeroCloserToBall(GlobalConstants.BLUE);
		else
			beliefs.opponent_closer_to_ball = GetHeroCloserToBall(GlobalConstants.RED);
//		if (beliefs.team == GlobalConstants.RED) {
//			beliefs.teammate_going_for_ball = ai_manager.GetGoingForBall(GlobalConstants.RED);
//			beliefs.opponent_going_for_ball = ai_manager.GetGoingForBall(GlobalConstants.BLUE);
//		} else {
//			beliefs.teammate_going_for_ball = ai_manager.GetGoingForBall(GlobalConstants.BLUE);
//			beliefs.opponent_going_for_ball = ai_manager.GetGoingForBall(GlobalConstants.RED);
//		}
	}

	private void UpdateDesires()
	{
//		if (beliefs.has_ball == false)
//			if (beliefs.teammate_has_ball == false)
//				if (beliefs.teammate_going_for_ball == false)
//					GoToBall();
//				else 
//					GoToArea(0);
//			else
//				GoToArea(10);
//		else
//			GoToArea(5);
		
	}

	private void Unmark()
	{
		Vector3 flanks = IsTeammateAloneInFlanks(this);

		List<Hero> pos = GetPlayersInPossession();
		if (pos.Count == 0) return;
		Hero hero = pos[0];
		int area_possession = hero.GetCurrentArea();
		int current_flank = AreaToFlank(area_possession);
		int new_depth = AreaToDepth(area_possession);
		
		if (beliefs.team == GlobalConstants.RED)
			new_depth += -2;
		else
			new_depth += 2;
		
		int area = DepthToArea(current_flank, new_depth );

		int unmark_to_area = 0;
		if (flanks.x == -1)
			unmark_to_area = UnmarkToArea(area, GlobalConstants.TOP_FLANK);
		else if (flanks.y == -1)
			unmark_to_area = UnmarkToArea(area, GlobalConstants.MID_FLANK);
		else if (flanks.z == -1)
			unmark_to_area = UnmarkToArea(area, GlobalConstants.BOTTOM_FLANK);

//		Debug.Log(unmark_to_area);
		GoToArea(unmark_to_area);
		//Debug.Log(AreaToDepth();
	}

	//area: current area with the added depth; flank: new flank to which to unmark
	// return: new area to which to unmark
	private int UnmarkToArea(int area, int flank)
	{
		int current_flank = AreaToFlank(area);



		if (current_flank == flank)
			return area;
		
		else if (current_flank == GlobalConstants.TOP_FLANK) {
			if (flank == GlobalConstants.MID_FLANK)
				return area-1;
			else if (flank == GlobalConstants.BOTTOM_FLANK)
				return area-2;
		
		} else if (current_flank == GlobalConstants.MID_FLANK) {
			if (flank == GlobalConstants.TOP_FLANK)
				return area+1;
			else if (flank == GlobalConstants.BOTTOM_FLANK)
				return area-1;

		} else if (current_flank == GlobalConstants.BOTTOM_FLANK) {
			if (flank == GlobalConstants.TOP_FLANK)
				return area+2;
			else if (flank == GlobalConstants.MID_FLANK)
				return area+1;
		}
		//Debug.Log(area + " - " + current_flank + " - " + flank);
		return -1; //happens if area < 0 || area > 17

	}

	// Receives a flank and a depth (from 1 to 6) and converts that depth
	// to the corresponding area 
	private int DepthToArea(int flank, int depth)
	{

		if (depth > MAX_DEPTH)
			depth = MAX_DEPTH;
		else if (depth < 1)
			depth = 1;

		int area = 0;
		int current_depth = 0;

		while (current_depth < depth){
		
			if (AreaToFlank(area) == flank)
				current_depth++;
		
			area++;
		}

		return area-1;
	}

	private int AreaToDepth(int area)
	{
		int area_counter = 0;
		int current_depth = 1;
		int flank = AreaToFlank(area);
		while (area_counter < area){
			
			if (AreaToFlank(area_counter) == flank) {
			//	Debug.Log(area_counter + " - " + area);
				current_depth++;
			}
			
			area_counter++;
		}
		//Debug.Log(current_depth + " - " + area + " - " + flank);
		//Debug.Log(current_depth);
		return current_depth;

	}

	private void UpdatePossession()
	{

		beliefs.distance_to_ball = FindDistanceToBall();
		beliefs.team_in_possession = GetTeamInPossession();

		bool teammate_has_ball = false;
		bool opponent_has_ball = false;
		beliefs.has_ball = HeroHasBall();

		if (beliefs.team_in_possession == beliefs.team && !beliefs.has_ball)
			beliefs.teammate_has_ball = true;
		else
			beliefs.teammate_has_ball = false;

//		if (beliefs.distance_to_ball < possession_distance_threshold) {
//			beliefs.has_ball = true;
		//	ai_manager.InsertPlayerInPossession(this);
//		} else {
//			beliefs.has_ball = false;
//			ai_manager.RemovePlayerInPossession(this);
	//	}
//		foreach(Hero hero in ai_manager.GetPlayersInPossession()) {
//			if (hero.GetTeam() == this.team)
//				teammate_has_ball = true;
//			else if (hero.GetTeam() != this.team)
//				opponent_has_ball = true;
//		}

//		beliefs.teammate_has_ball = teammate_has_ball;
//		beliefs.opponent_has_ball = opponent_has_ball;

	}

	private float FindDistanceToBall()
	{
		float x;
		float z;

		x = player.transform.position.x - ball.transform.position.x;
		z = player.transform.position.z - ball.transform.position.z;

		return Mathf.Abs(Mathf.Sqrt(x*x + z*z));
	}

	private bool CheckObstructedPath(int index)
	{
		if(beliefs.has_ball == false) {
			beliefs.is_obstructed_path = false;
			return false;
		
		}
		int layer_mask = 1 << 30;
		//int index =  ai_manager.GetPitchAreaCoords(point);
		Vector3 ball_vector = new Vector3 (ball.transform.position.x, -0.1f, ball.transform.position.z);
		Ray ray = new Ray(ball_vector, (GetPitchAreaCoords(index) - ball_vector));

		RaycastHit[] hits;
		hits = Physics.RaycastAll(ray, Mathf.Infinity , layer_mask);

		for (int i=0; i < hits.Length; i++) {
			RaycastHit hit = hits[i];
			if (IsOponnentInArea(int.Parse(hit.collider.name))) {
				beliefs.is_obstructed_path = true;
				return true;
			}
		}
		
//		Debug.DrawRay(ball_vector, ai_manager.GetPitchAreaCoords(index) - ball_vector);
//		Debug.DrawRay(ball_vector, -100*(ai_manager.GetPitchAreaCoords(index) - ball_vector));

		beliefs.is_obstructed_path = false;
		return false;
	}

	private bool IsOponnentInArea(int index)
	{
		List<Hero> hero_list = GetPlayerListFromArea(index);
		foreach(Hero hero in hero_list) {
	//		Debug.Log("hero - " + hero.GetTeam() + "this - " + this.team);
			if (hero.GetTeam() != this.team) {
			//	Debug.Log("true");
				return true;
					
			}
		}
	//	Debug.Log("false");
		return false;
	}

	private bool IsTeammateInArea(int index)
	{
		List<Hero> hero_list = GetPlayerListFromArea(index);
		foreach(Hero hero in hero_list) {
			if (hero.GetTeam() == this.team) {
				return true;
			}
		}
		return false;
	}

	private void GoToArea(int index)
	{
		int below_or_above = IsAboveOrBellow(player.transform.position, GetPitchAreaCoords(index));
		int left_or_right = IsLeftOrRight(player.transform.position, GetPitchAreaCoords(index));

		if (below_or_above == ABOVE)
			Move(DOWN);
		else if (below_or_above == BELOW)
			Move(UP);
		else 
			StopMovingVertically();

		if (left_or_right == LEFT)
			Move (RIGHT);
		else if (left_or_right == RIGHT)
			Move (LEFT);
		else
			StopMovingHorizontally();

	}

	private void Move(int direction)
	{
		if (direction == UP)
			local_player.player_controller.commands.vertical_direction = MOVE_UP;
		else if (direction == DOWN)
			local_player.player_controller.commands.vertical_direction = MOVE_DOWN;
		else if (direction == LEFT)
			local_player.player_controller.commands.horizontal_direction = MOVE_LEFT;
		else if (direction == RIGHT)
			local_player.player_controller.commands.horizontal_direction = MOVE_RIGHT;
	}

	private void StopMovingVertically()
	{
		local_player.player_controller.commands.vertical_direction = 0;
	}

	private void StopMovingHorizontally()
	{
		local_player.player_controller.commands.horizontal_direction = 0;
	}

	private void DribbleToArea(int index)
	{

		goto_area = index;

		RaycastHit hit;
		
		Vector3 ball_vector = new Vector3 (ball.transform.position.x, -0.1f, ball.transform.position.z);
		Ray ray = new Ray(ball_vector, -1*(GetPitchAreaCoords(index) - ball_vector));

		if (!beliefs.has_ball) {
	
			GoToBall();

		} else {

			ResetControllers();
		
			RotateAroundBall(GetPitchAreaCoords(index));
			
			int below_or_above = IsAboveOrBellow(ball_behaviour.transform.position, GetPitchAreaCoords(index));
			int left_or_right = IsLeftOrRight(ball_behaviour.transform.position, GetPitchAreaCoords(index));

			if (player_collider.GetComponent<Collider>().Raycast(ray, out hit, Mathf.Infinity)) {
				if (colliderAIPossessionCenter.GetComponent<Collider>().Raycast(ray, out hit, Mathf.Infinity)) {
				//	Debug.Log("HIT");
					if (below_or_above == ABOVE) //if ball's index is above the target's, that means player is above the ball so he must move down
						local_player.player_controller.commands.vertical_direction = MOVE_DOWN;
					else if (below_or_above == BELOW)
						local_player.player_controller.commands.vertical_direction = MOVE_UP;
					else
						local_player.player_controller.commands.vertical_direction = 0;
					
					if (left_or_right == LEFT)
						local_player.player_controller.commands.horizontal_direction = MOVE_RIGHT;
					else if (left_or_right == RIGHT)
						local_player.player_controller.commands.horizontal_direction = MOVE_LEFT;
					else
						local_player.player_controller.commands.horizontal_direction = 0;
				
				} else if (colliderAIPossessionLeft.GetComponent<Collider>().Raycast(ray, out hit, Mathf.Infinity)) {
				//	Debug.Log("LEFT");
					ResetControllers();
					AdjustAccordingToQuadrant(LEFT);
				
				} else if (colliderAIPossessionRight.GetComponent<Collider>().Raycast(ray, out hit, Mathf.Infinity)) {
				//	Debug.Log("RIGHT");
					ResetControllers();
					AdjustAccordingToQuadrant(RIGHT);
				}
			}
		}
//		Debug.DrawRay(ball_vector, ai_manager.GetPitchAreaCoords(index) - ball_vector);
//		Debug.DrawRay(ball_vector, -100*(ai_manager.GetPitchAreaCoords(index) - ball_vector));


	}


	private void RotateAroundBall(Vector3 target)
	{
		int ball_below_or_above_target = IsAboveOrBellow(ball_behaviour.transform.position, target);
		int ball_left_or_right_target = IsLeftOrRight(ball_behaviour.transform.position, target);

		int player_left_or_right_target = IsLeftOrRight(ball_behaviour.transform.position, target);
		int player_below_or_above_target = IsAboveOrBellow(player.transform.position, target);

		int player_below_or_above_ball = IsAboveOrBellow(player.transform.position, ball.transform.position);
		int player_left_or_right_ball = IsLeftOrRight(player.transform.position, ball.transform.position);


		float ball_target_slope = GetSlope(ball.transform.position, target);
		float ball_target_y_intercept = GetYIntercept(ball_target_slope, ball.transform.position);

		float player_target_slope = GetSlope(player.transform.position, target);

//		Debug.Log(IsAboveLine(player.transform.position, ball_target_slope, ball_target_y_intercept));

		if (IsAboveLine(player.transform.position, ball_target_slope, ball_target_y_intercept))
			if (ball_below_or_above_target == ABOVE)
				if (ball_target_slope > 0)
					RotateAroundBallClockwise();
				else
					RotateAroundBallCounterclockwise();
			else if (ball_below_or_above_target == BELOW)
				if(ball_target_slope > 0)
					RotateAroundBallCounterclockwise();
				else
					RotateAroundBallClockwise();
			else if(ball_left_or_right_target == LEFT)
				RotateAroundBallCounterclockwise();
			else
				RotateAroundBallClockwise();
		else
			if (ball_below_or_above_target == ABOVE)
				if(ball_target_slope > 0)
					RotateAroundBallCounterclockwise();
				else
					RotateAroundBallClockwise();
			else if (ball_below_or_above_target == BELOW)
				if (ball_target_slope > 0)
					RotateAroundBallClockwise();
				else
					RotateAroundBallCounterclockwise();
			else if(ball_left_or_right_target == LEFT)
				RotateAroundBallClockwise();
			else
				RotateAroundBallCounterclockwise();
		//	else
		//	RotateAroundBallClockwise();
	

	//	Debug.Log("player slope -> " + player_target_slope + " ball slope -> " + ball_target_slope);
		
		//ROTATE CLOCKWISE

		//Debug.Log(ball_left_or_right_target == LEFT);

		

	//	Debug.Log(ball_target_slope);
	}

	private float GetYIntercept(float slope, Vector3 point)
	{
		//y = m.x + b

		float y = point.x;
		float x = -point.z;

		float b = y - slope * x;

		return b;
	}

	private bool IsAboveLine(Vector3 point, float slope, float b)
	{
	//	Debug.Log("player point -> " + point);
		float x = -point.z;

		float y = x * slope + b;

		if (y > point.x)
			return false;

		else
			return true;
	}

	private float GetSlope(Vector3 vec1, Vector3 vec2)
	{
		float m = 0;

		m = -(vec2.x - vec1.x)/(vec2.z - vec1.z);

		return m;
	}



	//called when AI is controlling the ball but it slips to either of its left or right possession colliders
	private void AdjustAccordingToQuadrant(int left_or_right)
	{
		int quadrant = GetQuadrant();

		//Debug.Log("quadrant -> " + quadrant + " left_or_right-> " + left_or_right);
		ResetControllers();
		//if ray hit the left collider
		if (left_or_right == LEFT) {
			if(quadrant == 1)
				local_player.player_controller.commands.vertical_direction = MOVE_UP;
			else if (quadrant == 2)
				local_player.player_controller.commands.horizontal_direction = MOVE_LEFT;
			else if (quadrant == 3)
				local_player.player_controller.commands.vertical_direction = MOVE_DOWN;
			else if (quadrant == 4)
				local_player.player_controller.commands.horizontal_direction = MOVE_RIGHT;

		} else { //if ray hit the right collider
			if (quadrant == 1)
				local_player.player_controller.commands.horizontal_direction = MOVE_RIGHT;
			else if (quadrant == 2)
				local_player.player_controller.commands.vertical_direction = MOVE_UP;
			else if (quadrant == 3)
				local_player.player_controller.commands.horizontal_direction = MOVE_LEFT;
			else if (quadrant == 4) {
				local_player.player_controller.commands.vertical_direction = MOVE_DOWN;
			//Debug.Log("moving down");
			}
		}
	}

	private int IsAboveOrBellow(Vector3 ball_pos, Vector3 target_pos)
	{

		if (ball_pos.x > target_pos.x)
			return ABOVE;
		else
			return BELOW;

	}


	private void ResetControllers()
	{
		local_player.player_controller.commands.shoot = 0;
		local_player.player_controller.commands.vertical_direction = 0;
		local_player.player_controller.commands.horizontal_direction = 0;
	}

	private void GoToBall()
	{
		if (local_player.transform.position.x > ball.transform.position.x)
			local_player.player_controller.commands.vertical_direction = MOVE_DOWN;
		if (local_player.transform.position.x < ball.transform.position.x)
			local_player.player_controller.commands.vertical_direction = MOVE_UP;
		if (local_player.transform.position.z > ball.transform.position.z)
			local_player.player_controller.commands.horizontal_direction = MOVE_RIGHT;
		if (local_player.transform.position.z < ball.transform.position.z)
			local_player.player_controller.commands.horizontal_direction = MOVE_LEFT;
	}

	private void RotateAroundBallCounterclockwise()
	{
		int quadrant = GetQuadrant();

	//	Debug.Log(quadrant);

		if (quadrant == 1) {
		
			local_player.player_controller.commands.horizontal_direction = MOVE_RIGHT;
			local_player.player_controller.commands.vertical_direction = 0;
		
		} else if (quadrant == 2) {

			local_player.player_controller.commands.vertical_direction = MOVE_UP;
			local_player.player_controller.commands.horizontal_direction = 0;
		
		} else if (quadrant == 3) {
		
			local_player.player_controller.commands.horizontal_direction = MOVE_LEFT;
			local_player.player_controller.commands.vertical_direction = 0;
			
		} else if (quadrant == 4) {

			local_player.player_controller.commands.horizontal_direction = 0;
			local_player.player_controller.commands.vertical_direction = MOVE_DOWN;
		
		}
	}

	private void RotateAroundBallClockwise()
	{
		int quadrant = GetQuadrant();

		
		if (quadrant == 1) {
			
			local_player.player_controller.commands.horizontal_direction = 0;
			local_player.player_controller.commands.vertical_direction = MOVE_UP;
			
		} else if (quadrant == 2) {
			
			local_player.player_controller.commands.vertical_direction = 0;
			local_player.player_controller.commands.horizontal_direction = MOVE_LEFT;
			
		} else if (quadrant == 3) {
			
			local_player.player_controller.commands.horizontal_direction = 0;
			local_player.player_controller.commands.vertical_direction = MOVE_DOWN;
			
		} else if (quadrant == 4) {
			
			local_player.player_controller.commands.horizontal_direction = MOVE_RIGHT;
			local_player.player_controller.commands.vertical_direction = 0;
			
		}
	}

	private float GetAnglePlayerBall()
	{
		Vector2 a = new Vector2(ball.transform.position.x - player.transform.position.x, ball.transform.position.z-player.transform.position.z);
		Vector2 b = new Vector2(player.transform.position.x, player.transform.position.z);

		return Vector2.Angle(a,b);
	}

	private int GetQuadrant()
	{
		float Xb = ball.transform.position.x;
		float Xp = player.transform.position.x;

		float Zb = ball.transform.position.z;
		float Zp = player.transform.position.z;

		if (Xb > Xp)
		
			if (Zb < Zp) 
				return 1;
			else
				return 2;

		else

			if (Zb < Zp)
				return 4;
			else 
				return 3;

		return 0;

	}

	private int IsLeftOrRight(Vector3 area, Vector3 current_area)
	{

		if (area.z > current_area.z)
			return LEFT;
		else
			return RIGHT;
		
	}

	public override void UsePower(PlayerController.Commands commands){}

	public override void EmmitPowerFX(string type = "none"){}

	public void GoalScored()
	{		
		Debug.Log("GOAL NOTIFICATION");
	}

}
