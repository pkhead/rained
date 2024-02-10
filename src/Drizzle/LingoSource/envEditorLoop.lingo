global gLOprops, gEnvEditorProps, showControls


on exitFrame me
  if (showControls) then
    sprite(206).blend = 100
  else
    sprite(206).blend = 0
  end if
  
  script("levelOverview").goToEditor()
  
  if gLOprops.size.locH > gLOprops.size.locV then
    fac = 1366.0/gLOprops.size.locH
  else
    fac = 768.0/gLOprops.size.locV
  end if
  rct = rect(1366/2, 768/2, 1366/2, 768/2) + rect(-gLOprops.size.locH*0.5*fac, -gLOprops.size.locV*0.5*fac, gLOprops.size.locH*0.5*fac, gLOprops.size.locV*0.5*fac)
  
  
  
  repeat with q = 1 to 2 then
    sprite(q).rect = rct
  end repeat
  
  sprite(201).rect = rct
  sprite(202).rect = rct
  sprite(204).rect = rct
  
  if gEnvEditorProps.waterLevel >= 0 then
    sprite(203).visibility = true
    sprite(205).visibility = true
    h = rct.bottom - (gEnvEditorProps.waterLevel+gLOprops.extraTiles[4]+0.5)*fac
    sprite(203).rect = rect(rct.left-2,  h-1, rct.right+2, 768)
    sprite(205).rect = rect(rct.left-2, h-1, rct.right+2, 768)
    if gEnvEditorProps.waterInFront then
      sprite(203).blend = 5
      sprite(205).blend = 50
    else
      sprite(203).blend = 50
      sprite(205).blend = 5
    end if
  else
    sprite(203).visibility = false
    sprite(205).visibility = false
  end if
  
  if (_key.keyPressed("l") and _movie.window.sizeState <> #minimized)then
    waterLevel = gLOprops.size.locv - gLOprops.extraTiles[2] - gLOprops.extraTiles[4] - (_mouse.mouseLoc.locV/fac).integer
    gEnvEditorProps.waterLevel = waterLevel
  end if
  
  if(script("envEditorStart").checkKey("w")) then
    if gEnvEditorProps.waterLevel = -1 then
      gEnvEditorProps.waterLevel = gLOprops.size.locv/2
    else
      gEnvEditorProps.waterLevel = -1
    end if
  end if
  
  if(script("envEditorStart").checkKey("f")) then gEnvEditorProps.waterInFront = 1 - gEnvEditorProps.waterInFront
  go the frame
end




