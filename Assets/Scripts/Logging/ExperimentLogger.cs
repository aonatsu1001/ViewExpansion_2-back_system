using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// lock_log.csv / twoback_log.csv / session.csv の出力を担当．
// 練習セッションと本試行セッションのログは完全に分離したバッファに蓄積し，
// それぞれの完了時にまとめてファイル出力する．
public class ExperimentLogger : MonoBehaviour
{
    public static ExperimentLogger Instance { get; private set; }

    [Tooltip("空欄の場合はexe横（エディタではプロジェクト直下）のLogsフォルダに出力する")]
    [SerializeField] private string outputDirectoryOverride = "";

    private const string TwoBackHeader = "Timestamp,Digit,TargetDigit,UserAnswer,Correct,ReactionTimeMs,TrialAreaIndex";
    private const string LockHeader = "TrialIndex,AreaId,TargetColorIndex,AnsweredColorIndex,Correct,LockOnsetTimestampMs,AnswerTimestampMs,ResponseTimeMs";
    private const string SessionHeader = "SessionStartMs,SessionEndMs,TotalDurationMs";

    private readonly List<string> practiceTwoBackLines = new List<string> { TwoBackHeader };
    private readonly List<string> practiceLockLines = new List<string> { LockHeader };

    private readonly List<string> mainTwoBackLines = new List<string> { TwoBackHeader };
    private readonly List<string> mainLockLines = new List<string> { LockHeader };
    private readonly List<string> mainSessionLines = new List<string> { SessionHeader };

    private long mainSessionStartMs;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LogTwoBack(bool isPractice, long timestampMs, int digit, int targetDigit, string userAnswer, bool correct, long reactionTimeMs, int trialAreaIndex)
    {
        string line = $"{timestampMs},{digit},{targetDigit},{userAnswer},{correct},{reactionTimeMs},{trialAreaIndex}";
        (isPractice ? practiceTwoBackLines : mainTwoBackLines).Add(line);
    }

    public void LogLock(bool isPractice, int trialIndex, int areaId, int targetColorIndex, int answeredColorIndex, bool correct, long lockOnsetTimestampMs, long answerTimestampMs, long responseTimeMs)
    {
        string line = $"{trialIndex},{areaId},{targetColorIndex},{answeredColorIndex},{correct},{lockOnsetTimestampMs},{answerTimestampMs},{responseTimeMs}";
        (isPractice ? practiceLockLines : mainLockLines).Add(line);
    }

    public void MarkMainSessionStart()
    {
        mainSessionStartMs = Clock.NowMs();
    }

    public void FlushPractice()
    {
        WriteFile("practice_twoback", practiceTwoBackLines);
        WriteFile("practice_lock", practiceLockLines);
    }

    public void FlushMain()
    {
        long endMs = Clock.NowMs();
        mainSessionLines.Add($"{mainSessionStartMs},{endMs},{endMs - mainSessionStartMs}");

        WriteFile("twoback", mainTwoBackLines);
        WriteFile("lock", mainLockLines);
        WriteFile("session", mainSessionLines);
    }

    private void WriteFile(string logType, List<string> lines)
    {
        string dir = GetOutputDirectory();
        try
        {
            Directory.CreateDirectory(dir);
            string fileName = $"{ExperimentContext.ParticipantId}_{ExperimentContext.ConditionKey}_{ExperimentContext.DateKey}_{logType}.csv";
            string path = Path.Combine(dir, fileName);
            File.WriteAllLines(path, lines, new UTF8Encoding(true));
            Debug.Log($"[ExperimentLogger] wrote {path}");
        }
        catch (IOException e)
        {
            Debug.LogError($"[ExperimentLogger] ログ出力に失敗しました: {e.Message}");
        }
    }

    private string GetOutputDirectory()
    {
        if (!string.IsNullOrWhiteSpace(outputDirectoryOverride))
        {
            return outputDirectoryOverride;
        }
        // Application.dataPath はエディタ実行時はプロジェクト直下/Assets，
        // ビルド後は "ExeName_Data" を指すため，その親フォルダ（exe横／プロジェクト直下）に出力する．
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
    }
}
