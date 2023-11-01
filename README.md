# dotnet-bdwgc

This is a working example of running the [Boehm-Demers-Weiser garbage collector](https://github.com/ivmai/bdwgc) within a .NET 7/8 application.

## Test

For windows development, you could simply modify the `Program.cs` file to test the GC.

```bash
python make.py run
```

For linux/macos development, download `libgc.*` into `$PROJECT_ROOT/deps`. Then run:

```bash
python make.py run
```
