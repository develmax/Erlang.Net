# Tuple mapping precedence and positioned binary tail retention

All 41 selected tests of 9,837 pass after correcting the first
40/41 attempt. The original parser matched immediately adjacent
operations before repeated source line markers were removed. The
retake accepts and preserves those markers at their operations.

The 77-file uncached comparison completes in 189.78 seconds with
6 Equal, 71 Different, 0 Unsupported and 0 SourceRejected. No current
raw status changes against the complete Part 1875 checkpoint. The
410 already equal inputs all reproduce byte equality in 99.96
seconds. The 481-module union contains 71 differing files.

Against Part 1875 in the selected scope, 66 images change.
Pinned ERTS prepares 65, and the remaining crypto image loads through
the public callback and produces the same direct and incremental hash.
Three Dbgi chunks gain exact equality. Twenty-six already present
normalized functions gain equality with no losses. The rank records
102 closer frame widths and no farther widths.

The specialized tuple-mapping recognizer again handles a source
sequence before the generalized constructed list helper. This restores
the mnesia_ext_sup byte match lost in Part 1877. The exact emitted
and installed images share SHA256 F5B447B6E6350BF164A0CF2308B54888C14A6D492102D548D0E363819837EBB0.
Pinned ERTS verifies defaults, duplicate startup, termination, restart,
configured shutdown and cleanup on the actual release images.

For the proved binary shape, retain the original context, replace
tail length with the correct bit/byte remaining-size operation and
extract the tail only where needed. ERTS matches 20 synthetic input
and error cases and 19 cases of actual release ssh_bits:mpint/1. The
function has 107 instructions in Part 1877, 98 after the retake and
85 in the reference. The measured total Code chunks are 646, 621
and 623 bytes respectively. Whole-image and Code equality remain open.

[Source identity](source-freeze.json) ·
[Scoped release](pilot.json) ·
[Prior equal controls](prior-equal-controls/controls.json) ·
[Normalized delta](compare-1875/pilot-summary.terms) ·
[First failing regression](failed-ingress/focused.log) ·
[Native binary fixture](native-positioned-tail/results.terms) ·
[Native SSH](actual-ssh/mpint-runtime.terms) ·
[Native Mnesia](native-mnesia/native-1.terms).

