@rem https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test
@echo off
pushd %~dp0\..\

@rem we need x86 tool set for MSBuild and dotnet
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\Tools\VsDevCmd.bat"

@rem C/C++ projects were not build by dotnet, so we manually build them.
@rem MSBuild WhiteAlbum.sln -m -t:build -p:Configuration=Debug -p:Platform=x86
MSBuild spi\axnull\axnull.vcxproj -m -warnAsError -t:build -p:Platform=Win32;Configuration=Debug;SolutionDir=..\..\
MSBuild spi\ifnull\ifnull.vcxproj -m -warnAsError -t:build -p:Platform=Win32;Configuration=Debug;SolutionDir=..\..\

@rem run test
dotnet test WhiteAlbum.sln -s WA.Test.runsettings

popd
pause
