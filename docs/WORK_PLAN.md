# Active delivery plan

Updated: 2026-10-17. Source authority: OTP-29.1.1 `ad05823719d77c8faee87348ea39513d4e2f99c5`.

## Measures

| Measure | Confirmed | Remaining |
| --- | --- | --- |
| Integrated MFA implementations | 587 in 14 modules | New proposals are excluded until integrated |
| Complete MFA contracts | 16 verified; 30 known differences; 541 awaiting verification | Source presence and passing examples are separate measures |
| erlang presence | 351/351 (100%) in the explicit inventory | Conditional/generated inventory limits remain documented |
| erlang complete contracts | 16/351 (4.6%) | 13 known differences, 322 awaiting complete verification |
| Size109 and MakeRef28 | Source audit and supported-route witnesses accepted | Final publication-head CI; no MFA count increase |
| Final part361 regression | 7438/7438, 11 integration outcomes, two packages, 12 clean children | Same-publication-head CI remains separate |

## Current work, in order

| Lane | Stage | Accepted evidence | Next measurable result |
| --- | --- | --- | --- |
| Root: runtime, native bridge and CI owner | Integrating tested part361 | Windows34 and Linux68 exact callback-group comparisons; 17 unique groups; 3 ordinary-loader cases; 3 destructor-loader cases; binding and create/join witnesses; actual C++ registration and explicit exit; normal cleanup | Publish final source/docs and obtain green CI for that exact head |
| New API: proplists get_value/2,3 | Private implementation and SDK checks passed; original running | 59 distinct SDK checks (53 contracts, four HOST, two direct MFA); corrected shared fixture; both APIs absent from published counts | Original run39041200099 on pinned source: 53/53, loaded proplists provenance, raw ETF equality and cleanup |
| New API: queue core | Frozen source proposal, not integrated | Eight MFA implementations and dedicated HOST/original fixture proposal | Review and execute focused SDK/original contracts after proplists |
| New API: binary access | Frozen source proposal, not integrated | Five MFA implementations and 51-case fixture proposal | Review shared slice contracts and execute focused SDK/original checks |
| Existing gaps / regression | Final source regression accepted | 3319 stable source rows of sealed3320; 221 guards; 104 C# token witnesses; 26 unchanged DLL snapshots | Reuse exact-source results; rerun only affected blocks or newly unresolved concerns |

Target engineering allocation remains 60% new functionality with complete-contract verification and 40% existing gaps/regression. This is a planning target, not a measured compatibility percentage. Three helper slots are available; helpers prepare isolated proposals or independently inspect results. Root alone changes runtime/native integration and dispatches CI.

## Linux acceptance and efficiency

Validation head91a281319529ee3aadb353e14e5e6b08d93cbb51, run39037669366, passes the selected Linux composition. Per-runtime NEWLM namespaces retain original image paths. Eight native thread providers are bound before constructors. The primary verifier proves inspected bindings and actual create/join; it does not prove every TLS operation. Separate destructor fixtures prove their actual C++ registration/key/explicit-exit paths, without claiming arbitrary C++ TLS lifetime compatibility.

Both independent callback routes and ordinary scenarios are collected before aggregate failure. Failed shared preparation or unsettled cleanup blocks dependent execution. Eleven synthetic collector controls pass. The three existing ordinary fixtures retain origin-relative dependencies, LD_LIBRARY_PATH and missing-dependency behavior. Workflow aliases remain present and only one is dispatched per snapshot. No new gdb installation: installed debugger or the existing bounded dump fallback is selected.

Measured comparable job: 143 seconds before, 141 after; debugger step8 seconds before,0 after. Four distinct snapshots each ran once; the earlier three jobs total390 seconds including21 seconds of gdb installation. This is one observation, not proof of sustained speedup or fewer historical retries. Final Windows full4 takes370.047 seconds; the preceding restricted full3 remains failed and is not silently discarded.

## Preserved limitations

The exact cause of the earlier CoreCLR SIGSEGV is not established. The controlled thread-provider composition passes the selected original/runtime/loader witnesses. Hidden libc operations, cancellation, unjoined native workers, arbitrary TLS outliving its image, broader NIF APIs and performance parity remain separate work.

The restricted full3 gets actual Win32 error5 while opening its own named-pipe client. The same binaries pass all16 affected cases and then the full suite outside that restriction; runtime and ACLs are unchanged. Legacy full export overwrites32 curated Limits: preserve the generated report and restore the curated annotations before publication.

See [part361 evidence](validation/part-361/evidence-index.json) and [English changelog](changelog/0056.md).
