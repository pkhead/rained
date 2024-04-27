namespace RainEd;
using System.Numerics;
using Raylib_cs;
using ImGuiNET;
using Autotiles;

class AutotilePathBuilder : IAutotileInputBuilder
{
    [Flags]
    enum PathDirection
    {
        Left = 1,
        Right = 2,
        Up = 4,
        Down = 8
    };

    struct PreviewSegment
    {
        public Vector2 Center;
        public PathDirection Directions;
        public int Index;
    }

    private readonly Autotiles.Autotile autotile;

    private readonly List<Vector2i> autotilePath = [];
    private readonly List<PathDirection> autotilePathDirs = [];
    private readonly List<PreviewSegment> previewSegments = [];
    private readonly float gridOffset;

    public AutotilePathBuilder(Autotiles.Autotile autotile) {
        this.autotile = autotile;
        gridOffset = autotile.PathThickness % 2 == 0 ? 0f : 0.5f;
    }

    // get connected sides of the ith node, as a PathDirection

    /// <summary>
    /// Deduce which sides of the i th node are connected to another node.
    /// </summary>
    /// <param name="i">The index of the node.</param>
    /// <returns>A flag enum stating which sides are connected.</returns>
    private PathDirection GetPathDirections(int i)
    {
        GetPathDirections(i, out bool left, out bool right, out bool up, out bool down);
        PathDirection dir = 0;
        if (left) dir |= PathDirection.Left;
        if (right) dir |= PathDirection.Right;
        if (up) dir |= PathDirection.Up;
        if (down) dir |= PathDirection.Down;

        return dir;
    }

    /// <summary>
    /// Deduce which sides of the i th node are connected to another node.
    /// </summary>
    /// <param name="i">The index of the node.</param>
    /// <param name="left">True if the node is connected on the left side, false if not.</param>
    /// <param name="right">True if the node is connected on the right side, false if not.</param>
    /// <param name="up">True if the node is connected on the up side, false if not.</param>
    /// <param name="down">True if the node is connected on the down side, false if not.</param>
    private void GetPathDirections(int i, out bool left, out bool right, out bool up, out bool down)
    {
        var lastSeg = autotilePath[^1]; // wraps around
        var curSeg = autotilePath[i];
        var nextSeg = autotilePath[0]; // wraps around

        if (i > 0)
            lastSeg = autotilePath[i-1];

        if (i < autotilePath.Count - 1)
            nextSeg = autotilePath[i+1];
        
        left =  (curSeg.Y == lastSeg.Y && curSeg.X - 1 == lastSeg.X) || (curSeg.Y == nextSeg.Y && curSeg.X - 1 == nextSeg.X);
        right = (curSeg.Y == lastSeg.Y && curSeg.X + 1 == lastSeg.X) || (curSeg.Y == nextSeg.Y && curSeg.X + 1 == nextSeg.X);
        up =    (curSeg.X == lastSeg.X && curSeg.Y - 1 == lastSeg.Y) || (curSeg.X == nextSeg.X && curSeg.Y - 1 == nextSeg.Y);
        down =  (curSeg.X == lastSeg.X && curSeg.Y + 1 == lastSeg.Y) || (curSeg.X == nextSeg.X && curSeg.Y + 1 == nextSeg.Y);
    }

    private bool CanAppendPath(Autotiles.Autotile autotile, Vector2i newPos)
    {
        var lastPos = autotilePath[^1];
        int dx = newPos.X - lastPos.X;
        int dy = newPos.Y - lastPos.Y;

        // if newPos is too far or if being placed diagonally
        // (coincidentally, manhattan distance)
        if (MathF.Abs(dx) + MathF.Abs(dy) != 1)
            return false;

        bool noTurn = autotilePath.Count <= autotile.PathThickness / 2;
        
        if (autotilePath.Count > 2 && autotile.SegmentLength > 1)
        {
            // can only make a turn if the last node is in the middle of
            // a straight segment
            // TODO: this logic may be incorrect
            if (autotilePath.Count % autotile.SegmentLength != 1)
                noTurn = true;
        }

        // can't make a turn inside another turn segment
        if (autotilePath.Count >= autotile.PathThickness)
        {
            for (int i = autotilePath.Count - autotile.PathThickness; i < autotilePath.Count-1; i++)
            {
                GetPathDirections(i, out bool l, out bool r, out bool u, out bool d);
                if ((l || r) && (u || d))
                {
                    noTurn = true;
                    break;
                }
            }
        }

        // if noTurn is true,
        // disallow placement if the new node will make a turn
        if (autotilePath.Count >= 2)
        {
            var otherPos = autotilePath[^2];
            var lastDx = lastPos.X - otherPos.X;
            var lastDy = lastPos.Y - otherPos.Y;

            if (noTurn && (lastDx != dx || lastDy != dy))
                return false;
        }
        
        return true;
    }

