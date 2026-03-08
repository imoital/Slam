using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

	private const string RuntimeRootName = "RuntimePreLobbyUI";

	private Font defaultFont;
	private RectTransform runtimeRoot;
	private bool isSubscribed;

	void OnEnable()
	{
		ResolveReferences();
		if (preLobby == null) return;

		EnsureCanvasExists();
		EnsureEventSystemExists();
		EnsureRuntimeUi();
		ApplyLegacyGuiSetting();
		Subscribe();
		Refresh();
	}

	void OnDisable()
	{
		if (preLobby != null && isSubscribed) {
			preLobby.LobbyChanged -= Refresh;
			isSubscribed = false;
		}
	}

	void LateUpdate()
	{
		ApplyLegacyGuiSetting();
	}

	void ResolveReferences()
	{
		if (preLobby == null) {
			preLobby = GetComponent<PreLobby>();
		}
		if (preLobby == null) {
			preLobby = FindObjectOfType<PreLobby>();
		}
		if (canvas == null) {
			canvas = GetComponentInParent<Canvas>();
		}
		if (canvas == null) {
			canvas = FindObjectOfType<Canvas>();
		}
	}

	void EnsureCanvasExists()
	{
		if (canvas != null) return;

		GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		canvas = canvasObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;

		CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1280f, 720f);
		scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		scaler.matchWidthOrHeight = 0.5f;
	}

	void EnsureEventSystemExists()
	{
		if (FindObjectOfType<EventSystem>() != null) return;

		new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
	}

	void EnsureRuntimeUi()
	{
		defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		DisableOldAssignedUi();

		runtimeRoot = GetOrCreateRect(RuntimeRootName, canvas.transform);
		Stretch(runtimeRoot, 0f);
		runtimeRoot.SetAsLastSibling();

		ClearChildren(runtimeRoot);

		RectTransform teamRootRect = CreatePanel("TeamSelectionRoot", runtimeRoot, new Color(0.08f, 0.1f, 0.14f, 0.86f));
		Stretch(teamRootRect, 0f);
		teamSelectionRoot = teamRootRect.gameObject;

		RectTransform heroRootRect = CreateHeroSelectionRoot(runtimeRoot);
		Stretch(heroRootRect, 0f);
		heroSelectionRoot = heroRootRect.gameObject;

		BuildTeamSelection(teamSelectionRoot.GetComponent<RectTransform>());
		BuildHeroSelection(heroSelectionRoot.GetComponent<RectTransform>());
		WireButtons();
	}

	void DisableOldAssignedUi()
	{
		DisableIfForeign(teamSelectionRoot);
		DisableIfForeign(heroSelectionRoot);
		DisableIfForeign(backButton != null ? backButton.gameObject : null);
		DisableIfForeign(restartButton != null ? restartButton.gameObject : null);
		DisableIfForeign(startButton != null ? startButton.gameObject : null);
	}

	void DisableIfForeign(GameObject target)
	{
		if (target == null) return;
		if (runtimeRoot != null && target.transform.IsChildOf(runtimeRoot)) return;

		target.SetActive(false);
	}

	void Subscribe()
	{
		if (isSubscribed) return;

		preLobby.LobbyChanged += Refresh;
		isSubscribed = true;
	}

	void ApplyLegacyGuiSetting()
	{
		if (!disableLegacyGui) return;

		Lobby[] lobbies = FindObjectsOfType<Lobby>();
		for (int i = 0; i < lobbies.Length; i++) {
			if (lobbies[i] != null) {
				lobbies[i].useLegacyGUI = false;
			}
		}
	}

	void BuildTeamSelection(RectTransform root)
	{
		RectTransform columns = CreateRect("Columns", root);
		AnchorStretch(columns, new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.88f));

		HorizontalLayoutGroup columnsLayout = columns.gameObject.AddComponent<HorizontalLayoutGroup>();
		columnsLayout.spacing = 24f;
		columnsLayout.childAlignment = TextAnchor.UpperCenter;
		columnsLayout.childControlWidth = true;
		columnsLayout.childControlHeight = true;
		columnsLayout.childForceExpandWidth = true;
		columnsLayout.childForceExpandHeight = false;

		redPlayersContainer = CreateColumn(columns, "Red", new Color(0.72f, 0.2f, 0.24f, 0.92f));
		spectatingPlayersContainer = CreateColumn(columns, "Spectating", new Color(0.2f, 0.22f, 0.28f, 0.92f));
		bluePlayersContainer = CreateColumn(columns, "Blue", new Color(0.24f, 0.34f, 0.72f, 0.92f));

		RectTransform footer = CreateRect("FooterButtons", root);
		AnchorStretch(footer, new Vector2(0.2f, 0.04f), new Vector2(0.8f, 0.12f));

		HorizontalLayoutGroup footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
		footerLayout.spacing = 16f;
		footerLayout.childAlignment = TextAnchor.MiddleCenter;
		footerLayout.childControlWidth = false;
		footerLayout.childControlHeight = false;
		footerLayout.childForceExpandWidth = false;
		footerLayout.childForceExpandHeight = false;

		backButton = CreateButton(footer, "BackButton", "Back", new Color(0.28f, 0.31f, 0.39f, 0.95f));
		restartButton = CreateButton(footer, "RestartButton", "Restart", new Color(0.5f, 0.42f, 0.18f, 0.95f));
		startButton = CreateButton(footer, "StartButton", "Start", new Color(0.16f, 0.55f, 0.32f, 0.95f));
	}

	void BuildHeroSelection(RectTransform root)
	{
		RectTransform topBar = CreateRect("HeroTopBar", root);
		AnchorStretch(topBar, new Vector2(0.18f, 0.92f), new Vector2(0.82f, 0.985f));

		VerticalLayoutGroup layout = topBar.gameObject.AddComponent<VerticalLayoutGroup>();
		layout.childAlignment = TextAnchor.MiddleCenter;
		layout.childControlWidth = true;
		layout.childControlHeight = true;
		layout.childForceExpandWidth = true;
		layout.childForceExpandHeight = true;

		CreateLabel(topBar, "HeroSelectionTitle", "Hero Selection", 28, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
		CreateLabel(topBar, "HeroSelectionHint", "Use each player's input to rotate and confirm a hero.", 16, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.9f, 0.93f, 1f, 0.95f));
	}

	Transform CreateColumn(RectTransform parent, string title, Color panelColor)
	{
		RectTransform panel = CreatePanel(title + "Panel", parent, panelColor);
		LayoutElement panelLayout = panel.gameObject.AddComponent<LayoutElement>();
		panelLayout.preferredWidth = 260f;

		VerticalLayoutGroup panelGroup = panel.gameObject.AddComponent<VerticalLayoutGroup>();
		panelGroup.padding = new RectOffset(16, 16, 16, 16);
		panelGroup.spacing = 12f;
		panelGroup.childAlignment = TextAnchor.UpperCenter;
		panelGroup.childControlWidth = true;
		panelGroup.childControlHeight = false;
		panelGroup.childForceExpandWidth = true;
		panelGroup.childForceExpandHeight = false;

		CreateLabel(panel, title + "Title", title, 24, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);

		RectTransform container = CreateRect(title + "PlayersContainer", panel);
		VerticalLayoutGroup listLayout = container.gameObject.AddComponent<VerticalLayoutGroup>();
		listLayout.spacing = 10f;
		listLayout.childAlignment = TextAnchor.UpperCenter;
		listLayout.childControlWidth = true;
		listLayout.childControlHeight = false;
		listLayout.childForceExpandWidth = true;
		listLayout.childForceExpandHeight = false;

		ContentSizeFitter fitter = container.gameObject.AddComponent<ContentSizeFitter>();
		fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		LayoutElement containerLayout = container.gameObject.AddComponent<LayoutElement>();
		containerLayout.flexibleHeight = 1f;

		return container;
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
		if (container == null) return;

		ClearChildren(container);

		for (int i = 0; i < players.Length; i++) {
			CreatePlayerRow(container, players[i]);
		}
	}

	void CreatePlayerRow(Transform parent, Lobby.LobbyPlayerView player)
	{
		RectTransform row = CreatePanel(player.name + "Row", parent, new Color(1f, 1f, 1f, 0.12f));
		LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
		rowLayout.minHeight = 52f;
		rowLayout.preferredHeight = 52f;

		HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
		layout.padding = new RectOffset(10, 10, 8, 8);
		layout.spacing = 8f;
		layout.childAlignment = TextAnchor.MiddleCenter;
		layout.childControlWidth = false;
		layout.childControlHeight = true;
		layout.childForceExpandWidth = false;
		layout.childForceExpandHeight = false;

		CreateArrowButton(row, "Left", "<", CanMoveLeft(player), () => MoveLeft(player));
		CreatePlayerName(row, player.name);
		CreateArrowButton(row, "Right", ">", CanMoveRight(player), () => MoveRight(player));
	}

	void MoveLeft(Lobby.LobbyPlayerView player)
	{
		if (preLobby == null) return;

		int newTeam = player.team == 0 ? 1 : 0;
		preLobby.MoveLocalPlayer(player.controller, player.team, newTeam);
	}

	void MoveRight(Lobby.LobbyPlayerView player)
	{
		if (preLobby == null) return;

		int newTeam = player.team == 0 ? 2 : 0;
		preLobby.MoveLocalPlayer(player.controller, player.team, newTeam);
	}

	bool CanMoveLeft(Lobby.LobbyPlayerView player)
	{
		return preLobby != null && preLobby.IsLocalGame() && preLobby.CanShowLobbyArrows() && (player.team == 2 || player.team == 0);
	}

	bool CanMoveRight(Lobby.LobbyPlayerView player)
	{
		return preLobby != null && preLobby.IsLocalGame() && preLobby.CanShowLobbyArrows() && (player.team == 1 || player.team == 0);
	}

	Button CreateArrowButton(RectTransform parent, string name, string label, bool isEnabled, UnityEngine.Events.UnityAction onClick)
	{
		Button button = CreateButton(parent, name + "Button", isEnabled ? label : string.Empty, isEnabled ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0f));
		LayoutElement element = button.GetComponent<LayoutElement>();
		element.minWidth = 36f;
		element.preferredWidth = 36f;
		element.minHeight = 36f;
		element.preferredHeight = 36f;

		button.interactable = isEnabled;
		button.onClick.RemoveAllListeners();
		if (isEnabled) {
			button.onClick.AddListener(onClick);
		}

		return button;
	}

	Text CreatePlayerName(RectTransform parent, string playerName)
	{
		Text label = CreateLabel(parent, "PlayerName", playerName, 22, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
		LayoutElement element = label.gameObject.AddComponent<LayoutElement>();
		element.minWidth = 120f;
		element.flexibleWidth = 1f;

		label.horizontalOverflow = HorizontalWrapMode.Overflow;
		label.verticalOverflow = VerticalWrapMode.Truncate;
		return label;
	}

	Button CreateButton(Transform parent, string name, string text, Color backgroundColor)
	{
		RectTransform buttonRect = CreateRect(name, parent);
		Image image = buttonRect.gameObject.AddComponent<Image>();
		image.color = backgroundColor;
		image.type = Image.Type.Simple;

		Button button = buttonRect.gameObject.AddComponent<Button>();
		ColorBlock colors = button.colors;
		colors.normalColor = backgroundColor;
		colors.highlightedColor = MultiplyColor(backgroundColor, 1.1f);
		colors.pressedColor = MultiplyColor(backgroundColor, 0.85f);
		colors.disabledColor = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0.2f);
		button.colors = colors;

		RectTransform labelRect = CreateRect("Label", buttonRect);
		Stretch(labelRect, 0f);
		Text label = labelRect.gameObject.AddComponent<Text>();
		label.font = defaultFont;
		label.fontSize = 20;
		label.fontStyle = FontStyle.Bold;
		label.alignment = TextAnchor.MiddleCenter;
		label.color = Color.black;
		label.text = text;

		LayoutElement layout = buttonRect.gameObject.AddComponent<LayoutElement>();
		layout.minWidth = 120f;
		layout.preferredWidth = 120f;
		layout.minHeight = 42f;
		layout.preferredHeight = 42f;

		return button;
	}

	Text CreateLabel(Transform parent, string name, string text, int fontSize, TextAnchor alignment, FontStyle fontStyle, Color color)
	{
		RectTransform rect = CreateRect(name, parent);
		Text label = rect.gameObject.AddComponent<Text>();
		label.font = defaultFont;
		label.fontSize = fontSize;
		label.fontStyle = fontStyle;
		label.alignment = alignment;
		label.color = color;
		label.text = text;
		label.horizontalOverflow = HorizontalWrapMode.Wrap;
		label.verticalOverflow = VerticalWrapMode.Overflow;
		return label;
	}

	RectTransform CreateHeroSelectionRoot(RectTransform parent)
	{
		RectTransform root = CreateRect("HeroSelectionRoot", parent);
		Stretch(root, 0f);
		return root;
	}

	RectTransform CreatePanel(string name, Transform parent, Color color)
	{
		RectTransform rect = CreateRect(name, parent);
		Image image = rect.gameObject.AddComponent<Image>();
		image.color = color;
		image.type = Image.Type.Simple;
		return rect;
	}

	RectTransform GetOrCreateRect(string name, Transform parent)
	{
		Transform existing = parent.Find(name);
		if (existing != null) {
			return existing as RectTransform;
		}

		return CreateRect(name, parent);
	}

	RectTransform CreateRect(string name, Transform parent)
	{
		GameObject gameObject = new GameObject(name, typeof(RectTransform));
		RectTransform rect = gameObject.GetComponent<RectTransform>();
		rect.SetParent(parent, false);
		rect.localScale = Vector3.one;
		return rect;
	}

	void ClearChildren(Transform parent)
	{
		for (int i = parent.childCount - 1; i >= 0; i--) {
			if (Application.isPlaying) {
				Destroy(parent.GetChild(i).gameObject);
			} else {
				DestroyImmediate(parent.GetChild(i).gameObject);
			}
		}
	}

	void Stretch(RectTransform rect, float inset)
	{
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = new Vector2(inset, inset);
		rect.offsetMax = new Vector2(-inset, -inset);
	}

	void AnchorStretch(RectTransform rect, Vector2 min, Vector2 max)
	{
		rect.anchorMin = min;
		rect.anchorMax = max;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}

	Color MultiplyColor(Color color, float multiplier)
	{
		return new Color(
			Mathf.Clamp01(color.r * multiplier),
			Mathf.Clamp01(color.g * multiplier),
			Mathf.Clamp01(color.b * multiplier),
			color.a
		);
	}
}
