# Published 257 loader fixture diagnosis

The original run remains **9/11 passed, two failed**. Its declared expectations and raw outputs are immutable. This proposal changes the experimental fixture in a new revision; it does not justify changing runtime BIF implementation.

## Purge result

`code:purge/1` reports whether any lingering process was killed. Pinned `code.erl` documents this, `code_server.erl:1202` returns `DidKill`, and `erts_code_purger.erl:107–128` distinguishes `{WasOld, DidKill}`. The fixture has no process retaining old code: successful removal can return `false`. The original raw result preserves the third handle and successfully finishes it after purging. Change only the independently wrong purge boolean from `true` to `false` in a new fixture revision; leave all loading/consumption results intact.

## Apply route

The literal `apply(erlang,call_on_load_function,[M])` compiled into `call_ext_only,1,{extfunc,erlang,call_on_load_function,1}` in actual `-scenario/1-fun-14-/0`, source line 99. The JIT rewrites that direct instruction to its special on-load instruction. It invoked the real callback, whose absent registered fixture owner produced `{badmatch,false}`. This is not an observation of the generic C-BIF apply path.

Add exported `dynamic_apply/3` with arbitrary module/function/argument-list parameters and `-compile(no_inline)`. Call it through an external module-qualified call from the scenario. Before scenario execution, require its actual disassembly to retain a generic `erlang:apply/3` call and contain no fixed `call_on_load_function/1` target. Retain the existing independent `direct/1` disassembly gate. If compiler shape differs, stop and retain evidence rather than widening the gate.

Pinned `beam_bif_load.c:793–799` independently requires `badarg` for the generic C-BIF under BEAMASM, while `jit/x86/ops.tab:576–584` handles the direct instruction. Therefore retain `{badarg,badarg,badarg}` for the revised truly dynamic scenario, subject to fresh native observation. Preserve the old optimized route and its raw stack as separate evidence.

## Publication conditions

Capture the complete old/new source and expectation ledger, compile and disassemble the revised wrapper on the exact original OTP source pin, and run all 11 variants in fresh VMs. Preserve the original failed run, all five genuine inputs, independent static summary verification, and the failed CI allocation report. No successful rerun can replace those historical failures.
