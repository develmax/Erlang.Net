# Release include provenance audit

Full raw inspection uses the sealed uncached Part 1845 Windows OTP comparison. All 960 different images differ in `Code`. The minimum difference is `Code` plus `Attr` in `megaco_tcp_sup`. Next are `ssh_channel_sup` (`Code`, `Attr`, `Line`) and `mnesia_frag_hash` (`Code`, `Line`, `Type`). `groups.json` records all 103 section signatures with module lists. `dbgi-classify.terms` and `dbgi-count.log` record 324 differing debug sections. Of those, 302 have identical non-file abstract forms and options, and 295 differ only in filename strings. The largest groups are SNMP (58), kernel (50), Megaco (38), and inets (24).

The Megaco proposal is based on frozen Part 1847 compiler and tool binaries plus its source overlay, with SDK 10.0.400 and OTP 29.1.1 `ad05823719d77c8faee87348ea39513d4e2f99c5`. Pinned `lib/megaco/src/app/megaco.mk` supplies `-pa $(ERL_TOP)/lib/megaco/ebin`. OTP `epp:expand_lib_dir/1` uses that root for own-app `-include_lib` outside the ordinary search path. The preset declares the logical app root using the existing profile selection, including the separate flex WSL build root. Explicit recorded filename overrides retain priority. The physical include resolver and instruction emission are unchanged. The public positional constructor is preserved because the capability is an init property. No dependency was added.

| 59 modules, one worker | Equal | Different | Unsupported | SourceRejected | Comparison |
| --- | ---: | ---: | ---: | ---: | ---: |
| Frozen Part 1847 | 13 | 46 | 0 | 0 | 110.69 s |
| Candidate | 13 | 46 | 0 | 0 | 93.85 s |

Only `Dbgi` changed across 36 generated files. Thirty-four `Dbgi` chunks now match the release byte for byte, with zero section losses. `megaco_filter` and `megaco_text_mini_parser` gained partial metadata corrections. No normalized function or complete image changed its equality status. All 36 candidate images prepared and loaded in pinned ERTS and returned the correct `module_info(module)` result. The new permanent metadata test passed, exercising direct includes, an own-app library include, an explicit path override and an unrelated library. Scoped layout check required zero edits.

The first exploratory baseline was deliberately stopped to avoid recompiling large generated ASN.1 and text parsers in this iteration. Its partial outputs and stop receipt remain separate. The 59-module comparison excludes those heavy generators and five examples with unaffected profile handling. The full corpus and package gate were not run. Timing is from one observation per build and does not establish a speed change. Ordinary includes through a logical `-I` are a separate next dependency, particularly for kernel.
