# Next steps

Part603 preserves DETS fixed-scan continuations across deletion. A retained FREE collection remains readable, while an unknown status at a recorded collection address reports bad_object. The public select/3 to select/1 regression and Windows validation are recorded in [Part603](validation/part-603/README.md).

Next NIF work: validate prepared-loader finish_loading custody, resource monitors/down, takeover and unload, and port guarded 32-bit regex/search layouts. Then execute the same resource/GC fixture on measured ARM64/Unix hosts and add separate ARM32/other 32-bit ABI admission. Keep original ERTS observations and .NET target results separate.

Part601 verifies the native-word resource/magic and atomics/counters port in Windows x86/x64 Debug and Release. All six final-source CI jobs pass, including actual C resource NIF execution, GC retirement and byte parity against pinned original ERTS with word sizes 4 and 8. Other managed resource platforms and full resource lifecycle parity remain open. [Exact scope and results](validation/part-601/README.md).



Next NIF work after Part600: port and validate actual 32-bit resource/magic headers, atomics/counters allocation and loader resource custody; compare local PID-reference/alias wire numbers against original 32-bit ERTS. Then add measured ARM32/other 32-bit OS/CPU NIF adapters and target execution. OpenBSD/NetBSD remain C-only evidence, and Solaris/AIX have no runtime acceptance. Extend the finite fixture to resources, threads, dirty scheduling, upgrades and teardown; retain separate host receipts.

Part600 established ordinary native-word storage/GC and finite NIF execution across its separately measured platforms. [Historical Part600 evidence](validation/part-600/README.md).


Part599: port and validate the 32-bit native-cell/word source profile, including numeric, binary, identifier and GC dependencies, before opening full Windows x86 VM admission. Replace the explicit expected-gate CI test with actual guest load_nif/original OTP comparison only after that port. ARM32/other ILP32 profiles and 32-bit resources remain unimplemented or unverified. FreeBSD 15 amd64 community CLR execution now passes the finite NIF fixture; other BSD/FreeBSD CPU/runtime versions remain separate gates. See [current evidence](validation/nif-freebsd-ilp32-20261105/README.md).


Part597 NIF platform follow-up: obtain a usable pinned .NET10 host on FreeBSD and execute the existing managed callback/image suite; OpenBSD/NetBSD currently have native C evidence only and no managed admission. Add real ILP32 layout/callback profiles on 32-bit hosts, then remaining OTP OS/CPU targets. Extend the finite fixture to resource/GC/thread/upgrade differential cases and validate macOS deployment/signing and packaging. Keep each host's evidence separate; full OTP parity is open. See [target evidence and gates](validation/part-597/README.md).


Part596 fixes PR119 test synchronization: await physical dirty-handler unwind and give the two timer539 cases an outer preparation/phase/drain budget while preserving internal watchdogs and assertions. Production source is unchanged. See [failure analysis and focused evidence](validation/part-596/README.md); fresh hosted checks remain pending.

Part595 repairs the remaining PR119 CI function407 failure: original OTP confirms one tail-badfun source frame, including the exact synthetic image. Focused6/6 and complete66/66 raw/typed outcomes pass. See [native proof and receipt](validation/part-595/README.md); fresh hosted checks remain pending.

Part594 repairs the four CI failures reported for PR119: complete badfun/badarity/apply stacks and bounded Jaro actor batches. Focused final-source checks are recorded in [the repair receipt](validation/part-594/README.md); full hosted validation remains separate.

Part593 complete for the targeted platform package: Windows/Linux/macOS each pass74 managed/86 original grouped checks,21 whole OTP scenarios/full error stacks per backend and bidirectional files. Both review findings, native cursors, reopened free lists and readonly errno are fixed; C#/Erlang selection remains. Descriptor54/54 and exact source/pin/census checks pass. See [bound receipts](validation/dets-reconciliation-20261104/README.md). No full local regression or universal compatibility promotion.

