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

        // areaIndicatorGroups[areaId]には，そのエリアを表す全ての表示要素が入る．
        // ロック時はグループ内の全要素を同時にハイライトする．
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

        // 左の図：左視野外（平行四辺形，エリア1-4）・正面（FOV）・右視野外（平行四辺形，エリア5-8）．
        // 暗くする処理は無し．
        var roomViewSize = BuildRoomViewDiagram(diagramsRow.transform, areaIndicatorGroups);
        // 右の図：HMD画面をそのまま模した図（コーナーに2x2グリッド）．見ていない側を暗くする．
        var hmdScreenSize = BuildHmdScreenDiagram(diagramsRow.transform, areaIndicatorGroups, lockTask);

        // 各図が実際に必要とするサイズの合計を確保する（固定値だと，図のサイズを変えたときに
        // 2つの図が重なって干渉してしまうため，必ずここで計算し直す）．
        diagramsLe.preferredWidth = roomViewSize.x + diagramsHl.spacing + hmdScreenSize.x;
        diagramsLe.preferredHeight = Mathf.Max(roomViewSize.y, hmdScreenSize.y);

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
                btn.gameObject.AddComponent<HoverHighlight>();
                buttons.Add(btn);
            }
        }

        lockTask.Bind(instruction, buttons, areaColorConfig);
        lockTask.BindAreaMap(areaIndicatorGroups);
    }

    // 添付の構成図通りの図．左視野外（平行四辺形，エリア1-4）・正面（FOVとだけ表示）・
    // 右視野外（平行四辺形，エリア5-8）を横並びに配置し，中央下に参加者（You）アイコンを置く．
    // 写真は使わず，番号付きの単色セルで表示する．見ていない側を暗くする処理は行わない
    // （暗くする処理は右のHMD画面図でのみ行う）．
    // 戻り値：この図が実際に必要とする(幅,高さ)．呼び出し側でDiagramsRow全体のサイズを
    // 過不足なく確保し，隣（HMD画面図）との重なりを防ぐために使う．
    private Vector2 BuildRoomViewDiagram(Transform parent, List<List<Image>> indicatorGroups)
    {
        const float windowWidth = 220f;
        const float centerWidth = 300f;
        const float panelHeight = 220f;
        const float panelSpacing = 28f;
        // 左右の辺（手前・奥）はどちらもこの比率の高さのまま＝長さが同じ．正面の辺と同じ長さに
        // なるよう，窓全体の高さから逆算する．
        const float sideHeightFraction = 0.7f;
        // 手前側ほど中心を上へ，奥側ほど下へずらす量．これにより上下の辺が斜め・平行になる．
        const float shearFraction = 0.3f;
        float windowHeight = panelHeight / sideHeightFraction;
        float rowHeight = Mathf.Max(windowHeight, panelHeight);
        // 左右の図をもう少し上にずらす．
        const float sideWindowVerticalOffset = 50f;
        float totalWidth = windowWidth * 2f + centerWidth + panelSpacing * 2f;
        float totalHeight = rowHeight + 110f;
        // 俯瞰図全体をもう少し上にずらす．
        const float diagramVerticalOffset = 35f;

        // HorizontalLayoutGroup（diagramsRow）の子は位置を自動制御されるため，位置を手動で
        // ずらせるよう，レイアウト対象の「スロット」と，実際に描画する「図」を分離する．
        var slot = new GameObject("RoomViewDiagramSlot", typeof(RectTransform));
        slot.transform.SetParent(parent, false);
        var slotLe = slot.AddComponent<LayoutElement>();
        slotLe.preferredWidth = totalWidth;
        slotLe.preferredHeight = totalHeight;

        var column = CreateMapChild(slot.transform, "RoomViewDiagram", new Vector2(0f, diagramVerticalOffset), new Vector2(totalWidth, totalHeight));
        var columnVl = column.gameObject.AddComponent<VerticalLayoutGroup>();
        columnVl.childAlignment = TextAnchor.MiddleCenter;
        columnVl.spacing = 16;
        columnVl.childControlWidth = true;
        columnVl.childControlHeight = true;
        columnVl.childForceExpandWidth = false;
        columnVl.childForceExpandHeight = false;

        var row = new GameObject("PanelsRow", typeof(RectTransform));
        row.transform.SetParent(column.transform, false);
        var rowHl = row.AddComponent<HorizontalLayoutGroup>();
        rowHl.spacing = panelSpacing;
        rowHl.childAlignment = TextAnchor.MiddleCenter;
        rowHl.childControlWidth = true;
        rowHl.childControlHeight = true;
        rowHl.childForceExpandWidth = false;
        rowHl.childForceExpandHeight = false;
        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredWidth = totalWidth;
        rowLe.preferredHeight = rowHeight;

        // 左視野外の窓（エリア1-4）．左右の辺は正面の辺と同じ長さのまま，上下の辺を斜め・平行にする．
        CreateParallelogramGrid(row.transform, windowWidth, windowHeight, rowHeight, sideWindowVerticalOffset, nearEdgeOnRight: true, sideHeightFraction, shearFraction, new[] { 0, 1, 2, 3 }, indicatorGroups);

        // 正面（"HMD Screen"とだけ表示，装飾のみ・ハイライト対象外）．
        var centerFrame = CreatePanelFrame(row.transform, centerWidth, panelHeight);
        var fovText = UIFactory.CreateText(centerFrame, "HMD Screen", 32, uiFont, new Color(0.8f, 0.8f, 0.8f));
        StretchLabel(fovText);

        // 右視野外の窓（エリア5-8）．左と左右対称．
        CreateParallelogramGrid(row.transform, windowWidth, windowHeight, rowHeight, sideWindowVerticalOffset, nearEdgeOnRight: false, sideHeightFraction, shearFraction, new[] { 4, 5, 6, 7 }, indicatorGroups);

        return new Vector2(totalWidth, totalHeight);
    }

    // HMD画面をそのまま模した図．画面の左上に左視野外映像（エリア1-4），右上に右視野外映像
    // （エリア5-8）の2x2グリッドを重ねて表示する．部屋ビュー図と同じareaIdをindicatorGroupsに
    // 登録することで，同じエリアを両方の図で同時にハイライトできる．
    // こちらの図でのみ，見ていない側を暗く表示する．
    // 戻り値：この図が実際に必要とする(幅,高さ)．呼び出し側でDiagramsRow全体のサイズを
    // 過不足なく確保し，隣（部屋ビュー図）との重なりを防ぐために使う．
    private Vector2 BuildHmdScreenDiagram(Transform parent, List<List<Image>> indicatorGroups, LockTaskController lockTask)
    {
        const float screenWidth = 480f;
        const float screenHeight = 300f;
        const float cellSize = 70f;
        const float cellSpacing = 8f;
        const float marginFromEdge = 16f;
        float columnWidth = screenWidth + 40f;
        float columnHeight = screenHeight + 80f;

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
        columnLe.preferredWidth = columnWidth;
        columnLe.preferredHeight = columnHeight;

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
        float halfGrid = (cellSize * 2f + cellSpacing) / 2f;

        // 縦位置はHMDスクリーンの中央（y=0）に揃え，横方向だけ左右に振り分ける．
        var leftGridCenter = new Vector2(-halfWidth + marginFromEdge + halfGrid, 0f);
        var rightGridCenter = new Vector2(halfWidth - marginFromEdge - halfGrid, 0f);

        var leftCells = CreatePeripheralGrid(inner, leftGridCenter, cellSize, cellSpacing, new[] { 0, 1, 2, 3 }, indicatorGroups);
        var rightCells = CreatePeripheralGrid(inner, rightGridCenter, cellSize, cellSpacing, new[] { 4, 5, 6, 7 }, indicatorGroups);

        // スクリーン図でのみ，見ていない側を暗くする．
        lockTask.BindHmdScreenSides(leftCells, rightCells);

        return new Vector2(columnWidth, columnHeight);
    }

    // centerPosを中心に，2x2のエリアアイコン（番号付き単色セル）をグリッド状に配置し，
    // 生成したセルの一覧を返す．areaIdsTLTRBLBRは左上・右上・左下・右下の順でareaIdを指定する．
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

    // エリアアイコン用の単色セル（クリック不可）．エリア番号は表示しない
    // （labelはGameObject名の識別にのみ使用）．
    private Image CreateScreenCell(Transform parent, string label, Vector2 size)
    {
        var go = new GameObject(label + "Cell", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = size.x;
        le.preferredHeight = size.y;

        go.GetComponent<Image>().color = Color.white;

        return go.GetComponent<Image>();
    }

    // 固定サイズの縁取り付きパネル（フレーム＋内側コンテンツ領域）を作り，内側のTransformを返す．
    private Transform CreatePanelFrame(Transform parent, float width, float height)
    {
        var frame = new GameObject("PanelFrame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(parent, false);
        frame.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
        var le = frame.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = height;
        frame.GetComponent<Image>().color = new Color(0.55f, 0.55f, 0.6f);

        var inner = CreateMapChild(frame.transform, "Content", Vector2.zero, new Vector2(width - 8f, height - 8f));
        inner.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f);

        return inner;
    }

    // widthxheightの平行四辺形の窓（左右の辺の長さは不変，上下の辺だけ斜め・互いに平行）の中に，
    // 中心の縦線（左右分割）と，上下の辺に平行な斜めの境界線（上下分割）の2本で4分割した，
    // 番号付きの単色セルを配置する（areaIdsTLTRBLBRは左上・右上・左下・右下の順）．
    // 各セルは縦横ともに窓いっぱいの大きさのまま作り，外枠の斜め境界＋斜めの分割線の判定を
    // 1枚のマスクスプライトで表現する（セル自体の矩形を上下半分に区切ってしまうと，分割線が
    // 斜めにずれている部分でどちらのセルにも属さない隙間ができてしまうため，セルの矩形は
    // あえて窓いっぱいのままにし，マスクの判定だけで4分割を表現している）．
    // 分割線を見やすくするため，中心の縦線と斜めの境界線を実際に描画する．
    // verticalOffsetで窓全体を上下にずらせる（正面の図の位置に視覚的に合わせるため）．
    // 戻り値は生成した4つのセル（ハイライト対象）．
    private List<Image> CreateParallelogramGrid(Transform parent, float width, float height, float slotHeight, float verticalOffset, bool nearEdgeOnRight, float heightFraction, float shearFraction, int[] areaIdsTLTRBLBR, List<List<Image>> indicatorGroups)
    {
        // Horizontal/VerticalLayoutGroupの子は位置を自動制御されるため，位置を手動でずらせるよう
        // レイアウト対象の「スロット」と，実際に描画する「窓」を分離する．
        var slot = new GameObject("WindowSlot", typeof(RectTransform));
        slot.transform.SetParent(parent, false);
        var slotLe = slot.AddComponent<LayoutElement>();
        slotLe.preferredWidth = width;
        slotLe.preferredHeight = slotHeight;

        var window = CreateMapChild(slot.transform, "ParallelogramWindow", new Vector2(0f, -verticalOffset), new Vector2(width, height));

        var cells = new List<Image>();

        var cellTL = CreateParallelogramQuadrantCell(window, width, height, nearEdgeOnRight, heightFraction, shearFraction, isLeftHalf: true, isTopHalf: true, (areaIdsTLTRBLBR[0] + 1).ToString());
        indicatorGroups[areaIdsTLTRBLBR[0]].Add(cellTL);
        cells.Add(cellTL);

        var cellTR = CreateParallelogramQuadrantCell(window, width, height, nearEdgeOnRight, heightFraction, shearFraction, isLeftHalf: false, isTopHalf: true, (areaIdsTLTRBLBR[1] + 1).ToString());
        indicatorGroups[areaIdsTLTRBLBR[1]].Add(cellTR);
        cells.Add(cellTR);

        var cellBL = CreateParallelogramQuadrantCell(window, width, height, nearEdgeOnRight, heightFraction, shearFraction, isLeftHalf: true, isTopHalf: false, (areaIdsTLTRBLBR[2] + 1).ToString());
        indicatorGroups[areaIdsTLTRBLBR[2]].Add(cellBL);
        cells.Add(cellBL);

        var cellBR = CreateParallelogramQuadrantCell(window, width, height, nearEdgeOnRight, heightFraction, shearFraction, isLeftHalf: false, isTopHalf: false, (areaIdsTLTRBLBR[3] + 1).ToString());
        indicatorGroups[areaIdsTLTRBLBR[3]].Add(cellBR);
        cells.Add(cellBR);

        // 中心の縦線（左右分割）．窓の中心を通る，まっすぐな縦線．
        var vLine = CreateMapChild(window, "DividerVertical", Vector2.zero, new Vector2(3f, height));
        vLine.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f);

        // 斜めの境界線（上下分割）．窓の上下の辺と同じ傾きで，窓の中心を通る．
        float slope = (shearFraction * height) / width;
        float signedSlope = nearEdgeOnRight ? slope : -slope;
        float angleDeg = Mathf.Atan(signedSlope) * Mathf.Rad2Deg;
        float lineLength = width / Mathf.Cos(angleDeg * Mathf.Deg2Rad) * 1.05f; // 端まで届くよう少し余裕を持たせる
        var dLine = CreateMapChild(window, "DividerDiagonal", Vector2.zero, new Vector2(lineLength, 3f));
        dLine.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
        dLine.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f);

        return cells;
    }

    // 窓(windowWidth x windowHeight)のうち，isLeftHalf（中心の縦線による左右分割）と
    // isTopHalf（斜めの境界線による上下分割）で指定される1/4だけを示すセルを配置する．
    // セル自体の矩形は窓いっぱいの大きさのまま作り，マスクのアルファ判定だけで該当する1/4を表示する．
    // エリア番号は表示しない（labelはGameObject名の識別にのみ使用）．
    private Image CreateParallelogramQuadrantCell(Transform parent, float windowWidth, float windowHeight, bool nearEdgeOnRight, float heightFraction, float shearFraction, bool isLeftHalf, bool isTopHalf, string label)
    {
        var cellRect = CreateMapChild(parent, label + "Cell", Vector2.zero, new Vector2(windowWidth, windowHeight));

        var maskImg = cellRect.gameObject.AddComponent<Image>();
        maskImg.sprite = CreateParallelogramQuadrantSprite(nearEdgeOnRight, heightFraction, shearFraction, isLeftHalf, isTopHalf);
        maskImg.color = Color.white;
        var mask = cellRect.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true; // マスク画像自体がそのままセルの見た目（非選択時は白）になる

        return maskImg;
    }

    // 1/4セル用のアルファマスクを生成する．外枠の斜め境界判定（nearEdgeOnRight／heightFraction／
    // shearFraction，窓全体と同じ計算式）と，中心の斜め境界線による上下判定の両方をこの1枚に
    // まとめることで，セルの矩形自体を分割しなくても正しく1/4だけを表示できる．
    private Sprite CreateParallelogramQuadrantSprite(bool nearEdgeOnRight, float heightFraction, float shearFraction, bool isLeftHalf, bool isTopHalf)
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int px = 0; px < size; px++)
        {
            float xNorm = (px + 0.5f) / size; // 窓全体でのx(0〜1)
            bool inLeftHalf = xNorm < 0.5f;

            float t = nearEdgeOnRight ? xNorm : (1f - xNorm); // 0=奥側，1=手前（near）側
            float center = 0.5f + (t - 0.5f) * shearFraction;

            for (int py = 0; py < size; py++)
            {
                float yNorm = (py + 0.5f) / size; // 窓全体でのy(0〜1)

                bool insideOuter = Mathf.Abs(yNorm - center) <= heightFraction / 2f;
                bool insideHalf = isTopHalf ? (yNorm >= center) : (yNorm < center);
                bool insideSide = isLeftHalf ? inLeftHalf : !inLeftHalf;
                bool inside = insideOuter && insideHalf && insideSide;

                tex.SetPixel(px, py, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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
