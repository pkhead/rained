global gLOprops, gEEprops, gAnyDecals, gTEprops, gLEprops, gRenderCameraTilePos, DRLastMatImp, DRLastSlpImp, DRLastFlrImP, DRLastTexImp, DRCustomMatList, DRLastTL

on LCheckIfATileIsSolidAndSameMaterial(tl, lr, matName)
  tl = point(restrict(tl.locH, 1, gLOprops.size.loch), restrict(tl.locV, 1, gLOprops.size.locv))
  rtrn = 0
  if (gLEprops.matrix[tl.locH][tl.locV][lr][1] = 1) then
    matTile = gTEprops.tlMatrix[tl.locH][tl.locV][lr]
    if (matTile.tp = "material") and (matTile.data = matName) then
      rtrn = 1
    else if (matTile.tp = "default") and (gTEprops.defaultMaterial = matName) then
      rtrn = 1
    end if 
  end if
  return rtrn
end

on LIsMyTileSetOpenToThisTile(matName, tl, l)
  rtrn = 0
  if (tl.inside(rect(1, 1, gLOprops.size.loch + 1, gLOprops.size.locv + 1))) then
    if [1,2,3,4,5].getPos(gLEProps.matrix[tl.locH][tl.locV][l][1]) > 0 then
      tile = gTEprops.tlMatrix[tl.locH][tl.locV][l]
      if (tile.tp = "material") and (tile.data = matName) then
        rtrn = 1
      else if (tile.tp = "default") and (gTEprops.defaultMaterial = matName) then
        rtrn = 1
      end if
    end if
  else if (gTEprops.defaultMaterial = matName) then
    rtrn = 1
  end if
  return rtrn
end

