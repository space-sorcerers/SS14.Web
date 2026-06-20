@echo off
cd ../../

call dotnet run --project SS14.Auth --no-build %*

pause
