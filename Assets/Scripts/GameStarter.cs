using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameStarter : MonoBehaviour {

	Game_Settings game_settings;
	int team_1_count;
	int team_2_count;
	float distance_team_1;
	float distance_team_2;

	public GameObject court_start_position_team_1;
	public GameObject court_start_position_team_2;

	public GameObject local_player_prefab;
	public GameObject local_game_prefab;

	public GameObject net_player_prefab;
	public GameObject net_game_prefab;

	public GameObject settings_prefab;
	public GameObject AI_prefab;

	private GameObject game_manager_object;

	private int red_bots = 0;
	private int blue_bots = 0;

	// Use this for initialization
	void Start () {

		GameObject settings = GameObject.Find("Settings(Clone)");
		if(settings == null)
			settings = (GameObject)Instantiate(settings_prefab);

//		GameObject ai_manager = GameObject.Find("AIManager");
//		if (ai_manager == null)
//			ai_manager = (GameObject)Instantiate(AI_prefab);

		game_settings = settings.GetComponent<Game_Settings>();

		team_1_count = game_settings.team_1_count;
		team_2_count = game_settings.team_2_count;

		red_bots = game_settings.GetRedTeamBots();

		float court_lenght = court_start_position_team_1.transform.position.x*(-2);
		distance_team_1 = court_lenght/(team_1_count+1);
		distance_team_2 = court_lenght/(team_2_count+1);

		if(game_settings.IsLocalGame())
			StartLocalGame();
		
	}

	private void StartLocalGame()
	{
		Instantiate(local_game_prefab, Vector3.zero, transform.rotation);

		for (int i = 0; i < game_settings.players_list.Count; i++) {
			Hero_Selection.Player player = game_settings.players_list[i];
			InstantiateNewLocalPlayer(CalculatePosition(player.team), 
			                          player.team, 
			                          player.player_name, 
			                          player.controller, 
			                          player.texture_id, 
			                          player.hero_index
			                         );
		}

		for (int i = 0; i < game_settings.GetRedTeamBots(); i++) {
		
			InstantiateNewLocalPlayer(CalculatePosition(1), 1, "AI", i*(-1)-1, 4, 2); // bots have negative controller to differentiate
			int x =  i*(-1)-1;
			Debug.Log("RED" + x);
		}

		for (int i = 0; i < game_settings.GetBlueTeamBots(); i++) {
			
			InstantiateNewLocalPlayer(CalculatePosition(2), 2, "AI", (game_settings.GetRedTeamBots() + i)*(-1)-1, 4, 2);
			int x =  (game_settings.GetRedTeamBots() + i)*(-1)-1;
			Debug.Log("BLUE" + x);
		}

	}

	private Vector3 CalculatePosition(int team)
	{
		Vector3 start_position = new Vector3(0,0,0);
		GameObject court_start_position;
		float distance_team;
		int team_count;

		if(team == 1) {
			court_start_position = court_start_position_team_1;
			distance_team = distance_team_1;
			team_count = team_1_count;
			team_1_count--;
		} else {
			court_start_position = court_start_position_team_2;
			distance_team = distance_team_2;
			team_count = team_2_count;
			team_2_count--;
		}

		start_position = court_start_position.transform.position;
		start_position.x = start_position.x + distance_team*team_count;

		return start_position;
	}

	private void InstantiateNewLocalPlayer(Vector3 start_position, int team, string name, int controller, int texture_id, int hero_index)
	{
		GameObject player = (GameObject)Instantiate(local_player_prefab, start_position, transform.rotation);

		Local_Player local_player = (Local_Player)player.GetComponent<Local_Player>();
		local_player.InitializePlayerInfo(team, name, start_position, controller, texture_id, hero_index);
	}
}
