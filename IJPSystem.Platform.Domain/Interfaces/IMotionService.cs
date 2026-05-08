using IJPSystem.Platform.Domain.Models.Motion;
using System.Threading;
using System.Threading.Tasks;

namespace IJPSystem.Platform.Domain.Interfaces
{
    public interface IMotionService
    {
        // 기본은 Move 프로파일. 인쇄 구간 등에서는 Printing 프로파일 명시 가능.
        Task MoveToPointAsync(string pointName, CancellationToken ct, MotionProfileKind profile = MotionProfileKind.Move);
        Task ServoOnAllAsync();
        Task HomeAllAsync(CancellationToken ct);
    }
}