    private bool ProcessPoint(Vector2i pos)
    {
        if (autotilePath.Count >= 2 && autotilePath[^2] == pos)
        {
            // if the user backs their cursor up, erase the last segment
            autotilePath.RemoveAt(autotilePath.Count - 1);
            autotilePathDirs.RemoveAt(autotilePathDirs.Count - 1);
            return true;
        }
        else
        {
            // only place node if there isn't already a node here
            // and the CanAppendPath check returns true
            if (!autotilePath.Contains(pos))
            {
                if (CanAppendPath(autotile, pos))
                {
                    autotilePath.Add(pos);
                    autotilePathDirs.Add(0);
                    return true;
                }
            }
        }

        return false;
    }

    public void Update()
    {
        if (autotile is null) return;

        float gridOffsetInverse = 0.5f - gridOffset;

        // add current position to autotile path
        // only add the position if it is adjacent to the last
        // placed position
        var mousePos = new Vector2i(
            (int)(RainEd.Instance.Window.MouseCellFloat.X + gridOffsetInverse),
            (int)(RainEd.Instance.Window.MouseCellFloat.Y + gridOffsetInverse)
        );
        
        // first node to be placed
        if (autotilePath.Count == 0)
        {
            autotilePath.Add(mousePos);
            autotilePathDirs.Add(0);
        }
        else if (autotilePath[^1] != mousePos)
        {
            // if the user's mouse pos is not 1 manhattan-distance away from the
            // last point, then i will do some "interpolation".
            // tracing the shortest manhattan path from the last point to the
            // mouse position.
            
            // however, there are two shortest manhatten paths, so
            // "xFirst" determines if the X side will be traversed first,
            // or if the Y side will be traversed first.
            // this will be determined by the direction the line has been
            // facing thus far.
            bool xFirst;

            // delta vector from last path point to mouse pos
            var delta = mousePos - autotilePath[^1];
            int dx = Math.Sign(delta.X);
            int dy = Math.Sign(delta.Y);

            if (autotilePath.Count >= 2)
            {
                var posA = autotilePath[^2];
                var posB = autotilePath[^1];
                xFirst = Math.Abs(posB.X - posA.X) == 1;

                // if shift is held down, attempt to straighten path
                if (EditorWindow.IsKeyDown(ImGuiKey.ModShift))
                {
                    if (xFirst) dy = 0;
                    else        dx = 0;
                }
            }
            else
            {
                // if there is only one point in the path,
                // then there isn't enough info to determine
                // the recent direction of the path. so,
                // i use a different method.
                xFirst = Math.Abs(delta.X) > Math.Abs(delta.Y);
            }
            
            if (xFirst)
            {
                for (int x = autotilePath[^1].X + dx; x != mousePos.X + dx; x += dx)
                {
                    if (!ProcessPoint(new Vector2i(x, autotilePath[^1].Y))) goto interpolateEnd;
                }

                for (int y = autotilePath[^1].Y + dy; y != mousePos.Y + dy; y += dy)
                {
                    if (!ProcessPoint(new Vector2i(mousePos.X, y))) goto interpolateEnd;
                }
            }
            else
            {
                for (int y = autotilePath[^1].Y + dy; y != mousePos.Y + dy; y += dy)
                {
                    if (!ProcessPoint(new Vector2i(autotilePath[^1].X, y))) goto interpolateEnd;
                }

                for (int x = autotilePath[^1].X + dx; x != mousePos.X + dx; x += dx)
                {
                    if (!ProcessPoint(new Vector2i(x, mousePos.Y))) goto interpolateEnd;
                }
            }
            interpolateEnd:;
        }

        // pre-calculate autotile path node directions
        for (int i = 0; i < autotilePath.Count; i++)
        {
            autotilePathDirs[i] = GetPathDirections(i);
        }

        previewSegments.Clear();
        
        int firstIndex = autotile.PathThickness % 2 == 0 ? 1 : 0;
        int lineStart = firstIndex;
        PathDirection lineDir = 0;

        if (autotilePath.Count > 1)
        {
            // find the turns (or the end of the path)
            for (int i = 0; i <= autotilePath.Count; i++)
            {
                int lineEnd = -1;

                if (i < autotilePath.Count)
                {
                    var nodePos = autotilePath[i];
                    var directions = autotilePathDirs[i];
                    bool horiz = directions.HasFlag(PathDirection.Left) || directions.HasFlag(PathDirection.Right);
                    bool vert = directions.HasFlag(PathDirection.Up) || directions.HasFlag(PathDirection.Down);

                    // if this is a node where a turn occurs
                    if (horiz && vert)
                    {
                        previewSegments.Add(new PreviewSegment()
                        {
                            Center = nodePos + new Vector2(gridOffset, gridOffset),
                            Directions = directions,
                            Index = i
                        });

                        // where the end of the line before the turn is
                        lineEnd = i - autotile.PathThickness / 2;
                        if (autotile.PathThickness % 2 == 0) lineEnd++;

                        // set the start of the next line
                        i += autotile.PathThickness / 2;
                    }
                    else
                    {
                        lineDir = directions;
                    }
                }
                else
                {
                    // end of path reached
                    lineEnd = autotilePath.Count;
                }

                // procedure to create the line of segments
                // in between lineStart and lineEnd
                if (lineEnd >= 0 && lineStart < autotilePath.Count)
                {
                    // calculate the direction of the line
                    int dx, dy;
                    if (lineStart == autotilePath.Count - 1)
                    {
                        dx = autotilePath[lineStart].X - autotilePath[lineStart-1].X;
                        dy = autotilePath[lineStart].Y - autotilePath[lineStart-1].Y;
                    }
                    else
                    {
                        dx = autotilePath[lineStart+1].X - autotilePath[lineStart].X;
                        dy = autotilePath[lineStart+1].Y - autotilePath[lineStart].Y;
                    }

                    if (MathF.Abs(dx) + MathF.Abs(dy) != 1)
                        throw new Exception();
                    
                    var dir = new Vector2(dx, dy);
                    var pOffset = autotile.PathThickness % 2 == 0 ? -0.5f : 0f;

                    PathDirection dirFlags = 0;
                    if (dx != 0) dirFlags |= PathDirection.Right | PathDirection.Left;
                    if (dy != 0) dirFlags |= PathDirection.Down | PathDirection.Up;

                    // loop to create the segments
                    for (int j = lineStart; j < lineEnd; j += autotile.SegmentLength)
                    {
                        Vector2 nodePos = new(autotilePath[j].X, autotilePath[j].Y);
                        nodePos += new Vector2(gridOffset, gridOffset);

                        // if at the ends of the path, use the raw path direction of that node
                        // this is so an edge is created at the caps
                        PathDirection segmentDir;

                        if (j == firstIndex)
                            segmentDir = autotilePathDirs[0];
                        else if (j + autotile.SegmentLength >= autotilePath.Count)
                            segmentDir = autotilePathDirs[^1];
                        else
                            segmentDir = dirFlags;

                        // create the segment
                        previewSegments.Add(new PreviewSegment()
                        {
                            Center = nodePos + dir * pOffset,
                            Directions = segmentDir,
                            Index = j
                        });
                    }

                    lineStart = i+1;
                }
            }
        }

        DrawPreview();
    }

