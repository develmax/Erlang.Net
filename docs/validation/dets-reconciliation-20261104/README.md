# DETS review and cross-platform acceptance

PR119 is reconciled with main42820e0c7. Both review findings are fixed: hash
growth preserves the saved checksum-valid header; fixed continuations reread
current collection lengths. Original Erlang assets and the64-export backend
selection are preserved. No review thread was closed or PR merged.

Final production/probe head:9de54656cf5ed604f2aa8b6f53d4aeb5eb53b5f9.
Production src tree:13139936aeaa2ad410f9ebe71ef13c22d06e8c41.
[Exact tested inputs](tested-source-inputs.json) bind all7275 production/probe/
fixture files. Later documentation changes do not substitute for these bytes.

| Check | Result | Evidence |
| --- | --- | --- |
| Windows final hosted | PASS74 managed /86 original grouped assertions | [packet](platform-windows/provenance.json) |
| Linux final hosted | PASS74 managed /86 original grouped assertions | [packet](platform-linux/provenance.json) |
| macOS final expanded host | PASS74 managed /86 original grouped assertions | [packet](platform-macos/provenance.json) |
| Primary corpus |21 whole scenarios and complete error stacks per backend | Per-host receipts/ETFs in packets |
| Bidirectional v9 files | All3 kinds, both directions | Per-host packets |
| Permanent review/native-reopen regressions | PASS | [focused groups](focused-dets.json) |
| Local DETS primary/upstream/review |5/5 PASS | [focused groups](focused-dets.json) |
| Connected GC/deletion/native-file |4/4 PASS | [connected](connected-final.json) |
| Passive source admission |13 plans,11 producer pins;4489 Resource529 rows | [plans](source-preflight.json), [pins](source-bindings.json), [Resource529](resource529.json) |
| Descriptor exact checkout |56 inputs /4343 materialized source files | [source receipt](descriptor-source-final.json) |

Earlier macOS execution passes73 managed/85 original assertions and bidirectional
files. Its separate deletion probe rejected a native concurrent [ok,ok] result;
this is not advertised as a green complete matrix. The final host passes the full
primary corpus and independently acquired native leaf outcomes on all3 OSes.

POSIX C/H now live inside Runtime.Core/Platform/Posix/FileSystemNative, so the
existing src sparse checkout includes their actual compile inputs. The shared
C# boundary dispatches before Windows marshalling. Raw byte paths, positioned/
sequential I/O, metadata, directory operations, truncate/sync and cleanup use host
adapters. The SDK builds and copies the helper on Linux/macOS; a target host C
compiler is required. Cached handles preserve cursors across native I/O.

The original21-scenario Windows corpus and its error stacks remain immutable.
On Unix an independent C executable measures readonly ftruncate errno, unchanged
file effects, sequential unlink and2048 fixed pthread races. Its source/executable
hashes and actual outcomes are retained. Exactly3 anchored persistent disk-readonly
delete_all_objects failure fields receive that host's errno; RAM, all other
outcomes and full error stacks are unchanged. Linux captures errno22 (einval),
2048 one-success races and0 both-success races. macOS captures errno22 and
1401 both-success races in the DETS-host producer (a second independent run
observes1029). The Darwin pair is an actual native result, not a relaxed guess.
Whole native OTP execution on
Unix is not claimed by this leaf oracle.

Genuine OTP29.1.1 reopened-file fixtures reproduce the former bad_freelists
failure. See [RED log](native-reopened-red.log), [bound RED identity](native-reopened-red.json)
and the permanent test: read, large allocation, save/reopen and release for all3
kinds. OTP can preserve an earlier allocation origin after head.base resets;
strict alignment/extent checks against the new base were incorrect.

The user's descriptor job first stopped at a stale attributes hash, then at a
missing outside-src helper. Both are fixed while retaining exact raw hashes,
checkout/census checks and all assertions. Run41888977710 then passes all54
controls; final-head run41891986696 also passes all54 controls. See the
[hosted receipt](descriptor-hosted-final.json) and [raw archive](descriptor-evidence.zip). No manual full regression,
validate.ps1, global readiness regeneration or unrelated native suites were run.
Existing automatic repository workflows are unchanged in behavior.

Finite targeted evidence does not certify every upstream/resource/scheduling/
failure scenario or universal100% OTP compatibility. ETS memory measurement keeps
its exact CLR admission; Unix signal delivery is outside this library host.

[Earlier local journal](local-journal.md) preserves the local Part591/592 entries
whose numbers collided with main's history. Earlier failed hosted receipts are
historical; they are not relabelled as passes.
