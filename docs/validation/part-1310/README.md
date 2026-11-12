# Part 1310 — Construct list arguments before frame-free tail calls

Reuse the existing list operand planner and emitter in the per-clause dispatcher. For an unguarded cons branch, check heap space before extracting list fields. Reuse the list argument register for its head when the tail is discarded and the branch is terminal. Construct the proper list argument using registers whose values have reached their last use.

The focused case compares complete OTP and C# instruction streams for both list and tuple branches and executes proper lists, improper lists, nil and non-list inputs. The initial clause in beam_core_to_ssa:op_vars/1 also matches after label normalization. Previous list dispatch and tuple field call probes also pass. The compiler tools were rebuilt with --no-incremental after the source change. No dependencies or licenses changed.

- [Focused command](check-list-argument.ps1)
- [Matching instructions and execution](probe.txt)
- [Previous list dispatch regression](list-tail-regression.txt)
- [Tuple field call regression](tuple-regression.txt)

- [Release clause comparison](release-clause.txt)
- [Remaining list argument tails](list-argument-tails.txt)

The reference audit finds variable list tails in 177 modules in the investigated group, proper list tails in 15 and a computed tail in one. These categories overlap and cover tail calls after variable-head cons clauses, including calls with several arguments.

The final corpus generated all 1,366 release files with 361 Equal, 1,005 Different and zero rejected sources. Three images changed against part 1309. The Code size gap narrowed by 14 bytes in reltool_target, 26 in ssh_transport and 24 in erl_tar. The reltool_target:opt_join/2 opcode sequence now matches OTP after excluding lines and labels. The loader accepted all differing images.

- [Final corpus](otp-corpus.json)
- [Delta](delta.json)
- [Changed function opcode sequences](changed-functions.txt)
- [OTP loader](loader.txt)
