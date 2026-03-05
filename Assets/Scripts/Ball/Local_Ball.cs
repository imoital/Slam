using UnityEngine;
using System.Collections;

public class Local_Ball : Ball_Behaviour {

	void OnCollisionEnter(Collision collider)
	{
		if (collider.gameObject.tag == "forcefield") {
			CourtCollision(collider.contacts[0].point);
		} else {
			ReleasePlayers();
		}
	}
}
