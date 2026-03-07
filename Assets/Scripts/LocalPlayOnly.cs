using UnityEngine;
using System.Collections;

public class LocalPlayOnly : MonoBehaviour {

	public Texture soccer_pucks_logo;
	public GUISkin gui_skin;
	public GameObject settings_prefab;

	Game_Settings game_settings;

	void Start()
	{
		GameObject settings = (GameObject)Instantiate(settings_prefab);
		game_settings = settings.GetComponent<Game_Settings>();
	}

	void OnGUI()
	{
		GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
			GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.BeginVertical();
					GUILayout.FlexibleSpace();
					GUILayout.BeginVertical();
						GUILayout.BeginHorizontal();
							GUILayout.FlexibleSpace();
							GUILayout.Label(soccer_pucks_logo);
							GUILayout.FlexibleSpace();
						GUILayout.EndHorizontal();
						GUILayout.FlexibleSpace();
						GUILayout.BeginHorizontal();
							GUILayout.FlexibleSpace();
							GUILayout.BeginVertical("box", GUILayout.Height(0.3f*Screen.height), GUILayout.Width(0.3f*Screen.width));
								GUILayout.FlexibleSpace();
								GUILayout.BeginHorizontal();
									GUILayout.FlexibleSpace();
									if(GUILayout.Button("Start", GUILayout.Height(0.1f*Screen.height), GUILayout.Width(0.2f*Screen.width))){
										game_settings.local_game = true;
										game_settings.main_menu_scene = Application.loadedLevelName;
										Application.LoadLevel("Pre_Game_Lobby");
									}
									GUILayout.FlexibleSpace();
								GUILayout.EndHorizontal();
								GUILayout.FlexibleSpace();
								GUILayout.BeginHorizontal();
									GUILayout.FlexibleSpace();
									if(GUILayout.Button("Exit", GUILayout.Height(0.1f*Screen.height), GUILayout.Width(0.2f*Screen.width)))
										Application.Quit();
									GUILayout.FlexibleSpace();
								GUILayout.EndHorizontal();
								GUILayout.FlexibleSpace();
							GUILayout.EndVertical();
							GUILayout.FlexibleSpace();
						GUILayout.EndHorizontal();
					GUILayout.EndVertical();
					GUILayout.FlexibleSpace();
					GUILayout.FlexibleSpace();
					GUILayout.FlexibleSpace();
					GUILayout.FlexibleSpace();
				GUILayout.EndVertical();
				GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		GUILayout.EndArea();
	}
}
