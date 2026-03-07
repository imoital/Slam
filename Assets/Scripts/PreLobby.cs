using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;

public class PreLobby : Lobby 
{
	
	/*********** CONSTANTS ******************/

	private enum lobby_states {team_selection, hero_selection};

	/****************************************/

	public GameObject local_player_prefab;
	public GameObject choose_hero_prefab;
	private List<Camera> heroes_camera_list = new List<Camera>();
	public GameObject other_hero_choices_prefab;
	public GameObject network_loading_prefab;

	public Material team_1_material;
	public Material team_2_material;

	private int lobby_state;
	private int players_ready = 0;

	private Camera[] other_choices_cameras;
	private float native_horizontal_resolution = 1296f;
	private float native_vertical_resolution = 729f;

	bool[] is_pressed_button = new bool[4] {false, false, false, false};
	private bool hero_selection_initialized = false;

	void Awake()
	{
		base.Awake();
		lobby_state = (int)lobby_states.team_selection;

		GameObject settings = GameObject.FindGameObjectWithTag("settings");
		if(settings == null) {
			settings = (GameObject)Instantiate(settings_prefab, settings_prefab.transform.position, settings_prefab.transform.rotation);
			game_settings = settings.GetComponent<Game_Settings>();
			game_settings.local_game = true;
		} else {
			game_settings = settings.GetComponent<Game_Settings>();
			game_settings.local_game = game_settings.IsLocalGame();
		}
		
		if (game_settings.IsLocalGame()) {
			AddLocalPlayer(0, "Keyboard", SPECTATING);
			for (int i = 0; i < Input.GetJoystickNames().Length; i++) {
				AddLocalPlayer(i+1, "Gamepad " + (i+1), SPECTATING);
			}
		}

		show_lobby_arrows = true;
		NotifyLobbyChanged();
		
	}

	public bool IsTeamSelectionState()
	{
		return lobby_state == (int)lobby_states.team_selection;
	}

	public bool IsHeroSelectionState()
	{
		return lobby_state == (int)lobby_states.hero_selection;
	}

	public Camera[] GetHeroSelectionCameras()
	{
		return heroes_camera_list.ToArray();
	}

	public bool CanAddBot(int index)
	{
		return index >= 0 && index < is_pressed_button.Length && index < heroes_camera_list.Count && !is_pressed_button[index] && IsHeroSelectionState();
	}

	public void TryAddBot(int index)
	{
		if (!CanAddBot(index)) return;

		if (index == 0 || index == 2) {
			game_settings.IncBlueTeamBots();
			game_settings.team_2_count++;
		}
		else {
			game_settings.IncRedTeamBots();
			game_settings.team_1_count++;
		}

		is_pressed_button[index] = true;
		NotifyLobbyChanged();
	}

	public void StartHeroSelection()
	{
		if(!game_settings.IsLocalGame()) {
			return;
		}

		if(!hero_selection_initialized) {
			LocalHeroSelectScreen();
			hero_selection_initialized = true;
		}

		lobby_state = (int)lobby_states.hero_selection;
		HeroScreen();
		NotifyLobbyChanged();
	}

	public void RestartLobby()
	{
		if (!game_settings.IsLocalGame()) return;

		game_settings.players_list.Clear();
		Application.LoadLevel("Pre_Game_Lobby");
	}

	public void BackOrDisconnect()
	{
		if(!game_settings.IsLocalGame()) {
			BackToMainMenu();
		} else if (!string.IsNullOrEmpty(game_settings.main_menu_scene)) {
			Application.LoadLevel(game_settings.main_menu_scene);
		} else {
			Debug.LogWarning("PreLobby could not go back because main_menu_scene is empty.", this);
		}
	}

	void HeroScreen()
	{
		if(other_choices_cameras != null && other_choices_cameras.Length != 0) {
			for(int i = 0; i < 4; i++) {
				if(other_choices_cameras[i]) {
					Camera other_hero_camera = other_choices_cameras[i];
					GUI.Box(new Rect(other_hero_camera.pixelRect.x, 
					                 Screen.height - other_hero_camera.pixelRect.yMax, 
					                 other_hero_camera.pixelWidth, 
					                 other_hero_camera.pixelHeight), 
					        "") ;
				}
			}
		}
	}

