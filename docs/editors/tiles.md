# Tiles
![Level shown in the tiles mode.](../img/tile-editor.png)
The tile editor is where you decorate the level with materials and tiles. Editing tiles and materials is a crucial part of the level editing process, and is normally done after creating an initial block-out of the level in the geometry editor, and usually afterwards is worked on in tandem with the geometry editor. Forming a basic understanding of the principles of tiling is key to designing a good-looking level (although this manual will not delve into the general principles of tile design).

## Materials
TODO

## Tiles
A tile is a premade art asset that you can place in a level. Each tile was drawn by someone in an image editor or paint program, depicting things such as metal beams, pipes, stones, machines, and other set pieces.

Functionally, they behave similarly to tiles in tile-based games/level editors, but there are a few things specific to Rain World's version of tiles.

1. Tiles can be of any grid-aligned width and height.
2. Each tile has a geometry specification, which limits/forces the pattern of geometry the tile can be placed on.
3. Tiles can have either one or two layers of depth.
4. Parts of a tile can be removed and the tile will still render as normal.


In order to place a tile, you first must select the tile you want to place in the tile selector, depicted below. The left column displays tile categories, and the right column displays tiles in the currently selected tile category. Hovering over an item in the right column will show a pop-up preview of the tile, and clicking on it will select that tile.

<figure markdown="span">
    ![The tile selector.](../img/tile-selector.png)
    <figcaption>The tile selector.</figcaption>
</figure>

With a tile selected and your mouse over the level view, a preview of the tile will display over your mouse which is called the "tile cursor". Pressing down the left mouse button will place down the tile at that position. and pressing down the right mouse button will remove the tile that is being hovered over.

You may switch work layers either by pressing <kbd>Tab</kbd> or interacting with the "Work Layer" input box located on the very top of the "Tile Selector" window. 

### Geometry Specifications/Requirements
Each tile specifies the pattern of geometry that the tile occupies. This is referred to as "geometry/tile specs", "geometry/tile requirements", or simply "specs". These specs in Rained are shown as green outlines behind the tile cursor in the level view. The specs for the first layer are shown as a bright green, and the specs for the second layer, if demanded by the tile, are shown as a dark green.

<figure markdown="span">
    ![Tile cursor preview of "Very Large Beam"](../img/tile-cursor-white.png)
</figure>

The requirement of each individual cell should be obvious---as it shows an outline of the required geometry type at that cell---except for two cases:

- **Unspecified**: This will not show an outline at a given cell. This means that any kind of geometry is tolerated at that space, and if the tile is placed down with the "Force Geometry" modifier that space will not be modified.
- **Air**: This will show as a cross, meaning that the geometry at that space must be an air cell.

Normally, you will not be able to place down a tile if the geometry under the cursor does not meet the specifications for the selected tile. In this case, the cursor tile preview will be colored red instead of white. However, you can force Rained to place down the tile regardless of the underlying geometry if you have either the "Force Placement" or "Force Geometry" modifiers active.

In order to activate the "Force Placement" modifier, you must have the <kbd>F</kbd> key held down. Once this is active, you may
place down a tile regardless of if the underlying geometry fits the requirements of tile's specs. The "Force Geometry" modifier, activated by holding down the <kbd>G</kbd> key, will instead, upon placement, modify the geometry underneath the tile to fit the tile specs. This way, you don't have to labouriously manually place the geometry down for any large or complex tiles. Additionally, if you remove a tile with "Force Geometry" active, it will remove the tile's required geometry along with the tile itself.

### Heads and Bodies
TODO:

- Proper instances of a tile are composed of a tile head and zero or more of its tile bodies.
- "Tile Heads" view option
- You aren't allowed to place a tile over a tile head whatsoever.
- R key

### Tile Graphics
The image displayed by default in Rained for the visual representation of tiles is not what actually gets rendered in-game. Rather, it is a crude representation of the tile. These images are, by defualt, used in place of the real tile graphics while editing the level due to concerns of performance and video memory. However, you can choose to have Rained render tile graphics using their actual in-game representation by toggling `View > Tile Graphics`. Out of the box, tiles will be colored using their respective category colors, but for a more accurate in-game representation you may also enable palette rendering through the `View > Palettes` window.

Note that the tile graphics preview is inaccurate in regards to geometry rendering. When rendering, any cell that is occupied by a tile head or tile body will not have their geometry rendered so that the tile can display properly. This therefore means that if a tile requires solid geometry for any cell but has empty space in that portion of the graphic, Rained will display that cell as solid but in reality that cell will be invisible when rendering. A specific example of a tile that exhibits this behavior is "Ventilation Box Empty".

<figure markdown="span">
    ![Ventilation Box Empty in Rained](../img/ventbox-empty-preview.png)
    <figcaption>The tile as is displayed in Rained.</figcaption>
</figure>

<figure markdown="span">
    ![Ventilation Box Empty in-game](../img/ventbox-empty-render.png)
    <figcaption>The tile render as is shown in-game. Note how the solid geometry in the tile is not rendered.</figcaption>
</figure>

## Autotiles
TODO