using UnityEngine;
using System.Collections;

public class Tesla : Hero {

	private Transform magnet;
	private GameObject ball;
	private bool is_using_power;
	private Vector3 ball_pos;
	private Vector3 original_position;
	private Vector3 power_displacement;
	private bool is_velocity_zeroed = false;
	private float POWER_TIMER = 2f;
	private float POWER_COOLDOWN = 16f;

	private float last_power_key = 0f;

	private PlayerController.Commands commands;

	public Tesla(Player_Behaviour player)
	{
		hero_prefab = Resources.Load<GameObject>("Heroes/Tesla");
		this.player = player;
		is_using_power = false;
		power_displacement = Vector3.zero;
		player.setDashCooldown(POWER_COOLDOWN);
	}
	
	public override void UsePower(PlayerController.Commands commands)
	{
		this.commands = commands;

		if(ball == null)
			ball = GameObject.FindWithTag("ball");

		if(IsPowerKeyDown(commands.dash)) {
			if (player.IsCooldownOver() && !is_using_power) {
				power_cooldown = POWER_COOLDOWN + Time.time;
				is_using_power = true;
				player.setPowerActivatedTimer(POWER_TIMER);
				ball_pos = ball.transform.position;
					EmmitPowerFX("power_up");
			} else if (is_using_power) {
				StopPower();
			} 
		}

		if(player.IsPowerTimerOver()) {
			StopPower();
			
		}

		if (is_using_power && player.IsCollidingWithBall()) {
			if (!is_velocity_zeroed) {
				ball.transform.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
				is_velocity_zeroed = true;
			}
			power_displacement = player.transform.position - original_position;
			//			Debug.Log(power_displacement + "-" + original_position + "-" + ball.transform.position);
			ball.transform.position += power_displacement;
		} else {
			is_velocity_zeroed = false;
		}
		
		original_position = player.transform.position;
	}

	public override void EmmitPowerFX(string type = "none")
	{
		if(type == "power_up")
			magnet.GetComponent<ParticleSystem>().Play();
		if(type == "power_down")
			magnet.GetComponent<ParticleSystem>().Stop();
	}

	private void StopPower()
	{
		is_using_power = false;
		if(player.IsCollidingWithBall())
			ball.transform.GetComponent<Rigidbody>().linearVelocity = player.GetComponent<Rigidbody>().linearVelocity;
			EmmitPowerFX("power_down");
		player.setPowerActivatedTimer(0f);
		player.resetPowerBar();
	}

	public override void Start()
	{
		ai_manager.InsertHero(this);
		magnet = player.transform.Find("Mesh").Find("Base").Find("Magnet");
		magnet.GetComponent<ParticleSystem>().Stop();
		team = player.team;
	}

	bool IsPowerKeyDown(float power_key)
	{
		bool is_down = true;

		if(last_power_key != power_key && power_key == 1)
			is_down = true;
		else
			is_down = false;

		last_power_key = power_key;

		return is_down;
	}

	public override void Update ()
	{
		throw new System.NotImplementedException ();
	}
}
