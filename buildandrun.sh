#!/bin/sh
MONO_PATH=/usr/lib/mono/4.5/
EXE_PATH=./bin/Debug/net452/MouseIssue.exe

echo "[[ Setting FrameworkPathOverride to $MONO_PATH ]]"
export FrameworkPathOverride=$MONO_PATH

echo "[[ Running 'dotnet build' ... ]]"
echo ""
dotnet build

echo ""
echo "[[ Running executable ... ]]"
echo ""
mono $EXE_PATH
