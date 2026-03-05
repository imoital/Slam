	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;

	public class Crowd : MonoBehaviour {

		public GameObject center;
		public int team;
		public GameObject[] heroes;
		public Material team_1_material;
		public Material team_2_material;
		

		private List<GameObject> all_fans;
		private List<GameObject> teslas;
		private bool activate_crowd = true;

		void Start()
		{
			all_fans = new List<GameObject>();
			teslas = new List<GameObject>();

			if (heroes == null || heroes.Length < 2 || heroes[0] == null || heroes[1] == null) {
				Debug.LogError("Crowd heroes array is missing entries.", this);
				return;
			}

			Material team_material;

			if(team == 1)
				team_material = team_1_material;
			else
				team_material = team_2_material;
				
			foreach(Transform fan in transform) {

				int hero_selected = Random.Range(0, 100);
				GameObject hero_to_instanciate;

				if(hero_selected <= 60)
					hero_to_instanciate = heroes[0];
				else 
					hero_to_instanciate = heroes[1];

				GameObject hero = (GameObject)Instantiate(hero_to_instanciate);
				hero.transform.parent = fan;
				hero.transform.localPosition = Vector3.zero;
				hero.transform.localScale = Vector3.one;
				hero.transform.localRotation = Quaternion.Euler(0, 180, 0);

				Animation heroAnimation = hero.GetComponent<Animation>();
				if (heroAnimation != null && heroAnimation["Idle"] != null) {
					heroAnimation.Play("Idle");
					heroAnimation["Idle"].time = Random.Range(0.0f, heroAnimation["Idle"].length);
				}

			Transform hero_object = hero.transform;
			ApplyTeamMaterial(hero_object.gameObject, team_material, hero_to_instanciate.name);
				all_fans.Add(fan.gameObject);

				if(hero_to_instanciate.name.ToLower().Contains("tesla")) {
					DeactivateTeslaEffects(hero_object.gameObject);
					teslas.Add(hero_object.gameObject);
				}

				Fan_Behaviour fan_behaviour = fan.GetComponent<Fan_Behaviour>();
				if (fan_behaviour != null) {
					fan_behaviour.HeroStarted(center);
				}
			}
		}

		void DeactivateTeslaEffects(GameObject tesla)
		{
			Transform bulb = FindDeepChild(tesla.transform, "bulb_end");

			if (bulb != null && bulb.childCount > 0) {
				Destroy(bulb.GetChild(0).gameObject);
			}

			// Deactivate Magnet
			Transform magnet = tesla.transform.Find("Base/Magnet");
			if (magnet == null) {
				magnet = FindDeepChild(tesla.transform, "Magnet");
			}

			if (magnet != null) {
				Destroy(magnet.gameObject);
			}
		}

		Transform FindDeepChild(Transform parent, string childName)
		{
			foreach (Transform child in parent) {
				if (child.name == childName) return child;
				Transform result = FindDeepChild(child, childName);
				if (result != null) return result;
			}
			return null;
		}

	List<Transform> FindDeepChildren(Transform parent, string childName, List<Transform> results = null)
	{
		if (results == null) {
			results = new List<Transform>();
		}

		foreach (Transform child in parent) {
			if (child.name == childName) {
				results.Add(child);
			}
			FindDeepChildren(child, childName, results);
		}

		return results;
	}

	Transform FindTeslaBodyBase(Transform heroRoot)
	{
		List<Transform> baseCandidates = FindDeepChildren(heroRoot, "Base");
		foreach (Transform candidate in baseCandidates) {
			string lowerName = candidate.name.ToLower();
			if (lowerName.Contains("eyes") || lowerName.Contains("goggles")) {
				continue;
			}

			// Prefer a Base that has its own renderer (usually the body mesh).
			if (candidate.GetComponent<Renderer>() != null) {
				return candidate;
			}
		}

		// Fallback: first Base found.
		return baseCandidates.Count > 0 ? baseCandidates[0] : null;
	}

	void ApplyTeamMaterial(GameObject heroObject, Material teamMaterial, string heroName)
	{
		if (teamMaterial == null || heroObject == null) return;

		bool isTesla = heroName.ToLower().Contains("tesla");
		if (isTesla) {
			Transform teslaBaseTransform = FindTeslaBodyBase(heroObject.transform);
			if (teslaBaseTransform == null) {
				Debug.LogWarning("Tesla Base transform not found. Tesla materials were left unchanged to avoid tinting eyes/glasses.", heroObject);
				return;
			}

			Renderer[] teslaBaseRenderers = teslaBaseTransform.GetComponentsInChildren<Renderer>(true);
			if (teslaBaseRenderers == null || teslaBaseRenderers.Length == 0) {
				Debug.LogWarning("Tesla Base has no renderer. Tesla materials were left unchanged to avoid tinting eyes/glasses.", teslaBaseTransform);
				return;
			}

			foreach (Renderer renderer in teslaBaseRenderers) {
				renderer.material = teamMaterial;
			}
			return;
		}

		// Prefer a deep "Base" object for all heroes (TeslaV2 has Base nested under TeslaV2).
		Transform baseTransform = FindDeepChild(heroObject.transform, "Base");
		if (baseTransform != null) {
			Renderer baseRenderer = baseTransform.GetComponent<Renderer>();
			if (baseRenderer != null) {
				baseRenderer.material = teamMaterial;
				return;
			}
		}

		Renderer[] allRenderers = heroObject.GetComponentsInChildren<Renderer>(true);
		bool applied = false;
		foreach (Renderer renderer in allRenderers) {
			string nameLower = renderer.gameObject.name.ToLower();

			if (nameLower.Contains("bulb") || nameLower.Contains("magnet")) {
				continue;
			}

			renderer.material = teamMaterial;
			applied = true;
		}

		if (!applied) {
			Debug.LogWarning("Crowd could not find a renderer to apply team material for hero " + heroName + ".", heroObject);
		}
	}
		
		void Update() 
		{
			// randomly chooce a fan to cheer for his team
			if(all_fans == null || all_fans.Count == 0) {
				return;
			}

			if(Random.Range(0,20) == 0) {
				GameObject fan = all_fans[Random.Range(0, all_fans.Count)];
				
				Fan_Behaviour fan_behaviour = fan.GetComponent<Fan_Behaviour>();
				if (fan_behaviour != null) {
					StartCoroutine(fan_behaviour.Celebrate());
				}
			}

			if(Input.GetKeyDown(KeyCode.M)) {
				activate_crowd = !activate_crowd;
				foreach(Transform child in transform) {
					child.gameObject.SetActive(activate_crowd);
				}
			}
		}
	}
