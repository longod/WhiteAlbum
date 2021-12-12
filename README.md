# WHITE ALBUM

WHITE ALBUM is a lightweight Image Viewer, mixturing modern and legacy approach.

## Concept
- Fast and Lightweight
    - avoid unnecessary decoration and animation, therefore white
- Use Modern Foundation
    - dotnet core WPF
    - async and parallel
- Respect Legacy
    - support Susie Plugin (only **x86**)
    - and other plugins (TBD)

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
| axnull           | Stub (null) archive extractor Susie Plugin |
| ifnull           | Stub (null) image filter Susie Plugin |

### Dependency
WA.Susie -> WA -> WA.Viewer
