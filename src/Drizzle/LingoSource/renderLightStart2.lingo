global q, c, tm, dptsL, mvL, gLightEProps

on exitFrame me
  if _key.keyPressed(56) and _key.keyPressed(48) and _movie.window.sizeState <> #minimized then
    _player.appMinimize()
    
  end if
  if checkExit() then
    _player.quit()
  end if
  
  
  q = 1
  c = 1
  
  
  tm = _system.milliseconds
  
  repeat with q = 0 to 19 then
    sprite(40-q).loc = sprite(40-q).loc + point(-q, -q)
    member("layer"&string(q)&"sh").image = image(1040,800,32)
  end repeat
  
  
  
  member("dpImage").image = image(1040,800,32)
  member("dpImage").image.copyPixels(member("pxl").image, rect(0,0,1040,800), rect(0,0,1,1), {#color:255})
  
  smpl = image(4,1,32)
  smplPs = 0
  dptsL = []
  
  repeat with q = 1 to 20 then
    dp = 20-q-5
    
    
    
    pstRct = rect(depthPnt(point(0,0),dp),depthPnt(point(1040,800),dp))
    member("dpImage").image.copyPixels(member("layer"&string(20-q)).image, pstRct, rect(0,0,1040,800), {#ink:36, #color:color(255,255,255)})
    smpl.copyPixels(member("pxl").image, rect(smplPs,0,4,1), rect(0,0,1,1), {#color:0})
    
    if (dp+5=12)or(dp+5=8)or(dp+5=4)then
      
      smpl.copyPixels(member("pxl").image, rect(0,0,4,1), rect(0,0,1,1), {#blend:10, #color:255})
      smplPs = smplPs + 1
      member("dpImage").image.copyPixels(member("pxl").image, rect(0,0,1040,800), rect(0,0,1,1), {#blend:10, #color:255})
    end if
    
  end repeat
  
  
  repeat with q = 1 to 4 then
    dptsL.add(smpl.getPixel(4-q, 0))
  end repeat
  --put dptsL
  
  
  ang = gLightEProps.lightAngle
  ang = degToVec(ang)*2.8
  flatness = 1
  
  mvL = [[ang.locH, ang.locV,1]]
  repeat with q = 1 to gLightEProps.flatness then
    mvL.add([ang.locH, ang.locV,0])
  end repeat
end



