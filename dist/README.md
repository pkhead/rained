Read LICENSE.md for software licenses to the open-source libraries I have used.

Don't hestiate to report any bugs, complaints, and suggestions by creating an issue on GitHub, or pinging @chromosoze in either the main Rain World Discord server or one of both Rain World modding servers. I also accept DMs, but as I don't get pinged for DM requests, it would take a while for me to respond.

Also, if you are on Windows and Rained fails to even open a window when launching it, it's probably because you are missing the Microsoft Visual Studio C++ runtime package.  It can be installed [here](https://aka.ms/vs/17/release/vc_redist.x64.exe). (I might figure out how to remove that requirement at a later date.)

Hope you enjoy!

# Some Notes
## Updates
Check https://github.com/pkhead/rained/releases for any new releases. If you want to update it, you should remove and replace all the files and folders from this installation folder EXCEPT:
- config/
- scripts/
- Your Data folder, if present

## Importing Custom Tiles, Props, or Materials
You *can* do so by editing the Init.txt files manually and copying over the graphics and stuff... Or you can do the same thing automagically through the assets tab in the preferences window (File > Preferences). It'll save you some time! Although, if you have a .zip file you unfortunately must decompress it first. I'll think of a way to change that...

## User Interface
Here are some tips on using the GUI elements:
- You can freely move and resize docks.
- Dragging a window while holding shift won't show the docking options.
  - They also don't show up if you drag a window by its contents rather than by its title bar.
- In the prop editor, some inputs for the prop options are drag inputs, meaning that to change them you
  click and drag left/right in the box.
- Experiment with activating inputs with a modifier key down (ctrl, shift, alt).

## Autosaving
Rained does not have a system that auto-saves the level periodically. However, it does automatically save the current state
of the level to a different file if it catches a fatal exception and crashes, which should be an adequate alleviator.
I would recommend pressng Ctrl+S every minute anyway.

## Scripting
Rained has a Lua scripting API, used for autotiling and level manipulation.
For "documentation", check `scripts/init.lua`, `scripts/definition/rained.defs.lua`, and look at the built-in scripts as examples.

# Brief Guide
## If you've used a Rain World level editor before...
...learning how to use this software shouldn't be too obtuse. Assuming you have this knowledge,
here is a quick list of things that are different:
- Check out the menus (the bar on the top of the window) and the shortcuts!
- In the effects editor, you can drag effect slots up and down in the active effects list.
- You can also delete effect slots by right-clicking on them.
- In the light editor, you scale and rotate your brush by holding down a key and moving your mouse.
- You can also use your mouse to manipulate the light ring for the light angle.

The prop editor has some pretty significant changes in design when compared to RWE+ or the Lingo editor. It works akin to transform tools
in other software like Photoshop.

In the prop editor, right-click (or press C) in the level to add a prop. Afterward, moving a prop is done by simply selecting them and dragging them around. Scaling is done by dragging their corners.

If you want to warp them, you enable vertex mode by pressing F (by default), and, for normal props, you may warp them by dragging their corners. When vertex mode is on, the outline colors for each prop mean different things about the way they're moved/rotated/scaled:
  - Blue: The scale axis is determined by the prop's rotation - this will keep rectangles rectangles.
  - White: In contrast to blue, the scale axis is not determined by its rotation - it will be distorted if it's scaled while rotated.
  - Green: This is a rope or a long prop, and you can move its endpoints in vertex mode.
  - Red: A rope/long prop that has been warped in a different level editor. The editor only supports rectangular rope/long props, so you can't transform it until you press "Reset Transform".

If you do not like the default keyboard shortcuts, feel free to complain to me. You may also change them through the preferences menu,
accessible by going to the "File" clicking on "Preferences".

## If you are new to this...
...then I will provide a brief rundown on how to do level editing.

The level editor has seven different edit modes for each level.

### Environment Editor
This is basically an adaptation of the controls featured in the "level overview" screen from the original level editor. You may control simple parameters about the room.
- **Tile Random Seed:** The seed tiles use for generating graphics when rendering.
- **Enclosed Room:** Corresponds to the "Default Medium" switch in the original level editor. I don't think this does anything.
- **Sunlight:** If this is off, the room will be cast in darkness. (See the Light Editor section later)
- **Water:** If this room has water.
- **Is Water In Front:** If this is off, water will be behind the first layer.

### Geometry
This is where you edit the room's geometry and some game objects. On the right, you have a list of things you can add or remove from the level. Each layer has three work layers. The first is where the creatures reside, and the other two are background layers.

