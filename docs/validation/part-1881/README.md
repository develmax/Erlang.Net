# Map include-parent restoration and relaxed tuple alias helpers

All 43 selected cases out of 9,839 pass. The linked 111-module
release comparison takes 898.18 seconds and reports 6 Equal,
105 Different, 0 Unsupported and 0 SourceRejected. The 77-file
Part 1880 pilot and 34-file parent complement bind the before
comparison to the same source/compiler. The complement takes
654.49 seconds on the same bounded worker count.

There are no whole-file gains or losses. All 29 changed images
prepare in pinned ERTS. Seven Dbgi chunks and one LitT chunk
become exact. None lose byte equality. Twelve pre-existing
normalized bodies match, no existing match is lost, and 14
frame distances improve.

All seven expected distinct physical/logical include-parent
restorations become exact Dbgi on the actual compiled parser
cohort. All seven exact Dbgi comparison controls remain. Location
mapping for the executable Line section is a separate problem.
Native C# and OTP fixture images match two outcomes including
the virtual grammar line in the thrown frame.

The actual 25-module relaxed alias audit finds 24 of 28 expected
new helper keys and 26 additional exact function keys across all
forms. There are no exact instruction bodies among the new
expected helpers in this scope. Ten existing bodies match raw
operands/instructions with metadata excluded, 12 normalize to
equality and 13 opcode sequences match, with no losses. Twenty-three
of the new helpers still put the list tail in another register.
Three discarded cases remain gated, and one record_info capture
still has an extra helper argument. Native fixture output matches
seven alias and strict/error cases.

The earlier alias harness hunk no longer applied at its old
line after registering the binary context test. The integrated
registration is recorded against the actual harness. The style
tool removes the independent trailing empty line in the staged
EPP test file. Actual build/test and oracle transcripts are retained.

[Exact source](source-freeze.json) · [Release status](pilot.json) ·
[Complete same-parent comparison](compare-1880/pilot-common-summary.json) ·
[Native scenarios](native-include-and-alias/receipt.json) ·
[Alias audit](alias-audit/README.md).

