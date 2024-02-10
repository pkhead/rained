global gLOprops, gEditLizard, gLevel, gLeProps, gPrioCam, newSize, showControls

on exitFrame me
  _movie.exitLock = TRUE
  
  --  if showControls then
  --    sprite(120).blend = 100
  --    sprite(121).blend = 100
  --  else
  --    sprite(120).blend = 0
  --    sprite(121).blend = 0
  --  end if
  
  newSize[1] = gLOprops.size.loch
  newSize[2] = gLOprops.size.locv
  cols = gLOprops.size.loch
  rows = gLOprops.size.locv
  -- member("levelEditImage1").image = image(cols*5, rows*5, 1)
  -- member("levelEditImage2").image = image(cols*5, rows*5, 1)
  -- member("levelEditImage3").image = image(cols*5, rows*5, 1)
  
  
  -- sprite(21).member = member("libPal" & string(gLOprops.pal))
  --sprite(22).member = member("ecol" & string(gLOprops.eCol1))
  --sprite(23).member = member("ecol" & string(gLOprops.eCol2))
  
  gEditLizard = ["pink", 0, 0, 0]
  script("levelOverview").nextHole()
  member("addLizardTime").text = "0"
  member("addLizardFlies").text = "0"
  sprite(43).color = color(255, 0, 255)
  
  sprite(2).loc = point(312,312)+point(-1000+1000*gLevel.defaultTerrain, 0)
  
  sprite(56).visibility = 1
  sprite(57).visibility = 1
  sprite(58).visibility = 1
  sprite(59).visibility = 1
  
  sprite(67).loch = (gLEVEL.waterDrips*8)+50
  sprite(68).loch = (gLEVEL.maxFlies*10)+50
  sprite(69).loch = (gLEVEL.flySpawnRate*4)+50
  member("lightTypeText").text = gLevel.lightType
  sprite(70).loch = (gLOProps.tileseed)+50
  
  script("levelOverview").updateLizardsList()
  
  repeat with q = 0 to 29 then
    member("layer"&q).image = image(1,1,1)
    member("layer"&q&"sh").image = image(1,1,1)
  end repeat
  
  l = ["Dull", "Reflective", "Superflourescent"]
  member("color glow effects").text = l[gLOprops.colGlows[1]+1] && return && l[gLOprops.colGlows[2]+1]
  
  sprite(22).rect = rect(-100, -100, -100, -100)
  if(gPrioCam = 0) then
    member("PrioCamText").text = ""
  else 
    member("PrioCamText").text = "Will render camera " & gPrioCam & " first"
  end if
  
  the randomSeed = _system.milliseconds
end