namespace Rained.Autotiles;

interface IAutotileInputBuilder
{
    public Autotile Autotile { get; }
    void Update();
    void Finish(int layer, bool force, bool geometry);
}