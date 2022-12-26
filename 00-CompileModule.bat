@echo off

call MC7D2D EnclosedSpace.dll ^
  /reference:"%PATH_7D2D_MANAGED%\Assembly-CSharp.dll" ^
  Harmony\*.cs && ^
echo Successfully compiled EnclosedSpace.dll

pause