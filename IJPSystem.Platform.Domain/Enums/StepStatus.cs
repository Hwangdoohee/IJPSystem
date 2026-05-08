namespace IJPSystem.Platform.Domain.Enums
{
    public enum StepStatus
    {
        Waiting,
        Running,
        Done,
        Failed,
        Aborted
    }

    public enum SequenceState
    {
        Idle,
        Running,
        Paused,
        Completed,
        Aborted,
        Error
    }
}
