rem publish with seting in /Properties/PublishProfiles/FolderProfile.pubxml 
rem dotnet publish -p:PublishProfile=FolderProfile
dotnet publish -c Debug -r win-x64 --output .\\publish --self-contained -p:RuntimeIdentifier=win-x64 -p:Platform=x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
pause