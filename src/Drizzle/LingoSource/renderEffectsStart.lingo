global vertRepeater, r, gLEprops, gEEprops, gTEprops, gTiles, keepLooping, gLOprops

on exitFrame me
  type tm: number
  type val: number

  if _key.keyPressed(56) and _key.keyPressed(48) and _movie.window.sizeState <> #minimized then
    _player.appMinimize()
    
  end if
  if checkExit() then
    _player.quit()
  end if
  if checkExitRender() then
    _movie.go(9)
  end if
  
  
  
  tm = _system.milliseconds
  
  repeat with q = 0 to 29 then
    sprite(50-q).loc = point((1366/2)-q, (768/2)-q)
    val = (q.float+1.0)/30.0
    --  put val
    sprite(50-q).color = color(val*255, val*255, val*255)
  end repeat
  
  sprite(57).visibility = 0
  sprite(58).visibility = 0
  
  vertRepeater = 100000
  
  
  
  
  if gEEprops.effects.count >  0 then
    r = 0
    keepLooping = 1
  else 
    go(56)
  end if
end



