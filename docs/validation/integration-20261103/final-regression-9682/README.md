# Complete exact-head CI regression

[Raw report](tests.json.gz) contains **9682 passed, 0 failed**, exactly9682 test records, all Passed and no partial marker. It is the runner's original JSON, not a composition of local controls. [Manifest](manifest.json) binds its reconstructed bytes and the prior canonical documents. [CI receipt](ci-receipt.json) identifies source commit ad791db4222edddb99a08d0b6e43f0a7e43f193b, run41695595035, job126125501760 and artifact13263658676.

The downloaded19774190-byte ZIP matches GitHub SHA256 **865906B1CE84324874CFDC69A0086C39552F4251B0EDE244EE566FE976E8E9A4**. Raw report SHA256 is **93269D32CBED75B525E104C9BA3791A5AC67668C520AB51665AE0F47E236AAFA**. The completed job log independently confirms exact checkout ad791 and9682/9682 passed. All three jobs in [Validate](https://github.com/develmax/Erlang.Net/actions/runs/41695595035) completed successfully; every step conclusion was checked.

The existing lifecycle388 alternating/full-trace case passed in CI in9920ms. Its [failed complete local report and same-binary isolated pass](../local-regression-9682/README.md) remain separate and unchanged. No source expectation, original image or runtime/watchdog deadline was modified to admit the result.

Canonical readiness uses this genuine CI report. Its existing `LocalPassedTests` field denotes CLR regression outcomes; it does not claim a passing whole run on the developer workstation. The comparison input remains the immutable9263-test/708-MFA snapshot. Current registered APIs are733, delta+25, across21 modules. Source metadata and15 known ordsets differences are retained without a complete-module promotion.

The final documentation commit must retain identical production/test/compiler source trees and pass its own fresh CI and current review before merge. Earlier-head receipts establish only their explicitly stated source window.
