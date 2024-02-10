global gLOprops, gLeProps, gEnvEditButtons, gLastEnvEditButtons


on exitFrame me
  _movie.exitLock = TRUE
  cols = gLOprops.size.loch
  rows = gLOprops.size.locv
  
  --  member("TEimg1").image = image(cols*5, rows*4, 16)
  --  member("TEimg2").image = image(cols*5, rows*4, 16)
  --  member("TEimg3").image = image(cols*5, rows*4, 16)
  --  
  --  member("levelEditImageShortCuts").image = image(cols*5, rows*5, 1)
  
  repeat with l = 1 to 3 then
    miniLvlEditDraw(l)
  end repeat
  
  gEnvEditButtons = [#w:0, #f:0]
  gLastEnvEditButtons = gEnvEditButtons.duplicate()
end



on checkKey me, key
  rtrn = 0
  gEnvEditButtons[symbol(key)] = _key.keyPressed(key) and _movie.window.sizeState <> #minimized
  if (gEnvEditButtons[symbol(key)])and(gLastEnvEditButtons[symbol(key)]=0) then
    rtrn = 1
  end if
  gLastEnvEditButtons[symbol(key)] = gEnvEditButtons[symbol(key)]
  return rtrn
end