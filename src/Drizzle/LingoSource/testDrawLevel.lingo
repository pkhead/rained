global gSkyColor, lightRects, gFullRender
on exitFrame me
  gFullRender = 0
  lightRects = [rect(0,0,0,0), rect(0,0,0,0)]
  drawTestLevel()
  member("finalfg").image.setPixel(0, 0, gSkyColor)--skyColor
  member("finalfg").image.setPixel(1, 0, gSkyColor)--fogColor
  member("finalfg").image.setPixel(2, 0, color(10,10,10))--blackColor
  member("finalfg").image.setPixel(3, 0, color(10,10,10))--itemcolorColor
  
  member("finalfg").image.setPixel(0, 1, color(10,10,10))
  member("finalfg").image.setPixel(1, 1, color(10,10,10))
  global gLoadedName, levelName
  levelName = gLoadedName
  member("TextInput").text = gLoadedName
  
  put "I'M DOING A TEST RENDER!"
  alert("I'M DOING A TEST RENDER!")
  
  go(76)
end