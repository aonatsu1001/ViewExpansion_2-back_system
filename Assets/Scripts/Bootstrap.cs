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
        var digitText = UIFactory.CreateText(layout, "0", 220, uiFont);
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
        hlLe.preferredWidth = 760;
        hlLe.preferredHeight = 150;

        var sameBtn = UIFactory.CreateButton(buttonRow.transform, "Same", uiFont, null, new Vector2(340, 150), new Color(0.2f, 0.6f, 0.3f), fontSize: 48);
        var diffBtn = UIFactory.CreateButton(buttonRow.transform, "Different", uiFont, null, new Vector2(340, 150), new Color(0.7f, 0.3f, 0.2f), fontSize: 48);

        twoBack.Bind(digitText, sameBtn, diffBtn);
    }

    private void BuildLockPanel(Transform parent, LockTaskController lockTask)
    {
        var layout = CreateVerticalLayout(parent);

        var instruction = UIFactory.CreateText(layout, "", 32, uiFont);

        // areaIndicatorGroups[areaId]には，そのエリアを表す全ての図（俯瞰図＋HMD画面図）の
        // セルが入る．ロック時はグループ内の全セルを同時にハイライトする．
        var areaIndicatorGroups = new List<List<Image>>();
        for (int i = 0; i < 8; i++) areaIndicatorGroups.Add(new List<Image>());

        var diagramsRow = new GameObject("Diagrams", typeof(RectTransform));
        diagramsRow.transform.SetParent(layout, false);
        var diagramsHl = diagramsRow.AddComponent<HorizontalLayoutGroup>();
        diagramsHl.spacing = 40;
        diagramsHl.childAlignment = TextAnchor.MiddleCenter;
        diagramsHl.childForceExpandWidth = false;
        diagramsHl.childForceExpandHeight = false;
        diagramsHl.childControlWidth = true;
        diagramsHl.childControlHeight = true;
        var diagramsLe = diagramsRow.AddComponent<LayoutElement>();
        diagramsLe.preferredWidth = 420 + 40 + 520;
        diagramsLe.preferredHeight = 520;

        BuildAreaSpatialMap(diagramsRow.transform, areaIndicatorGroups);
        BuildHmdScreenDiagram(diagramsRow.transform, areaIndicatorGroups, lockTask);

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
                var btn = UIFactory.CreateButton(grid.transform, p.colorName, uiFont, null, new Vector2(180, 100), p.color, Color.black, showLabel: false);
                buttons.Add(btn);
            }
        }

        lockTask.Bind(instruction, buttons, areaColorConfig);
        lockTask.BindAreaMap(areaIndicatorGroups);
    }

    // 「You」を中心としたドーナツ状の俯瞰図．正面は広い1つの扇形（視野内），
    // 左右はそれぞれ内側/外側の2バンド×上/下の2半分＝計4セルの扇形（視野外）とする．
    // 扇形はUnity標準のImage Radial Fill（実行時生成したリング画像）で描画するため，
    // 独自メッシュに依存せず確実に表示される．
    // 角度は数学の慣例通り，0°=右，90°=真上（＝正面），180°=左とする．
    // 描画順：扇形（塗り）→境界線（扇形の上）→ラベル（一番上，常に読める）→中心のYou円．
    private void BuildAreaSpatialMap(Transform parent, List<List<Image>> indicatorGroups)
    {
        const float youRadius = 47f;
        const float innerBandOuterRadius = 127f;
        const float outerBandOuterRadius = 210f;
        const float forwardHalfWidth = 40f; // 正面の扇は幅80°（50°〜130°）
        const float sideHalfWidth = 70f;    // 左右の合計角度（140°）の半分．隙間を含めた枠の大きさ
        const float backGapHalfWidth = 32f; // Youの真後ろ（270°）に設ける隙間の半分（合計32°空ける）
        // 隙間の分を上下で均等に差し引き，8エリアすべてを同じ幅にする．
        const float cellWidth = sideHalfWidth - backGapHalfWidth / 2f;

        // 各セル位置に実際に割り当てるareaId（0始まり，表示番号は+1）．
        // 位置：Outer=Youから遠い／Inner=近い，Upper=正面寄り／Lower=背面寄り．
        const int leftOuterUpperAreaId = 1;  // 表示「2」
        const int leftInnerUpperAreaId = 3;  // 表示「4」
        const int leftOuterLowerAreaId = 0;  // 表示「1」
        const int leftInnerLowerAreaId = 2;  // 表示「3」
        const int rightOuterUpperAreaId = 4; // 表示「5」
        const int rightInnerUpperAreaId = 6; // 表示「7」
        const int rightOuterLowerAreaId = 5; // 表示「6」
        const int rightInnerLowerAreaId = 7; // 表示「8」

        var mapContainer = new GameObject("SpatialMap", typeof(RectTransform));
        mapContainer.transform.SetParent(parent, false);
        var containerSize = new Vector2(outerBandOuterRadius * 2f, outerBandOuterRadius * 2f);
        var containerRect = mapContainer.GetComponent<RectTransform>();
        containerRect.sizeDelta = containerSize;
        var containerLe = mapContainer.AddComponent<LayoutElement>();
        containerLe.preferredWidth = containerSize.x;
        containerLe.preferredHeight = containerSize.y;

        var forwardRingSprite = CreateRingSprite(youRadius / outerBandOuterRadius);
        var innerBandRingSprite = CreateRingSprite(youRadius / innerBandOuterRadius);
        var outerBandRingSprite = CreateRingSprite(innerBandOuterRadius / outerBandOuterRadius);

        var forwardColor = new Color(0.3f, 0.6f, 1.0f, 0.95f);
        var peripheralDefaultColor = new Color(0.5f, 0.5f, 0.55f, 0.9f);
        var boundaryColor = new Color(0.05f, 0.05f, 0.07f, 1f);

        float leftStart = 90f + forwardHalfWidth;
        float leftUpperStart = leftStart;
        float leftLowerStart = leftStart + cellWidth;
        float rightStart = 90f - forwardHalfWidth - sideHalfWidth * 2f;
        float rightLowerStart = rightStart + backGapHalfWidth;
        float rightUpperStart = rightLowerStart + cellWidth;

        // 1. 扇形（塗り）．正面＋左右各4セル．8セルはすべて同じ幅(cellWidth)．
        // 左のLowerセルの終端から右のLowerセルの始端までが隙間（backGapHalfWidth×2）になる．
        CreateRingWedge(mapContainer.transform, forwardRingSprite, outerBandOuterRadius * 2f, 90f - forwardHalfWidth, forwardHalfWidth * 2f, forwardColor);

        indicatorGroups[leftOuterUpperAreaId].Add(CreateRingWedge(mapContainer.transform, outerBandRingSprite, outerBandOuterRadius * 2f, leftUpperStart, cellWidth, peripheralDefaultColor));
        indicatorGroups[leftInnerUpperAreaId].Add(CreateRingWedge(mapContainer.transform, innerBandRingSprite, innerBandOuterRadius * 2f, leftUpperStart, cellWidth, peripheralDefaultColor));
        indicatorGroups[leftOuterLowerAreaId].Add(CreateRingWedge(mapContainer.transform, outerBandRingSprite, outerBandOuterRadius * 2f, leftLowerStart, cellWidth, peripheralDefaultColor));
        indicatorGroups[leftInnerLowerAreaId].Add(CreateRingWedge(mapContainer.transform, innerBandRingSprite, innerBandOuterRadius * 2f, leftLowerStart, cellWidth, peripheralDefaultColor));

        indicatorGroups[rightOuterUpperAreaId].Add(CreateRingWedge(mapContainer.transform, outerBandRingSprite, outerBandOuterRadius * 2f, rightUpperStart, cellWidth, peripheralDefaultColor));
        indicatorGroups[rightInnerUpperAreaId].Add(CreateRingWedge(mapContainer.transform, innerBandRingSprite, innerBandOuterRadius * 2f, rightUpperStart, cellWidth, peripheralDefaultColor));
        indicatorGroups[rightOuterLowerAreaId].Add(CreateRingWedge(mapContainer.transform, outerBandRingSprite, outerBandOuterRadius * 2f, rightLowerStart, cellWidth, peripheralDefaultColor));
        indicatorGroups[rightInnerLowerAreaId].Add(CreateRingWedge(mapContainer.transform, innerBandRingSprite, innerBandOuterRadius * 2f, rightLowerStart, cellWidth, peripheralDefaultColor));

        // 2. 境界線．放射状の直線（4本：正面と左右，左右それぞれの上下境界）と，
        // 内側/外側バンドの境界を示す円弧（左右それぞれ，背面の隙間を避けて描く）．扇形の上に重ねて描く．
        // 背面（270°）には隙間があるため，その位置には境界線を引かない．
        const float lineThickness = 5f;
        foreach (var angle in new[] { 90f - forwardHalfWidth, 90f + forwardHalfWidth, leftLowerStart, rightUpperStart })
        {
            CreateRadialBoundaryLine(mapContainer.transform, angle, youRadius, outerBandOuterRadius, lineThickness, boundaryColor);
        }

        const float bandBoundaryHalfThickness = 4f;
        float bandBoundaryDiameter = (innerBandOuterRadius + bandBoundaryHalfThickness) * 2f;
        var bandBoundarySprite = CreateRingSprite((innerBandOuterRadius - bandBoundaryHalfThickness) / (innerBandOuterRadius + bandBoundaryHalfThickness));
        CreateRingWedge(mapContainer.transform, bandBoundarySprite, bandBoundaryDiameter, leftStart, cellWidth * 2f, boundaryColor);
        CreateRingWedge(mapContainer.transform, bandBoundarySprite, bandBoundaryDiameter, rightLowerStart, cellWidth * 2f, boundaryColor);

        // 3. ラベル．境界線よりさらに上に描くことで，線に隠れず常に読めるようにする．
        // 表示番号は必ず「そのセルに実際に割り当てたareaId+1」から生成し，表示と実データがずれないようにする．
        // HMD画面図のセル番号（26pt）に合わせる．
        const int labelFontSize = 26;
        var labelHolderSize = new Vector2(70, 50);

        CreateWedgeLabel(mapContainer.transform, "Forward", (youRadius + outerBandOuterRadius) / 2f, 90f, labelFontSize, new Vector2(140, 50));

        float innerLabelRadius = (youRadius + innerBandOuterRadius) / 2f;
        float outerLabelRadius = (innerBandOuterRadius + outerBandOuterRadius) / 2f;

        CreateWedgeLabel(mapContainer.transform, (leftOuterUpperAreaId + 1).ToString(), outerLabelRadius, leftUpperStart + cellWidth / 2f, labelFontSize, labelHolderSize);
        CreateWedgeLabel(mapContainer.transform, (leftInnerUpperAreaId + 1).ToString(), innerLabelRadius, leftUpperStart + cellWidth / 2f, labelFontSize, labelHolderSize);
        CreateWedgeLabel(mapContainer.transform, (leftOuterLowerAreaId + 1).ToString(), outerLabelRadius, leftLowerStart + cellWidth / 2f, labelFontSize, labelHolderSize);
        CreateWedgeLabel(mapContainer.transform, (leftInnerLowerAreaId + 1).ToString(), innerLabelRadius, leftLowerStart + cellWidth / 2f, labelFontSize, labelHolderSize);
        CreateWedgeLabel(mapContainer.transform, (rightOuterUpperAreaId + 1).ToString(), outerLabelRadius, rightUpperStart + cellWidth / 2f, labelFontSize, labelHolderSize);
        CreateWedgeLabel(mapContainer.transform, (rightInnerUpperAreaId + 1).ToString(), innerLabelRadius, rightUpperStart + cellWidth / 2f, labelFontSize, labelHolderSize);
        CreateWedgeLabel(mapContainer.transform, (rightOuterLowerAreaId + 1).ToString(), outerLabelRadius, rightLowerStart + cellWidth / 2f, labelFontSize, labelHolderSize);
        CreateWedgeLabel(mapContainer.transform, (rightInnerLowerAreaId + 1).ToString(), innerLabelRadius, rightLowerStart + cellWidth / 2f, labelFontSize, labelHolderSize);

        // 4. 参加者（You）アイコン．周囲の扇形に合わせて円形にする（ラベルより上＝最前面）．
        var head = CreateMapChild(mapContainer.transform, "Head", Vector2.zero, new Vector2(youRadius * 2f, youRadius * 2f));
        var headImg = head.gameObject.AddComponent<Image>();
        headImg.sprite = CreateRingSprite(0f);
        headImg.color = new Color(0.85f, 0.85f, 0.85f);
        StretchLabel(UIFactory.CreateText(head, "You", 24, uiFont, Color.black));
    }

    // fromRadiusからtoRadiusまで，angleDeg方向に伸びる直線境界線を描画する．
    private void CreateRadialBoundaryLine(Transform parent, float angleDeg, float fromRadius, float toRadius, float thickness, Color color)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        var from = dir * fromRadius;
        var to = dir * toRadius;
        var mid = (from + to) / 2f;

        var rect = CreateMapChild(parent, "Boundary", mid, new Vector2(toRadius - fromRadius, thickness));
        rect.localRotation = Quaternion.Euler(0f, 0f, angleDeg);

        var img = rect.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    // 中心が半径innerNormalized(0〜1)〜1.0の範囲だけ不透明なリング（ドーナツ）状のスプライトを
    // 実行時に生成する．innerNormalized=0なら塗りつぶした円になる．
    // Image.Type.Filled + Radial360と組み合わせることで扇形の一部だけを表示できる．
    private Sprite CreateRingSprite(float innerNormalized)
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / maxDist;
                bool inside = dist >= innerNormalized && dist <= 1f;
                tex.SetPixel(x, y, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // ringSpriteをRadial360で扇形に切り出して表示する．startAngleDegから反時計回りにwidthDeg分だけ塗りつぶす．
    private Image CreateRingWedge(Transform parent, Sprite ringSprite, float diameter, float startAngleDeg, float widthDeg, Color color)
    {
        var rect = CreateMapChild(parent, "Wedge", Vector2.zero, new Vector2(diameter, diameter));

        var img = rect.gameObject.AddComponent<Image>();
        img.sprite = ringSprite;
        img.color = color;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = (int)Image.Origin360.Top;
        img.fillClockwise = false;
        img.fillAmount = Mathf.Clamp01(widthDeg / 360f);
        img.raycastTarget = false;

        // fillOrigin=Top（ローカルの真上＝90°）を起点に反時計回りで塗るため，
        // 起点をstartAngleDegに合わせるにはオブジェクト自体を(startAngleDeg-90)度回転させる．
        rect.localRotation = Quaternion.Euler(0f, 0f, startAngleDeg - 90f);

        return img;
    }

    // 俯瞰図内で絶対配置する子要素を作る（中心ピボット，anchoredPositionで配置）．
    private RectTransform CreateMapChild(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPos;

        return rect;
    }

    // 極座標(radius, angleDeg)の位置にラベルを配置する．
    private void CreateWedgeLabel(Transform parent, string label, float radius, float angleDeg, int fontSize, Vector2 holderSize)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        var pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;

        var holder = CreateMapChild(parent, "Label", pos, holderSize);
        var text = UIFactory.CreateText(holder, label, fontSize, uiFont, Color.white);
        StretchLabel(text);
    }

    // ラベル用テキストを親いっぱいに引き伸ばす（CreateTextが付与するLayoutElementは，
    // 絶対配置のコンテナ内では不要かつ無害だが，見た目のため破棄しておく）．
    private void StretchLabel(TextMeshProUGUI text)
    {
        var le = text.GetComponent<LayoutElement>();
        if (le != null) Object.Destroy(le);

        var rect = text.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // HMD画面をそのまま模した図．画面の左上に左視野外映像（エリア1-4），右上に右視野外映像
    // （エリア5-8）の2x2グリッドを重ねて表示する．俯瞰図（リング）と同じareaIdを
    // indicatorGroupsに登録することで，同じエリアを両方の図で同時にハイライトできる．
    private void BuildHmdScreenDiagram(Transform parent, List<List<Image>> indicatorGroups, LockTaskController lockTask)
    {
        const float screenWidth = 480f;
        const float screenHeight = 300f;
        const float cellSize = 70f;
        const float cellSpacing = 8f;
        const float marginFromEdge = 16f;

        var column = new GameObject("HmdScreenDiagram", typeof(RectTransform));
        column.transform.SetParent(parent, false);
        var vl = column.AddComponent<VerticalLayoutGroup>();
        vl.childAlignment = TextAnchor.MiddleCenter;
        vl.spacing = 16;
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = false;
        vl.childForceExpandHeight = false;
        var columnLe = column.AddComponent<LayoutElement>();
        columnLe.preferredWidth = screenWidth + 40f;
        columnLe.preferredHeight = screenHeight + 80f;

        UIFactory.CreateText(column.transform, "HMD Screen", 24, uiFont, new Color(0.8f, 0.8f, 0.8f));

        var frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(column.transform, false);
        frame.GetComponent<RectTransform>().sizeDelta = new Vector2(screenWidth, screenHeight);
        var frameLe = frame.AddComponent<LayoutElement>();
        frameLe.preferredWidth = screenWidth;
        frameLe.preferredHeight = screenHeight;
        frame.GetComponent<Image>().color = new Color(0.55f, 0.55f, 0.6f);

        var inner = CreateMapChild(frame.transform, "Content", Vector2.zero, new Vector2(screenWidth - 12f, screenHeight - 12f));
        inner.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f);

        float halfWidth = (screenWidth - 12f) / 2f;
        float halfHeight = (screenHeight - 12f) / 2f;
        float halfGrid = (cellSize * 2f + cellSpacing) / 2f;

        // 縦位置はHMDスクリーンの中央（y=0）に揃え，横方向だけ左右に振り分ける．
        var leftGridCenter = new Vector2(-halfWidth + marginFromEdge + halfGrid, 0f);
        var rightGridCenter = new Vector2(halfWidth - marginFromEdge - halfGrid, 0f);

        // 左視野外の映像（エリア1-4）：左上・右上・左下・右下の順．
        var leftCells = CreatePeripheralGrid(inner, leftGridCenter, cellSize, cellSpacing, new[] { 0, 1, 2, 3 }, indicatorGroups);
        // 右視野外の映像（エリア5-8）．左右の対応について指定が無かったため，左と同じ並び順で仮に割り当てる．
        var rightCells = CreatePeripheralGrid(inner, rightGridCenter, cellSize, cellSpacing, new[] { 4, 5, 6, 7 }, indicatorGroups);

        // 見るべきエリアが左右どちらかにある間，反対側のグリッドは非アクティブ（暗く）表示する．
        lockTask.BindHmdScreenSides(leftCells, rightCells);
    }

    // centerPosを中心に，2x2のエリアアイコンをグリッド状に配置し，生成したセルの一覧を返す．
    // areaIdsTLTRBLBRは左上・右上・左下・右下の順でareaIdを指定する．
    private List<Image> CreatePeripheralGrid(Transform parent, Vector2 centerPos, float cellSize, float spacing, int[] areaIdsTLTRBLBR, List<List<Image>> indicatorGroups)
    {
        float gridSize = cellSize * 2f + spacing;
        var gridHolder = CreateMapChild(parent, "PeripheralGrid", centerPos, new Vector2(gridSize, gridSize));

        var gl = gridHolder.gameObject.AddComponent<GridLayoutGroup>();
        gl.cellSize = new Vector2(cellSize, cellSize);
        gl.spacing = new Vector2(spacing, spacing);
        gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gl.constraintCount = 2;

        var cells = new List<Image>();
        for (int slot = 0; slot < areaIdsTLTRBLBR.Length; slot++)
        {
            int areaId = areaIdsTLTRBLBR[slot];
            var cell = CreateScreenCell(gridHolder.transform, (areaId + 1).ToString(), new Vector2(cellSize, cellSize));
            indicatorGroups[areaId].Add(cell);
            cells.Add(cell);
        }
        return cells;
    }

    // HMD画面図用の単純な番号付きセル（クリック不可）．
    private Image CreateScreenCell(Transform parent, string label, Vector2 size)
    {
        var go = new GameObject(label + "Cell", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = size.x;
        le.preferredHeight = size.y;

        var img = go.GetComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.35f);

        var text = UIFactory.CreateText(go.transform, label, 26, uiFont, Color.white);
        StretchLabel(text);

        return img;
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
