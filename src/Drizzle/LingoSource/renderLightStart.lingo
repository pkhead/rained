global c, tm, dptsL, gLightEProps, pos, gLevel, gLEProps, gLOprops, keepLooping, fogDptsL,gRenderCameraTilePos, gAnyDecals, solidMtrx

on exitFrame me
  if _key.keyPressed(56) and _key.keyPressed(48) and _movie.window.sizeState <> #minimized then
    _player.appMinimize()
    
  end if
  if checkExit() then
    _player.quit()
  end if
  if checkExitRender() then
    _movie.go(9)
  end if
  
  
  the randomSeed = gLOprops.tileSeed
  
  member("layer0dc").image.copyPixels(member("blackOutImg2").image, rect(0,0,100*20, 60*20), rect(0,0,100*20, 60*20), {#ink:36, #color:color(255, 255, 255)})
  
  repeat with layer = 1 to 3 then
    repeat with q = 1 to gLOprops.size.loch then
      repeat with c = 1 to gLOprops.size.locv then
        if (gLEProps.matrix[q][c][layer][2].getPos(3)>0)and(afaMvLvlEdit(point(q,c), layer) = 0)and(afaMvLvlEdit(point(q,c+1), layer) = 1) then
          -- pnt = giveMiddleOfTile(point(q,c))
          -- rct = rect((q-1)*20, (c-1)*20, q*20, c*20)--+rect(0, 8, 0, -8)
          --   mdlFrntImg.copyPixels(member("hiveGrass").image, rct, member("hiveGrass").image.rect, {color:pltt[1]})
          repeat with tp = 1 to 2 then
            repeat with grss = 1 to 6 then
              lr: number = ((layer-1)*10)+random(9)
              pos = giveMiddleOfTile(point(q,c) - gRenderCameraTilePos)+point(-10+random(20), 0)
              if (tp = 2)and(layer = 1) then
                member("layer"&string(lr)).image.copyPixels(member("hiveGrassGraf").image, rect(pos,pos)+rect(-2,random(5)-random(10)-random(random(14)),3,10), rect(0,0,5,29), {#ink:36})
              else
                member("layer"&string(lr)).image.copyPixels(member("hiveGrassGraf2").image, rect(pos,pos)+rect(-2,random(5)-random(10)-random(random(14)),3,10), rect(0,0,5,29), {#ink:36, #color:color(255,0,0)})
              end if  
            end repeat
          end repeat
        end if
      end repeat
    end repeat
  end repeat
  
  cols: number = 100
  rows: number = 60
  
  
  marginPixels: number = 150
  
  if(gAnyDecals)then
    repeat with l = 0 to 29 then
      me.quadifyMember("layer"&string(l)&"dc", (l-5)*1.5)
    end repeat
  end if
  
  repeat with l = 0 to 29 then
    me.quadifyMember("layer"&string(l), (l-5)*1.5)
    member("layer"&l&"sh").image = image((cols*20) + marginPixels*2, (rows*20) + marginPixels*2, 1)
    
    me.quadifyMember("gradientA"&string(l), (l-5)*1.5)
    me.quadifyMember("gradientB"&string(l), (l-5)*1.5)
  end repeat
  
  
  member("activeLightImage").image = image((cols*20)+marginPixels*2, (rows*20)+marginPixels*2, 1)
  member("activeLightImage").image.setPixel(0,0, color(0,0,0))
  member("activeLightImage").image.setPixel(member("activeLightImage").image.rect.right-1,member("activeLightImage").image.rect.bottom-1, color(0,0,0))
  
  
  inversedLightImage: image = makeSilhoutteFromImg( member("lightImage").image, 1)
  
  global  gRenderCameraPixelPos
  
  -- put "inversedLightImage: " && inversedLightImage.rect && marginPixels && cols && rows && gRenderCameraTilePos && gRenderCameraPixelPos && member("activeLightImage").image.rect
  -- angleAdd = degToVec(gLightEProps.lightAngle)*(gLightEProps.flatNess*10)
  
  member("activeLightImage").image.copyPixels(inversedLightImage, rect(0,0, (cols*20)+marginPixels*2, (rows*20)+marginPixels*2), rect(point(0, 0), point((cols*20)+marginPixels*2, (rows*20)+marginPixels*2)) + rect(gRenderCameraTilePos*20, gRenderCameraTilePos*20) + rect(150, 150, 150, 150))
  member("activeLightImage").image.copyPixels(member("blackOutImg2").image, rect(0,0, cols*20, rows*20)+rect(marginPixels,marginPixels,marginPixels,marginPixels), rect(0,0, cols*20, rows*20), {#ink:36, #color:color(255, 255, 255)})
  
  
  --member("activeLightImage").image.copyPixels(member("pxl").image, member("activeLightImage").image.rect, rect(0,0,1,1), {#color:color(0, 0, 0)})
  
  --  repeat with cl in ["A", "B"]then
  --    repeat with l = 1 to 4 then
  --      me.quadifyMember("gradient"&cl&string(l), (([0, 4, 8, 18])-5)*1.5)
  --    end repeat
  --  end repeat
  
  
  -- member("activeLightImage").image.copypixels(member("pxl").image, rect(0,0,(gLOprops.size.loch*20)+200, (gLOprops.size.locv*20))+200, member("pxl").image.rect, {#color:color(0,0,0)})
  
  
  c = 0
  keepLooping = 1
  
  pos = point(0,0)
  
  tm = _system.milliseconds
  
  repeat with q2 = 0 to 29 then
    sprite(50-q2).loc =  point((1366/2)-q2, (768/2)-q2)
  end repeat
  
  
  --  ang = gLightEProps.lightAngle
  --  ang = degToVec(ang)*2.8
  --  flatness = 1
  --  
  --  mvL = [[ang.locH, ang.locV,1]]
  --  repeat with q = 1 to gLightEProps.flatness then
  --    mvL.add([ang.locH, ang.locV,0])
  --  end repeat
  
  if(gLOprops.light = 0)then
    _movie.go(66)
  end if
end



on quadifyMember me, mem: string, fac: number
  --  seed =  the randomSeed
  --  the randomSeed = gLOprops.tileSeed
  --  qDirs = []
  --  frst = degToVec(random(360))
  --  l1 = [[random(100), frst], [random(100), -frst], [random(100), degToVec(random(360))], [random(100), degToVec(random(360))]]
  --  l1.sort()
  --  repeat with q = 1 to 4 then
  --    qDirs.add(l1[q][2])
  --  end repeat
  --  
  --  newImg = member(mem).image.duplicate()
  type newImg: image
  --  qd = [point(0,0), point(newImg.width, 0), point(newImg.width, newImg.height), point(0,newImg.height)]
  type qd: list
  --  qd = qd + qDirs*fac
  --  member(mem).image.copypixels(newImg, qd, newImg.rect)
  --  
  --  the randomSeed = seed
  
  global gCameraProps, gCurrentRenderCamera
  
  newImg = member(mem).image.duplicate()
  qd = [point(0,0), point(newImg.width, 0), point(newImg.width, newImg.height), point(0,newImg.height)]
  
  repeat with q = 1 to 4 then
    qd[q] = qd[q] + DegToVec(gCameraProps.quads[gCurrentRenderCamera][q][1])*gCameraProps.quads[gCurrentRenderCamera][q][2]*fac*2.5--arbitrary number, seemed to give the right amount
  end repeat
  
  member(mem).image.copypixels(newImg, qd, newImg.rect)
end


















