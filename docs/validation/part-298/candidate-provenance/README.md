# Installed NIF inventory candidate

Source only, based on Main Part 296 and disjoint from the BEAM entry overlay. This worker did not edit Main, build, run checks, invoke native Erlang, or change Git state. Add `Test(Erlang.Tests.NifInventoryHostNames.Test, Erlang.Tests.NifInventoryHostChecks.Run);` to the HOST registrations. Verify replaced parents against parent-source.json before applying.

## Pinned OTP source findings

In `otp/erts/emulator/beam/erl_bif_info.c`, nifs_in_module at line 5781 reads only the current module instance's installed NIF library. It iterates `entry.funcs` backwards and conses each pair, yielding the original native function-table order. It does not enumerate declarations, sort names, combine old/current libraries, or expose a pending image before publication. The adjacent has_native/native_addresses implementation still returns false/empty; those metadata keys do not describe installed NIFs.

In `erl_nif.c` around lines 4801–4820, dynamic loading adds a module taint after successful library open and init-symbol lookup but immediately before calling nif_init. A null entry, invalid ABI/name/function entry, or load-callback failure can therefore still leave a taint. erts_add_taint around line 4492 deduplicates persistent module atoms; erts_nif_taints around line 4517 reconstructs the first-admission order. Successful coordinator commit is the wrong taint boundary.

`system_info(nif_version)` in erl_bif_info.c around line 3243 returns the runtime ABI major/minor as a charlist; pinned erl_nif.h currently declares 2.18. This candidate does not advertise that ABI or implement taints. The separate native backend's pre-init boundary and actual supported ABI must govern those APIs. No invented native status key is added.

## Implementation

NifImageBindings prepares an immutable ordered Term while validating the supplied function table, then publishes that Term atomically with the ready dictionary/native owner. Validation failure and aborted initialization expose no installed functions. Explicit retirement clears the installed list; already returned immutable Terms remain valid.

ModuleRegistry.ModuleInformation reads only the exact current image using the non-creating table lookup. Cached metadata includes that installed Term. When direct internal publication or retirement changes its identity, the next query rebuilds the immutable metadata snapshot. Normal repeated queries reuse the cached snapshot and list. Coordinator commits already clear current-image snapshots; pending commits affect their own table, and pending publication installs the exact image through the existing generation path. ModuleInformationSnapshot accepts the installed list without changing the existing module_info/0 field layout or native/native_addresses behavior.

The registry metadata gate is acquired before the table read gate, matching coordinator publication order. No table operation calls back into the registry while holding its gate. A query concurrent with direct internal table publication can return the snapshot read before publication, while a subsequent query observes the changed immutable list identity.

## Prepared HOST coverage and limits

The checks use actual internal coordinator reservations and managed fake owners: inventory before initialization, invalid function validation plus owner cleanup, successful ordered publication distinct from descriptor order, reload rejection, old/current separation, explicit old/current retirement, immutable retained lists, cached snapshot reuse and invalidation. Existing original-hash-checked on_load bytes exercise a ready pending table that remains invisible, followed by actual callback termination and both keep/discard decisions. Fake native owners are explicitly retired in fixture cleanup because automatic native-owner retirement is a separate prerequisite.

All new waits have five-second bounds; shared coordinator HOST wrappers retain their existing ten-second outer actor watchdog. The new HOSTs were not run by this worker. They provide managed metadata evidence only after root execution; no original OTP runtime or native NIF parity claim is made here. Native library ABI, load_nif exposure, automatic owner retirement, taints API and nif_version remain separate work.
