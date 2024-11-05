# Effects
The effects editor is used to add procedurally generated effects to the level. Many of them manipulate pixels in the rendered image to make stuff, for example, look like they're drooping. Some effects may add decorative objects in the level, such as plants, grass, wires, chains, etc.

<figure markdown="span">
    ![The effects catalog](img/effects-catalog.png)
    <figcaption>The effects catalog and list of active effects.</figcaption>
</figure>

To create an effect, find the effect you want in the "Add Effect" window and click on it. A new instance of the effect will be added to the "Active Effects" list. Multiple instances of the same effect can be added. Afterwards, you may reorder the order that effects are rendered by clicking and dragging, or by pressing the "Move Up" and "Move Down" buttons. They can also be deleted by right-clicking on them or pressing the "Delete" button.

!!! tip

    The two most commonly used effects are BlackGoo and Slime. BlackGoo is used to create a "fog of war" effect, blacking out any solid geometry under it, and permeates the walls of indoor rooms rooms more than outdoor rooms. Slime is a basic erosion drooping effect, and is used in virtually every level.

## Effect Matrix
<figure markdown="span">
    ![Example BlackGoo effect matrix.](img/blackgoo.png)
    <figcaption>Example BlackGoo effect matrix.</figcaption>
</figure>

In the editor, when you have an effect selected, you can paint green pixels in the "effect matrix" overlaid on top of the level view, which controls the strength of the effect at that area. Pink represents no effect, and green represents a stronger effect. Some effects, namely those in the "Plants (Individual)" category, have you place individual pixels instead of brushing over areas to create the effect.

## Options
Effects can be configured. TODO.

### Common options
Effects can use their own custom options, but the following is a list of common ones:

- **Layers:** The layers that the effect will render in.
- **3D:** TODO: what does 3d do
- **Affect Gradients And Decals:** TODO: what does this do
- **Color:** The effect color to use. "Dead" means that it'll instead use the normal level palette, making it indeed look gray and dead.
- **Seed:** Seed used for random number generation.