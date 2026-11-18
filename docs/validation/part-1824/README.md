# Part 1824 — Leaf frame allocation after entry tests

The leaf register allocator now uses the existing entry frame finder. The
finder admits straight-line entry predicates and verifies that branches
before the allocation can only exit through function_clause. One plain
allocation can be removed when the existing leaf liveness and register
checks admit its body. The existing exclusions for calls, handlers, heap
allocation and instructions with unsupported register effects stay in force.

Pinned OTP 29.1.1 `beam_ssa_pre_codegen:place_frames` and `need_frame`
separate blocks that need a frame from entry checks. The reduced
`erl_boot_server:valid_option/1` shape uses no frame under OTP. The C#
revision emits a frameless leaf after tuple tests and keeps the frame in
a companion function whose argument survives a call. Five behavior
checks passed under ERTS for the pinned compiler and both C# images.

The same hash-bound Windows release inputs and build profiles as Part 1823
were used. The four-process run recompiled all 1,366 modules in 317 seconds.
It retained 400 Equal, 966 Different, zero Unsupported and zero
SourceRejected, with zero new or lost full-file matches.

ERTS accepted the reference, prior and new images for all 80 changed
modules. The new code removed the frame from 106 functions that have no
frame in OTP. No function lost a frame that exists in OTP. Normalized
instruction matches gained 15 and lost zero.

[Full corpus](otp-corpus.json) · [Summary](results.json) ·
[Frame and loader evidence](loader-and-frames.terms) ·
[Normalized instruction differences](normalized-delta.terms).
The compiler selection passed 3,775 of 3,836 tests on its first run.
The isolated worktree lacked the Debug port probe and official runtime
admission receipt. Building the probe and copying the verified receipt
made all 61 failing cases pass on the same test binaries. Both reports
are retained. Hosted validation is pending.

The corpus CLI accepts `--modules-file <path>` and `--jobs <count>`.
The selection file lists one release-relative BEAM path per line. Paths
must identify existing files and duplicates are rejected. Without a selection option, the tool compares the full release. The default is one worker.
The tool compares whole bytes and recompiles every selected module.
Set `ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES=1` to save differing outputs
outside the reference installation.

On the same 20 modules, one worker took 18.00 seconds and four took
12.95 seconds. The JSON report and all generated image hashes matched.
The full direct CLI run produced the same 1,366 report rows and 966 differing image hashes as the four-process run. The direct run took 877 seconds. The four-process run took 317 seconds. [Full CLI verification](cli-full.json).

[Compiler first run](compiler-first.json) · [Retake](compiler-retake.json) ·
[CLI benchmark](cli-benchmark.json).

## Reproduce the release comparison

Use the Windows release ZIP and SDK recorded in the
[Part 1823 provenance](../part-1823/provenance.json). Extract the pinned
`.hrl` files and documentation needed by `erts` and `stdlib` into a
separate directory from Git tag `OTP-29.1.1`. The
[release input manifest](../part-1823/windows-release-inputs.json) and
[tag input manifest](../part-1823/tag-inputs.json) identify the input
bytes. Generate the public_key headers with the release `erlc`,
the release `lib/public_key-1.21.7/asn1` schemas, and
`-bber +der +noobj +asn1config`. Keep that output in its own directory.

```powershell
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet run --project tools/Validation/Erlang.BeamCorpus -c Release -- `
  $otpRoot $report $publicKeyHeaders $pinnedSourceInputs --jobs 4
```

Use a new report path outside the installation on every run. Exit 1
indicates at least one module with a non-Equal result. Inspect the JSON
for individual diagnostics. Save the generated differences and the
source-generation directory beside the report. To rank first opcode
differences, pass the reference installation and the `.generated`
directory to `rank-opcode-differences.eval` with the pinned ERTS.
