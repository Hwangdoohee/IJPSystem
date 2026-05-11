using System.Collections.Generic;

namespace IJPSystem.Platform.Application.Sequences
{
    /// <summary>사용 가능한 모든 시퀀스 목록을 제공한다.</summary>
    /// <remarks>
    /// NameKey/DescriptionKey 는 번역 키 (Seq_*_Name, Seq_*_Desc).
    /// HMI 의 SequenceVM 이 ctor 에서 Loc.T 로 번역해 Name/Description 에 채워 넣음.
    /// </remarks>
    public static class SequenceRegistry
    {
        public static IReadOnlyList<SequenceDefinition> GetAll() => new[]
        {
            new SequenceDefinition
            {
                Id             = "INIT",
                Icon           = "🏠",
                NameKey        = "Seq_Init_Name",
                DescriptionKey = "Seq_Init_Desc",
                BuildSteps     = InitializeSequence.Build,
            },
            new SequenceDefinition
            {
                Id             = "PURGE",
                Icon           = "💧",
                NameKey        = "Seq_Purge_Name",
                DescriptionKey = "Seq_Purge_Desc",
                BuildSteps     = PurgeSequence.Build,
            },
            new SequenceDefinition
            {
                Id             = "BLOTTING",
                Icon           = "🩹",
                NameKey        = "Seq_Blotting_Name",
                DescriptionKey = "Seq_Blotting_Desc",
                BuildSteps     = BlottingSequence.Build,
            },
            new SequenceDefinition
            {
                Id             = "MAINTENANCE",
                Icon           = "🔧",
                NameKey        = "Seq_Maintenance_Name",
                DescriptionKey = "Seq_Maintenance_Desc",
                BuildSteps     = MaintenanceSequence.Build,
            },
            new SequenceDefinition
            {
                Id             = "NJI",
                Icon           = "🔍",
                NameKey        = "Seq_NJI_Name",
                DescriptionKey = "Seq_NJI_Desc",
                BuildSteps     = NJISequence.Build,
            },
            new SequenceDefinition
            {
                Id             = "AUTO_PRINT",
                Icon           = "🖨",
                NameKey        = "Seq_AutoPrint_Name",
                DescriptionKey = "Seq_AutoPrint_Desc",
                BuildSteps     = AutoPrintSequence.Build,
            },
            new SequenceDefinition
            {
                Id             = "HEAD_UP",
                Icon           = "⬆",
                NameKey        = "Seq_HeadUp_Name",
                DescriptionKey = "Seq_HeadUp_Desc",
                BuildSteps     = HeadUpSequence.Build,
            },
            new SequenceDefinition
            {
                Id             = "HEAD_DOWN",
                Icon           = "⬇",
                NameKey        = "Seq_HeadDown_Name",
                DescriptionKey = "Seq_HeadDown_Desc",
                BuildSteps     = HeadDownSequence.Build,
            },
        };
    }
}
