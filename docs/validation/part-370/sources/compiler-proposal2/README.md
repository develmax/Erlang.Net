Part370 bounded element no-return continuation proposal

Source only, unbuilt, unexecuted. Root owns integration/build/full validation.
No primitive tuple semantics or raw captured values are changed.

Why ordinary RuntimeTailCall is unsafe

InvocationLoop disposes the source frame/code lease before CallRuntime. Native
BEAM instead executes element before deallocate. A real badarg must therefore
occur inside the actual source frame, retaining arguments/file/line. This
proposal calls the actual BIF through ordinary dispatch and transfers only a
successful actual result afterward. SourceContinuationReturn is internal, caught
at named/anonymous function frames; it is not a guest throw/error or fabricated
result. Actual BIF failures take the unchanged exception path.

Direct and forwarded source contracts

The direct rule requires literal positive element index above2^24-1, correct
builtin resolution, unprotected body continuation and the actual canonical
element body selected in the very same dispatch. It uses pinned tuple-index
type constraints; no fixture/case/helper/atom-a hardcode exists.

The acquired exported-helper counterexample needs a caller contract as well.
SourceBifForwardingContracts recognizes a one-hop local function of any name:
one clause, distinct variable-only parameters, no guard, and a single element
call forwarding parameters. Export status does not weaken this source body
proof. Caller input at the forwarded index slot must be the out-of-domain
literal. Block/single-expression-sequence wrappers are accepted; arbitrary
callbacks, unknown apply, recursion and multi-clause/effectful helpers decline.

The helper executes normally, preserving its real frame, source image and
runtime dispatch. A scoped observation records only the exact analyzed terminal
Expr.Call object in the same ModuleDefinition/ProcessContext, exact argument
relationships and an actual canonical element selection. The caller discards
its continuation only when that event occurred once and the helper returned
the identical actual result object. This closes the new actual helper case
without assuming that every same-named native or host callback is canonical.

Native selection is captured atomically under ModuleRegistry's existing gate,
alongside the selected body and image, not by a racy before/after IsNativeEntry
query. Only CoreModules stamps its actual element delegate. Ordinary host
re-registration, a source replacement and custom InstallPreloaded delegates do
not acquire that stamp. CWT keys avoid retaining failed/retired registrations
after transaction rollback. Existing dispatch-kind enum is unchanged.

Lexical boundaries

The old Expr.Apply continuation analysis is unchanged. A second visitor annotates
only Expr.Call body continuations. Guard and pattern expressions are blocked.
Try.Body is protected; with no After, of/catch clause bodies inherit outer
protection because this try does not catch them. A try containing After is
conservatively declined throughout that region. Catch operands are protected.
Nested anonymous functions start their own frame analysis. Comprehensions are
declined because their generated helper return boundary is different. Shared
AST nodes appearing in allowed and forbidden positions are conservatively
blocked. Metadata is serialized by a public Expr.Call constructor into generated
C#; legacy constructors default false.

The AsyncLocal observation is per logical call flow, owns no native resources
or code lease, and clears context/module/AST/large tuple/result references on
Dispose. Exact-target and argument matching prevents unrelated calls or child
processes from establishing proof. Nested scopes restore their predecessor;
closed scopes reject late events. Existing source frames/leases own execution.

Required review and actual gates

Root's independent permanent-test owner covers direct and pure-forwarded return,
real badarg caller metadata, try expression versus of body, After cleanup,
anonymous function return, actual canonical-body override and shared128MiB tuple.
This proposal includes no duplicate tests. Root must build and run those gates,
replay all old9582 and new264 observations with full fields, and rehash source
guards before any completion claim. The declared one-hop scope is a conservative
implementation boundary, not a claim to reproduce every OTP SSA optimization.

copy-plan.json contains14 source files:7 replacements with exact base hashes and
7 new types. Unified diffs are alongside it. No Main files were edited.
