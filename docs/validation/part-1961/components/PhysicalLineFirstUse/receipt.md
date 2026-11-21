# Part 1961 line metadata after cold exit relocation

Pinned Windows OTP-29.1.1 comes from `ad05823719d77c8faee87348ea39513d4e2f99c5`. Pinned `beam_asm:make_op/2` assigns locations while traversing final physical instructions. In `beam_dict:line/3`, zero is reserved for unavailable locations and new coordinates receive the next index when first seen. The filename map reserves implicit filename zero and assigns explicit files in first-use order. Every ordinary, executable and debug line instruction increments the instruction count. `beam_asm:build_line_table/2` emits the associated flags and both ordered dictionaries.

Part 1959 physically moved terminal failure instructions with `MoveTerminalErrorBlocks`. The pass preserved resolved locations but only adjusted the instruction count, leaving stale dictionary indexes. The fix passes the current label count to the relocation pass and reuses `RebuildLineTable` after it moves a block. Late source byte streams with no such relocation and the separate assembly input path keep their existing behavior. [Strict source replay](replay-files.json) · [Production patch](production.patch).

A 153.367-second census decoded the already built corpus without recompilation. All 1,366 pinned ordinary Line tables were ordered and counted by actual first use. Candidate Part 1959 had 49 stale tables, including three modules using explicit filenames. The uncached 64-module retake included all 49 witnesses and 15 already-equal controls. It took 41.370 seconds with two bounded workers and returned exit 1 for reported differences. [Census receipt](census-summary.json) · [Scope](scope-inputs.json) · [Timing](scope-timing.json).

| Metric | Before | Candidate |
| --- | ---: | ---: |
| Whole-image Equal | 15 | 15 |
| Different | 49 | 49 |
| Unsupported | 0 | 0 |
| SourceRejected | 0 | 0 |
| CompilerError | 0 | 0 |
| Canonical order and counts across affected images | 0 | 49 |

All 49 changed modules retain the same decoded instruction stream, including resolved locations, after normalizing only the closure checksum derived from Code. The remaining 15 images retain their original bytes. `megaco_monitor` gains whole-section Line equality. `raw_file_io_list` now has pinned coordinate order and 12 extra physical Line instructions compared with OTP, so the chunk still differs. No section loses equality. [Whole-file comparison](wide-compare-scope.json) · [Section comparison](final-line-scope-summary.json) · [Instruction comparison](scope-instructions.log).

The permanent regression preserves zero-location operands, canonical first-use indexes and external filename provenance. It fails against the frozen Part 1959 compiler and passes with the candidate. All 49 rebuilt images and their parent images prepare successfully in pinned ERTS. Two actual `megaco_monitor:handle_info/2` observations match pinned, including its `badrecord` frame at line 316. Six synthetic observations confirm ordinary behavior and both frames of a propagated exception, including the named external file at line 122. [Focused check](focused-final-results.json) · [Loader evidence](load-scope-and-assembly.log) · [Real module execution](native-megaco.log) · [Synthetic execution](native-fixture.log).

Pinned source compiler profiles emitted 8 ordinary Line instructions, zero for `no_line_info`, 7 executable and 8 ordinary for forced coverage, and 8 debug and 8 ordinary for debug instrumentation. The assembly input controls without source locations and with an explicit zero location remain byte-identical before and after the C# patch and return `ok` on pinned ERTS. Source compilation with OTP-only instrumentation flags was not added here. Numeric instrumented C# assembly is rejected by the existing bounded linker's opcode-role check on both sides. Raw failures are retained. [Pinned profiles](pinned-profiles.log) · [Assembly controls](assembly-linked-controls.json) · [Instrumented assembly diagnostic](assembly-linked-beam_line_instrumented-after.log).

The initial permanent fixture looked for the line tuple in the file tuple's position and was corrected. The first assembly fixture lacked its existing native zlib dependency. The second run copied the exact frozen dependency and left the instrumentation limit visible. The full managed gate and a fresh 1,366-module build remain for linked package verification.
