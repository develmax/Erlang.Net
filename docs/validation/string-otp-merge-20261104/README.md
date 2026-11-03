# String OTP integration — 2026-11-04

The C# alternative and original Erlang/BEAM preserve all71 exports of OTP29.1.1.
This isolated branch starts at main f0c23d603 and excludes parallel map/ordsets
proposals. It retains main native-caller/async corrections and allocation controls.

The [historical API table](historical-api-audit.md),
[historical summary](historical-audit-summary.json) and
[complete local receipt archive](historical-string-receipts.tar.gz) preserve the
2026-11-03 working-tree audit:37145 observations per backend and9649 full tests.
Their source/assembly identities precede integration and are **not** current-head
acceptance. The archive preserves local part labels560/561/562; the existing main
changelog entries with those numbers remain unchanged.

Current merged-source build, focused tests, source admission and CI/review receipts
are recorded here separately. No compatibility status or default backend is changed
from case counts. The exact-head CI and review gates must pass before merge.

Current isolated build passes with0 warnings/errors. [Seven focused tests](focused-tests.json) pass, including the unchanged main allocation limits. Reusing each successfully compiled search pattern within one operation reduces the measured512KiB repeated-search allocation from2461297400 to2047696672 bytes (limit2147483648); actual native match and source error scopes remain. [Reviewed source guards](source-guard-review.json) and [raw admission](source-bindings.json) pass5400 rows without classification changes. Old publication-resource and actual allocation failures remain in local logs; no thresholds are raised.

The current review correction restores scheduler cooperation for ASCII length
and reverse. Four new public reduction-counter tests pass, while the same tests
fail4/4 with the preceding assembly. Separate reviewed source admission receipts
cover13 passive plans,4200 Resource529 dependencies and5400 MFA/native rows.
Source-plan refresh archives retain prior bytes without promoting contracts.
The reviewed full regression and OTP replay remain pending at this checkpoint.
