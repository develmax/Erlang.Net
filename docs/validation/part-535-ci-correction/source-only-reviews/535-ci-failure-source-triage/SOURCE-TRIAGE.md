# Exact535 CI failure source triage

Read-only investigation of PR87 CI40807031466, exact source034f6c76b8ed4b51b1f9d3e3646d9c4261055ad9. Actual CI reports9331 passed/2 failed. This author ran no tests/build/VM/native/Git/CI and changed no active source. Report separates concrete source mechanisms from still-required owner timing/reproduction evidence.

## DeepGraph: activated quadratic native-root walk

Backend NifNativeBinaryHostChecks.DeepGraph569 creates one actual pending native binary, wraps it in20,000 deferred unary tuple nodes through real callback MakeTuple, then resolves the final graph and validates mutated binary bytes. AddValue158 calls RetainGuestValue516162, which invokes RetainNativeValue524 even when ordinary-profile guestRoots is null. Native5248 allocates a fresh seen set/stack and traverses every pending descendant on every new node; there is no environment-level pending inventory in that walk. Construction has about200,030,000 pending-node visits (plus initial binary), rather than linear traversal. Separate AddValue.RetainGraph caches retainedNodes, but it executes after this independent walk and cannot remove its cost.

Crucial535 delta is explicitly in immutable612 diff: the old early return when no live native/atomics/counters resources existed was removed, with no substitute. The traversal code already existed, but that removal activates it on the resource-free ordinary DeepGraph path. This is a concrete performance-regression mechanism, not a claim that native regex predicates themselves walk these all-pending tuple nodes: there is no StaticTerm branch to call the new regex retainer during construction.

Outer test harness WaitAsync uses default15s; backend inner actor-completion wait is30s. The CI15004ms exception is at outer Harness7678 and neither identifies the phase nor cancels/joins the body. Do not extend either timeout or call this flaky without owner measurements. Existing root-owned unchanged-code two-test reproduction provides the timing gate; no timing is invented here.

A narrow prospective fix is a native-root-specific inventory of fully scanned immutable pending tuple/cons/binary/map504 nodes, analogous to existing guest516 stable inventory. Skip only previously committed stable identities, commit only after all required lease publication succeeds, clear at environment cleanup, and leave unknown/mutable pending graphs uncached. Legitimate persistent-map updates create new nodes; preserve actual map/native/regex/counters/foreign controls. Restoring the old three-family global predicate verbatim would omit regex and is not sound.

## Exit signal order499: terminal mailbox-before-token window

The held callback at test42 invokes ReceiveAsync after its release gate and catches OCE only if context.Cancellation is requested44. Mailbox.ThrowIfHalted54–56 also throws an un-tokened OCE on closed593. Actual Terminate first removes the process and marks Terminated1550, then retires its mailbox, and cancels Stop later1582/1677. There is therefore a real terminal window where mailbox closed is observable while the token is not requested. RuntimeNodeFoundationHostActor records the callback OCE in held; test finally106 awaits held, yielding the precise CI stack despite expected actual Killed process completion.

Parent exclusively owns499 implementation/execution. A narrowly terminal actual-dead-process-or-requested-token filter can represent the test's intended cleanup case without swallowing arbitrary live cancellation; preserve all liveness/signal-order/Killed assertions. No blanket OCE catch or runtime ordering change is justified by this static analysis alone.
