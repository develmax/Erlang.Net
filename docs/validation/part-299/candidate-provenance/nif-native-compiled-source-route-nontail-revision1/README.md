# Non-tail native installation fixture correction

Source-only two-file delta against the actual root299 isolated tool. Prior candidate/failure logs remain unchanged. No native/build/Main/Git operation occurred here.

Observed failure: target built and managed/native fixture checks passed, but generated-source install/0 called the host bridge in tail position. Execution.Evaluate emits RuntimeTailCall for that generic external host call; its source flow unwinds and the InvokeAsync tail handler disposes the source lease before invoking the intrinsic. The strict CaptureExecutingCode image/owner assertion therefore correctly refuses it. This failure precedes native initialization and is not an ABI failure.

Change only the authored test fixture: install() -> ok = nif_native_compiled_bridge:install(), ok. Sequence evaluation passes tail=false for its first expression; Match also evaluates its value with the non-tail default. The actual calling source scope/lease remains alive during this host intrinsic. Both the embedded source string and its .erl witness change identically. Identity assertions, coordinator authority, Runtime/Compiler code, native profile and other entry-route assertions remain intact.

Pinned OTP uses a dedicated i_load_nif instruction: call_ext, call_ext_last and call_ext_only are rewritten to load first, followed by deallocation/return. The instruction passes its actual instruction pointer to erts_load_nif and holds modification permission across initialization. JIT documents special frame handling. The dynamic BIF stub rejects apply/3. A future public source load_nif path must capture actual caller identity before generic tail-call cleanup and retain its real lease through init; this fixture correction does not implement that prerequisite or establish direct/dynamic loader parity.

Do not preserve every generic source frame globally or weaken identity validation to make this check pass. Root should apply and rerun the actual isolated native generated-source route; no successful retry is claimed here.
