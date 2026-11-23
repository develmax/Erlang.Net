# Part 1987 — Placed tuple projections, outgoing dictionary root and mixed generators

Compiler scope accepted. The currently linked repository report remains Part 1983.

| Measure | Actual output |
| --- | --- |
| Uncached complete release | 512 Equal, 854 Different, 0 Unsupported, 0 SourceRejected in 667.20 seconds. New exact None. Lost exact None. [Raw corpus](full-uncached/otp-corpus.json) |
| Focused compiler gate | 603 of 603 tests passed. [Exact coverage](focused-verified.json) |
| Derived affected scope | 334 images from the same uncached compilation. [Affected scope](derived-scope/scope.json) |
| Native/load checks | 82 observed outcomes in 13.08 seconds. 386 prepared, 1 shared on-load restrictions. [Observed behavior](native-scoped/receipt.json) |
| Normalized body equality | 63 gains and 0 losses in 1368 changed functions. [Ranked causes](ranked-causes.md) |
| Actual targeted frame widths | SNMP recovery True. CT retained widths True. [Physical measurement](p87-physical-width-receipt.json) |

The first two source-build failures exposed local-name collisions, nullable operand declarations and an operand/register value mismatch. The repaired source builds without warnings. The original error streams are retained.

The two named SNMP frames recover their Part 1985 widths. Separately, all 164 adverse frame deltas versus Part 1986 are real single- or multi-frame widths reverting to Part 1985 across 39 modules. These 164 frames remain wider than the optimized earlier attempt. [Physical comparison](early-vs-late-frame-physical.terms)

The candidate produces both pinned ordinal keys in both compile:diffable/2 and proplists:split/2. The ordered keys match for these owners. [Generated helper keys](mixed-build-helpers.terms)

The combined package constructs direct list destinations only after the checked ownership and outgoing-copy proofs. It uses the existing generator helper planner with source-derived from_keys and from_list selection, delays dictionary result retention until frame placement finishes, and reuses proven terminal tuple values across heap reservation. The local branch and fixture tests exercise checked list retries and tag fallback. The release characters arm follows the pinned projected order. The other two CT paths still need release-pass activation and independent register placement.
