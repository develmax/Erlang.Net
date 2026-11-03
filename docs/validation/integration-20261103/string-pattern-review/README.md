# Repeated binary pattern search review

[Source and raw evidence manifest](manifest.json) binds both exact source hashes, the originalP2 review, all5226 complete actual outcomes before/after and13 backing observations. Both raw result streams are byte-identical. The inherited1030 reference stack differences remain; no String compatibility promotion is claimed.

The same permanent public test fails on the old library (2623335888 allocated bytes) and passes on the fixed library (1953283688), under the unchanged2GiB aggregate budget. It exercises split(all), split(trailing), and trailing find on a512KiB binary with512 separators; complete result hashes agree. This is an allocation regression guard, not a timing or linear whole-runtime claim. Remaining guest/host copies and copied-suffix equality scans are outside that claim.

One operation owns private copied bytes and separately lazy initial/full patterns. Every read retains the original native copy-read lease and validates current owned metadata. Reuse applies to immutable ordinary backings, including their readonly runtime allocation; native/writable and owned-heap producers preserve the original reader. Different readonly backing identities may reuse only after actual suffix byte equality. No alias is inferred from size, no borrowed span survives await, and no native/host boundary grant or new friend assembly is added.

Initial narrower-cache allocation observations, the rejected first test-build attempt and the actual baseline RED are preserved. The current full9681 regression and final-head CI/review remain mandatory; earlier0e9680 evidence is scoped to its pre-fix source window.
