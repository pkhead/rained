global levelName, gTEprops, gTiles, gLEProps, gEEprops, gLightEProps
global gLEVEL, gLOprops, gLoadedName, gCameraProps, gEnvEditorProps, gPEprops, gLOADPATH



on exitFrame me
  _movie.exitLock = TRUE
  if _key.keyPressed(56) and _key.keyPressed(48) and _movie.window.sizeState <> #minimized then
    _player.appMinimize()
    
  end if
  if checkExit() then
    _player.quit()
  end if
  
  if _key.keyPressed(36) and _movie.window.sizeState <> #minimized and levelName <> void then
    
    --  l = [#mtrx:gLEProps.matrix, #gTEProps:gTEprops, #gEEprops:gEEprops, #gLightEProps:gLightEProps, #gLEVEL:gLEVEL, #gLOprops:gLOprops]
    str = ""
    put gLEProps.matrix after str
    put RETURN after str
    put gTEProps after str
    put RETURN after str
    put gEEprops after str
    put RETURN after str
    put gLightEProps after str
    put RETURN after str
    put gLEVEL after str
    put RETURN after str
    put gLOprops after str
    put RETURN after str
    put gCameraProps after str
    put RETURN after str
    put gEnvEditorProps after str
    put RETURN after str
    put gPEprops after str
    put RETURN after str
    
    objFileio = new xtra("fileio")
    pth = the moviePath & "LevelEditorProjects" & the dirSeparator
    repeat with f in gLOADPATH then
      pth = pth & f & the dirSeparator
    end repeat
    createFile (objFileio, pth&levelName&".txt")
    objFileio.openFile(pth&levelName&".txt", 0)
    objFileio.writeString(str)
    objFileio.closeFile()
    
    -- member("lightImageExport").image = image(1040+200, 800+200, 32)
    --  member("lightImageExport").image.copyPixels(member("lightImage").image, rect(0, 0, 1040+200, 800+200), rect(0, 0, 1040+200, 800+200))
    member("lightImage").image.setPixel(0,0,color(0,0,0))
    member("lightImage").image.setPixel(member("lightImage").rect.width-1, member("lightImage").rect.height-1, color(0,0,0))
    -- exportAnImage( member("lightImageExport").image, "\LevelEditorProjects\" & levelName)
    
    gImgXtra = xtra("ImgXtra").new()
    nwImg = image(member("lightImage").image.rect.width, member("lightImage").image.rect.height, 32)
    nwImg.copypixels(member("lightImage").image, rect(0,0,member("lightImage").image.rect.width, member("lightImage").image.rect.height), rect(0,0,member("lightImage").image.rect.width, member("lightImage").image.rect.height))
    props = ["image": nwImg, "filename":pth & levelName & ".png"]
    ok = gImgXtra.ix_saveImage(props)
    
    -- member("lightImageExport").image = image(1, 1, 1)
    
    gLoadedName = levelName
    member("Level Name").text = gLoadedName
    
    _movie.go(7)
    --else if _key.keyPressed(36) and _movie.window.sizeState <> #minimized and levelName = void then
    --  levelName = "New Project"
  else if _key.keyPressed(51) and _key.keyPressed(56) and _movie.window.sizeState <> #minimized then
    _movie.go(7)
  else
    go the frame
  end if
end