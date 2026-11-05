# Evidence-bound main CI reuse

The existing Validate main-push trigger and job/status names remain active. PR and
manual jobs always execute the existing checks. Only the Windows
`local-regressions` post-merge body can reuse completed evidence; the other jobs
still execute normally. No review approval or protection change is made.

`prepare` captures actual restored solution/transitive-project assets, package
contents, SDK/compiler bytes, hosted image, configuration, pinned OTP inventory
and workflow/helper sources. `seal` recaptures these inputs after the complete
successful PR body. A changed or missing input prevents a receipt. GitHub job
conclusions and every step remain authoritative, including failed steps hidden
by `continue-on-error`.

On main, the collector requires a unique same-repository merged PR, ordinary
two-parent merge, tested base ancestry, identical complete Git tree, identical
fingerprint, latest successful attempts of all five workflows and their exact
job matrix (including manually dispatched integrated Linux NIF), plus the
receipt artifact's origin, attempt and official ZIP digest. Existing failed,
timed-out, cancelled or action-required validation of the merge or an earlier identical tree prevents reuse. Repeated
workflow attempts also fall back because an earlier failure is not explained.
Missing metadata, API access, artifacts or environment identity falls back to
the complete existing body. A changed runner image also runs the full body.

Artifact storage requests never forward the GitHub bearer token; only approved
HTTPS storage origins are accepted. Diagnostics omit signed HTTP URLs.

Run `python -B -m unittest discover -s tools/Validation/Erlang.CiReuse -p 'test_*.py' -v`.
The positive collector lifecycle uses a controlled API double and is not live
GitHub admission evidence. A real successful PR receipt and subsequent eligible
merge are required to establish hosted end-to-end reuse. The receipt is a
provenance record, never a replacement or composition of raw test reports.

Passive controls also compare every integrated workflow plan pin and its admitted
source/driver bytes. Retry-history checks cover every required current, historical
equivalent-tree and producer run. After artifact download the collector rereads
producer lists/concrete attempts, equivalent-ancestor histories and merge lists; any change falls back. These
are bounded consistent API snapshots, not an atomic GitHub transaction.
