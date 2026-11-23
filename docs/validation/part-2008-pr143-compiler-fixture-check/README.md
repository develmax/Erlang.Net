# Part 2008 pinned compiler fixture check

The tuple-binding fixture now checks that both constructions consume their literal directly. The pinned OTP and current compiler both emit those exact construction operands. The bound-match, observable effect and error assertions remain active. [Nine paired runtime observations](tuple-native-observations.txt) agree. A separate bound-pattern instruction difference remains.

The [SDK build](build.json) and [registered whole-module case](whole-module.json) passed. The case compares 308 pinned fixture images byte for byte and runs the subsequent component tests. It took [23.5 seconds](whole-module-timing.json). The [recheck of the 44 previously observed failures](known-failures.json) passed 19 and failed 25. The earlier compiler block did not complete, so this recheck does not count every remaining compiler failure.

The [eight release representatives](release-selection.txt) all remain Different. No member is Equal, Unsupported or SourceRejected, and none was previously Equal. [Pinned ERTS prepared all eight](release-prepare.terms). The [file comparison](release-comparison.json) and [function rankings](ranked-functions.terms) measure different properties. Excess frame slots with equal incoming root counts affected 85 functions in seven of these modules, making frame ownership and slot reuse the broadest measured frame-size category in this sample. [Ranking summary](rank-summary.txt).

No dependency, license or readiness classification changes. The final mandatory matrix and full release corpus remain open.