    /// <summary>
    /// Draw the preview of the autotiler path.
    /// </summary>
    private void DrawPreview()
    {
        // draw autotile path nodes
        // only drawing lines where the path doesn't connect to another segment
        var color = RainEd.Instance.Preferences.LayerColor2.ToRGBA(120);
        for (int i = 0; i < autotilePath.Count; i++)
        {
            var nodePos = autotilePath[i];
            GetPathDirections(i, out bool left, out bool right, out bool up, out bool down);

            float x = nodePos.X + gridOffset;
            float y = nodePos.Y + gridOffset;
            var cellOrigin = new Vector2(x, y);
            
            // draw tile path line
            Raylib.DrawRectangleV(cellOrigin * Level.TileSize - new Vector2(4f, 4f), new Vector2(8f, 8f), color);

            /*if (left)
                Raylib.DrawLineV(cellOrigin*Level.TileSize, new Vector2(x-0.5f, y)*Level.TileSize, Color.White);
            if (right)
                Raylib.DrawLineV(cellOrigin*Level.TileSize, new Vector2(x+0.5f, y)*Level.TileSize, Color.White);
            if (up)
                Raylib.DrawLineV(cellOrigin*Level.TileSize, new Vector2(x, y-0.5f)*Level.TileSize, Color.White);
            if (down)
                Raylib.DrawLineV(cellOrigin*Level.TileSize, new Vector2(x, y+0.5f)*Level.TileSize, Color.White);*/
        }

        foreach (var segment in previewSegments)
        {
            bool left = segment.Directions.HasFlag(PathDirection.Left);
            bool right = segment.Directions.HasFlag(PathDirection.Right);
            bool up = segment.Directions.HasFlag(PathDirection.Up);
            bool down = segment.Directions.HasFlag(PathDirection.Down);
            
            bool horiz = left || right;
            bool vert = up || down;

            var cellCenter = segment.Center;
            
            var x = cellCenter.X;
            var y = cellCenter.Y;

            float vertThickness = vert ? autotile.PathThickness / 2f : autotile.SegmentLength / 2f;
            float horizThickness = horiz ? autotile.PathThickness / 2f : autotile.SegmentLength / 2f;

            if (!left)
                Raylib.DrawLineV(
                    new Vector2(x - vertThickness, y - horizThickness) * Level.TileSize,
                    new Vector2(x - vertThickness, y + horizThickness) * Level.TileSize,
                    Color.White
                );

            if (!right)
                Raylib.DrawLineV(
                    new Vector2(x + vertThickness, y - horizThickness) * Level.TileSize,
                    new Vector2(x + vertThickness, y + horizThickness) * Level.TileSize,
                    Color.White
                );

            if (!up)
                Raylib.DrawLineV(
                    new Vector2(x - vertThickness, y - horizThickness) * Level.TileSize,
                    new Vector2(x + vertThickness, y - horizThickness) * Level.TileSize,
                    Color.White
                );

            if (!down)
                Raylib.DrawLineV(
                    new Vector2(x - vertThickness, y + horizThickness) * Level.TileSize,
                    new Vector2(x + vertThickness, y + horizThickness) * Level.TileSize,
                    Color.White
                );
        }
    }

