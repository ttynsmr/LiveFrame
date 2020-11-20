@echo off
setlocal enabledelayedexpansion
cd %~dp0

msbuild LiveFrame.csproj -t:Build -p:Configuration=Release
