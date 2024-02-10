global r, gEEprops, solidMtrx, gRenderCameraTilePos, effectIn3D

on DRFSlimeApply(q, c)
  q2 = q + gRenderCameraTilePos.locH
  c2 = c + gRenderCameraTilePos.locV
  
  effectR = gEEprops.effects[r]
  tlPnt = point(q2, c2)
  sldMtrxLoc = solidMtrx[q2][c2]
  openAreas = effectR.affectOpenAreas
  efMtrx = effectR.mtrx[q2][c2]
  efRep = effectR.repeats
  pixL = member("pxl").image
  pixLRct = pixL.rect
  whiteLoc = color(255, 255, 255)
  
  fc = openAreas + (1.0 - openAreas) * (solidAfaMv(tlPnt, 3))
  
  repeat with d = 1 to 30
    lr = 30 - d
    
    flgPnt = 1 + (d > 9) + (d > 19)
    
    if (lr = 9) or (lr = 19) then
      sld = (sldMtrxLoc[flgPnt])
      fc = openAreas + (1.0 - openAreas) * (solidAfaMv(tlPnt, flgPnt))
    end if
    deepEffect = 0
    
    if (lr = 0) or (lr = 10) or (lr = 20) or (sld = 0) then
      deepEffect = 1
    end if
    
    imgLr = member("layer" & string(lr)).image
    
    mxCtr = efMtrx * (0.2 + (0.8 * deepEffect)) * 0.01 * efRep * fc
    repeat with cntr = 1 to mxCtr
      if (deepEffect) then
        pnt = (point(q - 1, c - 1) * 20) + point(random(20), random(20))
      else
        if (random(2) = 1) then
          pnt = (point(q - 1, c - 1) * 20) + point(1 + 19 * (random(2) - 1), random(20))
        else 
          pnt = (point(q - 1, c - 1) * 20) + point(random(20), 1 + 19 * (random(2) - 1))
        end if
      end if 
      
      cl = imgLr.getPixel(pnt)     
      if (cl <> whiteLoc) then
        ofst = random(2) - 1
        lgt = 3 + random(random(random(6)))
        if (effectIn3D) then
          nwLr = DRFGet3DLr(lr)
        else
          nwLr = restrict(lr - 1 + random(2), 0, 29)
        end if
        
        nwImg = member("layer" & string(nwLr)).image
        
        pntRct = rect(pnt, pnt)
        
        nwImg.copyPixels(pixL, pntRct + rect(ofst, 0, 1 + ofst, lgt), pixLRct, {#color:cl})
        if (random(2) = 1) then
          nwImg.copyPixels(pixL, pntRct + rect(ofst + 1, 1, 2 + ofst, lgt - 1), pixLRct, {#color:cl})
        else
          nwImg.copyPixels(pixL, pntRct + rect(ofst - 1, 1, ofst, lgt - 1), pixLRct, {#color:cl})
        end if
      end if
    end repeat
  end repeat
end

on DRFGet3DLr(lr)
  nwLr = restrict(lr - 2 + random(3), 0, 29)
  if (lr = 6) and (nwLr = 5) then
    nwLr = 6
  else if (lr = 5) and (nwLr = 6) then
    nwLr = 5
  end if
  return nwLr
end

--on cpy(me, source, imgRect, sourceRect, props)
--  x = 0
--  y = 0
--  repeat while x <> sourceRect.wi
--end
--
