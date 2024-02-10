global projects, ldPrps, gTEprops, gTiles, gLEProps, gEEprops, gLightEProps
global gLEVEL, gLOprops, gLoadedName, gLOADPATH, massRenderSelectL, showControls

on exitFrame me
--  if showControls then
--    sprite(32).blend = 100
--  else
--    sprite(32).blend = 0
--  end if
  
  if _key.keyPressed(56) and _key.keyPressed(48) and _movie.window.sizeState <> #minimized then
    _player.appMinimize()
    
  end if
  if checkExit() then
    _player.quit()
  end if
  
  pth = the moviePath & "LevelEditorProjects" & the dirSeparator
  repeat with f in gLOADPATH then
    pth = pth & f & the dirSeparator
  end repeat
  
  
  txt = "Use arrow keys and space to select projects for rendering."
  put RETURN after txt
  repeat with f in gLOADPATH then
    put f & "/" after txt
  end repeat
  put RETURN after txt
  put RETURN after txt
  put "Projects:" after txt
  put RETURN after txt
  repeat with q = ldPrps.listScrollPos to ldPrps.listScrollPos + ldPrps.listShowTotal  then
    if q > projects.count then
      exit repeat
    else
      if q <> ldPrps.currProject then
        if massRenderSelectL.getPos(pth & projects[q])=0 then
          put projects[q] after txt
        else
          put "*" & projects[q] & "*" after txt
        end if
      else
        if massRenderSelectL.getPos(pth & projects[q])=0 then
          put "<"&&projects[q]&&">" after txt
        else
          put "<*"&&projects[q]&&"*>" after txt
        end if
      end if
      put RETURN after txt
    end if
  end repeat
  
  member("ProjectsL").text = txt
  
  txt = "MASS RENDER"
  put RETURN after txt
  put RETURN after txt
  repeat with q in massRenderSelectL then
    put q after txt
    put RETURN after txt
  end repeat
  
  member("massRenderL").text = txt
  
  
  --lstKeys
  -- ldPrps
  up = _key.keyPressed(126)
  dwn = _key.keyPressed(125)
  lft = _key.keyPressed(123)
  rgth = _key.keyPressed(124)
  if _movie.window.sizeState = #minimized then
    up = false
    dwn = false
    lft = false
    rgth = false
  end if
  
  if (up) and (ldPrps.lstUp=0) then
    ldPrps.currProject = ldPrps.currProject -1
    if ldPrps.currProject < 1 then
      ldPrps.currProject = projects.count
    end if
  end if
  if (dwn) and (ldPrps.lstdwn=0) then
    ldPrps.currProject = ldPrps.currProject +1
    if ldPrps.currProject > projects.count then
      ldPrps.currProject = 1
    end if
  end if
  
  if ldPrps.currProject < ldPrps.listScrollPos then
    ldPrps.listScrollPos = ldPrps.currProject
  else if ldPrps.currProject > ldPrps.listScrollPos + ldPrps.listShowTotal then
    ldPrps.listScrollPos  = ldPrps.currProject - ldPrps.listShowTotal
  end if
  
  if(rgth)and(ldPrps.rgth = 0)and(projects.count > 0)then
    if(chars(projects[ldPrps.currProject], 1, 1) = "#")then
      me.loadSubFolder(projects[ldPrps.currProject])
    end if
  else if(lft)and(ldPrps.lft = 0)then
    if(gLOADPATH.count > 0)then
      gLOADPATH.deleteAt(gLOADPATH.count)
      _movie.go(4)
    end if
  end if
  
  ldPrps.lstUp = up
  ldPrps.lstDwn = dwn
  ldPrps.lft = lft
  ldPrps.rgth = rgth
  
  if _key.keyPressed("A") and _movie.window.sizeState <> #minimized then
    
    
    repeat with project in projects then
      if ( massRenderSelectL.getPos(pth &project) = 0)and(chars(project, 1, 1) <> "#")then
        massRenderSelectL.add(pth & project)
      end if
    end repeat
  else if _key.keyPressed("C") and _movie.window.sizeState <> #minimized then
    massRenderSelectL = []
  else  if (_key.keyPressed(48)) and _key.keypressed("Z") and _key.keypressed("R") or (_key.keyPressed("1")) and _movie.window.sizeState <> #minimized then
    _movie.go(9)
  end if
  
  entr = _key.keyPressed(" ") and _movie.window.sizeState <> #minimized
  
  if (entr)and(ldPrps.lstEnter=0) then
    
    if massRenderSelectL.getPos(pth&projects[ldPrps.currProject])=0 then
      massRenderSelectL.add(pth & projects[ldPrps.currProject])
    else
      massRenderSelectL.deleteOne(pth & projects[ldPrps.currProject])
    end if
  end if
  
  ldPrps.lstEnter = entr
  
  if _key.keyPressed(36) and _movie.window.sizeState <> #minimized then
    global gViewRender, gMassRenderL
    gViewRender = 1
    gMassRenderL = massRenderSelectL.duplicate()
    gMassRenderL.addAt(1, "DUMMY")
    _movie.go(90)
  end if
  
  go the frame
end


on loadSubFolder me, fldrName
  gLOADPATH.add(chars(fldrName, 2, fldrName.length))
  _movie.go(4)
end 
