namespace Rained.Drizzle;

class ProgressNoSync<T> : IProgress<T>
{
    public event EventHandler<T>? ProgressChanged;

    public void Report(T e)
    {
        ProgressChanged?.Invoke(this, e);
    }
}