# ETS PR116 review repair - Part591

All three findings are triaged against the current source. The P1 architecture
finding is addressed by an explicit ETS-only exception in AGENTS.md, reflecting
the user's prior instruction to port ETS to C# and retain selectable original
Erlang. Other pure-module policy is unchanged. Review threads are not closed.

The previous tested binary reproduces both native P2 issues: directory rename
fails, and8 independent append resources lose169 of4096 fixed-size records
(3927 survive). See red-native-files.json. The fixed binding passes both oracle
observations (green-native-files.json). Append uses WriteFile's kernel EOF offset,
not seek-to-length; positioned writes retain explicit offsets. A connected oracle
also verifies the Windows positioned-write cursor. Directory moves use the native
file/directory rename operation, including empty destination replacement; the
observer uses real make_dir/del_dir native bindings.

The ordinary no-exclusion build has0 warnings/errors. Both backends pass355 whole
observations,8 stateful,12 terminal and76 filesystem scenarios:451 per backend.
Official OTP29.1.1 validates both exported dumps. The new composite filesystem
scenario checks directory payload preservation, empty/nonempty destinations,
append/positioned-write cursors and8 writers x512 records. All87 selected source
files match tested PDB checksums. Literal/layout and13 passive CI plans pass;
Resource529 admits4320 dependencies. See the separate reports and source bindings.

Old hosted failures are distinguished from current results: Focused13 admission
failed before SDK work; the Linux poll job rejected the previous exact src tree.
The descriptor plan and its external admission now bind the actual final src
subtree and all54 existing declared inputs. Membership, expected54 poll cases,
workflow behavior, approval/execution flags and compatibility statuses are not
changed. Source-plan refresh changes only hashes, sizes, existing dependent pins
and a parent-linked monitor successor. No automatic CI resealing is introduced.

raw-evidence.zip preserves the new raw corpus, dumps, compiled binaries/PDBs,
bound sources and RED/GREEN native observations. archive-sha256.txt binds it.
Prior ETS receipts remain historical. Full local regression, canonical readiness
regeneration, unrelated native suites and manual CI dispatch were not run.
Universal OTP compatibility and final-head hosted CI remain separate gates.
