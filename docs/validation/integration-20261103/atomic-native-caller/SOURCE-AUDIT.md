# Atomic native caller selection audit — OTP29.1.1

## Defect and deterministic witness

ListUniqueMergeTrace previously queried IsNativeEntry and then called the registry
again. Replacement between the two gates could attach a native-only caller PC to
an ordinary selected body. The actual union([[2,1|tail],[1]|tail]) witness changes
the binding at the generated umerge2_2 frame event, without sleeps or racing loops.
The replacement calls genuine erlang:error(replacement_reverse), ensuring the
current source chain is observed. Baseline race-before2.log has the replacement
reason and stale lists:umerge2_2/4 at lists.erl:3471. The same replacement installed
before selection (control-before.log) has no native helper. The earlier direct
opaque throw did not capture the inner frame and was rejected as a witness.

## Selected identity and source writer

Body, image/code lease, native marker and OwnedBindingKind507 are read under the
same existing registry gate. IsNativeEntry's prepared-image/native-primitive test
runs reentrantly under that gate; it cannot observe a second publication epoch.
No callback or await executes while the gate is held. Publication, removal,
code-modification admission and lock ordering are unchanged. Old selected body
and permissions remain authoritative after a later DeleteModule/Register.
The ordinary error-handler path never creates a native caller scope.

## Invocation, tracing and ownership

The selected delegate and selected owned binding kind pass unchanged into the
existing InvokeAdmittedSelection path. There is no new opaque export, guessed
capability, source-image identity or native admission grant. Native scope enters
after selection but before tracing/admission, preserving call/return-to caller PC.
Wrapping only the body was rejected because tracing occurs before the body.
The existing six-argument CallWithDispatch ABI remains as a proxy; existing friend
assemblies are not forced to bind a changed internal signature.

The admitted invocation retains its existing ownership/code disposal protocol.
If scope creation fails first, the selected code lease is disposed and source
code flow restored. After admission takes ownership, the wrapper does not dispose
that lease again. The caller scope uses a finally-backed using across synchronous
return, async yield, Erlang exception and IOwnedControlTransfer506. No scope exists
for an ordinary selected replacement. Existing continuation scopes and all six
native reverse source PCs remain intact; successful values are unchanged.

## Evidence and acceptance limits

Original lists image SHA8941092DC34F0470CD404979E1890DF514D83F2EEB3B0AA8A276553E76471262
is checked. Managed/Beam startup choice and PreparedBeamImage ownership are checked.
New permanent controls cover two native publication kinds, native-to-ordinary
replacement after selection, success, throw, yield, real hibernate control payload,
factory cancellation and scope disposal. The actual source reverse witness and
its preinstalled replacement control are permanent.

The frame observer changes only the test's AsyncLocal execution-frame observation
slot, preserves frame values, ignores context switches, restores the original
slot and does not alter heap handles, owned capabilities or native admission.
It forces one legal registry publication interleaving at a defined frame event.

Authoritative candidate compilation uses read-only ProjectReference paths into
w-docs with all output under this lab, plus an external Compile Remove/Include
for only the four reviewed production inputs. Live source and index are unchanged.
A prior mixed runner used the shared root's newer Managed(string, dependencies)
ABI with the owner's older Managed(string) test binary; its MissingMethodException
is acquisition/ABI mismatch, not a semantic regression, and is retained.

No new dependency is introduced. Reuse existing registry/admission/owned/frame/
exception mechanisms and platform lock/ValueTask primitives. Pinned source
utils.c/erl_map.c/lists.erl and existing Apache-2.0 notices remain unchanged.

This narrow source gate can close after owner exact-source replay/full regression.
It does not independently prove all ordsets/list contracts or justify21/21 status.
Current full baseline9666/9666 belongs to the integration owner; the new two tests
require a fresh9668 run and final-source CI/review before merge.
