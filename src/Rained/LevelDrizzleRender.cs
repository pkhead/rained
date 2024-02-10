using Drizzle.Lingo.Runtime;
using Drizzle.Logic;
using Drizzle.Logic.Rendering;
using Drizzle.Ported;
using SixLabors.ImageSharp;
namespace RainEd;

class LevelDrizzleRender
{
    public static void Render(RainEd editor)
    {
        var filePath = editor.CurrentFilePath;
        if (string.IsNullOrEmpty(filePath)) throw new Exception("Render called but level wasn't saved");

        LevelSerialization.Save(editor, filePath);

        try
        {
            Configuration.Default.PreferContiguousImageBuffers = true;
            var runtime = new LingoRuntime(typeof(MovieScript).Assembly);
            runtime.Init();
            EditorRuntimeHelpers.RunStartup(runtime);
            EditorRuntimeHelpers.RunLoadLevel(runtime, filePath);

            var renderer = new LevelRenderer(runtime, null);

            Console.WriteLine("Begin render");
            renderer.DoRender();
            Console.WriteLine("Render successful!");
        }
        catch (Exception e)
        {
            editor.ShowError("An error occured when rendering the level");
            Console.WriteLine("ERROR: Could not render level!");
            Console.WriteLine(e);
        }
    }
}