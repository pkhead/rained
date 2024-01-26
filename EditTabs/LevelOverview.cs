namespace RainEd;

public class LevelOverview
{
    public bool IsWindowOpen = true;

    private readonly Level level;

    public LevelOverview(Level level) {
        this.level = level;
    }

    public void Render() {}
}