
## Usage

```
Log.Logger = new LoggerConfiguration()
    .WithCache("connectionString")
        .AddSink(new ConsoleSink(...)          // cache when offline
        .BuildCaching()
    .WriteTo().Console()    // always log here
    .CreateLogger();
```