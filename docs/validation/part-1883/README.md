# Size-result ranges and unread stack projections

The sealed compiler passes all 46 target checks of 9,842. The
first release manifest points to the wrong ELDAP version and is
rejected before comparison. The corrected manifest selects the
exact installed ELDAPv3 path and keeps the same compiler and test
payload. The 96-module uncached run completes in 445.72 seconds
with 5 Equal, 91 Different, 0 Unsupported and 0 SourceRejected.
All 91 changed images prepare. There are five raw Dbgi gains,
29 normalized body gains and no losses in the selected comparison
with Part 1875. Frame distances improve at 134 sites and worsen
at two. The old and new raw widths are retained separately.

The successful size-BIF proof annotates the nearest covered
consumer of byte_size, bit_size or map_size. It follows only a
source marker or a direct copy in the same block. Failing and
unrelated definitions stay outside the proof. Exact native fixture
images agree on 22 values or exception reasons.

The projection change replaces dead physical Y writes with an
initialization until frame compaction can drop the slot. The
single-frame scope declines trimmed, debug and handler shapes
and preserves all reads, including branch joins. The 67 measured
historical functions in 46 modules lose all 82 unread writes and
every historical frame shrinks. Eighteen widths reach pinned OTP.
The closest exact Part 1882 comparison covers 23 functions in 12
modules. Twenty-two shrink and their 30 unread writes disappear.
One already had the exact two-slot width and matching normalized
body. The broader exact intersection covers 31 modules, with
nine normalized gains and no losses.

The unrelated visible regressions are diameter_dict_util:vendor/1
(3 to 5 slots) and et_wx_viewer:queue_to_list/1 (1 to 2). Fresh
recompiles from the sealed Part 1882 compiler reproduce both old
widths. Existing operations consume the still-complete
destructuring form later in the optimization schedule. Earlier
unread-output removal changes that input before those operations
can run. A separate retake places this cleanup after existing
scheduling has settled and returns to compaction after each change.
The initial 67-site scope is preserved to check that fix.

[Source identity](source-freeze.json) ·
[Uncached selection](release-retake/pilot.json) ·
[Projection proof](projection-audit/README.md) ·
[Frame regression witnesses](release-retake/frame-increase-audit/) ·
[Native observation](native-size-ranges/receipt.json).

