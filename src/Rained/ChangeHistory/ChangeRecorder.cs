namespace Rained.ChangeHistory;

[Serializable]
public class ChangeRecorderException : Exception
{
    public ChangeRecorderException() { }
    public ChangeRecorderException(string message) : base(message) { }
    public ChangeRecorderException(string message, System.Exception inner) : base(message, inner) { }
}

abstract class ChangeRecorder
{
    protected static void ValidationError(string msg)
    {
        #if DEBUG
        throw new ChangeRecorderException(msg);
        #else
        Log.Error(msg);
        EditorWindow.ShowNotification("Error occured with change history!");
        #endif
    }
}