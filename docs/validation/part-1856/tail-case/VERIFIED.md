# Terminal case tuple scoped validation

Compiler candidate SHA-256 `21A56DE7EC305B5A1DD7432DBA230852CE95E282F77A2F1E79D908B4DD4F23C3`. Patch SHA-256 `0176BDC506846ACA2C783301847A958F3B58C81B5AD0EE146C96E34EA2624663`. Frozen Part 1855 supplied the comparison compiler, runtime and test support. The source and generated outputs stayed in separate directories.

| Scoped output | Frozen Part 1855 | Candidate |
|---|---:|---:|
| Equal | 5 | 5 |
| Different | 3 | 3 |
| Unsupported | 0 | 0 |
| SourceRejected | 0 | 0 |
| Uncached cohort time, one job | 17.09 s | 19.34 s |

The same permanent test failed on the intended entry-opcode assertion with the frozen compiler and passed against the candidate. Compiler and test builds took 75.70 s and 5.87 s end to end with one BelowNormal worker. The compiler build had zero warnings and errors.

All five prior-equal images retained full byte equality. The three transformer images account for 77 changed function disassemblies, including changed global label IDs in downstream functions. Against Windows OTP, direct comparison of the function disassembly gained 4 and lost 0. A comparison normalizing just local label IDs gained 25 and lost 0. The raw disassembly of `tr_MId/2` is now identical in all three modules. `tr_ammDescriptor/2` reached normalized equality in v1 and still differs in v2 and v3. Each transformer changed Attr, AtU8, Code, FunT and Line, with zero whole-section gains or losses. No transformer reached whole-file equality.

Native OTP 29.1.1 loaded and executed both generated fixture images. Outputs, exact matches, already-bound mismatch, case errors and lookup after a side effect behaved as expected. For an unconditional local raising call, the baseline retained the case frame and the candidate omitted it like OTP. For a call with a possible normal return, all three versions retained the caller frame when it raised and produced the expected success value on a returning input. Native ERTS also loaded all three compiled transformers. Public message transformation returned the same values for device and IPv4 input, the same IPv6 and case error values and the same `tr_MId/2` frame presence as the release compiler.

The first native launcher accidentally used PowerShell's automatic argument variable, so it supplied no image paths. Its original logs and dumps remain in `runs/worker-first`. The corrected native-only run reused the already generated immutable images; all checks passed in `runs/worker-first/native-retake`. The complete regression and uncached corpus remain the integration gate.
