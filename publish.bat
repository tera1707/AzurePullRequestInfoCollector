cd %~dp0
dotnet publish PullReqInfoCollector.sln -c Debug -r win-x64 --output .\publish --self-contained -p:RuntimeIdentifier=win-x64 -p:Platform=x64 -p:PublishSingleFile=true
pause