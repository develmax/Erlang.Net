# Singleton record tail call register planning

The compiler keeps a matched singleton head and its record in the same
register, keeps the checked nil tail available during heap
allocation, places verified field projections in available X registers
and constructs directly into the outgoing argument. Both terminal
branches keep their call, line and stack inputs. An alias-retaining
fallback and an early projection are negative controls.

The first fixture registration used a void return, which the harness
rejected. The unchanged-compiler, test-only correction passes all 32
selected cases from 9,828. The uncached 26-file comparison reports
5 Equal, 21 Different, 0 Unsupported and 0 SourceRejected in 24.32
seconds. inet6_sctp.beam is a new whole-file match. No old match is
lost. Both Code and Attr gain raw equality in that file. All 14
changed files prepare in ERTS and both send/4 bodies gain normalized
equality. The remaining inet_sctp differences are outside that body.

The native test runs 11 exact source cases on pinned and generated
BEAMs, including successful permutations, improper tails and fallback
paths. Both actual generated SCTP modules are separately re-emitted
using the exact corpus profile and compiler. They load in ERTS and
produce the same invalid-socket outcomes as their reference modules.
The exact generated and reference inet6_sctp SHA-256 is
8ca803f2b45a0f72a4d01eb8bc0c5822e45fcc54180fb148a948c1dc0d83d34b.

The independent model identifies the two matching release sites in
918 differing library images before the production compiler retake.
Its data is used for diagnosis. Tested production outputs come from
source and build parameters. The complete uncached 1,366-file run is
in progress. Whole-repository managed and readiness gates are pending.

[Raw scoped comparison](pilot.json) · [Exact scope](scope.json)
· [Function and section deltas](compare-1870/)
· [Release hashes](actual-release-image-hashes.json)
· [Native fixture](native-sctp-registers/receipt.json)
· [Native release outcomes](actual-sctp-runtime-pairs/).
