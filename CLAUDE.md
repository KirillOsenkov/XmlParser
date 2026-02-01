# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
dotnet build Microsoft.Language.Xml.sln
dotnet test test/Microsoft.Language.Xml.Tests/Microsoft.Language.Xml.Tests.csproj
dotnet test test/Microsoft.Language.Xml.Editor.Tests/Microsoft.Language.Xml.Editor.Tests.csproj

# Run a single test
dotnet test test/Microsoft.Language.Xml.Tests/Microsoft.Language.Xml.Tests.csproj --filter "FullyQualifiedName~TestMethodName"

# Benchmarks
dotnet run -c Release --project benchmark/Microsoft.Language.Xml.Benchmarks/Microsoft.Language.Xml.Benchmarks.csproj
```

Note: The core library targets netstandard2.0. Test and editor projects target net472.

## Architecture

This is a Roslyn-inspired full-fidelity XML parser. Every character of input (including whitespace/trivia) is preserved in the syntax tree. It is error-tolerant and supports incremental parsing.

### Green/Red Tree Pattern

The parser uses Roslyn's two-layer immutable tree design:

- **Green nodes** (`src/Microsoft.Language.Xml/InternalSyntax/`): No parent pointers, structurally shared and cached. Built by the parser.
- **Red nodes** (`src/Microsoft.Language.Xml/Syntax/`): Wrap green nodes, provide parent pointers and absolute positions. Created lazily on demand.

### Parsing Pipeline

`Parser.cs` drives parsing; `Scanner.cs` handles lexical tokenization. `Blender.cs` supports incremental re-parsing by reusing unchanged green nodes.

### Key Entry Point

`Parser.ParseText(string xml)` returns an `XmlDocumentSyntax` (red node) representing the full document.

### Tree Traversal

`SyntaxVisitor` and `SyntaxRewriter` implement the visitor pattern for read-only traversal and immutable tree transformation respectively.

## Project Layout

- `src/Microsoft.Language.Xml/` — Core parser library (NuGet: GuiLabs.Language.Xml)
- `src/Microsoft.Language.Xml.Editor/` — VS editor integration: classification, outlining, smart indent, comment toggling (NuGet: GuiLabs.Language.Xml.Editor)
- `test/Microsoft.Language.Xml.Tests/` — Core parser tests (xUnit)
- `test/Microsoft.Language.Xml.Editor.Tests/` — Editor tests (xUnit)
