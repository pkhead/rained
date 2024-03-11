# RAINED LEVEL EDITOR!!!

Read LICENSE.md for software licenses

Check https://github.com/pkhead/rained/releases for any new releases. If you want to update it, you should remove and replace all the files and folders from this installation folder EXCEPT:
- preferences.json
- imgui.ini
- Data/
- logs/

Also, this is still in beta, so expect changes and bugs. Please report bugs, complaints, and suggestions by creating an issue or pinging @chromosoze in the Rain World Discord server.

The Data folder in this zip does not contain all the level files used in the vanilla game and More Slugcats. I did this to decrease download and decompression time.
If you want all of the levels, you can download them from this repository:
https://github.com/SlimeCubed/Drizzle.Data/tree/community ([Direct download](https://github.com/SlimeCubed/Drizzle.Data/archive/refs/heads/community.zip))

Some usability hints, as I don't yet know a better place to put this:
- In the effects editor, you can drag effect slots up and down in the active effects list.
- You can also delete effect slots by right-clicking on them.
- In the prop editor's freeform warp mode, the outline colors for each prop mean different things about the way they're moved/rotated/scaled:
  - Blue: The scale axis is determined by the prop's rotation - this will keep rectangles rectangles.
  - White: In contrast to blue, the scale axis is not determined by its rotation - it will be distorted if it's rotated.
  - Green: You can move its endpoints. This applies to all rope-type props, and, once added, long-type props.
- If you load in a rope-type prop that has been warped from another editor, you can't transform it until you press "Reset Transform"
- If you scale multiple props, it will force proportional scaling.
- You can use your mouse to manipulate the light circle in the light editor.

Here are some tips on using Dear ImGui (the GUI library I'm using):
- You can freely move and resize docks (e.g. the level dock, the build dock, the shortcuts dock, etc).
- In the prop editor, some inputs for the prop options are drag inputs, meaning that to change them you
  click and drag left/right in the box.
- Experiment with activating inputs with a modifier key down (ctrl, shift, alt).

Hope you like it!