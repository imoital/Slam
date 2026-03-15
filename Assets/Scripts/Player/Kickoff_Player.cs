using UnityEngine;
using System.Collections;

public class Kickoff_Player : Player_Behaviour {
	
	//public int team = 1;
	public int player_num = 1;
	
	public Material normal_team_2_material;
	public Material shoot_team_2_material;
	
	protected GameObject center_circle_left;
	protected GameObject center_circle_right;
	protected GameObject[] center_planes;

	public Vector3 initial_position;
	protected GameObject controller_object;

	private Transform base_collider;
	private Transform shoot_collider;
	private Transform colliderAIPossessionCenter;
	private Transform colliderAIPossessionLeft;
	private Transform colliderAIPossessionRight;

	public void DisableGotoCenter(int scored_team)
	{
		player_base = ResolvePlayerBase(transform.Find("Mesh"));
		base_collider = transform.Find("Collider");
		shoot_collider = transform.Find("ColliderShoot");
		colliderAIPossessionCenter = transform.Find("ColliderAIPossession/ColliderAIPossessionCenter");
		colliderAIPossessionLeft = transform.Find("ColliderAIPossession/ColliderAIPossessionLeft");
		colliderAIPossessionRight = transform.Find("ColliderAIPossession/ColliderAIPossessionRight");
		
		if (scored_team == 0){
			if(team == 1){

				IgnoreCollision(center_circle_right.GetComponent<Collider>(), false);
//				Physics.IgnoreCollision(center_circle_right.collider, base_collider.collider, false);
//				Physics.IgnoreCollision(center_circle_right.collider, shoot_collider.collider, false);
//				Physics.IgnoreCollision(center_circle_right.collider, colliderAIPossession.collider, false);
				IgnoreCollision(center_circle_left.GetComponent<Collider>(), true);

//				Physics.IgnoreCollision(center_circle_left.collider, base_collider.collider);
//				Physics.IgnoreCollision(center_circle_left.collider, shoot_collider.collider);
//				Physics.IgnoreCollision(center_circle_left.collider, colliderAIPossession.collider);
			} else {

				IgnoreCollision(center_circle_left.GetComponent<Collider>(), false);
//				Physics.IgnoreCollision(center_circle_left.collider, base_collider.collider, false);
//				Physics.IgnoreCollision(center_circle_left.collider, shoot_collider.collider, false);
//				Physics.IgnoreCollision(center_circle_left.collider, colliderAIPossession.collider, false);
				IgnoreCollision(center_circle_right.GetComponent<Collider>(), true);
//				Physics.IgnoreCollision(center_circle_right.collider, base_collider.collider);
//				Physics.IgnoreCollision(center_circle_right.collider, shoot_collider.collider);
//				Physics.IgnoreCollision(center_circle_right.collider, colliderAIPossession.collider);
			}
		} else if (team == 1) {
			if (scored_team == 1) {
				IgnoreCollision(center_circle_left.GetComponent<Collider>(), false);
//				Physics.IgnoreCollision(center_circle_left.collider, base_collider.collider, false);
//				Physics.IgnoreCollision(center_circle_left.collider, shoot_collider.collider, false);
//				Physics.IgnoreCollision(center_circle_left.collider, colliderAIPossession.collider, false);

				IgnoreCollision(center_circle_right.GetComponent<Collider>(), true);
//				Physics.IgnoreCollision(center_circle_right.collider, base_collider.collider);
//				Physics.IgnoreCollision(center_circle_right.collider, shoot_collider.collider);
//				Physics.IgnoreCollision(center_circle_right.collider, colliderAIPossession.collider);
			} else {
				IgnoreCollision(center_circle_right.GetComponent<Collider>(), false);
//				Physics.IgnoreCollision(center_circle_right.collider, base_collider.collider, false);
//				Physics.IgnoreCollision(center_circle_right.collider, shoot_collider.collider, false);
//				Physics.IgnoreCollision(center_circle_right.collider, colliderAIPossession.collider, false);
				IgnoreCollision(center_circle_left.GetComponent<Collider>(), true);
//				Physics.IgnoreCollision(center_circle_left.collider, base_collider.collider);
//				Physics.IgnoreCollision(center_circle_left.collider, shoot_collider.collider);
//				Physics.IgnoreCollision(center_circle_left.collider, colliderAIPossession.collider);
			}
		} else {
			if (scored_team == 1) {
				IgnoreCollision(center_circle_left.GetComponent<Collider>(), false);
//				Physics.IgnoreCollision(center_circle_left.collider, base_collider.collider, false);
//				Physics.IgnoreCollision(center_circle_left.collider, shoot_collider.collider, false);
//				Physics.IgnoreCollision(center_circle_left.collider, colliderAIPossession.collider, false);
				IgnoreCollision(center_circle_right.GetComponent<Collider>(), true);
//				Physics.IgnoreCollision(center_circle_right.collider, base_collider.collider);
//				Physics.IgnoreCollision(center_circle_right.collider, shoot_collider.collider);
//				Physics.IgnoreCollision(center_circle_right.collider, colliderAIPossession.collider);
			} else {
				IgnoreCollision(center_circle_right.GetComponent<Collider>(), false);
//				Physics.IgnoreCollision(center_circle_right.collider, base_collider.collider, false);
//				Physics.IgnoreCollision(center_circle_right.collider, shoot_collider.collider, false);
//				Physics.IgnoreCollision(center_circle_right.collider, colliderAIPossession.collider, false);
				IgnoreCollision(center_circle_left.GetComponent<Collider>(), true);
//				Physics.IgnoreCollision(center_circle_left.collider, base_collider.collider);
//				Physics.IgnoreCollision(center_circle_left.collider, shoot_collider.collider);
//				Physics.IgnoreCollision(center_circle_left.collider, colliderAIPossession.collider);
			}
		}
		for(int i = 0; i < center_planes.Length; i++) {
			Physics.IgnoreCollision(center_planes[i].GetComponent<Collider>(), shoot_collider.GetComponent<Collider>(), false);
			Physics.IgnoreCollision(center_planes[i].GetComponent<Collider>(), base_collider.GetComponent<Collider>(), false);
			Physics.IgnoreCollision(center_planes[i].GetComponent<Collider>(), colliderAIPossessionCenter.GetComponent<Collider>(), false);
		}
	}

