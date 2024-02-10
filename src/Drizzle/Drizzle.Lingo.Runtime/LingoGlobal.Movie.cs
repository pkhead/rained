using System;
using System.Dynamic;

namespace Drizzle.Lingo.Runtime;

public sealed partial class LingoGlobal
{
    public Movie _movie { get; private set; } = default!;

    public void go(LingoNumber frame) => _movie.go(frame);
    public LingoNumber the_frame => _movie.frame;

    public sealed class Movie
    {
        private readonly LingoGlobal _global;

        public Window window { get; }

        public Movie(LingoGlobal global)
        {
            _global = global;
            window = new Window(global);
        }

        public LingoNumber frame => 0;

        public string path => _global.the_moviePath;

        public dynamic stage => throw new NotImplementedException();

        public LingoNumber exitlock { get; set; } = FALSE;

        public void go(LingoNumber newFrame)
        {
            // score not implemented.
        }

        public void halt() => throw new NotImplementedException();
    }

    public sealed class Window
    {
        public Window(LingoGlobal lingoGlobal)
        {

        }

        // Literally unused except for one set, just return it.
        public dynamic appearanceoptions => new ExpandoObject();
        public LingoNumber resizable { get; set; }
        public LingoRect rect { get; set; }
        public LingoSymbol sizestate => new ("normal");
    }
}
