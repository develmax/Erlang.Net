# Part368 local completion

All30 remaining queue exports are integrated and accepted against OTP-29.1.1, commit ad05823719d77c8faee87348ea39513d4e2f99c5. Queue now has **38/38 registered APIs and38/38 complete observable source contracts**. Main implementation remains unpublished.

| Measure | Before | After | Delta |
| --- | ---: | ---: | ---: |
| Registered APIs |617|647|+30|
| Complete contracts |46|76|+30|
| Known differences |30|30|0|
| Needs verification |541|541|0|
| Permanent tests |7561|7599|+38|
| Modules with registrations |16|16|0|

New ends/aliases: in_r/2,out_r/1,get/1,get_r/1,peek/1,peek_r/1,drop/1,drop_r/1,cons/2,head/1,tail/1,snoc/2,last/1,daeh/1,init/1,liat/1,lait/1. New structural/callback APIs: member/2,reverse/1,join/2,split/2,filter/2,filtermap/2,fold/3,any/2,all/2,delete/2,delete_r/2,delete_with/2,delete_with_r/2. The prior eight core contracts remain accepted.

Complete here means the supported finite-term observable Erlang source contract: exact immutable two-list shape and persistence, shallow/improper clause guards, evaluation/error precedence, strict equality, callback order/results/exception propagation, and complete effective arguments/error_info/file/line/depth/caller chains. Source-complete per-MFA branch arguments supplement actual original observations. Genuine own code identities and native member/reverse entry protocols are bound to each VM's actual state; cross-VM physical heap layout, scheduling quantum, timings and performance identity are not asserted. The overall runtime/compiler and other incomplete modules retain their previous classifications.

Original-only Linux GitHub CI[39078144460](https://github.com/develmax/Erlang.Net/actions/runs/39078144460) succeeds at validation head29be5d7c68c5ad04784a57ee399cc99d14a655a4. Exact manifest, all12008 pinned source blobs, producer Git inputs, installed/loaded queue BEAM+MD5 and19normal original/3preflight process receipts are admitted. It records893values and4465whole stack profiles. Windows Main capture4 matches all5358observations:5007strict,331actual named-code relationships and20predeclared own saved-entry relationships. All unchanged fields remain exact. The previous core960 observations pass954strict+6own identities. All72 corruption controls reject altered evidence. No downloaded native binary executes locally; no production BEAM dependency is added.

Actual differences found and fixed: extra apply2 badarity frame; missing filter_r765 and member262 BIF callers; extra staticapply3 probe continuation; and a real already-canceled callback-entry gap, closed with a zero-charge checkpoint immediately before callback execution. Permanent tests preserve exact stack vectors and actual actor progress/normal completion/cancellation/logical death/physical finally unwind, including later filter/delete reconstruction. Existing loader regression then catches staticapply lowering incorrectly granting a direct load_nif source capability; exclude that destination and preserve its original indirect-call refusal.

Capture4 precedes that last one-line load_nif exclusion. [Source review](../source-review-load-nif/review.json) independently checks21exact fixture sources contain no load_nif; their execution path is unchanged, so the existing observations are reused explicitly rather than relabeled a new binary capture. [LF equivalence](../source-review-style/style5-equivalence.json) proves the only subsequent formatting change. Final whole regression runs after both corrections on3299unchanged inputs/382current source guards.

Validation chronology: full1 settles7598/7599 in346.062seconds, failing only the existing direct-source-load-nif capability test; no integration/package credit is taken. Affected8/8 then passes. Full2 stops at style preflight in17.125seconds before tests; onlyCRLF→LF changes. Finalfull3 passes**7599/7599+11integration outcomes+2localpackages in419.204seconds**. Canonical [test report](canonical-tests.json),[integration outcomes](canonical-integration.json),[owned receipt](canonical.exit.json),[source closure](canonical-source-before.json),[acceptance](acceptance-before-classification.json) and[classification](classification.json) are retained.

All617previous API identities/non-Limits fields and7561ordered test names remain. Restore canonical-emitter-reset custom notes byMFA key; explicitly update only the eight obsolete queue Limits descriptions. Exactly30new queue registrations receive complete classification. Global76complete/30Known/541pending; proplists22/22 and erlang16complete are unchanged. Current and installed packaged CLI produce five byte-identical readiness files, with delta+30 against the immutable617-API part367 checkpoint. See[restoration](registry-restoration.json),[full module readiness](../../../MODULE_READINESS.md) and[every command](../../../API_COMPATIBILITY.md).

Raw ETF and metadata stay whole. Oversized files are stored losslessly with exact raw/stored hashes in[raw index](../lossless-raw-index.json) and[extra index](../lossless-extra-index.json). Earlier failed captures, comparisons, source proposals and process receipts remain distinct. One new original build serves all bounded Main correction rounds; two whole test traversals and one style-only preflight are recorded. Timing scopes differ from part367 and do not prove a benchmark speedup.

No Main implementation commit/push/PR/release/package publication. The owner-license draft remains uncommitted. Next60% lane is the prepared binary5 packet; next40% lane is existing contract gaps in dependency order.
