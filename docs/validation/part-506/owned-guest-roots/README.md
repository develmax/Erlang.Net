# Part506: actual guest payload and root integration

State: local final full9097/9097 and both native loader comparisons pass; exact-head GitHub CI/review pending. No full-contract promotion; all24 differences remain known. Published505 and this working506 source are distinct.

Actual tuple/list payload references now live in reclaimable managed slab cells in a private internal profile. Source invocation, asynchronous continuations, every trampoline carrier, dictionary storage and explicit host borrows retain registered roots. Collection clears the actual unreachable cell payload. Retirement closes new dictionary/heap admission after logical process removal, and process Completion joins physical frame/borrow retirement. DOWN can precede physical completion. The default runtime retains its ordinary storage path; no public resource metric is derived from this incomplete heap.

`ProcessContext.Dictionary` now exposes a `ProcessDictionary` implementing IDictionary and IReadOnlyDictionary. This is a pre-release C# facade migration from concrete Dictionary: enumeration/Keys/Values are snapshots and owned values detach before public escape, outside dictionary gates. Consumers requiring the concrete Dictionary type must use the interface or materialize a snapshot. This is not a claim of an unchanged concrete host API. Complex-key callbacks and native physical-map copying remain conservative/limited.

## Finite evidence

- Independent review found lock inversion, trampoline roots, physical map copy and shutdown join issues; retained findings and main/overlay manifests describe the integration.
- Initial main controls7/8 include an observer blocked on an arbitrary host Task. Replace that test barrier with real ReceiveAsync and explicit resume; combined12/12 then pass. Preserve the failed receipt.
- Source-send retirement reproduces0/1 before repair and1/1 after exporting queued destination/message before escape gates. The source expression still returns its original message.
- Earlier affected6671/6671 predates the final send/private-option/import changes; it is historical, not final-source acceptance.
- Original initial3 jobs and spawn/flags2 jobs run in independent admitted OTP-29.1.1 VMs, retain6427 provider files and real cleanup. Dynamic PID/resource files remain observations, not literal managed goldens.
- A genuine original compiled MFA worker produces complete335-byte default/deterministic profiles. Original BEAM worker/observer and generated source leaf modules run on both ordinary/private runtimes and match both entire profiles strictly after disposal. Raw original/ordinary/private files and actual receipts are retained. Full error stacks remain present. Supported GC/spawn wrapper imports were initially rejected; the narrow host-wrapper admission correction leaves native primitive classification unchanged.
- Final build0 warnings/errors. A constrained full run was stopped incomplete after three port-admission failures; no descendants remained. The complete test-process-enabled rerun is the final acceptance gate. Do not combine partial reports into a full report.

## Remaining shared prerequisites

Trusted same-process calls currently detach conservatively. Precise evaluation liveness, BEAM X/Y and receive roots, owned message fragments, binary/map/function/native payload and roots, generations, exact min/bin/max enforcement, global defaults and public process/statistics/crash/hibernate observations are incomplete. All24 must be compared as complete contracts after these shared domains are integrated. Separate prepared507/508 proposals have not been integrated or executed here.

The ledger remains708 registered /180 full /504 pending /24 known. The implemented selected native map family remains15 callbacks, with whole original comparisons documented in Parts504/505. This packet closes no MFA and makes no whole-project performance or completion claim. LICENSE.local.txt remains excluded.

Final local receipts: full-final.json.gz is the complete9097/0 report. materialized-final.json, reentry-final.json and retained-native-family-final.json compare actual originals/goldens through both loaders with physical cleanup. relocated-original-six.json is successful original-only acquisition, not24 complete managed comparisons. final-source-binding-validation.json checks actual current metadata dependencies; the canonical readiness regeneration succeeds.
