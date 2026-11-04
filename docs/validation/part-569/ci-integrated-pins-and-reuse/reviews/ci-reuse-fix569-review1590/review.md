# Bounded fix569 peer review1590

Exact reviewed head `1a85b50ffb9e14c9d17fa3a0ddec7110af15218d`, compared with main f0c23d603f304acb4f60aa8502e23ddf0fbe0b56 / prior reviewed guard6c966ada688cf35f9202febf8029872015825671. Explicit Git object reads only. No tests, network, shared inputs, source edits, Git mutations, CI runs or formal approval. Collector after SHA256 `CCD2CAFA709257AFE7EBB0249671BCBFFB031D049C465F7FDBF45CA571DBAABF`. Before/after blobs and hashes are retained in bindings-before-after.json.

## Closed findings

1586 P2 is closed by inspection. collector.py:120-126 rejects repeated attempts for every required current-merge row and this admission's own attempt. Lines145-147 reject non-success/incomplete/repeated equivalent-tree history;149-152 reject repeated or incomplete tested-head runs, including nonselected ones. Missing attempt metadata also falls back. Earlier pending-run guards and exact own-run-ID exemption remain.

After ZIP capture, lines184-186 compare the complete required-workflow head list snapshot (ID/path/event/head/attempt/status/conclusion);187-191 re-fetch each selected concrete producer;192-194 compare the current-merge list snapshot. A failed/pending sibling newly appearing on the tested head or current merge changes the list and forces full validation. Producer rerun/completion changes are rejected. Therefore the previously described same-head/current-merge late cases are covered. This is bounded consistency, not an atomic GitHub API transaction.

Artifact admission still verifies unique unexpired named artifact, actual producer run/head origin, actual raw ZIP SHA256 against API digest, exact single bounded receipt member and receipt head/run/attempt. Existing policy still requires the complete configured matrix and manual integrated workflow. A focused component result cannot replace those rows.

## Remaining P2: equivalent histories are not included in final re-fetch

collector.py:144-147 reads each equivalent ancestor's history once and discards its snapshot. The post-ZIP section184-194 refreshes only tested head and current merge.

Concrete source-level counterexample: an equivalent ancestor history was completed/success/attempt1 when read; during ZIP download that required run is retried and becomes attempt2/failure (or a new failed/pending required sibling appears at that ancestor). Tested-head/current-merge snapshots and selected producers remain unchanged, so all final checks pass and reuse can be admitted. No actual occurrence or retry of41706938411 is alleged. This differs from unavoidable change after a final observation: this admitted evidence category has no final observation at all.

If the final re-fetch guarantee is intended to cover every equivalent-history input used to reject known failures, retain per-ancestor required-run snapshots and re-fetch/compare them alongside the other final lists, rejecting any changed/ambiguous history. Root/owner can add a bounded negative control for a history mutation specifically after ZIP capture. Same-head/current-merge cases are already fixed and should remain marked closed.

## ENV709 corrections and unchanged inputs

All three workflow literals now equal both actual raw Git-blob SHA256 of their declared source plans and the existing Integrated PlanPins values:

- Native build-inputs:69762E8ABFC31F6C539B4D66F963C4A3414B5CED99C5D3AF41A339B2CEF5BF42
- Original inputs:AD4A56E2AD3E4B24BAEEFA263DFC9F3DCF16176BA6C8B8551D21C6FC110C499C
- Managed inputs:26EC753A2E8CDADD5BC1C9BF12DADDCB01CD0F6218D8D5A7C7855DDF6083B236

The three source plan Git blobs themselves are unchanged from f0. Production src diff is empty. All178 declared Integrated Inputs and6 DriverSources also have unchanged Git blobs; no actual native/C#/Erlang body was altered by this correction. All10 committed evidence receipt members independently match declared SHA256 and byte length; their actual Git blob IDs are saved in evidence-bindings.json. Original failed ZIP/log evidence remains preserved as failed/preparation-refused evidence, not promoted to a successful full run.

No hosted result acceptance was performed. The supplied19 local controls and pending targeted/manual/full runs remain separate evidence; this review does not assert they passed or confer GitHub approval.