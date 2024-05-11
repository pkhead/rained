# Rained Level Editor
Read LICENSE.md for software licenses

Also, this is still in beta, so expect changes and bugs. Please report bugs, complaints, and suggestions by creating an issue on GitHub, or pinging @chromosoze in the Rain World Discord server or Rain World Modding Academy server. I also accept DMs. Hope you like it!

## Updates
Check https://github.com/pkhead/rained/releases for any new releases. If you want to update it, you should remove and replace all the files and folders from this installation folder EXCEPT:
- logs/
- config/
- scripts/
- Your Data folder, if present

## Importing Custom Tiles, Props, or Materials
You *can* do so by editing the Init.txt files manually and copying over the graphics and stuff... Or you can do the same thing automagically through the assets tab in the preferences window (File > Preferences). It'll save you some time! Although, if you have a .zip file you unfortunately must decompress it first. I'll think of a way to change that... 

## Quick Start
If you've used a Rain World level editor before, how to use Rained hopefully shouldn't be too obtuse. But here are some notes on new stuff I added:
- Check out the menus (on the top) and the shortcuts!
  (In the future I may make keyboard actions in each editor accessible through a menu)
- In the effects editor, you can drag effect slots up and down in the active effects list.
- You can also delete effect slots by right-clicking on them.
- In the prop editor, double-click (or press C) in the level to add a prop.
- When vertex mode is on in the prop editor, the outline colors for each prop mean different things about the way they're moved/rotated/scaled:
  - Blue: The scale axis is determined by the prop's rotation - this will keep rectangles rectangles.
  - White: In contrast to blue, the scale axis is not determined by its rotation - it will be distorted if it's rotated.
  - Green: You can move its endpoints in vertex mode. This applies to all rope and long props.
  - Red: A rope/long prop that isn't a rectangle. The editor only supports rectangular rope/long props, so you can't transform it until you press "Reset Transform".
- If you scale multiple props, it will force proportional scaling.
- You can use your mouse to manipulate the light circle in the light editor.

If you do not like the keyboard shortcuts, feel free to complain to me. You may also change them through the preferences menu,
accessible by going to the "File" clicking on "Preferences".

Here are some tips on using the GUI elements:
- You can freely move and resize docks (e.g. the level dock, the build dock, the shortcuts dock, etc).
- In the prop editor, some inputs for the prop options are drag inputs, meaning that to change them you
  click and drag left/right in the box.
- Experiment with activating inputs with a modifier key down (ctrl, shift, alt).

## Scripting
Rained has a Lua scripting API. For "documentation", check `scripts/init.lua`, `scripts/definition/rained.defs.lua`, and
look at the built-in scripts as examples.