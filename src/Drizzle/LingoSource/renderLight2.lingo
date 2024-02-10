global q, c, tm, mvL

on exitFrame me
  if _key.keyPressed(56) and _key.keyPressed(48) and _movie.window.sizeState <> #minimized then
    _player.appMinimize()
    
  end if
  if checkExit() then
    _player.quit()
  end if
  
  
  repeat with q = 1  to 1040 then
    pnt = point(q , c)
    d = 0
    stp = 0
    
    mvlPs = 1
    
    if member("lightImage").getPixel(q-1,c-1) = 0 then
      repeat while stp = 0 then
        
        
        if d > 19 then
          exit repeat
        else if member("layer"&string(d)).image.getPixel(pnt+point(-1,-1)) <> color(255, 255, 255) and member("layer"&string(d)).image.getPixel(pnt+point(-1,-1)) <> color(0, 0, 0) then
          member("layer"&string(d)&"sh").image.setPixel(pnt+point(-1,-1), color(255, 0, 0))
          if d > 0 then
            repeat with dir in [point(-1, 0), point(0,-1), point(1,0), point(0,1)]then
              if member("layer"&string(d)).image.getPixel(pnt+point(-1,-1)+dir) <> color(255, 255, 255) and member("layer"&string(d)).image.getPixel(pnt+point(-1,-1)+dir) <> color(0, 0, 0) then
                member("layer"&string(d)&"sh").image.setPixel(pnt+point(-1,-1)+dir, color(255, 0, 0))
              end if
            end repeat
          end if
          exit repeat
        end if
        
        
        mvlPs = mvlPs +1
        if mvlPs > mvL.count then
          mvlPs = 1
        end if
        
        pnt = pnt + point(mvL[mvLPs][1], mvL[mvLPs][2])
        d = d + mvL[mvLPs][3]
        
      end repeat
    end if
    
  end repeat
  
  c = c + 1
  
  
  member("timeLeft").text = string(((c.float/800.0)*100.0).integer) && "% Rendered, Approx. " && string((((   (_system.milliseconds-tm).float  /c.float)  *  (800-c)  )/1000).integer) && "seconds left"
  sprite(42).loc = point(10, restrict(c, 30, 700))
  
  if c > 800 then
    
    member("shadowImage").image  = image(52*20, 40*20, 32)
    
    repeat with q = 1 to 20 then
      dp = 20-q-5
      pstRct = rect(depthPnt(point(0,0),dp),depthPnt(point(1040,800),dp))
      member("shadowImage").image.copyPixels(member("layer"&string(20-q)).image, pstRct, rect(0,0,1040,800), {#ink:36, #color:color(255,255,255)})
      member("shadowImage").image.copyPixels(member("layer"&string(20-q)&"sh").image, pstRct, rect(0,0,1040,800), {#ink:36})
    end repeat
    
    inv = image(52*20, 40*20, 1)
    inv.copyPixels(member("pxl").image, rect(0,0,1040,800), rect(0,0,1,1), {#color:255})
    inv.copyPixels(member("shadowImage").image, rect(0,0,1040,800), rect(0,0,1040,800), {#ink:36, #color:color(255,255,255)})
    member("shadowImage").image.copyPixels(inv, rect(0,0,1040,800), rect(0,0,1040,800))
    
    
    
    --   member("shadowImage").image  = image(52*20, 40*20, 32)--ALL LIGHT
  else
    go the frame
  end if
  
end





