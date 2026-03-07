using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreLobbyCanvasUI : MonoBehaviour
{
	public PreLobby preLobby;
	public Canvas canvas;
	public bool disableLegacyGui = true;

	[Header("Panels")]
	public GameObject teamSelectionRoot;
	public GameObject heroSelectionRoot;

	[Header("Player Lists")]
	public Transform redPlayersContainer;
	public Transform spectatingPlayersContainer;
	public Transform bluePlayersContainer;
	public LobbyPlayerRowUI playerRowPrefab;

	[Header("Top Buttons")]
	public Button backButton;
	public Button restartButton;
	public Button startButton;

	[Header("Hero Selection")]
	public RectTransform[] addBotButtons;

	private readonly List<GameObject> spawnedRows = new List<GameObject>();

	void OnEnable()
	{
		if (preLobby == null) {
			preLobby = GetComponent<PreLobby>();
		}
		if (canvas == null) {
			canvas = GetComponentInParent<Canvas>();
		}
		if (preLobby == null) return;

		if (disableLegacyGui) {
			preLobby.useLegacyGUI = false;
		}

		preLobby.LobbyChanged += Refresh;
		WireButtons();
		Refresh();
	}

	void OnDisable()
	{
		if (preLobby != null) {
			preLobby.LobbyChanged -= Refresh;
		}
	}

	void LateUpdate()
	{
		UpdateAddBotButtons();
	}

	void WireButtons()
	{
		if (backButton != null) {
			backButton.onClick.RemoveAllListeners();
			backButton.onClick.AddListener(() => preLobby.BackOrDisconnect());
		}

		if (restartButton != null) {
			restartButton.onClick.RemoveAllListeners();
			restartButton.onClick.AddListener(() => preLobby.RestartLobby());
		}

		if (startButton != null) {
			startButton.onClick.RemoveAllListeners();
			startButton.onClick.AddListener(() => preLobby.StartHeroSelection());
		}

		if (addBotButtons == null) return;

		for (int i = 0; i < addBotButtons.Length; i++) {
			RectTransform buttonRect = addBotButtons[i];
			if (buttonRect == null) continue;

			Button button = buttonRect.GetComponent<Button>();
			if (button == null) continue;

			int buttonIndex = i;
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(() => preLobby.TryAddBot(buttonIndex));
		}
	}

	void Refresh()
	{
		if (preLobby == null) return;

		if (teamSelectionRoot != null) {
			teamSelectionRoot.SetActive(preLobby.IsTeamSelectionState());
		}
		if (heroSelectionRoot != null) {
			heroSelectionRoot.SetActive(preLobby.IsHeroSelectionState());
		}

		RebuildPlayerList(redPlayersContainer, preLobby.GetPlayersForTeam(1));
		RebuildPlayerList(spectatingPlayersContainer, preLobby.GetPlayersForTeam(0));
		RebuildPlayerList(bluePlayersContainer, preLobby.GetPlayersForTeam(2));
		UpdateTopButtons();
		UpdateAddBotButtons();
	}

	void UpdateTopButtons()
	{
		bool teamSelection = preLobby.IsTeamSelectionState();
		bool localGame = preLobby.IsLocalGame();

		if (startButton != null) {
			startButton.gameObject.SetActive(teamSelection && localGame);
		}
		if (restartButton != null) {
			restartButton.gameObject.SetActive(teamSelection && localGame);
		}
		if (backButton != null) {
			backButton.gameObject.SetActive(teamSelection);
		}
	}

	void RebuildPlayerList(Transform container, Lobby.LobbyPlayerView[] players)
	{
		if (container == null || playerRowPrefab == null) return;

		for (int i = container.childCount - 1; i >= 0; i--) {
			Destroy(container.GetChild(i).gameObject);
		}

		for (int i = 0; i < players.Length; i++) {
			LobbyPlayerRowUI row = Instantiate(playerRowPrefab, container);
			row.Bind(preLobby, players[i]);
		}
	}

	void UpdateAddBotButtons()
	{
		if (preLobby == null || addBotButtons == null || canvas == null) return;

		bool heroSelection = preLobby.IsHeroSelectionState();
		Camera[] cameras = preLobby.GetHeroSelectionCameras();
		RectTransform canvasRect = canvas.transform as RectTransform;

		for (int i = 0; i < addBotButtons.Length; i++) {
			RectTransform buttonRect = addBotButtons[i];
			if (buttonRect == null) continue;

			bool shouldShow = heroSelection && i < cameras.Length && cameras[i] != null && preLobby.CanAddBot(i);
			buttonRect.gameObject.SetActive(shouldShow);
			if (!shouldShow) continue;

			Vector3 screenPoint = cameras[i].ViewportToScreenPoint(new Vector3((Screen.width * 0.315f) / 748f, 0.5f, 0));
			Vector2 anchoredPosition;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out anchoredPosition);
			buttonRect.anchoredPosition = anchoredPosition;
		}
	}
}