on LDrawATileMaterial(q, c, l, nm) --frntImg,
  if (DRCustomMatList.count >= 1) then
    matTl = DRCustomMatList[DRLastTL]
    if (matTl.nm <> nm) then
      repeat with inti = 1 to DRCustomMatList.count
        if (DRCustomMatList[inti].nm = nm) then
          matTl = DRCustomMatList[inti]
          DRLastTL = inti
          exit repeat
        end if
      end repeat
    end if
    if (matTl.nm = nm) then
      case l of
        1: 
          dp = 0
        2: 
          dp = 10
        otherwise:
          dp = 20
      end case
      qcp = point(q, c)
      LEMatrixT = gLEProps.matrix[q][c][l][1]
      if (matTl.findPos(#texture) <> VOID) then
        mText = matTl.texture
        matFile = member("MatTexImport")
        if (DRLastTexImp <> nm) then
          member("MatTexImport").importFileInto("Materials" & the dirSeparator & nm & "Texture.png")
          matFile.name = "MatTexImport"
          DRLastTexImp = nm
        end if
        matImg = matFile.image
        colored = (mText.tags.getPos("colored") > 0)
        if (colored) then
          gAnyDecals = 1
        end if
        effectColorA = (mText.tags.getPos("effectColorA") > 0)
        effectColorB = (mText.tags.getPos("effectColorB") > 0)
        size = mText.sz
        bsRect = rect((q mod size.locH) * 20, (c mod size.locV) * 20 + 1, ((q mod size.locH) + 1) * 20, ((c mod size.locV) + 1) * 20 + 1)
        if (colored) or (effectColorA) or (effectColorB) then
          gradRect = rect(size.locH * 20, 0, size.locH * 20, 0)
        end if
        pstRect = rect((q - 1) * 20, (c - 1) * 20, q * 20, c * 20) - rect(gRenderCameraTilePos, gRenderCameraTilePos) * 20
        case LEMatrixT of
          1:
            d = -1
            repeat with ps = 1 to mText.repeatL.count
              gtRect = bsRect + rect(0, size.locV * 20 * (ps - 1), 0, size.locV * 20 * (ps - 1))
              repeat with ps2 = 1 to mText.repeatL[ps]
                d = d + 1 
                if (d + dp > 29) then
                  exit repeat
                else
                  lstr = string(d + dp)
                  member("layer" & lstr).image.copyPixels(matImg, pstRect, gtRect, {#ink:36})
                  if (colored) then
                    if (effectColorA = 0) and (effectColorB = 0) then
                      member("layer" & lstr & "dc").image.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:36})
                    end if
                  end if
                  if (effectColorA) then
                    member("gradientA" & lstr).image.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:39})
                  end if
                  if (effectColorB) then
                    member("gradientB" & lstr).image.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:39})
                  end if
                end if
              end repeat
            end repeat
          2,3,4,5:
            rct = rect((q - 1) * 20, (c - 1) * 20, q * 20, c * 20)
            case LEMatrixT of
              5:
                rct = [point(rct.left, rct.top), point(rct.left, rct.top), point(rct.right, rct.bottom), point(rct.left, rct.bottom)]  
              4:
                rct = [point(rct.right, rct.top), point(rct.right, rct.top), point(rct.left, rct.bottom), point(rct.right, rct.bottom)]
              3:
                rct = [point(rct.left, rct.bottom), point(rct.left, rct.bottom), point(rct.right, rct.top), point(rct.left, rct.top)]
              2:
                rct = [point(rct.right, rct.bottom), point(rct.right, rct.bottom), point(rct.left, rct.top), point(rct.right, rct.top)]
            end case
            rct = rct - [gRenderCameraTilePos, gRenderCameraTilePos, gRenderCameraTilePos, gRenderCameraTilePos] * 20
            pxlI = member("pxl").image
            pxlR = rect(0, 0, 1, 1)
            wh = color(255, 255, 255)
            d = -1
            repeat with ps = 1 to mText.repeatL.count
              gtRect = bsRect + rect(0, size.locV * 20 * (ps - 1), 0, size.locV * 20 * (ps - 1))
              repeat with ps2 = 1 to mText.repeatL[ps]
                d = d + 1 
                if (d + dp > 29) then
                  exit repeat
                else
                  lstr = string(d + dp)
                  lri = member("layer" & lstr).image
                  lri.copyPixels(matImg, pstRect, gtRect, {#ink:36})
                  lri.copyPixels(pxlI, rct, pxlR, {#color:wh})
                  if (colored) then
                    if (effectColorA = 0) and (effectColorB = 0) then
                      lri = member("layer" & lstr & "dc").image
                      lri.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:36})
                      lri.copyPixels(pxlI, rct, pxlR, {#color:wh})
                    end if
                  end if
                  if (effectColorA) then
                    lri = member("gradientA" & lstr).image
                    lri.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:39})
                    lri.copyPixels(pxlI, rct, pxlR, {#color:wh})
                  end if
                  if (effectColorB) then
                    lri = member("gradientB" & lstr).image
                    lri.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:39})
                    lri.copyPixels(pxlI, rct, pxlR, {#color:wh})
                  end if
                end if
              end repeat
            end repeat
          6:
            if (mText.tags.getPos("textureOnFloor") > 0) then
              rct = rect((q - 1) * 20, (c - 1) * 20 + 10, q * 20, c * 20) - rect(gRenderCameraTilePos, gRenderCameraTilePos) * 20
              pxlI = member("pxl").image
              pxlR = rect(0, 0, 1, 1)
              wh = color(255, 255, 255)
              d = -1
              repeat with ps = 1 to mText.repeatL.count
                gtRect = bsRect + rect(0, size.locV * 20 * (ps - 1), 0, size.locV * 20 * (ps - 1))
                repeat with ps2 = 1 to mText.repeatL[ps]
                  d = d + 1 
                  if (d + dp > 29) then
                    exit repeat
                  else
                    lstr = string(d + dp)
                    lri = member("layer" & lstr).image
                    lri.copyPixels(matImg, pstRect, gtRect, {#ink:36})
                    lri.copyPixels(pxlI, rct, pxlR, {#color:wh})
                    if (colored) then
                      if (effectColorA = 0) and (effectColorB = 0) then
                        lri = member("layer" & lstr & "dc").image
                        lri.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:36})
                        lri.copyPixels(pxlI, rct, pxlR, {#color:wh})
                      end if
                    end if
                    if (effectColorA) then
                      lri = member("gradientA" & lstr).image
                      lri.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:39})
                      lri.copyPixels(pxlI, rct, pxlR, {#color:wh})
                    end if
                    if (effectColorB) then
                      lri = member("gradientB" & lstr).image
                      lri.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:39})
                      lri.copyPixels(pxlI, rct, pxlR, {#color:wh})
                    end if
                  end if
                end repeat
              end repeat
            end if
        end case
      end if
      case LEMatrixT of
        1:
          if (matTl.findPos(#block) <> VOID) then
            fl = matTl.block
            rct2 = rect((q - 1) * 20 - 5, (c - 1) * 20 - 5, q * 20 + 5, c * 20 + 5) - rect(gRenderCameraTilePos, gRenderCameraTilePos) * 20
            colored = (fl.tags.getPos("colored") > 0)
            if (colored) then
              gAnyDecals = 1
            end if
            effectColorA = (fl.tags.getPos("effectColorA") > 0)
            effectColorB = (fl.tags.getPos("effectColorB") > 0)
            tlRnd = fl.rnd
            rnd = random(tlRnd) - 1
            matFile = member("MatImport")
            if (DRLastMatImp <> nm) then
              member("MatImport").importFileInto("Materials" & the dirSeparator & nm & ".png")
              matFile.name = "MatImport"
              DRLastMatImp = nm
            end if
            matImg = matFile.image
            repeat with f = 1 to 4
              case f of
                1:
                  profL = [point(-1, 0), point(0, -1)]
                  gtAtV = 2
                  pstRect = rct2 + rect(0, 0, -10, -10)
                2:
                  profL = [point(1, 0), point(0, -1)]
                  gtAtV = 4
                  pstRect = rct2 + rect(10, 0, 0, -10)
                3:
                  profL = [point(1, 0), point(0, 1)]
                  gtAtV = 6
                  pstRect = rct2 + rect(10, 10, 0, 0)
                otherwise:
                  profL = [point(-1, 0), point(0, 1)]
                  gtAtV = 8
                  pstRect = rct2 + rect(0, 10, -10, 0)
              end case
              ID = ""
              repeat with dr in profL
                ID = ID & string(LIsMyTileSetOpenToThisTile(nm, qcp + dr, l))
              end repeat
              if (ID = "11") then
                if ([1,2,3,4,5].getPos(LIsMyTileSetOpenToThisTile(nm, qcp + profL[1] + profL[2], l)) > 0) then
                  gtAtH = 10
                  gtAtV = 2
                else
                  gtAtH = 8
                end if
              else
                gtAtH = [0, "00", 0, "01", 0, "10"].getPos(ID)
              end if
              if (gtAtH = 4) then
                if (gtAtV = 6) then
                  gtAtV = 4
                else if (gtAtV = 8) then
                  gtAtV = 2
                end if
              else if (gtAtH = 6) then
                if (gtAtV = 4) or (gtAtV = 8) then
                  gtAtV = gtAtV - 2
                end if
              end if
              bsRect = rect((gtAtH - 1) * 10 - 5 + 100 * rnd, (gtAtV - 1) * 10 - 5, gtAtH * 10 + 5 + 100 * rnd, gtAtV * 10 + 5)
              --frntImg.copyPixels(matImg, pstRect, bsRect, {#ink:36})
              if (colored) or (effectColorA) or (effectColorB) then
                gradRect = rect(100 * tlRnd, 0, 100 * tlRnd, 0)
              end if
              d = -1
              repeat with ps = 1 to fl.repeatL.count
                gtRect = bsRect + rect(0, 80 * (ps - 1), 0, 80 * (ps - 1))
                repeat with ps2 = 1 to fl.repeatL[ps]
                  d = d + 1 
                  if (d + dp > 29) then
                    exit repeat
                  else
                    lstr = string(d + dp)
                    member("layer" & lstr).image.copyPixels(matImg, pstRect, gtRect, {#ink:36})
                    if (colored) then
                      if (effectColorA = 0) and (effectColorB = 0) then
                        member("layer" & lstr & "dc").image.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:36})
                      end if
                    end if
                    if (effectColorA) then
                      member("gradientA" & lstr).image.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:39})
                    end if
                    if (effectColorB) then
                      member("gradientB" & lstr).image.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:39})
                    end if
                  end if
                end repeat
              end repeat
            end repeat
          end if
        2,3,4,5:
          if (matTl.findPos(#slope) <> VOID) then
            matFile = member("MatSlpImport")
            if (DRLastSlpImp <> nm) then
              member("MatSlpImport").importFileInto("Materials" & the dirSeparator & nm & "Slopes.png")
              matFile.name = "MatSlpImport"
              DRLastSlpImp = nm
            end if
            fl = matTl.slope
            matImg = matFile.image
            tlRnd = fl.rnd
            rnd = random(tlRnd) - 1
            colored = (fl.tags.getPos("colored") > 0)
            if (colored) then
              gAnyDecals = 1
            end if
            effectColorA = (fl.tags.getPos("effectColorA") > 0)
            effectColorB = (fl.tags.getPos("effectColorB") > 0)
            slp = gLEProps.matrix[q][c][l][1]
            askDirs = [0, [point(-1, 0), point(0, 1)], [point(0, 1), point(1, 0)], [point(-1, 0), point(0, -1)], [point(0, -1), point(1, 0)]]
            myAskDirs = askDirs[slp]
            pstRect = rect((q - 1) * 20 - 5, (c - 1) * 20 - 5, q * 20 + 5, c * 20 + 5) - rect(gRenderCameraTilePos, gRenderCameraTilePos) * 20
            if (colored) or (effectColorA) or (effectColorB) then
              gradRect = rect(120 * tlRnd, 0, 120 * tlRnd, 0)
            end if
            repeat with ad = 1 to myAskDirs.count
              bsRect = rect(5 + 60 * (ad = 2) + 120 * rnd, 5 + 30 * (slp - 2), 35 + 60 * (ad = 2) + 120 * rnd, 35 + 30 * (slp - 2))
              if (LIsMyTileSetOpenToThisTile(nm, qcp + myAskDirs[ad], l)) then
                bsRect = bsRect + rect(30, 0, 30, 0)
              end if
              d = -1
              repeat with ps = 1 to fl.repeatL.count
                gtRect = bsRect + rect(0, 130 * (ps - 1), 0, 130 * (ps - 1))
                repeat with ps2 = 1 to fl.repeatL[ps]
                  d = d + 1 
                  if (d + dp > 29) then
                    exit repeat
                  else
                    lstr = string(d + dp)
                    member("layer" & lstr).image.copyPixels(matImg, pstRect, gtRect, {#ink:36})
                    if (colored) then
                      if (effectColorA = 0) and (effectColorB = 0) then
                        member("layer" & lstr & "dc").image.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:36})
                      end if
                    end if
                    if (effectColorA) then
                      member("gradientA" & lstr).image.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:39})
                    end if
                    if (effectColorB) then
                      member("gradientB" & lstr).image.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:39})
                    end if
                  end if
                end repeat
              end repeat
            end repeat
          end if
        6:
          if (matTl.findPos(#floor) <> VOID) then
            matFile = member("MatFlrImport")
            if (DRLastFlrImp <> nm) then
              member("MatFlrImport").importFileInto("Materials" & the dirSeparator & nm & "Floor.png")
              matFile.name = "MatFlrImport"
              DRLastFlrImp = nm
            end if
            fl = matTl.floor
            matImg = matFile.image
            tlRnd = fl.rnd
            rnd = random(tlRnd) - 1
            colored = (fl.tags.getPos("colored") > 0)
            if (colored) then
              gAnyDecals = 1
            end if
            effectColorA = (fl.tags.getPos("effectColorA") > 0)
            effectColorB = (fl.tags.getPos("effectColorB") > 0)
            vbf = 20 * fl.bfTiles
            pstRect = rect((q - 1) * 20 - vbf, (c - 1) * 20 - vbf, q * 20 + vbf, c * 20 + vbf) - rect(gRenderCameraTilePos, gRenderCameraTilePos) * 20
            bfCal = 20 + 40 * fl.bfTiles
            bsRect = rect(0, 1, bfCal, bfCal + 1)
            bsRect = bsRect + rect(bsRect.width * rnd, 0, bsRect.width * rnd, 0)
            if (colored) or (effectColorA) or (effectColorB) then
              gradRect = rect(bfCal * tlRnd, 0, bfCal * tlRnd, 0)
            end if
            d = -1
            repeat with ps = 1 to fl.repeatL.count
              gtRect = bsRect + rect(0, bfCal * (ps - 1), 0, bfCal * (ps - 1))
              repeat with ps2 = 1 to fl.repeatL[ps]
                d = d + 1 
                if (d + dp > 29) then
                  exit repeat
                else
                  lstr = string(d + dp)
                  member("layer" & lstr).image.copyPixels(matImg, pstRect, gtRect, {#ink:36})
                  if (colored) then
                    if (effectColorA = 0) and (effectColorB = 0) then
                      member("layer" & lstr & "dc").image.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:36})
                    end if
                  end if
                  if (effectColorA) then
                    member("gradientA" & lstr).image.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:39})
                  end if
                  if (effectColorB) then
                    member("gradientB" & lstr).image.copyPixels(matImg, pstRect, gtRect + gradRect, {#ink:39})
                  end if
                end if
              end repeat
            end repeat
          end if
      end case
    end if
  end if
  --return frntImg
end