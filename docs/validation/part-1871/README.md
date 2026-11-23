# Call argument lifetimes and nested cleanup selector scheduling

The compiler composes an owned frame that serves only as temporary
storage for a guarded local tail call. It moves a retained input
directly to an ordinary consuming call argument when liveness and a
single predecessor chain prove the interval safe. It spills each
selector of the source-proven nested cleanup pattern after its call
and clears that selector on the success path before the next call.

The first focused report passes 25 of 27 cases. The rejected synthetic
operand uses an atom tag for an integer and the fixture attempts a
nonexistent reader overload. Both raw failures and exact identities
are retained. The test-only retake passes all 27 targeted checks from
9,823. The exact compiler, tests and corpus assemblies are sealed.

The 55-file selection runs uncached in 134.94 seconds with 7 Equal,
48 Different, 0 Unsupported and 0 SourceRejected. No complete file
or section transitions against Part 1870. Twelve changed outputs
prepare in pinned ERTS. Six normalized function bodies gain equality,
including file_io_server:start/3 and start_link/3, the group_history
ordinary call, filename:basedir/2, and the two SCTP peeloff_opts/2
bodies. Four frame distances improve and none increase.

All nine additional modules with a normalized loss in the previous
full report are selected separately. They are Different in 15.44
seconds. All five changed files prepare. Three previously matching
instruction bodies restore equality, including diameter_dict_util,
ssh_acceptor and cover. There are no losses and four frame distances
improve. The combined 64-file selection therefore contains 7 Equal
and 57 Different, with no new or lost raw file matches. Six of the
12 preceding normalized losses are restored.

Pinned ERTS runs actual file_io_server list and binary paths, both
start forms, read/close and invalid guards, and filename:basedir/2
success and invalid-input checks on the reference and generated files.
The nested cleanup fixture agrees for success, both cleanup exits,
ordinary exceptions and protected exceptions. The native file runner
retains the first transcript assertion and the corrected retake.

The next selection combines proof for straight call suffix copies with
strict generator helper lowering. No full corpus or whole-repository
managed acceptance has been run on this exact assembly.

[Source and assemblies](source-freeze.json) · [Focused tests](focused.json)
· [55-file scope against Part 1870](compare-1870/pilot-summary.terms)
· [Nine additional witnesses](added-full-loss-witnesses/pilot-summary.terms)
· [Native cleanup check](native-cleanup/receipt.json)
· [Native file witnesses](native-file-tail-retake/).
