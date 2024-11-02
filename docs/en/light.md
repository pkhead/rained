# Light
In the light editor, you edit an image used to render shadows onto the level, called the "light map". The image has only dark areas and light areas. The dark areas of the image, represented in red in the editor, cast a shadow into the world. You can think of it as the stuff behind the camera that is casting a shadow into the viewable area.

Do note that the shadow projection shown in the light editor is not entirely accurate to what it will look like in-game, as when rendering the different depths of each sublayer will be accounted for.

Light maps are usually fairly simple. As a basic template, if you have an interior room, the light map will be filled with shadow except for some holes where light peeks through. If you have an exterior room, the light map will be filled with light except for some stuff that normally sort of looks like shapes found in the level itself, like there's more of what you're seeing in the level behind the camera.

## Light Angle
There are also controls on the top-right that control the light angle. This is basically the angle light is coming from. It's probably best to have the dot come from somewhere near the top-left, since tiles have lighting information baked in and they assume the light comes from that area.

The radius of the ring with the dot on it basically controls how "head-on" the light angle is. This is known as "light flatness" or "light distance". If the radius of the ring that the dot resides on is larger, shadows will be longer. It's kind of similar to how shadow lengths change based on what time of day it is. If it's noon, the sun is directly overhead, so shadows are short and cast directly downwards. That corresponds to a smaller circle in the light editor. If it's sunset or sunrise, the sun is closer to the horizon and shadows will be longer. That corresponds to a larger circle.