using UnityEngine;
using System.Collections;

public class Sam : Hero {
	
	private Transform dash_smoke;
	private Transform dash_bar_fill;
	private float DASH_COOLDOWN = 12f;
	private float DASH_STRENGTH = 2f;
	private float last_dash;

	public Sam(Player_Behaviour player)
	{
		hero_prefab = Resources.Load<GameObject>("Heroes/Sam");
		this.player = player;

		player.setDashCooldown(DASH_COOLDOWN);
		player.setPowerActivatedTimer(0f);
	}

	public override void Start()
	{
		AI.RegisterHero(this);
		dash_smoke = player.transform.Find("Mesh").Find("Dash_Smoke");
		team = player.team;
	}

	public override void UsePower(PlayerController.Commands commands)
	{
		if (commands.dash != 0 && player.IsCooldownOver() && (commands.horizontal_direction != 0 || commands.vertical_direction != 0)) {
//			power_cooldown =  DASH_COOLDOWN + Time.time;
			player.transform.GetComponent<Rigidbody>().linearVelocity *= DASH_STRENGTH;
			player.resetPowerBar();
			
			// if networkView == null means localplay so we can't make an RPC

				EmmitPowerFX();	
		}

		last_dash = commands.dash;

	}

	public override void EmmitPowerFX(string type ="none")
	{
		dash_smoke.GetComponent<ParticleSystem>().Play();
	}

	public override void Update ()
	{
		throw new System.NotImplementedException ();
	}


//	protected void Dash(float dash, float horizontal_direction, float vertical_direction)
//	{		
//		if (dash != 0 && (Time.time > dash_cooldown) && (horizontal_direction != 0 || vertical_direction != 0)) {
//			dash_cooldown =  DASH_COOLDOWN + Time.time;
//			rigidbody.velocity *= DASH_STRENGTH;
//			dash_bar_fill.renderer.material.color = Color.red;
//			
//			// if networkView == null means localplay so we can't make an RPC
//			if (networkView != null)
//				networkView.RPC("EmmitDashSmoke",RPCMode.All);
//			else
//				EmmitDashSmoke();	
//		}
//	}
//
//	protected virtual void VerifyDash()
//	{
//		Dash(commands.dash, commands.horizontal_direction, commands.vertical_direction);
//	}
}
