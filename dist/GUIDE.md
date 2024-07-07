# User Guide
## Introduction
This is a user guide that serves as a brief introduction to the level editing process. The level editor has seven different edit modes for each level, and there will be a section covering the basics of each edit mode, simply called "editors" in other level editors.

## Environment Editor
This is basically an adaptation of the controls featured in the "level overview" screen from the original level editor. You may control simple parameters about the room.
- **Tile Random Seed:** The seed tiles use for generating graphics when rendering.
- **Enclosed Room:** Corresponds to the "Default Medium" switch in the original level editor. I don't think this does anything.
- **Sunlight:** If this is off, the room will be cast in darkness. If on, the light map created in the light editor will apply.
- **Water:** On if this room has water. You can change the height of the water by holding down your mouse in the level view.
- **Is Water In Front:** If this is off, water will be behind the first layer.

## Geometry
This is where you edit the room's geometry and some interactable objects. On the right dock, titled "Build", you have a list of things you can add or remove from the level. Each level has three work layers. The first, the closest, is where gameplay happens, and the other two are background layers.

### Non-Shortcut Items
Here is the list of items not related to shortcuts:
- **Wall:** Solid geometry. If a wall is placed on layer two, then background-climbing creatures like blue lizards can walk on it.
- **Air:** Empty space.
- **Toggle Wall/Air:** Corresponds to "Inverse" in the official level editor. Should be obvious.
- **Slope:** A 45-degree slope. Can only be placed on corners.
- **Invisible Wall:** Corresponds to "Glass" in the original level editor.
- **Platform:** A half-block. In game, you may freely pass through it if entering from the bottom, but in order to go through it from above, you have to hold the down button.
- **Horizontal Beam/Vertical Beam**: Otherwise known as poles. Creatures can climb on them.
- **Fissure:** Corresponds to "Crack Terrain" in the official level editor. You can use this to create a special type of tunnel. It's not used in game that often. For normal tunnels, you use air blocks.
- **Waterfall:** You use this to create decorative waterfalls. Water flows down from the cell that the waterfall object is placed in.
- **Batfly Hive:** Can only be placed on the ground. It makes those spiky white things that batflies burrow into.
- **Forbid Fly Chain:** Placed on the ceiling, and prevents batflies from hanging onto them in chains.
- **Garbage Worm:** Placed in the ground, and can be used to mark a location where a Garbage Worm spawns.
- **Worm Grass:** Placed on the ground. Spawns worm grass. Bigger worm grass is made by stacking them up vertically.
- **Copy Backwards:** Copies the geometry of highlighted area one layer backwards.

### Shortcuts
Shortcuts, otherwise known as "pipes", are the things you go through to transport to a different room or a different place in the same room.
Shortcut entrances have specific requirements in order to be recognized as valid.

First, the 3x3 area of geometry around the shortcut entrance must be made of solid blocks, save for one block, where you enter the shortcut from, which should be either air or a platform (platforms are used for downward-facing shortcut entrances). So, for example, to create a shortcut entrance into a wall, you will place a single air block into the wall, and replace the next solid block in the hole with the shortcut entrance. Second, you must have a shortcut dot placed in the direction of the shortcut entrance. So, to illustrate, if you have an air block to the left of the shortcut entrance, you will need to place a shortcut dot to the right of the entrance.

If you have created a shortcut entrance correctly, the graphic for it will show an arrow pointing in a singular direction. Afterwards, you will use the "Shortcut Dot" item to create a path for the shortcut. Each shortcut dot connects to the next. They will only connect on its four sides, so you can't have paths that go diagonally.

The path will then be concluded by one of several shortcut items. Which shortcut item the path ends in changes the behavior of the shortcut.
- **Shortcut Entrance:** Creates a two-way connection between two points in the same room.
- **Room Entrance:** Creates a connection to another room.
- **Creature Den:** Known as the Dragon Den in the official level editor, a place for creatures other than the slugcat to spawn and hibernate in during the rain.
- **Whack-a-mole Hole:** When a creature other than the slugcat enters this shortcut, they will be warped to another whack-a-mole hole in the same room, chosen at random.
- **Scavenger Hole:** Allows scavengers to spawn in or travel to this room as they wander through a region.
- **Shortcut Dot:** You can end a shortcut path with a shortcut dot, in which case the shortcut will not be enterable.

