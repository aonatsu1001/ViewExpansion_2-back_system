using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ロック解除タスク．エリアマップ（8エリア分のアイコン）上で対象エリアをハイライトし，
// 8色パレットからのマウス回答を受け付ける．
// 正解の色を選ぶまでロックは解除されない（誤答時も1行ずつログを記録した上で再入力を待つ）．
public class LockTaskController : MonoBehaviour
{
    private static readonly Color InactiveIndicatorColor = new Color(0.28f, 0.28f, 0.32f);
    private static readonly Color ActiveIndicatorColor = new Color(0.9f, 0.1f, 0.1f);
    private const float PulseScaleMax = 1.06f;
    private const float PulseSpeed = 2f;

    private TextMeshProUGUI instructionText;
    private List<Button> paletteButtons;
    private List<Image> areaIndicators;
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

    // areaIndicators[areaId]が，そのエリアを表すマップ上のアイコンに対応する．
    public void BindAreaMap(List<Image> areaIndicators)
    {
        this.areaIndicators = areaIndicators;
        foreach (var indicator in areaIndicators)
        {
            indicator.color = InactiveIndicatorColor;
        }
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
        if (areaIndicators == null) return;

        for (int i = 0; i < areaIndicators.Count; i++)
        {
            areaIndicators[i].color = InactiveIndicatorColor;
            areaIndicators[i].rectTransform.localScale = Vector3.one;
        }

        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        if (areaId >= 0 && areaId < areaIndicators.Count)
        {
            areaIndicators[areaId].color = ActiveIndicatorColor;
            pulseCoroutine = StartCoroutine(PulseIndicator(areaIndicators[areaId].rectTransform));
        }
    }

    // 正解するまで対象エリアが拡大縮小を繰り返すようにして注意を引く．
    private IEnumerator PulseIndicator(RectTransform target)
    {
        while (true)
        {
            float t = Mathf.PingPong(Time.time * PulseSpeed, 1f);
            float scale = Mathf.Lerp(1f, PulseScaleMax, t);
            target.localScale = Vector3.one * scale;
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
            if (areaIndicators != null && currentAreaId < areaIndicators.Count)
            {
                areaIndicators[currentAreaId].color = InactiveIndicatorColor;
                areaIndicators[currentAreaId].rectTransform.localScale = Vector3.one;
            }
            OnTrialComplete?.Invoke();
        }
        // 誤答の場合はパレットを有効なままにして，正解を選ぶまで再入力を待つ．
    }

    private void SetInteractable(bool value)
    {
        foreach (var b in paletteButtons) b.interactable = value;
    }
}
