@echo off
rem Run this batch script with this directory as the CWD.

rem Clean release directory.
if exist ..\temp-release echo Deleting previous temporary release dir. && rmdir /S /Q ..\temp-release\
mkdir ..\temp-release

echo .git >> ..\temp-release\exclude.txt
echo makerelease.bat >> ..\temp-release\exclude.txt
echo tasks.txt >> ..\temp-release\exclude.txt
echo Source\dehydration\bin >> ..\temp-release\exclude.txt
echo Source\dehydration\obj >> ..\temp-release\exclude.txt

echo Copying to temporary release dir.
xcopy . ..\temp-release\Dehydration\ /E /EXCLUDE:..\temp-release\exclude.txt /Q

echo Creating zip.
"C:\Program Files\7-Zip\7z.exe" a Dehydration.zip ..\temp-release\Dehydration
