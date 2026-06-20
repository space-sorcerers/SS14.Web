@echo off
cd ../../

call dotnet run --project SS14.Web --no-build %*

pause
