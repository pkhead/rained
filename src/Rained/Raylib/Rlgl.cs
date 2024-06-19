namespace Raylib_cs;

static class Rlgl
{
    public static void PushMatrix()
    {
        Raylib.GlibWindow.RenderContext!.PushTransform();
    }

    public static void PopMatrix()
    {
        Raylib.GlibWindow.RenderContext!.PopTransform();
    }

    public static void LoadIdentity()
    {
        Raylib.GlibWindow.RenderContext!.ResetTransform();
    }

    public static void Scalef(float x, float y, float z)
    {
        Raylib.GlibWindow.RenderContext!.Scale(x, y, z);
    }

    public static void Translatef(float x, float y, float z)
    {
        Raylib.GlibWindow.RenderContext!.Translate(x, y, z);
    }
}