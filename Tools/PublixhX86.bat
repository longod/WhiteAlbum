@echo off
pushd %~dp0\..\

@rem 共有ファイルも一つになるのでexeの数分容量が増えがち
@rem dotnet publish -p:Configuration=Release -p:Platform=x86 -r win-x86 --self-contained false -p:DebugType=None -p:PublishSingleFile=true -o Publish\
dotnet publish -p:Configuration=Release -p:Platform=x86 -r win-x86 --self-contained false -p:DebugType=None -o Publish\

popd
