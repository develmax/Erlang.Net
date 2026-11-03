# Runtime build profile

The three runtime libraries `Erlang.Runtime.Core`, `Erlang.Terms` and `Erlang.Serialization` enable compiler/JIT optimization in both Debug and Release. This also applies to ordinary .NET project references. Debug symbols and Debug conditional assertions remain enabled.

Tests, the Erlang compiler and native observation hosts retain their normal build configuration. The policy is declared in the repository `Directory.Build.props`.

For managed step-through debugging with unoptimized runtime locals, explicitly rebuild:

```powershell
dotnet build -c Debug -t:Rebuild -p:ErlangRuntimeOptimize=false
```

Return to the normal runtime profile with:

```powershell
dotnet build -c Debug -t:Rebuild -p:ErlangRuntimeOptimize=true
```

An explicit rebuild matters when switching compiler options in the same output directory: an incremental invocation can retain the previous DLL. Part550 validation inspects actual PE DebuggableAttribute flags and DLL hashes, together with the unchanged test deadlines and complete original OTP outcomes.
