using System;

public enum ExperimentCondition
{
    Proposed,
    Baseline
}

// 起動画面で設定された参加者ID・条件・エリア順序セットを
// アプリ全体で共有するための静的コンテキスト
public static class ExperimentContext
{
    public static string ParticipantId { get; private set; } = "P000";
    public static ExperimentCondition Condition { get; private set; } = ExperimentCondition.Proposed;
    public static int SequenceSetIndex { get; private set; } = 0;
    public static DateTime SessionDate { get; private set; } = DateTime.Now;

    public static void Configure(string participantId, ExperimentCondition condition, int sequenceSetIndex)
    {
        ParticipantId = string.IsNullOrWhiteSpace(participantId) ? "P000" : participantId.Trim();
        Condition = condition;
        SequenceSetIndex = sequenceSetIndex;
        SessionDate = DateTime.Now;
    }

    public static string ConditionKey => Condition == ExperimentCondition.Proposed ? "proposed" : "baseline";

    public static string DateKey => SessionDate.ToString("yyyyMMdd");
}
