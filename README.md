# Erlang.Net

An experimental C#/.NET implementation targeting Erlang/OTP **29.1.1**, pinned to `ad05823719d77c8faee87348ea39513d4e2f99c5`. The full assignment is not complete. See [progress](docs/PROGRESS.md), [compatibility](docs/COMPATIBILITY.md), [detailed changelog](CHANGELOG.md) and [next steps](docs/NEXT_STEPS.md).

The implemented subset runs on .NET 10 without BEAM. It includes immutable terms, selective receive, local processes/links/monitors, an Erlang parser and evaluator, generated C# modules, initial OTP behaviours and a mixed C#/Erlang MSBuild preprocessor.

The source subset includes map construction, associative/exact updates and map patterns with supported guard-expression keys. Overall project completion is estimated at roughly 5%; this is an engineering estimate, not verified compatibility coverage. See progress and the changelog for evidence and remaining scope.

```powershell
dotnet build
dotnet run --project tests/Erlang.Tests --no-build -- artifacts/tests.json
dotnet run --project examples/HelloHybrid --no-build
```

The example prints `Hello World`, receives a stop message, and checks a compiled `.erl` module returning 42. Native Erlang syntax appears in an ordinary `.cs` file:

```csharp
var result = receive
    {hello, Name} when is_list(Name) ->
        io:format("Hello ~s~n", [Name]), ok;
    {stop} -> stop
end.
```

The enclosing method/lambda must be async and supply `ProcessContext erlangProcess`. Erlang constructs currently begin at C# statement/assignment/return/lambda-body boundaries; `end.` terminates each block. Blocks are independent Erlang scopes. C# locals are not implicitly imported into them. Synchronous suspension, arbitrary nested hybrid expressions and IDE language services are pending.

SDK 10.0.400 is pinned in `global.json`. On restricted hosts that prevent MSBuild worker processes, use `dotnet build -m:1`.

Compile Erlang source to C#:

```powershell
dotnet tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll compile examples/HelloHybrid/arithmetic.erl artifacts/arithmetic.g.cs
```

Generate the pinned source inventory without changing the reference checkout:

```powershell
dotnet tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll inventory ../otp docs
```

Validate build integration and a local NuGet consumer:

```powershell
pwsh -File tools/validate.ps1
```

No packages are published. The prototype package targets net10.0. Install the local tool package to run `dotnet erlang compile INPUT OUTPUT`; without installation, use the DLL command above. Compilation currently emits AST construction code evaluated by the C# runtime, rather than direct native lowering of every operation.

Tests are a dependency-free executable harness and return a failing exit code on any failed assertion. `dotnet test` is not the test entry point. A separate differential runner requires an exact OTP 29.1.1 development oracle; production execution and builds do not.
