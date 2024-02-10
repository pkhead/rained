global gTEprops, gLEProps, gTiles, gLOprops

on TEdraw(drwRect, layer, drawAll)
  
  --  if (drwRect.width > 1)and(drwRect.height>1)then
  --   drwRect = drwRect.intersect(rect(1,1,gLOprops.size.loch,gLOprops.size.locv))
  -- end if
  --global gLEProps
  
  global gLEProps, gLoprops
  
  repeat with q = drwRect.left to drwRect.right then
    repeat with c = drwRect.top to drwRect.bottom then
      drawQ = q - gLEProps.camPos.locH
      drawC = c - gLEProps.camPos.locV
      if (point(drawQ,drawC).inside(rect(1,1,53,41)))or(drawAll) then
        if (point(q,c).inside(rect(1,1,gLOprops.size.loch+1,gLOprops.size.locv+1))) then
          
          rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)
          if gTEprops.tlMatrix[q][c][layer].tp <>   "tileBody" then
            member("TEimg"&string(layer)).image.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {color:color(255, 255, 255)})
          end if
          case gTEprops.tlMatrix[q][c][layer].tp of 
            "material":
              
              rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)+rect(5, 5, -5, -5)
              case gLEProps.matrix[q][c][layer][1] of
                0:
                  rct = rect(-1, -1, -1, -1) 
                1:
                2:
                  rct = [point(rct.left, rct.top), point(rct.left, rct.top), point(rct.right, rct.bottom), point(rct.left, rct.bottom)]
                3:
                  rct = [point(rct.right, rct.top), point(rct.right, rct.top), point(rct.left, rct.bottom), point(rct.right, rct.bottom)]
                4:
                  rct = [point(rct.left, rct.bottom), point(rct.left, rct.bottom), point(rct.right, rct.top), point(rct.left, rct.top)]
                5:
                  rct = [point(rct.right, rct.bottom), point(rct.right, rct.bottom), point(rct.left, rct.top), point(rct.right, rct.top)]
                6:
                  rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)+rect(5, 5, -5, -8)
                7:
                8: 
              end case
              
              
              -- put gTEprops.tlMatrix[q][c][layer] && random(234234)
              cl = color(0,0,0)
              --CAT CHANGE
              repeat with nc = 1 to getLastMatCat()
                repeat with t in gTiles[nc].tls then
                  if t.nm = gTEprops.tlMatrix[q][c][layer].data then
                    cl = t.color
                    exit repeat
                  end if
                end repeat
              end repeat
              
              member("TEimg"&string(layer)).image.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {#color:cl})
              
            "tileHead":
              --  data = gTEprops.tlMatrix[q][c][layer].data
              -- rct = rect((q-1)*16, (c-1)*16, q*16, c*16)+rect(5, 5, -5, -5)
              tl = gTiles[gTEprops.tlMatrix[q][c][layer].data[1].locH].tls[gTEprops.tlMatrix[q][c][layer].data[1].locV]
              clr = gTiles[gTEprops.tlMatrix[q][c][layer].data[1].locH].clr
              -- member("TEimg"&string(layer)).image.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {#color:cl})
              --        "tileBody":
              --          rct = rect((q-1)*16, (c-1)*16, q*16, c*16)+rect(5, 5, -5, -5)
              --          cl = color(200, 150, 150)
              --          member("TEimg"&string(layer)).image.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {#color:cl})
              
              
              
              mdPnt = point(((tl.sz.locH*0.5)+0.4999).integer,((tl.sz.locV*0.5)+0.4999).integer)
              strt = point(q,c)-mdPnt+point(1,1)-gLEprops.camPos
              
              
              if (tl.specs2 <> void)and(layer<3) then
                repeat with g = strt.locH to strt.locH + tl.sz.locH-1 then
                  repeat with h = strt.locV to strt.locV + tl.sz.locV-1 then
                    if(g+gLEprops.camPos.locH>0)and(h+gLEprops.camPos.locV>0)and(g+gLEprops.camPos.locH<gTEprops.tlMatrix.count+1)and(h+gLEprops.camPos.locV<gTEprops.tlMatrix[1].count+1)then
                      
                      drw = true
                      if(tl.specs2[(h-strt.locV) + (g-strt.locH)*tl.sz.locV + 1] = -1)then
                        drw = false
                      else if(gTEprops.tlMatrix[g+gLEprops.camPos.locH][h+gLEprops.camPos.locV][layer+1].tp = "tileHead")and(gTEprops.tlMatrix[q][c][layer].data <> gTEprops.tlMatrix[g+gLEprops.camPos.locH][h+gLEprops.camPos.locV][layer+1].data)then
                        drw = false
                      end if
                      
                      if(drw)then
                        rct2 = rect((g-1)*16, (h-1)*16, g*16, h*16)
                        if (tl.ptPos > 60000) and (getBoolConfig("More tile previews")) then
                          drps = tl.ptPos - 60000
                          member("TEimg"&string(layer+1)).image.copyPixels(member("previewTilesDR").image, rct2, rct2+rect(drps+16, 0+16, drps+16, 0+16) - rect(strt.locH*16, strt.locV*16, strt.locH*16, strt.locV*16), {#color:clr*0.5})
                        else
                          member("TEimg"&string(layer+1)).image.copyPixels(member("previewTiles").image, rct2, rct2+rect(tl.ptPos+16, 0+16, tl.ptPos+16, 0+16) - rect(strt.locH*16, strt.locV*16, strt.locH*16, strt.locV*16), {#color:clr*0.5})
                        end if
                      end if
                      
                    end if
                  end repeat
                end repeat
              end if
              
              repeat with g = strt.locH to strt.locH + tl.sz.locH-1 then
                repeat with h = strt.locV to strt.locV + tl.sz.locV-1 then
                  if(g+gLEprops.camPos.locH>0)and(h+gLEprops.camPos.locV>0)and(g+gLEprops.camPos.locH<gTEprops.tlMatrix.count+1)and(h+gLEprops.camPos.locV<gTEprops.tlMatrix[1].count+1)then
                    
                    drw = true
                    
                    if(tl.specs[(h-strt.locV) + (g-strt.locH)*tl.sz.locV + 1] = -1)then
                      drw = false  
                    else if (gTEprops.tlMatrix[g+gLEprops.camPos.locH][h+gLEprops.camPos.locV][layer].tp = "tileHead")and(gTEprops.tlMatrix[q][c][layer].data<> gTEprops.tlMatrix[g+gLEprops.camPos.locH][h+gLEprops.camPos.locV][layer].data)then
                      drw = false
                    end if
                    
                    if(drw)then
                      rct2 = rect((g-1)*16, (h-1)*16, g*16, h*16)  
                      if (tl.ptPos > 60000) and (getBoolConfig("More tile previews")) then
                        drps2 = tl.ptPos - 60000
                        member("TEimg"&string(layer)).image.copyPixels(member("previewTilesDR").image, rct2, rct2+rect(drps2+16, 0+16, drps2+16, 0+16) - rect(strt.locH*16, strt.locV*16, strt.locH*16, strt.locV*16), {#color:clr})
                      else
                        member("TEimg"&string(layer)).image.copyPixels(member("previewTiles").image, rct2, rct2+rect(tl.ptPos+16, 0+16, tl.ptPos+16, 0+16) - rect(strt.locH*16, strt.locV*16, strt.locH*16, strt.locV*16), {#color:clr})
                      end if
                    end if  
                  end if
                end repeat
              end repeat
              
          end case
          
        else
          rct = rect((drawq-1)*16, (drawc-1)*16, drawq*16, drawc*16)
          member("TEimg"&string(layer)).image.copyPixels(member("pxl").image, rct, member("pxl").image.rect, {color:color(255, 255, 255)})
        end if
      end if
    end repeat
  end repeat
end