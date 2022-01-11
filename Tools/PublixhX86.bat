@echo off
pushd %~dp0\..\

@rem todo use p:PublishSingleFile=true
@rem https://www.hanselman.com/blog/making-a-tiny-net-core-30-entirely-selfcontained-single-executable
dotnet publish -p:Configuration=Release -p:Platform=x86 -r win-x86 --self-contained false -o Publish\

popd
