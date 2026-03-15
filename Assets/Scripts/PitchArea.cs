using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PitchArea : MonoBehaviour {

	private int index;
	private Ball_Behaviour ball;

	// Static registry for AI (replaces AIManager pitch/area data)
	public static Dictionary<int, Vector3> AreaPositions = new Dictionary<int, Vector3>();
	public static Dictionary<int, List<Hero>> HeroesInArea = new Dictionary<int, List<Hero>>();

	void Start () {
		index = int.Parse(name);
		if (!AreaPositions.ContainsKey(index))
			AreaPositions[index] = transform.position;
	}

	public static Vector3 GetPosition(int areaIndex) {
		return AreaPositions.ContainsKey(areaIndex) ? AreaPositions[areaIndex] : Vector3.zero;
	}

	public static List<Hero> GetHeroesInArea(int areaIndex) {
		if (!HeroesInArea.ContainsKey(areaIndex)) return new List<Hero>();
		return new List<Hero>(HeroesInArea[areaIndex]);
	}

	void OnTriggerEnter(Collider collider)
	{
		if (collider.gameObject.CompareTag("ColliderAIPossessionCenter")) {
			Player_Behaviour player = collider.transform.parent.parent.gameObject.GetComponent<Player_Behaviour>();
			if (player != null) {
				Hero hero = player.GetHero();
				if (hero != null) {
					if (!HeroesInArea.ContainsKey(index)) HeroesInArea[index] = new List<Hero>();
					if (!HeroesInArea[index].Contains(hero)) HeroesInArea[index].Add(hero);
				}
			}
		} else if (collider.gameObject.CompareTag("ball")) {
			if (!ball)
				ball = GameObject.Find("Ball").GetComponent<Ball_Behaviour>();
			ball.SetCurrentArea(index);
		}
	}

	void OnTriggerExit(Collider collider)
	{
		if (collider.gameObject.CompareTag("player_collider")) {
			Player_Behaviour player = collider.transform.parent.gameObject.GetComponent<Player_Behaviour>();
			if (player != null) {
				Hero hero = player.GetHero();
				if (hero != null && HeroesInArea.ContainsKey(index)) {
					HeroesInArea[index].Remove(hero);
				}
			}
		}
	}
}
