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
	private Text redEmptyLabel;
	private Text blueEmptyLabel;
	private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

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
		if (preLobby == null)
			preLobby = GetComponent<PreLobby>();
		if (preLobby == null)
			preLobby = FindObjectOfType<PreLobby>();
		if (canvas == null)
			canvas = GetComponentInParent<Canvas>();
		if (canvas == null)
			canvas = FindObjectOfType<Canvas>();
	}

	void EnsureCanvasExists()
	{
		if (canvas == null) {
			GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			canvas = canvasObject.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		}

		CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
		if (scaler == null)
			scaler = canvas.gameObject.AddComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
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

		RectTransform teamRootRect = CreatePanel("TeamSelectionRoot", runtimeRoot, new Color(0.035f, 0.04f, 0.065f, 0.97f));
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
			if (lobbies[i] != null)
				lobbies[i].useLegacyGUI = false;
		}
	}

	void BuildTeamSelection(RectTransform root)
	{
		// --- HEADER ---
		RectTransform header = CreateRect("Header", root);
		AnchorStretch(header, new Vector2(0.1f, 0.85f), new Vector2(0.9f, 0.97f));

		VerticalLayoutGroup headerLayout = header.gameObject.AddComponent<VerticalLayoutGroup>();
		headerLayout.childAlignment = TextAnchor.MiddleCenter;
		headerLayout.spacing = 6f;
		headerLayout.childControlWidth = true;
		headerLayout.childControlHeight = true;
		headerLayout.childForceExpandWidth = true;
		headerLayout.childForceExpandHeight = false;

		Text subtitle = CreateLabel(header, "Subtitle", "L O C A L   M U L T I P L A Y E R",
			16, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.45f, 0.5f, 0.62f, 0.8f));
		LayoutElement subtitleLe = subtitle.gameObject.AddComponent<LayoutElement>();
		subtitleLe.preferredHeight = 24f;

		Text title = CreateLabel(header, "Title", "Choose your team",
			42, TextAnchor.MiddleCenter, FontStyle.Bold, Color.white);
		LayoutElement titleLe = title.gameObject.AddComponent<LayoutElement>();
		titleLe.preferredHeight = 54f;

		// --- COLUMNS ---
		RectTransform columns = CreateRect("Columns", root);
		AnchorStretch(columns, new Vector2(0.025f, 0.13f), new Vector2(0.975f, 0.82f));

		HorizontalLayoutGroup columnsLayout = columns.gameObject.AddComponent<HorizontalLayoutGroup>();
		columnsLayout.spacing = 14f;
		columnsLayout.childAlignment = TextAnchor.UpperCenter;
		columnsLayout.childControlWidth = true;
		columnsLayout.childControlHeight = true;
		columnsLayout.childForceExpandWidth = false;
		columnsLayout.childForceExpandHeight = true;

		redPlayersContainer = CreateTeamColumn(columns, "Red",
			new Color(0.22f, 0.05f, 0.07f, 1f),
			new Color(0.4f, 0.1f, 0.12f, 0.4f),
			out redEmptyLabel);

		spectatingPlayersContainer = CreateCenterColumn(columns);

		bluePlayersContainer = CreateTeamColumn(columns, "Blue",
			new Color(0.05f, 0.09f, 0.24f, 1f),
			new Color(0.1f, 0.16f, 0.42f, 0.35f),
			out blueEmptyLabel);

		// --- FOOTER ---
		RectTransform footerPanel = CreateRoundedGradientPanel(
			"FooterPanel",
			root,
			new Color(0.045f, 0.05f, 0.08f, 1f),
			new Color(0.055f, 0.06f, 0.1f, 1f),
			new Color(0.045f, 0.05f, 0.08f, 1f),
			new Color(0.12f, 0.16f, 0.26f, 0.38f),
			22,
			2,
			1024,
			128
		);
		AnchorStretch(footerPanel, new Vector2(0.025f, 0.01f), new Vector2(0.975f, 0.105f));

		HorizontalLayoutGroup footerLayout = footerPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
		footerLayout.padding = new RectOffset(30, 18, 10, 10);
		footerLayout.spacing = 20f;
		footerLayout.childAlignment = TextAnchor.MiddleLeft;
		footerLayout.childControlWidth = true;
		footerLayout.childControlHeight = false;
		footerLayout.childForceExpandWidth = false;
		footerLayout.childForceExpandHeight = false;

		Text hint = CreateLabel(footerPanel, "HintText",
			"Each player presses left or right on their own controller.",
			17, TextAnchor.MiddleLeft, FontStyle.Normal, new Color(0.48f, 0.52f, 0.62f, 0.9f));
		hint.horizontalOverflow = HorizontalWrapMode.Overflow;
		hint.verticalOverflow = VerticalWrapMode.Truncate;
		LayoutElement hintLe = hint.gameObject.AddComponent<LayoutElement>();
		hintLe.minWidth = 0f;
		hintLe.flexibleWidth = 1f;
		hintLe.preferredHeight = 60f;

		startButton = CreateFooterButton(footerPanel, "StartButton", "Start Character Select");
		backButton = null;
		restartButton = null;
	}

	Transform CreateTeamColumn(RectTransform parent, string teamName, Color panelColor, Color borderColor, out Text emptyLabel)
	{
		Color columnGlow = teamName == "Red"
			? new Color(0.28f, 0.07f, 0.1f, 1f)
			: new Color(0.06f, 0.18f, 0.32f, 1f);
		RectTransform outer = CreateRoundedGradientPanel(
			teamName + "Panel",
			parent,
			panelColor,
			columnGlow,
			panelColor,
			borderColor,
			42,
			3,
			384,
			512
		);
		LayoutElement outerLe = outer.gameObject.AddComponent<LayoutElement>();
		outerLe.minWidth = 0f;
		outerLe.preferredWidth = 560f;
		outerLe.flexibleWidth = 1f;

		VerticalLayoutGroup layout = outer.gameObject.AddComponent<VerticalLayoutGroup>();
		layout.padding = new RectOffset(20, 20, 18, 18);
		layout.spacing = 12f;
		layout.childAlignment = TextAnchor.UpperCenter;
		layout.childControlWidth = true;
		layout.childControlHeight = false;
		layout.childForceExpandWidth = true;
		layout.childForceExpandHeight = false;

		RectTransform topSpacer = CreateRect(teamName + "HeaderSpacer", outer);
		LayoutElement spacerLe = topSpacer.gameObject.AddComponent<LayoutElement>();
		spacerLe.preferredHeight = 26f;

		Text teamLabel = CreateLabel(outer, teamName + "Label", SpaceOutText(teamName.ToUpper()),
			16, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.55f, 0.58f, 0.68f, 0.55f));
		RectTransform teamLabelRect = teamLabel.GetComponent<RectTransform>();
		teamLabelRect.anchorMin = new Vector2(0f, 1f);
		teamLabelRect.anchorMax = new Vector2(1f, 1f);
		teamLabelRect.pivot = new Vector2(0.5f, 1f);
		teamLabelRect.anchoredPosition = new Vector2(0f, -18f);
		teamLabelRect.sizeDelta = new Vector2(0f, 26f);
		LayoutElement labelLe = teamLabel.gameObject.AddComponent<LayoutElement>();
		labelLe.ignoreLayout = true;

		RectTransform container = CreateRect(teamName + "PlayersContainer", outer);
		VerticalLayoutGroup listLayout = container.gameObject.AddComponent<VerticalLayoutGroup>();
		listLayout.spacing = 10f;
		listLayout.childAlignment = TextAnchor.UpperCenter;
		listLayout.childControlWidth = true;
		listLayout.childControlHeight = false;
		listLayout.childForceExpandWidth = true;
		listLayout.childForceExpandHeight = false;

		LayoutElement containerLe = container.gameObject.AddComponent<LayoutElement>();
		containerLe.flexibleHeight = 1f;

		emptyLabel = CreateLabel(outer, teamName + "Empty", "EMPTY",
			18, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.4f, 0.43f, 0.52f, 0.3f));
		RectTransform emptyRect = emptyLabel.GetComponent<RectTransform>();
		emptyRect.anchorMin = new Vector2(0f, 0.2f);
		emptyRect.anchorMax = new Vector2(1f, 0.7f);
		emptyRect.offsetMin = Vector2.zero;
		emptyRect.offsetMax = Vector2.zero;
		LayoutElement emptyLe = emptyLabel.gameObject.AddComponent<LayoutElement>();
		emptyLe.ignoreLayout = true;

		return container;
	}

	Transform CreateCenterColumn(RectTransform parent)
	{
		RectTransform outer = CreateRoundedGradientPanel(
			"CenterPanel",
			parent,
			new Color(0.065f, 0.07f, 0.11f, 1f),
			new Color(0.07f, 0.075f, 0.12f, 1f),
			new Color(0.065f, 0.07f, 0.11f, 1f),
			new Color(0.18f, 0.22f, 0.32f, 0.35f),
			28,
			2,
			384,
			512
		);
		LayoutElement outerLe = outer.gameObject.AddComponent<LayoutElement>();
		outerLe.minWidth = 0f;
		outerLe.preferredWidth = 560f;
		outerLe.flexibleWidth = 1f;

		VerticalLayoutGroup layout = outer.gameObject.AddComponent<VerticalLayoutGroup>();
		layout.padding = new RectOffset(20, 20, 18, 18);
		layout.spacing = 12f;
		layout.childAlignment = TextAnchor.UpperCenter;
		layout.childControlWidth = true;
		layout.childControlHeight = false;
		layout.childForceExpandWidth = true;
		layout.childForceExpandHeight = false;

		RectTransform topSpacer = CreateRect("HeaderSpacer", outer);
		LayoutElement spacerLe = topSpacer.gameObject.AddComponent<LayoutElement>();
		spacerLe.preferredHeight = 26f;

		RectTransform container = CreateRect("SpectatingPlayersContainer", outer);
		VerticalLayoutGroup listLayout = container.gameObject.AddComponent<VerticalLayoutGroup>();
		listLayout.spacing = 10f;
		listLayout.childAlignment = TextAnchor.UpperCenter;
		listLayout.childControlWidth = true;
		listLayout.childControlHeight = false;
		listLayout.childForceExpandWidth = true;
		listLayout.childForceExpandHeight = false;

		LayoutElement containerLe = container.gameObject.AddComponent<LayoutElement>();
		containerLe.flexibleHeight = 1f;

		return container;
	}

	void BuildHeroSelection(RectTransform root)
	{
		RectTransform topBar = CreatePanel("HeroTopBar", root, new Color(0.04f, 0.045f, 0.08f, 0.9f));
		AnchorStretch(topBar, new Vector2(0.15f, 0.9f), new Vector2(0.85f, 0.99f));

		VerticalLayoutGroup layout = topBar.gameObject.AddComponent<VerticalLayoutGroup>();
		layout.padding = new RectOffset(24, 24, 14, 14);
		layout.spacing = 6f;
		layout.childAlignment = TextAnchor.MiddleCenter;
		layout.childControlWidth = true;
		layout.childControlHeight = true;
		layout.childForceExpandWidth = true;
		layout.childForceExpandHeight = true;

		CreateLabel(topBar, "HeroSelectionTitle", "CHOOSE YOUR HERO",
			40, TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.94f, 0.95f, 1f, 1f));
		CreateLabel(topBar, "HeroSelectionHint", "Use each player's input to rotate and confirm a hero.",
			20, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.55f, 0.6f, 0.72f, 0.8f));
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

		if (teamSelectionRoot != null)
			teamSelectionRoot.SetActive(preLobby.IsTeamSelectionState());
		if (heroSelectionRoot != null)
			heroSelectionRoot.SetActive(preLobby.IsHeroSelectionState());

		Lobby.LobbyPlayerView[] redPlayers = preLobby.GetPlayersForTeam(1);
		Lobby.LobbyPlayerView[] specPlayers = preLobby.GetPlayersForTeam(0);
		Lobby.LobbyPlayerView[] bluePlayers = preLobby.GetPlayersForTeam(2);

		RebuildPlayerList(redPlayersContainer, redPlayers);
		RebuildPlayerList(spectatingPlayersContainer, specPlayers);
		RebuildPlayerList(bluePlayersContainer, bluePlayers);

		if (redEmptyLabel != null)
			redEmptyLabel.gameObject.SetActive(redPlayers.Length == 0);
		if (blueEmptyLabel != null)
			blueEmptyLabel.gameObject.SetActive(bluePlayers.Length == 0);

		UpdateFooter();
	}

	void UpdateFooter()
	{
		bool teamSelection = preLobby.IsTeamSelectionState();
		bool localGame = preLobby.IsLocalGame();

		if (startButton != null)
			startButton.gameObject.SetActive(teamSelection && localGame);
	}

	void RebuildPlayerList(Transform container, Lobby.LobbyPlayerView[] players)
	{
		if (container == null) return;
		ClearChildren(container);
		for (int i = 0; i < players.Length; i++)
			CreatePlayerRow(container, players[i]);
	}

	void CreatePlayerRow(Transform parent, Lobby.LobbyPlayerView player)
	{
		RectTransform row = CreateRect(player.name + "Row", parent);
		LayoutElement rowLe = row.gameObject.AddComponent<LayoutElement>();
		rowLe.minWidth = 0f;
		rowLe.preferredWidth = 0f;
		rowLe.flexibleWidth = 1f;
		rowLe.minHeight = 90f;
		rowLe.preferredHeight = 90f;

		HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
		layout.padding = new RectOffset(0, 0, 4, 4);
		layout.spacing = 18f;
		layout.childAlignment = TextAnchor.MiddleCenter;
		layout.childControlWidth = false;
		layout.childControlHeight = false;
		layout.childForceExpandWidth = false;
		layout.childForceExpandHeight = false;

		bool canLeft = CanMoveLeft(player);
		bool canRight = CanMoveRight(player);

		CreateArrowLabel(row, "LeftArrow", "\u2190", canLeft,
			new Color(0.65f, 0.4f, 0.38f, canLeft ? 0.7f : 0.1f),
			() => MoveLeft(player));

		CreatePlayerCard(row, player);

		CreateArrowLabel(row, "RightArrow", "\u2192", canRight,
			new Color(0.38f, 0.48f, 0.75f, canRight ? 0.7f : 0.1f),
			() => MoveRight(player));
	}

	void CreatePlayerCard(RectTransform parent, Lobby.LobbyPlayerView player)
	{
		Color leftColor;
		Color centerColor;
		Color rightColor;
		Color borderColor;
		GetTokenColors(player.team, out leftColor, out centerColor, out rightColor, out borderColor);

		RectTransform card = CreateRoundedGradientPanel(
			player.name + "Card",
			parent,
			leftColor,
			centerColor,
			rightColor,
			borderColor,
			30,
			3,
			320,
			176
		);
		LayoutElement cardLe = card.gameObject.AddComponent<LayoutElement>();
		cardLe.minWidth = 252f;
		cardLe.preferredWidth = 252f;
		cardLe.minHeight = 92f;
		cardLe.preferredHeight = 92f;

		VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
		cardLayout.padding = new RectOffset(18, 18, 16, 12);
		cardLayout.spacing = 3f;
		cardLayout.childAlignment = TextAnchor.MiddleCenter;
		cardLayout.childControlWidth = true;
		cardLayout.childControlHeight = true;
		cardLayout.childForceExpandWidth = true;
		cardLayout.childForceExpandHeight = false;

		float availableNameWidth = cardLe.preferredWidth - cardLayout.padding.left - cardLayout.padding.right;
		Text nameLabel = CreateLabel(card, "Name", player.name,
			ResolveFontSizeForWidth(player.name, availableNameWidth, 24, 12, FontStyle.Bold),
			TextAnchor.MiddleCenter, FontStyle.Bold, new Color(0.95f, 0.96f, 1f, 1f));
		nameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
		nameLabel.verticalOverflow = VerticalWrapMode.Truncate;
		nameLabel.resizeTextForBestFit = false;
		LayoutElement nameLe = nameLabel.gameObject.AddComponent<LayoutElement>();
		nameLe.preferredHeight = 34f;

		Text posLabel = CreateLabel(card, "Position", GetPositionLabel(player.team),
			14, TextAnchor.MiddleCenter, FontStyle.Normal, new Color(0.5f, 0.54f, 0.64f, 0.75f));
		LayoutElement posLe = posLabel.gameObject.AddComponent<LayoutElement>();
		posLe.preferredHeight = 20f;
	}

	void CreateArrowLabel(RectTransform parent, string name, string arrow, bool isActive, Color color, UnityEngine.Events.UnityAction onClick)
	{
		RectTransform rect = CreateRect(name, parent);
		rect.sizeDelta = new Vector2(40f, 40f);

		Text label = rect.gameObject.AddComponent<Text>();
		label.font = defaultFont;
		label.fontSize = 26;
		label.fontStyle = FontStyle.Normal;
		label.alignment = TextAnchor.MiddleCenter;
		label.color = color;
		label.text = arrow;

		if (isActive) {
			Button btn = rect.gameObject.AddComponent<Button>();
			btn.targetGraphic = label;
			ColorBlock colors = btn.colors;
			colors.normalColor = Color.white;
			colors.highlightedColor = new Color(1.2f, 1.2f, 1.3f, 1f);
			colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
			colors.fadeDuration = 0.08f;
			btn.colors = colors;
			btn.onClick.AddListener(onClick);
		}
	}

	Button CreateFooterButton(Transform parent, string name, string text)
	{
		RectTransform buttonRect = CreateRoundedGradientPanel(
			name,
			parent,
			new Color(0.08f, 0.09f, 0.15f, 1f),
			new Color(0.10f, 0.12f, 0.20f, 1f),
			new Color(0.08f, 0.09f, 0.15f, 1f),
			new Color(0.18f, 0.24f, 0.38f, 0.6f),
			18,
			2,
			512,
			128
		).GetComponent<RectTransform>();

		Image image = buttonRect.GetComponent<Image>();
		Button button = buttonRect.gameObject.AddComponent<Button>();
		ColorBlock colors = button.colors;
		colors.normalColor = Color.white;
		colors.highlightedColor = new Color(0.9f, 0.92f, 1f, 1f);
		colors.pressedColor = new Color(0.7f, 0.72f, 0.78f, 1f);
		colors.fadeDuration = 0.08f;
		button.colors = colors;
		button.targetGraphic = image;

		RectTransform labelRect = CreateRect("Label", buttonRect);
		Stretch(labelRect, 0f);
		Text label = labelRect.gameObject.AddComponent<Text>();
		label.font = defaultFont;
		label.fontSize = 19;
		label.fontStyle = FontStyle.Bold;
		label.alignment = TextAnchor.MiddleCenter;
		label.color = new Color(0.85f, 0.88f, 0.96f, 1f);
		label.text = text;

		LayoutElement le = buttonRect.gameObject.AddComponent<LayoutElement>();
		le.minWidth = 290f;
		le.preferredWidth = 290f;
		le.minHeight = 58f;
		le.preferredHeight = 58f;

		return button;
	}

	string GetPositionLabel(int team)
	{
		switch (team) {
			case 1: return "RED";
			case 2: return "BLUE";
			default: return "MID";
		}
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

	RectTransform CreateRoundedGradientPanel(string name, Transform parent, Color leftColor, Color centerColor, Color rightColor, Color borderColor, int radius, int borderSize, int textureWidth, int textureHeight)
	{
		RectTransform rect = CreateRect(name, parent);
		Image image = rect.gameObject.AddComponent<Image>();
		image.sprite = GetRoundedGradientSprite(name, leftColor, centerColor, rightColor, borderColor, radius, borderSize, textureWidth, textureHeight);
		image.color = Color.white;
		image.type = Image.Type.Simple;
		return rect;
	}

	RectTransform GetOrCreateRect(string name, Transform parent)
	{
		Transform existing = parent.Find(name);
		if (existing != null)
			return existing as RectTransform;
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
			if (Application.isPlaying)
				Destroy(parent.GetChild(i).gameObject);
			else
				DestroyImmediate(parent.GetChild(i).gameObject);
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

	void GetTokenColors(int team, out Color leftColor, out Color centerColor, out Color rightColor, out Color borderColor)
	{
		switch (team) {
			case 1:
				leftColor = new Color(0.30f, 0.06f, 0.10f, 1f);
				centerColor = new Color(0.38f, 0.10f, 0.14f, 1f);
				rightColor = new Color(0.30f, 0.06f, 0.10f, 1f);
				borderColor = new Color(0.55f, 0.22f, 0.26f, 0.75f);
				break;
			case 2:
				leftColor = new Color(0.05f, 0.14f, 0.30f, 1f);
				centerColor = new Color(0.08f, 0.22f, 0.40f, 1f);
				rightColor = new Color(0.05f, 0.14f, 0.30f, 1f);
				borderColor = new Color(0.15f, 0.40f, 0.60f, 0.75f);
				break;
			default:
				leftColor = new Color(0.12f, 0.14f, 0.20f, 1f);
				centerColor = new Color(0.16f, 0.18f, 0.24f, 1f);
				rightColor = new Color(0.12f, 0.14f, 0.20f, 1f);
				borderColor = new Color(0.28f, 0.32f, 0.40f, 0.65f);
				break;
		}
	}

	Sprite GetRoundedGradientSprite(string keyPrefix, Color leftColor, Color centerColor, Color rightColor, Color borderColor, int radius, int borderSize, int textureWidth, int textureHeight)
	{
		string key = keyPrefix + "_" + leftColor + "_" + centerColor + "_" + rightColor + "_" + borderColor + "_" + radius + "_" + borderSize + "_" + textureWidth + "_" + textureHeight;
		Sprite sprite;
		if (spriteCache.TryGetValue(key, out sprite))
			return sprite;

		Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = FilterMode.Bilinear;

		float halfWidth = textureWidth * 0.5f;
		float halfHeight = textureHeight * 0.5f;
		float cornerRadius = Mathf.Min(radius, Mathf.Min(textureWidth, textureHeight) * 0.5f - 1f);
		float aa = 1.25f;

		for (int y = 0; y < textureHeight; y++) {
			float v = textureHeight <= 1 ? 0f : (float)y / (textureHeight - 1);
			for (int x = 0; x < textureWidth; x++) {
				float u = textureWidth <= 1 ? 0f : (float)x / (textureWidth - 1);
				Vector2 p = new Vector2(x + 0.5f - halfWidth, y + 0.5f - halfHeight);
				Vector2 halfSize = new Vector2(halfWidth, halfHeight);
				float sd = SignedDistanceRoundedBox(p, halfSize, cornerRadius);

				float alpha = sd <= 0f ? 1f : Mathf.Clamp01(1f - (sd / aa));
				if (alpha <= 0f) {
					texture.SetPixel(x, y, Color.clear);
					continue;
				}

				Color fillColor = EvaluateHorizontalGradient(leftColor, centerColor, rightColor, u);
				float verticalBoost = Mathf.Lerp(0.94f, 1.02f, 1f - Mathf.Abs(v - 0.5f) * 2f);
				fillColor = new Color(
					Mathf.Clamp01(fillColor.r * verticalBoost),
					Mathf.Clamp01(fillColor.g * verticalBoost),
					Mathf.Clamp01(fillColor.b * verticalBoost),
					1f
				);

				float borderLerp = borderSize <= 0 ? 0f : Mathf.Clamp01(1f + (sd / borderSize));
				Color color = Color.Lerp(fillColor, borderColor, borderLerp);
				color.a *= alpha;
				texture.SetPixel(x, y, color);
			}
		}

		texture.Apply();
		sprite = Sprite.Create(texture, new Rect(0f, 0f, textureWidth, textureHeight), new Vector2(0.5f, 0.5f), 100f);
		spriteCache[key] = sprite;
		return sprite;
	}

	Color EvaluateHorizontalGradient(Color leftColor, Color centerColor, Color rightColor, float t)
	{
		if (t <= 0.5f)
			return Color.Lerp(leftColor, centerColor, t / 0.5f);
		return Color.Lerp(centerColor, rightColor, (t - 0.5f) / 0.5f);
	}

	float SignedDistanceRoundedBox(Vector2 point, Vector2 halfSize, float radius)
	{
		Vector2 innerSize = halfSize - new Vector2(radius, radius);
		Vector2 q = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - innerSize;
		Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
		return outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
	}

	string SpaceOutText(string text)
	{
		if (string.IsNullOrEmpty(text)) return text;
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		for (int i = 0; i < text.Length; i++) {
			if (i > 0) sb.Append(' ');
			sb.Append(text[i]);
		}
		return sb.ToString();
	}

	int ResolveFontSizeForWidth(string text, float maxWidth, int maxSize, int minSize, FontStyle fontStyle)
	{
		if (string.IsNullOrEmpty(text) || defaultFont == null)
			return maxSize;

		TextGenerator generator = new TextGenerator();
		TextGenerationSettings settings = new TextGenerationSettings();
		settings.font = defaultFont;
		settings.fontStyle = fontStyle;
		settings.scaleFactor = 1f;
		settings.richText = false;
		settings.lineSpacing = 1f;
		settings.textAnchor = TextAnchor.MiddleCenter;
		settings.alignByGeometry = false;
		settings.resizeTextForBestFit = false;
		settings.updateBounds = false;
		settings.horizontalOverflow = HorizontalWrapMode.Overflow;
		settings.verticalOverflow = VerticalWrapMode.Overflow;
		settings.generationExtents = new Vector2(maxWidth, 40f);
		settings.pivot = new Vector2(0.5f, 0.5f);
		settings.color = Color.white;

		for (int size = maxSize; size >= minSize; size--) {
			settings.fontSize = size;
			float preferredWidth = generator.GetPreferredWidth(text, settings);
			if (preferredWidth <= maxWidth)
				return size;
		}

		return minSize;
	}
}
