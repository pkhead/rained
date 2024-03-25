# RAINED LEVEL EDITOR!!!

Read LICENSE.md for software licenses

Check https://github.com/pkhead/rained/releases for any new releases. If you want to update it, you should remove and replace all the files and folders from this installation folder EXCEPT:
- preferences.json
- imgui.ini
- logs/
- themes/
- Your Data folder, if present

Also, this is still in beta, so expect changes and bugs. Please report bugs, complaints, and suggestions by creating an issue on GitHub, or pinging @chromosoze in the Rain World Discord server or Rain World Modding Academy server. I also accept DMs.

Some usability hints, as I don't yet know a better place to put this:
- Check out the menus (on the top) and the shortcuts!
  (In the future I may make keyboard actions in each editor accessible through a menu)
- In the effects editor, you can drag effect slots up and down in the active effects list.
- You can also delete effect slots by right-clicking on them.
- In the prop editor, double-click or press N in the level to add a prop.
- When vertex mode is on in the prop editor, the outline colors for each prop mean different things about the way they're moved/rotated/scaled:
  - Blue: The scale axis is determined by the prop's rotation - this will keep rectangles rectangles.
  - White: In contrast to blue, the scale axis is not determined by its rotation - it will be distorted if it's rotated.
  - Green: You can move its endpoints in vertex mode. This applies to all rope and long props.
  - Red: A rope/long prop that isn't a rectangle. The editor only supports rectangular rope/long props, so you can't transform it until you press "Reset Transform".
- If you scale multiple props, it will force proportional scaling.
- You can use your mouse to manipulate the light circle in the light editor.

Here are some tips on using the GUI elements:
- You can freely move and resize docks (e.g. the level dock, the build dock, the shortcuts dock, etc).
- In the prop editor, some inputs for the prop options are drag inputs, meaning that to change them you
  click and drag left/right in the box.
- Experiment with activating inputs with a modifier key down (ctrl, shift, alt).

Hope you like it!