# Timer PR113 review and main reconciliation

The explicit user instruction was: «нужно целиком портировать эту библиотеку с erlang на C#, при этом оставляя версию так же на erlang (чтобы можно было выбирать что использовать)». This is overriding authorization for the timer library port, including its server/interval logic. Keep the retained Erlang source and BEAM unchanged; the general pure-OTP architecture policy still applies elsewhere.

Review of head1b80ebb: the architecture finding is inapplicable under this explicit authorization. The two script findings are confirmed: the driver omitted the requested depth, and captured suite results without running comparisons. Forward depth to both backends and run both suite comparisons when requested; suite mode now accepts an optional depth argument. Both oracle captures receive the same requested depth. Comparison failures still terminate the command.

Merge current main1a2e3e359 without overwriting file/gen_event/gen_statem integration, send-failure stack capture or the newer badfun-frame fix. Preserve main's Part581 and relocate this PR's publication receipts to timer-publication-20261104; its raw archive is unchanged. The merged sys continuation changes are kept.

Validation results and exact source/assembly bindings are recorded alongside this note. Full local regression is not requested. Earlier complete 47-case suite/depth receipts refer to the publication head; fresh focused review checks are reported separately.

Current-main focused results: clean connected build0 warnings/errors; both backends pass728/728 primary and11/11 scenarios at depth0 and8. The new suite-plus-depth CLI executes the selected unchanged upstream `tc` case1/1 on each backend at depth0. This is a focused CLI smoke test; the earlier47/47 full-suite receipts remain scoped to publication head1b80ebb. Driver dispatch checks12/12 and SDK C# layout pass. Passive source admissions pass13/13 plans,4320 Resource529 dependencies and5400 MFA/native source rows. Refresh29 files with one parented488-row current snapshot; membership/status/workflow logic/historical freezes remain unchanged. Exact predecessors are archived.

The new committed-checkout unit initially failed because the exported source had inherited the unrelated parent checkout HEAD. Run it against the actual candidate Git tree before publication; preserve this initial result in the evidence. Full regression remains unrun.

Final candidate-tree checkout test: all23 CI-reuse controls pass, including exact committed Monitor inputs and freeze bytes with Windows checkout filters enabled/disabled. Candidate Git tree 73827ceac6e2f4dd1ce8dd4746faee9098de4df2 contains the updated Monitor manifest itself, as well as all dependent pins. No historical snapshot is overwritten.
