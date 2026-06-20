#!/bin/sh
cd ../../

git submodule update --init --recursive
dotnet build -c Debug
