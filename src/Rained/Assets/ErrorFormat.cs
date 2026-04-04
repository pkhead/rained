namespace Rained.Assets;

static class ErrorFormat
{
    // helper function to create error string with line information
    public static string ErrorString(int lineNo, string msg)
    {
        if (lineNo == -1)
            return "[EMBEDDED]: " + msg;
        else if (msg.Length > 2 && msg[..2] == "1:")
            return "Line " + lineNo + ":" + msg[2..];
        else
            return "Line " + lineNo + ": " + msg;
    }
}