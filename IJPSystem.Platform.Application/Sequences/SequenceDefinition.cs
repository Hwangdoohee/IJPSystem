using IJPSystem.Platform.Domain.Common;
using IJPSystem.Platform.Domain.Interfaces;
using System;
using System.Collections.Generic;

namespace IJPSystem.Platform.Application.Sequences
{
    /// <summary>
    /// 시퀀스 메타데이터 + 빌드 팩토리.
    /// Name/Description 은 표시용 — 처음엔 번역 키(NameKey/DescriptionKey)와 동일,
    /// 언어 변경 시 HMI 가 Loc.T(...) 로 갱신해서 UI 가 자동 반영.
    /// </summary>
    public class SequenceDefinition : ViewModelBase
    {
        public string Id             { get; init; } = "";
        public string Icon           { get; init; } = "";
        public string NameKey        { get; init; } = "";
        public string DescriptionKey { get; init; } = "";

        private string _name = "";
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _description = "";
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// 매 실행마다 새 스텝 목록을 반환하는 팩토리.
        /// IMachine: IO/Motion/Vision 드라이버 직접 접근 (WaitHelper + ScheduleInput 사용)
        /// IMotionService: 티칭 포인트 이동 등 고수준 동작
        /// </summary>
        public Func<IMachine, IMotionService, IReadOnlyList<SequenceStepDef>> BuildSteps { get; init; }
            = (_, _) => Array.Empty<SequenceStepDef>();
    }
}
