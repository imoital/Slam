#pragma strict

var acceleration = 0.5;
var player_max_speed = 4;

// Directions Vectors
var left_direction = Vector3(0.0, 0.0, 1.0);
var right_direction = Vector3(0.0, 0.0, -1.0);
var up_direction = Vector3(1.0, 0.0, 0.0);
var down_direction = Vector3(-1.0, 0.0, 0.0);
var velocity = Vector3.zero;
var normalized_velocity = Vector3.zero;
// Movement keys pressed?
var up_keyUp = true; 
var down_keyUp = true; 
var right_keyUp = true;
var left_keyUp = true;

function horizontal_velocity() 
{
	return rigidbody.velocity.z;
}

function vertical_velocity()
{
	return rigidbody.velocity.x;
}

function increase_speed() 
{
	
	var vertical_speed : float = Input.GetAxis("Vertical")*acceleration;
	var horizontal_speed : float = Input.GetAxis("Horizontal")*acceleration;
	
	rigidbody.velocity.z -= horizontal_speed;
	rigidbody.velocity.x += vertical_speed;
	
	if(rigidbody.velocity.x < -player_max_speed)
		rigidbody.velocity.x = -player_max_speed;
	else if (rigidbody.velocity.x > player_max_speed)
		rigidbody.velocity.x = player_max_speed;
		
	if(rigidbody.velocity.z < -player_max_speed)
		rigidbody.velocity.z = -player_max_speed;
	else if (rigidbody.velocity.z > player_max_speed)
		rigidbody.velocity.z = player_max_speed;
		
	/*switch (direction) {
	case "right":
		if(horizontal_velocity() > -player_max_speed)
			rigidbody.velocity += right_direction * acceleration;
		break;
	case "left":
		if(horizontal_velocity() < player_max_speed)
			rigidbody.velocity += left_direction * acceleration;
		break;
	case "up":
		if(vertical_velocity() < player_max_speed)
			rigidbody.velocity += up_direction * acceleration;
		break;
	case "down":
		if(vertical_velocity() > -player_max_speed)
			rigidbody.velocity += down_direction * acceleration;
	}*/

}

function normalize_velocity()
{
	var degree = Mathf.Atan2(Mathf.Abs(horizontal_velocity()), 
							 Mathf.Abs(vertical_velocity()));
	normalized_velocity.x = rigidbody.velocity.x * Mathf.Cos(degree);
	normalized_velocity.z = rigidbody.velocity.z * Mathf.Sin(degree); 
}

function Start () {
	var court_walls = GameObject.FindGameObjectWithTag("court_walls");
	Physics.IgnoreCollision(court_walls.collider, this.collider);
}

function Update () 
{
	/*if(Input.GetKeyUp("a"))
		left_keyUp = true;
	if(Input.GetKeyUp("d"))
		right_keyUp = true;
	if(Input.GetKeyUp("w"))
		up_keyUp = true;
	if(Input.GetKeyUp("s"))
		down_keyUp = true;*/
		
	
	
	
	if (Input.GetAxis("Vertical")!=0)
		rigidbody.velocity.x = velocity.x;
		
	if (Input.GetAxis("Horizontal")!=0)
		rigidbody.velocity.z = velocity.z;
		
	increase_speed();
	
	/*if(Input.GetKey("a")) {
		increase_speed("left");
		left_keyUp = false;
	}
	if(Input.GetKey("d")) {
		increase_speed("right");
		right_keyUp = false;
	}
	if(Input.GetKey("w")) {
		increase_speed("up");
		up_keyUp = false;
	}
	if(Input.GetKey("s")) {
		increase_speed("down");
		down_keyUp = false;
	}*/
	
	//if(rigidbody.velocity.magnitude > player_max_speed)
	
	normalize_velocity();
	velocity = rigidbody.velocity;
	if (Input.GetAxis("Vertical")!=0)
		rigidbody.velocity.x = normalized_velocity.x;
		
	if (Input.GetAxis("Horizontal")!=0)
		rigidbody.velocity.z = normalized_velocity.z;
		
	Debug.Log(rigidbody.velocity);

}