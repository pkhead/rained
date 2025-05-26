# Brief
This page is intended for users who have experience using a Rain World level editor prior. Here is a quick list of things that are different:

- Rained has a menubar (on the top of the window). Check them out!
- In the effects editor, you can drag effect slots up and down in the active effects list.
- You can also delete effect slots by right-clicking on them.
- In the light editor, you scale and rotate your brush by holding down a key and moving your mouse.
- You can also use your mouse to manipulate the light ring for the light angle.
- Press Ctrl+E (or Edit > Select) to begin geo/tile select/move, copy, and paste mode.

The prop editor has some pretty significant changes in design when compared to RWE+ or the Lingo editor. It works akin to transform tools
in other software like Photoshop.

In the prop editor, right-click (or press C) in the level to add a prop. Afterward, moving a prop is done by simply selecting them and dragging them around. Scaling is done by dragging their corners.

If you want to warp them, you enable vertex mode by pressing F (by default), and, for normal props, you may warp them by dragging their corners. When vertex mode is on, the outline colors for each prop mean different things about the way they're moved/rotated/scaled:

- Blue: The scale axis is determined by the prop's rotation - this will keep rectangles rectangles.
- White: In contrast to blue, the scale axis is not determined by its rotation - it will be distorted if it's scaled while rotated.
- Green: This is a rope or a long prop, and you can move its endpoints in vertex mode.
- Red: A rope/long prop that has been warped in a different level editor. The editor only supports rectangular rope/long props, so you can't transform it until you press "Reset Transform".

If you do not like the default keyboard shortcuts, feel free to complain to me. You may also change them through the preferences menu,
accessible by going to the "File" menu, then clicking on "Preference