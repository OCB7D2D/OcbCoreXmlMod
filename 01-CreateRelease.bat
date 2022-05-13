@echo off

SET NAME=0cbCoreXmlMod
set ZIPNAME=OcbCoreXmlMod

if not exist build\ (
  mkdir build
)

if exist build\%NAME%\ (
  echo remove existing directory
  rmdir build\%NAME% /S /Q
)

mkdir build\%NAME%

SET VERSION=snapshot

if not "%1"=="" (
  SET VERSION=%1
)

echo create %VERSION%

xcopy *.xml build\%NAME%\
xcopy *.md build\%NAME%\
xcopy *.dll build\%NAME%\
xcopy Config build\%NAME%\Config\ /S
xcopy Resources build\%NAME%\Resources\ /S
xcopy UIAtlases build\%NAME%\UIAtlases\ /S

cd build
echo Packaging %NAME%-%VERSION%.zip
powershell Compress-Archive %NAME% %ZIPNAME%-%VERSION%.zip -Force
cd ..

SET RV=%ERRORLEVEL%
if "%CI%"=="" pause
exit /B %RV%