## Tiles
Tiles are used to make level geometry actually look like something. Without them, levels will just look like they're made out of big blocks of concrete.
Each tile is a specific asset hand-crafted by someone. These include things like huge beams, pipes, stones, metal things, machinery-looking stuff, and other set pieces.

Each tile has a defined width and height, as well the geometry the tile matches up with, known as "geometry requirements". These geometry requirements are visualized by a set of green-colored outlines shown behind the tile before placing them in the level editor. A full square outline represents that it needs a solid block here, a cross represents that it needs air here, and an empty space represents that it doesn't care what goes here. Tiles may also need slopes at certain locations too.

It's laborious to manually place geometry to fit a tile, so there is a quick way to deal with that. If you hold G while placing a tile, it will automatically place the geometry for you. Likewise, if you hold G while removing a tile, it will clear up all the geometry that the tile requires. You may also hold F while placing a tile to force-place it, even if the geometry doesn't match up with the tile's requirements. You can even have tiles overwrite portions of an already existing tile when force-placing, as long as it doesn't mess with the tile root, which is at the center of every tile.

The tile editor also allows you to place materials. These are represented in the editor by colored squares centered in each cell, but when rendering they will look fancier. You may also set the default material for a level, in which case any cells that don't have a material set will use that material. 

## Cameras
Rooms in the Rain World game are all red-colored, pre-rendered, static images. A camera defines an area of the level that the player can view. In-game, when the player intersects with the rectangular area a camera inhibits, the game will switch to viewing the level through that camera.

The outer black rectangle of the camera shows the viewable area of the camera when Rain World is played in a 16:9 resolution. The thick inner green rectangle (red-colored in the official level editor) shows the viewable area when Rain World is played in a 4:3 resolution. It's also the boundary between cameras when the game considers which camera to switch over to. Usually cameras are arranged in a grid, but this isn't required (unless you want the SBCameraScroll mod to work properly).

### Camera Angles
Cameras also have a thing called "camera angles". These are controlled by the green rings that appear on the corners of a camera when you have one selected. Moving them around basically warps the perspective of the level when rendering so it kind of looks like it's viewed at an angle, which can look pretty cool.

These rings control the offsets of the corners of the green quad. The green quad actually represents the shape of the furthest sublayer for the camera. Each screen/camera in a Rain World level is rendered in 30 sublayers, as each of the three work layers are subdivided into ten sublayers for rendering. The fifth closest sublayer is always a perfect rectangle, but the farther back the sublayer is, the more intense the warping becomes, until the furthest layer's shape matches the green quad. The sublayers combine in rendering into a single, 3Dish image.

## Light
In the light editor, you edit an image used to render shadows onto the level, called the "light map". The image has only dark areas and light areas. The dark areas of the image, represented in red in the editor, cast a shadow into the world. You can think of it as the stuff behind the camera that is casting a shadow into the viewable area.

Do note that the shadow projection shown in the light editor is not entirely accurate to what it will look like in-game, as when rendering the different depths of each sublayer will be accounted for.

Light maps are usually fairly simple. As a basic template, if you have an interior room, the light map will be filled with shadow except for some holes where light peeks through. If you have an exterior room, the light map will be filled with light except for some stuff that normally sort of looks like shapes found in the level itself, like there's more of what you're seeing in the level behind the camera.

## Light Angle
There's also controls on the top-right that control the light angle. This is basically the angle light is coming from. It's probably best to have the dot come from somewhere near the top-left, since tiles have lighting information baked in and they assume the light comes from that area.

The radius of the ring with the dot on it basically controls how "head-on" the light angle is. This is known as "light flatness" or "light distance". If the radius of the ring that the dot resides on is larger, shadows will be longer. It's kind of similar to how shadow lengths change based on what time of day it is. If it's noon, the sun is directly overhead, so shadows are short and cast directly downwards. That corresponds to a smaller circle in the light editor. If it's sunset or sunrise, the sun is closer to the horizon and shadows will be longer. That corresponds to a larger circle.

