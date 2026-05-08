using System;
using System.Threading;
using System.Threading.Tasks;

namespace IJPSystem.Platform.Application.Sequences
{
    public record SequenceStepDef(
        int Number,
        string Name,
        Func<CancellationToken, Task> Action);
}
