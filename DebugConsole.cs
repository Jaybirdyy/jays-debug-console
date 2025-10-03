using System.Collections.Concurrent;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System;
using TMPro;

class DebugMod : GameModification
{
    Harmony _harmony;

    public DebugMod(Mod p_mod) : base(p_mod)
    {
        Debug.Log($"Registering mod: {p_mod.name}");
    }

    public override void OnModInitialization(Mod p_mod)
    {
        Debug.Log($"Initializing mod: {p_mod.name}");

        mod = p_mod;

        PatchGame();
    }

    public override void OnModUnloaded()
    {
        Debug.Log($"Unloading mod: {mod.name}");

        _harmony?.UnpatchAll(_harmony.Id);
    }

    void PatchGame()
    {
        Debug.Log($"Applying patch from mod: {mod.name}");

        _harmony = new Harmony("com.hexofsteel." + mod.name);
        _harmony.PatchAll();
         DebugConsoleBootstrap.EnsureExists();
    }
}
    [HarmonyPatch(typeof(MainMenu))]
    static class MainMenu_Awake
    {
        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        static void Postfix()
        {
            DebugConsoleBootstrap.EnsureExists();
        }
    }

    public class DebugConsoleBootstrap : MonoBehaviour
    {
        private static bool _created;

        public static void EnsureExists()
        {
            if (_created) return;
            var go = new GameObject("RuntimeDebugConsoleHost");
            DontDestroyOnLoad(go);
            go.AddComponent<RuntimeDebugConsole>(); // console creation
            _created = true;
        }
    }


    public class RuntimeDebugConsole : MonoBehaviour
    {
        private Button _autoScrollBtn;
        private Image _autoScrollImg;
        private Canvas _canvas;
        private ScrollRect _scrollRect;
        private RectTransform _contentRT;
        private TMP_Text _text;
        private Button _clearBtn;
        private Button _jmpBtn;
        //Buffering
    private readonly ConcurrentQueue<(string msg, string stack, LogType type)> _queue
        = new ConcurrentQueue<(string, string, LogType)>();
        private readonly StringBuilder _sb = new StringBuilder(8 * 1024);

        //Settings, character maximum 
        private const int MaxChars = 200_000;
        private bool _autoScroll = true;

    private void OnEnable()
    {
            Application.logMessageReceivedThreaded += OnLogMessage;
          //  Debug.Log("[DebugConsole] OnEnable fired");
        }

    private void OnDisable()
    {
        Application.logMessageReceivedThreaded -= OnLogMessage;
        }

    private void Awake()
    {
        BuildUI();
        //    Debug.Log("[DebugConsole] Awake fired");
        }


        private void Update()
        {
         
        // Debug.Log("[DebugConsole] Update ran");

        {
         if (UnityEngine.Input.GetKeyDown(KeyCode.F1) || UnityEngine.Input.GetKeyDown(KeyCode.BackQuote))
        {
                if (_canvas != null)
                {
                    _canvas.gameObject.SetActive(!_canvas.gameObject.activeSelf);
         //Debug.Log("[DebugConsole] F1 pressed → toggling console looooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooong");
           //Debug.Log("[DebugConsole] F1 pressed → toggling console ");
           
        }
         }
    }       // dequeue that shit
            bool hadAny = false;
            while (_queue.TryDequeue(out var entry))
            {
                hadAny = true;
                AppendLine(entry.msg, entry.stack, entry.type);
            }

            if (hadAny)
            {
                // Trim if too long
                if (_sb.Length > MaxChars)
                {
                    //Remove oldest half
                    int cut = _sb.Length - (MaxChars / 2);
                    _sb.Remove(0, cut);
                }

                setText(_sb.ToString());
                if (_autoScroll) ScrollToBottom();
            }
        }

        private void OnLogMessage(String condition, string stackTrace, LogType type)
        {
            _queue.Enqueue((condition, stackTrace, type));
        }

        private void AppendLine(string msg, string stack, LogType type)
        {
            string prefix;
            switch (type)
            {
                case LogType.Warning: prefix = "<color=#E6C200>[WARN]</color> "; break;
                case LogType.Error: prefix = "<color=#FF5555>[ERROR]</color> "; break;
                case LogType.Exception: prefix = "<color=#FF2222>[EXC]</color> "; break;
                case LogType.Assert: prefix = "<color=#FF8888>[ASSERT]</color> "; break;
                default: prefix = "<color=#A0A0A0>[LOG]</color> "; break;
            }

            _sb.Append(prefix).Append(msg).Append('\n');
            //stack trace included with errors
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                if (!string.IsNullOrEmpty(stack))
                {
                    _sb.Append("<color=#888888>")
                       .Append(stack.Replace("\n", "\n   "))
                       .Append("</color>\n");
                }
            }
        }
    private void setText(string s)
{
    _text.text = s;

    Canvas.ForceUpdateCanvases();
    LayoutRebuilder.ForceRebuildLayoutImmediate(_text.rectTransform);

    float h = _text.preferredHeight;
    if (h < 10f) h = 10f;
    var rt = _text.rectTransform;
    rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);

    if (_autoScroll)
        _scrollRect.verticalNormalizedPosition = 0f;
}
     
        private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = 0f;
    }
    // Creating the UI -------------------------------------------------------------------------------------------------------- 
    private void BuildUI()
    {
        // Canvas generation
        var canvasGO = new GameObject("DebugConsoleCanvas");
        canvasGO.transform.SetParent(transform);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 5000; // on top
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Panel - as it stands, 25% of the screen is taken
        var panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGO.transform, false);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.75f);
        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0f, 0f);
        panelRT.anchorMax = new Vector2(1f, 0.25f); // 25% height
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        //constants
        const float sideBarWidth = 120f;
        const float pad = 2f;

        // Scroll view
        var scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(panel.transform, false);
        _scrollRect = scrollGO.AddComponent<ScrollRect>();
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0f, 0f);
        scrollRT.anchorMax = new Vector2(1f, 1f);
        scrollRT.offsetMin = new Vector2(pad, pad); 
        scrollRT.offsetMax = new Vector2(-(sideBarWidth + 2f * pad), -pad); // leave room for side bar

        // Viewport
        var viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollGO.transform, false);
        var viewportRT = viewportGO.AddComponent<RectTransform>();
        viewportRT.anchorMin = new Vector2(0, 0);
        viewportRT.anchorMax = new Vector2(1, 1);
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;
        viewportGO.AddComponent<RectMask2D>();

        // Text

        var textGO = new GameObject("TMP_Text");
        textGO.transform.SetParent(viewportGO.transform, false);
        var tmp = textGO.AddComponent<TextMeshProUGUI>();   // ** using TextMeshPro, make sure to import TMPro **
        tmp.font = TMP_Settings.defaultFontAsset;           // ensure a font
        tmp.enableWordWrapping = true;                      // wrap long lines
        tmp.richText = true;
        tmp.color = Color.white;
        tmp.fontSize = 8;                                  // smaller font
        tmp.alignment = TextAlignmentOptions.TopLeft;       // top-left alignment
        _text = tmp;
        var textRT = tmp.rectTransform;

        // Anchor text to top, stretch width
        textRT.anchorMin = new Vector2(0, 1);
        textRT.anchorMax = new Vector2(1, 1);
        textRT.pivot = new Vector2(0.5f, 1);
        textRT.offsetMin = new Vector2(8, 8);
        textRT.offsetMax = new Vector2(-8, 0);

        // Let TEXT drive its own height
        var fitter = textGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Connecting scrollrect to everything
        _scrollRect.viewport = viewportRT;
        _scrollRect.content = textRT;
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.inertia = true;

        // Fix to adjust character height always equating to 0, don't copy this lel
        _contentRT = textRT;

        // Side bar, autoscroll and clear
        var sideBar = new GameObject("sideBar");
        sideBar.transform.SetParent(panel.transform, false);
        var sidebarImg = sideBar.AddComponent<Image>(); sidebarImg.color = new Color(0f, 0f, 0f, 0.35f);
        var sideBarRT = sideBar.GetComponent<RectTransform>();
        sideBarRT.anchorMin = new Vector2(1f, 0f);
        sideBarRT.anchorMax = new Vector2(1, 1);
        sideBarRT.offsetMin = new Vector2(-sideBarWidth - pad, pad); //left and buttom
        sideBarRT.offsetMax = new Vector2(-pad, -pad); // right and teop

        var verticalBar = sideBar.AddComponent<VerticalLayoutGroup>();
        verticalBar.childAlignment = TextAnchor.UpperCenter;
        verticalBar.childControlHeight = false;
        verticalBar.childControlWidth = true;
        verticalBar.childForceExpandWidth = true;
        verticalBar.childForceExpandHeight = false;
        verticalBar.spacing = 6f;
        verticalBar.padding = new RectOffset(6, 6, 6, 6);

        _clearBtn = CreateButton(sideBar.transform, "Clear", () =>
        {
            _sb.Length = 0;
            setText("");
        }).Item1;


        _jmpBtn = CreateButton(sideBar.transform, "Jump to Bottom", () =>
        {
            Canvas.ForceUpdateCanvases(); _scrollRect.verticalNormalizedPosition = 0f;
        }).Item1;

        (_autoScrollBtn, _autoScrollImg) = CreateButton(sideBar.transform, "AutoScroll", () =>
        {
            _autoScroll = !_autoScroll;
            UpdateAutoScrollVisual(); //calls to update the autoscroll button color
        });
         UpdateAutoScrollVisual();

        // Final layout
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(textRT);

        _canvas.gameObject.SetActive(false);
    }

        private void UpdateAutoScrollVisual() // autoscroll button
{
        if (_autoScrollImg == null) return;

        if (_autoScroll)
    {
        _autoScrollImg.color = new Color(0.2f, 0.6f, 0.2f, 0.9f); // green-ish for enabled
    }
        else
    {
        _autoScrollImg.color = new Color(0.6f, 0.2f, 0.2f, 0.9f); // red-ish for disabled
    }
}

       private (Button, Image) CreateButton(Transform parent, string label, Action onClick)
    {
        var go = new GameObject(label + "Button");
        go.transform.SetParent(parent, false);

        // background
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(60, 20);  
        rt.anchorMin = new Vector2(1, 1);     // top-right anchor
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);         // pivot also top-right
        rt.sizeDelta = new Vector2(0, 22); //for now, this forces the height to be 22 pxs. if this becomes an issue on higher/lower monitors, i could setup a proper ui scaler

        var preferredLayout = go.AddComponent<LayoutElement>();
        preferredLayout.preferredHeight = 22;
        preferredLayout.minHeight = 20;

        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());

        // label
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);

        var txt = textGO.AddComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 8;
        txt.alignment = TextAlignmentOptions.Center;
        txt.rectTransform.anchorMin = Vector2.zero;
        txt.rectTransform.anchorMax = Vector2.one;
        txt.rectTransform.offsetMin = Vector2.zero;
        txt.rectTransform.offsetMax = Vector2.zero;
        return (btn, img);
    }

    private static string StripRichTextIfNeeded(string s) => s; // or do stripping if desired

    }
