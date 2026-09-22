using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// シーンに1つだけ配置するエントリポイント．実行時にUI階層一式と
// コントローラを組み立てて配線する（Editor上での手動シーン編集は不要）．
// 配置は Assets/Editor/TwoBackSetupMenu.cs のメニューから行う．
public class Bootstrap : MonoBehaviour
{
    [Header("Config Assets")]
    public AreaColorConfig areaColorConfig;
    public SequenceSetLibrary sequenceSetLibrary;

    [Header("Optional custom font. Leave empty to use TMP's default (Liberation Sans SDF).")]
    public TMP_FontAsset uiFont;

    [Header("Task difficulty: N-back distance (1 = 1-back, 2 = 2-back, ...)")]
    public int nBackDistance = 1;

    private void Awake()
    {
        UIFactory.EnsureEventSystem();
        var canvas = UIFactory.CreateRootCanvas();
        var root = canvas.transform;

        if (ExperimentLogger.Instance == null)
        {
            new GameObject("ExperimentLogger").AddComponent<ExperimentLogger>();
        }

        var setupPanel = UIFactory.CreatePanel(root, "SetupPanel");
        var twoBackPanel = UIFactory.CreatePanel(root, "TwoBackPanel");
        var lockPanel = UIFactory.CreatePanel(root, "LockPanel");
        var practiceTransitionPanel = UIFactory.CreatePanel(root, "PracticeTransitionPanel");
        var endPanel = UIFactory.CreatePanel(root, "EndPanel");

        var session = new GameObject("ExperimentSessionController").AddComponent<ExperimentSessionController>();
        var twoBack = new GameObject("TwoBackTaskController").AddComponent<TwoBackTaskController>();
        twoBack.NBackDistance = Mathf.Max(1, nBackDistance);
        var lockTask = new GameObject("LockTaskController").AddComponent<LockTaskController>();

        BuildSetupPanel(setupPanel.transform, session);
        BuildTwoBackPanel(twoBackPanel.transform, twoBack);
        BuildLockPanel(lockPanel.transform, lockTask);
        BuildPracticeTransitionPanel(practiceTransitionPanel.transform, session);
        BuildEndPanel(endPanel.transform);

        session.twoBackPanel = twoBackPanel;
        session.lockPanel = lockPanel;
        session.practiceTransitionPanel = practiceTransitionPanel;
        session.endPanel = endPanel;
        session.twoBack = twoBack;
        session.lockTask = lockTask;
        session.sequenceLibrary = sequenceSetLibrary;
        session.Init();

        setupPanel.SetActive(true);
        twoBackPanel.SetActive(false);
        lockPanel.SetActive(false);
        practiceTransitionPanel.SetActive(false);
        endPanel.SetActive(false);
    }

