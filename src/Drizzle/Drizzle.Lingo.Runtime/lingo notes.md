# Notes about Lingo/Director

This just contains various bits of detail I figured I should write down about lingo/director.

## Math

Lingo has two number types, integers and floats.
* Integers are standard 32 bit signed integers.
* Floats are IEEE 754 double precision. They can contain Infinity, -Infinity and NaN (`INF`, `-INF` and `NAN` is used when printed).

Doing operations on int generally returns int (operators do including div, `sqrt()` does, `tan()` doesn't which is probably fair). Float returns float.

`point` and `rect` remember which type their components are constructed out of. So `point(0, 0)` is different from `point(0.0, 0)` in calculations (accessing `locH` will give a float instead of a number, and all the consequences of that...). This is absolutely insane but hey.

Constructing a rect from two points rounds the points' components though. Yeah. *Sigh*.

## Images

* Images are 0-indexed, so 0,0 is the top left corner. Stuff like `copyPixels()` rectangles all uses sane indexing.
* The bitmap view in Director doesn't put 0,0 at the top left corner, you have to scroll to the top left to view the whole image.
* **image depths:**
    * 1/2/4/8 are all indexed color.
    * 16 is RGBA5551 or similar.
    * 32 is RGBA32 with alpha channel only one bit still.
* out-of-bounds reads with `copyPixels()` will basically be treated as if the out of bounds region is empty.
* Copying to a 1-bit image seems somewhat intelligent about whether to pick white or black based on input color. Primary colors (like pure red) seem to be black, but secondaries like yellow are white. Probably just a specific case of palettization, but still.
* Masked copies position the mask at the top left corner at the source image, and scale the mask 1:1 with the source image.
  * If you have a mask of equal size as a source image and sample the right half of that image, you get the right half of the mask 