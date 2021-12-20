@rem https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test
@echo off
pushd %~dp0\..\

call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvars64.bat"

MSBuild WhiteAlbum.sln -m -t:build -p:Platform="Any CPU";Configuration=Release

WA.Benchmark\bin\Release\netcoreapp3.1\WA.Benchmark.exe

popd
