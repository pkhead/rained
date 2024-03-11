  RAINED LEVEL EDITOR!!!

Read LICENSE.md for software licenses

Check [https://github.com/pkhead/rained/releases] for any new releases. If you want to update it, you should remove and replace all the files and folders from this installation folder EXCEPT:
- preferences.json
- imgui.ini
- Data/
- logs/

Also, this is still in beta, so expect changes and bugs. Please report bugs, complaints, and suggestions by creating an issue or pinging @chromosoze in the Rain World Discord server.

Some usability hints, as I don't yet know a better place to put this:
- In the effects editor, you can drag effect slots up and down in the active effects list.
- You can also delete effect slots by right-clicking on them.
- In the prop editor's freeform warp mode, the outline colors for each prop mean different things about the way they're moved/rotated/scaled:
  - Blue: The scale axis is determined by the prop's rotation - this will keep rectangles rectangles.
  - White: In contrast to blue, the scale axis is not determined by its rotation - it will be distorted if it's rotated.
  - Green: You can move its endpoints. This applies to all rope-type props, and, once added, long-type props.
- If you load in a rope-type prop that has been warped from another editor, you can't transform it until you press "Reset Transform"
- If you scale multiple props, it will force proportional scaling.

Other than that, I don't have much to put on here. Hope you like it!