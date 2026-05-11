using IJPSystem.Platform.Domain.Models.Vision;
using System.Collections.Generic;

namespace IJPSystem.Platform.Domain.Interfaces
{
    /// <summary>
    /// Drop Watcher 이미지를 받아 노즐 단위로 상태(Good/Weak/Missing)를 분류한다.
    /// Phase 1: RandomNozzleClassifier (Score 기반 확률 분포)
    /// Phase 2: OnnxNozzleClassifier (실제 이미지 분석, ONNX Runtime)
    /// </summary>
    public interface INozzleClassifier
    {
        /// <summary>
        /// 검사 결과(Score)와 노즐 수를 받아 노즐별 상태(0=Unknown,1=Good,2=Weak,3=Missing)를 반환.
        /// 키 = 노즐 인덱스(1-based), 값 = 상태 코드.
        /// </summary>
        IDictionary<int, int> Classify(InspectionResult result, int nozzleCount);
    }
}
