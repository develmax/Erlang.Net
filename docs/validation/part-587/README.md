# Managed Unicode utility and Erlang-free Unicode closure

## Scope and baseline

Part 587 ports every public `unicode_util` export (20 signatures) to C# and retains
all 22 existing `unicode` exports. The unchanged original `.erl` and `.beam` files
remain selectable for each module. The acceptance baseline is OTP 29.1.1,
`ad05823719d77c8faee87348ea39513d4e2f99c5`, Unicode 17.0. This is the user's explicit
exception to the general pure-Erlang module policy. No package dependency is added.
The existing default registry and global MFA classifications are not promoted.

Implementation is complete before acceptance execution: lazy cp/cpl traversal,
all grapheme states, canonical/compatibility normalization, full casing mappings,
lookup/category/width/identifier/whitespace predicates and spec version. Algorithms
execute in C# against embedded pinned data, never through an Erlang evaluator or
platform-dependent .NET normalization/casing/classification database. Iterative
work slices preserve runtime scheduling/cancellation and avoid CLR recursion on
long prepend, Hangul and deeply nested chardata. Source continuations retain OTP
function names, clauses, original locations and complete error stack behavior.

## Production and backend boundary

Select `Managed("unicode_util")` and
`Managed("unicode", ["lists", "unicode_util"])` alongside managed erlang/lists.
No Erlang source, BEAM asset, Erlang compiler or original VM is needed to start
or execute that application. Data generation and original-OTP observations are
development activities. The permanent production smoke executes NFC before
loading the Erlang observer, verifies every selected module is managed, rejects
BEAM source ownership and checks null BEAM input identities.

Selecting original images explicitly retains original Erlang execution on .NET.
Mixed selections are supported; there is no implicit fallback or live backend
switch. The existing mixed string corpus is retained, and a separate string
consumer test exercises managed string/unicode/utility together. The MSBuild
example chooses both managed modules by default and both originals for `Beam`.
See [selection examples](../../unicode-backends.md).

## Independent data and oracle acquisition

The exact pinned `gen_unicode_mod.escript` and its pinned uc_spec inputs generate
`unicode_util.erl` byte-for-byte identical to the retained upstream source:
`82CDA59A022D642B61A858407043E06B775D9602CEDAFC3EDF58577622F79870`.
The strict extractor produces 4,144 category ranges, 6,878 normalization entries,
3,037 case entries and 961 composition pairs in an 89,285-byte gzip resource.
The gzip independently reproduces byte-for-byte. Original Apache 2.0 and
Unicode 3.0 notices remain under licenses/.

The official admitted OTP 29.1.1 provider is checked against all 6,427 execution
files before and after acquisition. The reference checkout is read-only. The
original observer is compiled with relative source names and identical worker
bodies on both runtimes. `provenance.json` binds the provider execution manifest,
source pin, exact capture command and every input/output hash. Runtime-local
transport pids stay outside compared outcomes; no stack frames are stripped.

ETF cannot preserve subbinary parent ownership. Backing descriptors reconstruct
the identical parent, subbinary, short copy and unaligned source **inside each
runtime**, and the observer recursively compares `binary:referenced_byte_size`
alongside the complete returned value. Fixture ETF is decoded under the receiving
runtime's atom table, as real incoming ETF is; comparison remains exact term
comparison and real `=:=`. Neither correction normalizes observed outcomes.

## Acceptance partitions

- Core:14,801 original observations covering all 20 exports, malformed single-byte
  UTF8, scalars, boundaries, improper/nested/split inputs, every grapheme state,
  invalid suffixes, exact errors/full stacks and backing metadata.
- Long:55 original observations over 12,000 prepends, Hangul leads, combining
  marks, trailing errors and 200 levels of ETF nesting. A permanent managed-only
  smoke additionally constructs12,000 levels directly without ETF's nesting cap.
- Tables: all 1,114,112 codepoints, including surrogate/unassigned values, compare
  category, complete lookup maps, integer/list case shapes and all five Boolean
  predicates. Independent original SHA256 chunks also include eight complete
  grapheme results per codepoint: 8,912,896 transition probes. Integer/list result
  shape participates in the digest; there is no textual normalization.
- Conformance:400,680 normalization comparisons (20 per 20,034 Unicode 17 rows)
  plus 766 grapheme rows, using complete pinned upstream conformance files.
- Connected Unicode and string consumers: explicit exact-case harness selection,
  original Unicode corpus replay, native conversion, backing, growth/cancellation,
  original image/source ownership and both MSBuild choices.
- Two deliberate get_case mutations (changed fold and missing fold key) must fail
  the actual equality comparator in each core/long backend run.

Final reports and executed command lists are retained with their actual assembly,
resource, corpus and source identities. A passing source-plan admission is not a
runtime comparison. No full local regression or manually dispatched workflow is
run; existing required automatic PR workflows remain delivery gates.