    /// <summary>
    /// Submit the constructed path to the autotiler.
    /// </summary>
    /// <param name="layer">The layer to autotile.</param>
    /// <param name="force">If the autotiler should force-place the tiles.</param>
    /// <param name="geometry">If the autotiler should modify geometry.</param>
    public void Finish(int layer, bool force, bool geometry)
    {
        RainEd.Logger.Information("Run autotile {Name}", autotile.Name);
            
        if (previewSegments.Count > 0)
        {
            previewSegments.Sort(static (PreviewSegment a, PreviewSegment b) => a.Index.CompareTo(b.Index));

            // create path segment table from the TileEditor PreviewSegment class
            var pathSegments = new PathSegment[previewSegments.Count];
            for (int i = 0; i < previewSegments.Count; i++)
            {
                var seg = previewSegments[i];
                pathSegments[i] = new PathSegment()
                {
                    X = (int)MathF.Ceiling(seg.Center.X) - 1,
                    Y = (int)MathF.Ceiling(seg.Center.Y) - 1,
                    Left = seg.Directions.HasFlag(PathDirection.Left),
                    Right = seg.Directions.HasFlag(PathDirection.Right),
                    Up = seg.Directions.HasFlag(PathDirection.Up),
                    Down = seg.Directions.HasFlag(PathDirection.Down)
                };
            }

            autotile.TilePath(layer, pathSegments, force, geometry);
        }
    }
}