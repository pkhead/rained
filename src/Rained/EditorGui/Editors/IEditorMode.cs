using Rained.LevelData;

namespace Rained.EditorGui.Editors;

interface IEditorMode
{
    string Name { get; }
    bool SupportsCellSelection { get; }

    void Load() {}
    void Unload() {}
    void ShowEditMenu() {}
    void SavePreferences(UserPreferences prefs) {}

    // write dirty changes to the Level object
    // this is used by the light editor, since most everything is done in the GPU
    // since doing the processing on the CPU would prove too slow
    void FlushDirty() {}

    // this is called when the level is resized
    void ReloadLevel() {}
    // for compatibility purposes, automatically call ReloadLevel
    void ChangeLevel(Level newLevel) { ReloadLevel(); }
    void LevelCreated(Level level) {}
    void LevelClosed(Level level) {}

    void DrawToolbar();
    void DrawViewport(RlManaged.RenderTexture2D mainFrame, RlManaged.RenderTexture2D[] layerFrames);
    void DrawStatusBar() {}
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