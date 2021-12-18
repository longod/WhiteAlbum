@rem https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test
@echo off
pushd %~dp0
dotnet test ..\WhiteAlbum.sln -s ..\WA.Test.runsettings
popd
pause
