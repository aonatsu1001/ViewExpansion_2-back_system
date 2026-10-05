using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ロック解除タスク．エリアマップ（リング図＋HMD画面図，それぞれ8エリア分のアイコン）上で
// 対象エリアをハイライトし，8色パレットからのマウス回答を受け付ける．
// 正解の色を選ぶまでロックは解除されない（誤答時も1行ずつログを記録した上で再入力を待つ）．
public class LockTaskController : MonoBehaviour
{
    private static readonly Color InactiveIndicatorColor = new Color(0.28f, 0.28f, 0.32f);
    private static readonly Color ActiveIndicatorColor = new Color(0.9f, 0.1f, 0.1f);
    private static readonly Color DisabledScreenColor = new Color(0.04f, 0.04f, 0.05f, 1f);
    // 割合(%)ではなく絶対ピクセル量で伸び幅を揃える．リング図は内側/外側でセルの半径が
    // 大きく異なるため，同じ倍率を掛けると半径の小さいセルの伸びがほぼ見えなくなるため．
    private const float PulseGrowthPixels = 10f;
    private const float PulseSpeed = 2f;

    private TextMeshProUGUI instructionText;
    private List<Button> paletteButtons;
    // areaIndicatorGroups[areaId]には，そのエリアを表す全ての表示要素（リング図のセル，
    // HMD画面図のセルなど）が入る．ハイライト時はグループ内の全要素を同時に変化させる．
    private List<List<Image>> areaIndicatorGroups;
    // HMD画面図の左視野外(エリア1-4)／右視野外(エリア5-8)のセル．見るべきエリアが
    // 反対側にある間，使われていない側は非アクティブ（暗く）表示する．
    private List<Image> hmdLeftCells;
    private List<Image> hmdRightCells;
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
        foreach (var group in areaIndicatorGroups)
        {
            foreach (var indicator in group)
            {
                indicator.color = InactiveIndicatorColor;
            }
        }
    }

    // HMD画面図の左右セル一覧を登録する．BindAreaMapとは独立に呼び出してよい．
    public void BindHmdScreenSides(List<Image> leftCells, List<Image> rightCells)
    {
        hmdLeftCells = leftCells;
        hmdRightCells = rightCells;
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
                indicator.color = InactiveIndicatorColor;
                indicator.rectTransform.localScale = Vector3.one;
            }
        }

        // HMD画面図：見るべきエリアが左(0-3)なら右側グリッドを，右(4-7)なら左側グリッドを
        // 非アクティブ（暗く）表示する．上の全体リセットの後，対象ハイライトの前に適用する．
        if (hmdLeftCells != null && hmdRightCells != null)
        {
            bool isLeftArea = areaId >= 0 && areaId < 4;
            SetColor(isLeftArea ? hmdRightCells : hmdLeftCells, DisabledScreenColor);
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
                    indicator.color = InactiveIndicatorColor;
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
