

on drawShortCutsImg(drwRect, scl, drawAll)
  global gLEProps, gLoprops
  
  drwRect = drwRect + rect(-1, -1, 1, 1)
  -- drwRect = drwRect.intersect(rect(1,1,gLOprops.size.loch,gLOprops.size.locv))
  
  repeat with q = drwRect.left to drwRect.right then
    repeat with c = drwRect.top to drwRect.bottom then
      if(drawAll = 0) then
        drawQ = q - gLEProps.camPos.locH
        drawC = c - gLEProps.camPos.locV
      else
        drawQ = q
        drawC = c   
      end if
      if (point(drawQ,drawC).inside(rect(1,1,53,41)))or(drawAll = 1) then
        if (point(q,c).inside(rect(1,1,gLOprops.size.loch+1,gLOprops.size.locv+1))) then
          rct = rect((drawQ-1)*scl, (drawc-1)*scl, drawQ*scl, drawc*scl)
          member("levelEditImageShortCuts").image.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {color:color(255, 255, 255)})
          if gLEProps.matrix[q][c][1][2].getPos(4)>0 then
            l = []
            l2 = []
            repeat with dir in [point(-1, 0), point(0, -1), point(1, 0), point(0,1)] then
              if (point(q,c)+dir).inside(rect(1,1,gLOprops.size.loch,gLOprops.size.locv)) then
                if (gLEProps.matrix[q+dir.locH][c+dir.locV][1][2].getPos(5) > 0)or(gLEProps.matrix[q+dir.locH][c+dir.locV][1][2].getPos(6) > 0)or(gLEProps.matrix[q+dir.locH][c+dir.locV][1][2].getPos(7) > 0)or(gLEProps.matrix[q+dir.locH][c+dir.locV][1][2].getPos(19) > 0)or(gLEProps.matrix[q+dir.locH][c+dir.locV][1][2].getPos(21) > 0) then
                  l.add(dir)
                end if
                if [0,6].getPos(gLEProps.matrix[q+dir.locH][c+dir.locV][1][1]) <> 0 then
                  l2.add(dir)
                end if
              end if
            end repeat
            shrtCut = 0
            if (l.count = 1)and(l2.count = 1) then
              if (l[1] = - l2[1]) then
                shrtCut = 1
              end if  
            end if
            repeat with dir in [point(-1, -1), point(1, -1), point(1, 1), point(-1,1)] then
              if (point(q,c)+dir).inside(rect(1,1,gLOprops.size.loch,gLOprops.size.locv)) then
                if gLEProps.matrix[q+dir.locH][c+dir.locV][1][1] <> 1 then
                  shrtCut = 0
                  exit repeat
                end if
              end if
            end repeat
            if shrtCut then
              rct = rect((drawq-1)*scl, (drawc-1)*scl, drawq*scl, drawc*16)+rect(4,4,-4,-4)*scl/16
              member("levelEditImageShortCuts").image.copyPixels(member("shortCutArrow"&string(l[1].locH)&"."&string(l[1].locV)).image, rct, member("shortCutArrow"&string(l[1].locH)&"."&string(l[1].locV)).image.rect)
              gLEProps.matrix[q][c][1][1] = 7
              --  lvlEditDraw(rect(q,c,q,c), 1)
            else
              member("levelEditImageShortCuts").image.copyPixels(member("shortCutArrow0.0").image, rct, member("shortCutArrow0.0").image.rect)
              gLEProps.matrix[q][c][1][1] = 0 
              --  lvlEditDraw(rect(q,c,q,c), 1)
            end if
          end if
          if gLEProps.matrix[q][c][1][2].getPos(5)>0 then
            rct = rect((drawq-1)*scl, (drawc-1)*scl, drawq*scl, drawc*scl)+rect(7,7,-7,-7)*scl/16
            member("levelEditImageShortCuts").image.copyPixels(member("pxl").image, rct, member("pxl").image.rect)
          end if
          
          if gLEProps.matrix[q][c][1][2].getPos(6)>0 then
            rct = rect((drawq-1)*scl, (drawc-1)*scl, drawq*scl, drawc*scl)
            member("levelEditImageShortCuts").image.copyPixels(member("p").image, rct+rect(3,3,-3,-3)*scl/16, member("p").image.rect, {#ink:36})
          end if
          if gLEProps.matrix[q][c][1][2].getPos(7)>0 then
            rct = rect((drawq-1)*scl, (drawc-1)*scl, drawq*scl, drawc*scl)
            member("levelEditImageShortCuts").image.copyPixels(member("e").image, rct+rect(3,3,-3,-3)*scl/16, member("e").image.rect, {#ink:36})
          end if
          if gLEProps.matrix[q][c][1][2].getPos(9)>0 then
            rct = rect((drawq-1)*scl, (drawc-1)*scl, drawq*scl, drawc*scl)
            member("levelEditImageShortCuts").image.copyPixels(member("rockIcon").image, rct+rect(3,3,-3,-3)*scl/16, member("rockIcon").image.rect, {#ink:36})
          end if
          if gLEProps.matrix[q][c][1][2].getPos(10)>0 then
            rct = rect((drawq-1)*scl, (drawc-1)*scl, drawq*scl, drawc*scl)
            member("levelEditImageShortCuts").image.copyPixels(member("spearIcon").image, rct, member("spearIcon").image.rect, {#ink:36})
          end if
          if gLEProps.matrix[q][c][1][2].getPos(12)>0 then
            rct = rect((drawq-1)*scl, (drawc-1)*scl, drawq*scl, drawc*scl)
            member("levelEditImageShortCuts").image.copyPixels(member("iconforbidbats").image, rct, member("iconforbidbats").image.rect, {#ink:36})
          end if
          if gLEProps.matrix[q][c][1][2].getPos(13)>0 then
            rct = rect((drawq-1)*scl, (drawc-1)*scl, drawq*scl, drawc*scl)
            member("levelEditImageShortCuts").image.copyPixels(member("g").image, rct+rect(3,3,-3,-3)*scl/16, member("g").image.rect, {#ink:36})
          end if
          if gLEProps.matrix[q][c][1][2].getPos(14)>0 then
            rct = rect((drawq-1)*scl, (drawc-1)*scl, drawq*scl, drawc*scl)
            member("levelEditImageShortCuts").image.copyPixels(member("bubble").image, rct, member("bubble").image.rect, {#ink:36})
          end if
          if gLEProps.matrix[q][c][1][2].getPos(15)>0 then
            rct = rect((drawq-1)*scl, (drawc-1)*scl, drawq*scl, drawc*scl)
            member("levelEditImageShortCuts").image.copyPixels(member("eggIcon").image, rct, member("eggIcon").image.rect, {#ink:36})
          end if
          
          if gLEProps.matrix[q][c][1][2].getPos(16)>0 then
            rct = rect((drawq-1)*scl, (drawc-1)*scl, drawq*scl, drawc*scl)
            member("levelEditImageShortCuts").image.copyPixels(member("miniFlyGraf").image, rct+rect(10,0,0,-10)*scl/16, member("miniFlyGraf").image.rect, {#ink:36})
          end if
          
          if gLEProps.matrix[q][c][1][2].getPos(17)>0 then
            rct = rect((drawq-1)*scl, (drawc-1)*scl, drawq*scl, drawc*scl)
            member("levelEditImageShortCuts").image.copyPixels(member("wallBugGraf").image, rct+rect(5,5,-5,5)*scl/16, member("wallBugGraf").image.rect, {#ink:36})
          end if
          
          if gLEProps.matrix[q][c][1][2].getPos(18)>0 then
            rct = rect((drawq-1)*scl, (drawc-1)*scl, drawq*scl, drawc*scl)
            member("levelEditImageShortCuts").image.copyPixels(member("waterfallW").image, rct+rect(2,2,-2,2)*scl/16, member("waterfallW").image.rect, {#ink:36})
          end if
          
          if gLEProps.matrix[q][c][1][2].getPos(19)>0 then
            rct = rect((drawq-1)*scl, (drawc-1)*scl, drawq*scl, drawc*scl)
            member("levelEditImageShortCuts").image.copyPixels(member("W").image, rct+rect(3,3,-3,-3)*scl/16, member("W").image.rect, {#ink:36})
          end if
          
          if gLEProps.matrix[q][c][1][2].getPos(20)>0 then
            rct = rect((drawq-1)*scl, (drawc-1)*scl, drawq*scl, drawc*scl)
            member("levelEditImageShortCuts").image.copyPixels(member("iconWormGrass").image, rct, member("iconWormGrass").image.rect, {#ink:36})
          end if
          
          if gLEProps.matrix[q][c][1][2].getPos(21)>0 then
            rct = rect((drawq-1)*scl, (drawc-1)*scl, drawq*scl, drawc*scl)
            member("levelEditImageShortCuts").image.copyPixels(member("s").image, rct+rect(3,3,-3,-3)*scl/16, member("s").image.rect, {#ink:36})
          end if
          
        else
          rct = rect((drawq-1)*scl, (drawc-1)*scl, drawq*scl, drawc*scl)
          member("levelEditImageShortCuts").image.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {#color:color(255, 255, 255)})
        end if
      end if
    end repeat
  end repeat
end


on lvlEditDraw(drwRect, layer, drawAll)
  
  global gLEProps, gLoprops, gLASTDRAWWASFULLANDMINI
  
  gLASTDRAWWASFULLANDMINI = 0
  
  drwRect = drwRect + rect(-1, -1, 1, 1)
  -- drwRect = drwRect - rect(gLEProps.camPos, gLEProps.camPos)
  
  repeat with q = drwRect.left to drwRect.right then
    repeat with c = drwRect.top to drwRect.bottom then
      drawQ = q - gLEProps.camPos.locH
      drawC = c - gLEProps.camPos.locV
      if (point(drawQ,drawC).inside(rect(1,1,53,41)))or(drawAll) then
        if (point(q,c).inside(rect(1,1,gLOprops.size.loch+1,gLOprops.size.locv+1))) then
          rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)
          member("levelEditImage"&string(layer)).image.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {color:color(255, 255, 255)})
          repeat with t in gLEProps.matrix[q][c][layer][2] then
            case t of
              1:
                rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)+rect(0, 6, 0, -6)
                member("levelEditImage"&string(layer)).image.copyPixels(member("pxl").image, rct, member("pxl").image.rect)
              2:
                rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)+rect(6, 0, -6, 0)
                member("levelEditImage"&string(layer)).image.copyPixels(member("pxl").image, rct, member("pxl").image.rect)
              3:
                rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)--+rect(0, 6, 0, -6)
                member("levelEditImage"&string(layer)).image.copyPixels(member("hiveGrass").image, rct, member("hiveGrass").image.rect, {color:color(100, 100, 100), #ink:36})
            end case
          end repeat
          
          case gLEProps.matrix[q][c][layer][1] of
            0:
              rct = rect(-1, -1, -1, -1) 
            1:
              
              rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)
              
            2:
              rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)
              rct = [point(rct.left, rct.top), point(rct.left, rct.top), point(rct.right, rct.bottom), point(rct.left, rct.bottom)]
            3:
              rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)
              rct = [point(rct.right, rct.top), point(rct.right, rct.top), point(rct.left, rct.bottom), point(rct.right, rct.bottom)]
            4:
              rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)
              rct = [point(rct.left, rct.bottom), point(rct.left, rct.bottom), point(rct.right, rct.top), point(rct.left, rct.top)]
            5:
              rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)
              rct = [point(rct.right, rct.bottom), point(rct.right, rct.bottom), point(rct.left, rct.top), point(rct.right, rct.top)]
            6:
              rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, (drawc*16)-8)
            7, 9:
              rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)
              member("levelEditImage"&string(layer)).image.copyPixels(member("semiTransperent").image, rct, member("semiTransperent").image.rect)
              rct = rect(-1, -1, -1, -1) 
              --        8:
              --          rct = rect((q-1)*16, (c-1)*16, q*16, c*16)
              --          member("levelEditImage"&string(layer)).image.copyPixels(member("semiTransperent").image, rct, member("semiTransperent").image.rect)
              --          rct = rect(-1, -1, -1, -1)  
          end case
          member("levelEditImage"&string(layer)).image.copyPixels(member("pxl").image, rct, member("pxl").image.rect)
          
          if(gLEProps.matrix[q][c][layer][2].getPos(11)) then
            if(gLEProps.matrix[q][c][layer][1]=1)then
              any = 0
              if (checkTileIfCrackOpen(point(q,c)+point(-1,0), layer))or(checkTileIfCrackOpen(point(q,c)+point(1,0), layer)) then
                rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)+rect(0, 4, 0, -4)
                member("levelEditImage"&string(layer)).image.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {#color:color(255,255,255)})
                any = 1
              end if
              if (checkTileIfCrackOpen(point(q,c)+point(0, -1), layer))or(checkTileIfCrackOpen(point(q,c)+point(0, 1), layer)) then
                rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)+rect(4, 0, -4,0)
                member("levelEditImage"&string(layer)).image.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {#color:color(255,255,255)})
                any = 1
              end if
              if any = 0 then
                rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)
                member("levelEditImage"&string(layer)).image.copyPixels(member("spearIcon").image, rct, member("spearIcon").image.rect, {#ink:36, #color:color(255,255,255)})
              else
                rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)
                member("levelEditImage"&string(layer)).image.copyPixels(member("semiTransperent").image, rct, member("semiTransperent").image.rect, {#ink:36})
              end if
            else
              rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)
              member("levelEditImage"&string(layer)).image.copyPixels(member("spearIcon").image, rct, member("spearIcon").image.rect, {#ink:36, #color:color(0,0,0)})
            end if
          end if
        else --   if (point(q,c).inside(rect(1,1,gLOprops.size.loch+1,gLOprops.size.locv+1))) then
          rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)
          member("levelEditImage"&string(layer)).image.copyPixels(member("semiTransperent").image, rct, member("semiTransperent").image.rect)
        end if
      end if        
    end repeat
    -- end repeat
  end repeat
end


on miniLvlEditDraw(layer)
  global gLEProps, gLoprops, gLASTDRAWWASFULLANDMINI
  
  if(gLASTDRAWWASFULLANDMINI)then
    return
  else if (layer = 3)then
    gLASTDRAWWASFULLANDMINI = 1
  end if
  
  member("levelEditImage"&string(layer)).image = image(gLoprops.size.locH*5, gLoprops.size.locV*5, 1)
  
  --drwRect = drwRect + rect(-1, -1, 1, 1)
  
  repeat with q = 1 to gLoprops.size.locH then
    repeat with c = 1 to gLoprops.size.locV then
      drawQ = q -- gLEProps.camPos.locH
      drawC = c -- gLEProps.camPos.locV
      if (point(q,c).inside(rect(1,1,gLOprops.size.loch+1,gLOprops.size.locv+1))) then
        rct = rect((drawq-1)*5, (drawc-1)*5, drawq*5, drawc*5)
        member("levelEditImage"&string(layer)).image.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {color:color(255, 255, 255)})
        repeat with t in gLEProps.matrix[q][c][layer][2] then
          case t of
            1:
              rct = rect((drawq-1)*5, (drawc-1)*5, drawq*5, drawc*5)+rect(0, 2, 0, -2)
              member("levelEditImage"&string(layer)).image.copyPixels(member("pxl").image, rct, member("pxl").image.rect)
            2:
              rct = rect((drawq-1)*5, (drawc-1)*5, drawq*5, drawc*5)+rect(2, 0, -2, 0)
              member("levelEditImage"&string(layer)).image.copyPixels(member("pxl").image, rct, member("pxl").image.rect)
            3:
              rct = rect((drawq-1)*5, (drawc-1)*5, drawq*5, drawc*5)--+rect(0, 6, 0, -6)
              member("levelEditImage"&string(layer)).image.copyPixels(member("hiveGrass").image, rct, member("hiveGrass").image.rect, {color:color(100, 100, 100), #ink:36})
          end case
        end repeat
        
        case gLEProps.matrix[q][c][layer][1] of
          0:
            rct = rect(-1, -1, -1, -1) 
          1:
            
            rct = rect((drawq-1)*5, (drawc-1)*5, drawq*5, drawc*5)
            
          2:
            rct = rect((drawq-1)*5, (drawc-1)*5, drawq*5, drawc*5)
            rct = [point(rct.left, rct.top), point(rct.left, rct.top), point(rct.right, rct.bottom), point(rct.left, rct.bottom)]
          3:
            rct = rect((drawq-1)*5, (drawc-1)*5, drawq*5, drawc*5)
            rct = [point(rct.right, rct.top), point(rct.right, rct.top), point(rct.left, rct.bottom), point(rct.right, rct.bottom)]
          4:
            rct = rect((drawq-1)*5, (drawc-1)*5, drawq*5, drawc*5)
            rct = [point(rct.left, rct.bottom), point(rct.left, rct.bottom), point(rct.right, rct.top), point(rct.left, rct.top)]
          5:
            rct = rect((drawq-1)*5, (drawc-1)*5, drawq*5, drawc*5)
            rct = [point(rct.right, rct.bottom), point(rct.right, rct.bottom), point(rct.left, rct.top), point(rct.right, rct.top)]
          6:
            rct = rect((drawq-1)*5, (drawc-1)*5, drawq*5, (drawc*5)-3)
          7, 9:
            rct = rect((drawq-1)*5, (drawc-1)*5, drawq*5, drawc*5)
            member("levelEditImage"&string(layer)).image.copyPixels(member("semiTransperent").image, rct, member("semiTransperent").image.rect)
            rct = rect(-1, -1, -1, -1) 
            --        8:
            --          rct = rect((q-1)*16, (c-1)*16, q*16, c*16)
            --          member("levelEditImage"&string(layer)).image.copyPixels(member("semiTransperent").image, rct, member("semiTransperent").image.rect)
            --          rct = rect(-1, -1, -1, -1)  
        end case
        member("levelEditImage"&string(layer)).image.copyPixels(member("pxl").image, rct, member("pxl").image.rect)
        
        if(gLEProps.matrix[q][c][layer][2].getPos(11)) then
          if(gLEProps.matrix[q][c][layer][1]=1)then
            any = 0
            if (checkTileIfCrackOpen(point(q,c)+point(-1,0), layer))or(checkTileIfCrackOpen(point(q,c)+point(1,0), layer)) then
              rct = rect((drawq-1)*5, (drawc-1)*5, drawq*5, drawc*5)+rect(0, 1, 0, -1)
              member("levelEditImage"&string(layer)).image.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {#color:color(255,255,255)})
              any = 1
            end if
            if (checkTileIfCrackOpen(point(q,c)+point(0, -1), layer))or(checkTileIfCrackOpen(point(q,c)+point(0, 1), layer)) then
              rct = rect((drawq-1)*5, (drawc-1)*5, drawq*5, drawc*5)+rect(1, 0, -1,0)
              member("levelEditImage"&string(layer)).image.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {#color:color(255,255,255)})
              any = 1
            end if
            if any = 0 then
              rct = rect((drawq-1)*5, (drawc-1)*5, drawq*5, drawc*5)
              member("levelEditImage"&string(layer)).image.copyPixels(member("spearIcon").image, rct, member("spearIcon").image.rect, {#ink:36, #color:color(255,255,255)})
            else
              rct = rect((drawq-1)*5, (drawc-1)*5, drawq*5, drawc*5)
              member("levelEditImage"&string(layer)).image.copyPixels(member("semiTransperent").image, rct, member("semiTransperent").image.rect, {#ink:36})
            end if
          else
            rct = rect((drawq-1)*5, (drawc-1)*5, drawq*5, drawc*5)
            member("levelEditImage"&string(layer)).image.copyPixels(member("spearIcon").image, rct, member("spearIcon").image.rect, {#ink:36, #color:color(0,0,0)})
          end if
        end if
      else --   if (point(q,c).inside(rect(1,1,gLOprops.size.loch+1,gLOprops.size.locv+1))) then
        rct = rect((drawq-1)*5, (drawc-1)*5, drawq*5, drawc*5)
        member("levelEditImage"&string(layer)).image.copyPixels(member("semiTransperent").image, rct, member("semiTransperent").image.rect)
      end if     
    end repeat
    -- end repeat
  end repeat
end


on checkTileIfCrackOpen(tl, lr)
  global gLEProps, gLOProps
  if tl.inside(rect(1,1,gLOprops.size.loch+1,gLOprops.size.locv+1)) then
    if (gLEProps.matrix[tl.locH][tl.locV][lr][1]<>1)or(gLEProps.matrix[tl.locH][tl.locV][lr][2].getPos(11)>0) then
      return 1
    else
      return 0
    end if
  else
    return 0
  end if
end

on saveLvl()
  -- member("matrixTxt").text = string(gMatrix)
end
