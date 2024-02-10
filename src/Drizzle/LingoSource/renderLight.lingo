global q, c, tm, pos, gLightEProps, gLevel, keepLooping, gViewRender, gLOprops

on exitFrame me
  if _key.keyPressed(56) and _key.keyPressed(48) and _movie.window.sizeState <> #minimized then
    _player.appMinimize()
    
  end if
  if checkExit() then
    _player.quit()
  end if
  if gViewRender then
    if checkExitRender() then
      _movie.go(9)
    end if
    me.newFrame()
    if keepLooping then
      go the frame
    end if
  else
    repeat while keepLooping
      me.newFrame()
    end repeat
  end if
  
  
end

on newFrame me
  -- member("layer"&c&"sh").image = image(gLOprops.size.loch*20, gLOprops.size.locv*20, 8)
  cols: number = 100
  rows: number = 60
  
  marginPixels: number = 150
  
  marginRect: rect = rect(0,0,(cols*20)+marginPixels*2,(rows*20)+marginPixels*2)
  fullRect:rect  = rect(0,0,(cols*20),(rows*20))
  
  inv: image = image(marginRect.right, marginRect.bottom, 1)
  svPos: point = pos
  activeSilhouette: image = makeSilhoutteFromImg(member("activeLightImage").image, 1)
  repeat with q = 1 to gLightEProps.flatness then
    inv.copypixels(activeSilhouette, marginRect+rect(pos,pos), marginRect, {#ink:36, #color:color(255,0,0)})
    
    pos = pos + degToVec(gLightEProps.lightAngle)
  end repeat
  
  
  inv = makeSilhoutteFromImg(inv, 1)
  
  --member("layer"&c&"sh").image.copypixels(member("layer"&c).image, fullRect+rect(marginPixels,marginPixels,marginPixels,marginPixels), fullRect, {#ink:36, #color:color(0,255,0)})
  
  repeat with dir in [point(0,0),point(-1, 0), point(0,-1), point(1,0), point(0,1)]then
    type dir: point
    member("layer"&c&"sh").image.copypixels(inv, marginRect+rect(dir,dir), marginRect, {#ink:36})
  end repeat
  
  member("layer"&c&"sh").image.copypixels(makeSilhoutteFromImg( member("layer"&c).image, 1), fullRect+rect(marginPixels,marginPixels,marginPixels,marginPixels), fullRect, {#ink:36, #color:color(255,255,255)})
  
  layerSilhouette: image = makeSilhoutteFromImg( member("layer"&c).image, 0)

  repeat with q = 1 to gLightEProps.flatness then
    member("activeLightImage").image.copypixels(layerSilhouette, fullRect-rect(svPos,svPos)+rect(marginPixels,marginPixels,marginPixels,marginPixels), fullRect, {#ink:36, #color:color(255,255,255)})
    svPos = svPos + degToVec(gLightEProps.lightAngle)
  end repeat
  
  c = c + 1
  if (c > 29)then
    keepLooping = 0
  end if
end


on newFrame2 me
   --
  --lightImage
  inv: image = image(1040+200, 800+200, 1)--)makeSilhoutteFromImg(member("layer"&c&"sh").image, 1)
  svPos: point = pos
  repeat with q = 1 to gLightEProps.flatness then
    
    inv.copypixels(makeSilhoutteFromImg(member("activeLightImage").image, 1), rect(0,0,1040+200,800+200)+rect(pos,pos), rect(0,0,1040+200,800+200), {#ink:36, #color:color(255,0,0)})
    
    pos = pos + degToVec(gLightEProps.lightAngle)
  end repeat
  
  
  inv = makeSilhoutteFromImg(inv, 1)
  
  inv.copypixels(member("pxl").image, rect(0,0,pos.locH,800), rect(0,0,1,1), {#color:color(255,255,255)})
  inv.copypixels(member("pxl").image, rect(0,0,1040,pos.locV), rect(0,0,1,1), {#color:color(255,255,255)})
  
  repeat with dir in [point(0,0),point(-1, 0), point(0,-1), point(1,0), point(0,1)]then
    type dir: point
    member("layer"&c&"sh").image.copypixels(inv, rect(0,0,1040,800)+rect(dir,dir)+rect(-100,-100,100,100)+rect(0,8,0,8), rect(0,0,1040+200,800+200), {#ink:36, #color:color(255,0,0)})
  end repeat
  
  member("layer"&c&"sh").image.copypixels(makeSilhoutteFromImg( member("layer"&c).image, 1), rect(0,0,1040,800), rect(0,0,1040,800), {#ink:36, #color:color(255,255,255)})
  
  repeat with q = 1 to gLightEProps.flatness then
    member("activeLightImage").image.copypixels(makeSilhoutteFromImg( member("layer"&c).image, 0), rect(0,0,1040,800)-rect(svPos,svPos)+rect(100,100,100,100), rect(0,0,1040,800), {#ink:36, #color:color(255,255,255)})
    svPos = svPos + degToVec(gLightEProps.lightAngle)
  end repeat
  
  c = c + 1
  
  
  if (c > 29)or(gLevel.lightType="no Light") then
    
    member("shadowImage").image  = image(52*20, 40*20, 32)
    
    repeat with q = 1 to 30 then
      dp: number = 30-q-5
      pstRct: rect = rect(depthPnt(point(0,0),dp),depthPnt(point(1040,800),dp))
      member("shadowImage").image.copyPixels(member("layer"&string(30-q)).image, pstRct, rect(0,0,1040,800), {#ink:36, #color:color(255,255,255)})
      member("shadowImage").image.copyPixels(member("layer"&string(30-q)&"sh").image, pstRct, rect(0,0,1040,800), {#ink:36})
    end repeat
    
    inv = image(52*20, 40*20, 1)
    inv.copyPixels(member("pxl").image, rect(0,0,1040,800), rect(0,0,1,1), {#color:255})
    inv.copyPixels(member("shadowImage").image, rect(0,0,1040,800), rect(0,0,1040,800), {#ink:36, #color:color(255,255,255)})
    member("shadowImage").image.copyPixels(inv, rect(0,0,1040,800), rect(0,0,1040,800))
    
    
    
    --   member("shadowImage").image  = image(52*20, 40*20, 32)--ALL LIGHT
    keepLooping = 0
  end if
end





