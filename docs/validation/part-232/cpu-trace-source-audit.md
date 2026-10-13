# Part232 CPU and trace query failure audit - 2026-10-14

Read-only Main/pinned source review before fresh repair. Main/frozen6102/6375/profilehash candidates unchanged. Pinned OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 via git show. Root original232 completed5670 comparisons,5664pass/6fail,FD8pass; oldCI6346/6347 allocation failure retained separately. This audit covers three measured CPU/trace-query rows, not other match/return-action/linked-tracer failures.

## Measured rows and exact source identity

- erlang-cpu-state-invalid-query-36: native {error,error,[]}; SDK {ok,done}; independent {ok,done}. SourceSHA256 63DF7B03ED06290ADD058543507A90DC72D8C62D194FE2238D283C92CF4D7D56; generatedCSharpSHA256 183E02742CC7B911C9FEB3D8B49F4E8CD4235B624E789C5FC8ABCA1A6E309152. Full Source retained in part-232-initial-mismatches.json and immutable231 ledger.
- erlang-process-trace-function-query: native {error,error,{badmatch,{traced,undefined}}}; SDK {ok,done}; independent {ok,done}. SourceSHA256 624732C039F6F7A7F18D71CF021A2652F58043BF4126E33EBC0581EF776EC481; generatedCSharpSHA256 8ACEAFA1EFABBAC429D7445D7B2328A91D38854DC686E66E165E24CC1D275931. Full Source retained in part-232-initial-mismatches.json and immutable231 ledger.
- erlang-process-trace-mfa-arity-boundaries: native {error,error,{badmatch,{traced,undefined}}}; SDK {ok,done}; independent {ok,done}. SourceSHA256 1CE8843E974CE63ABFFF9465EA5EDDBDCDC57B67EAB8AB0478CD03B6546F0D64; generatedCSharpSHA256 4E7EB0DC082DD6ECDA80A92BA80737D5872467CCE17CDB3443FBC1CD059EE96A. Full Source retained in part-232-initial-mismatches.json and immutable231 ledger.

All three independent outcomes remain {ok,done}. A correction must update concrete assertions and preserve original failed source/hash/report; no oracle exclusions/normalization/harness skip.

## Internal CPU query is an unprepared native error

erl_bif_info.c2539-2540 internal_cpu_topology arity2 returns erts_get_cpu_topology_term directly. External cpu_topology2541-2547 checks THE_NON_VALUE and goto badarg before formatting. erl_cpu_topology.c1619-1638 recognizes used/detected/defined; unknown which returns THE_NON_VALUE under its reader lock, WITHOUT preparing a BIF error. The raw internal branch is not semantically equivalent to the external/public badarg branch.

erl_process.c12642-12644 initializes fresh process fvalue=NIL,freason=EXC_NULL,ftrace=NIL. error.h EXC_NULL0 means no previous exception; EXC_ERROR includes EXF_SAVETRACE. beam_common.c449+ handle_error begins Value=c_p->fvalue,495+ only saves stack when EXF_SAVETRACE is set,519+ expands the existing reason;527 clears fvalue back to NIL after handling. Therefore a fresh-context unprepared internal failure has default error-class/empty reason, matching measured error[]; it does not establish a universal THE_NON_VALUE=>error[] conversion. Prior caught exceptions can leave primary exception flags/trace state and native internal invalid query does not reset them. This is an undocumented branch with state-sensitive sentinel behavior; general stale-register emulation is outside the bounded repair.

Current managed CpuTopologyState.Get default throws badarg; CpuTopologyStateRegistrations only admits atom selectors for either internal/external. SequenceTraceRegistrations system_info dispatcher falls back to badarg. This masks the specific measured fresh internal error[]; external badarg is already correct.

Metadata: a plain new ErlangException(Nil.Value) defaults error but ExplicitStackTrace=false. Execution.cs139 and768 add WithStackFrame for interpreted/generated call paths, producing an SDK builtin frame. Fresh native EXC_NULL has no EXF_SAVETRACE and initial ftraceNIL, so an empty stack is a source-derived inference for that fresh slice, not an already captured native stack result (mismatch report strips stack). A source-specific fresh internal failure should use explicit empty stack if chosen; do not fabricate erl_erts_errors/badopt metadata, and do not globally change ErlangException or generic sentinel handling.

