@echo off
setlocal enabledelayedexpansion
cd %~dp0

dotnet restore
dotnet publish --runtime win-x64 --configuration Release --self-contained false

