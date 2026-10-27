# Independent focused review of joint key-scope fix507

Reviewed the actual frozen manifest3D08816FD9F634011B015CD194B0E9EDA10A88E03AFFCB576A9E3856271F771C, not the superseded956063 boundary. All five frozen payload hashes match. Active Pattern.Owned507 matches exactly; active OpaquePatternInvocation507 has only formatting changes in Frame.Set arguments, confirmed by its diff and whitespace-normalized equality. Active test-only GuestModule renaming is separately recorded in snapshot.json. No build, execution, active edit, git mutation or CI by this reviewer.

## Verdict

No additional concrete correctness, root-lifetime or lock-order defect identified. This fix resolves the successful key-scope propagation finding in the previous final-hybrid-review. Runtime acceptance remains the parent's independent red/green/final-source regression evidence.

## Mutable scope semantics

Both sync and async completion receive the actual sibling-shared key-scope destination. Complete507 snapshots/imports Bindings and KeyScope jointly, constructs both replacement dictionaries before copyback, and publishes successful key changes to later protected tuple/list siblings. False results leave destinations unchanged; error/control handoff remains protected. Host-retained callback snapshot dictionaries are distinct from destinations and are never refilled with owned wrappers.

Public bindings==keyscope alias does not alias internal destinations: Match/MatchAsync always create a candidate dictionary and a separate key-scope dictionary, even when the original arguments are the same host object. Protected tuple/list descent carries those distinct objects. Alias/map/bit/native-record child public Match calls create fresh pairs again. No canonical route supplies one internal dictionary as both destinations. An opaque extension can reenter public Match, which repeats the cloning boundary. Sibling edits remain visible while the host's original supplied key-scope map remains unchanged and its binding outputs remain portable.

## Output root handoff

The joint imported output is first held by the boundary frame, then published with owned.Preserve before dictionaries reach the source caller. With a genuine guest parent, Preserve reserves a distinct caller root via RetainLocalGraph507; it is not the reusable result/handoff slot. Later guard/BIF calls therefore cannot overwrite the only root of newly produced binding/key-scope payloads. No-parent/public/opaque callers export portable values instead. False/error routes dispose their own admission.

This Preserve slot is not tagged as an expression temporary, so pending511 retirement does not remove it. It conservatively survives until invocation disposal. That longer lifetime is intentional safety, not a public resource-accounting claim. Pending510 graph completeness and512 composition remain their separate tracked work.

## Async, reentry and gates

Unreviewed MatchCore/MatchCoreAsync executes after input export and outside heap/runtime/mailbox gates. Output import/preservation uses existing frame preparation; descriptor work remains outside heap gates. Subsequent string dictionary replacement invokes no guest callback. Matcher awaits are directly awaited and the admitted scope survives output/error transfer. Public ModuleRegistry.Call and FunctionTerm.Invoke keep independent host isolation. Known canonical matcher bodies are closed typed operations; supported equality is nonvirtual, so no arbitrary literal Equals override is presumed.

## Control scope

The frozen desired-safe control covers tuple/list siblings, immediate/suspending overrides, nested public synchronous Match, key-dependent map lookup, output portability, refc backing identity and host readability after actual collection. Stable Preserve rooting across result-slot reuse is established here by source inspection. False/error semantics and same-original-dictionary arguments were checked against actual public clone/return code. This review claims no newly executed test result and no full runtime compatibility closure.
