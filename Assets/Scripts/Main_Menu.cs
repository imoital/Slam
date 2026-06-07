using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Main_Menu : MonoBehaviour
{
    public GameObject settings_prefab;

    [SerializeField] string _localPlayScene = "Pre_Game_Lobby";
    [SerializeField] string _onlinePlayScene = "Pre_Game_Lobby";

    public Texture background_texture;

    private const int NICKNAME_SCREEN = 0;
    private const int MAIN_MENU = 1;
    private const int STANDARD_MAX_CHARS = 20;

    private GameObject settings;
    private Game_Settings game_settings;
    private int menu_state;
    private GameObject _canvasGo;
    private InputField _nicknameInput;

    void Awake()
    {
        if (settings_prefab != null && GameObject.FindGameObjectWithTag("settings") == null)
            settings = Instantiate(settings_prefab);
        else if (GameObject.FindGameObjectWithTag("settings") != null)
            settings = GameObject.FindGameObjectWithTag("settings");

        if (settings != null)
            game_settings = settings.GetComponent<Game_Settings>();

        menu_state = game_settings != null && game_settings.player_name != "" ? MAIN_MENU : NICKNAME_SCREEN;
    }

    void Start()
    {
        EnsureEventSystem();
        EnsureCanvas();
        RefreshMenuUi();
    }

    void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    void EnsureCanvas()
    {
        if (_canvasGo != null)
            return;
        _canvasGo = new GameObject("MainMenuCanvas");
        var canvas = _canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = _canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        _canvasGo.AddComponent<GraphicRaycaster>();

        if (background_texture != null)
        {
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(_canvasGo.transform, false);
            var raw = bgGo.AddComponent<RawImage>();
            raw.texture = background_texture;
            var rawRt = (RectTransform)raw.transform;
            rawRt.anchorMin = Vector2.zero;
            rawRt.anchorMax = Vector2.one;
            rawRt.offsetMin = Vector2.zero;
            rawRt.offsetMax = Vector2.zero;
        }
    }

    void RefreshMenuUi()
    {
        for (int i = _canvasGo.transform.childCount - 1; i >= 0; i--)
        {
            var ch = _canvasGo.transform.GetChild(i);
            if (ch.GetComponent<RawImage>() != null && ch.name == "Background")
                continue;
            Destroy(ch.gameObject);
        }

        if (menu_state == NICKNAME_SCREEN)
            BuildNicknameUi();
        else
            BuildMainMenuUi();
    }

    void BuildNicknameUi()
    {
        var panel = CreateFullStretchPanel(_canvasGo.transform, new Color(0.12f, 0.12f, 0.14f, 0.92f));
        var box = new GameObject("Box");
        box.transform.SetParent(panel.transform, false);
        var boxRt = box.AddComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(520f, 260f);
        box.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.2f, 1f);
        var vlg = box.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(24, 24, 24, 24);
        vlg.spacing = 16f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        CreateLabel(box.transform, "Nickname");

        _nicknameInput = CreateInputField(box.transform, game_settings != null ? game_settings.player_name : "");
        var leIn = _nicknameInput.gameObject.AddComponent<LayoutElement>();
        leIn.minHeight = 48f;
        leIn.preferredHeight = 48f;

        var row = new GameObject("ButtonsRow");
        row.transform.SetParent(box.transform, false);
        var rowRt = row.AddComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(0f, 52f);
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlHeight = true;
        hlg.childControlWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = false;

        CreateMenuButton(row.transform, "Exit", () => Application.Quit(), 140f);
        CreateMenuButton(row.transform, "Start", () =>
        {
            if (game_settings != null && _nicknameInput != null)
                game_settings.player_name = _nicknameInput.text;
            menu_state = MAIN_MENU;
            RefreshMenuUi();
        }, 140f);
    }

    void BuildMainMenuUi()
    {
        var panel = CreateFullStretchPanel(_canvasGo.transform, new Color(0.12f, 0.12f, 0.14f, 0.92f));
        var layoutGo = new GameObject("Buttons");
        layoutGo.transform.SetParent(panel.transform, false);
        var layoutRect = layoutGo.AddComponent<RectTransform>();
        layoutRect.anchorMin = new Vector2(0.5f, 0.5f);
        layoutRect.anchorMax = new Vector2(0.5f, 0.5f);
        layoutRect.pivot = new Vector2(0.5f, 0.5f);
        layoutRect.sizeDelta = new Vector2(480f, 220f);
        layoutRect.anchoredPosition = Vector2.zero;
        var vlg = layoutGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 16f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        CreateMenuButton(layoutGo.transform, "Local Play", OnLocalPlay);
        CreateMenuButton(layoutGo.transform, "Online Play", OnOnlinePlay);
    }

    void OnLocalPlay()
    {
        if (game_settings != null)
            game_settings.local_game = true;
        LoadIfSet(_localPlayScene);
    }

    void OnOnlinePlay()
    {
        if (game_settings != null)
            game_settings.local_game = false;
        LoadIfSet(_onlinePlayScene);
    }

    void LoadIfSet(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;
        SceneManager.LoadScene(sceneName);
    }

    static GameObject CreateFullStretchPanel(Transform parent, Color color)
    {
        var panel = new GameObject("Panel");
        panel.transform.SetParent(parent, false);
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = color;
        var panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        return panel;
    }

    static void CreateLabel(Transform parent, string label)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 28f;
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontSize = 22;
        text.font = ResolveUiFont();
    }

    InputField CreateInputField(Transform parent, string initial)
    {
        var root = new GameObject("InputField");
        root.transform.SetParent(parent, false);
        var bg = root.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.95f);
        var input = root.AddComponent<InputField>();
        input.characterLimit = STANDARD_MAX_CHARS;
        input.text = initial ?? "";

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(root.transform, false);
        var text = textGo.AddComponent<Text>();
        var textRt = (RectTransform)text.transform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10f, 6f);
        textRt.offsetMax = new Vector2(-10f, -6f);
        text.supportRichText = false;
        text.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        text.fontSize = 22;
        text.font = ResolveUiFont();

        var phGo = new GameObject("Placeholder");
        phGo.transform.SetParent(root.transform, false);
        var ph = phGo.AddComponent<Text>();
        var phRt = (RectTransform)ph.transform;
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = new Vector2(10f, 6f);
        phRt.offsetMax = new Vector2(-10f, -6f);
        ph.text = "Enter nickname";
        ph.color = new Color(0.4f, 0.4f, 0.4f, 0.9f);
        ph.fontSize = 22;
        ph.font = ResolveUiFont();

        input.textComponent = text;
        input.placeholder = ph;

        var rootRt = (RectTransform)root.transform;
        rootRt.anchorMin = new Vector2(0.5f, 0.5f);
        rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.sizeDelta = new Vector2(440f, 48f);

        return input;
    }

    static void CreateMenuButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, float width = 0f)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 72f;
        le.preferredHeight = 72f;
        if (width > 0f)
        {
            le.minWidth = width;
            le.preferredWidth = width;
        }
        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.45f, 0.85f, 1f);
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.55f, 0.95f, 1f);
        colors.pressedColor = new Color(0.2f, 0.35f, 0.7f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.AddComponent<Text>();
        var textRect = (RectTransform)text.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontSize = 26;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.font = ResolveUiFont();
    }

    static Font ResolveUiFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null)
            return f;
        f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (f != null)
            return f;
        return Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Helvetica" }, 16);
    }
}
