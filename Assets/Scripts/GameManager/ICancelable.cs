public interface ICancelable
{
    bool CanCancel();
    void Cancel();
    int Priority { get; }
}