Here are the items not related to shortcuts:
- **Wall:** Solid geometry. If a wall is placed on layer two, then background-climbing creatures like blue lizards can interact with it.
- **Air:** Empty space.
- **Toggle Wall/Air:** Corresponds to "Inverse" in the official level editor. Should be obvious.
- **Slope:** A 45-degree slope. Can only be placed on corners.
- **Invisible Wall:** Corresponds to "Glass" in the original level editor.
- **Platform:** A half-block. In game, you may freely pass through it if entering from the bottom, but in order to go through it from above, you have to hold the down button.
- **Horizontal Beam/Vertical Beam**: Otherwise known as poles. Creatures can climb on them.
- **Fissure:** Corresponds to "Crack Terrain" in the official level editor. You can use this to create a special type of tunnel. It's not used in game that often. For normal tunnels, you use air blocks.
- **Waterfall:** You use this to create decorative waterfalls. Water is flow down from the block.
- **Batfly Hive:** Can only be placed on ground. It makes those spiky white things that batflies burrow into.
- **Forbid Fly Chain:** Placed on the ceiling, and prevents batflies from hanging onto them in chains.
- **Garbage Worm:** Placed in the ground, and can be used to mark a location where a Garbage Worm spawns.
- **Worm Grass:** Placed over the ground. Spawns worm grass. Bigger worm grass is made by stacking them up vertically.
- **Copy Backwards:** Copies the geometry of highlighted area one layer backwards.

Shortcuts, otherwise known as "pipes", are the things you go through to transport to a different room or a different place in the same room.
Shortcut entrances have specific requirements in order to be recognized as valid.

First, the 3x3 area of geometry around the shortcut entrance must be made of solid blocks, save for one block, where you enter the shortcut from, which should be either air or a platform (Platforms are used for downward-facing shortcut entrances). So, for example, to create a shortcut entrance into a wall, you will place a single air block into the wall, and replace the next solid block in the hole with the shortcut entrance. Second, you must have a shortcut block placed in the direction of the shortcut entrance. So, to illustrate, if you have an air block to the left of the shortcut entrance, you will need to place a shortcut block to the right of the entrance.

If you have created a shortcut entrance correctly, the graphic for it will show an arrow pointing in a singular direction. Afterwards, you will use the "Shortcut Dot" item to create a path for the shortcut. Each shortcut dot connects to the next. Note that they will only connect on its four sides, so you can't have paths that go diagonally.

The path will then be concluded by one of several shortcut blocks. Which shortcut block the path ends in changes the behavior of the shortcut.
- **Shortcut Entrance:** Creates a two-way connection between two points in the same room.
- **Room Entrance:** Creates a connection to another room.
- **Creature Den:** Otherwise known as a Dragon Den, a place for creatures other than the slugcat to spawn and sleep in during the rain.
- **Whack-a-mole Hole:** When a creature other than the slugcat enters this shortcut, they will be warped to another whack-a-mole hole in the same room, chosen at random.
- **Scavenger Hole:** Allows scavengers to spawn or travel to this room as they wander through a region.
- **Shortcut Dot:** You can end a shortcut path with a shortcut dot, in which case the shortcut will not be enterable.

### Tiles
Tiles are used to make level geometry actually look like something. Without them, levels will just look like they're made out of big blocks of concrete.
Each tile is a specific asset hand-crafted by someone. These include things like huge beams, pipes, stones, metal things, machinery-looking stuff, and other set pieces.

Each tile has a defined width and height, as well the geometry the tile matches up with, known as "geometry requirements". These geometry requirements are visualized by a set of green-colored square outlines placed behind the tile before placing them in the level editor. A full square represents that it needs a solid block here, a cross represents that it needs air here, and an empty space represents that it doesn't care what goes here. Tiles may also need slopes at certain locations, too.

It is laborious to manually place geometry to fit a tile, so there is a quick way to deal with that. If you hold G while placing a tile, it will automatically place the geometry for you. Likewise, if you hold G while deleting a tile, it will clear up all the geometry that the tile requires. You may also hold F while placing a tile to force-place it, even if the geometry doesn't match up with the tile's requirements.

The tile editor also allows you to place materials in a different mode. These are represented in the editor by colored squares centered in each cell, but when rendering they will look fancier. You may also set the default material for a level, in which case any cells that don't have a material set will use that material. 

