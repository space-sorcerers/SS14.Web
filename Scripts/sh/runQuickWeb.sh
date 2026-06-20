#!/bin/sh
cd ../../

dotnet run --project SS14.Web --no-build "$@"
