﻿using UnityEngine;
using System.Collections;

public class LED_Screen : MonoBehaviour {
	
	private float planeScale = 10.0f;
	private float DEFAULT_PIXEL_SIZE = 20.4f;
	private float INITIAL_PIXEL_SIZE = 100f;
	private int BLINK_COUNTER_MAX = 30;
	//private int blink_counter = 0;
	private bool is_animating_tie_message = false;
	private bool is_animating_scored_message = false;
	
	public Texture golden_goal_texture;
	public Texture red_team_scores;
	public Texture blue_team_scores;

	public void DrawTieMessage()
	{
		GetComponent<Renderer>().material.SetFloat("_PixelSize", INITIAL_PIXEL_SIZE);
		GetComponent<Renderer>().material.SetTexture("_MainTex", golden_goal_texture);
		GetComponent<Renderer>().material.SetColor("_DrawColor", Color.yellow);
		is_animating_tie_message = true;
		
	}

	public void DrawGoalScored(int team)
	{
		if (team == 1){
			GetComponent<Renderer>().material.SetTexture("_MainTex", red_team_scores);
			GetComponent<Renderer>().material.SetColor("_DrawColor", Color.red);
		} else if (team == 2) {
			GetComponent<Renderer>().material.SetTexture("_MainTex", blue_team_scores);
			GetComponent<Renderer>().material.SetColor("_DrawColor", Color.blue);
		}

		is_animating_scored_message = true;
		StartCoroutine("AnimateGoalScored");
		//blink_counter = 0;
	}

	private IEnumerator AnimateGoalScored()
	{	
		int blink_counter = 0;
		while(blink_counter <= BLINK_COUNTER_MAX) {

			if((blink_counter % 2) == 0){
				GetComponent<Renderer>().material.SetFloat("_Invert", 1f);
			} else {
				GetComponent<Renderer>().material.SetFloat("_Invert", 0f);
			}

			blink_counter++;
			yield return new WaitForSeconds(0.1f);
		}
		GetComponent<Renderer>().material.SetFloat("_Invert", 0f);
		GetComponent<Renderer>().material.SetTexture("_MainTex", null);
	}

	private void AnimateTieMessage()
	{
		if (GetComponent<Renderer>().material.GetFloat("_PixelSize") > DEFAULT_PIXEL_SIZE)
			GetComponent<Renderer>().material.SetFloat("_PixelSize", GetComponent<Renderer>().material.GetFloat("_PixelSize")-1f);
		else {
			is_animating_tie_message = false;
		}
	}

	private void Update()
	{
		if (is_animating_tie_message)
			AnimateTieMessage();
		
//		if (is_animating_scored_message) {
//			//if (blink_counter >= BLINK_COUNTER_MAX)
//				is_animating_scored_message=false;
//			//StartCoroutine("AnimateGoalScoredInverted");
//			StartCoroutine("AnimateGoalScored");
//		}
	}
}