Earlier local Part592: deletion-only portability work is superseded by Part593. Preserve separate host receipts, focused checks and C#/Erlang selection.

Part591: review the isolated [DETS backend PR](dets-backends.md) and its focused OTP receipts. Keep full upstream-suite/resource/platform/interleaving and release-manifest startup acceptance explicit; preserve backend selection and do not promote canonical readiness from partial reports.

ETS PR116 review fixes: see [current evidence](validation/ets-review-20261104/README.md). Original Erlang remains selectable; directory rename and OS-atomic append are connected native acceptance scopes.

Part589 updates ASN.1 PR118 from timer main29b92f63d, retaining both components and the qualified file atom. Fresh focused build,65 checks/26 OTP outcomes and passive bindings pass. [Evidence](validation/part-589/README.md).

Part588 fixes the ASN.1 consumer CS0119 exposed after the file backend was added: qualify the existing file atom constant. Focused build and65 checks/26 fresh OTP outcomes pass; no runtime or compatibility change. [Evidence](validation/part-588/README.md).

[Part587](validation/part-587/README.md) fixes the gen_statem sys continuation review finding:97 retained managed machines become1 after32 generations. Both backends pass74+311 full observations/scenarios. Final-head hosted review/CI and wider OTP acceptance remain separate.

[gen_statem merge repair](validation/gen-statem-merge-20261104/README.md) reconciles gen_statem PR112 with code/file/gen_event main and refreshes declared source identities. Prior module comparisons remain historical; final-head hosted checks and the sys continuation review finding remain open.
Part586 fixes the omitted Monitor source-freeze manifest behind main CI41836436436 and adds a committed-manifest guard. No runtime or compatibility change. [Failure and focused validation](validation/part-586/README.md).

PR111 Part585 reconciles code/file/gen_event on current main, refreshes declared source/producer pins and checks the retained BEAM image hash. Fresh105/105 per backend passes; final-head CI remains required. [Repair evidence](validation/part-585/README.md).


Part584: review the selectable gen_event implementation and semantic constants PR; investigate the retained Managed depth3 zero-timeout upstream failure before merge. Primary105/105 per backend passes; no full regression/readiness refresh was run. [Scope and reports](validation/part-584/README.md).

[Part583](validation/part-583/README.md) repairs Windows checkout conversion of the Part582 source freeze. Raw source admission remains strict; no compatibility status is promoted.

[Part582](validation/part-582/README.md) reconciles file PR110 with code main, retains both backend selections and refreshes source/producer identities. Connected validation and final-head hosted CI apply; compatibility classifications remain unchanged.

[Part581](validation/part-581/README.md) repairs PR110 source admission and producer pins; runtime behavior and compatibility classifications are unchanged. Final-head hosted CI remains required.

Part574 consolidates the final Linux descriptor poll source and audits stale branches. Exact-head Linux/original replay, CI, review and merge remain pending; no API/full-contract promotion. See [branch decisions and acceptance gates](validation/part-574/README.md).

Draft Part568 prepares the bulk physical-occurrence runtime patch in a separate PR to main. Ordsets stays6 Verified/15 KnownDifferences; current whole ETF replay remains RED. See [evidence and open acceptance gates](validation/part-568/ordsets-bulk/README.md).

## Executable queue

- [Managed Unicode utility](validation/part-591/README.md): all20 C# utility exports, an Erlang-free managed closure and retained original/backend selection; current evidence and delivery gates remain explicit.

- Complete final-head CI for [PR114 analysis consumer boot/layout repair](validation/code-testanalysis-ci-20261104/README.md), preserving the unchanged2463-case oracle and original code routing. No full local regression or compatibility promotion.

- Preserve [file literal ownership and focused acceptance](validation/part-580/README.md); run the permanent literal audit when editing this module. Complete prim_file/prim_buffer/RAM native support, then replay the five independent raw/normal/RAM/consult/compressed gates in both backends. Keep verification focused and leave full compatibility/readiness unpromoted while the gates fail.

