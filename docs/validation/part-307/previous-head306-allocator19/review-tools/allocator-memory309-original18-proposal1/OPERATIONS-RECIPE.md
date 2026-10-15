# Prepared operations; nothing dispatched

These artifact scripts were prepared and syntax checked locally. They did not execute network requests, dispatch workflows, invoke original/SDK/build code, change Main or mutate Git. They retain the approved execution source seal `C73AFABAF43F98F81BD94C52788189429F85931C3D7228C8C8346C38FCEDD0F5`. These operation scripts sit outside that source closure and must not be copied into tools or reseal the reviewed workflow.

After part306 is published, supply its exact recorded40-character SHA. The following is a future single authorized dispatch; the default published ref is `main`, which must resolve to the supplied SHA during GET preflight. The script also compares the published workflow/seal bytes at that SHA with installed validated source. It creates and flushes `dispatch-attempt.json` with `FileMode.CreateNew` before the sole POST. Any existing attempt permanently prevents a second invocation. A failed/ambiguous response requires GET inspection; do not remove the attempt or repeat POST. If `main` moves between preflight and dispatch, the watcher refuses a run whose actual head differs.

```powershell
& D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/allocator-memory309-original18-proposal1/dispatch-once.ps1 -ExpectedHead '<published306sha>'
```

The watcher makes GET requests only and captures one snapshot per invocation; it does not sleep or trigger reruns. Initially omit RunId to discover a unique workflow_dispatch run at the expected head after the recorded attempt. If no run is visible, repeat only GET inspection later. If selection is ambiguous, independently identify the exact run and supply RunId. It refuses head/workflow/event/time drift and any attempt other than1.

```powershell
& D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/allocator-memory309-original18-proposal1/watch-get-only.ps1 -ExpectedHead '<published306sha>'
& D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/allocator-memory309-original18-proposal1/watch-get-only.ps1 -ExpectedHead '<published306sha>' -RunId '<exactrun>' -DownloadTerminalArtifact
```

Use the second form only once terminal state is reached. It writes a fresh `watch-<run>-<utc>/` directory containing complete run/jobs/artifact metadata, artifact.zip, terminal-logs.zip and local download hashes. No credentials or temporary download URLs are written. Credentials come from `git credential fill` into local variables, requests suppress credential/error details, and secret/header variables are cleared in finally. The scripts do not log authorization headers or token values.

Then independently revalidate the downloaded artifact using the local frozen source. Supply the snapshot directory just returned and a new output directory. This is passive Python verification and archive extraction; it executes no Erlang VM, SDK or network calls:

```powershell
py -3.12 -B D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/allocator-memory309-original18-proposal1/review-terminal-artifact.py --repository D:/Distr/Work/Codex/Erlang.Net/Erlang.Net --snapshot '<snapshotdirectory>' --artifact-zip '<snapshotdirectory>/artifact.zip' --output '<newreviewdirectory>' --expected-head '<published306sha>' --expected-run '<exactrun>'
```

The review requires terminal workflow/job/attempt1 success, all original gate steps, one nonexpired original19 artifact, ZIP/download/log hash bindings and the GitHub artifact digest when available. It extracts all archive members without truncation under a bounded fresh directory, rejects path traversal/symlinks/duplicates, checks the installed execution closure, exact OTP29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 source-build/header provenance, fresh controls52/35/16/budget and the run/head/job seal. It reruns the original19 passive typed guard against the complete raw receipt/ETF/capture controls, requires equality with the uploaded typed report, and retains its own full report plus archive member hashes in `terminal-review.json`.

The local JSON snapshots are evidence captured by authenticated HTTPS GET, not a cryptographic attestation of CI identity. Preserve the original snapshot, ZIP and logs for independent inspection. A review refusal or unexpected original result must retain full raw evidence; no retry, policy relaxation or exclusion is authorized by this recipe. Even a successful original19 typed report leaves SDK comparison, generic parity and the disabled startup profile false.
