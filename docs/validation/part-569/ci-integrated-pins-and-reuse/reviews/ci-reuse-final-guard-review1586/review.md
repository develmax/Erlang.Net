# Bounded final guard review1586

Exact scope: collector.py diff from23f85530c05e59dca157ea5827caddc387747f05 to6c966ada688cf35f9202febf8029872015825671, plus adjacent collection/normalization context required to assess the guards. Read through explicit Git objects. No tests, network, shared file edits, Git mutations, CI actions or formal GitHub approval. Raw before/after source, reviewed.diff and hashes are retained beside this report.

After collector Git blob `ccaba6a41cb203a5ca16c50bc8d9f96267b8a399`, SHA256 `709715E97D663377176961D0833AF0C44317AE64DBF580759C54C338BD56CB6F`. Before SHA256 `D00403B043817F92A6CED32240BAD870C27EEF4DF172E6D7FDD74B4C3C672C31`.

## Earlier pending-run concern: resolved

- Lines114-116 require every OTHER required current-merge run to be completed/success. Another queued/in-progress run, or neutral/skipped/stale/startup_failure conclusion, now falls back to full validation.
- Exact GITHUB_RUN_ID exemption permits this admission run's own natural in-progress state. The existing explicit failure check remains before the exemption; a recorded negative conclusion is not ignored merely by matching the ID.
- Lines140-142 reject every required tested-PR-head run not completed/success, including a nonselected pending run. This closes the second1580 case.
- Lines117-118 reject an own merge run attempt other than1. normalize_run:81-83 still rejects selected PR attempts other than1. Equivalent-tree non-success history rejection remains at138-139.

## Remaining bounded finding: successful retries in other history are not rejected

P2, collector.py:138-139 (and113-116 for another current-merge run). The equivalent-tree history condition inspects only conclusion. A concrete metadata counterexample is a previous equivalent-tree required run with `{run_attempt:2,status:completed,conclusion:success}` whose first attempt failed. GitHub's run list exposes the latest attempt's green conclusion; this branch never calls normalize_run or queries earlier attempts. That previous run therefore passes history admission, despite the intended rule that an unexplained earlier failure cannot be erased by a retry. The new own-attempt guard concerns a different current run and does not close this path.

The same omission applies to a different current-merge run that is completed/success at attempt2 while this admission is attempt1. Its completed/success check passes. This is a source-level counterexample; no actual retry of41706938411 is alleged or executed.

Conservative bounded correction: require run_attempt==1 (and completed/success status) for every nonexempt required run used in current-merge/equivalent-history scans, or explicitly inspect all previous attempts and reject unexplained failure/incompleteness. Missing attempt metadata should cause fallback. The selected PR normalization already follows this conservative model. Root/owner may address this separately without changing the now-correct pending guards.

No broader audit or new runtime result is claimed. Focused23 successes remain separate from required full-matrix evidence. Earlier known failed main validation cannot be overridden while it is still recorded as a negative conclusion; the finding concerns the uninspected attempt history after a later green retry.