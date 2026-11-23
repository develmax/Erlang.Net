# Strict list helper extraction and straight call suffix copies

The straight entry-copy proof accepts independent argument moves and
constant setup before an ordinary consuming call. Plain call_fun
uses only its explicit argument prefix for this proof. Collecting
operations, closure capture, overwrites and implicit fun operands
retain their existing barriers. The strict list generator lowers a
single unguarded list qualifier to the normal recursive helper while
preserving return order, rejection reasons and source lines.

All 28 selected cases from 9,824 pass. Native execution compares the
same source compiled by pinned Erlang and the sealed C# compiler.
Empty, successful, relaxed and nested results agree. Effect order,
mismatch, improper remainder and malformed input agree, with stack
lines 4, 4 and 26. The first oracle command's syntax failure remains
archived with its corrected retake.

The 49-file uncached release scope finishes in 113.44 seconds with
6 Equal, 43 Different, 0 Unsupported and 0 SourceRejected. Whole-file
statuses and raw whole-section status counts are unchanged against
Part 1870. All 39 changed images prepare. Fifteen previously present
normalized bodies gain equality, none lose it, and 36 frame-distance
entries improve. Nine of the 12 known Part 1870 losses are restored.

In the separate exact 34-site helper census, 27 pinned helper keys
appear and 14 of the emitted bodies match after normalization.
There are 23 modules in this comparison. The remaining keys and body
differences are retained individually. This count does not imply a
complete file match.

[Assembly identities](source-freeze.json) · [Tests](focused.json)
· [Release counts and timings](pilot-receipt.json)
· [Normalized body deltas](compare-1870/pilot-summary.terms)
· [Exact helper census](strict-helpers.terms)
· [Native result](native-strict-generator-retake/receipt.json).
