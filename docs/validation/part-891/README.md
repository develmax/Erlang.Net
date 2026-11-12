# Part891 Map patterns and list qualifiers

Map patterns test the source and extract fields with BEAM `get_map_elements`. Field keys are evaluated before field bindings, and generator map keys retain their enclosing scope when a value pattern shadows a name. The direct one-field extractor matches the deterministic OTP fixture. [OTP assembly](otp-direct-disassembly.S).

General list comprehensions handle multiple list generators, guard and expression filters, and strict mismatches. The compiled loop preserves iteration order, rejects improper generator tails and distinguishes guard rejection from invalid non-guard filter results. Binary, map and zip generators remain outside this path. The general helper and register layout still differs from OTP.

The compound test compares nested order, strict mismatch and a shadowed map key. The OTP and C# images produced `{[{1,2},{3,4},{3,5}],{badmatch,other},[1,3]}` on pinned OTP.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q` and run its DLL with the pinned installation and report paths. Build the focused runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` and select the whole-module, Docs and installed release image cases.

All three focused tests passed with the final compiler assembly. Full runtime and canonical readiness checks remain pending. [Focused report](focused.json).

The full 1,366-file corpus recorded 100 Equal, 673 Different, 164 SourceRejected and 429 Unsupported. A total of 133 files moved from Unsupported to Different, including 64 previously blocked in comprehension lowering and 58 in pattern lowering. [Corpus report](otp-corpus.json).
