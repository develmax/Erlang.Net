# Native map lookup comparison

The C# callback implements pinned NIF slot112, `enif_get_map_value`. This Windows x64 fixture compares whole returned ETF from actual pinned OTP, ordinary managed loading and reviewed managed loading. It verifies the compiled source copy, recipe/headers/tool/DLL hashes, sole nif_init export, actual policy invocations, native module presence/unload and environment/buffer cleanup. Preparation failure gates dependent execution; independent routes preserve their results before aggregate failure.

The golden was acquired from the original OTP process, never from managed output. The source covers exact numeric and compound keys, a 65-entry map, reference/PID/function identity keys, missing keys, eleven nonmap families, NULL/unrelated environment hints, unchanged output on failure, independent map-owner lifetime and mutable pending binary keys. Successful lookup with a NULL output is excluded from original execution: OTP dereferences that pointer. Managed HOST checks exercise its safe refusal separately.

Run with `py -3 -B tools/Native/Erlang.NifMapValue503Observations/collect.py --repository . --provider-admission artifacts/library376-official-compiler/official-admission.json --plan-sha256 D1A9BE80F21C40E7FD8D84489401BF18F673EA14A5A383FB0170C11766EA204A --output artifacts/native-map503/fresh-output --check-golden`. Use a fresh output directory. Provider6427 files must match before and after execution.

This selected callback is not a whole map mutation/iterator library or general NIF ABI acceptance. Linux/ARM/32-bit execution and future pending map constructors remain separate work.
