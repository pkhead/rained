global gLOprops, gBlurOptions, gEditLizard, gLevel, gLEprops, gLoadedName, levelName, gPrioCam, snapToGrid, preciseSnap, stg, ps, lvlPropOutput, showControls, hideHelpClick

on exitFrame me
  if (showControls) then
    sprite(120).blend = 100
    sprite(121).blend = 100
    sprite(150).blend = 100
    sprite(151).blend = 100
  else
    sprite(120).blend = 0
    sprite(121).blend = 0
    sprite(150).blend = 0
    sprite(151).blend = 0
  end if
  
  gLOprops.lastMouse = gLOprops.mouse
  gLOprops.mouse = _mouse.mouseDown
  if (gLOprops.mouse)*(gLOprops.lastMouse=0) then
    gLOprops.mouseClick = 1
  end if
  
  if gLOprops.mouse = 0 then
    gLOprops.mouseClick = 0
  end if
  
  lc = _mouse.mouseLoc+point(30,30)
  if lc.locH > 1366-300 then
    lc.locH = 1366-300
  end if
  
  --sprite(50).loc = lc
  
  me.goToEditor()
  
  -- put _key.keyCode
  
  go the frame
end


on buttonClicked me, bttn
  case bttn of
    "button geometry editor":
      _movie.go(15)
    "button tile editor":
      _movie.go(25)
    "button effects editor":
      _movie.go(34)
    "button light editor":
      _movie.go(38)
    "button render level":
      global gViewRender
      gViewRender = 1
      _movie.go(43)
    "button test render":
      newMakeLevel(gLoadedName)
      _movie.go(8)
    "button save project":
      levelName = gLoadedName
      member("projectNameInput").text = gLoadedName
      _movie.go(11)
    "button load project":
      _movie.go(2)
      --  #pal:1, totPalettes:2, #eCol1:1, #eCol2:2, #totEcols:2]
    "button previous palette":
      gLOprops.pal = gLOprops.pal - 1
      if gLOprops.pal < 1 then
        gLOprops.pal = gLOprops.pals.count
      end if
      sprite(21).member = member("libPal" & string(gLOprops.pal))
      member("palName").text = gLOprops.pals[gLOprops.pal].name
      gBlurOptions = [#blurLight:gLOprops.pals[gLOprops.pal].blurLight, #blurSky:gLOprops.pals[gLOprops.pal].blurSky]
    "button next palette":
      gLOprops.pal = gLOprops.pal + 1
      if gLOprops.pal > gLOprops.pals.count then
        gLOprops.pal = 1
      end if
      sprite(21).member = member("libPal" & string(gLOprops.pal))
      member("palName").text = gLOprops.pals[gLOprops.pal].name
      gBlurOptions = [#blurLight:gLOprops.pals[gLOprops.pal].blurLight, #blurSky:gLOprops.pals[gLOprops.pal].blurSky]
    "button previous EC1":
      gLOprops.eCol1 = gLOprops.eCol1 - 1
      if gLOprops.eCol1 < 1 then
        gLOprops.eCol1 = gLOprops.totEcols
      end if
      sprite(22).member = member("ecol" & string(gLOprops.eCol1))
    "button next EC1":
      gLOprops.eCol1 = gLOprops.eCol1 + 1
      if gLOprops.eCol1 > gLOprops.totEcols then
        gLOprops.eCol1 = 1
      end if
      sprite(22).member = member("ecol" & string(gLOprops.eCol1))
    "button previous EC2":
      gLOprops.eCol2 = gLOprops.eCol2 - 1
      if gLOprops.eCol2 < 1 then
        gLOprops.eCol2 = gLOprops.totEcols
      end if
      sprite(23).member = member("ecol" & string(gLOprops.eCol2))
    "button next EC2":
      gLOprops.eCol2 = gLOprops.eCol2 + 1
      if gLOprops.eCol2 > gLOprops.totEcols then
        gLOprops.eCol2 = 1
      end if
      sprite(23).member = member("ecol" & string(gLOprops.eCol2))
    "button more flies":
      gEditLizard[2] = restrict(gEditLizard[2]+1, 0, 40)
      member("addLizardFlies").text = string(gEditLizard[2])
    "button less flies":
      gEditLizard[2] = restrict(gEditLizard[2]-1, 0, 40)
      member("addLizardFlies").text = string(gEditLizard[2])
    "button more time":
      gEditLizard[3] = restrict(gEditLizard[3]+1, 0, 4000)
      member("addLizardTime").text = string(gEditLizard[3])
    "button less time":
      gEditLizard[3] = restrict(gEditLizard[3]-1, 0, 4000)
      member("addLizardTime").text = string(gEditLizard[3])
    "button super more time":
      gEditLizard[3] = restrict(gEditLizard[3]+100, 0, 4000)
      member("addLizardTime").text = string(gEditLizard[3])
    "button super less time":
      gEditLizard[3] = restrict(gEditLizard[3]-100, 0, 4000)
      member("addLizardTime").text = string(gEditLizard[3])
    "button lizard hole":
      me.nextHole()
    "button delete lizard":
      if gLevel.lizards.count > 0 then
        gLevel.lizards.deleteAt(gLevel.lizards.count)
        me.updateLizardsList()
      end if
    "button add lizard":
      if gEditLizard[4]>0 then
        if gLevel.lizards.count < 4 then
          gLevel.lizards.add(gEditLizard.duplicate())
          me.updateLizardsList()
        end if
      end if
      if gEditLizard[1]<>"yellow" then
        me.nextHole()
      end if
      
    "button lizard color":
      l1 = ["pink", "green", "blue", "white", "red", "yellow"]
      curr = 1
      repeat with q = 1 to l1.count then
        if gEditLizard[1]=l1[q] then
          curr = q
          exit repeat
        end if
      end repeat
      curr = curr + 1
      if curr > l1.count then
        curr = 1
      end if
      gEditLizard[1] = l1[curr]
      sprite(43).color = [color(255, 0, 255), color(0,255,0), color(0, 100, 255), color(255, 255, 255), color(255,0,0), color(255,200,0)][curr]
      
    "button standard medium":
      gLevel.defaultTerrain = 1-gLevel.defaultTerrain
      sprite(112).loc = point(312,312)+point(-1000+1000*gLevel.defaultTerrain, 0)
    "button light type":
      gLOprops.light = 1-gLOprops.light
    "button next color glow 1":
      gLOprops.colGlows[1] = gLOprops.colGlows[1] + 1
      if gLOprops.colGlows[1] > 2 then
        gLOprops.colGlows[1] = 0
      end if
      l = ["Dull", "Reflective", "Superflourescent"]
      member("color glow effects").text = l[gLOprops.colGlows[1]+1] && return && l[gLOprops.colGlows[2]+1]
    "button next color glow 2":
      gLOprops.colGlows[2] = gLOprops.colGlows[2] + 1
      if gLOprops.colGlows[2] > 2 then
        gLOprops.colGlows[2] = 0
      end if
      l = ["Dull", "Reflective", "Superflourescent"]
      member("color glow effects").text = l[gLOprops.colGlows[1]+1] && return && l[gLOprops.colGlows[2]+1]
      
    "button sound editor":
      _movie.go(18)
      
    "button mass render":
      global   massRenderSelectL 
      massRenderSelectL = []
      _movie.go(4)
      
    "button prop editor":
      _movie.go(23)
      
    "button level size":
      _movie.go(19)
      
      member("widthInput").text = gLOprops.size.locH
      member("heightInput").text = gLOprops.size.locV
      
      global newSize, extraBufferTiles
      newSize = [gLOprops.size.locH, gLOprops.size.locV, 0, 0]
      extraBufferTiles = gLOProps.extraTiles.duplicate()
      
      member("extraTilesLeft").text = extraBufferTiles[1]
      member("extraTilesTop").text = extraBufferTiles[2]
      member("extraTilesRight").text = extraBufferTiles[3]
      member("extraTilesBottom").text = extraBufferTiles[4]
      
      member("addTilesTop").text = "0"
      member("addTilesLeft").text = "0"
      
      
    "button cameras":
      _movie.go(32)
      
    "button environment editor":
      _movie.go(30)
      
    "button exit lock":
      if _movie.window.sizeState <> #minimized and _movie.exitLock = TRUE then
        _movie.exitLock = FALSE
      else
        _movie.exitLock = TRUE
      end if
      
    "button grid snap":
      if snapToGrid = 0 and preciseSnap = 0 then
        snapToGrid = 1
        preciseSnap = 0
        stg = 1
        ps = 0
      else if snapToGrid = 1 and preciseSnap = 0 then
        snapToGrid = 0
        preciseSnap = 1
        stg = 0
        ps = 1
      else if preciseSnap = 1 and snapToGrid = 0 then
        snapToGrid = 0
        preciseSnap = 0
        stg = 0
        ps = 0
      else if preciseSnap = 1 and snapToGrid = 1 then
        snapToGrid = 0
        preciseSnap = 0
        stg = 0
        ps = 0
      end if
      
    "button update preview":
      
      
      
      repeat with a = 1 to 3 then
        miniLvlEditDraw(a)
      end repeat
      
      sav = gLeProps.camPos
      gLeProps.camPos = point(0,0)
      cols = gLOprops.size.loch
      rows = gLOprops.size.locv
      member("levelEditImageShortCuts").image = image(cols*5, rows*5, 1)
      drawShortCutsImg(rect(1,1,cols,rows), 5, 1)
      gLeProps.camPos = sav
      
    "button prio cam":
      
      gPrioCam = gPrioCam + 1
      global gCameraProps
      if(gPrioCam > gCameraProps.cameras.count)then
        gPrioCam = 0
      end if
      
      if(gPrioCam = 0) then
        member("PrioCamText").text = "NONE"
        sprite(123).rect = rect(-100, -100, -100, -100)
      else 
        member("PrioCamText").text = "Will render camera " & gPrioCam & " first"
        mapRect = sprite(115).rect
        cPos = gCameraProps.cameras[gPrioCam]+point(0.001, 0.001)
        
        -- relativeRect = rect(cPos.locH / (gLOprops.size.locH*20), cPos.locV / (gLOprops.size.locV*20), (cPos.locH+1400) / (gLOprops.size.locH*20), (cPos.locV + 800) / (gLOprops.size.locV*20))
        
        sprite(123).rect = rect(lerp(mapRect.left, mapRect.right, cPos.locH / (gLOprops.size.locH*20)) , lerp(mapRect.top, mapRect.bottom, cPos.locV / (gLOprops.size.locV*20)), lerp(mapRect.left, mapRect.right, (cPos.locH + 1366) / (gLOprops.size.locH*20)), lerp(mapRect.top, mapRect.bottom, (cPos.locV + 768) / (gLOprops.size.locV*20)))
        
      end if
      
    "button lvlPropOutput":
      if lvlPropOutput = FALSE then
        lvlPropOutput = TRUE
      else
        lvlPropOutput = FALSE
      end if
      
  end case
  
end


on goToEditor me 
  goFrm = 0
  if _key.keyPressed("1") and _movie.window.sizeState <> #minimized then
    goFrm = 9
  else  if _key.keyPressed("2") and _movie.window.sizeState <> #minimized then
    goFrm = 15
  else  if _key.keyPressed("3") and _movie.window.sizeState <> #minimized then
    goFrm = 25
  else  if _key.keyPressed("4") and _movie.window.sizeState <> #minimized then
    goFrm = 32
  else  if _key.keyPressed("5") and _movie.window.sizeState <> #minimized then
    goFrm = 38
  else  if _key.keyPressed("6") and _movie.window.sizeState <> #minimized then
    goFrm = 19
    member("widthInput").text = gLOprops.size.locH
    member("heightInput").text = gLOprops.size.locV
    
    global newSize, extraBufferTiles
    newSize = [gLOprops.size.locH, gLOprops.size.locV, 0, 0]
    extraBufferTiles = gLOProps.extraTiles.duplicate()
    
    member("extraTilesLeft").text = extraBufferTiles[1]
    member("extraTilesTop").text = extraBufferTiles[2]
    member("extraTilesRight").text = extraBufferTiles[3]
    member("extraTilesBottom").text = extraBufferTiles[4]
    
    member("addTilesTop").text = "0"
    member("addTilesLeft").text = "0"
  else  if _key.keyPressed("7") and _movie.window.sizeState <> #minimized then
    goFrm = 34
  else  if _key.keyPressed("8") and _movie.window.sizeState <> #minimized then
    goFrm = 23
  else  if _key.keyPressed("9") and _movie.window.sizeState <> #minimized then
    goFrm = 30
  else if _key.keyPressed(56) and _key.keyPressed(48) and _movie.window.sizeState <> #minimized then
    _player.appMinimize()
  else if checkExit() then
    _player.quit()
    --else if _key.keyPressed(59) and _movie.window.sizeState <> #minimized then
    --if showControls = FALSE then
    --  showControls = TRUE
    --end if
    
  else if _key.keyPressed("0") and _movie.window.sizeState <> #minimized then
    levelName = gLoadedName
    member("projectNameInput").text = gLoadedName
    goFrm = 13
--  else if _key.keyPressed(BACKSPACE) and _movie.window.sizeState <> #minimized then
--    hideHelpClick = TRUE
  end if
  
  
  
  if goFrm <> 0 then
    repeat with q = 1 to 5 then
      sound(q).stop()
    end repeat
    
    global gSEprops
    
    if gSEprops.sounds <> void then
      gLevel.ambientSounds = []
      repeat with q = 1 to 4 then
        if (gSEprops.sounds[q].mem <> "none")and(gSEprops.sounds[q].vol > 0) then
          gLevel.ambientSounds.add(gSEprops.sounds[q].duplicate())
          gSEprops.sounds[q].mem = "None"
        end if
      end repeat
      gSEprops.sounds = void
    end if
    
    repeat with q = 1 to 22 then
      sprite(q).visibility = 1
    end repeat
    repeat with q = 800 to 820 then
      sprite(q).visibility = (goFrm = 15)
    end repeat
    _movie.go(goFrm)
  end if
end











on updateLizardsList me
  l1 = ["pink", "green", "blue", "white", "red", "yellow"]
  l2 = [color(255, 0, 255), color(0,255,0), color(0, 100, 255), color(255, 255, 255), color(255,0,0), color(255,200,0)]
  
  
  lz = []
  repeat with q = 1 to gLOprops.size.loch then
    repeat with c = 1 to gLOprops.size.locv then
      if gLEprops.matrix[q][c][1][2].getPos(7)>0 then
        lz.add(point(q,c))
      end if
    end repeat
  end repeat
  
  if lz <> [] then
    
    repeat with q = 1 to gLevel.lizards.count then
      member("lizard"&q&"text").text = gLevel.lizards[q][1] && "- Flies:"&& gLevel.lizards[q][2] && "- Time:" && gLevel.lizards[q][3]
      sprite(51+q).color = l2[l1.getPos(gLevel.lizards[q][1])]
      sprite(55+q).color = l2[l1.getPos(gLevel.lizards[q][1])]
      if gLevel.lizards[q][4] > lz.count then
        gLevel.lizards[q][4] = lz.count
      end if
      pnt1 = (lz[gLevel.lizards[q][4]]*10)+point(52, 112)+point(-5,-5)
      pnt2 = point(sprite(51+q).rect.left, sprite(51+q).rect.top+sprite(51+q).rect.height*0.5)
      sprite(55+q).rect = rect(pnt1, pnt2) 
      sprite(55+q).member = member("line" & (1+(pnt1.locV>pnt2.locV)))
    end repeat
    repeat with q = gLevel.lizards.count+1 to 4 then
      member("lizard"&q&"text").text = ""
      sprite(55+q).rect = rect(-100,-100,-100,-100)
    end repeat
  else
    repeat with q = 1 to 4 then
      member("lizard"&q&"text").text = ""
      sprite(55+q).rect = rect(-100,-100,-100,-100)
    end repeat
  end if
end 


on nextHole me
  global gLoprops
  lz = []
  repeat with q = 1 to gLOprops.size.loch then
    repeat with c = 1 to gLOprops.size.locv then
      if gLEprops.matrix[q][c][1][2].getPos(7)>0 then
        lz.add(point(q,c))
      end if
    end repeat
  end repeat
  if lz <> [] then
    gEditLizard[4] = gEditLizard[4] + 1
    if gEditLizard[4]>lz.count then
      gEditLizard[4] = 1
    end if
    pnt = (lz[gEditLizard[4]]*10)+point(52, 112)+point(-5,-5)
    sprite(60).rect = rect(pnt,pnt)+rect(-5,-5,5,5)
  else
    sprite(60).rect = rect(-5,-5,-5,-5)
  end if
end 