	private void BotActivationGUI()
	{


		for (int i = 0; i < heroes_camera_list.Count; i++) {
			//Debug.Log(screen_to_viewport.x);
			Vector3 add_bot_label_position = (heroes_camera_list[i].ViewportToScreenPoint(new Vector3((Screen.width*0.315f)/748, 0.5f, 0)));
			if(CanAddBot(i) && GUI.Button(new Rect(add_bot_label_position.x, -add_bot_label_position.y+Screen.height,100,30), "Add Bot")) {
				TryAddBot(i);
			}
		}
	}

	void LocalHeroSelectScreen()
	{
		if (hero_selection_initialized) return;

		int total_players_team_1 = team_1.Count;
		int total_players_team_2 = team_2.Count;

		int player_number = 0;

		for(int i = 0; i < 4; i++) {

			float team = i % 2f;

			GameObject choose_hero = (GameObject)Instantiate(choose_hero_prefab);
			Camera choose_hero_camera = choose_hero.transform.Find("Main Camera").GetComponent<Camera>();
		
			Vector3 new_choose_hero_position = new Vector3(choose_hero.transform.position.x,
			                                               choose_hero.transform.position.y + 30*i,
			                                               choose_hero.transform.position.z
			                                              );
			choose_hero.transform.position = new_choose_hero_position;
			choose_hero_camera.GetComponent<Camera>().rect = new Rect(0.05f + (0.5f * team), 0.55f - (0.5f * player_number), 0.4f, 0.4f);

			choose_hero.transform.name = "team_" + (team + 1) + "_" + (player_number + 1);

			Hero_Selection hero_script = choose_hero.GetComponent<Hero_Selection>();

			if((team + 1) == 1 && total_players_team_1 != 0) {
				hero_script.InitializeLocalPlayer(TEAM_1, team_1[player_number].name, i, team_1[player_number].controller, this);
				total_players_team_1--;
			} else if((team + 1) == 2 && total_players_team_2 != 0) {
				hero_script.InitializeLocalPlayer(TEAM_2, team_2[player_number].name, i, team_2[player_number].controller, this);
				total_players_team_2--;
			} else {
				heroes_camera_list.Add(choose_hero_camera);
				GameObject lights = choose_hero.transform.Find("Lights").gameObject;
				lights.SetActive(false);
				GameObject halo = choose_hero.transform.Find("ready_led").Find("Halo").gameObject;
				halo.SetActive(false);
				hero_script.SetTeam((int)team + 1);
			//	PrintAddBotOverlay();
			}

			player_number += (i%2);
		}
	}

	public void PlayerReady(Hero_Selection.Player player)
	{
		if(game_settings.IsLocalGame()) {
			players_ready++;
			game_settings.AddPlayer(player);
			NotifyLobbyChanged();

			if (players_ready == (team_1.Count + team_2.Count))
				Application.LoadLevel("Main_Game");

		}
	}

	protected override void LobbyMenu()
	{

		if(!game_settings.IsLocalGame() && GUILayout.Button("Disconnect", GUILayout.MinWidth(0.15f*Screen.width), GUILayout.MinHeight(0.06f*Screen.height))){
			BackOrDisconnect();
		} else if(game_settings.IsLocalGame() && GUILayout.Button("Back", GUILayout.MinWidth(0.15f*Screen.width), GUILayout.MinHeight(0.06f*Screen.height))) {
			BackOrDisconnect();
		}
		GUILayout.FlexibleSpace();
		if (game_settings.IsLocalGame()) {
			if(GUILayout.Button("Restart and Refresh", GUILayout.MinWidth(0.15f*Screen.width), GUILayout.MinHeight(0.06f*Screen.height))) {
				RestartLobby();
			}
			GUILayout.FlexibleSpace();
		}
		if(game_settings.IsLocalGame()){
			if(!game_settings.IsLocalGame())
				GUILayout.FlexibleSpace();
			if(GUILayout.Button("Start", GUILayout.MinWidth(0.15f*Screen.width), GUILayout.MinHeight(0.06f*Screen.height))) {
				StartHeroSelection();
			}
		}
	}

	protected override void LobbyStates()
	{
		switch (lobby_state)
		{
		case (int)lobby_states.team_selection:
			LobbyScreen();
			break;
		case (int)lobby_states.hero_selection:
			BotActivationGUI();
			break;
		}
	}

}
