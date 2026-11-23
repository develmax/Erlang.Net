# Interior projection slots and ordinary conditional list helpers

The two independent scopes use Windows OTP 29.1.1 at
ad05823719d77c8faee87348ea39513d4e2f99c5. The linked candidate
starts at the Part 1857 source. The incoming uncached Part 1855 baseline is 408 Equal and 958 Different. The 64-file Part 1857
pilot gains Mnesia, for 12 Equal and 52 Different.

The interior-slot scope retains 5 Equal and 6 Different.
Only inet6_sctp:send/4 changes. The frame and its two tail
deallocation operands shrink from ten to four, and all six
previous stack projections use X. The pinned frame width
now agrees. The C# success heap check still follows the
projections, with five roots to OTP's two. The actual native
positive and malformed input observations agree.

The conditional helper scope retains 2 Equal and 5 Different.
All three Megaco name resolver modules now have the same
recursive helper name, typed operand order and source line as
OTP. Their to_lower/1 bodies also match normalized instructions.
Pinned ERTS loads all three changed images and agrees on proper,
empty and malformed generator inputs. The saved bad_generator
stack includes the lifted helper identity and the original
503, 518 and 577 source lines. Exact controls are retained.

The frozen linked candidate passes all 50 selected tests from
a 9,799-case catalog. Its 73-file release selection reports
12 Equal, 61 Different, 0 Unsupported and 0 SourceRejected
in 198.02 seconds. It retains every exact match from the
64-file Part 1857 selection, including mnesia_kernel_sup.beam.
Compared with the full Part 1855 result, Mnesia is the single
new whole-file match in this selection and no match is lost.

All 55 changed images prepare in pinned ERTS. Among 200
comparable changed functions, 46 gain normalized equality
and none lose it. Thirteen frame comparisons improve and four
move farther, all from the inherited fresh-reference marker
sequence. The three Megaco resolver ImpT chunks and the Mnesia
Code chunk become byte-identical. The three new Megaco helpers
were checked separately because they do not share a key with
the previous generated image. The complete managed gate on this payload is pending.

[Focused cases](focused.json) · [Corpus](pilot.json) ·
[Previous scope overlap](pilot-overlap-delta.json) ·
[Native preparation](pilot-loader-summary.terms) ·
[Function delta](pilot-summary.terms) · [Raw chunks](pilot-section-delta.json).

[Interior slot proof](interior-frame/VERIFIED.md) ·
[Conditional helper proof](conditional-list/README.md).

## Full uncached Windows release result

The frozen candidate confirms 409 Equal, 957 Different, 0 Unsupported
and 0 SourceRejected across all 1,366 installed modules. The only new
byte match is mnesia_kernel_sup.beam and the earlier 408 matches all
remain. Four fresh isolated groups finish in 556.72 seconds. The
previous full comparison took 646.91 seconds. Shard assignments
changed, so elapsed time alone cannot isolate a compiler speedup.

The new compiler changes 415 generated images. Pinned ERTS prepares
413. The other two load through their normal on-load path and produce
the same native result as the reference and preceding candidate:
compressed zstd output with round-trip decoding and the crypto
SHA-256 result. Three MEGACO ImpT sections and the Mnesia Code
section gain raw equality. No chunk equality is lost.

[Full classifications](full-otp-corpus.json) · [Elapsed time](full-timing.json)
· [Frozen launch](full-launch.json).

Across 1,046 changed comparable functions, 73 gain and two lose
normalized instruction equality. The two regressions are
ct_doctest:clean_stacktrace_item/1 and test_server:rewrite_loc_item/1.
Both introduce an intermediate X0 field result and a redundant move
before X0 receives the call's first literal argument. The previous
generated and pinned results project directly into the outgoing X2
or X1 register. The raw listings preserve both cases for the next
general register-lifetime fix. Forty-six comparable frame distances
improve and fifteen increase.

All 1,366 effective build profiles match. The LDAP output's temporary
directory differs, with identical generated source and header hashes.
The independently captured generated Mnesia file is 3,528 bytes with
SHA-256 5B28241196989A4F5AC5993E82CB2803D3970E63F053384AB6497087EE462CC6.
Loading that actual file on pinned ERTS gives the same nine child specs
and supervisor flags as the released image.

[Normalized counts](full-normalized-counts.terms) ·
[Raw sections](full-section-delta.json) · [Build profiles](profile-equivalence.json)
· [Mnesia capture](equal-image-capture/capture-identity.json).
