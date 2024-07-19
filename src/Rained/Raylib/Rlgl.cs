namespace Raylib_cs;

static class Rlgl
{
    public static void PushMatrix()
    {
        Glib.RenderContext.Instance!.PushTransform();
    }

    public static void PopMatrix()
    {
        Glib.RenderContext.Instance!.PopTransform();
    }

    public static void LoadIdentity()
    {
        Glib.RenderContext.Instance!.ResetTransform();
    }

    public static void Scalef(float x, float y, float z)
    {
        Glib.RenderContext.Instance!.Scale(x, y, z);
    }

    public static void Translatef(float x, float y, float z)
    {
        Glib.RenderContext.Instance!.Translate(x, y, z);
    }
}