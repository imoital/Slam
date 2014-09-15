using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class AIManager : MonoBehaviour {

	//The pitch is divided in 18 areas. This list relates the players to each area of the pitch
	private List<Hero>[] pitch_area_list = new List<Hero>[18];

	//The list of instantiated AI that will have their Update() function ran through the AIManager
	private List<Hero> ai_list = new List<Hero>();

	//which heroes are in each of the three flanks
	private List<Hero>[] top_flank_heroes = new List<Hero>[6];
	private List<Hero>[] mid_flank_heroes = new List<Hero>[6];
	private List<Hero>[] bottom_flank_heroes = new List<Hero>[6];

	//If more than one player is in possession, than the involved players are fighting for possession
	private List<Hero> players_in_possession = new List<Hero>();
	private int red_team_bots;
	private int blue_team_bots;

	//The player who is intending to grab possession
	private Hero red_going_for_ball;
	private Hero blue_going_for_ball;


	//in which of the 18 areas is the disk
	private int disk_area;

	private Game_Settings game_settings;

	private GameStarter game_starter;

	protected GameObject AI_prefab;

	private Vector3[] pitch_area_coordinates = new Vector3[18];

	private GameObject red_team_goal;

	private GameObject blue_team_goal;

	void Start () {

		GameObject game_starter_object = GameObject.Find("GameStarter");
		game_starter = game_starter_object.GetComponent<GameStarter>();
		
		game_starter.SetAIManager(this);
		
		AI_prefab = Resources.Load<GameObject>("Heroes/AI");

		for (int i = 0; i <= 17; i++) {

			pitch_area_list[i] = new List<Hero>();
		
		}

		for (int i = 0; i < 6; i++) {
			top_flank_heroes[i] = new List<Hero>();
			mid_flank_heroes[i] = new List<Hero>();
			bottom_flank_heroes[i] = new List<Hero>();
		}



		red_team_goal = GameObject.Find("Score_Team1");
		blue_team_goal = GameObject.Find("Score_Team2");
	}

	public Vector3 GetRedTeamGoalPosition()
	{
		return red_team_goal.transform.position;
	}

	public float GoalWidth()
	{
		return red_team_goal.transform.lossyScale.x;
	}

	public Vector3 GetBlueTeamGoalPosition()
	{
		return blue_team_goal.transform.position;
	}

	public void SetGoingForBall(Hero hero)
	{
		if (hero.GetTeam() == GlobalConstants.RED)
			red_going_for_ball = hero;
		else
			blue_going_for_ball = hero;
	}

	public void ResetGoingForBall(Hero hero)
	{
		if (hero.GetTeam() == GlobalConstants.RED)
			red_going_for_ball = null;
		else
			blue_going_for_ball = null;
	}

	public int GetGoingForBall(int team)
	{
		if (team == GlobalConstants.RED)
			return GlobalConstants.RED;
		else if (team == GlobalConstants.BLUE)
			return GlobalConstants.BLUE;
		else
			return 0;

	}


	public void InsertPlayerInPossession(Hero hero)
	{
		players_in_possession.Add(hero);
	}

	public void RemovePlayerInPossession(Hero hero)
	{
		players_in_possession.Remove(hero);
	}

	public List<Hero> GetPlayersInPossession()
	{
		return players_in_possession;
	}

	public void InsertAI(Hero hero)
	{
		ai_list.Add(hero);
	}

	void Update () 
	{
		foreach (AI ai in ai_list)
			ai.Update();
		IsTeammateAloneInFlanks();
	}

	public void InsertPitchAreaCoordinates(int index, Vector3 pos)
	{
		pitch_area_coordinates[index] = pos;
	}

	public void InstantiateBot(Vector3 start_position, int team)
	{


	}

	public Vector3 GetPitchAreaCoords(int index)
	{
		return pitch_area_coordinates[index];
	}

	public void PrintPitchAreaCoords()
	{
		for (int i = 0; i < 18; i++)
			Debug.Log(i + " - " + pitch_area_coordinates[i]);
	}

	public void InsertHeroInList(Hero hero, int index)
	{
		pitch_area_list[index].Add(hero);
		SetHeroeFlank(hero, index);
		hero.SetCurrentArea(index);
		
	}
	
	public void RemoveHeroFromList(Hero hero, int index)
	{
		pitch_area_list[index].Remove(hero);
		//Debug.Log("removed " + index);
	}

	public List<Hero> GetPlayerListFromArea(int index)
	{
		return pitch_area_list[index];
	}

	public void SetDiskArea(int index)
	{
		disk_area = index;
		//Debug.Log(index);
	}

	private void SetHeroeFlank(Hero hero, int index)
	{
		int flank = AreaToFlank(index);
		int previous_area = hero.GetCurrentArea();
		int previous_flank = AreaToFlank(previous_area);
		
		if (previous_flank == GlobalConstants.BOTTOM_FLANK)
			bottom_flank_heroes[previous_area/3].Remove(hero);

		if (previous_flank == GlobalConstants.MID_FLANK)
			mid_flank_heroes[previous_area/3].Remove(hero);

		if (previous_flank == GlobalConstants.TOP_FLANK)
			top_flank_heroes[previous_area/3].Remove(hero);

		if (flank == GlobalConstants.BOTTOM_FLANK)
			bottom_flank_heroes[index/3].Add(hero);

		else if (flank == GlobalConstants.MID_FLANK)
			mid_flank_heroes[index/3].Add(hero);

		else if (flank == GlobalConstants.TOP_FLANK)
			top_flank_heroes[index/3].Add(hero);
		
	}

	public List<Hero>[] GetTopFlankHeroes()
	{
		return top_flank_heroes;
	}

	public List<Hero>[] GetMidFlankHeroes()
	{
		return mid_flank_heroes;
	}

	public List<Hero>[] GetBottomFlankHeroes()
	{
		return bottom_flank_heroes;
	}

	// The vector it returns will be (RED,0,-1) if the Top flank has at least a red teammate and
	// no opponent in the flank, the Mid has both Red and Blue, and Bottom has no one.
	public Vector3 IsTeammateAloneInFlanks()
	{
		Vector3 flanks = new Vector3(-1,-1,-1);

		flanks.x = IsTeammateAloneInTopFlank();
		flanks.y = IsTeammateAloneInMidFlank();
		flanks.z = IsTeammateAloneInBottomFlank();

		//Debug.Log(flanks);

		return flanks;

	}

	private int IsTeammateAloneInTopFlank()
	{
		int flank = -1;

		for (int i = 0; i < 6; i++)
			foreach (Hero hero in top_flank_heroes[i])
				if (hero.GetTeam() == GlobalConstants.RED)
					if (flank == -1 || flank == GlobalConstants.RED)
						flank = GlobalConstants.RED;
					else
						flank = 0;
				else if (hero.GetTeam() == GlobalConstants.BLUE)
					if (flank == -1 || flank == GlobalConstants.BLUE)
						flank = GlobalConstants.BLUE;
					else
						flank = 0;

		return flank;

	}

	private int IsTeammateAloneInMidFlank()
	{
		int flank = -1;
		
		for (int i = 0; i < 6; i++)
			foreach (Hero hero in mid_flank_heroes[i]) {
				if (hero.GetTeam() == GlobalConstants.RED)
					if (flank == -1 || flank == GlobalConstants.RED)
						flank = GlobalConstants.RED;
					else
						flank = 0;
				else if (hero.GetTeam() == GlobalConstants.BLUE)
					if (flank == -1 || flank == GlobalConstants.BLUE)
						flank = GlobalConstants.BLUE;
					else
						flank = 0;
		

		}
				return flank;
		
	}

	private int IsTeammateAloneInBottomFlank()
	{
		int flank = -1;
		
		for (int i = 0; i < 6; i++)
			foreach (Hero hero in bottom_flank_heroes[i])
				if (hero.GetTeam() == GlobalConstants.RED)
					if (flank == -1 || flank == GlobalConstants.RED)
						flank = GlobalConstants.RED;
					else
						flank = 0;
				else if (hero.GetTeam() == GlobalConstants.BLUE)
					if (flank == -1 || flank == GlobalConstants.BLUE)
						flank = GlobalConstants.BLUE;
					else
						flank = 0;
		
		return flank;
		
	}


	// given an area, it returns the flank
	private int AreaToFlank(int area) 
	{
		int flank;
		
		for (int i = 0; i < 6; i++)
			if (area == 3*i)
				return GlobalConstants.BOTTOM_FLANK;
		else if (area == 3*i+1)
			return GlobalConstants.MID_FLANK;
		else if (area == 3*i+2)
			return GlobalConstants.TOP_FLANK;
		return -1;
		
	}

}
