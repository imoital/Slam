using UnityEngine;
using System.Collections;

public class Local_Game : Game_Behaviour {

	protected override void MovePlayersToStartPositions()
	{
		ball.transform.position = ball_position;
		ball.transform.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
		if (scored_team != 0) {
			Ball_Behaviour bb = ball.GetComponent<Ball_Behaviour>();
			bb.GameHasRestarted();
		}
		
		Hashtable data = new Hashtable();
//		Debug.Log(scored_team);
		data["scored_team"] = scored_team;
		NotificationCenter.DefaultCenter.PostNotification(this, "DisableGotoCenter", data);
		NotificationCenter.DefaultCenter.PostNotification(this, "InitializePosition");
	}
	
//	private Color setIndicatorColor(int i)
//	{
//		switch(i)
//		{
//			case 0:
//				return Color.white;
//			case 1:
//				return Color.red;
//			case 2:
//				return Color.blue;
//			case 3:
//				return Color.green;
//		}
//		return Color.black;
//	}
	
	void Awake()
	{
		ball = (GameObject)Instantiate(ball_prefab, ball_position, transform.rotation);
		ball.transform.name = "Ball";
		GameObject settings =  GameObject.FindGameObjectWithTag("settings");		
	}
	
	public void StartGame()
	{
		team_scored_message_xpos = DEFAULT_TEAM_SCORED_MESSAGE_XPOS;
		MovePlayersToStartPositions();
	}
}
