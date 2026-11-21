# Checked record continuation with a cold failure edge

## Result

The compiler uses the immediately preceding call result in X0 for checked record fields. The outgoing cons roots the record during heap allocation. The live stack homes in the target arm are Reason and State. The frame trims Reason after the cons consumes it. A malformed record still reports the original value and its first field-access location. The short exception block follows the ordinary tuple-entry arms, as in pinned `beam_jump`.

| Measure | Linked Part 1963 | Candidate | Pinned |
| --- | ---: | ---: | ---: |
| Representative files classified Equal | 7 | 7 | 17 |
| Representative files classified Different | 10 | 10 | 0 |
| Unsupported | 0 | 0 | 0 |
| SourceRejected | 0 | 0 | 0 |
| `megaco_monitor:handle_info/2` normalized instructions | 122 | 114 | 107 |
| `megaco_monitor` Code bytes | 2,433 | 2,405 | 2,357 |

The uncached representative corpus took 20.089 seconds with one job. There were no gained or lost whole-file matches and no gained or lost normalized function matches. The only changed module was `megaco_monitor`, and its only changed normalized function was `handle_info/2`. Its exact `Line` section remains matched. `Code` and `Attr` still differ from pinned. [Module and section delta](cold-17-delta.json), [normalized function delta](cold-17-functions.terms), [raw disassembly](cold-monitor-handle-info.terms), [timing](cold-17-timing.json).

## Verification

The focused test checks outgoing projection operands, the live X count, frame trimming, the cold exception placement and the runtime trace. The original compiler failed the adjacency test at the checked result liveness assertion. The first candidate passed adjacency and execution but failed the added grouped fixture checked against the cold placement assertion. The final compiler passed the complete focused test. Compiler build and layout check passed. The standalone focused project emitted one MSB3246 warning for an unnecessary native Zlib PE reference. Its input runtime libraries loaded and the runtime test passed. [Baseline failure](fixture-red.log), [cold placement failure](focused-cold-red.log), [final test](focused-cold-red-green-final.log), [compiler build](compiler-cold.log), [focused build](focused-cold-build.log), [layout](style-cold-check.log).

Pinned ERTS loaded and executed the release, linked baseline and candidate for five real DOWN scenarios. These cover the undefined result, wrong protocol kind, valid callback dispatch and malformed-record stack with a line-316 frame. Pinned ERTS also compared five synthetic executions on the original, intermediate and final fixture images. They matched the positive result, absent result, malformed frame, call-order trace and malformed frame after a notification. The synthetic source was compiled in an isolated working directory in the final run. Raw logs from earlier temporary path and payload setup failures remain in the stage. [Real native log](cold-17-native.log), [synthetic native log](native-cold-final.log), [native source build](native-cold-compile.log).

The production and permanent-test patches replayed with strict whitespace checks and exact postimage hashes in a private replay directory. The saved Part 1962 source closure and both Part 1963 production patch identities bind the base. The black-box C# and native checks were limited to the stated fixture and scope. A broader corpus and the complete repository regression are integration gates.

The next Code differences to audit are the shared no-match tail with the cancel arm and the legacy arity-four branch's allocated argument fields. The separate `handle_call/3` clause partition and stack interval remain out of this focused patch.

[Production patch](production-cold.patch), [permanent regression patch](test-cold.patch), [patch manifest](patch-identity-cold.json), [evidence hashes](receipt-hashes.json).
