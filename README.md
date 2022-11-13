# Visual Studio for Mac addin compat tool

Checks an addin's compatibility with Visual Studio for Mac. 

## Installation

    dotnet tool install vsmac-addin-compact

## Usage

```
dotnet vsmac-addin-compat --help
dotnet vsmac-addin-compat --vsmac-app "/Applications/Visual Studio.app" --addin-dir ./addin
dotnet vsmac-addin-compat --vsmac-preview --addin-dir ./addin
dotnet vsmac-addin-compat --addin-dir ./addin
dotnet vsmac-addin-compat --mpack MyExtension.mpack
dotnet vsmac-addin-compat
```