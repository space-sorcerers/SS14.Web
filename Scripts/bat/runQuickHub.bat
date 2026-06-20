@echo off
cd ../../

call dotnet run --project SS14.ServerHub --no-build %*

pause
