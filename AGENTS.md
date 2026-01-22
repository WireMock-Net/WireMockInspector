# Agent Guidelines for WireMockInspector

This document provides coding guidelines and development commands for agentic coding tools working on the WireMockInspector codebase.

## Project Overview

WireMockInspector is a cross-platform UI application built with Avalonia (C# MVVM framework) that helps troubleshoot WireMock servers. The solution contains:
- `WireMockInspector` - Main Avalonia UI application (dotnet tool)
- `WireMock.Net.Extensions.WireMockInspector` - Extension library for WireMock.Net

## Build Commands

### Prerequisites
- .NET 6.0 SDK or higher (project targets .NET 6.0)
- Solution path: `src\WireMockInspector.sln`

### Common Build Commands

```bash
# Navigate to source directory
cd src

# Restore dependencies
dotnet restore WireMockInspector.sln

# Build entire solution (Debug)
dotnet build WireMockInspector.sln

# Build specific project
dotnet build WireMockInspector\WireMockInspector.csproj
dotnet build WireMock.Net.Extensions.WireMockInspector\WireMock.Net.Extensions.WireMockInspector.csproj

# Build in Release mode
dotnet build WireMockInspector.sln -c Release

# Clean build artifacts
dotnet clean WireMockInspector.sln

# Create NuGet packages (GeneratePackageOnBuild is enabled)
dotnet pack WireMockInspector.sln -c Release

# Run the application
dotnet run --project WireMockInspector\WireMockInspector.csproj

# Install as global tool locally
dotnet pack WireMockInspector\WireMockInspector.csproj -c Release
dotnet tool uninstall WireMockInspector -g
dotnet tool install --global --add-source WireMockInspector\bin\Release WireMockInspector

# Run installed tool
wiremockinspector
```

## Test Commands

**Note:** This project currently has no test projects. When writing tests in the future:

```bash
# Run all tests (when tests exist)
dotnet test WireMockInspector.sln

# Run tests with detailed output
dotnet test WireMockInspector.sln --verbosity detailed

# Run specific test project (when created)
dotnet test path/to/TestProject.csproj

# Run single test method (when tests exist)
dotnet test --filter "FullyQualifiedName=Namespace.ClassName.TestMethodName"
```

## Lint/Format Commands

**Note:** This project does not currently use explicit linting tools like StyleCop or dotnet-format. Code style is maintained through:
- ReSharper settings (`.sln.DotSettings` files)
- Manual code review
- Visual Studio/Rider formatting

Future recommendations:
```bash
# Install dotnet-format tool
dotnet tool install -g dotnet-format

# Format code
dotnet format WireMockInspector.sln
```

## Code Style Guidelines

### General Conventions

1. **Target Framework**: .NET 6.0 for main app, netstandard2.0/2.1 for extensions
2. **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`)
3. **Language Version**: C# 10 minimum (LangVersion 10 for extensions)

### Naming Conventions

- **Classes/Methods**: PascalCase
- **Private fields**: `_camelCase` with underscore prefix
- **Properties**: PascalCase
- **Local variables**: camelCase
- **Constants**: PascalCase
- **Interfaces**: IPascalCase (prefix with 'I')

### File Organization

1. **Namespace Style**: File-scoped namespaces (C# 10+)
   ```csharp
   namespace WireMockInspector.CodeGenerators;
   
   public class MyClass { }
   ```

2. **Using Directives**: Place at top of file, before namespace
   ```csharp
   using System;
   using System.Linq;
   using Avalonia;
   using ReactiveUI;
   
   namespace WireMockInspector;
   ```

3. **File Structure**:
   - Views: `src/WireMockInspector/Views/*.axaml` and `*.axaml.cs`
   - ViewModels: `src/WireMockInspector/ViewModels/*.cs`
   - Code Generators: `src/WireMockInspector/CodeGenerators/*.cs`

### Class Structure

- Order members: fields, constructors, properties, methods
- Private fields before properties
- Use `ObservableAsPropertyHelper<T>` for computed properties in ViewModels

```csharp
public class ExampleViewModel : ViewModelBase
{
    private string _fieldName;
    private readonly ObservableAsPropertyHelper<string> _computedValue;
    
    public string PropertyName
    {
        get => _fieldName;
        set => this.RaiseAndSetIfChanged(ref _fieldName, value);
    }
    
    public string ComputedValue => _computedValue.Value;
    
    public ExampleViewModel()
    {
        this.WhenAnyValue(x => x.PropertyName)
            .Select(x => x.ToUpper())
            .ToProperty(this, x => x.ComputedValue, out _computedValue);
    }
}
```

### Types and Nullability

- Use nullable reference types: `string?` for nullable, `string` for non-null
- Prefer `is not null` over `!= null`
- Use pattern matching where appropriate: `if (obj is MyType myType)`
- Use null-coalescing: `value ?? defaultValue`

### ReactiveUI Patterns

- ViewModels inherit from `ViewModelBase` (which inherits `ReactiveObject`)
- Use `this.RaiseAndSetIfChanged(ref field, value)` for property setters
- Use `WhenAnyValue()` for reactive property observables
- Use `ReactiveCommand.CreateFromTask()` for async commands
- Use `ToProperty()` for computed properties
- Subscribe on appropriate schedulers: `RxApp.MainThreadScheduler`, `RxApp.TaskpoolScheduler`

### Error Handling

- Use try-catch only where specific error handling is needed
- For reactive streams, use `.DiscardExceptions()` extension method or `.Catch()`
- Throw `InvalidOperationException` with descriptive messages for invalid states
- Include inner exceptions when rethrowing: `throw new Exception("message", innerException)`

### Async/Await

- Use `async Task` for asynchronous methods (not `async void` except event handlers)
- Always use `ConfigureAwait(false)` in library code
- Use `Task.WhenAll()` for parallel operations
- Prefer `ReactiveCommand.CreateFromTask()` over manual async handling in ViewModels

### Comments and Documentation

- Use XML documentation comments (`///`) for public APIs
- Keep inline comments minimal and meaningful
- Prefer self-documenting code over comments
- Document complex algorithms or non-obvious logic

### String Handling

- Use string interpolation: `$"Value: {variable}"` over `string.Format()`
- Use `string.IsNullOrWhiteSpace()` for validation
- Use `StringComparison.OrdinalIgnoreCase` for case-insensitive comparisons
- Use `StringBuilder` for concatenating multiple strings in loops

### LINQ and Collections

- Prefer LINQ methods over loops where appropriate
- Use `ObservableCollection<T>` for bindable collections in ViewModels
- Use `.ToList()`, `.ToArray()` to materialize queries when needed
- Chain LINQ methods on separate lines for readability in complex queries

### Code Generation

- Templates use Liquid syntax (Fluid.Core library)
- Store templates as embedded resources or in `%LOCALAPPDATA%\WireMockInspector\templates\`
- Use `CSharpFormatter.ToCSharpStringLiteral()` for escaping strings in generated code

## Git Commit Messages

Follow conventional commit style observed in the repository:

- Start with action verb (Handle, Update, Fix, Add, Refactor, etc.)
- Be concise but descriptive
- Examples from history:
  - "Fix annoying issue with code selection"
  - "Add JSON code generator for mapping definition"
  - "Refactor: Change namespace of CSharpFormatter class"
  - "Update package dependencies in project files"

## Project-Specific Notes

1. **Avalonia UI**: Uses AXAML (Avalonia XAML) for UI definitions
2. **ViewLocator**: Automatically maps ViewModels to Views by naming convention
3. **WireMock Integration**: Communicates with WireMock Admin API via RestClient
4. **Command Line**: Uses `System.CommandLine` for CLI parsing
5. **Code Highlighting**: Uses TextMate grammar for syntax highlighting
6. **Search**: Lucene.NET for request searching functionality

## Common Pitfalls to Avoid

1. Don't forget to dispose ReactiveCommand subscriptions
2. Don't use blocking calls on UI thread (use async/await)
3. Don't modify ObservableCollections off the main thread
4. Always handle null for optional WireMock response properties
5. Remember to update both AXAML and code-behind when modifying Views

## References

- Avalonia Documentation: https://docs.avaloniaui.net/
- ReactiveUI Documentation: https://www.reactiveui.net/
- WireMock.Net: https://github.com/WireMock-Net/WireMock.Net
