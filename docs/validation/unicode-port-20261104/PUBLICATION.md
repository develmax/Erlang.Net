# Unicode publication on current main

This PR adds all 22 pinned OTP 29.1.1 Unicode exports as an explicit managed
alternative and preserves original Erlang/BEAM selection. Shared native conversion
primitives remain runtime-owned; normalization and classification use the selected
pinned unicode_util image. There is no fallback or live backend switch.

The publication starts from main136d956c7b0ff1947a7d599ad2804bfb0a7e130c (PR107).
Only Unicode changes and its required append/guest-function/native-boundary fixes
are transferred. Existing main map semantics, String changes, CI and closed
changelog volumes are preserved. The original dirty checkout remains untouched.

## Fresh checks on the rebased source

- Test and Unicode validator builds: PASS, zero warnings/errors.
- Nineteen connected Unicode/String/binary/function/cancellation groups:19/19 PASS.
- Each backend:490/490 permanent original observations,711/711 upstream-suite
  observations and29/29 binary backing probes, with complete exception stacks.
- Both HelloOtpUnicode MSBuild selections build and execute with equal results.
- Changed24 C# files pass the SDK SourceLayout formatter/check; the append helper
  receives only a blank-line layout correction during transfer.
- Full regression was not run, per the user's explicit focused-check instruction.
  Global readiness, required final-head CI and review are separate merge gates.

Fresh machine reports and input/assembly/source hashes are under
[publication evidence](../unicode-publication-20261104/). They cover the rebased
assembly identities, independently from the earlier working receipt.

## Earlier working evidence

[The preserved working receipt](README.md) records66,458 primary observations per
backend,490 overlapping permanent rows,29 backing probes and19 selected groups.
Its local label Part563 predates the current main changelog and is retained as
history, without replacing main's unrelated Part563 entry. Its hashes/archives
remain unchanged. These older larger-corpus results are not represented as fresh
full regression or universal platform/resource/metadata equivalence.

The current publication journal is Part575. No whole-contract status is promoted
from finite counts. Source checkout consumers build tools/Build/Erlang.BeamBuild
before using the example's manifest tasks. The new audit script accepts an optional
reference-checkout path, allowing an isolated checkout without changing the pinned
OTP reference tree.

## Inherited documentation guard

The global documentation-layout command reports four stale historical separator
source pins. All four affected String files are byte-identical to base main136d956c7;
the PR does not modify them. The exact hashes and failing guard output are retained
in the publication evidence. No shared CI/helper change is made, and this failure
is not reported as a passing metadata gate.
