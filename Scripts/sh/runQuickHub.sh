#!/bin/sh
cd ../../

dotnet run --project SS14.ServerHub --no-build "$@"
