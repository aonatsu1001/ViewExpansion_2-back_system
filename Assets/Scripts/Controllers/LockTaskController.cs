using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ロック解除タスク．エリア番号を提示し，8色パレットからのマウス回答を受け付ける．
// 正解の色を選ぶまでロックは解除されない（誤答時も1行ずつログを記録した上で再入力を待つ）．
public class LockTaskController : MonoBehaviour
{
    private TextMeshProUGUI instructionText;
    private List<Button> paletteButtons;
    private AreaColorConfig colorConfig;

    private int currentTrialIndex;
    private int currentAreaId;
    private int currentColorTarget;
    private bool isPractice;
    private double onsetRealtime;
    private long onsetMs;

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

    public void BeginTrial(int trialIndex, int areaId, bool isPractice)
    {
        currentTrialIndex = trialIndex;
        currentAreaId = areaId;
        this.isPractice = isPractice;
        currentColorTarget = colorConfig.GetCorrectColorIndex(areaId);

        instructionText.text = $"Please check Area {areaId + 1}";
        onsetRealtime = Time.realtimeSinceStartupAsDouble;
        onsetMs = Clock.NowMs();

        SetInteractable(true);
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
            OnTrialComplete?.Invoke();
        }
        // 誤答の場合はパレットを有効なままにして，正解を選ぶまで再入力を待つ．
    }

    private void SetInteractable(bool value)
    {
        foreach (var b in paletteButtons) b.interactable = value;
    }
}
