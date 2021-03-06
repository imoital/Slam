﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameStarter_1 : MonoBehaviour {
	
	Game_Settings game_settings;
	int team_1_count;
	int team_2_count;
	float distance_team_1;
	float distance_team_2;
	
	public GameObject court_start_position_team_1;
	public GameObject court_start_position_team_2;
	
	public GameObject local_player_prefab;
	public GameObject local_game_prefab;
	
	//public GameObject net_player_prefab;
	//public GameObject net_game_prefab;
	
	public GameObject settings_prefab;
	public GameObject AI_prefab;
	
	private GameObject game_manager_object;
	private AIManager AIManager;
	
	private int red_bots = 2;
	private int blue_bots = 2;
	
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
		
		red_bots = 2;
		
		float court_lenght = court_start_position_team_1.transform.position.x*(-2);
		distance_team_1 = court_lenght/(team_1_count+1);
		distance_team_2 = court_lenght/(team_2_count+1);
		
		if(game_settings.IsLocalGame())
			StartLocalGame();
//		else if(Network.isServer)
//			StartNetworkGame();
		
		GameObject AIManager_object = GameObject.Find("AIManager");
		AIManager = AIManager_object.GetComponent<AIManager>();
		
	}
	
	private void StartLocalGame()
	{
		Instantiate(local_game_prefab, Vector3.zero, transform.rotation);
		
		
		
		//		for (int i = 0; i < 4; i++) {
		//			Hero_Selection.Player player = game_settings.players_list[i];
		//			InstantiateNewLocalPlayer(CalculatePosition(player.team), 
		//			                          player.team, 
		//			                          player.player_name, 
		//			                          player.controller, 
		//			                          player.texture_id, 
		//			                          player.hero_index
		//			                         );
		//		}
		
		for (int i = 0; i < 2; i++) { //red bots
			
			InstantiateNewLocalPlayer(CalculatePosition(1), 1, "AI", i*(-1)-1, 4, 2); // bots have negative controller to differentiate
			int x =  i*(-1)-1;
		}
		
		for (int i = 0; i < 2; i++) { // blue
			
			InstantiateNewLocalPlayer(CalculatePosition(2), 2, "AI", (2 + i)*(-1)-1, 4, 2);
			int x =  (2 + i)*(-1)-1;
		}
		
	}
	
//	public void StartNetworkGame()
//	{		
//		int texture_id = 0;
//		int team_1_total = team_1_count;
//		int team_2_total = team_2_count;
//		
//		game_manager_object = (GameObject)Network.Instantiate(net_game_prefab, Vector3.zero, transform.rotation, 0);
//		
//		foreach (Hero_Selection.Player player in game_settings.players_list) {
//			Vector3 start_position = new Vector3(0,0,0);
//			if(player.team == 1){
//				start_position = court_start_position_team_1.transform.position;
//				start_position.x = start_position.x + distance_team_1*team_1_total;
//				team_1_total--;
//			} else {
//				start_position = court_start_position_team_2.transform.position;
//				start_position.x = start_position.x + distance_team_2*team_2_total;
//				team_2_total--;
//			}
//			
//			InstantiateNewNetworkPlayer(start_position, player.network_player, player.team, player.player_name, texture_id, player.hero_index);
//			
//			texture_id++;
//		}
//	}
	
//	void InstantiateNewNetworkPlayer(Vector3 start_position, NetworkPlayer network_player, int team, string name, int texture_id, int hero_index) 
//	{
//		GameObject player = (GameObject)Network.Instantiate(net_player_prefab, start_position, transform.rotation, 0);
//		
//		Network_Player np = (Network_Player)player.GetComponent<Network_Player>();
//		np.InitializePlayerInfo(network_player, team, name, start_position, texture_id, hero_index);
//		np.Start();
//		
//		Network_Game net_game = game_manager_object.GetComponent<Network_Game>();
//		if(net_game.is_game_going) {
//			np.ReleasePlayer();
//		} else {
//			np.UpdateCollisions(net_game.scored_team);
//		}
//	}
	
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
