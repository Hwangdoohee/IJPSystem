using IJPSystem.Platform.Domain.Interfaces;
using IJPSystem.Platform.Domain.Models.Vision;
using System;
using System.Collections.Generic;

namespace IJPSystem.Platform.HMI.Services
{
    /// <summary>
    /// Phase 1 임시 구현 — Score 를 기반으로 확률 분포를 따라 노즐 상태를 임의 할당.
    /// Phase 2 에서 OnnxNozzleClassifier 로 교체할 예정.
    /// </summary>
    public class RandomNozzleClassifier : INozzleClassifier
    {
        private readonly Random _rng = new();

        public IDictionary<int, int> Classify(InspectionResult result, int nozzleCount)
        {
            // State: 0=Unknown, 1=Good, 2=Weak, 3=Missing  (NozzleState enum 과 일치)
            double healthPct = Math.Clamp(result.Score / 100.0, 0.0, 1.0);

            var states = new Dictionary<int, int>(nozzleCount);
            for (int i = 1; i <= nozzleCount; i++)
            {
                double r = _rng.NextDouble();
                states[i] = r < healthPct        ? 1   // Good
                          : r < healthPct + 0.05 ? 2   // Weak
                                                 : 3;  // Missing
            }
            return states;
        }
    }
}
