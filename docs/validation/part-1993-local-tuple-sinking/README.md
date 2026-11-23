# Part 1993 local tuple projection sinking

Windows OTP 29.1.1 baseline ad05823719d77c8faee87348ea39513d4e2f99c5. The [observed ledger](../part-1990-local-parity/observed-status.json) remains 539 Equal, 827 Different, 0 Unsupported and 0 SourceRejected. No new raw equality or raw loss was measured in the selected Different files.

| Scope | Different | Changed images prepared | Normalized gains / losses | Corpus time |
| --- | ---: | ---: | ---: | ---: |
| [Initial deferred projections](p144-source-tuple-projection-sinking-regions/status.json) | 31 | 16 | 28 / 1 | 41.849 s |
| [Correct source entry roots](p145-source-projection-entry-roots/status.json) | 31 | 2 | 1 / 0 | 40.819 s |
| [Deferred return projections](p146-return-projection-regions/status.json) | 37 | 3 | 2 / 0 | 50.099 s |

Each scope has zero Equal, Unsupported and SourceRejected. The initial normalized loss compares different historical runs. An immediate-parent rebuild reproduced the Mnesia body exactly and showed that its missing frame relocation predates projection sinking. The [initial compiler errors](initial-compiler-build.log) and initial managed load check are retained. The managed load initially exposed an invalid live input count before allocation. The accepted rule excludes deferred values from entry availability until their actual definition.

The last scope adds normalized equality for disk_log_1:mf_int_close/2 and systools_make:pack_app/1. Four Dialyzer contract warning functions retain differences. Three focused managed checks and 100 native paired observations matched. See the [generated observations](native-generated.stdout.terms), [OTP observations](native-reference.stdout.terms) and [source transfer](source-transfer-2146.json). Public disk_log close and reopen, EDoc date formatting and Diameter error formatting also matched in the corrected preceding scope. The complete package regression, readiness, whole-corpus comparison and commit remain pending.
