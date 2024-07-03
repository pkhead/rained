namespace RainEd;

/// <summary>
/// Provides generalized functions where I don't know a good class to put it in.
/// </summary>
static class Util
{
    // adapted from the psuedocode on the Wikipedia page
    public static void Bresenham(int x0, int y0, int x1, int y1, Action<int, int> plot)
    {
        int dx = Math.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int error = dx + dy;

        while (true)
        {
            plot(x0, y0);
            if (x1 == x0 && y0 == y1) break;
            int e2 = 2 * error;
            
            if (e2 >= dy)
            {
                if (x0 == x1) break;
                error += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                if (y0 == y1) break;
                error += dx;
                y0 += sy;
            }
        }
    }
}