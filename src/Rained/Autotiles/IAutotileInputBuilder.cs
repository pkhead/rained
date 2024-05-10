namespace RainEd.Autotiles;

interface IAutotileInputBuilder
{
    void Update();
    void Finish(int layer, bool force, bool geometry);
}