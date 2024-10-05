# Resizing
Pressing the "Resize Level" button in the **Edit** menu will open this window:

![The "Resize Level" window](img/level-resize.png)

- **Width/Height**: This is the desired width and height of the level in grid units.
- **Screen Width/Screen Height**: This is the desired width and height of the level in screens. Below are the formulas used for grid unit/screen conversion.

        Width = 52 * Screens + 20
        Height = 40 * Screens + 3

- **Anchors**: Controls the origin point of the resize operation. It can be set to one of the four corners of the level, the centers of the four edges, or the center of the level.
- **Border Tiles**: Controls the offset from the level's edge for each edge of the level border.

## Border
Rain World levels have a border which dictates the region of the level that is interactable in-game. The border is displayed in Rained as a white rectangle.

Anything outside of the border rectangle will appear in the .png renders of the level, but will not be present in the .txt file describing the geometry of the level. As such, creatures interacting with geometry outside of the level border will either pass through solid blocks or stand on thin air, depending on the closest block that is inside the border at a given position. Any objects in the geometry editor that is outside of the border will be colored red instead of white, indicating that the object will have no effect in-game.