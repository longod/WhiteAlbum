# WHITE ALBUM

WHITE ALBUM is a lightweight Image Viewer with a mix of modern and legacy features.

[![Build](https://github.com/longod/WhiteAlbum/actions/workflows/build.yml/badge.svg)](https://github.com/longod/WhiteAlbum/actions/workflows/build.yml)

## Concept
- Fast and Lightweight
    - Avoid unnecessary decoration and animation, therefore white
- Use modern features
    - dotnet core
    - WPF
    - async and parallel
    - and other plugins (TBD)
- Respect legacy features
    - support Susie Plugin (only **x86**)
    - and other plugins (TBD)

## Requirements
- Windows x64 or x86
- Visual Studio 2019
- .NET Core 3.1 (for Prism)

## Projects

| Project          | Description |
|------------------|---|
| WA.Viewer        | Main Viewer Application |
| WA.Catalog (TBD) | Main Catalog Application |
| WA               | Main Library |
| WA.Susie         | Interoperate Susie Plugin |
| WA.Test          | xUnit Test |
| WA.Benchmark     | BenchmarkDotNet Benchmark |
| WA.Blank         | Minimized WPF application for comparing |
| axnull (VC)      | Stub (null) archive extractor Susie Plugin |
| ifnull (VC)      | Stub (null) image filter Susie Plugin |

### Dependency
```
WA.Susie    ->  WA  ->  WA.Viewer
                    ->  WA.Catalog (TBD) 
                    ->  WA.Test
                    ->  WA.Benchmark
```

## Build and Run

**Currently, WHITE ALBUM supports only x86.**

1. Open `WhiteAlbum.sln`
1. Set `Debug` or `Release` and `x86`
1. Build it

## How to view

- Write a file path to 1st commandline argument
    - Drop a file to execution binary file
- Drop a file to the main window

## Susie Plug-in
TODO

https://www.digitalpad.co.jp/~takechin/

