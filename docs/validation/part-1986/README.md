# Part 1986 — Guarded lifetimes and register ownership

Accepted compiler-only checkpoint.

| Gate | Actual result |
| --- | --- |
| Full fresh Windows release | 512 Equal, 854 Different, 0 Unsupported, 0 SourceRejected in 683.30 seconds. [Raw report](full-uncached/otp-corpus.json) |
| New and lost full images | New None. Lost None. [Delta](full-uncached/status-changes.json) |
| Selected C# compiler tests | 600 passed. [Exact coverage](focused-verified.json) |
| Affected image scope | 486 images. [Scope](derived-scope/scope.json) |
| Loaded and executed | 402 of 405 changed images prepared, 3 shared on-load restrictions. 79 pinned native observations. [Native report](native-scoped/receipt.json) |
| Normalized instruction bodies | 4 gains and 0 losses among 1478 changed functions. [Ranked causes](ranked-causes.md) |

The source package retains the retired unary argument frame slot, eliminates tuple carrier copies under register/lifetime proof, and splits preprojected guarded branches while preserving cold selected values. The same retake aligns exact atom guards, preserves an outgoing process-dictionary root, keeps a positioned raw cursor and removes guarded initialization only after all-path checks.

The first candidate's compiler build succeeded, then its exchange fixture failed because the synthetic function atom was not registered. The final package explicitly registers the fixture atom. The original raw output and compiled identity are retained. The separate layout query of the isolated source root encountered the absent benchmark directory. The full linked workspace gate remains separate.

Physical comparison restores the pinned allocation sequence 1,2 for ct_config_xml:transform_entity/1 and its full normalized body is exact. Two SNMP frames widen by one slot. snmpa_trap:find_dest/1 has 14 slots versus 13 before and 8 in OTP. snmpm_usm:generate_outgoing_msg/5 has 12 versus 11 before and 7 in OTP. These regressions remain open. The earlier 217 mandatory frame controls passed. [Physical widths](p86-selected-widths-lf.tsv)

[Initial focused failure](rejected-initial-fixture/fast-splits/lane-0.stdout.log)
