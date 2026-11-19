# Verified direct post-call tuple store

The compiler builds cleanly with the outgoing call-order work already applied. The exact parent assembly SHA-256 is `AE8BCD75FF78B498CE330B92C1EF60D274809C591EAF2D92D9C9782496AF3ECF`. The candidate assembly is `C4A17C768350594EF6A32BA5AFEF395250FDF9C8EE46CB42024770A2AE89186F`. The tested patch is `direct-post-call-tuple-store-checked-header.patch`, SHA-256 `0688ABE56D4008C9DD60D2BCCAF2ADFA4667970ADAAA3EF637B8E2D6B0CBB926`.

| Scoped measure | Outgoing baseline | Tuple-store candidate |
|---|---:|---:|
| Byte-equal files | 5 | 5 |
| Different files | 6 | 6 |
| Unsupported | 0 | 0 |
| SourceRejected | 0 | 0 |
| Uncached cohort, one BelowNormal job | 49.06 s | 45.00 s |

Four files changed. Forty function disassemblies changed. Exactly one function reached raw and local-label-normalized disassembly equality, with zero function losses in either comparison. Five prior-equal files, control module `ct_groups` and control module `yecc` retained their complete bytes. There were no gained or lost whole sections.

`mnesia_kernel_sup:init/1` matches the pinned function in raw disassembly. Its direct Y8 store removed the only previous mismatch. The module still differs, including the independently unresolved `worker_spec/3`; that function's disassembly is unchanged from the outgoing baseline. Only Code changed in the Mnesia module, from 495 to 492 bytes, against 478 bytes in the release.

| Mnesia whole-image SHA-256 | Digest |
|---|---|
| Windows release reference | `5B28241196989A4F5AC5993E82CB2803D3970E63F053384AB6497087EE462CC6` |
| Outgoing baseline | `61E0B704EA80BF1038043C2890C420C6207D08034D65251145BFE2F678D765E0` |
| Direct-store candidate | `2B89B935414E169835EB2367EC4D8966B0DD3AEC5B05B82BBACE885598B5566C` |

Pinned Erlang loaded the reference and both generated versions of all four changed modules and executed their module identity functions. It also executed `mnesia_kernel_sup:init([])` in each version and obtained identical flags and nine child specs. The standalone managed fixture passes on the candidate and fails the intended tuple destination assertion on the outgoing baseline. It explicitly checks an X0 self-copy, an intervening X0 read, an X0 heap root, a Y source/destination alias, an ordinary construction lacking the preceding call and a jump-and-label boundary.

The first test run declared a synthetic maximum opcode below the runtime's compatible header bound. Its raw failure is in `runs/outgoing-ae8b-worker`. The corrected run changed only the test header and reused the already-built compiler after matching all 569 patched compiler source hashes. Both cohorts compiled their images from source in the corrected run. Compiler build time was 77.08 seconds; the corrected scoped worker exited 0. Reports are under `runs/outgoing-ae8b-corrected-fixture`. The full package regression and corpus are separate integration gates.
