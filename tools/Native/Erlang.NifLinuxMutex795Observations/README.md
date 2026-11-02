# Linux mutex namespace capability and genuine native-worker controls

SOURCE-ONLY proposal. No active source, C/managed build, original VM, managed VM or CI executed by author. Main ABI9/496 and all existing field offsets remain unchanged. Existing pthread_create/join/key/TLS/exit forwarding source is untouched. No full TLS, C++ TLS or full API compatibility claim.

## Real ownership and ABI

Optional independent native extension1/56 publishes six fixed C callback entries once per ACTUAL bridge/ELF namespace. Explicit enif_mutex_create/destroy/trylock/lock/unlock/name exports use it. No va_list, guessed caller address, first-NIF owner or manufactured language environment is involved. Old9/496 bridges without the extension keep unrelated ordinary NIF loading; they do not pretend to support missing mutex exports.

The bound context is the logical VM's actual native allocation arena. Scoped calls first require that the real current allocation owner belongs to that EXACT arena, then use its true image owner. Unscoped worker create lazily joins a real allocation owner attached to the actual bridge image. Every pointer operation verifies actual registry ownership/arena against the bound VM even if a scope exists. Same-VM upgrade transfer remains valid; foreign VM/foreign scope/destroyed handles reject before unsafe pointer use. NULL name query returns NULL without granting allocation authority; it still participates in callback admission/lifetime accounting.

Namespace close refuses executing callbacks, retains native table/delegates through real bridge close, marks bridge-owner ImageClosed only AFTER that close, then existing final arena reclaim releases outstanding VM-owned opaque allocations. Runtime currently closes namespace before final arena drain, matching this order. Close failure retains the namespace/table through the existing retention path. Callback counters are not worker-completion counters. Actual NIF body leases and real native unload joins fence worker/code lifetime. Arbitrary unjoined workers, calls through stale code after unload, worker-created leaked data in a failed plugin, all TLS APIs and C++ TLS are not magically made safe. VM-global unscoped allocation ownership is explicit (PIN mutex API delegates global driver allocator with no env argument).

## Executable controls

New test-driver mode --nif-linux-mutex795-child (one guarded Program hunk only), two real runtime namespaces, ordinary/reviewed × ordinary/private-owned profiles. It consumes genuine original BEAMs and full original8-int raw+deterministic ETF. It exercises a real C pthread worker creating/destroying its own mutex with no inherited language environment, then compares both VMs' WHOLE outcomes.

Host-only authority controls call ACTUAL C extension exports with real current allocation owners: foreign scope must not override bound namespace authority; foreign handles and destroyed handles reject; denied operations leave both actual allocation ledgers unchanged. Fresh failure state for EACH negative prevents a sticky prior failure from producing a false pass. Global registry counts and real native allocation/domain/env/binary baselines must return, not merely shape/phase assertions.

Native hold/2 uses the same actual worker and mutex. File marker is written only after actual worker-ready publication; the owning native body remains active while the real worker waits. Host starts actual NifBindings.RetireAsync; it must stay incomplete with physical image still open. Releasing the file lets main unlock and perform real pthread_join, after which holder outcome must equal original8 and retirement must physically close image. No cancellation outcome is weakened. Pthread witnesses must prove actual capsule→wrapper bindings plus genuine create/join counts; other six TLS-function execution is not claimed.

## Source-bound Linux execution (Root only)

Apply guarded runtime/test files and Program hunk; build current backend and a CURRENT native bridge with existing build-linux/prepare pipeline. Header change invalidates old source receipts even though main ABI stays compatible. Preserve the mandatory68 comparisons, pthread bindings/create/join,3 ordinary-loader cases and cleanup on FINAL sources; these controls add coverage and do not replace them. All seven current ProjectHeaderInputs consumers already bind this existing API header and snprintf header, so do not weaken/reuse stale hashes.

New repository tool tools/Native/Erlang.NifLinuxMutex795Observations/collect.py reuses existing owned original/provider runner and existing gcc/nm. It checks current bridge receipt/source/header closure, explicit actual host hash and adjacent ordinary bridge copy, compiles genuine fixture, acquires original once, seals exact BEAM/ETF/source/host inputs, runs all four independent managed children and reports aggregate failure. Default is prepare-only. Linux x64 with ASCII fresh output and admitted pinned original build required. Root supplies exact already-produced provider/bridge/host hashes, not placeholders in a claimed execution record:

    python3 tools/Native/Erlang.NifLinuxMutex795Observations/collect.py \
      --repository "$repoRoot" --otp-source "$pinnedOtpSource" \
      --build-receipt "$originalBuildReceipt" --admission-sha256 "$originalBuildSha" \
      --expected-head "$exactHead" --bridge-receipt "$currentBridgeReceipt" \
      --bridge-receipt-sha256 "$currentBridgeSha" --managed-host "$actualBackendDll" \
      --host-sha256 "$actualBackendSha" --runtime-version "$actualRuntimeVersion" \
      --output "$repoRoot/artifacts/linux-mutex795/final-01" --execute

No new CI workflow replaces original gates. Root must integrate source-plan/workflow dependencies and execute; author only validates source syntax/token identity and Python AST. C and all dynamic behavior remain uncompiled/unexecuted here.
