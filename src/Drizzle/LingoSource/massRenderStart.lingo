global projects, ldPrps, gLOADPATH, massRenderSelectL

on exitFrame me
  _movie.exitLock = TRUE
  if _key.keyPressed(56) and _key.keyPressed(48) and _movie.window.sizeState <> #minimized then
    _player.appMinimize()
    
  end if
  if checkExit() then
    _player.quit()
  end if
  
  pth = the moviePath & the dirSeparator & "LevelEditorProjects"
  repeat with f in gLOADPATH then
    pth = pth & the dirSeparator & f
  end repeat
  
  projects = []
  fileList = [ ]
  repeat with i = 1 to 300 then
    n = getNthFileNameInFolder(pth, i)
    if n = EMPTY then exit repeat
    if (char n.length-3 of n <> ".")then
      projects.add("#" & n)
    else
      fileList.append(n)
    end if
  end repeat
  
  
  
  
  repeat with l in fileList then
    if chars(l, l.length-3, l.length) = ".txt" then
      projects.add( chars(l, 1, l.length-4))
    end if
  end repeat
  
  txt = "Use the arrow keys to select a project. Use enter to open it."
  put RETURN after txt
  repeat with f in gLOADPATH then
    put f & "/" after txt
  end repeat
  put RETURN after txt
  put RETURN after txt
  repeat with q in projects then
    put q after txt
    put RETURN after txt
  end repeat
  
  ldPrps = [#lstUp:1, lstDwn:1, #lft:1, #rgth:1, #lstEnter:0, #currProject:1, #listScrollPos:1, #listShowTotal:30]
  
  member("ProjectsL").text = txt
  
  member("PalName").text = "Press 'A' to select all in folder. Press 'C' to deselect all. Press ENTER to start rendering."
end