# Cameras
This is the editor where you can place and move cameras. Each camera represents an individual screen in a room and will always map to one rendered .png file.

## Editing
Cameras are represented by green rectangles/quadrilaterals. You can select cameras by clicking on them, and you can move them by dragging them around. Multiple cameras can be selected at once by holding <kbd>Shift</kbd> while selecting another camera. Selected cameras can be deleted by pressing the `X` key **TODO: i forgot which key**. You may also add cameras by either double-clicking on an empty space or by pressing the `C` key.

By default, cameras can only be viewed in the camera editor, but by enabling `View > Camera Borders`, you can view the outlines of cameras in all editors, colored green. Which outlines will be visible is determined by the Camera Border Mode option in the Preferences window. **TODO: is it called that?**

Cameras can be aligned with other cameras by holding the <kbd>A</kbd>/<kbd>D</kbd> or <kbd>W</kbd>/<kbd>S</kbd> key, while moving one or more cameras, and moving a camera near the axis another camera lies on.

**TODO: figure visualizing camera snap lines extending from cameras**

Usually, cameras are aligned in a grid, which is why snapping exists. However, unless you want to be compatible with mods that implement camera scrolling (i.e. SBCameraScroll), this is not required. Otherwise, there may be visual artifacts from the absence of a visual for portions of the screen.

## Sections of a camera
Each camera has three concentric sections. Sorted from largest to smallest, these are:

1. The entire rendered area. The bounds of this area are not shown with an outline.
2. The entire visible are when Rain World is played with a 16:9 aspect ratio. The bounds of this area is shown by a black outline. It is slightly smaller than the full size of the screen to accommodate for screen shake.
3. The entire visible area when Rain World is played with a 4:3 aspect ratio. The bounds of this area is shown by a green outline (though red in the official editor). It also serves as the in-game area where the game will switch cameras once the player intersects with it.

## Camera angles
**TODO: rewrite, also add figure visualizing camera warping although I need to ask for credits.**

Cameras also have a thing called "camera angles". These are controlled by the green rings that appear on the corners of a camera when you have one selected. Moving them around basically warps the perspective of the level when rendering so it kind of looks like it's viewed at an angle, which can look pretty cool.

These rings control the offsets of the corners of the green quad. The green quad actually represents the shape of the furthest sublayer for the camera. Each screen/camera in a Rain World level is rendered in 30 sublayers, as each of the three work layers are subdivided into ten sublayers for rendering. The fifth closest sublayer is always a perfect rectangle, but the farther back the sublayer is, the more intense the warping becomes, until the furthest layer's shape matches the green quad. The sublayers combine in rendering into a single, 3Dish image.

## Rendering order
Cameras are rendered in order from first created to last created. The order that cameras will be rendered in can be shown by enabling "Show Camera Numbers" in the preferences window. They will show as white numbers in the middle of a camera.

You cannot do much to change the order of cameras, other than moving or recreating them in the order you want. However, you can set which camera should be rendered first by selecting a camera and pressing `Edit > Set Priority`. Only one camera can be prioritized at a time. Also, this flag is not saved in the level file and thus will be reset on the next reload. If you want to remove the prioritization of any camera, simply press `Edit > Clear Priority`. You do not need to have the pertinent camera selected to do this.