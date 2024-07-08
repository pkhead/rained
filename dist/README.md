# Rained Level Editor
LICENSE.md contains licenses for the software dependencies I have used.

Don't hestiate to report any bugs, complaints, and suggestions by creating an issue on GitHub, or pinging @chromosoze in either the main Rain World Discord server or one of both Rain World modding servers. I also accept DMs, but as I don't get pinged for DM requests, it would take a while for me to respond.

Also, if you are on Windows and Rained fails to even open a window when launching it, it's probably because you are missing the Microsoft Visual Studio C++ runtime package, which is needed for its dependencies. It can be installed [here](https://aka.ms/vs/17/release/vc_redist.x64.exe).

Hope you enjoy!

## Updates
Check https://github.com/pkhead/rained/releases for any new releases. If you want to update it, you should remove and replace all the files and folders from this installation folder EXCEPT:
- config/
- scripts/
- Your Data folder, if present

## Importing Custom Tiles, Props, or Materials
You *can* do so by editing the Init.txt files manually and copying over the graphics and stuff... Or you can do the same thing automagically through the assets tab in the preferences window (File > Preferences). It'll save you some time! Although, if you have a .zip file you unfortunately must decompress it first. I'll think of a way to change that...

## User Interface
Here are some tips on using the GUI elements:
- You can freely move and resize docks.
- Dragging a window while holding shift won't show the docking options.
  - They also don't show up if you drag a window by its contents rather than by its title bar.
- In the prop editor, some inputs for the prop options are drag inputs, meaning that to change them you
  click and drag left/right in the box.
- Experiment with activating inputs with a modifier key down (ctrl, shift, alt).

## How to Use
If you've used a Rain World level editor before, learning how to use this software shouldn't be too obtuse. However, if you're new to this, then I have a brief guide on how to do level editing in Rained [here](GUIDE.md).

Assuming you have the expertise, here is a quick list of things that are different:
- Check out the menus (the bar on the top of the window) and the shortcut list.
- In the effects editor, you can drag effect slots up and down in the active effects list.
- You can also delete effect slots by right-clicking on them.
- In the light editor, you scale and rotate your brush by holding down a key and moving your mouse.
- You can also use your mouse to manipulate the light ring for the light angle.

The prop editor has some pretty significant changes in design when compared to RWE+ or the Lingo editor. It works akin to transform tools
in other software like Photoshop.

In the prop editor, right-click (or press C) in the level to add a prop. Afterward, moving a prop is done by simply selecting them and dragging them around. Scaling is done by dragging their corners.

If you want to warp them, you enable vertex mode by pressing F (by default), and, for normal props, you may warp them by dragging their corners. When vertex mode is on, the outline colors for each prop mean different things about the way they're moved/rotated/scaled:
  - Blue: The scale axis is determined by the prop's rotation - this will keep rectangles rectangles.
  - White: In contrast to blue, the scale axis is not determined by its rotation - it will be distorted if it's scaled while rotated.
  - Green: This is a rope or a long prop, and you can move its endpoints in vertex mode.
  - Red: A rope/long prop that has been warped in a different level editor. The editor only supports rectangular rope/long props, so you can't transform it until you press "Reset Transform".

If you do not like the default keyboard shortcuts, feel free to complain to me. You may also change them through the preferences menu,
accessible by going to the "File" menu, then clicking on "Preferences".

## Autosaving
Rained does not have a system that auto-saves the level periodically. However, it does automatically save the current state
of the level to a different file if it catches a fatal exception and crashes, which should be an adequate alleviator.
I would recommend pressng Ctrl+S every minute anyway.

## Scripting
Rained has a Lua scripting API, used for autotiling and level manipulation.
For "documentation", check `scripts/init.lua`, `scripts/definition/rained.defs.lua`, and look at the built-in scripts as examples.