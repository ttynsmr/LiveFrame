@echo off
setlocal enabledelayedexpansion
cd %~dp0

dotnet restore
msbuild LiveFrame.csproj -t:Build -p:Configuration=Release