- Complete [code acceptance](validation/code-port-20261104/README.md) beyond the passing69-export/156+25 corpus: full consumer startup closure, documentation/gzip dependencies, platform errors, races and upstream code_SUITE. Preserve selected code:is_loaded routing and semantic literal ownership; no100% promotion from finite counts.
- Review the scoped gen_statem PR and its [gen_statem publication receipt](validation/gen-statem-port-20261104/README.md). Preserve explicit backend selection and original OTP assets; full upstream-suite, distributed/global/via, resource/platform and metadata acceptance remain separate work.

- [Downstream CI producer repair](validation/part-577/README.md): reconcile the remaining native/integrated producer identities and monitor completed final-head workflows.

- [Unicode CI repairs](validation/part-576/README.md): linear conversion accumulation, 20 focused groups, retained OTP buffer observations and refreshed source-plan bindings; final-head CI remains the delivery gate.

- [Unicode alternatives](validation/unicode-port-20261104/PUBLICATION.md): complete 22-export C# surface and retained original Erlang; focused validation and prior working-corpus evidence are tracked separately.


- Preserve both explicit [string backends](string-backends.md) and their [source-bound local acceptance](validation/string-otp-merge-20261104/README.md). Final delivery is tracked in [PR105](https://github.com/develmax/Erlang.Net/pull/105); further work must retain value/error/full-stack, backing and scheduler/resource controls.

- Validate the combined PR102/main0386ff27 source package, including the accepted [native async/cancellation contracts](validation/part-557/REPORT.md). Keep the [incoming next-step history](history/main-103-next-steps/index.md), existing library scope and unresolved public statistics contracts. The separate persistent-poll owner retains shared runtime/checklist ownership; do not start a competing full run on the PC.

- Follow [integration evidence](validation/integration-20261103/README.md) and [PR #102](https://github.com/develmax/Erlang.Net/pull/102) for the exact source checkpoint and delivery state. Continue the module acceptance tasks below after its recorded merge gates pass; preserve later isolated proposals until their own acceptance.

- Close the [ordsets acceptance gates](ORDSETS_COMPATIBILITY.md): shared term ordering/equality and `lists` helper closure, followed by complete regression and readiness publication. Preserve the exact OTP corpus and repaired native reverse source frames.

- Complete the [math backend acceptance gates](math-backends.md): platform numerical audit and complete regression/readiness publication. The 26 exports must not be presented as 100% semantic compatibility.

1. Read the [remaining-work index](remaining-work/INDEX.md), its linked checkpoint and the latest [changelog volume](../CHANGELOG.md). Resolve their recorded scope against the actual source and current task before implementation.
2. Select the next bounded task from that queue; retain its acceptance criteria, unresolved limitations and original-OTP comparison requirements.
3. Run checks appropriate to that task and record actual results and unrun gates in the changelog and owning plan.

The documentation split does not authorize publication, merge or a new runtime work part. Recorded historical CI and local results are evidence for their original revisions.

## Historical plans

All previous plans, including unfinished scope and handoffs, remain in [ordered historical volumes](history/next-steps/index.md). They preserve the source order and must be interpreted in their recorded checkpoint context. The old entry page started at Part485; consult the maintained queue for later work.

## Maintenance

Keep only current priorities and links here. Maintain detailed plans in remaining-work/, and record completed steps in the active changelog volume.

- Complete the frozen Part569 CI repair acceptance and merge after current mandatory checks/review. [Actual evidence](validation/part-569/ci-integrated-pins-and-reuse/hosted-results/README.md) separates passed9687 units/native cohort/async23 from the still-failed original main pipeline; do not retry unchanged or promote whole-module status.


Timer PR113: the user explicitly requested a full C# timer port with retained Erlang/BEAM and selectable implementation. This is the overriding authorization for this module; the general pure-OTP policy remains. See [timer backend and review evidence](timer-backends.md).
