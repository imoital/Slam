using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Game_Behaviour : MonoBehaviour {
	
	// For when a player scores
	private string GOAL_STR = "GOAL!";
	private int GOAL_STR_CHAR_WIDTH = 60;

	private bool is_golden_goal_drawn = false;
	
	// initial ball position
	public Vector3 ball_position = new Vector3(0, -0.04788643f, 0);
	
	// initial z position for player to be distribute
	public float team_1_inicial_z_position = 7.812522f;
	public float team_2_inicial_z_position = -7.812522f;

	public int num_team_1_players = 1;
	public int num_team_2_players = 1;

	public GameObject player_prefab;
	public GameObject ball_prefab;

	public Camera m_camera;
	public GameObject settings_prefab;
	
	public GameObject crowd_team_1;
	public GameObject crowd_team_2;
	
//	float timer = 200f;
	float ENDGAME_TIMER = 100f;

	protected List<GameObject> players_team_1 = new List<GameObject>();
	protected List<GameObject> players_team_2 = new List<GameObject>();
	protected GameObject ball;
	
	protected bool trigger_timer;
	protected bool finish_game = false;
	// timer for celebration
	public float timer_value;
	
	public  int scored_team = 0;
	
	protected int score_team_1 = 0;
	protected int score_team_2 = 0;
	
	protected GUIManager gui_manager;
	public GUIStyle main_game_manager;
	protected bool is_celebrating = false;
	
	protected int team_celebrating;
	public AudioClip goal_cheer;
	
	protected int team_scored = 0;
	protected float DEFAULT_TEAM_SCORED_MESSAGE_XPOS = -400f;
	protected float TEAM_SCORED_MESSAGE_SPEED_MULTIPLIER = 400f;
	protected float team_scored_message_xpos;
	
	
	public GameObject spawn_team_1;
	public GameObject spawn_team_2;
	public GameObject player_controller;
	
	public Texture[] player_arrows;
	
	private bool isTimeUp = false;
	
	public bool isOnLobbyScreen = false;

	private LED_Screen led_screen_script;
	public bool is_game_going;
	
	public void ScoreTeam(int team)
	{		
		if(team == 1) {
			TeamReaction(1, "Celebrate");
			TeamReaction(2, "Sad");
			scored_team = 1;
		} else {
			TeamReaction(2, "Celebrate");
			TeamReaction(1, "Sad");
			scored_team = 2;
		}	
		
		timer_value = 0f;
		trigger_timer = true;
	}
	
	public void FinishGame()
	{
		Destroy(GameObject.FindGameObjectWithTag("settings"));
		Application.LoadLevel("Pre_Game_Lobby");
	}
	
//	public void setTimeUp(bool val)
//	{
//		isTimeUp = val;
//	}
	
	public virtual void ReleasePlayers()
	{
		NotificationCenter.DefaultCenter.PostNotification(this, "ReleasePlayers");
		is_game_going = true;
	}
	
	protected virtual void MovePlayersToStartPositions(){}
	
	protected void StartGameAgain()
	{
		StopCelebration();

		MovePlayersToStartPositions();
		trigger_timer = false;
		is_game_going = false;
	}
	
	public void TimeFinished()
	{
		timer_value = 0f;
		isTimeUp = true;
		
		if(score_team_1 > score_team_2){
			TeamReaction(1, "Celebrate");
			TeamReaction(2, "Sad");
			finish_game = true;
			is_celebrating = true;
			trigger_timer = true;
			AudioSource.PlayClipAtPoint(goal_cheer, Vector3.zero);
		} else if(score_team_1 < score_team_2) {
			TeamReaction(2, "Celebrate");
			TeamReaction(1, "Sad");
			finish_game = true;
			is_celebrating = true;
			trigger_timer = true;
			AudioSource.PlayClipAtPoint(goal_cheer, Vector3.zero);
		} else {
			finish_game = false;
			is_celebrating = false;
		}
		
//		AudioSource.PlayClipAtPoint(goal_cheer, Vector3.zero);
	}
	
	protected void TeamReaction(int team, string reaction)
	{
	
		Hashtable data = new Hashtable();
		data["team"] = team;
		data["reaction"] = reaction;
		
		NotificationCenter.DefaultCenter.PostNotification(this, "ChangeReaction", data);
	}

	protected bool IsOdd(int num)
	{
		return (num % 2) != 0;
	}

	void DestroyAllPlayers()
	{
		for(int i = 0; i < players_team_1.Count; i++)
			Destroy(players_team_1[i]);
		players_team_1.Clear();

		for(int i = 0; i < players_team_2.Count; i++)
			Destroy (players_team_2[i]);
		players_team_2.Clear();
	}

	protected string GetPlayerNum(string player_name)
	{
		return player_name.Replace("Player ", "");
	}

	protected void AddPlayerToTeam(int team, GameObject player) 
	{	
		if(team == 1)
			players_team_1.Add(player);
		else
			players_team_2.Add(player);
	}
	
	public Texture GetTexture(int id)
	{
		return player_arrows[id];
	}
	
	protected void Start() 
	{
		NotificationCenter.DefaultCenter.AddObserver(this, "OnGoal");
		GameObject guiManager = GameObject.FindGameObjectWithTag("GuiManager");
		gui_manager = guiManager.GetComponent<GUIManager>();
		GameObject led_screen = GameObject.FindGameObjectWithTag("LedScreen");
		led_screen_script = led_screen.GetComponent<LED_Screen>();
		
		m_camera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
		is_game_going = false;
		MovePlayersToStartPositions();
		
	}

	// Update is called once per frame
	protected void Update () 
	{
		if(trigger_timer){
			if(timer_value > ENDGAME_TIMER && !finish_game)
				StartGameAgain();
			else if(timer_value > ENDGAME_TIMER && finish_game)
				FinishGame();
			else timer_value++;
//			Debug.Log("Endgame: " + ENDGAME_TIMER + " -> " + timer_value);
		}
		
		if(is_celebrating)
			team_scored_message_xpos += (Time.deltaTime * TEAM_SCORED_MESSAGE_SPEED_MULTIPLIER);

		if (isTimeUp) {
			if (score_team_1 == score_team_2 && !is_golden_goal_drawn) {
				led_screen_script.DrawTieMessage();
				is_golden_goal_drawn = true;
			}
		}
//		if (is_celebrating && !isTimeUp)
////			gui_manager.DrawGoalScored(team_scored, GOAL_STR);
//			led_screen_script.DrawGoalScored(team_scored);

		if(Input.GetKeyUp(KeyCode.Escape)) {

		}
	}
	
	public int StopCelebration()
	{
		is_celebrating = false;
		team_scored_message_xpos = DEFAULT_TEAM_SCORED_MESSAGE_XPOS;
		
		if (!is_celebrating)
			NotificationCenter.DefaultCenter.PostNotification(this, "StopCelebration");
		
		return 0;
	}
	
	protected void OnGoal(NotificationCenter.Notification notification)
	{		
		if(!is_celebrating){
			if((int)notification.data["team"] == 1) {
				team_scored = 1;
				score_team_1++;
				ScoreTeam(1);
				team_celebrating = 1;
			}
			else {
				team_scored = 2;
				score_team_2++;
				ScoreTeam(2);
				team_celebrating = 2;
			}
			led_screen_script.DrawGoalScored((int)notification.data["team"]);
			if (isTimeUp)
				finish_game = true;
			
			AudioSource.PlayClipAtPoint(goal_cheer, Vector3.zero);
			is_celebrating = true;			
			ScoreBoard scoreboard = GameObject.Find("Score Board").GetComponent<ScoreBoard>();
			scoreboard.UpdateScore(score_team_1, score_team_2);

		} 

		if(ball == null)
			ball = GameObject.FindGameObjectWithTag("ball");
		Ball_Behaviour bb = ball.GetComponent<Ball_Behaviour>();
		GameObject last_player_touched = bb.GetLastPlayerTouched();
		Kickoff_Player pb = last_player_touched.GetComponent<Kickoff_Player>();
		int player_score_team = pb.GetTeam();
		if(player_score_team == team_scored)
			pb.GoalScored();
	}
	
	public int GetTeamScore(int team)
	{
		if (team == 1)
			return score_team_1;
		else
			return score_team_2;
	}
	
	protected void OnGUI()
	{	
//		if (is_celebrating && !isTimeUp)
//			gui_manager.DrawGoalScored(team_scored, GOAL_STR);
		if (isTimeUp) {
//			if (score_team_1 == score_team_2 && !drawn) {
//				led_screen_script.DrawTieMessage();
//				drawn = true;
//				Debug.Log("drawing");
//			}
			if (score_team_1 > score_team_2)
				gui_manager.DrawGoalScored(1, "RED TEAM WINS");
			else if (score_team_1 < score_team_2)
				gui_manager.DrawGoalScored(2, "BLUE TEAM WINS");
		}
	}
}