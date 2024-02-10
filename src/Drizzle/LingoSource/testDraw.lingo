global gSkyColor, gLEProps, gLOprops

on drawTestLevel()
  img = image(1040, 800, 16)
  member("finalbg").image = drawTestLevelLayer(img, 3, color(150, 150, 150))
  member("finalbg").image.copyPixels(drawTestLevelLayer(img, 2, color(100, 100, 100)), rect(0,0,1040,800), rect(0,0,1040,800), {#ink:36})
  --member("finalbg").image.copyPixels(member("pxl").image, rect(0,0, 1040,800), rect(0,0,1,1), {color:color(255, 255, 255), #blend:50})
  
  img = image(1040, 800, 16)
  member("finalfg").image = drawTestLevelLayer(img, 1, color(50, 50, 50))
  member("finalbgLight").image = image(1040, 800, 16)
  member("finalfgLight").image = image(1040, 800, 16)
  
  repeat with q = 1 to gLOprops.size.loch then
    repeat with c = 1 to gLOprops.size.locv then
      if (gLEProps.matrix[q][c][1][2].getPos(5) > 0)then
        rct = giveMiddleOfTile(point(q,c))--rect((q-1)*20, (c-1)*20, q*20, c*20)+rect(8, 8, -8, -8)
        rct = rect(rct,rct)+rect(-1,-1,2,2)
        if(gLEProps.matrix[q][c][1][1]=1) then
          
          -- rct = rect(depthPnt(point(rct.left, rct.top), -5), depthPnt(point(rct.right, rct.bottom), -5))
          member("finalfg").image.copyPixels(member("pxl").image, rct, rect(0,0,1,1), {#color:color(0, 0, 0)})
          -- member("finalbg").image.copyPixels(member("pxl").image, rct, rect(0,0,1,1), {#color:color(0, 0, 0)})
        else  if(gLEProps.matrix[q][c][2][1]=1) then
          member("finalbg").image.copyPixels(member("pxl").image, rct, rect(0,0,1,1), {#color:color(70, 70, 70)})
        end if
      end if
    end repeat
  end repeat
  
  gSkyColor = color(170, 170, 170)
end

on drawTestLevelLayer(img, layer, col)

  drwRect = rect(1,1,gLOprops.size.loch,gLOprops.size.locv)
  
  repeat with q = drwRect.left to drwRect.right then
    repeat with c = drwRect.top to drwRect.bottom then
      if point(q,c).inside(rect(1,1,gLOprops.size.loch+1,gLOprops.size.locv+1)) then
        rct = rect((q-1)*20, (c-1)*20, q*20, c*20)
        img.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {color:color(255, 255, 255)})
        repeat with t in gLEProps.matrix[q][c][layer][2] then
          case t of
            1:
              rct = rect((q-1)*20, (c-1)*20, q*20, c*20)+rect(0, 8, 0, -8)
              img.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {#color:col})
            2:
              rct = rect((q-1)*20, (c-1)*20, q*20, c*20)+rect(8, 0, -8, 0)
              img.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {#color:col})
            3:
              rct = rect((q-1)*20, (c-1)*20, q*20, c*20)+rect(0, 6, 0, -6)
              img.copyPixels(member("hiveGrass").image, rct, member("hiveGrass").image.rect, {color:color(100, 100, 100)})
          end case
        end repeat
        
        case gLEProps.matrix[q][c][layer][1] of
          0:
            rct = rect(-1, -1, -1, -1) 
          1:
            
            rct = rect((q-1)*20, (c-1)*20, q*20, c*20)
            
          2:
            rct = rect((q-1)*20, (c-1)*20, q*20, c*20)
            rct = [point(rct.left, rct.top), point(rct.left, rct.top), point(rct.right, rct.bottom), point(rct.left, rct.bottom)]
          3:
            rct = rect((q-1)*20, (c-1)*20, q*20, c*20)
            rct = [point(rct.right, rct.top), point(rct.right, rct.top), point(rct.left, rct.bottom), point(rct.right, rct.bottom)]
          4:
            rct = rect((q-1)*20, (c-1)*20, q*20, c*20)
            rct = [point(rct.left, rct.bottom), point(rct.left, rct.bottom), point(rct.right, rct.top), point(rct.left, rct.top)]
          5:
            rct = rect((q-1)*20, (c-1)*20, q*20, c*20)
            rct = [point(rct.right, rct.bottom), point(rct.right, rct.bottom), point(rct.left, rct.top), point(rct.right, rct.top)]
          6:
            rct = rect((q-1)*20, (c-1)*20, q*20, (c*20)-10)
          7, 9:
            rct = rect((q-1)*20, (c-1)*20, q*20, c*20)
            img.copyPixels(member("semiTransperent").image, rct, member("semiTransperent").image.rect, {#color:col})
            rct = rect(-1, -1, -1, -1) 
            --        8:
            --          rct = rect((q-1)*16, (c-1)*16, q*16, c*16)
            --          member("levelEditImage"&string(layer)).image.copyPixels(member("semiTransperent").image, rct, member("semiTransperent").image.rect)
            --          rct = rect(-1, -1, -1, -1)  
        end case
        img.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {#color:col})
        
        if (gLEProps.matrix[q][c][layer][1]=1)and(gLEProps.matrix[q][c][layer][2].getPos(11)) then
          any = 0
          if (checkTileIfCrackOpen(point(q,c)+point(-1,0), layer))or(checkTileIfCrackOpen(point(q,c)+point(1,0), layer)) then
            rct = rect((q-1)*20, (c-1)*20, q*20, c*20)+rect(0, 4, 0, -4)
           img.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {#color:color(255,255,255)})
            any = 1
          end if
          if (checkTileIfCrackOpen(point(q,c)+point(0, -1), layer))or(checkTileIfCrackOpen(point(q,c)+point(0, 1), layer)) then
            rct = rect((q-1)*20, (c-1)*20, q*20, c*20)+rect(4, 0, -4,0)
           img.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {#color:color(255,255,255)})
            any = 1
          end if
          if any = 0 then
            rct = rect((q-1)*20, (c-1)*20, q*20, c*20)
            img.copyPixels(member("spearIcon").image, rct, member("spearIcon").image.rect, {#ink:36, #color:color(255,255,255)})
          else
            rct = rect((q-1)*20, (c-1)*20, q*20, c*20)
            img.copyPixels(member("semiTransperent").image, rct, member("semiTransperent").image.rect, {#ink:36})
          end if
        end if
        
      end if        
    end repeat
    -- end repeat
  end repeat
  
  return img
end