Concrete bounded proposal: distinguish internal selector validation in CPU dispatcher only; fresh invalid internal selector raises error reasonNil with explicit empty stack; external invalid selector remains badarg. Keep actual used/defined/detected getter and configuration unaffected. Explicitly record missing native stale freason/fvalue/ftrace parity. Correct only failed published invalid-query-36 catch from error:badarg to error:[] (preserve outer restore/Detected assertion/done). Add fresh internal unknown atom/nonatom versus public invalid-selector contrasts and valid used/defined/detected independence. A case after caught badarg needs an actual pinned query before asserting trace/class state; do not invent its expected result from generic error conventions.

## Function trace_info missing MFA returns a keyed tuple

erl_bif_trace.c1386-1401 trace_info_2 delegates default session;1460+ routes tuple What to trace_info_func. get_mfa_tuple1890-1911 accepts atom module/function and SMALL integer arity, using signed_val without 0..255 restriction. trace_info_func1922-1968 validates the requested key before function lookup. Its epilogue2129+ function_is_traced result FUNC_TRACE_NOEXIST branch2156-2159 RETURNS TUPLE2(key,am_undefined).

Therefore missing exported fixture function trace_info({M,missing,1},traced) is {traced,undefined}. Negative arity and huge-but-signed-small arity also describe nonexistent functions and return {traced,undefined}; they are not malformed MFA errors. Signed-small overflow becomes malformed MFA/badarg. Existing trace_pattern count0 for these nonexistent arities remains correct.

Current ProcessTracing.Info validates supported keys traced/match_spec then returns bare undefined at both arity outside int range and HasExport=false exits. Its successful existing-MFA path already wraps Term.Tuple(key,value). Missing local PID branch returns bare undefined by different native protocol and MUST remain unchanged. Event match-spec query is separate. Keyed missing match_spec should be {match_spec,undefined} too.

Bounded authorized repair applied only to shared original-232-repair-prototype Info: both MFA early returns now Term.Tuple(key,Term.A(undefined)); no whole-file overwrite, Pattern/action owner hunks preserved, no Main/frozen edits/build. Correct failed function-query fixture missingtraced assertion; correct both arity-boundary traced assertions; retain actual trace_pattern counts, last overflowbadarg and independentdone. Add missing match_spec, missing module/function, negative/minimum-small/maximum-small arity, overflow and existing disabled/global/cleared queries. Native function lookup also includes code/BIF metadata; registry-export-only lookup remains a broader private/local function coverage gap, not silently claimed full native table parity.

## Boundaries and evidence

No Main edits/builds/original execution by this audit. Shared Info-only repair is source-authorized; other owners control Pattern/actions, match-spec empty-body and linked EXIT. Three explicit correction ledgers must retain full old/new Source bytes, source/generated hashes, caller/policy/finite bytes/independentdone before shared build. New Part233 source contrasts follow all inherited4747. Original232 raw mismatches remain immutable. New profiler MFAs are not original verified merely because local6375 passes.

Inspected Main hashes:
- src/Erlang.Runtime/CpuTopologyState.cs: B8D7E57AC47FB0F374C60CB4388FBB9397C37E3139751E3AFF94B532CF15332E
- src/Erlang.Runtime/CpuTopologyStateRegistrations.cs: 52562F3461A77EA13FAD38E051DAC0FE2308C31794259ADB6B7A504E2660B1A0
- src/Erlang.Runtime/ProcessTracing.cs: 01206E9DB9D9719F0121460AC033B30152135D0DBAFC496EBED8351D4C08ACE2
- src/Erlang.Runtime/SequenceTraceRegistrations.cs: 064485A7C18ACF9F3D2970BF46F3885C83FE9AE6993651C1B6E5D1FC9CAE8BF8
- src/Erlang.Compiler/Execution.cs: D9B3F6680A242843BD49B8B9A848AE196030D2FAD7AE4623C313850D3D71B270
- src/Erlang.Terms/ErlangException.cs: 9A13427A1381FF98965157357BBAC056284057DD89509F5A5923130963600845