## Retained failures and corrections

Initial compilation failures and exact-outcome differences remain in the working
artifacts. Frozen representative failures are retained here. Early discrepancies
revealed missing internal source frames, clause locations, full width-tail
traversal and fixture atom-table/backing admission. All were corrected against
unaltered original outcomes; golden outputs were not edited.

The expanded finite-domain GC audit then detected one failing 4,096-codepoint
chunk. A targeted 32,768-result replay isolated exactly six Balinese consonants
(U+1B0D..U+1B12) followed by Devanagari linker/consonant. Original generated Boolean
clauses overlap broad ranges with singleton clauses; sorting without merging
violates binary-search assumptions. The extractor now coalesces overlapping and
adjacent ranges, the loader rejects overlapping data, and six permanent cases
retain the original cross-script behavior. The exhaustive rerun passes.

The first connected harness attempt exceeded its shared 15-second smoke budget
for the new 14,801-row corpora. Its raw failure is preserved. Complete utility
corpora are divided into four exhaustive interleaved partitions per backend,
each with a bounded two-minute outer budget; the new complete
managed string consumer has two minutes. Actor-command and cleanup limits and
all other test budgets are unchanged. Final connected execution is serialized.

## Compatibility and delivery limits

The20/20 utility and 22/22 Unicode implementation surfaces are complete. Acceptance
is pinned to OTP 29.1.1/Unicode 17.0 and the exact partitions recorded above. Finite
witnesses, even an exhaustive codepoint domain, do not constitute a mathematical
proof over all arbitrarily nested terms, all possible sequences, all platforms
and physical ERTS resource states. No universal 100% claim, unrelated API credit,
full-project readiness refresh, cross-platform receipt, merge or package
publication follows from these local results. Current-head required CI and review
must be checked independently after PR publication.

## Main integration

The work started on main 67809531b. During acceptance, code/file/gen_event and
their source-admission repairs merged through b317a9c2f. Part numbers 578–586 were
already used, so this work is Part 587. Earlier reports and plan proofs retain
their original assembly/source scope under earlier-source. The combined candidate
preserves every incoming backend and original artifact, then rebuilds and repeats
the dependent Unicode/utility acceptance before publication. No earlier receipt
is presented as current-head CI.

## Current candidate results

On the combined b317a9c2f-based candidate, all 28 explicitly selected harness
cases pass. Four interleaved utility partitions per backend cover every one of
the 14,801 original rows exactly once (measured partition times 14–44 seconds in
the earlier-source run, with bounded two-minute outer budgets). No other harness
case or full local regression is run.

Both current utility backends pass 14,801 complete outcomes, including errors,
full source stacks and backing metadata. The current managed assembly also
passes all 1,114,112 codepoint rows / 8,912,896 transition probes, all 401,446
conformance comparisons and all 55 long cases. Each core/long run retains two
comparator-negative controls. Current report filenames start with integrated-.

Both current MSBuild examples build and run with identical results. The managed
manifest selects only managed erlang/lists/unicode/utility, has no image/hash
inputs and produces no BEAM files. The original example keeps both pinned images.

Earlier-source evidence separately records 66,458 original Unicode observations
per backend (65,339 primary + 711 upstream suite + 408 long), plus the 490-case
permanent subset and 29 backing probes. Both earlier utility backends also pass
all 55 long rows. These are retained with their actual assembly identities;
they are not relabelled as the combined candidate. The combined candidate
repeats the 490/711/408 dependent Unicode partitions independently per backend;
all1,609 observations per backend pass with29 backing probes.

## Reproduction

Extract oracle-inputs.zip into a fresh artifact directory; the manifest binds
every original input byte. Build the focused utility validator, then run:

```powershell
dotnet run --project tools/Validation/Erlang.UnicodeUtilCompatibility -- . artifacts/utility-oracle Managed core
dotnet run --project tools/Validation/Erlang.UnicodeUtilCompatibility -- . artifacts/utility-oracle Beam core
dotnet run --project tools/Validation/Erlang.UnicodeUtilCompatibility -- . artifacts/utility-oracle Managed tables
dotnet run --project tools/Validation/Erlang.UnicodeUtilCompatibility -- . artifacts/utility-oracle Managed conformance
dotnet run --project tools/Validation/Erlang.UnicodeUtilCompatibility -- . artifacts/utility-oracle Managed long
```

The retained oracle-capture.py is the original working capture script for the
previously admitted local provider; its absolute/provider assumptions are recorded
in provenance. It is development evidence, not a production dependency. The data
extractor can independently regenerate the gzip from the retained original
unicode_util.erl without executing Erlang. Current source-only admissions and
explicit producer-pin refresh scripts are under source-plan-refresh/.
