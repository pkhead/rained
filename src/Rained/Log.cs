// for some reason i was stupid and didn't realize Serilog.Log existed
// and wrote RainEd.Logger.[x] everywhere.
// don't wanna bother including Serilog in the files i find+replace
// so i'm just making a mirror of it in the RainEd namespace.
namespace RainEd;

static class Log
{
    public static void Debug(string msgTemplate, params object[] args) => Serilog.Log.Debug(msgTemplate, args);
    public static void Verbose(string msgTemplate, params object[] args) => Serilog.Log.Verbose(msgTemplate, args);
    public static void Information(string msgTemplate, params object[] args) => Serilog.Log.Information(msgTemplate, args);
    public static void Warning(string msgTemplate, params object[] args) => Serilog.Log.Warning(msgTemplate, args);
    public static void Error(string msgTemplate, params object[] args) => Serilog.Log.Error(msgTemplate, args);
    public static void Fatal(string msgTemplate, params object[] args) => Serilog.Log.Fatal(msgTemplate, args);
}