## Effects
These add procedurally generated effects to the level. Many of them manipulate pixels in the rendered image to make stuff, for example, look like they're drooping. Some effects may add decorative objects in the level, such as plants, grass, wires, chains, etc.

To create an effect, find the effect you want in the "Add Effect" window and click on it. The effect will be added to the "Active Effects" list. You can reorder each effect in the active effects list to change which order they are applied in. For example, you may not want your plants to have Rust and Slime on it, so you would have your Rust and Slime effect above your plant effect. You may also create multiple effects of the same type, which is useful for if you want an effect to have different options based on what area it's applied in, or if you only want an effect for a specific layer to be applied to a specific area.

In the editor, when you have an effect selected, you can paint green pixels in the "effect matrix" overlaid on top of the level view, which controls the strength of the effect at that area. Pink represents no effect, and green represents a stronger effect. Some effects, namely those in the "Plants (Individual)" category, have you place individual pixels instead of brushing over areas to create the effect.

BlackGoo is an especially notable effect. When you create a BlackGoo effect, it automatically fills in its matrix with maximum green. You then erase parts of the level that aren't solid using a big brush, though some pink is supposed to bleed into what is solid. BlackGoo creates a sort of "fog of war" effect, blacking out anything around the edges of a level, and it is used in pretty much every room in the game. An exception to this I can think of is Underhang, which is already dark enough that BlackGoo is not necessary.

## Props
Props are sort of like tiles, except they don't have to be grid-aligned and you can rotate them freely and put them into any of the 30 sublayers (as described previously in the Camera section). You also use props to place destruction effects, a selection of decals, and manually placed tubes and wires. The tubes and wires are under the "Rope-type props" category, and you can physically simulate them while editing to get them to look like they're actually a rope-type object. If you press F, it will toggle "Vertex Mode", which if on will allow you to move the vertices of a prop, or, if the prop is a rope or long prop, its endpoints.

Each prop has a certain amount of options you can configure in regards to their rendering. Here is a list of all possible configuration options:
- **Render Order:** For props with the same depth offset, props with a lower value of this render above props with a higher value.
- **Depth Offset:** This is the sublayer that the prop is in (though, props actually span multiple sublayers so this isn't technically accurate). Ranges from 0 to 29.
- **Seed:** This is the random seed the prop uses when generating its graphics. You can change this to make certain props look different than other instances of the same prop.
- **Render Time:** This controls at what stage the prop is rendered in. As far as I'm aware, this option is only relevant if you have "Apply Color" turned on for the prop.
- **Custom Depth:** Allows you to control the size of depth in regards to sublayers.
- **Variation:** This changes the graphic of the prop.
- **Custom Color:** This allows you to use a custom color for the prop. Only applies to decals.
- **Apply Color:** A checkbox you can switch on and off. If it is off, it will use a color from the room palette.

## Playing Your Level
To export your level to Rain World, select `File > Render` from the menubar (on the top). This is a process that takes a while. It will render a .txt file and one or more .png files into Levels folder. You can view this folder by clicking `View > Show Render Folder...` from the menubar. These rendered files use a different format than the format used for editing levels, so be sure to not have them mixed up with the type of level file you load in Rained.

You have to copy these files to a mod's "levels" folder to get them into Arena, or a subfolder within the "worlds" folder to get them into story mode. Once you are in them, you will use the Dev Tools to configure the level further, such as changing the color palette of the level, adding sounds, objects, etc. You will also have to edit a world_XX.txt file, or use a program like World Editor, to connect rooms together and add creature spawns.

This guide will not go over the details of doing that since Rained only concerns itself with level editing. But there are resources for that on the World Wide Web, such as [this wiki page about the Dev Tools](https://rainworldmodding.miraheze.org/wiki/Dev_Tools) as well as [this page in regards to the world file format](https://rainworldmodding.miraheze.org/wiki/World_File_Format). Happy lediting!