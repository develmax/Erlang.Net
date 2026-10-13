# Original232 match-spec failure source audit

Read-only against source pin OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 and published d2e2e034fe5d0ff618184bd45eac49cec360fd57. No Main/candidate edits, builds, harness changes, outcome normalization or exclusions. Original232 executed5670 with5664pass6fail; independent fd8 passed. These two measured failures remain failed evidence; they are not observer-context or raw-decoder issues.

## Table empty body

Pinned erts/emulator/beam/erl_db_util.c2011-2020, after guard compilation and before body compilation, checks `(context.cflags & DCOMP_TABLE) && !is_list(body)` and records exactly `Body clause does not return anything.` before failing compilation. Native is_list requires a cons cell, so [] does not satisfy it. The following general body compiler handles trace dialect separately: this restriction must not be globally applied to all dialects.

The published MatchSpecCompiler.ValidateExpressions allows Nil for both guards and body. MatchSpecProgram initializes body value to true, evaluates no body entries and returns true. MatchSpecRegistrations therefore produces `{ok,true,[],[]}`. The fixture asserts that incorrect SDK behavior, causing native badmatch against the real diagnostic result. Classification: genuine runtime compiler validation gap AND source fixture contract error. A future isolated repair must reject empty table bodies with the exact domain-owned diagnostic and revise this explicit source assertion to the measured error tuple while retaining independent `{ok,done}` completion. Preserve its old source/hash and original232 row. Do not replace its expected outcome with the current native badmatch or exclude it.

Check boundary coverage separately: table [] body error, trace [] body allowed, table nonempty body returning literal [] allowed, empty guards allowed. Diagnostic precedence follows the native guard-before-body ordering; do not append errors blindly after unrelated failures.

## Event return_trace action

Pinned erl_bif_trace.c442 compiles explicit trace patterns through erts_match_set_compile_trace. erl_db_util.c1178-1194 selects send flags DCOMP_TRACE|DCOMP_ALLOW_TRACE_OPS, receive DCOMP_TRACE, and call patterns additionally DCOMP_CALL_TRACE. dmc_return_trace4917-4944 checks trace dialect, body rather than guard context, and tuple arity1 (zero arguments). It does not require DCOMP_CALL_TRACE or DCOMP_ALLOW_TRACE_OPS. dmc_exception_trace has the corresponding validation and likewise emits its flag action. Their VM actions2770 onward set return_flags and push true.

Pinned erl_trace.c807-817 send and886-910 receive run event match programs, retrieve a return_flags local and use the match result to decide output; these paths do not install call-return continuation tracing from those flags. Therefore installation accepts `{return_trace}` for send/receive even though call-specific return behavior has no event effect. The measured original old fixture entered its try `of` branch and returned unexpected, producing `{badmatch,unexpected}`: this independently corroborates acceptance rather than badarg.

SDK ProcessTracing.Pattern currently explicitly rejects RequiredTraceActions return_trace/exception_trace for event targets; ProcessTraceActions(false).Supports also rejects both. Removing only the explicit Pattern condition is insufficient because RequireActions and production Evaluate still reject Execute. A future repair must permit these exact zero-arity true-valued flag actions for event contexts and continue ignoring the flags in event output, preserving call tracing flag behavior. Other action context restrictions are separate; do not make every trace action valid for receive merely because these two are permitted.

Classification: genuine runtime event action validation/support gap AND source fixture assertion error. An explicit future source correction should assert installation return1 and exact trace_info match-spec retrieval, then cleanup false; new boundaries should exercise send/receive, return/exception actions, real events, and absence of fabricated return_from output. Keep original fixture identity if required by the corpus guard and document its historically misleading rejected suffix. Do not weaken the independent done result, rewrite original232 failure, or claim merely registering the action proves event execution.

## Exact old sources and measured outcomes

The following complete rows are copied from artifacts/part-232-initial-mismatches.json, not reconstructed from source-provider shorthand. Expected in these raw differential rows is the measured original result; FixtureExpected is the unchanged independently authored fixture completion and Actual is SDK execution. The raw evidence itself is unchanged.

```json
[
  {
    "Name": "erlang-match-spec-table-empty-body",
    "Source": "-module(match_spec_contract). -export([run/0]). run()->{ok,true,[],[]}=erlang:match_spec_test(any,[{'_',[],[]}],table),done.",
    "Expected": "{error,error,{badmatch,{error,[{error,[66,111,100,121,32,99,108,97,117,115,101,32,100,111,101,115,32,110,111,116,32,114,101,116,117,114,110,32,97,110,121,116,104,105,110,103,46]}]}}}",
    "Actual": "{ok,done}",
    "FixtureExpected": "{ok,done}",
    "NativeStandardOutputHex": null,
    "NativeStandardErrorHex": null,
    "SdkStandardOutputHex": null,
    "SdkStandardErrorHex": null
  },
  {
    "Name": "erlang-process-trace-event-return-action-rejected",
    "Source": "-module(process_trace_contract). -export([run/0,target/1,identity/1]). run()->ok=try erlang:trace_pattern(send,[{'_',[],[{return_trace}]}]) of _->unexpected catch error:badarg->ok end,done. target(S)->receive {send,V}->S!V;{call,V}->S!process_trace_contract:identity(V);{timeout,V}->receive never->ok after 0->S!V end;{dead,Q}->Q!payload,S!sent;stop->ok end. identity(fail)->error(trace_failure);identity(V)->V.",
    "Expected": "{error,error,{badmatch,unexpected}}",
    "Actual": "{ok,done}",
    "FixtureExpected": "{ok,done}",
    "NativeStandardOutputHex": null,
    "NativeStandardErrorHex": null,
    "SdkStandardOutputHex": null,
    "SdkStandardErrorHex": null
  }
]
```

Source notices: original erl_db_util.c, erl_bif_trace.c and erl_trace.c remain covered by the repository consolidated Apache-2.0 notices. No production source adaptation is performed in this audit.
