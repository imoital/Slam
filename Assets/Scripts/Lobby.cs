using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public abstract class Lobby : MonoBehaviour {

	/*********** CONSTANTS ******************/
	
	protected const int JOIN_TAB = 0;
	protected const int CREATE_TAB = 1;
	protected const int FAVOURITES_TAB = 2;
	protected const int SETTINGS_TAB = 3;
	
	protected const int NICKNAME_SCREEN = 0;
	protected const int MAIN_MENU = 1;
	protected const int LOBBY = 2;
	
	protected const int SPECTATING = 0;
	protected const int TEAM_1 = 1;
	protected const int TEAM_2 = 2;

	/****************************************/

	protected struct Player
	{
		public string name;
		public int team;
		public int controller;
		
		public int hero;
		public bool ready;
		public GameObject hero_choosen;
	}

	public struct LobbyPlayerView
	{
		public string name;
		public int team;
		public int controller;
	}

	protected int team_1_color;
	protected int team_2_color;

	protected List<Player> spectating;
	protected List<Player> team_1;
	protected List<Player> team_2;

	protected Game_Settings game_settings;
	public GameObject settings_prefab;
	public bool useLegacyGUI = true;

	protected Player self_player;
	public GUISkin gui_skin;

	public string[] team_colors = new string[] {"Red", "Blue", "Green"};

	protected bool show_lobby;
	protected bool show_lobby_arrows;
	public event System.Action LobbyChanged;

	protected void Awake()
	{
		team_1_color = 0;
		team_2_color = 1;
		
		spectating = new List<Player>();
		team_1 = new List<Player>();
		team_2 = new List<Player>();

		show_lobby = true;
	}

	protected void NotifyLobbyChanged()
	{
		if (LobbyChanged != null) {
			LobbyChanged();
		}
	}

	protected void Start()
	{
		GameObject settings = GameObject.FindGameObjectWithTag("settings");
		if(settings != null) {
			game_settings = settings.GetComponent<Game_Settings>();
			game_settings.local_game = game_settings.IsLocalGame();
		}
	}

	Player CreatePlayer(string player_name, int team)
	{		
		Player player = new Player();
		player.name = player_name;
		player.team = team;
		return player;
	}
	
	protected void AddLocalPlayer(int controller, string player_name, int team=0) 
	{
		Player player = CreatePlayer(player_name, team);
		player.controller = controller;
		switch(team){
		case SPECTATING:
			spectating.Add(player);
			break;
		case TEAM_1:
			team_1.Add(player);
			break;
		case TEAM_2:
			team_2.Add(player);
			break;
		}
		NotifyLobbyChanged();
	}

	public LobbyPlayerView[] GetPlayersForTeam(int team)
	{
		if (spectating == null) spectating = new List<Player>();
		if (team_1 == null) team_1 = new List<Player>();
		if (team_2 == null) team_2 = new List<Player>();

		List<Player> players = spectating;
		switch (team) {
		case TEAM_1:
			players = team_1;
			break;
		case TEAM_2:
			players = team_2;
			break;
		}

		LobbyPlayerView[] views = new LobbyPlayerView[players.Count];
		for (int i = 0; i < players.Count; i++) {
			views[i] = new LobbyPlayerView {
				name = players[i].name,
				team = players[i].team,
				controller = players[i].controller
			};
		}
		return views;
	}

	public void MoveLocalPlayer(int controller, int old_team, int new_team)
	{
		ChangeLocalPlayerTeam(controller, old_team, new_team);
		NotifyLobbyChanged();
	}

	public bool IsLocalGame()
	{
		return game_settings != null && game_settings.IsLocalGame();
	}

	public bool CanShowLobbyArrows()
	{
		return show_lobby_arrows;
	}
	
	void DrawPlayers(List<Player> players, int team)
	{
		for(int i = 0; i < players.Count; i++) {
			bool change_team = false;
			int new_team = SPECTATING;
			
			GUILayout.BeginHorizontal();
				if(show_lobby_arrows && (team == TEAM_2 || team == SPECTATING) && (game_settings.IsLocalGame())) {
					if(GUILayout.Button("<", GUILayout.MaxWidth(0.03f*Screen.width))) {
						if(team == SPECTATING)
							new_team = TEAM_1;
						change_team = true;
					}
				}
				GUILayout.FlexibleSpace();
				GUILayout.Label(players[i].name);

				GUILayout.FlexibleSpace();
				if(show_lobby_arrows && (team == TEAM_1 || team == SPECTATING) && (game_settings.IsLocalGame())) {
					if(GUILayout.Button(">", GUILayout.MaxWidth(0.03f*Screen.width))) {
						if(team == SPECTATING)
							new_team = TEAM_2;
						change_team = true;
					}
				}
			GUILayout.EndHorizontal();

			if(change_team) {
				if(game_settings.IsLocalGame())
					ChangeLocalPlayerTeam(players[i].controller, team, new_team);
			}
		}
	}

	void ChangeLocalPlayerTeam(int controller, int old_team, int new_team)
	{
		List<Player> old_player_team = spectating;
		
		switch(old_team){
		case SPECTATING:
			old_player_team = spectating;
			break;
		case TEAM_1:
			old_player_team = team_1;
			break;
		case TEAM_2:
			old_player_team = team_2;
			break;
		}
		
		for(int i = 0; i < old_player_team.Count; i++){
			if(old_player_team[i].controller == controller){
				AddLocalPlayer(controller, old_player_team[i].name, new_team);
				old_player_team.RemoveAt(i);
				return;
			}
		}
	}

	protected abstract void LobbyStates();

	void OnGUI()
	{	
		if(show_lobby && useLegacyGUI) {
			GUI.skin = gui_skin;
			GUILayout.BeginArea(new Rect(Screen.width*0.01f, Screen.height*0.01f, Screen.width - Screen.width*0.02f, Screen.height - Screen.height*0.02f));
			LobbyStates();
			GUILayout.EndArea();
		}
	}

	protected void LobbyScreen()
	{
		GUILayout.BeginVertical();
			GUILayout.BeginHorizontal("box", GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
				GUILayout.BeginVertical(GUILayout.MinWidth(0.3f*Screen.width));
					GUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();
						GUILayout.Label("Red");
						GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
						DrawPlayers(team_1, TEAM_1);
					GUILayout.EndVertical();
				GUILayout.EndVertical();
				GUILayout.FlexibleSpace();
				GUILayout.BeginVertical(GUILayout.MinWidth(0.3f*Screen.width));
					GUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();
						GUILayout.Label("Spectating");
						GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
						DrawPlayers(spectating, SPECTATING);
					GUILayout.EndVertical();
				GUILayout.EndVertical();
				GUILayout.FlexibleSpace();
				GUILayout.BeginVertical(GUILayout.MinWidth(0.3f*Screen.width));
					GUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();
						GUILayout.Label("Blue");
						GUILayout.FlexibleSpace();
					GUILayout.EndHorizontal();
					GUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
						DrawPlayers(team_2, TEAM_2);
					GUILayout.EndVertical();
				GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				LobbyMenu();
				GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		GUILayout.EndVertical();
	}

	protected abstract void LobbyMenu();

	protected void BackToMainMenu()
	{
		Application.LoadLevel(game_settings.main_menu_scene);
	}
}