    private Transform CreateVerticalLayout(Transform parent)
    {
        var go = new GameObject("Content", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var vl = go.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment = TextAnchor.MiddleCenter;
        vl.spacing = 24;
        vl.childForceExpandWidth = false;
        vl.childForceExpandHeight = false;
        vl.childControlWidth = true;
        vl.childControlHeight = true;

        return go.transform;
    }

    private SelectorControl CreateSelector(Transform parent)
    {
        var row = new GameObject("Selector", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        var hl = row.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.spacing = 10;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;
        hl.childControlWidth = true;
        hl.childControlHeight = true;

        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredWidth = 420;
        rowLe.preferredHeight = 60;

        var prev = UIFactory.CreateButton(row.transform, "<", uiFont, null, new Vector2(60, 60));

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(row.transform, false);
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 26;
        label.color = Color.white;
        if (uiFont != null) label.font = uiFont;
        var labelLe = labelGo.AddComponent<LayoutElement>();
        labelLe.preferredWidth = 260;
        labelLe.preferredHeight = 60;

        var next = UIFactory.CreateButton(row.transform, ">", uiFont, null, new Vector2(60, 60));

        var selector = row.AddComponent<SelectorControl>();
        selector.label = label;
        selector.prevButton = prev;
        selector.nextButton = next;
        return selector;
    }

    private void BuildSetupPanel(Transform parent, ExperimentSessionController session)
    {
        var layout = CreateVerticalLayout(parent);

        UIFactory.CreateText(layout, $"{Mathf.Max(1, nBackDistance)}-back Task System - Setup", 40, uiFont);
        UIFactory.CreateText(layout, "Participant ID", 26, uiFont);
        var idInput = UIFactory.CreateInputField(layout, "e.g. P001", uiFont, new Vector2(400, 60));

        UIFactory.CreateText(layout, "Condition", 26, uiFont);
        var conditionSelector = CreateSelector(layout);

        UIFactory.CreateText(layout, "Area Sequence Set", 26, uiFont);
        var sequenceSelector = CreateSelector(layout);

        var controller = new GameObject("ExperimentSetupController").AddComponent<ExperimentSetupController>();
        controller.participantIdInput = idInput;
        controller.conditionSelector = conditionSelector;
        controller.sequenceSetSelector = sequenceSelector;
        controller.setupPanel = parent.gameObject;
        controller.session = session;
        controller.sequenceLibrary = sequenceSetLibrary;
        controller.PopulateOptions();

        UIFactory.CreateButton(layout, "Start Practice", uiFont, () => controller.OnStartPressed(), new Vector2(300, 80), new Color(0.2f, 0.55f, 0.3f));
    }

    private void BuildTwoBackPanel(Transform parent, TwoBackTaskController twoBack)
    {
        var layout = CreateVerticalLayout(parent);

        UIFactory.CreateText(layout, $"{Mathf.Max(1, nBackDistance)}-back Task", 32, uiFont, new Color(0.8f, 0.8f, 0.8f));
        // "0"などの1桁プレースホルダで初期化しておくことで，最初の数字表示時に
        // 空文字→1桁の文字数変化によるレイアウトの揺れが起きないようにする．
        var digitText = UIFactory.CreateText(layout, "0", 140, uiFont);
        digitText.alpha = 0f;

        var buttonRow = new GameObject("ButtonRow", typeof(RectTransform));
        buttonRow.transform.SetParent(layout, false);
        var hl = buttonRow.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 40;
        hl.childAlignment = TextAnchor.MiddleCenter;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        var hlLe = buttonRow.AddComponent<LayoutElement>();
        hlLe.preferredWidth = 700;
        hlLe.preferredHeight = 120;

        var sameBtn = UIFactory.CreateButton(buttonRow.transform, "Same", uiFont, null, new Vector2(300, 120), new Color(0.2f, 0.6f, 0.3f));
        var diffBtn = UIFactory.CreateButton(buttonRow.transform, "Different", uiFont, null, new Vector2(300, 120), new Color(0.7f, 0.3f, 0.2f));

        twoBack.Bind(digitText, sameBtn, diffBtn);
    }

    private void BuildLockPanel(Transform parent, LockTaskController lockTask)
    {
        var layout = CreateVerticalLayout(parent);

        var instruction = UIFactory.CreateText(layout, "", 36, uiFont);

        var grid = new GameObject("PaletteGrid", typeof(RectTransform));
        grid.transform.SetParent(layout, false);
        var gridLayout = grid.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(180, 100);
        gridLayout.spacing = new Vector2(20, 20);
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        var gridLe = grid.AddComponent<LayoutElement>();
        gridLe.preferredWidth = 820;
        gridLe.preferredHeight = 220;

        var buttons = new List<Button>();
        if (areaColorConfig != null && areaColorConfig.palette != null)
        {
            foreach (var p in areaColorConfig.palette)
            {
                var btn = UIFactory.CreateButton(grid.transform, p.colorName, uiFont, null, new Vector2(180, 100), p.color, Color.black);
                buttons.Add(btn);
            }
        }

        lockTask.Bind(instruction, buttons, areaColorConfig);
    }

    private void BuildPracticeTransitionPanel(Transform parent, ExperimentSessionController session)
    {
        var layout = CreateVerticalLayout(parent);

        UIFactory.CreateText(layout, "Practice Complete", 40, uiFont);
        UIFactory.CreateText(layout, "Once you've confirmed the participant understands the task, start the main trial.", 26, uiFont);
        UIFactory.CreateButton(layout, "Start Main Trial", uiFont, () => session.StartMainTrial(), new Vector2(320, 90), new Color(0.2f, 0.55f, 0.3f));
    }

    private void BuildEndPanel(Transform parent)
    {
        var layout = CreateVerticalLayout(parent);

        UIFactory.CreateText(layout, "Experiment Complete", 44, uiFont);
        UIFactory.CreateText(layout, "Logs have been saved.", 28, uiFont);
    }
}
