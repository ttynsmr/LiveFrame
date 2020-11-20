@echo off
setlocal enabledelayedexpansion
cd %~dp0

call BuildReleaseApplication.bat
call RunReleaseApplication.bat
