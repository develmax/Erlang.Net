# Part 1994 local call results and operand scheduling

Baseline Windows OTP 29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The aggregate [observed ledger](../part-1990-local-parity/observed-status.json) is 539 Equal, 827 Different, 0 Unsupported and 0 SourceRejected.

| Scope | Different | Changed images prepared | Normalized gains / losses | Corpus time |
| --- | ---: | ---: | ---: | ---: |
| [Y relocation and checked BIF](p147-frame-relocation-and-checked-bif/status.json) | 40 | 6 | 7 / 1 | 48.887 s |
| [Call-result constructor regions](p149-projected-call-result-regions/status.json) | 86 | 30 | 23 / 1 | 194.517 s |
| [Checked list payload retake](p150-checked-list-payload-scheduling/status.json) | 12 | 0 | 0 / 0 | 44.646 s |

Each scope has zero Equal, Unsupported and SourceRejected. The first scope matched mnesia_schema:make_delete_table2/1, two index schema methods and ct_property_test:dup_module/2. Its order change in ct_framework:init_per_group/2 was corrected in the second scope. The second scope's loss in prim_tty:move/3 compares against a historical image. An immediate-parent compile reproduced the same normalized function (SHA-256 AA8CAC698263A2EB0BA828F0771C65F9CB5001EB6981F628A645CF532E294C33).

The final five focused managed cases passed. On the native oracle, 104 fixture observations matched, including success and badmatch. The released Mnesia table, index and deletion path produced matching observations. Public Xref scanning, record expansion and DTLS encoding also matched. See the [paired native outputs](native-generated.terms), [released behavior digests](release-paired-hashes.json), [initial C# compiler failure](initial-ordered-payload-compiler.log), [managed build diagnostics](initial-fixture-build.log), [initial schedule assertion](initial-ordered-payload-fixture.stderr) and [source transfer](source-transfer-2150.json). Full regression, acceptance, readiness and commit remain pending.
