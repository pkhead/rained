global levelName, gSkyColor, gLoadedName, gFullRender, gViewRender, gDecalColors


on exitFrame me
  if (_key.keyPressed(48) and _movie.window.sizeState <> #minimized)and(gViewRender) then
    _movie.go(9)
  end if
  
  if gViewRender = 0 then
    levelName = gLoadedName
  end if
  
  repeat with q = 0 to gDecalColors.count-1 then
    member("finalImage").image.setPixel(q, 0, gDecalColors[q+1])
  end repeat
  -- sprite(1).color = gSkyColor
  -- put member("levelName").text
  -- if (_key.keyPressed(36))or(gViewRender=0) then
  -- put levelName
  --  if gFullRender then
  --      member("finalfg").image.setPixel(0, 0, member("newPalette").image.getPixel(8,0))--skyColor
  --      member("finalfg").image.setPixel(1, 0, member("newPalette").image.getPixel(8,1))--fogColor
  --      member("finalfg").image.setPixel(2, 0, member("newPalette").image.getPixel(8,2))--blackColor
  --      member("finalfg").image.setPixel(3, 0, member("newPalette").image.getPixel(8,3))--itemcolorColor
  --      member("finalfg").image.setPixel(2, 1, member("newPalette").image.getPixel(8,4))--menuColor
  --      
  --      member("finalfg").image.setPixel(0, 1, member("effectColor1").image.getPixel(4,0))
  --      member("finalfg").image.setPixel(1, 1, member("effectColor2").image.getPixel(4,0))
  --  end if
  -- else
  --  go the frame
  --end if
end

