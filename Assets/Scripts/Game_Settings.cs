using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* This class is created when a 'main_game' is initiated and is never destroyed */

public class Game_Settings : MonoBehaviour {
	
	public bool local_game = true;
	public string player_name;
	public bool connected;
	//public HostData connect_to;

	public string main_menu_scene;

	public List<Hero_Selection.Player> players_list;

	public int team_1_count = 0;
	public int team_2_count = 0;

	private int red_team_bots = 0;
	private int blue_team_bots = 0;

	void Awake()
	{
		connected = false;
		DontDestroyOnLoad(this);
		players_list = new List<Hero_Selection.Player>();
	}
	
	public bool IsLocalGame()
	{
		return local_game;
	}
	
	public string PlayerName()
	{
		return player_name;
	}

	public void AddPlayer(Hero_Selection.Player player)
	{
		if (player.team == 1)
			team_1_count++;
		else
			team_2_count++;

		players_list.Add(player);
		
	}

	public int GetRedTeamBots()
	{
		return red_team_bots;
	}

	public int GetBlueTeamBots()
	{
		return blue_team_bots;
	}

	public void IncRedTeamBots()
	{
		red_team_bots++;
	}

	public void IncBlueTeamBots()
	{
		blue_team_bots++;
	}

	public void ResetLobbyState()
	{
		players_list.Clear();
		team_1_count = 0;
		team_2_count = 0;
		red_team_bots = 0;
		blue_team_bots = 0;
	}
}
