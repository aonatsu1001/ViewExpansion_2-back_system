using UnityEngine;

// 「2-back 3回答ごとにロック発生」「8エリア完了で本試行終了」を管理する状態機械．
// 練習：2-back 1ブロック(3回答) → エリア確認×1 → 実験者操作待ち
// 本試行：(2-back 3回答 → ロック解除) を8エリア分繰り返して終了
// 2-back系列はロックをまたいで連続する（ロック前後で比較対象がリセットされない）．
public class ExperimentSessionController : MonoBehaviour
{
    [HideInInspector] public GameObject twoBackPanel;
    [HideInInspector] public GameObject lockPanel;
    [HideInInspector] public GameObject practiceTransitionPanel;
    [HideInInspector] public GameObject endPanel;

    [HideInInspector] public TwoBackTaskController twoBack;
    [HideInInspector] public LockTaskController lockTask;
    [HideInInspector] public SequenceSetLibrary sequenceLibrary;

    private const int PracticeLockCount = 1;

    private enum Phase
    {
        Idle,
        PracticeTwoBack,
        PracticeLock,
        WaitForMainStart,
        MainTwoBack,
        MainLock,
        Finished
    }

    private Phase phase = Phase.Idle;
    private int mainAreaIndex;
    private int practiceLockIndex;
    private SequenceSetLibrary.SequenceSet activeSequence;

    // フィールド代入完了後にBootstrapから明示的に呼び出す（Awake時点では
    // twoBack/lockTaskがまだ未代入のため，イベント購読はここで行う）．
    public void Init()
    {
        twoBack.OnBlockComplete += HandleTwoBackComplete;
        lockTask.OnTrialComplete += HandleLockComplete;

        twoBackPanel.SetActive(false);
        lockPanel.SetActive(false);
        practiceTransitionPanel.SetActive(false);
        endPanel.SetActive(false);
    }

    public void StartPractice()
    {
        activeSequence = sequenceLibrary.Get(ExperimentContext.SequenceSetIndex);

        phase = Phase.PracticeTwoBack;
        twoBack.ResetSequence();
        twoBackPanel.SetActive(true);
        twoBack.StartBlock(trialAreaIndex: -1, isPractice: true);
    }

    public void StartMainTrial()
    {
        practiceTransitionPanel.SetActive(false);
        mainAreaIndex = 0;
        phase = Phase.MainTwoBack;

        ExperimentLogger.Instance?.MarkMainSessionStart();
        twoBack.ResetSequence();
        twoBackPanel.SetActive(true);
        twoBack.StartBlock(mainAreaIndex, isPractice: false);
    }

    private void HandleTwoBackComplete()
    {
        if (phase == Phase.PracticeTwoBack)
        {
            twoBackPanel.SetActive(false);
            practiceLockIndex = 0;
            phase = Phase.PracticeLock;
            BeginNextPracticeLock();
        }
        else if (phase == Phase.MainTwoBack)
        {
            twoBackPanel.SetActive(false);
            phase = Phase.MainLock;
            lockPanel.SetActive(true);
            int areaId = activeSequence.areaVisitOrder[mainAreaIndex];
            lockTask.BeginTrial(mainAreaIndex, areaId, isPractice: false);
        }
    }

    private void BeginNextPracticeLock()
    {
        if (practiceLockIndex >= PracticeLockCount)
        {
            lockPanel.SetActive(false);
            ExperimentLogger.Instance?.FlushPractice();
            phase = Phase.WaitForMainStart;
            practiceTransitionPanel.SetActive(true);
            return;
        }

        lockPanel.SetActive(true);
        int areaId = activeSequence.areaVisitOrder[practiceLockIndex % activeSequence.areaVisitOrder.Length];
        lockTask.BeginTrial(practiceLockIndex, areaId, isPractice: true);
    }

    private void HandleLockComplete()
    {
        if (phase == Phase.PracticeLock)
        {
            lockPanel.SetActive(false);
            practiceLockIndex++;
            BeginNextPracticeLock();
        }
        else if (phase == Phase.MainLock)
        {
            lockPanel.SetActive(false);
            mainAreaIndex++;

            if (mainAreaIndex >= activeSequence.areaVisitOrder.Length)
            {
                FinishMain();
            }
            else
            {
                phase = Phase.MainTwoBack;
                twoBackPanel.SetActive(true);
                twoBack.StartBlock(mainAreaIndex, isPractice: false);
            }
        }
    }

    private void FinishMain()
    {
        phase = Phase.Finished;
        ExperimentLogger.Instance?.FlushMain();
        endPanel.SetActive(true);
    }
}
