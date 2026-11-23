# Part 2006 PR 143 source admission and compiler recovery

[GitHub Actions run 45083599674](https://github.com/develmax/Erlang.Net/actions/runs/45083599674) at 74903c091 stopped before SDK setup on stale source identities. The Resource529 closure, focused source checks, MFA/native ledgers and Owned506 admission failed independently. The [original focused diagnostic](before-focused.json) preserves nine exact failing checks. The refreshed plans retain the original dependency rows and add the required current compiler inputs. The prior Monitor645 source freeze is preserved as the parent of the [current freeze](monitor-current-source.json). Three workflow constants follow the hashes of their owning plans. Workflow logic and required checks are unchanged.

## Compiler changes

The grouped tuple entry yields to the complete checked flat selector when distinct tags can share their reads. Captured list length retains its incoming typed register and GC roots after an early list split when the immediate call cannot read the scratch registers. Closure frame and record passes resolve deferred labels and map them once. The straight-line analysis follows local calls through the same eligible plan used for emission. Existing fixed-arity apply, constructed continuation and scrolled-panel planners retain their proven instruction schedules.

The physical optimizer no longer restarts when frame placement reconstructs identical code. The [compiler stack](compiler-block-timeout-stack.txt) and [diagnostic of the identical rewrite](frame-placement-noop.txt) preserve the cause of the timeout. The [reduced standalone input](compiler-stall-representative.erl) [compiles successfully](compiler-stall-resolved.json). The full prepared-map fixture still has a distinct cold-error layout difference.

Fixture changes restore raw operand bounds, compact operand encoding and explicit arguments for reflection calls. The ordinal fixture reflects the pinned elimination of the immediate anonymous application. Existing negative controls remain active. This work reuses the managed compiler and the Apache-2.0 pinned OTP compiler sources at ad05823719d77c8faee87348ea39513d4e2f99c5. No dependency or public API classification changed.

## Validation

| Check | Result |
| --- | --- |
| .NET SDK 10.0.400 test project build | [0 errors and 0 warnings](build.json) |
| C# layout on changed source | [Pass](source-layout.txt) |
| Independent passive plans | [13/13](focused-source-plans.json) |
| MFA and native raw source seals | [5,400 verified bindings](mfa-native-source-bindings.json) |
| Resource529 | [5,761 dependencies verified](resource529-source-plan.json) |
| Owned506 | [Passive preparation passed](owned506-passive.json) |
| Final selected harness | [76/80](fixture-selection.json) |
| Initial compiler block | [Incomplete with raw failures](compiler-block-before.stdout) and [local stop receipt](compiler-block-stop.json) |

All 60 cases from Part 2005 pass. The whole-module test completes its loop over 308 pinned fixture images before a separate BIF predicate and frame assertion fails. The focused and broad reports record remaining opcode, line, frame and metadata differences. Neither this result nor the source admissions certify the full corpus or native contract execution. The last measured full-file ledger is 539 Equal and 827 Different. Current module readiness requires the complete passed harness report. No readiness claim was advanced.

The next validation step is the required PR workflow on the next head, followed by the remaining compiler and runtime failures it exposes.
