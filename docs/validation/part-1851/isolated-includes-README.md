# Kernel include search provenance

The candidate uses the independently frozen Part 1847 source and compiler dependencies, SDK 10.0.400, and pinned Windows OTP 29.1.1 `ad05823719d77c8faee87348ea39513d4e2f99c5`. No dependency was added. The public positional profile constructor is unchanged.

Pinned `lib/kernel/src/Makefile` passes `-I../include` from the kernel source directory. The pinned `epp:enter_file/4` records the name returned by `file:path_open/3`. `epp:enter_file2/5` then uses that path when entering the header and establishes its directory as current for nested headers. The corpus's first physical include directory is the installed kernel include directory, and the Windows profile binds that index to its exact logical build path. The C# preprocessor records the directory that actually opened each direct include, preserves the lexical path and parent restoration, and uses it only for abstract-code file forms. Local shadowing, explicit profile file overrides and unmapped include directories retain their prior precedence. Existing deterministic instruction and documentation inputs are unaffected. The Megaco own-app `include_lib` proposal is independent.

| 34 modules, one worker | Equal | Different | Unsupported | SourceRejected | Comparison |
| --- | ---: | ---: | ---: | ---: |
| Frozen Part 1847 | 5 | 29 | 0 | 0 | 61.90 s |
| Candidate | 5 | 29 | 0 | 0 | 56.39 s |

Twenty-three images changed exclusively in `Dbgi`. All 21 anticipated byte-exact `Dbgi` gains were observed, including `inet6_sctp`, `inet_hosts`, and `inet_sctp`. `inet_db` and `net_kernel` closed only part of their debug differences. No regression or new complete image match was observed in the cohort. No normalized function status changed because code bytes and their related chunks are unchanged. The timings are individual observations and do not establish a performance change.

The permanent test passed in a staged SDK harness and covers local shadowing, fallback to an unbound search entry, selection of a later configured directory, lexical `..`, nested relative includes and restoration of the parent header path. Scoped format checks required zero edits. Pinned ERTS prepared all 23 changed images. It also loaded `inet6_sctp`, `inet_hosts` and `inet_sctp` and ran `module_info(module)` on each. The raw observations are in `normalized.log` and `execution.log`. The full corpus and package gate were not run in this isolated proposal.
