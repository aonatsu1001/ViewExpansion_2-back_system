using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// N-back課題本体（N=NBackDistance，既定は1-back．旧2-back実装のNを可変にしたもの）．
// 数字列はロックをまたいでも連続しており，比較対象は常に
// 「ロック前も含めたNBackDistance個前に提示された数字」になる（エリアごとに系列がリセットされない）．
// セッション（練習／本試行）の最初のNBackDistance桁のみ比較対象が無く回答不可のため1〜2秒間隔で提示，
// それ以降は回答可能で，マウスボタン（同じ／異なる）で回答した時点で次の数字へ進む
// （自己ペース＝マルチタスク全体のスループットを所要時間に反映させるため，固定間隔にはしない．
// タイムアウトは設けず，回答があるまで無期限に待つ）．
// 1ブロックにつき3回答．
public class TwoBackTaskController : MonoBehaviour
{
    private const int AnswersPerBlock = 3;
    private const float MinIsiSeconds = 1f;
    private const float MaxIsiSeconds = 2f;
    private const float PostAnswerPauseSeconds = 0.3f;
    // 同じ数字が連続した際に「クリックが反映されず数字が変わっていない」のか
    // 「同じ数字が続けて提示された」のか区別できるよう，提示ごとに一瞬空白を挟む．
    private const float InterStimulusBlankSeconds = 0.2f;

    // Bootstrapから難易度調整用に設定される（1=1-back，2=2-back，…）．StartBlock呼び出し前に設定すること．
    public int NBackDistance = 1;

    private TextMeshProUGUI digitText;
    private Button sameButton;
    private Button differentButton;

    // セッション（練習／本試行）を通して連続する数字列．ResetSequence()で初期化される．
    private readonly List<int> fullSequence = new List<int>();

    private int currentIndex;
    private int trialAreaIndex;
    private bool isPractice;
    private bool waitingForAnswer;
    private bool answeredThisTrial;
    private double digitOnsetRealtime;

    public event Action OnBlockComplete;

    public void Bind(TextMeshProUGUI digitText, Button sameButton, Button differentButton)
    {
        this.digitText = digitText;
        this.sameButton = sameButton;
        this.differentButton = differentButton;

        sameButton.onClick.AddListener(() => SubmitAnswer(true));
        differentButton.onClick.AddListener(() => SubmitAnswer(false));
    }

    // 練習開始・本試行開始のタイミングで呼び出し，系列を断ち切る．
    public void ResetSequence()
    {
        fullSequence.Clear();
    }

    public void StartBlock(int trialAreaIndex, bool isPractice)
    {
        this.trialAreaIndex = trialAreaIndex;
        this.isPractice = isPractice;

        // 系列が空（このセッションの最初のブロック）の場合のみ，比較対象を持たない
        // 先頭NBackDistance桁を追加してから回答可能な3桁を続ける．2回目以降のブロックは
        // 前のブロック（ロック前）の末尾NBackDistance桁がそのまま比較対象になるため3桁のみ追加する．
        int startIndex = fullSequence.Count;
        int newDigitsCount = startIndex == 0 ? AnswersPerBlock + NBackDistance : AnswersPerBlock;
        AppendDigits(newDigitsCount);

        SetAnswerButtonsInteractable(false);

        StopAllCoroutines();
        StartCoroutine(RunSequence(startIndex, newDigitsCount));
    }

    private void AppendDigits(int count)
    {
        var rng = new System.Random();

        for (int k = 0; k < count; k++)
        {
            int i = fullSequence.Count;
            if (i >= NBackDistance && rng.NextDouble() < 0.4)
            {
                fullSequence.Add(fullSequence[i - NBackDistance]);
            }
            else
            {
                int d;
                int guard = 0;
                do
                {
                    d = rng.Next(1, 10);
                    guard++;
                } while (i >= NBackDistance && d == fullSequence[i - NBackDistance] && guard < 10);
                fullSequence.Add(d);
            }
        }
    }

    private IEnumerator RunSequence(int startIndex, int count)
    {
        for (int offset = 0; offset < count; offset++)
        {
            currentIndex = startIndex + offset;

            // text ("")を変更するとTMPの行高さが変わりレイアウトが揺れるため，
            // 文字列は常に1桁の数字のまま保ち，alphaだけで見た目上消す．
            digitText.alpha = 0f;
            yield return new WaitForSeconds(InterStimulusBlankSeconds);
            digitText.text = fullSequence[currentIndex].ToString();
            digitText.alpha = 1f;

            bool answerable = currentIndex >= NBackDistance;
            waitingForAnswer = answerable;
            answeredThisTrial = false;
            digitOnsetRealtime = Time.realtimeSinceStartupAsDouble;
            SetAnswerButtonsInteractable(answerable);

            if (!answerable)
            {
                float isi = UnityEngine.Random.Range(MinIsiSeconds, MaxIsiSeconds);
                yield return new WaitForSeconds(isi);
            }
            else
            {
                while (!answeredThisTrial)
                {
                    yield return null;
                }

                yield return new WaitForSeconds(PostAnswerPauseSeconds);
            }

            waitingForAnswer = false;
            SetAnswerButtonsInteractable(false);
        }

        digitText.alpha = 0f;
        OnBlockComplete?.Invoke();
    }

    private void SubmitAnswer(bool sameAnswer)
    {
        if (!waitingForAnswer || answeredThisTrial) return;
        answeredThisTrial = true;

        long rtMs = (long)((Time.realtimeSinceStartupAsDouble - digitOnsetRealtime) * 1000);
        int digit = fullSequence[currentIndex];
        int target = fullSequence[currentIndex - NBackDistance];
        bool actualSame = digit == target;
        bool correct = actualSame == sameAnswer;

        ExperimentLogger.Instance?.LogTwoBack(isPractice, Clock.NowMs(), digit, target, sameAnswer ? "Same" : "Different", correct, rtMs, trialAreaIndex);
    }

    private void SetAnswerButtonsInteractable(bool value)
    {
        sameButton.interactable = value;
        differentButton.interactable = value;
    }
}
