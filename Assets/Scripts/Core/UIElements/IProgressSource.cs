namespace Core.UIElements
{
    public interface IProgressSource
    {
        bool IsInProgress { get; }
        float Progress01 { get; }
        string ProgressLabel { get; }
    }
}