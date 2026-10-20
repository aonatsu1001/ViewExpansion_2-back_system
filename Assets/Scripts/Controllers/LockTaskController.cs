using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ロック解除タスク．エリアマップ（部屋ビュー図＋HMD画面図，それぞれ8エリア分のアイコン）上で
// 対象エリアをハイライトし，8色パレットからのマウス回答を受け付ける．
// 正解の色を選ぶまでロックは解除されない（誤答時も1行ずつログを記録した上で再入力を待つ）．
public class LockTaskController : MonoBehaviour
{
    private static readonly Color ActiveIndicatorColor = new Color(0.9f, 0.1f, 0.1f);
    // スクリーン図（HMD Screen）でのみ使用．注視されていない側を暗く見せる．
    private static readonly Color DisabledScreenColor = new Color(0.08f, 0.08f, 0.09f, 1f);
    // 割合(%)ではなく絶対ピクセル量で伸び幅を揃える．図によってセルの大きさが
    // 大きく異なるため，同じ倍率を掛けると小さいセルの伸びがほぼ見えなくなるため．
    private const float PulseGrowthPixels = 10f;
    private const float PulseSpeed = 2f;

    private TextMeshProUGUI instructionText;
    private List<Button> paletteButtons;
    // areaIndicatorGroups[areaId]には，そのエリアを表す全ての表示要素（複数の図にまたがってもよい）が入る．
    // ハイライト時はグループ内の全要素を同時に変化させる．
    private List<List<Image>> areaIndicatorGroups;
    // HMD画面図（スクリーン図）の左視野外(エリア1-4)／右視野外(エリア5-8)のセルのみ．
    // 見るべきエリアが反対側にある間，こちらだけ非アクティブ（暗く）表示する．
    // 部屋ビュー図（写真）は対象外．
    private List<Image> hmdScreenLeftCells;
    private List<Image> hmdScreenRightCells;
    // 各表示要素の非選択時の色（図ごとに作成時点で設定された色）．グローバルな単一色に
    // 固定すると，写真の上に重ねる半透明ハイライトのように「非選択時は完全に透明にしたい」
    // 図があっても対応できないため，要素ごとに記録しておく．
    private readonly Dictionary<Image, Color> indicatorBaseColors = new Dictionary<Image, Color>();
    private AreaColorConfig colorConfig;

    private int currentTrialIndex;
    private int currentAreaId;
    private int currentColorTarget;
    private bool isPractice;
    private double onsetRealtime;
    private long onsetMs;
    private Coroutine pulseCoroutine;

    public event Action OnTrialComplete;

    public void Bind(TextMeshProUGUI instructionText, List<Button> paletteButtons, AreaColorConfig colorConfig)
    {
        this.instructionText = instructionText;
        this.paletteButtons = paletteButtons;
        this.colorConfig = colorConfig;

        for (int i = 0; i < paletteButtons.Count; i++)
        {
            int idx = i;
            paletteButtons[i].onClick.AddListener(() => SubmitAnswer(idx));
        }
    }

    // areaIndicatorGroups[areaId]が，そのエリアを表す全ての表示要素（複数の図にまたがってもよい）に対応する．
    public void BindAreaMap(List<List<Image>> areaIndicatorGroups)
    {
        this.areaIndicatorGroups = areaIndicatorGroups;
        indicatorBaseColors.Clear();
        foreach (var group in areaIndicatorGroups)
        {
            foreach (var indicator in group)
            {
                // 作成時点の色（図ごとに異なる）をそのまま非選択時の色として記録する．
                indicatorBaseColors[indicator] = indicator.color;
            }
        }
    }

    // HMD画面図（スクリーン図）の左右セル一覧を登録する．BindAreaMapとは独立に呼び出してよい．
    public void BindHmdScreenSides(List<Image> leftCells, List<Image> rightCells)
    {
        hmdScreenLeftCells = leftCells;
        hmdScreenRightCells = rightCells;
    }

    public void BeginTrial(int trialIndex, int areaId, bool isPractice)
    {
        currentTrialIndex = trialIndex;
        currentAreaId = areaId;
        this.isPractice = isPractice;
        currentColorTarget = colorConfig.GetCorrectColorIndex(areaId);

        instructionText.text = "Check the highlighted area, then select its color below";
        HighlightArea(areaId);

        onsetRealtime = Time.realtimeSinceStartupAsDouble;
        onsetMs = Clock.NowMs();

        SetInteractable(true);
    }

    private void HighlightArea(int areaId)
    {
        if (areaIndicatorGroups == null) return;

        foreach (var group in areaIndicatorGroups)
        {
            foreach (var indicator in group)
            {
                indicator.color = indicatorBaseColors[indicator];
                indicator.rectTransform.localScale = Vector3.one;
            }
        }

        // スクリーン図のみ：見るべきエリアが左(0-3)なら右側，右(4-7)なら左側を暗くする．
        if (hmdScreenLeftCells != null && hmdScreenRightCells != null)
        {
            bool isLeftArea = areaId >= 0 && areaId < 4;
            SetColor(isLeftArea ? hmdScreenRightCells : hmdScreenLeftCells, DisabledScreenColor);
        }

        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        if (areaId >= 0 && areaId < areaIndicatorGroups.Count)
        {
            foreach (var indicator in areaIndicatorGroups[areaId])
            {
                indicator.color = ActiveIndicatorColor;
            }
            pulseCoroutine = StartCoroutine(PulseIndicatorGroup(areaIndicatorGroups[areaId]));
        }
    }

    // 正解するまで対象エリアの全表示要素が拡大縮小を繰り返すようにして注意を引く．
    // 各要素ごとの実寸（半径）からその場で必要な倍率を計算し，見た目の伸び幅（ピクセル数）を揃える．
    private IEnumerator PulseIndicatorGroup(List<Image> group)
    {
        while (true)
        {
            float t = Mathf.PingPong(Time.time * PulseSpeed, 1f);
            foreach (var indicator in group)
            {
                float halfSize = indicator.rectTransform.sizeDelta.x / 2f;
                float maxScale = halfSize > 0f ? 1f + PulseGrowthPixels / halfSize : 1f;
                float scale = Mathf.Lerp(1f, maxScale, t);
                indicator.rectTransform.localScale = Vector3.one * scale;
            }
            yield return null;
        }
    }

    private void SubmitAnswer(int answeredColorIndex)
    {
        long answerMs = Clock.NowMs();
        long rtMs = (long)((Time.realtimeSinceStartupAsDouble - onsetRealtime) * 1000);
        bool correct = answeredColorIndex == currentColorTarget;

        ExperimentLogger.Instance?.LogLock(isPractice, currentTrialIndex, currentAreaId, currentColorTarget, answeredColorIndex, correct, onsetMs, answerMs, rtMs);

        if (correct)
        {
            SetInteractable(false);
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            if (areaIndicatorGroups != null && currentAreaId < areaIndicatorGroups.Count)
            {
                foreach (var indicator in areaIndicatorGroups[currentAreaId])
                {
                    indicator.color = indicatorBaseColors[indicator];
                    indicator.rectTransform.localScale = Vector3.one;
                }
            }
            OnTrialComplete?.Invoke();
        }
        // 誤答の場合はパレットを有効なままにして，正解を選ぶまで再入力を待つ．
    }

    private void SetInteractable(bool value)
    {
        foreach (var b in paletteButtons) b.interactable = value;
    }

    private static void SetColor(List<Image> images, Color color)
    {
        foreach (var image in images) image.color = color;
    }
}
