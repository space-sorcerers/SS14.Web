#!/bin/sh
cd ../../

dotnet run --project SS14.Auth --no-build "$@"
