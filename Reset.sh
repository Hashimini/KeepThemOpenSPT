#!/bin/bash

rm -rf bin obj
dotnet clean
dotnet restore
clear
echo "Cleansed"