# Shared terminal tagged payload

The proposal is based on the frozen Part 1843 integrated source. Reference is Windows OTP 29.1.1 at `ad05823719d77c8faee87348ea39513d4e2f99c5`. The staged compiler uses SDK 10.0.400 and the frozen managed dependencies. No dependency was added.

In pinned OTP `beam_core_to_ssa:select_val/4`, `select_extract_tuple/3` extracts used tuple fields at the checked arity block. The common-dominator placement rule in `beam_ssa_opt:ssa_opt_sink` keeps the payload before tag dispatch when a guard and a nested pattern both read it. The C# terminal case path reuses that extraction for equal-arity two-element tuples with disjoint atom tags. It reuses existing type guard resolution and leaves the selector in X0 for alias return and `case_clause`. A nested branch must have only fresh or discarded fields, return one projected field in a tagged pair, and have no additional tests or effects. A shared X2 payload and the lower contiguous X registers are initialized when the nested arm checks the heap. The existing general case path handles other forms.

| Windows OTP cohort | Equal | Different | Unsupported | SourceRejected | `jobs=1` comparison |
| --- | ---: | ---: | ---: | ---: | ---: |
| Part 1843 compiler | 4 | 4 | 0 | 0 | 4.19 s |
| Candidate | 4 | 4 | 0 | 0 | 4.25 s |

Only `snmp_app_sup.beam` changed. Normalized instructions for `start_agent/2` and `start_manager/2` gained exact matches, with no normalized losses. Its release comparison now lists `Dbgi` as the sole differing section. No release whole-file status changed in this patch. Timing is from one observation per compiler and does not establish a speed change.

The permanent fixture `beam_shared_tag_payload` is generated with `erlc +deterministic` and registered in the whole-module fixture suite. The candidate and OTP BEAM share SHA-256 `74B2CB14B99492C10418A95D688F702DFA6ADC8A85D38AEB34E5941C5B5B17CD`. The baseline generates `C49F5AE124363354C6A542C1649DEA9A52D0615EB3952A44AAE9FFCE454F2E98`. The staged mini harness runs the adapted existing adjacent-call test and checks the new fixture byte for byte. The prebuilt whole-module fixture suite passed under the candidate compiler DLL.

Pinned ERTS prepared and loaded `snmp_app_sup` from the candidate. Both exports run against a local registered supervisor protocol fixture. The reference and candidate agree on all fourteen outcome and child-spec pairs, covering success alias, rejected PID guard, nested error extraction, bad nested shape, unknown tag, wrong nested arity and non-tuple selector. The process identity was normalized before comparison. Raw transcripts are in `reference-execution.log` and `candidate-execution.log`.

The focused comparison did not run the full corpus or repository gate. The remaining SNMP source provenance change is being handled separately.
