namespace RainEd.EditorGui.Editors;

interface IEditorMode
{
    string Name { get; }

    void Load() {}
    void Unload() {}
    void ShowEditMenu() {}
    void SavePreferences(UserPreferences prefs) {}

    // write dirty changes to the Level object
    // this is used by the light editor, since most everything is done in the GPU
    // since doing the processing on the CPU would prove too slow
    void FlushDirty() {}
    void ReloadLevel() {}

    void DrawToolbar();
    void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D[] layerFrames);
}

enum EditModeEnum
{
    None = -1,
    Environment = 0,
    Geometry,
    Tile,
    Camera,
    Light,
    Effect,
    Prop
};