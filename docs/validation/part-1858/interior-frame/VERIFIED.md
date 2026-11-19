# Verified interior-Y terminal projection

The exact linked Part 1856 compiler is `4334EA6E7F30F2DF399A11ABCB721CFD2FF30C510382B74356152DB1952A51B9`. The candidate assembly is `AF339023E9305FF4CC9031908DFA09BA20DFFC6610063DAA38E369F0D7ED72AA`. The linked source overlay and patch hashes are in the managed receipt. The compiler and test builds took 72.02 seconds and 5.45 seconds with no warnings or errors.

| Scoped measure | Exact linked baseline | Candidate |
|---|---:|---:|
| Equal | 5 | 5 |
| Different | 6 | 6 |
| Unsupported | 0 | 0 |
| SourceRejected | 0 | 0 |
| Uncached compilation and comparison with one job | 29.67 s | 30.62 s |

All five already exact images retained equality. The only changed complete image is inet6_sctp. Its single changed function is send/4. It still differs both in raw disassembly and after local label normalization. The scope gained and lost zero full-file, section or exact-function matches.

| send/4 structure | OTP release | Linked 1856 | Candidate |
|---|---:|---:|---:|
| AllocateHeap slots | 4 | 10 | 4 |
| Caller frame width at both tail transfers | 4 | 10 | 4 |
| Tuple-element destinations in X | 6 | 0 | 6 |
| Success heap-check X root prefix | 2 | 0 | 5 |
| Whole module Code bytes | 831 | 830 | 821 |

The module's candidate SHA-256 is `3B22475FC07B043AC1B4E179E9DF7A4CB7EF28428DDCF721C626FA94DA732331`, its linked baseline is `186CCA3288D07CCE58051393CDB9D7487C1DACEA047E0A0D3A08CB47EB03D44C`, and its pinned release is `8CA803F2B45A0F72A4D01EB8BC0C5822E45FCC54180FB148A948C1DC0D83D34B`.

The exact permanent test passed. It checks preserved and reindexed high Y, the collector root prefix, allocation, deallocation and terminal call widths, and refusal for a fallback that reads a projected Y or roots the old X0. A second ordinary call also keeps the original lowering. Native x64 Erlang OTP 29.1.1 loaded and executed module_info/1 for the reference and both inet6_sctp images. The independently compiled source fixture exercised both success and two failure shapes, including preservation of the entire original call result on failure. OTP and both generated fixture images executed all values identically.

Several screening witnesses have earlier ordinary calls outside the qualifying interval. file_sorter uses four or five in the selected functions, while filename and xref_base use two. Their exclusion is conservative under the first prototype. The single-call io_lib_format functions branch to `badmatch x0`, so the allocator cannot reuse X0 before the guard until it models that physical fallback constraint as a reserved color. The raw negative is preserved in the native outputs.

The immediate exactness dependency for send/4 is guard and heap scheduling. OTP tests the last list tail before the four safe record reads, then performs the heap test with two roots before those reads. The candidate uses initialized X roots with the original read order. The wider-corpus dependency is support for earlier calls outside the proven slice and failure-protected X allocation. The patch's code and permanent controls belong to this scoped verification. Integration and full-corpus acceptance remain separate.
