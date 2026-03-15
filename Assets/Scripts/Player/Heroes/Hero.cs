using UnityEngine;
using System.Collections;

public abstract class Hero {

	protected GameObject hero_prefab;
	protected Player_Behaviour player;
	public abstract void UsePower(PlayerController.Commands commands);
	protected float power_cooldown;
	protected int team;
	protected bool is_ai = false;

	protected int current_area = -1; //every player knows where it is in the pitch;

	public abstract void Start();

	public abstract void Update();

	public abstract void EmmitPowerFX(string type = "none");

	public void InstantiateMesh(Transform player)
	{
		GameObject hero = (GameObject)MonoBehaviour.Instantiate(hero_prefab);
		hero.transform.parent = player;

		hero.transform.localPosition = Vector3.zero;
		hero.transform.localScale = Vector3.one;

		hero.transform.name = "Mesh";
		
	}



	public int GetTeam()
	{
		return team;
	}

	public bool IsCooldownOver()
	{
		return player.IsCooldownOver();
	}

	public int GetCurrentArea()
	{
		return current_area;
	}

	public void SetCurrentArea(int current_area)
	{
//		Debug.Log(current_area);
		this.current_area = current_area;
	}

	public Vector3 GetPosition()
	{
		return player.transform.position;
	}

	public bool IsAI()
	{
		return is_ai;
	}

}
