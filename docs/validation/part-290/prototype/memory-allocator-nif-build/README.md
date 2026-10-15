# Prepared pinned Windows fixture build

**Source only: no compiler, linker, layout EXE or native fixture has been executed.** This recipe needs a separate root-approved heavy slot. It installs nothing, uses no network and never builds/loads production BEAM.

`source-plan.json` binds the exact four OTP header/template blobs to commit ad05823719d77c8faee87348ea39513d4e2f99c5, reviewed fixture C source, layout C verifier and builder script. `freeze_plan.py` only reads Git/source bytes and creates this plan; it performs no native build. Tag HEAD is never used for headers. The three public headers remain byte-exact. The generated integer-size header derives from the pinned .in template with six explicitly recorded Windows x64 LLP64 substitutions. It is **not** claimed to be a byte-exact configured OTP build output. Native layout compilation verifies those assumptions.

After independent review, run the prepared command with the reviewed plan hash and a new output directory under repository artifacts:

```powershell
py -3 artifacts/memory-allocator-nif-build/build_fixture.py --plan artifacts/memory-allocator-nif-build/source-plan.json --approved-plan-sha256 <independently-reviewed-plan-sha256> --output artifacts/<new-unique-fixture-build-directory>
```

The script refuses existing output directories and mismatched pin/script/source/header hashes before creating output. It locates existing VC x64 tools via vswhere/vcvars64, hydrates only the pinned blobs, uses /W4 /WX C11 and reproducible-link /Brepro, and retains compile/link/layout/export/dependency/PE logs. Tool executables and vcvars are hashed in the receipt. This binds a recipe and toolchain; cross-toolchain byte-for-byte binary identity is not asserted.

The layout EXE reports native sizeof and offsetof for the complete entry/function structs plus **all 180 callback slots and both trailing fields**. The script compares every slot to its actual pinned declaration ordinal and verifies 1456-byte table/96-byte entry/32-byte function/40-byte binary predictions. Fixture DLL exports must be exactly nif_init and memory_allocator_fixture_unload_count. Dependency and PE headers are retained for independent examination; recording dependency names does not prove their deployment/search-path integrity.

The builder executes only the layout EXE, never loads the fixture DLL. A completed build receipt therefore says NativeFixtureBehaviorExecuted=false. No SDK/runtime compatibility result follows from layout alone. Root must independently inspect approved source/header/probe bindings, build logs, native offsets, exact export set, toolchain/dependencies and receipt hashes **before** using its DLL SHA as the fixture bootstrap allowlist. Computing a DLL hash does not approve that DLL.

The C fixture and Erlang declaring module are from ../memory-allocator-nif-audit. Actual native behavior, original OTP comparison, native failure/rollback interleavings and Main integration remain unexecuted. Original legal notices in copied upstream headers/templates stay byte-exact; no new source license banner is added to project-owned C/C# code.
