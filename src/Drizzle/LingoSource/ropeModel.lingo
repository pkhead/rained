


global ropeModel

on resetRopeModel me, pA, pB, prop, lengthFac, lr, rel
  
  ropeModel = [#posA:pA, #posB:pB, #segmentLength:prop.segmentLength, #grav:prop.grav, #stiff:prop.stiff, #release:rel, #segments:[], #friction:prop.friction, #airFric:prop.airFric, #layer:lr, #segRad:prop.segRad, #rigid:prop.rigid, #edgeDirection:prop.edgeDirection, #selfPush:prop.selfPush, sourcePush:prop.sourcePush]
  numberOfSegments = (Diag(pA, pB) / prop.segmentLength)*lengthFac
  if(numberOfSegments < 3)then
    numberOfSegments = 3
  end if
  step = Diag(pA, pB)/numberOfSegments
  repeat with i = 1 to numberOfSegments then
    ropeModel.segments.Add([#pos:pA + MoveToPoint(pA, pB, (i-0.5)*step), #lastPos:pA + MoveToPoint(pA, pB, (i-0.5)*step), #vel:point(0,0)])
  end repeat
end

on modelRopeUpdate me, preview, cameraPos, previewScale
  
  
  if(ropeModel.edgeDirection > 0) then
    dir = MoveToPoint(ropeModel.posA, ropeModel.posB, 1.0)
    if(ropeModel.release > -1)then
      repeat with A = 1 to ropeModel.segments.count/2 then
        fac = Power(1.0 - ((A.float-1.0)/(ropeModel.segments.count/2)), 2)
        ropeModel.segments[A].vel = ropeModel.segments[A].vel + dir*fac*ropeModel.edgeDirection
      end repeat
      idealFirstPos = ropeModel.posA + dir * ropeModel.segmentLength
      ropeModel.segments[1].pos = point(lerp(ropeModel.segments[1].pos.locH, idealFirstPos.locH, ropeModel.edgeDirection), lerp(ropeModel.segments[1].pos.locV, idealFirstPos.locV, ropeModel.edgeDirection))
    end if
    if(ropeModel.release < 1)then
      repeat with A1 = 1 to ropeModel.segments.count/2 then
        fac = Power(1.0 - ((A1.float-1.0)/(ropeModel.segments.count/2)), 2)
        A = ropeModel.segments.count + 1 - A1
        ropeModel.segments[A].vel = ropeModel.segments[A].vel - dir*fac*ropeModel.edgeDirection
      end repeat
      idealFirstPos = ropeModel.posB - dir * ropeModel.segmentLength
      ropeModel.segments[ropeModel.segments.count].pos = point(lerp(ropeModel.segments[ropeModel.segments.count].pos.locH, idealFirstPos.locH, ropeModel.edgeDirection), lerp(ropeModel.segments[ropeModel.segments.count].pos.locV, idealFirstPos.locV, ropeModel.edgeDirection))
    end if
  end if
  
  if(ropeModel.release > -1)then
    ropeModel.segments[1].pos = ropeModel.posA
    ropeModel.segments[1].vel = point(0,0)
  end if
  if(ropeModel.release < 1)then
    ropeModel.segments[ropeModel.segments.count].pos = ropeModel.posB
    ropeModel.segments[ropeModel.segments.count].vel = point(0,0)
  end if
  
  repeat with i = 1 to ropeModel.segments.count then
    ropeModel.segments[i].lastPos = ropeModel.segments[i].pos
    ropeModel.segments[i].pos = ropeModel.segments[i].pos + ropeModel.segments[i].vel
    ropeModel.segments[i].vel =   ropeModel.segments[i].vel * ropeModel.airFric
    ropeModel.segments[i].vel.locV = ropeModel.segments[i].vel.locV + ropeModel.grav
  end repeat
  
  repeat with i = 2 to ropeModel.segments.count then
    ConnectRopePoints(i, i-1)
    if(ropeModel.rigid > 0)then
      ApplyRigidity(i)
    end if
  end repeat
  
  repeat with i = 2 to ropeModel.segments.count then
    a = ropeModel.segments.count - i + 1
    ConnectRopePoints(a, a+1)
    if(ropeModel.rigid > 0)then
      ApplyRigidity(a)
    end if
  end repeat
  
  if(ropeModel.selfPush > 0)then
    repeat with A = 1 to ropeModel.segments.count then
      repeat with B = 1 to ropeModel.segments.count then
        if(A <> B) and (diagWI(ropeModel.segments[A].pos, ropeModel.segments[B].pos, ropeModel.selfPush))then
          dir = MoveToPoint(ropeModel.segments[A].pos, ropeModel.segments[B].pos, 1.0)
          dist = Diag(ropeModel.segments[A].pos, ropeModel.segments[B].pos)
          mov = dir * (dist-ropeModel.selfPush)
          
          ropeModel.segments[A].pos = ropeModel.segments[A].pos + mov * 0.5
          ropeModel.segments[A].vel = ropeModel.segments[A].vel + mov * 0.5
          ropeModel.segments[B].pos = ropeModel.segments[B].pos - mov * 0.5
          ropeModel.segments[B].vel = ropeModel.segments[B].vel - mov * 0.5
        end if
      end repeat
    end repeat
  end if
  
  if(ropeModel.sourcePush > 0)then
    repeat with A = 1 to ropeModel.segments.count then
      ropeModel.segments[A].vel =   ropeModel.segments[A].vel + MoveToPoint(ropeModel.posA, ropeModel.segments[A].pos, ropeModel.sourcePush) * restrict(((A-1.0)/(ropeModel.segments.count-1.0))-0.7,0,1)
      ropeModel.segments[A].vel =   ropeModel.segments[A].vel + MoveToPoint(ropeModel.posB, ropeModel.segments[A].pos, ropeModel.sourcePush) * restrict((1.0-((A-1.0)/(ropeModel.segments.count-1.0)))-0.7,0,1)
    end repeat
  end if
  
  repeat with i = 1 + (ropeModel.release > -1) to ropeModel.segments.count-(ropeModel.release < 1) then
    PushRopePointOutOfTerrain(i)
  end repeat
  
  if(preview)then
    member("ropePreview").image.copyPixels(member("pxl").image,  member("ropePreview").image.rect, rect(0,0,1,1), {#color:color(255, 255, 255)})
    repeat with i = 1 to ropeModel.segments.count then
      adaptedPos = me.SmoothedPos(i)
      adaptedPos = adaptedPos - cameraPos*20.0
      adaptedPos = adaptedPos * previewScale
      member("ropePreview").image.copyPixels(member("pxl").image, rect(adaptedPos-point(1,1), adaptedPos+point(2,2)), rect(0,0,1,1), {#color:color(0, 0, 0)})
    end repeat
  end if
end


on ConnectRopePoints(A, B)
  dir = MoveToPoint(ropeModel.segments[A].pos, ropeModel.segments[B].pos, 1.0)
  dist = Diag(ropeModel.segments[A].pos, ropeModel.segments[B].pos)
  if(ropeModel.stiff = 1)or(dist > ropeModel.segmentLength)then
    mov = dir * (dist-ropeModel.segmentLength)
    
    ropeModel.segments[A].pos = ropeModel.segments[A].pos + mov * 0.5
    ropeModel.segments[A].vel = ropeModel.segments[A].vel + mov * 0.5
    ropeModel.segments[B].pos = ropeModel.segments[B].pos - mov * 0.5
    ropeModel.segments[B].vel = ropeModel.segments[B].vel - mov * 0.5
  end if
end

on ApplyRigidity(A)
  repeat with B2 in [-2, 2, -3, 3, -4, 4] then
    B = A+B2
    if(B>0)and(B<= ropeModel.segments.count)then
      dir = moveToPoint(ropeModel.segments[A].pos, ropeModel.segments[B].pos, 1.0)
      ropeModel.segments[A].vel = ropeModel.segments[A].vel - (dir * ropeModel.rigid * ropeModel.segmentLength)/(Diag(ropeModel.segments[A].pos, ropeModel.segments[B].pos)+0.1+abs(B2))
      ropeModel.segments[B].vel = ropeModel.segments[B].vel + (dir * ropeModel.rigid * ropeModel.segmentLength)/(Diag(ropeModel.segments[A].pos, ropeModel.segments[B].pos)+0.1+abs(B2))
    end if
  end repeat
end

on SmoothedPos me, A
  if(a = 1)then
    if(ropeModel.release > -1)then
      return ropeModel.posA
    else
      return ropeModel.segments[a].pos
    end if
  else if (a = ropeModel.segments.count)then
    if(ropeModel.release < 1)then
      return ropeModel.posB
    else
      return ropeModel.segments[a].pos
    end if
  else
    smoothpos = (ropeModel.segments[a-1].pos + ropeModel.segments[a+1].pos)/2.0
    return (ropeModel.segments[a].pos + smoothpos)/2.0
  end if
end

on PushRopePointOutOfTerrain(A)
  p = [#Loc:ropeModel.segments[A].pos, #LastLoc:ropeModel.segments[A].lastPos, #Frc:ropeModel.segments[A].vel, #SizePnt:point(ropeModel.segRad,ropeModel.segRad)]
  p = sharedCheckVCollision(p, ropeModel.friction, ropeModel.layer)
  ropeModel.segments[A].pos = p.Loc
  ropeModel.segments[A].vel = p.Frc
  
  gridPos = giveGridPos(ropeModel.segments[A].pos)
  repeat with dir in [point(0,0), point(-1,0), point(-1,-1), point(0, -1), point(1, -1), point(1,0), point(1,1), point(0, 1), point(-1, 1)] then
    if afaMvLvlEdit(gridPos+dir, ropeModel.layer) = 1 then
      midPos = giveMiddleOfTile(gridPos+dir)
      terrainPos = point(restrict(ropeModel.segments[A].pos.locH, midPos.locH-10, midPos.locH+10), restrict(ropeModel.segments[A].pos.locV, midPos.locV-10, midPos.locV+10))
      terrainPos = ((terrainPos * 10.0) + midPos) / 11.0
      
      dir = MoveToPoint(ropeModel.segments[A].pos, terrainPos, 1.0)
      dist = Diag(ropeModel.segments[A].pos, terrainPos)
      if(dist < ropeModel.segRad)then
        mov = dir * (dist-ropeModel.segRad)
        ropeModel.segments[A].pos = ropeModel.segments[A].pos + mov 
        ropeModel.segments[A].vel = ropeModel.segments[A].vel + mov 
      end if
    end if
  end repeat
  
  
end








on sharedCheckVCollision(p, friction, layer)
  bounce = 0
  
  
  if p.Frc.locV > 0 then
    lastGridPos = giveGridPos(p.LastLoc)
    feetPos = giveGridPos(p.Loc+point(0, p.SizePnt.locV+0.01))
    lastFeetPos = giveGridPos(p.LastLoc+point(0, p.SizePnt.locV))
    leftPos = giveGridPos(p.Loc+point(-p.SizePnt.locH+1, p.SizePnt.locV+0.01))
    rightPos = giveGridPos(p.Loc+point(p.SizePnt.locH-1, p.SizePnt.locV+0.01))
    repeat with q = lastFeetPos.locV to feetPos.locV then
      repeat with c = leftPos.locH to rightPos.locH then
        
        if (afaMvLvlEdit(point(c, q), layer) = 1)and(afaMvLvlEdit(point(c, q-1), layer) <> 1) then
          --if (c = gridPos.locH)or(ABS(p.Frc.locV)>ABS(p.Frc.LocH))then
          if (lastGridPos.locV >= q)and(afaMvLvlEdit(lastGridPos, layer) = 1) then
          else
            p.Loc.locV = ((q-1)*20.0)-p.SizePnt.locV--+8
            p.Frc.locH = p.Frc.locH * friction
            p.Frc.locV = -p.Frc.locV*bounce
            return p
            exit
          end if
          -- end if
        end if
      end repeat
    end repeat
    
  else if p.Frc.locV < 0 then
    lastGridPos = giveGridPos(p.LastLoc)
    headPos = giveGridPos(p.Loc-point(0, p.SizePnt.locV+0.01))
    lastHeadPos = giveGridPos(p.LastLoc-point(0, p.SizePnt.locV))
    leftPos = giveGridPos(p.Loc+point(-p.SizePnt.locH+1, p.SizePnt.locV+0.01))
    rightPos = giveGridPos(p.Loc+point(p.SizePnt.locH-1, p.SizePnt.locV+0.01))
    repeat with d = headPos.locV to lastHeadPos.locV then
      q = (lastHeadPos.locV) - (d-headPos.locV)--, 1, 38-1)--1--restrict added after
      repeat with c = leftPos.locH to rightPos.locH then
        if (afaMvLvlEdit(point(c, q), layer) = 1)and(afaMvLvlEdit(point(c, q+1), layer) <> 1) then
          if (lastGridPos.locV <= q)and(afaMvLvlEdit(lastGridPos, layer) <> 1) then
          else
            p.Loc.locV = (q*20.0)+p.SizePnt.locV
            p.Frc.locH = p.Frc.locH * friction
            p.Frc.locV = -p.Frc.locV*bounce
            return p
            exit
          end if
        end if
      end repeat
    end repeat
  end if
  
  return p
end