	private void IgnoreCollision(Collider circle,  bool value)
	{
		Physics.IgnoreCollision(circle, base_collider.GetComponent<Collider>(), value);
		Physics.IgnoreCollision(circle, shoot_collider.GetComponent<Collider>(), value);
		Physics.IgnoreCollision(circle, colliderAIPossessionCenter.GetComponent<Collider>(), value);
		Physics.IgnoreCollision(circle, colliderAIPossessionLeft.GetComponent<Collider>(), value);
		Physics.IgnoreCollision(circle, colliderAIPossessionRight.GetComponent<Collider>(), value);
	}
	
	/* Only one team should kickoff, the other cannot go through the midfield circle or opposing side */
	public void DisableGotoCenter(NotificationCenter.Notification notification)
	{
		int scored_team = (int)notification.data["scored_team"];	
		DisableGotoCenter(scored_team);
	}
	
	public void ReleasePlayers()
	{
//		player_base = transform.Find("Mesh").Find("Base");
//		Transform base_collider = transform.Find("Collider");
//		Transform shoot_collider = transform.Find("ColliderShoot");
//		Transform colliderAIPossession = transform.Find("ColliderAIPossession");

		for (int i = 0; i < center_planes.Length; i++) {
			Physics.IgnoreCollision(center_planes[i].GetComponent<Collider>(), base_collider.GetComponent<Collider>());
			Physics.IgnoreCollision(center_planes[i].GetComponent<Collider>(), shoot_collider.GetComponent<Collider>());
			Physics.IgnoreCollision(center_planes[i].GetComponent<Collider>(), colliderAIPossessionCenter.GetComponent<Collider>());
			Physics.IgnoreCollision(center_planes[i].GetComponent<Collider>(), colliderAIPossessionLeft.GetComponent<Collider>());
			Physics.IgnoreCollision(center_planes[i].GetComponent<Collider>(), colliderAIPossessionRight.GetComponent<Collider>());
		}
		Physics.IgnoreCollision(center_circle_left.GetComponent<Collider>(), base_collider.GetComponent<Collider>());
		Physics.IgnoreCollision(center_circle_left.GetComponent<Collider>(), shoot_collider.GetComponent<Collider>());
		Physics.IgnoreCollision(center_circle_left.GetComponent<Collider>(), colliderAIPossessionCenter.GetComponent<Collider>());
		Physics.IgnoreCollision(center_circle_left.GetComponent<Collider>(), colliderAIPossessionLeft.GetComponent<Collider>());
		Physics.IgnoreCollision(center_circle_left.GetComponent<Collider>(), colliderAIPossessionRight.GetComponent<Collider>());

		Physics.IgnoreCollision(center_circle_right.GetComponent<Collider>(), base_collider.GetComponent<Collider>());
		Physics.IgnoreCollision(center_circle_right.GetComponent<Collider>(), shoot_collider.GetComponent<Collider>());
		Physics.IgnoreCollision(center_circle_right.GetComponent<Collider>(), colliderAIPossessionCenter.GetComponent<Collider>());
		Physics.IgnoreCollision(center_circle_right.GetComponent<Collider>(), colliderAIPossessionLeft.GetComponent<Collider>());
		Physics.IgnoreCollision(center_circle_right.GetComponent<Collider>(), colliderAIPossessionRight.GetComponent<Collider>());
	}
	
	protected override void Awake()
	{
		NotificationCenter.DefaultCenter.AddObserver(this, "InitializePosition");
		NotificationCenter.DefaultCenter.AddObserver(this, "ReleasePlayers");
		NotificationCenter.DefaultCenter.AddObserver(this, "DisableGotoCenter");
		NotificationCenter.DefaultCenter.AddObserver(this, "ChangeReaction");
		NotificationCenter.DefaultCenter.AddObserver(this, "StopCelebration");
		
		center_planes = GameObject.FindGameObjectsWithTag("center-plane");
		center_circle_left = GameObject.FindGameObjectWithTag("center-circle-left");
		center_circle_right = GameObject.FindGameObjectWithTag("center-circle-right");

		base.Awake();
	}
	
	new public void Start () {
		base.Start();
				
		if(team == 1) {
			normal_material = normal_team_1_material;
			shoot_material = shoot_team_1_material;
		} else {
			normal_material = normal_team_2_material;
			shoot_material = shoot_team_2_material;
		}
		Renderer baseRenderer = player_base != null ? player_base.GetComponent<Renderer>() : null;
		if (baseRenderer != null) {
			baseRenderer.material = normal_material;
		}
	}	
	
	public void InitializePosition()
	{
		transform.position = initial_position;
	}
	
	public int GetTeam()
	{
		return team;
	}

	protected void InstantiateHero(int hero_index)
	{
		switch(hero_index) {
		case 0:
			hero = new Sam(this);
			break;
		case 1:
			hero = new Tesla(this);
			break;
		case 2:
			hero = new AI(this);
			break;
		}
		hero.InstantiateMesh(this.transform);
		
		hero.Start();
	}
	
}
