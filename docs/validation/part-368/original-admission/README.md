# Queue368 original capture and passive admission preparation

Exact run: 39078144460, head `29be5d7c68c5ad04784a57ee399cc99d14a655a4`, branch `validation/queue-368-all`, attempt 1, workflow `proplist-get-value-original.yml`. Expected artifact name remains the registered `original-queue8` workflow alias. Trusted active profile is `artifacts/queue-original368-original1/tool/profile.json`: eight cohorts, 893 named values and 4,465 full-stack profiles. Producer manifest is `F97449F5E82DA98897DEC6CA693FE5880A9C6F542C3BF2CF854BC59152B8A78A`.

Root-only acquisition, after terminal (PowerShell, from repository root):

```powershell
& 'C:/Users/Maksim/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B artifacts/queue-original368-peer1/capture.py --kind original --output artifacts/queue-original368-original1/download
```

Then passive audit, after a successful capture:

```powershell
& 'C:/Users/Maksim/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -B artifacts/queue-original368-peer1/audit.py --download-run artifacts/queue-original368-original1/download/run-39078144460 --output artifacts/queue-original368-peer1/actual1
```

Both output directories must be fresh. Acquisition is GET-only and does not dispatch CI. It checks exact run/head/branch/attempt/workflow/event, one terminal job, artifact identity/digest/size, bounded safe ZIP paths and log size; redirects never forward authorization. Failed terminal runs are retained but cannot pass the success-only passive audit. Credentials are never printed.

The audit executes only these trusted local Python scripts and readonly Git commands, never downloaded code or a VM. It reconstructs a producer snapshot from exact Git blobs; verifies source-plan inputs, all 12,008 pinned OTP blobs, source before/after, 19 normal original processes and three preflight processes; binds installed/loaded queue BEAM full MD5 and module_info significant-chunk MD5; verifies compiler flags, fixture source paths and every retained BEAM hash. It decodes all 893 ordered values and 4,465 ordered full-stack rows with unchanged 16 MiB / 8 million terms / depth 128 bounds. All raw fields are retained, with no frame projection or acceptance of SDK equality. Named-FUN semantic comparison and own trap-state relationships remain separate later comparison gates.

Preparation status: Python syntax checks only passed. No capture, network action, original execution, SDK operation or Main modification has been performed by the author. Actual audit success is not claimed until terminal bytes pass these checks.
