# Serilog.Sink.Cache

Caching Sink for Serilog
This Sink forwards all LogEvents to its output sinks when there is an available network connection.
When there is no connection to the internet, all LogEvents are stored in a local database and forwarded as soon as a network connection is detected.

[![NuGet Badge](https://buildstats.info/nuget/serilog.sink.cache)](https://www.nuget.org/packages/Serilog.Sink.Cache/)
[![Build Status](https://app.bitrise.io/app/b8ab60d5eddce15a/status.svg?token=kBuN-dQKUhDpaOp0ntsBSw&branch=master)](https://app.bitrise.io/app/b8ab60d5eddce15a)
[![CodeFactor](https://www.codefactor.io/repository/github/digitalrmdy/serilog.sink.caching/badge)](https://www.codefactor.io/repository/github/digitalrmdy/serilog.sink.caching)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=digitalrmdy_Serilog.Sink.Caching&metric=code_smells)](https://sonarcloud.io/dashboard?id=digitalrmdy_Serilog.Sink.Caching)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=digitalrmdy_Serilog.Sink.Caching&metric=sqale_rating)](https://sonarcloud.io/dashboard?id=digitalrmdy_Serilog.Sink.Caching)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=digitalrmdy_Serilog.Sink.Caching&metric=security_rating)](https://sonarcloud.io/dashboard?id=digitalrmdy_Serilog.Sink.Caching)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=digitalrmdy_Serilog.Sink.Caching&metric=coverage)](https://sonarcloud.io/dashboard?id=digitalrmdy_Serilog.Sink.Caching)
[![Dependabot Status](https://api.dependabot.com/badges/status?host=github&repo=digitalrmdy/Serilog.Sink.Caching)](https://dependabot.com)


## Usage

```
Log.Logger = new LoggerConfiguration()
    .WithCache("connectionString")
        .AddSink(new ConsoleSink(...)          // cache when offline
        .BuildCaching()
    .WriteTo().Console()    // always log here
    .CreateLogger();
```