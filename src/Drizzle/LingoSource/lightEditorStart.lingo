global gTEprops, gTiles, gEEprops, gLightEProps, firstFrame, geverysecond, glgtimgQuad, gDirectionKeys,gLOprops

on exitFrame me
  _movie.exitLock = TRUE
  firstFrame = 1
  l = [ #m1:1, m2:0, #w:0, #a:0, #s:0, #d:0, #r:0, f:0]
  gLightEProps.lastKeys = l.duplicate()
  gLightEProps.keys = l.duplicate()
  
  repeat with l = 1 to 3 then
    miniLvlEditDraw(l)
  end repeat
  
  geverysecond = 0
  
  gDirectionKeys = [0,0,0,0]
  
  glgtimgQuad = [point(0,0), point(member("lightImage").image.width,0), point(member("lightImage").image.width,member("lightImage").image.height), point(0,member("lightImage").image.height)]
  
  gLightEProps.lastTm = _system.milliseconds
  
  sprite(181).member = member("pxl")
  sprite(182).member = member("pxl")
  gLightEProps.paintShape = "pxl"
  
  sprite(175).rect = rect(0,0,gLOprops.size.loch*20,gLOprops.size.locv*20)
  
  sprite(178).rect = rect(0,0,gLOprops.size.loch*20,gLOprops.size.locv*20)
  
  sprite(179).member = member("lightImage")
  sprite(180).member = member("lightImage")
  sprite(176).member = member("lightImage")
end


