﻿using UnityEngine;
using System.Collections;

public class PitchArea : MonoBehaviour {

	private AIManager AIManager;
	private int index;
	private Ball_Behaviour ball;


	// Use this for initialization
	void Start () {
		GameObject AIManager_object = GameObject.Find("AIManager");
		AIManager = AIManager_object.GetComponent<AIManager>();

		index = int.Parse(name);

		AIManager.InsertPitchAreaCoordinates(index, this.transform.position);
		//Debug.Log(index + " - " +transform.localPosition);
		
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnTriggerEnter(Collider collider)
	{
		if (collider.gameObject.CompareTag("ColliderAIPossessionCenter")) {
			Player_Behaviour player = collider.transform.parent.parent.gameObject.GetComponent<Player_Behaviour>();
			Hero hero = player.GetHero();
		//	player.SetCurrentArea(index); //every player knows where it is in the pitch;
		//	Debug.Log("area: " + index + "player_area: " + player.getCurrentArea());
			AIManager.InsertHeroInList(hero, index);

		} else if(collider.gameObject.CompareTag("ball")) {
			if (!ball)
				ball = GameObject.Find("Ball").GetComponent<Ball_Behaviour>();
			ball.SetCurrentArea(index);
		}
		else if (collider.gameObject.CompareTag("ball"))

		         AIManager.SetDiskArea(index);
	}

	void OnTriggerExit(Collider collider)
	{
		if (collider.gameObject.CompareTag("player_collider")) {
			Player_Behaviour player = collider.transform.parent.gameObject.GetComponent<Player_Behaviour>();
			Hero hero = player.GetHero();
			AIManager.RemoveHeroFromList(hero, index);
		}
	}
}
