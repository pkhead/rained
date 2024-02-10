 on saveImages savePath
  retVal = 1
  -- check savePath
  if (not stringP(savePath)) and (not savePath contains the dirSeparator) then
    retVal = -1
    return retVal
  else
    if the last char of savePath <> the dirSeparator then 
      savePath = savePath & the dirSeparator
    end if
  end if
  
  -- create instance of ImgXtra
  imgObj = xtra("ImgXtra").new()
  
  -- iterate through the Internatl Cast
  -- and save as bmp using the member name, 
  -- or if empty then "member_#"
  m = castLib(4).member.count
  repeat with i = 1 to m
    if member(i, 4).type = #bitmap then
      fName = member(i, 4).name & ".bmp"
      if fName = ".bmp" then fName = "member_" & i & ".bmp"
      put "savePath: " & savePath & fName
      ixErr = imgObj.ix_saveImage(["image":  member(i, 4).image, "filename": savePath & fName, "format": "BMP"])
      if ixErr = 0 then 
        retVal = -2
      end if
    end if
  end repeat

  -- destroy instance of ImgXtra
  imgXtra = 0
  
  return retVal
end