### Cameras
Rooms in the Rain World game are all red-colored, pre-rendered, static images. A camera defines an area of the level that the player can view. In-game, when the player intersects with the rectangular area a camera inhibits, the game will switch to viewing the level through that camera.

The outer rectangle of the camera defines the area the camera encompasses when Rain World is played in a 16:9 resolution. The inner thick green rectangle (red in the official level editor) defines the viewable area when Rain World is played in a 4:3 resolution. It's also the boundary between cameras when the game considers which camera to switch over to. Usually cameras are arranged in a grid, but this isn't required (unless you want the SBCameraScroll mod to work properly).

Cameras also have a thing called "camera angles". These are the green rings that appear on the corners of a camera when you have them selected. Moving them around basically warps the perspective of the level when rendering so it kind of looks like it's viewed at an angle, which can look pretty cool.

These rings control the locations of the corners of the green quad. The green quad actually represents the shape of the furthest sublayer for the camera. Each screen/camera in a Rain World level is rendered in 30 sublayers, as each of the three work layers are subdivided into ten sublayers for rendering. The sixth closest sublayer is always a perfect rectangle, but the farther back the sublayer is, the more intense the warping becomes, until the furthest layer's shape matches the green quad. The sublayers combine in rendering into a single, 3Dish image.

### Light
In the light editor, you edit an image used to render light, called the "light map". The image has only dark areas and light areas. I think of this image as the "occlusion plane", as the contents of the image are used to cast a shadow into the world. You can think of it as the stuff behind the camera that is casting a shadow into the level area.

Light maps are normally pretty simple. If you have an interior room, the light map will be filled with shadow except for some holes where light peeks through. If you have an exterior room, the light map will be filled with shadow except for some stuff that normally sort of looks like shapes found in the level itself, like there's more of what you're seeing in the level behind the camera.

There's also controls on the top-right that control the light angle. This is basically the angle light is coming from for the level. It's probably best to have the dot come from somewhere near the top-left, since tiles have lighting information baked in and they assume the light comes from that area. The size of the circle, known as "light flatness" or "light distance", is basically how "head-on" the light is.

If the ring that the dot resides on is larger, shadows will be longer. It's kind of similar to how shadows look like based on what time of day it is. If it's noon, the sun is directly overhead, so shadows are pretty short and directly cast downwards. That corresponds to a smaller circle in the light editor. If it's sunset or sunrise, the sun is on the horizon and shadows will be longer. That corresponds to a larger circle.

### Effects
These add procedurally generated effects to the level. Many of them manipulate pixels in the rendered image to make stuff, for example, look like they're drooping. Some effects may add stuff in the level for detail, such as plants, grass, wires, chains, e.t.c. They are used a lot in the game.

In the editor, when you have a selected effect, you can paint green pixels in the "effect matrix", which controls the strength of the effect at that area of the level. Pink represents no effect, and green represents a stronger effect. Some effects, namely those in the "Plants (Individual)" category, have you place individual green pixels to create the effect.

Here are descriptions of two effects: BlackGoo and Slime. When you create a BlackGoo effect matrix, it automatically fills it in with maximum green. You then erase parts of the level that aren't solid using a big brush. Some pink is supposed to bleed a little into what is solid. BlackGoo creates a sort of "fog of war" effect, blacking out anything around the edges of a level. The Slime effect, among other effects, is used to make things droop. It is used extensively within the levels of the game.

### Props
Props are sort of like tiles (in fact, you can use tiles as props), except they don't have to be grid-aligned and you can rotate them freely and put them into any of the 30 sublayers (as described previously in the Camera section). You also use props to place destruction effects, a selection of decals, and manually placed tubes and wires. The tubes and wires are under the "Rope-type props" category, and you can physically simulate them while editing to get them to look like they're actually a rope-type object.

### Exporting/Rendering
To export your level to Rain World, select File > Render. It will render a .txt file and one or more .png files into the editor's Levels folder. These rendered files use a different format than the format used for editing levels, so be sure to not have them mixed up with the type of level file you load in Rained.

You then have to copy these files to a mod's "levels" folder to get them into Arena, or a subfolder within the "worlds" folder to get them into story mode. Once you are in them, you will use the Dev Tools to configure the level further, such as changing the color palette of the level, adding sounds, objects, etc. You will also have to edit a world_XX.txt (or use a program like World Editor) file located within the Rain World game directories to connect rooms together and add spawn points.

This guide will not go over the details of doing that since Rained only concerns itself with level editing. (FOR NOW.) There are resources for that online, like in the modding wiki.