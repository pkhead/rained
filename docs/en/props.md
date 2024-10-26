# Props
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