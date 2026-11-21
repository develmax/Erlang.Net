# Verification of the grouped call projection schedule

The candidate compiler is `c4715382631bfd615519917282e06621e288ee6b1f1d08580c09b95744fa4aff`, built on sealed `320040bc2b8a10be48cde3a0f68b9b8f1d94862f44202c98f8a95d31c92b8d4a` from frozen Part 1963 and the three verified follow-on components. SDK 10.0.400 and .NET runtime 10.0.11 were used in the private build. The compiler and focused tests built with zero warnings and errors. The layout check and strict replay passed. [Verified patch identities](patch-manifest-verified.json), [build receipt](first/build-result.json), [resource snapshot](first/resource.json), [format check](first/style-check.log).

| Measure | 320040bc | Candidate | Pinned OTP |
| --- | ---: | ---: | ---: |
| Representative whole-file Equal | 7 | 7 | 17 |
| Representative whole-file Different | 10 | 10 | 0 |
| Unsupported | 0 | 0 | 0 |
| SourceRejected | 0 | 0 | 0 |
| `handle_info/2` normalized instruction count with line operations removed | 109 | 107 | 107 |
| `megaco_monitor` Code section bytes | 2,390 | 2,384 | 2,357 |

The 17-file uncached scope took 19.156 seconds with one job. No whole-file matches were gained or lost. The only changed image was `megaco_monitor`. Its `handle_info/2` became equal to the pinned function after normalization both with and without line operations. The raw disassembly also matches, including absolute labels and source locations at their physical positions. The module's `Line` remains byte-equal. The two differing sections are `Code` and `Attr`. The other four remaining normalized functions are `warning_msg/2`, `handle_call/3`, `cancel_apply_after/1` and `apply_after/5`. [Scope](first/scope.json), [section delta](first/scope-delta.json), [normalized delta](first/functions.terms), [function listing and separate line checks](first/handle-info.terms), [remaining functions](first/module-residual.terms).

The focused test failed on 320040bc at the register-schedule assertion and passed on the candidate. The smaller fixture's baseline already allocates one state slot. The original runner expected an earlier frame assertion. The raw red failure identified the correct assertion and the wrapper now accepts either specified defect assertion. The production and permanent test code used for the run remained the same. [Red](first/red.log), [green](first/green.log), [build receipt](first/build-result.json).

Pinned ERTS loaded and executed the release, 320040bc and candidate monitor images. All ten observations agreed, including legacy callback delivery, the DOWN controls, the handled and unhandled cancellations, and the original `badrecord` and `badarg` locations. It also loaded and executed the pinned, baseline and candidate fixture images. The four observations agreed for call arguments, a field retained across the call, the order of effects and the unmatched fallback. [Monitor execution](first/native-monitor.log), [fixture execution](first/native-fixture.log), [native and scoped exit codes](first/scope-result.json).

No full-corpus or repository-wide regression was executed in this lane. [Verified production patch](production-verified.patch), [verified test patch](tests-verified.patch), [evidence hashes](verified-receipt-hashes.json).
