# ConcurrencyAnalyzers.NET

The ConcurrencyAnalyzers.NET performs analysis of concurrency-related code for target C# applications. 
It consists of a runtime library and a Roslyn Analyzer project that is capable to perform compilation-time transformations (thanks to project [Metalama.Compiler](https://github.com/postsharp/Metalama.Compiler)). 
The Roslyn Analyzer project performs instrumentation of target application that is then analyzed during execution by the runtime library. 

> [!WARNING]
> This project is experimental proof-of-concept. It is not tested in real applications yet. A lot of things are subject to change. 

## Building

To build the project, execute the standard `dotnet build` command on the whole solution.

```bash
dotnet build src/ConcurrencyAnalyzers.NET.sln
```

## Documentation

1. Add the following references to the projects that you want to analyze:

```xml
<ItemGroup>
    <ProjectReference Include="..\<your-path-to>\Concurrency.Analyzers.NET.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />

    <ProjectReference Include="..\<your-path-to>\Concurrency.Analyzers.NET.Runtime.csproj" />

    <PackageReference Include="Metalama.Compiler" Version="2024.0.8" PrivateAssets="all" />
</ItemGroup>
```

2. Optional: if you want to inspect instrumented code or need debugging support, you can use define the following MSBuild properties:

```xml
<PropertyGroup>
    <MetalamaDebugTransformedCode>true</MetalamaDebugTransformedCode>
    <MetalamaEmitCompilerTransformedFiles>true</MetalamaEmitCompilerTransformedFiles>
</PropertyGroup>
```

3. Build the application.

4. Run your application. Analysis will be performed during the execution automatically.

*Note: NuGet packages for the analyzers are not published yet.*

## License

This project is licensed under the [Apache-2.0](LICENSE) license.