# Source project layout

Part486 categorizes `src` and, following the user's additional request, `tools`. Test projects retain their existing locations. Assembly identities, package identities, namespaces, backend selection and embedded resource names remain unchanged. This layout does not promote API compatibility.

```text
src/
  Runtime/              # terms, serialization, core runtime, bundle, native driver host
  Compiler/             # source and BEAM compiler
  Integration/          # ordinary .NET/MSBuild and package integration
  Otp/
    Managed/            # existing C# stdlib, kernel and behaviour alternatives
    Original/
      Foundation/       # erts, kernel, stdlib, sasl
      Development/      # compiler, syntax/parsing, tools, tests, analysis
      Data/             # ASN.1, Mnesia, XML
      Networking/       # application protocols and network management
      Security/         # crypto, public_key, ssl, ssh
      Gui/              # wx and graphical tools
      Platform/         # os_mon and ODBC
      Documentation/    # edoc
```

There are44 source projects: five runtime projects, one compiler project, one integration project, three managed OTP projects and34 original OTP libraries. Original pure modules stay Erlang/BEAM; C# implements runtime/native primitives and existing explicitly selected alternatives. Preparation of1321 primary images is distinct from application execution or complete API compatibility.

The65 existing tool directories are grouped under `tools/Build`, `Validation`, `Native`, `Distribution`, `Collections`, `Compiler` and `Runtime`. Public root `tools/*.ps1` commands stay in place. Native and distribution observer scripts keep their own projects, source closures and admission checks; moving an observer does not constitute a fresh native acceptance run.

## Suggested work.md section4 boundaries

The source proposal lists26 suggested projects and allows adjusted boundaries. The following crosswalk records the current ownership instead of introducing empty assemblies to reproduce names.

| Suggested project | Current source/assembly ownership | Boundary and validation owner |
| --- | --- | --- |
| Erlang.Core | Runtime/Erlang.Runtime.Core (`Erlang.Runtime`) | Renamed; shared runtime tests |
| Erlang.Terms | Runtime/Erlang.Terms | Separate; term/comparison tests |
| Erlang.PatternMatching | Runtime.Core pattern matching and compiler | Combined; pattern/compiler tests |
| Erlang.Runtime | Runtime.Core plus Runtime/Erlang.Runtime bundle | Core and compatibility bundle separated |
| Erlang.Processes | Runtime.Core processes | Combined; process/lifecycle tests |
| Erlang.Scheduling | Runtime.Core scheduler | Combined; scheduling tests; three-strategy design remains open |
| Erlang.Messaging | Runtime.Core mailboxes/signals | Combined; receive/message tests |
| Erlang.Distribution | Runtime.Core distribution and native host | Combined; distribution tests and original-peer observers |
| Erlang.Serialization | Runtime/Erlang.Serialization | Separate; ETF tests |
| Erlang.Bifs | Runtime.Core native and core-module primitives | Combined; MFA/original comparison gates |
| Erlang.Modules | Runtime.Core code loading and registry | Combined; source/BEAM loading tests |
| Erlang.Otp.Kernel | Otp/Managed/Erlang.Otp.Kernel; Original/Foundation/Kernel library | Explicit managed and original alternatives |
| Erlang.Otp.Stdlib | Otp/Managed/Erlang.Otp.Stdlib; Original/Foundation/Stdlib library | Explicit managed and original alternatives |
| Erlang.Otp.Sasl | Original/Foundation/Erlang.Otp.Sasl.Beam | Original library; cold package tests |
| Erlang.Otp.Applications | Original libraries and runtime application bootstrap | Distributed by application; boot/package tests |
| Erlang.Otp.Behaviours | Otp/Managed/Erlang.Otp.Behaviours | Separate managed adapters; original modules separately owned |
| Erlang.Compiler | Compiler/Erlang.Compiler | Separate compiler assembly |
| Erlang.Compiler.Frontend | Erlang.Compiler frontend | Combined; parsing/language tests |
| Erlang.Compiler.Semantics | Erlang.Compiler semantics | Combined; semantic/error tests |
| Erlang.Compiler.CodeGeneration | Erlang.Compiler code generation/BEAM | Combined; compiler/image tests |
| Erlang.CSharp.Preprocessor | Compiler and tools/Erlang.Tool | Combined integration; generated consumer tests |
| Erlang.CSharp.MSBuild | Integration/Erlang.CSharp.MSBuild | Separate package; source/BEAM/cold consumer gates |
| Erlang.CSharp.Analyzers | Existing compiler diagnostics; no separate analyzer project | Dedicated IDE analyzer boundary remains a future decision |
| Erlang.CSharp.Tooling | tools/Erlang.Tool and Erlang.BeamBuild | Tools stay outside src; build/package gates |
| Erlang.Interop | Runtime.Core native adapters; Runtime/Erlang.DriverHost | Host isolated; native ABI/original tests |
| Erlang.Diagnostics | Runtime.Core diagnostics and compiler reports | Combined; diagnostics/readiness tests |

| Suggested test project | Current ownership | Boundary |
| --- | --- | --- |
| Erlang.UnitTests | tests/Erlang.Tests/Terms and other focused families | Combined executable harness |
| Erlang.Runtime.Tests | tests/Erlang.Tests/Runtime | Combined harness |
| Erlang.Compiler.Tests | tests/Erlang.Tests/Compiler | Combined harness |
| Erlang.Compatibility.Tests | Harness and original comparison families | Combined harness and independent observers |
| Erlang.Differential.Tests | tools/Erlang.Differential and harness | Original OTP comparisons; not an extra test assembly |
| Erlang.Concurrency.Tests | Runtime process/scheduler/mailbox families | Combined harness |
| Erlang.Distribution.Tests | Runtime distribution families and observer tools | Combined harness plus owned real-peer runs |
| Erlang.Otp.Tests | Otp families plus17 ordinary original-library consumer projects | Existing separate package consumers retained |
| Erlang.CSharp.Integration.Tests | Compiler/integration families and Hello examples | Ordinary projects and cold package scripts |
| Erlang.Performance.Tests | benchmarks/Erlang.Benchmarks and scoped allocation probes | Separate measurements; no whole-project speed claim |

Dependencies retain their existing direction: Terms → Serialization/Core → managed OTP/bundle → Compiler/host/integration and consumers, where arrows mean increasing dependency level, not newly introduced references. Original libraries reference the runtime bundle; consumers select original/managed modules explicitly. The migration must preserve the exact project-reference graph, all3054 tracked original OTP asset hashes, package paths and logical resources. No new project or reference edge is introduced. Solution discovery, dynamic original-library discovery, build/import paths, validation scripts and current source bindings must follow the physical locations. Historical validation snapshots and closed changelog volumes retain their recorded paths and bytes.
