# Part586: commit the omitted Monitor manifest

Run41836436436 on merged main c6ce68bfc7ca29726d1a6f804ba9bc323b3e5795
fails before SDK work. The uploaded source-plan566/preflight.json proves three
failures: Owned506 and Pending reject Monitor inputs.json, while Monitor uses
the predecessor freeze whose registration hash is obsolete. Resource529 and
all5400 MFA/native raw bindings pass. Preserve the hosted failure here.

The previous PR111 helper changed Monitor inputs.json before initializing its
audit. Publication selected paths from that audit and omitted this manifest.
The dependent plans already bind the desired manifest bytes. Commit its exact
Part585 SourceFreezePath and SourceFreezeSha256; do not recalculate expected
values around the stale predecessor, change any acceptance gate or overwrite
the historical freezes. No product/runtime behavior changes.

Strengthen the CIReuse checkout-filter test to compare each working source-freeze
manifest with its filtered HEAD content before checking its referenced freeze.
The new guard rejects the previous candidate that omitted the manifest. This
checks publication input identity as well as checkout behavior.

Reproduction from the committed main manifest produces the same three passive
failures. Restoring the intended manifest makes all13 passive checks pass.
The final isolated committed-tree CIReuse result is retained separately. SDK
build, product VM, full regression and readiness refresh are unnecessary for
this manifest/test-only repair and were not run. Hosted final-head CI remains
required; prior gen_event timing limitations and compatibility scope stand.

Final isolated committed candidate:23/23 CIReuse tests and13/13 passive
source admissions pass. Monitor manifest checkout bytes match the intended
E5EA62E9EC7E72A85439BCC2A1D5DD9B4A722931F7762AA33F1CE448648135FB
identity with core.autocrlf=true and false. No dependent hash changes are needed.
