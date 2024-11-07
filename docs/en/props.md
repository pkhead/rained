# Props
**TODO:** screenshot of the props editor

Props are used similarly to tiles but with a higher degree of freedom for placement. They don't have to be grid-aligned, can be rotated freely, and can be
positioned into any of the 30 sublayers rather than only one of the 3 work layers. Additionally, props are also used to make destruction effects and place
physically simulated ropes, tubes, and wires.

!!! tip

    Unlike tiles, props do not affect geometry. If you want a prop to be collidable, you must place fitting geometry and make it invisible. This is done by either using the Glass material, or filling them with an empty tile.

## Editing props
You first must select the prop you want to place in the prop catalog, similar to how you would with tiles. Then, to place it down, either right-click in the level view or press the <kbd>C</kbd> key.

**TODO:** screenshot of a selected prop

With a prop selected, you can move the prop by clicking and dragging it, scale the prop by dragging one of its corners or edges, and rotate it by either clicking
and dragging the extruding widget, or holding down the <kbd>Q</kbd> or <kbd>E</kbd> keys.

Multiple props can be selected at once, by holding down the <kbd>Shift</kbd> key while selecting a new one. Deselection is done by doing the same action on a prop
that was already selected. In addition, if you have more than one prop overlaid on top of the other, and you want to select a specific one, you can select an
individual prop of your desire by double-clicking and selecting it from the pop-up menu.

**TODO:** demonstration of this pop-up menu

!!! info

    If you are annoyed with accidentally selecting props from layers you don't want to, the **Prop selection layer filter** option can be used to change which layers you are allowed to select props from relative to the current work layer.

You may also warp a prop as if editing it as an arbitrary quadrilateral. You do so by first activating Vertex Mode, done by pressing the <kbd>F</kbd> key.
The outlines of props will then either turn white or green. You may manipulate the corners of white props as you please. Green-colored props are explained later.

## Prop types
There are three different prop types:

- Standard
- Longs
- Rope-type props

This prop type determines how you can edit and interact with the prop.

### Standard
These props are signaled by a blue outline when they are selected. When Vertex Mode is on, they will instead have a white outline, meaning that you can warp
them as usual.

#### Destruction props
These render differently than other props, rendering subtractively rather than additively. The presence of them in your
level adds meaning to the **Render Order** option.

#### Decals
When rendering, decals paste its texture to whatever solid area the prop is intersecting with.

### Longs
These props are signaled by a green outline when they are selected. You edit them as you would with standard props when Vertex Mode is off, but if Vertex Mode
is on, you can't warp them as you would with a standard prop. Instead, you can only move their endpoints.

**TODO:** image of a long prop

### Rope-type props
Rope-type props are similar to long props in terms of editing, but they also include a physically simulated rope. Each segment of the rope is displayed as
a dot. To simulate the rope, hold down the Simulate button or the <kbd>Space</kbd> key. The rope will freeze once the button is let go.

## Prop options

The following is a list of all possible prop options. Note that not all props will have every option available.

- **Render Order:** Affects the order the prop is rendered in if a destruction prop is in the level.
- **Depth Offset:** The sublayer position of the prop, ranging from 0-29. The next work layer is reached every 10 units.
- **Seed:** Seed used for procedural generation.
- **Render Time:** Used to control whether or not the prop renders before or after effects.
- **Custom Depth:** The size of the prop in terms of sublayers.
- **Variation:** The variation of the prop used.
- **Custom Color:** The color of the decal prop.
- **Apply Color:** True if the prop should use its custom color.

The following options are applicable only to rope-type props:

- **Flexibility:** Controls the flexibility of the prop. Scaling the prop vertically does the same thing.
- **Release:** Controls which end of the prop is not attached.
- **Thickness:** The thickness of the rope.

<!--
Multiple props can be selected at once

, except that they don't have to be grid-aligned, can be rotated freely, 
Props are sort of like tiles, except they don't have to be grid-aligned and you can rotate them freely and put them into any of the 30 sublayers (as described previously in the Camera section). You also use props to place destruction effects, a selection of decals, and manually placed tubes and wires. The tubes and wires are under the "Rope-type props" category, and you can physically simulate them while editing to get them to look like they're actually a rope-type object. If you press F, it will toggle "Vertex Mode", which if on will allow you to move the vertices of a prop, or, if the prop is a rope or long prop, its endpoints.

Each prop has a certain amount of options you can configure in regards to their rendering. Here is a list of all possible configuration options:

- **Render Order:** For props with the same depth offset, props with a lower value of this render above props with a higher value.
- **Depth Offset:** This is the sublayer that the prop is placed in. Ranges from 0 to 29.
- **Seed:** This is the random seed the prop uses when generating its graphics. You can change this to make certain props look different than other instances of the same prop.
- **Render Time:** This controls at what stage the prop is rendered in. As far as I'm aware, this option is only relevant if you have "Apply Color" turned on for the prop.
- **Custom Depth:** Allows you to control the size of depth in regards to sublayers.
- **Variation:** This changes the graphic of the prop.
- **Custom Color:** This allows you to use a custom color for the prop. Only applies to decals.
- **Apply Color:** A checkbox you can switch on and off. If it is off, it will use a color from the room palette.
-->