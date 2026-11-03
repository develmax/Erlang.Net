# PR81 empty committed resource-type retirement: guarded source-only fix

No active edits, build, VM/native execution, Git or CI mutation. Root requested review of its current749-based w504 sources. Two production targets only: NativeResourceTypeTable527.cs and NativeResourceDomain.Types527.cs. Root's independent normal-scheduler/native-release changes to Domain524/Payload524 are not copied or overwritten.

## Confirmed defect and pinned behavior

TryRetireEmpty527 currently calls DetachIfEmpty527, removes the committed/name reference and disposes only its selected callback version. The type remains in NativeResourceDomain524.types, NativeResourceTypes527's static dictionary and its GCHandle until Join524 at whole-VM shutdown. A long-lived VM repeatedly loading/purging empty resource-type images therefore accumulates these actual owned objects even though no payload or pending load remains.

The exact pinned erl_nif.c snapshot at primary artifacts/batch528/pinned/erts/emulator/beam/erl_nif.c, lines5349-5364, unlinks the retiring owner's type and frees it when its type refcount reaches0. This is an actual retained-resource defect, not a managed-memory estimate or CLR GC timing issue. The snapshot's raw hash is recorded as supporting provenance; no original acquisition was run here.

## Tiny correction

The table still decides detachment while holding callback writer -> names -> type gates and checks registered, ownerRetired, payloads0 and pending0. It records one successful detachment, exits all table/name locks, then invokes domain.RetireDetachedType575(type). That helper atomically removes the type from the domain list and invokes the existing type.Dispose only if this domain ownership was actually present. Dispose unregisters the opaque token, frees the GCHandle, and releases the current callback-version pin through its existing idempotent Dispose.

Removal and disposal intentionally stay together under DOMAIN gate. A remove-then-unlock-before-Dispose version has a genuine shutdown race: Join524 could observe an empty list and report domain retirement while the GCHandle still exists. The atomic helper shares Join524's existing domain -> type -> static-registry/image-pin order, with no extra async task/state/counter. It runs AFTER all table/name locks. No callback/native body runs in type.Dispose: NifResourceImagePin524.Dispose calls ReleaseCall, which only decrements/signals an asynchronously-continuing drain TCS. Actual C destructor/unload code is not called inline under this domain gate. Root separately confirmed this source property.

No changes to native type token format, allocation/callback admission, payload counts, name visibility, takeover semantics, public counters, scheduler, original fixture or serialization.

## Race/custody audit

- Two concurrent retirement attempts: only the first registered->detached transition succeeds; the second does not enter domain cleanup. Domain list removal adds a second ownership guard.
- Pending takeover/rollback: FindPending527 takes a pending reference under names/type gates. Detach cannot succeed while that reference exists. Successful Install527 selects the new version and clears ownerRetired; retiring the old image cannot detach the new owner. On rollback, the final pending release may legitimately trigger the now-empty retired type's cleanup; the created/uncommitted rollback path still uses its existing DiscardPendingType527.
- Held payload/destructor: payload count is retained through the complete actual C destructor and only released in NativeResourcePayload524.Complete524 afterward. Detach therefore cannot free the type or callback version while that valid payload/callback is active. Callback invocation's own selected-version image pin remains independent.
- Held image/native invocation: removing an empty type drops only its own version pin. Existing NifImageCall/resource invocation pins continue to count in NifImageBindings.activeCalls and prevent actual native-owner/DSO disposal until physical completion. The proposed finite pin control distinguishes empty token retirement from that independent image drain.
- Allocation: registry-read epoch protects payload admission against table writer detachment; after successful detach, existing handle/registered checks refuse new payload retention. No domain gate is acquired while still holding the table writer in the new cleanup call. Resolve527 releases its static-registry gate before callers acquire domain/type gates, so this does not introduce a registry->type inversion.
- VM shutdown: if Join524 acquires domain ownership first, it disposes/clears the type list; the later helper sees no ownership and does not dispose again. If the helper wins, Join cannot consume/report that type until the atomic disposal completes. Existing Join and all table-retirement wait boundaries remain unchanged.
- Raw stale type pointers after their owning image/type lifetime ends are invalid native inputs. Do not invent stable-token reuse guarantees. The repeated control checks the just-retired token before opening another type, when allocator/token reuse could otherwise be legitimate. This patch does not broaden stale-pointer compatibility.

## Permanent finite controls (not executed by this author)

Two new test files and three registration lines reuse existing527 controlled image/transaction helpers; no new native framework or fake original golden:

1.32 repeated CREATE/publication/real binding retirements in ONE logical VM. While loaded, actual domain/static-registry counts are1/1 and GCHandle allocated. After each empty retirement, require0/0 and handle freed, without shutting the VM down. Old code should fail at the first retirement rather than hiding the accumulation in final Dispose.
2. Pending TAKEOVER blocks detachment while old image retires; rollback of the last pending reservation then removes the old empty type. Separately commit TAKEOVER, retire old image, and prove the same type/new owner remains until that replacement image itself retires.
3. Held resource payload prevents type cleanup until actual payload retirement. Separately hold a real physical image pin, retire an empty type, and require its domain/token ownership gone while image retirement remains blocked and its native-owner object stays alive; release pin and require physical image retirement.

The helpers use actual protected list/registry/handle state, never CLR WeakReference/finalizer observations or public manufactured counters. Primary assertion plus all independent transaction/reservation/image/runtime cleanup failures are retained. The controls use existing managed image-owner seams and DO NOT claim a genuine C fixture executed. Parent should compile tests first on old production for red evidence, then apply both guarded core targets and rerun all three controls plus existing create/rollback/takeover/held-callback/logical-unlink/host-scope/allocation/normal-retirement blocks.

For genuine current-source evidence reuse the existing accepted original family and real C/BEAM loader protocol. Optionally capture domain/type-registry counts after actual NIF image retirement and BEFORE runtime.Dispose to prove the real library no longer retains empty registrations; this is a separate host lifetime witness, not a changed serialized original result. Preserve whole default/deterministic ETF, original failed observations and physical cleanup. Root's independent normal-scheduler defect already has its own genuine before/after observations; do not mix that semantic fix with this retained-type cleanup or promote full resource-family/API compatibility from this bounded packet.
