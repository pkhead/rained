
global gLOProps, newSize, extraBufferTiles, gLEprops
on exitFrame me
  _movie.exitLock = TRUE
  if _key.keyPressed("A") and _movie.window.sizeState <> #minimized then
    if (gLOprops.size <> point(newSize[1], newSize[2]))or(newSize[3]<>0)or(newSize[4]<>0) then
      if newSize[1] > 0 and newSize[2] > 0 and (-newSize[3] < newSize[1]) and (-newSize[4] < newSize[2])then
        resizeLevel(point(newSize[1], newSize[2]),newSize[3],newSize[4] )
      end if
    end if
    gLOProps.extraTiles = extraBufferTiles.duplicate()
    _movie.go(9)
  else if _key.keyPressed("C") and _movie.window.sizeState <> #minimized then
    _movie.go(9)
  else
    go the frame
  end if
end