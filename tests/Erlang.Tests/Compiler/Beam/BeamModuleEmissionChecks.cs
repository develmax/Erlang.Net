using System.Text;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamModuleEmissionChecks
{
    internal const string TestName = "compiler/beam/whole-module-matches-otp";
    internal const string CallResultBifTestName = "compiler/beam/call-result-bif-keeps-register";
    internal const string EntryResultCaseTestName = "compiler/beam/call-result-case-keeps-register";
    internal const string AtomTailDispatchTestName = "compiler/beam/atom-case-emits-local-tails";
    private const string ResourcePrefix = "Erlang.Tests.Resources.Beam.";
    private const string Mismatch = "Generated module differs from OTP: ";
    private const string AcceptedUnsupported = "The BEAM compiler accepted a module attribute outside its supported profile.";
    private const string IncludeFixture = "beam_include_probe";
    private const string OuterHeader = "beam_probe_outer.hrl";
    private const string InnerHeader = "beam_probe_inner.hrl";

    internal static async Task Run()
    {
        string[] fixtures =
        [
            "beam_probe",
            "beam_nif_marker",
            "beam_on_load_marker",
            "beam_string_sigils",
            "beam_maybe_enabled",
            "beam_maybe_disabled",
            "beam_guard_extract",
            "tiny",
            "beam_same_line",
            "beam_empty",
            "beam_macro_probe",
            "beam_compile_probe",
            "beam_diagnostic_options",
            "beam_conditional_probe",
            "beam_macro_args_probe",
            "beam_if_probe",
            "beam_argument_probe",
            "beam_local_call_probe",
            "beam_move_probe",
            "beam_frame_probe",
            "beam_straight_call_probe",
            "beam_saved_call_probe",
            "beam_list_element_case_probe",
            "beam_list_call_sequence_probe",
            "beam_list_terminal_calls",
            "beam_list_live_inputs",
            "beam_singleton_call_construct",
            "beam_measured_record",
            "beam_singleton_cons_probe",
            "beam_list_branch_probe",
            "beam_arithmetic_list_entry",
            "beam_case_call_sequence_probe",
            "beam_matched_tail_argument",
            "beam_rebuild_binary",
            "beam_fixed_literal_prefix",
            "beam_binary_tail_recursion",
            "beam_guarded_binary_split",
            "beam_split_continuation",
            "beam_case_tuple_sequence_probe",
            "beam_case_constructor_spill_probe",
            "beam_tuple_arity_fallback_probe",
            "beam_call_sequence_case_probe",
            "beam_computed_tail_probe",
            "beam_forwarded_tail_probe",
            "beam_bif_tail_probe",
            "beam_dynamic_apply_tail_probe",
            "beam_append_tail_probe",
            "beam_octet_shift_range_probe",
            "beam_result_union_probe",
            "beam_atom_selector_probe",
            "beam_finite_atom_probe",
            "beam_finite_entry_dispatch_probe",
            "beam_entry_tuple_tail_probe",
            "beam_singleton_conversion_probe",
            "beam_nested_tuple_field_calls",
            "beam_call_tuple_case_projection",
            "beam_tuple_case_fallbacks",
            "beam_tuple_case_call_chain",
            "beam_tuple_case_construct",
            "beam_tuple_case_known_atom",
            "beam_case_saved_default",
            "beam_tuple_case_effects",
            "beam_case_tail_saved",
            "beam_case_projected_tail",
            "beam_case_multi_projected_tail",
            "beam_tuple_case_continuation",
            "beam_tuple_case_forward_chain",
            "beam_wx_guarded_forward_probe",
            "beam_wx_colour_pair_probe",
            "beam_wx_tuple_repack_probe",
            "beam_wx_option_colour_probe",
            "beam_wx_tuple_overload_probe",
            "beam_dictionary_sequence_probe",
            "beam_captured_list_filter_probe",
            "beam_call_sequence_tuple_probe",
            "beam_tuple_field_calls_probe",
            "beam_call_closures_probe",
            "beam_call_tuple_match_probe",
            "beam_checked_call_sequence_probe",
            "beam_checked_tuple_chain_probe",
            "beam_tuple_fields_chain_probe",
            "beam_tuple_spill_probe",
            "beam_deferred_tuple_probe",
            "beam_tuple_dynamic_case_probe",
            "beam_guarded_list_scan_probe",
            "beam_record_guard_list_probe",
            "beam_list_prefix_case_probe",
            "beam_list_record_merge_probe",
            "beam_list_cons_conversion_probe",
            "beam_list_token_prefix_probe",
            "beam_caught_throw_probe",
            "beam_tuple_case_probe",
            "beam_dispatch_probe",
            "beam_predefined_probe",
            "beam_dynamic_probe",
            "beam_nested_dynamic_tail_probe",
            "beam_record_transport_probe",
            "beam_timed_monitor_probe",
            "beam_guarded_socket_probe",
            "beam_socket_option_read_probe",
            "beam_socket_address_probe",
            "beam_tuple_result_append_probe",
            "beam_listener_start_probe",
            "beam_listener_ownership_probe",
            "beam_guarded_list_lookup_probe",
            "beam_active_socket_probe",
            "beam_maybe_listener_probe",
            "beam_maybe_listener_bind_probe",
            "beam_maybe_socket_connect_probe",
            "beam_guarded_capture_probe",
            "beam_guarded_call_probe",
            "beam_guarded_reply_probe",
            "beam_guarded_stat_probe",
            "beam_guarded_filter_probe",
            "beam_linked_record_closures_probe",
            "beam_socket_accept_probe",
            "beam_socket_controller_probe",
            "beam_tuple_key_filter_probe",
            "beam_tuple_projection_probe",
            "beam_certificate_name_probe",
            "beam_listener_delete_probe",
            "beam_plain_socket_connect_probe",
            "beam_output_setup_probe",
            "beam_output_dispatch_probe",
            "beam_bif_probe",
            "beam_constructor_probe",
            "beam_catch_probe",
            "beam_import_probe",
            "beam_record_guard_probe",
            "beam_record_access_guard_probe",
            "beam_guard_pid_size_probe",
            "beam_not_product_probe",
            "beam_map_emit_probe",
            "beam_length_fun_guard_probe",
            "beam_bit_emit_probe",
            "beam_number_map_head_guard_probe",
            "beam_div_list_ops_probe",
            "beam_try_after_one_line",
            "beam_node_size_guards",
            "beam_body_fallback_probe",
            "beam_callback_probe",
            "beam_callback_docs_probe",
            "beam_map_pattern_probe",
            "beam_fixed_bits_probe",
            "beam_numeric_compare_probe",
            "beam_subtract_probe",
            "beam_local_reference_probe",
            "beam_apply_value_probe",
            "beam_literal_probe",
            "beam_based_float_probe",
            "beam_guarded_forward_probe",
            "beam_disjunctive_forward_probe",
            "beam_literal_call_probe",
            "beam_external_fun_probe",
            "beam_auto_imported_fun",
            "beam_constant_append",
            "beam_local_module_info",
            "beam_wide_dispatch",
            "beam_dynamic_fun_probe",
            "beam_local_fun_probe",
            "beam_multi_capture_probe",
            "beam_recursive_fun_probe",
            "beam_accumulator_fun_probe",
            "beam_tuple_fun_probe",
            "beam_multiple_tuple_fun_probe",
            "beam_adjacent_strings_probe",
            "beam_file_directive_probe",
            "beam_targeted_inline_probe",
            "beam_automatic_inline",
            "beam_inline_bindings_probe",
            "beam_wx_static_probe",
            "beam_wx_nested_option_probe",
            "beam_wx_mixed_option_probe",
            "beam_wx_option_class_probe",
            "beam_wx_object_options_probe",
            "beam_wx_text_options_probe",
            "beam_wx_static_text_options_probe",
            "beam_wx_colour_probe",
            "beam_wx_middle_text_probe",
            "beam_wx_unicode_list_probe",
            "beam_wx_option_list_probe",
            "beam_wx_tuple_options_probe",
            "beam_wx_text_overload_probe",
            "beam_inline_reduction_probe",
            "beam_type_forms_probe",
            "beam_deprecated_docs_probe",
            "beam_eunit_autoexport_probe",
            "beam_grouped_dispatch_probe",
            "beam_binary_register_bifs",
            "beam_terminal_call_match",
            "beam_saved_terminal_match",
            "beam_case_saved_value",
            "beam_deferred_record_call",
            "beam_dead_call_binding",
            "beam_record_branch_frame",
            "beam_inline_record_branch",
            "beam_wildcard_tail",
            "beam_tail_list_suffix",
            "beam_receive_hibernate",
            "beam_discard_constructors",
            "beam_duplicate_lines",
            "beam_monitored_receive",
            "beam_guarded_monitor",
            "beam_singleton_checked_calls",
            "beam_short_append",
            "beam_append_destination",
            "beam_append_frame",
            "beam_append_tuple_branches",
            "beam_tuple_atom_tail",
            "beam_discarded_case_literals",
            "beam_retained_tuple_field",
            "beam_constructed_continuation",
            "beam_reused_constructor_slot",
            "beam_shared_cons_replacement",
            "beam_cons_head_alternatives",
            "beam_shared_frameless_return",
            "beam_ordered_cons_search",
            "beam_shared_local_tails",
            "beam_shared_integer_guard",
            "beam_disjoint_range",
            "beam_closure_argument_tail",
            "beam_retained_record_call",
            "beam_retained_record_dispatch",
            "beam_checked_record_call",
            "beam_trim_tagged_payload",
            "beam_trim_saved_call",
            "beam_trim_tuple_slots",
            "beam_nested_record_call",
            "beam_call_literal_prefix",
            "beam_call_prefix_spill",
            "beam_saved_argument_order",
            "beam_saved_append_swap",
            "beam_cons_identity_tail",
            "beam_nested_literal_record",
            "beam_relocate_before_call",
            "beam_saved_nil_argument",
            "beam_saved_literal_arguments",
            "beam_reuse_saved_literal",
            "beam_trim_live_after_trim",
            "beam_exact_list_projection",
            "beam_shared_call_case",
            "beam_guarded_call_return",
            "beam_call_inside_tail_list",
            "beam_deferred_record_field",
            "beam_spawn_tail_wrappers",
            "beam_terminal_wrappers",
            "beam_apply_tail",
            "beam_call_result_case_tails",
            "beam_membership_filter",
            "beam_guarded_cons_result",
            "tuple_register_entry_probe",
            "tuple_nested_entry_probe",
            "tuple_nested_fallback_probe",
            "tuple_guard_entry_probe",
            "tuple_shared_dispatch_probe",
            "entry_clause_frames_probe",
            "beam_list_nil_construction_probe",
            "beam_list_leading_nil_probe",
            "guard_first_call_probe",
            "bound_tuple_probe",
            "beam_record_details_probe",
            "beam_expanded_reply_probe",
            "beam_binary_digit_list_probe",
            "beam_formatting_closure_probe",
            "beam_captured_table_probe",
            "beam_split_list_join_probe",
            "beam_column_mapping_probe",
            "beam_term_expansion_probe",
            "beam_protected_term_closure_probe",
            "beam_dynamic_detail_reply_probe",
            "beam_detail_window_probe",
            "beam_detail_tabs_probe",
            "beam_tagged_argument_error_probe",
            "beam_constant_record_argument_probe",
            "beam_atom_checked_tail_probe",
            "beam_page_comprehension_probe",
            "beam_detail_callbacks_probe",
            "beam_guarded_page_args_probe",
            "beam_returned_record_update_probe",
            "beam_delayed_page_info_probe",
            "beam_record_field_call_update_probe",
            "beam_deconstruct_probe",
            "beam_move_five_probe",
            "beam_call_case_probe",
            "schema_probe",
            "beam_finite_dispatch_probe",
            "beam_eldap_pattern_probe",
            "beam_eldap_calls_probe",
            "beam_eldap_try_probe",
            "beam_signed_integer_probe",
            "beam_ber_tag_probe",
            "beam_choice_search_probe",
            "beam_field_codec_probe",
            "beam_tag_decoding_probe",
            "beam_ber_lists_probe"
        ];
        foreach (string module in fixtures)
        {
            CompareFixture(module);
        }

        BeamEldapEmissionChecks.Check();
        CompareWarningOptions();
        await CompareBranchBindings();
        await CompareComprehensionFilterBindings();
        await ComparePatternedFunctionBody();
        await CompareCallResultComparison();
        await CompareTupleTailArgument();
        await CompareConsCallMatch();
        await CompareMixedListNilFallback();
        await CompareCaseTupleSlot();
        await CompareThrowStackObservers();
        await BeamListRecordTailChecks.Run();
        await CompareEntryTaggedProjection();
        await CompareUnusedEntryTupleFields();
        await CompareEntryAtomSuffix();
        await CompareLiveListBranch();
        await CompareNestedEntryCalls();
        await CompareNestedInlineListCheck();
        await CompareGuardedBifTail();
        await CompareBuildMacroDefinitions();
        await CompareMatchSpecSourceTransform();
        await CompareDiameterRecordSourceTransform();
        CompareIncludeFixture();
        CompareHeaderFixture("beam_lowering_probe", "beam_lowering_header.hrl");
        CompareHeaderFixture("beam_header_docs_probe", "beam_docs_types.hrl", "include");
        RejectUnsupported("-module(custom). -compile(no_ssa_opt).");
        _ = CodeGeneration.CompileBeamModule("-module(custom). -export([f/1]). f(X) -> X = g(), io:write(X). g() -> ok.", "custom.erl");
        _ = CodeGeneration.CompileBeamModule("-module(custom). -export([f/1]). f(X) -> case application:get_env(a,b) of {ok,X} -> X; undefined -> [] end.", "custom.erl");

        RejectMissingStringSeparator();
        RejectInvalidOnLoadDeclarations();
        await CompareExternalFunctionInvocation();
        await CompareDynamicFunctionInvocation();
        await CompareGeneralDynamicRemoteInvocation();
        await CompareGeneralClosureInvocation();
        await CompareGeneralNamedClosure();
        await CompareGeneralAppend();
        await CompareGeneralCatch();
        await CompareForwardedCatch();
        await CompareGuardedEntryCallChain();
        await CompareConstantReturningCallChain();
        await CompareSavedCheckedPayload();
        await CompareTrimmedCheckedPayload();
        await CompareGeneralRecordGuard();
        await CompareGeneralBitStringReferenceAndMapKeyGuards();
        await CompareGeneralMapComprehensions();
        await CompareGeneralBinaryComprehensions();
        await CompareGeneralBinaryGenerators();
        await CompareInlineEffectsAndReferences();
        await CompareInlineClosureBindings();
        await CompareInlineComprehensionBindings();
        await CompareInlineRecordActions();
        await CompareInlineGuardedClauses();
        await CompareInlinePatterns();
        await CompareFoldCallbackClauses();
        await CompareListEntryFallback();
        await CompareGeneralMaybeAndMapSizeGuard();
        await CompareGeneralNumericAndMapLookupGuards();
        await CompareGeneralRecordAccessGuard();
        await CompareGeneralPidAndTupleSizeGuards();
        await CompareGeneralElementNotAndProduct();
        await CompareGeneralMapExpressions();
        await CompareGeneralLengthAndFunctionGuards();
        await CompareGeneralBitExpressions();
        await CompareSplitBinarySingleton();
        await CompareFinalBinaryCall();
        await CompareMeasuredRecordSegments();
        await CompareGeneralNumberMapHeadGuards();
        await CompareGeneralDivisionAndListOperators();
        await CompareGeneralTryAfter();
        await CompareGeneralNodeAndSizeGuards();
        await CompareBodyBitstringSizes();
        await CompareGeneralBodyFallback();
        await CompareEntryTuplePatterns();
        await CompareListCallerFacts();
        await CompareStructuralListEntry();
        await CompareConsResultEntry();
        await CompareGeneratedBehaviourInfo();
        await CompareGeneralListQualifiers();
        await CompareGeneralFixedBitPatterns();
        await CompareBinaryTailRecursion();
        await CompareBinaryArgumentProof();
        await CompareGuardedBinaryBodyMatch();
        await CompareInlineLocalBindings();
        await CompareGeneralNumericEquality();
        await CompareGeneralSubtraction();
        await CompareXorOperators();
        await CompareRangeAndNativeRecordPredicates();
        await CompareGuardExtraction();
        await CompareZippedListGenerators();
        await CompareGeneralLocalFunctionReference();
        await CompareGeneralSelectiveReceive();
        await CompareReceiveMapAndTypeGuards();
        await CompareReceiveComputedGuardsAndCatchAll();
        await CompareReceiveGuardAlternativesAndMapKey();
        await CompareReceiveTupleAndDisjunctionGuards();
        await CompareReceiveBinaryPatterns();
        await CompareGuardExtrema();
        await CompareGeneralFunctionValueCall();
        await CompareConstructedTupleBinding();
        await CompareLocalClosureInvocation();
        await CompareRecursiveClosureInvocation();
        await CompareAccumulatorClosureInvocation();
        await CompareTupleClosureInvocation();
        await CompareMultipleTupleClosures();
        await CompareInlineListReduction();
        await BeamGeneralTryEmissionChecks.Check();
    }

    private static void RejectInvalidOnLoadDeclarations()
    {
        string[] invalidForms =
        [
            "-on_load(init/1). init(_) -> ok.",
            "-on_load(missing/0).",
            "-on_load(init/0). -on_load(init/0). init() -> ok."
        ];
        foreach (string forms in invalidForms)
        {
            try
            {
                _ = CodeGeneration.CompileBeamModule("-module(invalid_on_load). " + forms, "invalid_on_load.erl");
            }
            catch (CompileException error) when (error.Code == CompilerDiagnosticCodes.FunctionDefinition)
            {
                continue;
            }

            throw new InvalidOperationException("The BEAM compiler accepted an invalid on_load declaration.");
        }
    }

    private static void RejectMissingStringSeparator()
    {
        const string source = """-module(custom). -export([f/0]). f() -> "a""b".""";
        try
        {
            CodeGeneration.CompileBeamModule(source, "custom.erl");
        }
        catch (CompileException error) when (error.Message == "Adjacent quoted strings require a separator.")
        {
            return;
        }

        throw new InvalidOperationException("Expected the compiler to reject quoted strings without a separator.");
    }

    private static async Task CompareInlineListReduction()
    {
        const string file = "beam_inline_reduction_probe.erl";
        byte[] bytes = CodeGeneration.CompileBeamModule(Encoding.UTF8.GetString(Read(file)), file);
        BeamModule image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        Term improper = new Cons(Term.A("head"), Term.A("tail"));
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term success = await image.Invoke(context, "second", Term.A("ignored"), improper);
            Term failure = Nil.Value;
            Term location = Nil.Value;
            try
            {
                await image.Invoke(context, "run", Nil.Value);
            }
            catch (ErlangException error)
            {
                failure = error.Reason;
                if (error.StackTraceTerm is Cons { Head: TupleTerm { Items: [_, _, _, var frameInfo] } })
                {
                    location = frameInfo;
                }
            }

            actual = Term.Tuple(success, failure, location);

            return actual;
        });
        Term exitReason = await process.Completion;
        if (actual is not TupleTerm { Items: [var value, var reason, var info] }
            || !value.Equals(improper) || !reason.Equals(Term.Tuple(Term.A("badmatch"), Nil.Value))
            || !Cons.Items(info).Contains(Term.Tuple(Term.A("file"), Term.String("grammar.yrl")))
            || !Cons.Items(info).Contains(Term.Tuple(Term.A("line"), Term.I(102))))
        {
            throw new InvalidOperationException($"Inline list reduction returned {actual}, exit {exitReason}.");
        }
    }

    private static async Task CompareMultipleTupleClosures()
    {
        const string module = "beam_multiple_tuple_fun_probe";
        const string file = module + ".erl";
        byte[] bytes = CodeGeneration.CompileBeamModule(Encoding.UTF8.GetString(Read(file)), file);
        await using var runtime = new ProcessRuntime();
        using PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes);
        runtime.Modules.PublishPreparedBeam(image);
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            var pair = (TupleTerm)await runtime.Modules.Call(context, module, "make", [Term.A("left"), Term.A("right")]);
            var shared = (TupleTerm)await runtime.Modules.Call(context, module, "shared", [Term.A("value")]);
            var mixed = (TupleTerm)await runtime.Modules.Call(context, module, "mixed", [Term.A("mixed")]);
            actual = Term.Tuple(
                await ((FunctionTerm)pair.Items[0]).Invoke(context, []),
                await ((FunctionTerm)pair.Items[1]).Invoke(context, []),
                pair.Items[2],
                await ((FunctionTerm)shared.Items[0]).Invoke(context, []),
                await ((FunctionTerm)shared.Items[1]).Invoke(context, []),
                await ((FunctionTerm)mixed.Items[0]).Invoke(context, [Term.I(2000)]),
                await ((FunctionTerm)mixed.Items[1]).Invoke(context, [])
            );

            return actual;
        });
        Term exitReason = await process.Completion;
        Term expected = Term.Tuple(
            Term.A("right"),
            Term.A("left"),
            Term.A("right"),
            Term.Tuple(Term.A("first"), Term.A("value")),
            Term.Tuple(Term.A("second"), Term.A("value")),
            Term.A("mixed"),
            Term.A("mixed")
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Multiple BEAM closures returned {actual}, exit {exitReason}.");
        }
    }

    private static async Task CompareTupleClosureInvocation()
    {
        const string module = "beam_tuple_fun_probe";
        const string file = module + ".erl";
        byte[] bytes = CodeGeneration.CompileBeamModule(Encoding.UTF8.GetString(Read(file)), file);
        await using var runtime = new ProcessRuntime();
        using PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes);
        runtime.Modules.PublishPreparedBeam(image);
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            var tagged = (TupleTerm)await runtime.Modules.Call(context, module, "make", [Term.I(7)]);
            var recursive = (FunctionTerm)tagged.Items[1];
            var retained = (TupleTerm)await runtime.Modules.Call(context, module, "keep", [Term.A("first"), Term.A("captured")]);
            var closure = (FunctionTerm)retained.Items[1];
            actual = Term.Tuple(
                tagged.Items[0],
                await recursive.Invoke(context, [Term.I(2000), Term.I(0)]),
                retained.Items[0],
                await closure.Invoke(context, []),
                retained.Items[2]
            );

            return actual;
        });
        Term exitReason = await process.Completion;
        Term expected = Term.Tuple(Term.A("ok"), Term.I(2001007), Term.A("first"), Term.A("captured"), Term.A("captured"));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Tuple BEAM closure returned {actual}, exit {exitReason}.");
        }
    }

    private static async Task CompareAccumulatorClosureInvocation()
    {
        const string module = "beam_accumulator_fun_probe";
        const string file = module + ".erl";
        byte[] bytes = CodeGeneration.CompileBeamModule(Encoding.UTF8.GetString(Read(file)), file);
        await using var runtime = new ProcessRuntime();
        using PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes);
        runtime.Modules.PublishPreparedBeam(image);
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            var accumulator = (FunctionTerm)await runtime.Modules.Call(context, module, "make", [Term.I(7)]);
            Term value = await accumulator.Invoke(context, [Term.I(2000), Term.I(0)]);
            Term zero = await accumulator.Invoke(context, [Term.I(0), Term.I(10)]);
            Term failure = Nil.Value;
            try
            {
                await accumulator.Invoke(context, [Term.I(1), Term.A("invalid")]);
            }
            catch (ErlangException error)
            {
                failure = error.Reason;
            }

            actual = Term.Tuple(value, zero, failure);

            return actual;
        });
        Term exitReason = await process.Completion;
        if (!Term.Tuple(Term.I(2001007), Term.I(17), Term.A("badarith")).Equals(actual))
        {
            throw new InvalidOperationException($"Accumulator BEAM closure returned {actual}, exit {exitReason}.");
        }
    }

    private static async Task CompareRecursiveClosureInvocation()
    {
        const string module = "beam_recursive_fun_probe";
        const string file = module + ".erl";
        byte[] bytes = CodeGeneration.CompileBeamModule(Encoding.UTF8.GetString(Read(file)), file);
        await using var runtime = new ProcessRuntime();
        using PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes);
        runtime.Modules.PublishPreparedBeam(image);
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            var countdown = (FunctionTerm)await runtime.Modules.Call(context, module, "countdown", []);
            var captured = (FunctionTerm)await runtime.Modules.Call(context, module, "captured", [Term.A("value")]);
            Term done = await countdown.Invoke(context, [Term.I(2000)]);
            Term value = await captured.Invoke(context, [Term.I(2000)]);
            Term failure = Nil.Value;
            try
            {
                await captured.Invoke(context, [Term.A("invalid")]);
            }
            catch (ErlangException error)
            {
                failure = error.Reason;
            }

            actual = Term.Tuple(done, value, failure);

            return actual;
        });
        Term exitReason = await process.Completion;
        if (!Term.Tuple(Term.A("done"), Term.A("value"), Term.A("badarith")).Equals(actual))
        {
            throw new InvalidOperationException($"Recursive BEAM closure returned {actual}, exit {exitReason}.");
        }
    }

    private static async Task CompareLocalClosureInvocation()
    {
        const string module = "beam_local_fun_probe";
        const string file = module + ".erl";
        byte[] bytes = CodeGeneration.CompileBeamModule(Encoding.UTF8.GetString(Read(file)), file);
        await using var runtime = new ProcessRuntime();
        using PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes);
        const string multiModule = "beam_multi_capture_probe";
        const string multiFile = multiModule + ".erl";
        byte[] multiBytes = CodeGeneration.CompileBeamModule(Encoding.UTF8.GetString(Read(multiFile)), multiFile);
        using PreparedBeamImage multiImage = runtime.Modules.PrepareBeam(multiBytes);
        runtime.Modules.PublishPreparedBeam(image, multiImage);
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            var constant = (FunctionTerm)await runtime.Modules.Call(context, module, "constant", []);
            var captured = (FunctionTerm)await runtime.Modules.Call(context, module, "captured", [Term.A("value")]);
            var pair = (FunctionTerm)await runtime.Modules.Call(
                context,
                multiModule,
                "pair",
                [Term.A("zebra"), Term.A("unused"), Term.A("alpha")]
            );
            actual = Term.Tuple(
                await constant.Invoke(context, []),
                await captured.Invoke(context, []),
                Term.I(constant.Environment.Count),
                Term.I(captured.Environment.Count),
                await pair.Invoke(context, [Term.A("argument")]),
                Term.List(pair.Environment.ToArray())
            );

            return actual;
        });
        Term exitReason = await process.Completion;
        Term expected = Term.Tuple(
            Term.A("done"),
            Term.A("value"),
            Term.I(0),
            Term.I(1),
            Term.Tuple(Term.A("alpha"), Term.A("argument"), Term.A("zebra"), Term.A("alpha")),
            Term.List(Term.A("zebra"), Term.A("alpha"))
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Local BEAM closure returned {actual}, exit {exitReason}.");
        }
    }

    private static async Task CompareDynamicFunctionInvocation()
    {
        const string file = "beam_dynamic_fun_probe.erl";
        byte[] bytes = CodeGeneration.CompileBeamModule(Encoding.UTF8.GetString(Read(file)), file);
        BeamModule image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            var converter = (FunctionTerm)await image.Invoke(
                context,
                "make",
                Term.A("erlang"),
                Term.A("atom_to_list"),
                Term.I(1)
            );
            Term converted = await converter.Invoke(context, [Term.A("probe")]);
            Term invalid = Nil.Value;
            try
            {
                await image.Invoke(context, "arity_only", Term.A("invalid"));
            }
            catch (ErlangException error)
            {
                invalid = error.Reason;
            }

            actual = Term.Tuple(converted, invalid);

            return actual;
        });
        Term exitReason = await process.Completion;
        if (!Term.Tuple(Term.String("probe"), Term.A("badarg")).Equals(actual))
        {
            throw new InvalidOperationException($"Dynamic external BEAM function returned {actual}, exit {exitReason}.");
        }
    }

    private static async Task CompareCallResultComparison()
    {
        const string source = """
            -module(beam_call_comparison_probe).
            -export([same/2, different/2, literal/1, case_same/2, case_different/2, case_fresh/1]).
            same(Input, Expected) -> helper(Input) =:= Expected.
            different(Expected, Input) -> Expected =/= helper(Input).
            literal(Input) -> helper(Input) =:= ok.
            case_same(Input, Expected) ->
                case helper(Input) of Expected -> true; _ -> false end.
            case_different(Input, Expected) ->
                case helper(Input) of Expected -> false; _ -> true end.
            case_fresh(Input) ->
                case helper(Input) of _Fresh -> true; _ -> false end.
            helper(Value) -> Value.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_call_comparison_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term one = Term.I(1);
            Term floatOne = new FloatTerm(1);
            Term same = await image.Invoke(context, "same", one, one);
            Term distinctTypes = await image.Invoke(context, "same", one, floatOne);
            Term different = await image.Invoke(context, "different", floatOne, one);
            Term literal = await image.Invoke(context, "literal", Term.A("ok"));
            Term caseSame = await image.Invoke(context, "case_same", one, one);
            Term caseDistinctTypes = await image.Invoke(context, "case_same", one, floatOne);
            Term caseDifferent = await image.Invoke(context, "case_different", one, floatOne);
            Term caseFresh = await image.Invoke(context, "case_fresh", one);
            actual = Term.Tuple(
                same,
                distinctTypes,
                different,
                literal,
                caseSame,
                caseDistinctTypes,
                caseDifferent,
                caseFresh
            );

            return actual;
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(
            Term.A(ErlangBooleanAtoms.True),
            Term.A(ErlangBooleanAtoms.False),
            Term.A(ErlangBooleanAtoms.True),
            Term.A(ErlangBooleanAtoms.True),
            Term.A(ErlangBooleanAtoms.True),
            Term.A(ErlangBooleanAtoms.False),
            Term.A(ErlangBooleanAtoms.True),
            Term.A(ErlangBooleanAtoms.True)
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM call result comparisons returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareTupleTailArgument()
    {
        const string source = """
            -module(beam_tuple_tail_argument_probe).
            -export([run/3, member/3, forward/3]).
            run(Ref, State, Module) ->
                Error = {undefined_import, helper(Ref), Module},
                finish(State, Ref, Error).
            member(Source, Key, Value) ->
                List = read(Source),
                lists:member({Key, Value}, List).
            forward(Source, Key, Value) ->
                List = read(Source),
                finish({Key, Value}, List).
            read(Source) -> Source.
            finish(Key, Value) -> {Key, Value}.
            helper(Ref) -> {name, Ref}.
            finish(State, Ref, Error) -> {State, Ref, Error}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_tuple_tail_argument_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term reference = Term.A("ref");
        Term state = Term.A("state");
        Term module = Term.A("sample_module");
        Term expected = Term.Tuple(
            state,
            reference,
            Term.Tuple(Term.A("undefined_import"), Term.Tuple(Term.A("name"), reference), module)
        );
        Term pair = Term.Tuple(reference, state);
        Term values = Term.List(pair);
        expected = Term.Tuple(expected, Term.A("true"), Term.Tuple(pair, values));
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term original = await image.Invoke(context, "run", reference, state, module);
            Term found = await image.Invoke(context, "member", values, reference, state);
            Term forwarded = await image.Invoke(context, "forward", values, reference, state);
            actual = Term.Tuple(original, found, forwarded);

            return actual;
        });
        Term exit = await process.Completion;
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM tuple tail argument returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareConsCallMatch()
    {
        const string source = """
            -module(beam_cons_call_match_probe).
            -export([run/1, loop/1]).
            run([H|T]) -> true = valid(H), loop(T).
            loop([]) -> ok;
            loop([H|T]) -> true = valid(H), loop(T).
            valid(Value) -> Value =:= ok.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_cons_call_match_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term ok = Term.A("ok");
            Term success = await image.Invoke(context, "run", Term.List(ok, ok));
            Term empty = await image.Invoke(context, "loop", Nil.Value);
            Term mismatch = await ReadFailure("run", Term.List(Term.A("wrong")));
            Term badInput = await ReadFailure("loop", new Cons(ok, ok));
            actual = Term.Tuple(success, empty, mismatch, badInput);

            return actual;

            async Task<Term> ReadFailure(string function, Term input)
            {
                try
                {
                    return await image.Invoke(context, function, input);
                }
                catch (ErlangException error)
                {
                    return error.Reason;
                }
            }
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(
            Term.A("ok"),
            Term.A("ok"),
            Term.Tuple(Term.A("badmatch"), Term.A(ErlangBooleanAtoms.False)),
            Term.A("function_clause")
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM cons call match returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareMixedListNilFallback()
    {
        const string source = """
            -module(beam_mixed_nil_probe).
            -export([scan/2]).
            scan(Acc, [{skip,_}|Rest]) -> scan(Acc, Rest);
            scan(Acc, [skip|Rest]) -> scan(Acc, Rest);
            scan(Acc, [Value|Rest]) -> scan([Value|Acc], Rest);
            scan([], []) -> [];
            scan(Acc, []) -> Acc.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_mixed_nil_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? failure = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            try
            {
                await image.Invoke(
                    context,
                    "scan",
                    Term.List(Term.A("first")),
                    Term.A("improper")
                );
            }
            catch (ErlangException error)
            {
                failure = error.Reason;
            }

            return Nil.Value;
        });
        Term exit = await process.Completion;
        if (!Term.A("function_clause").Equals(failure))
        {
            throw new InvalidOperationException($"The mixed list fallback returned failure {failure}, exit {exit}.");
        }
    }

    private static async Task CompareCaseTupleSlot()
    {
        const string file = "beam_case_tuple_slot_probe.erl";
        string source = Encoding.UTF8.GetString(Read(file));
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, file));
        const string selectorFile = "beam_case_register_selector_probe.erl";
        string selectorSource = Encoding.UTF8.GetString(Read(selectorFile));
        BeamModule selector = BeamModule.Prepare(CodeGeneration.CompileBeamModule(selectorSource, selectorFile));
        await using var runtime = new ProcessRuntime();
        Term payload = Term.A("payload");
        Term rule = Term.Tuple(Term.A("rule"), payload);
        Term state = Term.Tuple(Term.A("state"), payload, rule);
        Term key = Term.A("key");
        Term entry = Term.A("entry");
        Term fallback = Term.A("fallback");
        Term invalid = Term.Tuple(entry, Term.A("other"), payload);
        Term errorTuple = Term.Tuple(Term.A("error"), payload);
        Term? actual = null;
        Exception? invocationError = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            try
            {
                Term success = await image.Invoke(context, "run", Term.I(1), payload);
                Term failure = await image.Invoke(context, "run", Term.I(2), payload);
                Term retained = await image.Invoke(context, "retain", Term.I(1), payload);
                Term selected = await selector.Invoke(context, "run", Term.Tuple(entry, key, payload), key, payload);
                Term alternative = await selector.Invoke(context, "run", Term.Tuple(entry, fallback, payload), key, payload);
                Term aliased = await selector.Invoke(context, "run", errorTuple, key, payload);
                Term tail = await selector.Invoke(context, "tail", Term.Tuple(entry, key, payload), key, payload);
                Term tailMiss = await selector.Invoke(context, "tail", invalid, key, payload);
                Term continued = await selector.Invoke(context, "continue_after", Term.Tuple(entry, key, payload), key, payload);
                Term grouped = await selector.Invoke(context, "arity_fallback", Term.Tuple(entry, fallback), fallback, payload);
                Term tagged = await selector.Invoke(
                    context,
                    "arity_fallback",
                    Term.Tuple(Term.A("other_tag"), payload),
                    key,
                    payload
                );
                Term missed = await selector.Invoke(context, "arity_fallback", Term.Tuple(entry, payload), key, payload);
                Term ignored = Term.Tuple(Term.A("ignored"), key, payload);
                Term copy = Term.Tuple(
                    Term.A("copy"),
                    key,
                    payload,
                    Term.A("unused")
                );
                Term retainedAlias = await selector.Invoke(
                    context,
                    "projection",
                    ignored,
                    key,
                    payload
                );
                Term copiedField = await selector.Invoke(
                    context,
                    "projection",
                    copy,
                    key,
                    payload
                );
                Term repeatedMismatch = Term.Tuple(Term.A("repeat"), key, payload);
                Term projectionFallback = await selector.Invoke(
                    context,
                    "projection",
                    repeatedMismatch,
                    key,
                    payload
                );
                Term binary = new BitString("guard"u8);
                Term directGuard = await selector.Invoke(context, "guard_route", binary, payload);
                Term rejectedGuard = await selector.Invoke(context, "guard_route", Nil.Value, payload);
                Term failedLeft = await selector.Invoke(context, "guard_failure", payload, binary);
                Term nested = Term.Tuple(Term.A("outer"), Term.Tuple(Term.A("inner"), key, payload));
                Term nestedMatch = await selector.Invoke(context, "nested_tagged", nested, key);
                Term nestedFallback = await selector.Invoke(context, "nested_tagged", nested, payload);
                Term fresh = Term.Tuple(
                    Term.A("outer"),
                    Term.Tuple(Term.A("inner"), Term.Tuple(Term.A("leaf"), key), payload),
                    payload
                );
                Term freshMatch = await selector.Invoke(context, "fresh_nested", fresh, key);
                Term argument = Term.Tuple(Term.A("tagged"), rule, payload);
                Term packedArgument = await selector.Invoke(
                    context,
                    "argument_case",
                    key,
                    argument,
                    fallback
                );
                Term guarded = Term.Tuple(Term.A("guarded"), key);
                Term named = await selector.Invoke(context, "guarded_case", guarded, Term.I(5));
                Term tooSmall = Term.Tuple(Term.A("guarded"), Term.I(4));
                Term original = await selector.Invoke(context, "guarded_case", tooSmall, Term.I(5));
                Term record = Term.Tuple(Term.A("parity_record"), key, payload);
                Term recordUpdate = await selector.Invoke(context, "update_one", record, fallback);
                Term unchangedRecord = await selector.Invoke(context, "update_one", record, key);
                Term checkedRecord = await selector.Invoke(context, "read_checked", record);
                Term rejectedRecord = Nil.Value;
                try
                {
                    await selector.Invoke(context, "record_case", payload);
                }
                catch (ErlangException error)
                {
                    rejectedRecord = error.Reason;
                }

                Term recordArgs = await selector.Invoke(context, "record_args", record);
                Term recordSelect = await selector.Invoke(context, "record_select", record);
                Term finalField = await selector.Invoke(context, "final_field", fallback, record);
                Term invalidFinalField = Nil.Value;
                try
                {
                    await selector.Invoke(context, "final_field", fallback, payload);
                }
                catch (ErlangException error)
                {
                    invalidFinalField = error.Reason;
                }
                Term checkedFields = await selector.Invoke(
                    context,
                    "checked_fields",
                    record,
                    record
                );
                var fieldErrors = new List<Term>();
                foreach (var (left, right) in new[] { (fallback, payload), (record, payload) })
                {
                    try
                    {
                        await selector.Invoke(
                            context,
                            "checked_fields",
                            left,
                            right
                        );
                    }
                    catch (ErlangException error)
                    {
                        fieldErrors.Add(error.Reason);
                    }
                }

                Term comprehension = await selector.Invoke(
                    context,
                    "map_after_call",
                    Term.List(key, payload),
                    fallback
                );
                Term emptyComprehension = await selector.Invoke(
                    context,
                    "map_after_call",
                    Nil.Value,
                    fallback
                );
                Term caughtComprehension = await selector.Invoke(
                    context,
                    "map_caught",
                    Term.List(key, Term.A("invalid")),
                    fallback
                );
                Term badGenerator = Nil.Value;
                try
                {
                    await selector.Invoke(
                        context,
                        "map_after_call",
                        new Cons(key, payload),
                        fallback
                    );
                }
                catch (ErlangException error)
                {
                    badGenerator = error.Reason;
                }

                Term tupleSource = Term.List(
                    Term.Tuple(Term.A("field"), key),
                    Term.Tuple(Term.A("other"), fallback),
                    Term.Tuple(Term.A("field"), payload),
                    Term.Tuple(Term.A("field"), key, fallback),
                    fallback
                );
                Term taggedFields = await selector.Invoke(context, "tuple_fields", tupleSource);
                Term plainFields = await selector.Invoke(context, "tuple_field_bound", tupleSource);
                Term malformedFields = Nil.Value;
                try
                {
                    await selector.Invoke(context, "tuple_fields", new Cons(fallback, key));
                }
                catch (ErlangException error)
                {
                    malformedFields = error.Reason;
                }

                Term mapInput = Term.List(key, payload);
                Term linearMap = await selector.Invoke(context, "map_linear", mapInput);
                Term nestedMap = await selector.Invoke(context, "map_nested", mapInput);
                Term mapFailure = Nil.Value;
                try
                {
                    await selector.Invoke(context, "map_linear", new Cons(Term.A("invalid"), key));
                }
                catch (ErlangException error)
                {
                    mapFailure = error.Reason;
                }

                Term filtered = await selector.Invoke(context, "filter_known", mapInput);
                Term negatedFilter = await selector.Invoke(context, "filter_negated", mapInput);
                Term mappedFilter = await selector.Invoke(context, "filter_mapped", mapInput);
                var filterErrors = new List<Term>();
                foreach (string function in new[] { "filter_general", "filter_general_not" })
                {
                    try
                    {
                        await selector.Invoke(context, function, Term.List(payload));
                    }
                    catch (ErlangException error)
                    {
                        filterErrors.Add(error.Reason);
                    }
                }

                Term binaryTable = await selector.Invoke(context, "dispatch_binary", Term.A("beta"));
                Term binaryFallback = await selector.Invoke(context, "dispatch_binary", new BitString("key"u8));
                Term computedFallback = await selector.Invoke(context, "dispatch_computed", payload);
                Term literalCaseHit = await selector.Invoke(context, "literal_case_guard", Term.A("beta"));
                Term literalCaseIdentity = await selector.Invoke(context, "literal_case_guard", Term.I(7));
                Term literalFunctionMiss = await selector.Invoke(context, "literal_function_guard", Term.A("unknown"));
                Term atomGuardDefault = await selector.Invoke(context, "atom_guard_table", Term.A("default"));
                Term atomGuardHit = await selector.Invoke(context, "atom_guard_table", Term.A("gamma"));
                Term atomGuardPriority = await selector.Invoke(context, "atom_guard_table", Term.A("alpha"));
                Term atomGuardIf = await selector.Invoke(context, "atom_guard_if", Term.A("delta"));
                Term outerCaseIdentity = await selector.Invoke(context, "literal_case_outer", Term.I(7));
                Term dispatchRecord = Term.Tuple(Term.A("parity_record"), key, payload);
                Term recordDispatch = await selector.Invoke(
                    context,
                    "record_dispatch",
                    Term.A("gamma"),
                    dispatchRecord
                );
                Term recordDispatchTail = await selector.Invoke(
                    context,
                    "record_dispatch_tail",
                    Term.A("beta"),
                    dispatchRecord
                );
                Term recordDispatchFailure = Nil.Value;
                try
                {
                    await selector.Invoke(context, "record_dispatch_tail", Term.A("alpha"), payload);
                }
                catch (ErlangException error)
                {
                    recordDispatchFailure = error.Reason;
                }

                var literalFailures = new List<Term>();
                foreach (string function in new[] { "literal_case_guard", "literal_case_partial", "literal_function_error" })
                {
                    try
                    {
                        await selector.Invoke(context, function, Term.A("unknown"));
                    }
                    catch (ErlangException error)
                    {
                        literalFailures.Add(error.Reason);
                    }
                }

                Term entryTailMatch = await selector.Invoke(
                    context,
                    "dispatch_entry_tail",
                    key,
                    Term.A("beta"),
                    payload
                );
                Term entryTailMiss = await selector.Invoke(
                    context,
                    "dispatch_entry_tail",
                    key,
                    Term.I(7),
                    payload
                );
                Term groupedArity = await selector.Invoke(
                    context,
                    "grouped_arity",
                    Term.A("beta"),
                    Term.I(2)
                );
                Term groupedArityMiss = await selector.Invoke(
                    context,
                    "grouped_arity",
                    Term.A("gamma"),
                    Term.I(2)
                );
                Term groupedArityTail = await selector.Invoke(
                    context,
                    "grouped_arity_tail",
                    Term.A("delta"),
                    Term.I(3)
                );
                Term groupedArityClass = await selector.Invoke(
                    context,
                    "grouped_arity_class",
                    Term.A("delta"),
                    Term.I(2)
                );
                Term groupedArityDefault = await selector.Invoke(
                    context,
                    "grouped_arity_default",
                    Term.A("beta"),
                    Term.A("wrong_arity")
                );
                Term groupedArityFailure = Nil.Value;
                try
                {
                    await selector.Invoke(
                        context,
                        "grouped_arity",
                        Term.A("beta"),
                        Term.A("wrong_arity")
                    );
                }
                catch (ErlangException error)
                {
                    groupedArityFailure = error.Reason;
                }

                Term binaryFailure = Nil.Value;
                try
                {
                    await selector.Invoke(context, "dispatch_binary", Term.I(7));
                }
                catch (ErlangException error)
                {
                    binaryFailure = error.Reason;
                }

                Term thrownFallback = Nil.Value;
                try
                {
                    await selector.Invoke(context, "dispatch_throw", payload);
                }
                catch (ErlangException error)
                {
                    thrownFallback = error.Reason;
                }

                Term plainInput = Term.Tuple(Term.A("tagged"), Term.Tuple(key, payload));
                Term plainMatch = await selector.Invoke(context, "plain_case", plainInput, Term.A("tagged"));
                Term plainAlternate = await selector.Invoke(context, "plain_case", plainInput, key);
                Term repeatedMismatchFields = Term.Tuple(
                    Term.A("same"),
                    key,
                    payload,
                    fallback
                );
                Term retainedMatchCheck = await selector.Invoke(context, "retain_fields", repeatedMismatchFields, key);
                Term keptExport = await selector.Invoke(context, "retain_export", Term.Tuple(Term.A("picked"), key, payload));
                Term rejected = Nil.Value;
                try
                {
                    await selector.Invoke(context, "run", invalid, key, payload);
                }
                catch (ErlangException error)
                {
                    rejected = error.Reason;
                }

                actual = Term.Tuple(
                    success,
                    failure,
                    retained,
                    selected,
                    alternative,
                    aliased,
                    tail,
                    tailMiss,
                    continued,
                    grouped,
                    tagged,
                    missed,
                    retainedAlias,
                    copiedField,
                    projectionFallback,
                    directGuard,
                    rejectedGuard,
                    failedLeft,
                    nestedMatch,
                    nestedFallback,
                    freshMatch,
                    packedArgument,
                    named,
                    original,
                    recordUpdate,
                    unchangedRecord,
                    checkedRecord,
                    rejectedRecord,
                    recordArgs,
                    recordSelect,
                    finalField,
                    invalidFinalField,
                    checkedFields,
                    Term.List(fieldErrors.ToArray()),
                    comprehension,
                    emptyComprehension,
                    caughtComprehension,
                    badGenerator,
                    taggedFields,
                    plainFields,
                    malformedFields,
                    linearMap,
                    nestedMap,
                    mapFailure,
                    filtered,
                    negatedFilter,
                    mappedFilter,
                    Term.List(filterErrors.ToArray()),
                    binaryTable,
                    binaryFallback,
                    computedFallback,
                    literalCaseHit,
                    literalCaseIdentity,
                    literalFunctionMiss,
                    atomGuardDefault,
                    atomGuardHit,
                    atomGuardPriority,
                    atomGuardIf,
                    outerCaseIdentity,
                    recordDispatch,
                    recordDispatchTail,
                    recordDispatchFailure,
                    Term.List(literalFailures.ToArray()),
                    entryTailMatch,
                    entryTailMiss,
                    groupedArity,
                    groupedArityMiss,
                    groupedArityTail,
                    groupedArityClass,
                    groupedArityDefault,
                    groupedArityFailure,
                    binaryFailure,
                    thrownFallback,
                    plainMatch,
                    plainAlternate,
                    retainedMatchCheck,
                    keptExport,
                    rejected
                );
            }
            catch (Exception error)
            {
                invocationError = error;
            }

            return Nil.Value;
        });
        Term exit = await process.Completion;
        Term prepared = Term.Tuple(Term.A("state"), payload);
        Term expected = Term.Tuple(
            state,
            Term.A("no"),
            Term.Tuple(state, rule),
            Term.Tuple(Term.A("match"), prepared, payload),
            Term.Tuple(fallback, prepared, payload),
            errorTuple,
            Term.Tuple(payload, payload, key),
            Term.A("no"),
            Term.Tuple(Term.A("continued"), Term.Tuple(payload, payload, key)),
            Term.Tuple(Term.A("match"), payload),
            Term.A("tagged"),
            Term.Tuple(Term.A("other"), Term.Tuple(entry, payload)),
            Term.Tuple(Term.A("retained"), Term.Tuple(Term.A("ignored"), key, payload)),
            Term.Tuple(Term.A("value"), payload, payload),
            Term.Tuple(Term.A("other"), Term.Tuple(Term.A("repeat"), key, payload)),
            Term.A("accepted"),
            Term.A("rejected"),
            Term.A("rejected"),
            Term.Tuple(Term.A("ok"), payload),
            Term.Tuple(Term.A("other"), Term.Tuple(Term.A("outer"), Term.Tuple(Term.A("inner"), key, payload))),
            Term.Tuple(Term.A("ok"), payload),
            Term.Tuple(
                Term.Tuple(key, Term.Tuple(Term.A("ok"), payload), fallback),
                Term.List(fallback, Term.Tuple(Term.A("tagged"), rule, payload), key)
            ),
            Term.Tuple(Term.A("named"), key),
            Term.Tuple(Term.A("other"), Term.Tuple(Term.A("guarded"), Term.I(4))),
            Term.Tuple(
                Term.Tuple(Term.A("parity_record"), key, payload),
                Term.Tuple(Term.A("parity_record"), fallback, payload)
            ),
            Term.Tuple(
                Term.Tuple(Term.A("parity_record"), key, payload),
                Term.Tuple(Term.A("parity_record"), key, payload)
            ),
            Term.Tuple(key, Term.Tuple(Term.A("parity_record"), key, Term.A("updated"))),
            Term.Tuple(Term.A("badrecord"), payload),
            Term.Tuple(key, payload, key),
            Term.Tuple(key, payload),
            Term.Tuple(fallback, payload, Term.A("marker")),
            Term.Tuple(Term.A("badrecord"), payload),
            Term.Tuple(key, Term.A("marker"), payload),
            Term.List(Term.Tuple(Term.A("badrecord"), fallback), Term.Tuple(Term.A("badrecord"), payload)),
            Term.List(Term.Tuple(fallback, key), Term.Tuple(fallback, payload)),
            Nil.Value,
            Term.List(Term.Tuple(fallback, key), Term.Tuple(Term.A("recovered"), fallback)),
            Term.Tuple(Term.A("bad_generator"), payload),
            Term.List(key, payload),
            Term.Tuple(Term.A("mapped"), Term.List(key, fallback, payload)),
            Term.Tuple(Term.A("bad_generator"), key),
            Term.List(
                Term.Tuple(Term.A("mapped"), Term.Tuple(key, Term.A("suffix"))),
                Term.Tuple(Term.A("mapped"), Term.Tuple(payload, Term.A("suffix")))
            ),
            Term.List(
                Term.Tuple(Term.A("mapped"), Term.Tuple(key, Term.A("suffix"))),
                Term.Tuple(Term.A("mapped"), Term.Tuple(payload, Term.A("suffix")))
            ),
            Term.A("invalid_item"),
            Term.List(key),
            Term.List(payload),
            Term.List(Term.Tuple(Term.A("mapped"), key)),
            Term.List(Term.Tuple(Term.A("bad_filter"), payload), Term.A("badarg")),
            new BitString("one"u8),
            new BitString("prefix:key"u8),
            Term.Tuple(Term.A("mapped"), payload),
            Term.I(1),
            Term.I(7),
            Term.A("false"),
            Term.I(0),
            Term.I(2),
            Term.I(1),
            Term.A("second"),
            Term.I(7),
            key,
            Term.Tuple(Term.String("beta"), payload),
            Term.Tuple(Term.A("badrecord"), payload),
            Term.List(
                Term.Tuple(Term.A("case_clause"), Term.A("unknown")),
                Term.Tuple(Term.A("case_clause"), Term.A("unknown")),
                Term.A("badarg")
            ),
            Term.Tuple(key, Term.I(1), payload),
            Term.Tuple(Term.A("missing"), Term.I(7), payload),
            Term.A("true"),
            Term.A("false"),
            Term.A("true"),
            Term.A("second"),
            Term.A("false"),
            Term.A("function_clause"),
            Term.A("badarg"),
            Term.Tuple(Term.A("error"), Term.Tuple(Term.A("bad"), payload)),
            Term.Tuple(Term.A("nested"), key, payload),
            Term.Tuple(Term.A("tagged"), Term.Tuple(key, payload)),
            Term.Tuple(Term.A("other"), Term.Tuple(Term.A("same"), key, payload, fallback)),
            Term.Tuple(Term.A("exported"), key),
            Term.Tuple(Term.A("case_clause"), invalid)
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"The case tuple slot checks returned {actual}, exit {exit}.", invocationError);
        }
    }

    private static async Task CompareThrowStackObservers()
    {
        const string file = "beam_throw_stack_guard_probe.erl";
        string source = Encoding.UTF8.GetString(Read(file));
        BeamModule image = BeamModule.Prepare(
            CodeGeneration.CompileBeamModule(source, file),
            new BeamDependencySelection([ListBeamHostNames.Module])
        );
        const string computedFile = "beam_computed_throw_probe.erl";
        string computedSource = Encoding.UTF8.GetString(Read(computedFile));
        BeamModule computed = BeamModule.Prepare(CodeGeneration.CompileBeamModule(computedSource, computedFile));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        Exception? invocationError = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            try
            {
                Term handled = await image.Invoke(context, "safe", Nil.Value);
                Term observed = await image.Invoke(context, "observed", Nil.Value);
                Term mismatch = await image.Invoke(context, "mismatch");
                Term escaped = await image.Invoke(context, "escaped");
                Term switched = await computed.Invoke(context, "tuple_switch", Term.I(42));
                actual = Term.Tuple(
                    handled,
                    observed,
                    mismatch,
                    escaped,
                    await computed.Invoke(context, "caught", Term.I(42)),
                    await computed.Invoke(context, "observed", Term.I(-7)),
                    switched
                );
            }
            catch (Exception error)
            {
                invocationError = error;
            }

            return Nil.Value;
        });
        Term exit = await process.Completion;
        Term yes = Term.A(ErlangBooleanAtoms.True);
        Term expected = Term.Tuple(
            Term.A("handled"),
            yes,
            yes,
            yes,
            Term.String("42"),
            Term.Tuple(Term.String("-7"), yes),
            Term.Tuple(Term.A("first"), Term.String("42"))
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"The throw stack checks returned {actual}, exit {exit}.", invocationError);
        }
    }

    private static async Task CompareLiveListBranch()
    {
        const string source = """
            -module(beam_live_list_branch).
            -export([run/2]).
            run([$=|Rest], Acc) -> {reverse(Acc, []), Rest};
            run([Char|Rest], Acc) -> run(Rest, [Char|Acc]);
            run([], Acc) -> reverse(Acc, []).
            reverse([Head|Tail], Acc) -> reverse(Tail, [Head|Acc]);
            reverse([], Acc) -> Acc.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_live_list_branch.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term split = await image.Invoke(context, "run", Term.String("key=value"), Nil.Value);
            Term noSeparator = await image.Invoke(context, "run", Term.String("key"), Nil.Value);
            actual = Term.Tuple(split, noSeparator);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(Term.Tuple(Term.String("key"), Term.String("value")), Term.String("key"));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Live list branch returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareEntryAtomSuffix()
    {
        const string source = """
            -module(beam_entry_atom_suffix).
            -export([run/3]).
            run(_, {missing, _}, _) -> missing;
            run(raw, {Value, _}, _) -> {ok, Value};
            run(convert, {Value, Options}, Extra) ->
                Prepared = prepare(Options, Extra),
                {Value, Prepared};
            run(_, _, _) -> fallback.
            prepare(Options, Extra) -> {Options, Extra}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_entry_atom_suffix.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term raw = Term.A("raw");
            Term extra = Term.A("extra");
            Term missing = Term.A("missing");
            Term options = Term.A("options");
            Term value = Term.A("value");
            Term pair = Term.Tuple(value, options);
            Term priority = await image.Invoke(context, "run", raw, Term.Tuple(missing, options), extra);
            Term selected = await image.Invoke(context, "run", raw, pair, extra);
            Term converted = await image.Invoke(context, "run", Term.A("convert"), pair, extra);
            Term invalidTuple = await image.Invoke(context, "run", raw, value, extra);
            Term unknownKey = await image.Invoke(context, "run", Term.I(42), pair, extra);
            actual = Term.Tuple(priority, selected, converted, invalidTuple, unknownKey);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(
            Term.A("missing"),
            Term.Tuple(Term.A("ok"), Term.A("value")),
            Term.Tuple(Term.A("value"), Term.Tuple(Term.A("options"), Term.A("extra"))),
            Term.A("fallback"),
            Term.A("fallback")
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Entry atom suffix returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareUnusedEntryTupleFields()
    {
        const string source = """
            -module(beam_unused_entry_tuple_fields).
            -export([run/2, same/1]).
            run({missing, _Unused}, _) -> missing;
            run({item, Value, Gate, _Unused}, Other) when Gate =:= ready ->
                Prepared = prepare(Value),
                {Prepared,Other};
            run(_, _) -> fallback.
            same({same, Value, Value}) -> same;
            same(_) -> fallback.
            prepare(Value) -> {prepared,Value}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_unused_entry_tuple_fields.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            var input = Term.A("input");
            var ignored = Term.A("ignored");
            var item = Term.A("item");
            var same = Term.A("same");
            Term missing = await image.Invoke(context, "run", Term.Tuple(Term.A("missing"), ignored), input);
            Term selected = await image.Invoke(context, "run", Term.Tuple(item, input, Term.A("ready"), ignored), ignored);
            Term guardFailure = await image.Invoke(context, "run", Term.Tuple(item, input, ignored, ignored), input);
            Term repeated = await image.Invoke(context, "same", Term.Tuple(same, input, input));
            Term mismatch = await image.Invoke(context, "same", Term.Tuple(same, input, ignored));
            actual = Term.Tuple(missing, selected, guardFailure, repeated, mismatch);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(
            Term.A("missing"),
            Term.Tuple(Term.Tuple(Term.A("prepared"), Term.A("input")), Term.A("ignored")),
            Term.A("fallback"),
            Term.A("same"),
            Term.A("fallback")
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Entry tuple projection returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareEntryTaggedProjection()
    {
        const string source = """
            -module(beam_entry_tagged_projection_probe).
            -export([read/2, project/1, forward/1, reverse/1, dispatch/1, retained/2, staged/2, tag_only/2]).
            -record(entry, {key, value}).
            read(Key, Value) ->
                case fetch(Key, Value) of
                    #entry{value=V} when is_list(V) -> list_to_atom(V);
                    #entry{value=V} -> V;
                    _ -> missing
                end.
            project(Input) ->
                case identity(Input) of
                    #entry{value=V} -> V;
                    _ -> missing
                end.
            forward(Input) ->
                case identity(Input) of
                    #entry{key=K,value=V} -> pair(K,V);
                    _ -> missing
                end.
            reverse(Input) ->
                case identity(Input) of
                    #entry{key=K,value=V} -> pair(V,K);
                    _ -> missing
                end.
            dispatch(Input) ->
                case identity(Input) of
                    {left,K,V} -> pair(K,V);
                    {right,K,V} -> pair(V,K);
                    _ -> missing
                end.
            retained(Input, Saved) ->
                case identity(Input) of
                    #entry{value=V} -> pair(Saved,V);
                    Other -> Other
                end.
            staged(Input, Saved) ->
                remember(identity(Saved)),
                case identity(Input) of
                    #entry{value=V} -> pair(Saved,V);
                    Other -> Other
                end.
            tag_only(Input, Saved) ->
                remember(identity(Saved)),
                case identity(Input) of
                    #entry{value=_Unused} -> pair(Saved,accepted);
                    Other -> Other
                end.
            remember(Value) -> erlang:put(probe_value, Value).
            fetch(entry, Value) -> #entry{key=entry, value=Value};
            fetch(_, _) -> missing.
            identity(Value) -> Value.
            pair(First, Second) -> {First,Second}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_entry_tagged_projection_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term entry = Term.A("entry");
            Term value = Term.I(42);
            Term converted = await image.Invoke(context, "read", entry, Term.String("projected"));
            Term projected = await image.Invoke(context, "read", entry, value);
            Term fallback = await image.Invoke(context, "read", Term.A("wrong"), value);
            Term direct = await image.Invoke(context, "project", Term.Tuple(entry, Nil.Value, value));
            Term tagged = Term.Tuple(entry, entry, value);
            Term forward = await image.Invoke(context, "forward", tagged);
            Term reverse = await image.Invoke(context, "reverse", tagged);
            Term left = await image.Invoke(context, "dispatch", Term.Tuple(Term.A("left"), entry, value));
            Term right = await image.Invoke(context, "dispatch", Term.Tuple(Term.A("right"), entry, value));
            Term unknown = await image.Invoke(context, "dispatch", tagged);
            Term wrongArity = await image.Invoke(context, "dispatch", Term.Tuple(Term.A("left"), value));
            Term retained = await image.Invoke(context, "retained", tagged, entry);
            Term unmatched = await image.Invoke(context, "retained", value, entry);
            Term staged = await image.Invoke(context, "staged", tagged, entry);
            Term tagOnly = await image.Invoke(context, "tag_only", tagged, entry);
            Term tagMismatch = await image.Invoke(context, "tag_only", value, entry);
            actual = Term.Tuple(
                converted, projected, fallback, direct, forward, reverse,
                retained, unmatched, staged, tagOnly, tagMismatch, Term.Tuple(left, right, unknown, wrongArity)
            );

            return actual;
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(
            Term.A("projected"),
            Term.I(42),
            Term.A("missing"),
            Term.I(42),
            Term.Tuple(Term.A("entry"), Term.I(42)),
            Term.Tuple(Term.I(42), Term.A("entry")),
            Term.Tuple(Term.A("entry"), Term.I(42)),
            Term.I(42),
            Term.Tuple(Term.A("entry"), Term.I(42)),
            Term.Tuple(Term.A("entry"), Term.A("accepted")),
            Term.I(42),
            Term.Tuple(
                Term.Tuple(Term.A("entry"), Term.I(42)),
                Term.Tuple(Term.I(42), Term.A("entry")),
                Term.A("missing"),
                Term.A("missing")
            )
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM entry tagged projection returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareNestedEntryCalls()
    {
        const string source = """
            -module(beam_nested_entry_calls_probe).
            -export([run/3, chain/2, survivors/3, boxed/3, discarded/3, named/0, siblings/2, siblings_keep/2, siblings_nonzero/2, siblings_three/3]).
            run(First, Second, _Unused) ->
                finish(read(First, Second, []), tail).
            chain(First, Second) ->
                finish(tail, wrap(tag, read(First, Second, []))).
            survivors(First, Second, Third) ->
                finish(Second, wrap(Third, read(First, Second, []))).
            boxed(First, Second, Third) ->
                finish(Third, [wrap(tag, read(First, Second, []))]).
            discarded(First, Second, Third) ->
                read(First, entry, []),
                read(Second, middle, []),
                finish(Third, tail).
            named() ->
                fun(T, A) ->
                    read(T, tag, []),
                    finish(T, A)
                end.
            siblings(First, Second) ->
                finish(left(First), right(Second)).
            siblings_keep(First, Second) ->
                finish_keep(left(First), right(Second), Second).
            siblings_nonzero(First, Second) ->
                finish(left(First), finish_keep(literal, Second, tail)).
            siblings_three(First, Second, Third) ->
                finish_keep(left(First), right(Second), last(Third)).
            left(Value) -> {left, Value}.
            right(Value) -> {right, Value}.
            last(Value) -> {last, Value}.
            read(fail, _, _) -> erlang:error(probe_failure);
            read(First, Second, Empty) -> {First, Second, Empty}.
            wrap(Tag, Value) -> {Tag, Value}.
            finish(First, Second) -> {First, Second}.
            finish_keep(First, Second, Saved) -> {First, Second, Saved}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_nested_entry_calls_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term first = Term.A("one");
        Term second = Term.A("two");
        Term tail = Term.A("tail");
        Term tuple = Term.Tuple(first, second, Nil.Value);
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term simple = await image.Invoke(context, "run", first, second, tail);
            Term chain = await image.Invoke(context, "chain", first, second);
            Term survivors = await image.Invoke(context, "survivors", first, second, tail);
            Term boxed = await image.Invoke(context, "boxed", first, second, tail);
            Term discarded = await image.Invoke(context, "discarded", first, second, tail);
            var named = (FunctionTerm)await image.Invoke(context, "named");
            Term namedResult = await named.Invoke(context, [first, second]);
            Term siblings = await image.Invoke(context, "siblings", first, second);
            Term retained = await image.Invoke(context, "siblings_keep", first, second);
            Term nonzero = await image.Invoke(context, "siblings_nonzero", first, second);
            Term three = await image.Invoke(context, "siblings_three", first, second, tail);
            Term failure = Nil.Value;
            try
            {
                await image.Invoke(context, "chain", Term.A("fail"), second);
            }
            catch (ErlangException error)
            {
                failure = error.Reason;
            }

            actual = Term.Tuple(simple, chain, survivors, boxed, discarded, namedResult, failure, siblings, retained, nonzero, three);

            return actual;
        });
        Term exit = await process.Completion;
        Term siblingsExpected = Term.Tuple(Term.Tuple(Term.A("left"), first), Term.Tuple(Term.A("right"), second));
        Term expected = Term.Tuple(
            Term.Tuple(tuple, tail),
            Term.Tuple(tail, Term.Tuple(Term.A("tag"), tuple)),
            Term.Tuple(second, Term.Tuple(tail, tuple)),
            Term.Tuple(tail, Term.List(Term.Tuple(Term.A("tag"), tuple))),
            Term.Tuple(tail, tail),
            Term.Tuple(first, second),
            Term.A("probe_failure"),
            siblingsExpected,
            Term.Tuple(Term.Tuple(Term.A("left"), first), Term.Tuple(Term.A("right"), second), second),
            Term.Tuple(Term.Tuple(Term.A("left"), first), Term.Tuple(Term.A("literal"), second, tail)),
            Term.Tuple(Term.Tuple(Term.A("left"), first), Term.Tuple(Term.A("right"), second), Term.Tuple(Term.A("last"), tail))
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM nested entry calls returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareNestedInlineListCheck()
    {
        const string source = """
            -module(beam_nested_inline_list_probe).
            -export([run/1, forward/2, same/1, dead/3, dead_same/2]).
            -compile({inline, checked/1}).
            run(Value) ->
                Kept = checked(Value),
                {ok, Kept}.
            forward(Value, Other) ->
                Kept = checked(Value),
                finish(hd(Other), Kept).
            same(Value) ->
                Kept = checked(Value),
                finish(hd(Kept), Kept).
            dead(_Ignored, Value, Other) ->
                Kept = checked(Value),
                finish(hd(Other), Kept).
            dead_same(_Ignored, Value) ->
                Kept = checked(Value),
                finish(hd(Kept), Kept).
            finish(Head, Value) -> {Head, Value}.
            checked(Value) ->
                [Head|Tail] = Value,
                [begin Head end|Tail].
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_nested_inline_list_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term improper = new Cons(Term.A("head"), Term.A("tail"));
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term accepted = await image.Invoke(context, "run", improper);
            Term forwarded = await image.Invoke(context, "forward", improper, Term.List(Term.I(7)));
            Term same = await image.Invoke(context, "same", improper);
            Term dead = await image.Invoke(context, "dead", Nil.Value, improper, Term.List(Term.I(7)));
            Term deadSame = await image.Invoke(context, "dead_same", Nil.Value, improper);
            Term rejected = Nil.Value;
            Term headFailure = Nil.Value;
            try
            {
                await image.Invoke(context, "run", Nil.Value);
            }
            catch (ErlangException error)
            {
                rejected = error.Reason;
            }
            try
            {
                await image.Invoke(context, "forward", improper, Nil.Value);
            }
            catch (ErlangException error)
            {
                headFailure = error.Reason;
            }

            actual = Term.Tuple(accepted, rejected, forwarded, same, dead, deadSame, headFailure);

            return actual;
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(
            Term.Tuple(Term.A("ok"), improper),
            Term.Tuple(Term.A("badmatch"), Nil.Value),
            Term.Tuple(Term.I(7), improper),
            Term.Tuple(Term.A("head"), improper),
            Term.Tuple(Term.I(7), improper),
            Term.Tuple(Term.A("head"), improper),
            Term.A("badarg")
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM nested inline list check returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGuardedBifTail()
    {
        const string source = """
            -module(beam_guarded_bif_tail_probe).
            -export([run/2, arithmetic/2, choose/2, preserve/3]).
            run(A,B) when is_float(A) -> run(erlang:floor(A), B);
            run(A,B) when is_float(B) -> run(A, erlang:floor(B));
            run(A,B) -> {A,B}.
            arithmetic(A,B) when A >= 0, B > 0 -> A div B;
            arithmetic(A,B) when A < 0, B < 0 -> A rem B;
            arithmetic(A,B) -> {A,B}.
            choose(A,B) ->
                case A rem B of
                    0 -> A div B;
                    _ -> (A div B) - 1
                end.
            preserve(A,B,C) ->
                case B rem C of
                    0 -> A + (B div C);
                    _ -> (B div C) - A
                end.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_guarded_bif_tail_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term conversion = await image.Invoke(context, "run", new FloatTerm(-1.5), new FloatTerm(3.75));
            Term quotient = await image.Invoke(context, "arithmetic", Term.I(8), Term.I(3));
            Term remainder = await image.Invoke(context, "arithmetic", Term.I(-8), Term.I(-3));
            Term fallback = await image.Invoke(context, "arithmetic", Term.I(8), Term.I(0));
            Term oppositeSigns = await image.Invoke(context, "arithmetic", Term.I(-8), Term.I(3));
            Term exact = await image.Invoke(context, "choose", Term.I(8), Term.I(2));
            Term adjusted = await image.Invoke(context, "choose", Term.I(8), Term.I(3));
            Term preserved = await image.Invoke(context, "preserve", Term.I(10), Term.I(8), Term.I(2));
            actual = Term.Tuple(conversion, quotient, remainder, fallback, oppositeSigns, exact, adjusted, preserved);

            return actual;
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(
            Term.Tuple(Term.I(-2), Term.I(3)),
            Term.I(2),
            Term.I(-2),
            Term.Tuple(Term.I(8), Term.I(0)),
            Term.Tuple(Term.I(-8), Term.I(3)),
            Term.I(4),
            Term.I(1),
            Term.I(14)
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM guarded BIF tail returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralDynamicRemoteInvocation()
    {
        const string source = """
            -module(beam_general_apply_probe).
            -export([ordinary/3, tail/3, record/3]).
            -record(sample, {first, second, third, fourth = default}).
            ordinary(M,F,{X}) -> {M:F(X), X}.
            tail(M,F,{X}) -> M:F(X).
            record(M,F,{X}) -> #sample{second = M:F(X), first = X, _ = missing}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_apply_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term module = Term.A("erlang");
            Term function = Term.A("atom_to_list");
            Term argument = Term.Tuple(Term.A("probe"));
            Term ordinary = await image.Invoke(context, "ordinary", module, function, argument);
            Term tail = await image.Invoke(context, "tail", module, function, argument);
            Term record = await image.Invoke(context, "record", module, function, argument);
            actual = Term.Tuple(ordinary, tail, record);

            return actual;
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(
            Term.Tuple(Term.String("probe"), Term.A("probe")),
            Term.String("probe"),
            Term.Tuple(Term.A("sample"), Term.A("probe"), Term.String("probe"), Term.A("missing"), Term.A("missing"))
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Dynamic remote BEAM calls returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralClosureInvocation()
    {
        const string source = """
            -module(beam_general_closure_probe).
            -export([make/1]).
            make({Captured}) ->
                fun
                    (first) -> fun() -> Captured end;
                    (Other) -> {Other, Captured}
                end.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_closure_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            var outer = (FunctionTerm)await image.Invoke(context, "make", Term.Tuple(Term.I(17)));
            var inner = (FunctionTerm)await outer.Invoke(context, [Term.A("first")]);
            Term captured = await inner.Invoke(context, []);
            Term pair = await outer.Invoke(context, [Term.A("second")]);
            actual = Term.Tuple(captured, pair);

            return actual;
        });
        Term exit = await process.Completion;
        if (!Term.Tuple(Term.I(17), Term.Tuple(Term.A("second"), Term.I(17))).Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM closures returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralNamedClosure()
    {
        const string source = """
            -module(beam_named_fun_probe).
            -export([make/1]).
            make(Offset) ->
                fun Again(0) -> Offset;
                    Again(N) when N > 0 -> 1 + Again(N-1);
                    Again(_) -> Again
                end.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_named_fun_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            var original = (FunctionTerm)await image.Invoke(context, "make", Term.I(7));
            var self = (FunctionTerm)await original.Invoke(context, [Term.I(-1)]);
            actual = Term.Tuple(
                await original.Invoke(context, [Term.I(0)]),
                await original.Invoke(context, [Term.I(3)]),
                await self.Invoke(context, [Term.I(2)])
            );

            return actual;
        });
        Term exit = await process.Completion;
        if (!Term.Tuple(Term.I(7), Term.I(10), Term.I(9)).Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM named closure returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralAppend()
    {
        const string source = """
            -module(beam_general_append_probe).
            -export([nested/2, tail/2]).
            nested({A}, B) -> {A ++ B, B}.
            tail({A}, B) -> A ++ B.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_append_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term left = Term.Tuple(Term.List(Term.I(1)));
            Term right = Term.A("tail");
            Term nested = await image.Invoke(context, "nested", left, right);
            Term tail = await image.Invoke(context, "tail", left, right);
            Term invalid = Nil.Value;
            try
            {
                await image.Invoke(context, "tail", Term.Tuple(right), Nil.Value);
            }
            catch (ErlangException error)
            {
                invalid = error.Reason;
            }
            actual = Term.Tuple(nested, tail, invalid);

            return actual;
        });
        Term exit = await process.Completion;
        Term list = Cons.From([Term.I(1)], Term.A("tail"));
        if (!Term.Tuple(Term.Tuple(list, Term.A("tail")), list, Term.A("badarg")).Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM append returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareTrimmedCheckedPayload()
    {
        const string source = """
            -module(beam_trimmed_checked_payload).
            -export([run/2]).
            run(First, Second) ->
                {ok, Payload} = prepare(),
                ok = inspect(Payload),
                ok = write_value(Payload, First, Second),
                ok = inspect(Payload),
                done.
            prepare() -> {ok,payload}.
            inspect(payload) -> ok.
            write_value(payload, first, second) -> ok;
            write_value(_, _, _) -> invalid.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_trimmed_checked_payload.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term result = await image.Invoke(context, "run", Term.A("first"), Term.A("second"));
            Term failure;
            try
            {
                failure = await image.Invoke(context, "run", Term.A("second"), Term.A("first"));
            }
            catch (ErlangException error)
            {
                failure = error.Reason;
            }

            actual = Term.Tuple(result, failure);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(Term.A("done"), Term.Tuple(Term.A("badmatch"), Term.A("invalid")));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Trimmed checked payload returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareSavedCheckedPayload()
    {
        const string source = """
            -module(beam_saved_checked_payload).
            -export([run/2]).
            run(Input, Other) ->
                {ok, Payload} = prepare(Input),
                ok = touch(Payload),
                finish(Payload, Other).
            prepare(fail) -> invalid;
            prepare(Value) -> {ok,{payload,Value}}.
            touch(_) -> ok.
            finish(Value, Other) -> {Value,Other}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_saved_checked_payload.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term result = await image.Invoke(context, "run", Term.A("value"), Term.I(7));
            Term failure;
            try
            {
                failure = await image.Invoke(context, "run", Term.A("fail"), Term.I(7));
            }
            catch (ErlangException error)
            {
                failure = error.Reason;
            }

            actual = Term.Tuple(result, failure);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(
            Term.Tuple(Term.Tuple(Term.A("payload"), Term.A("value")), Term.I(7)),
            Term.Tuple(Term.A("badmatch"), Term.A("invalid"))
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Saved checked payload returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareConstantReturningCallChain()
    {
        const string source = """
            -module(beam_constant_tail_chain).
            -export([run/1, keep/1]).
            run(Input) when is_atom(Input) ->
                step(Input, first),
                step(Input, second),
                done.
            keep(Input) when is_atom(Input) ->
                step(Input, first),
                different(Input),
                done.
            step(Input, Phase) ->
                case {Input, Phase} of
                    {fail, second} -> throw(second_call);
                    _ -> ok
                end,
                done.
            different(value) -> other;
            different(_) -> done.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_constant_tail_chain.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term result = await image.Invoke(context, "run", Term.A("value"));
            Term kept = await image.Invoke(context, "keep", Term.A("value"));
            Term failure;
            try
            {
                failure = await image.Invoke(context, "run", Term.A("fail"));
            }
            catch (ErlangException error)
            {
                failure = Term.Tuple(Term.A("caught"), error.Reason);
            }

            actual = Term.Tuple(result, kept, failure);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(
            Term.A("done"), Term.A("done"), Term.Tuple(Term.A("caught"), Term.A("second_call"))
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Constant-returning call chain returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGuardedEntryCallChain()
    {
        const string source = """
            -module(beam_guarded_call_chain).
            -export([run/3]).
            run(Input, Saved, Mode) when is_atom(Input), Mode =:= run ->
                Value = prepare(Input),
                Checked = prepare(Value),
                finish(Checked, Saved).
            prepare(Input) -> {prepared,Input}.
            finish(Value, Saved) -> {Value,Saved}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_guarded_call_chain.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term value = await image.Invoke(context, "run", Term.A("input"), Term.I(7), Term.A("run"));
            Term failure;
            try
            {
                failure = await image.Invoke(context, "run", Term.A("input"), Term.I(7), Term.A("skip"));
            }
            catch (ErlangException error)
            {
                failure = error.Reason;
            }

            actual = Term.Tuple(value, failure);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term prepared = Term.Tuple(Term.A("prepared"), Term.Tuple(Term.A("prepared"), Term.A("input")));
        Term expected = Term.Tuple(Term.Tuple(prepared, Term.I(7)), Term.A("function_clause"));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Guarded call chain returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareForwardedCatch()
    {
        const string source = """
            -module(beam_forwarded_catch_probe).
            -export([run/1]).
            run(Input) ->
                Value = prepare(Input),
                catch finish(Input, Value, []).
            prepare(fail_before) -> throw(before_call);
            prepare(Input) -> {prepared,Input}.
            finish(fail_inside, _, _) -> throw(inside_call);
            finish(Input, Value, Acc) -> {Input,Value,Acc}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_forwarded_catch_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term value = await image.Invoke(context, "run", Term.A("value"));
            Term inside = await image.Invoke(context, "run", Term.A("fail_inside"));
            Term before;
            try
            {
                before = await image.Invoke(context, "run", Term.A("fail_before"));
            }
            catch (ErlangException error)
            {
                before = Term.Tuple(Term.A("uncaught"), error.Reason);
            }

            actual = Term.Tuple(value, inside, before);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(
            Term.Tuple(Term.A("value"), Term.Tuple(Term.A("prepared"), Term.A("value")), Nil.Value),
            Term.A("inside_call"),
            Term.Tuple(Term.A("uncaught"), Term.A("before_call"))
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Forwarded catch returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralCatch()
    {
        const string source = """
            -module(beam_general_catch_probe).
            -export([run/1, select/4, status/3, lookup/3, pick/2, status_nested/3, tagged_result/2, mixed_result/2, nested_result/2, tagged_tail/2, prepend/1, tuple_tail/3, list_tail/3, pattern_tail/2, grouped_tail/2, single_tail/2, tagged_single_tail/2, tuple_fallback/2, cons_fallback/2, cons_tuple_fallback/2, forwarded_tuple/2, embedded_tuple/3, reused_case/4, nil_case/1, literal_call_case/2, tuple_or_atom/1, tagged_call_tuple/1, nested_binary/1, literal_result/2, joined_case/1, nested_catch/3]).
            run({M,F,X}) -> catch M:F(X).
            select(M,F,X,Saved) ->
                case catch M:F(X) of
                    4 -> Saved;
                    _ -> failed
                end.
            status(Key, Entries, Saved) ->
                case lists:member(Key, Entries) of
                    true -> Saved;
                    Other -> Other
                end.
            lookup(Key, Entries, Saved) ->
                case lists:keyfind(Key, 1, Entries) of
                    Value when is_tuple(Value) -> pair(Saved, Value);
                    Other -> Other
                end.
            pick(Entries, Saved) ->
                case pick_value(Entries) of
                    c -> pair(Saved,c);
                    a -> Saved;
                    b -> skipped;
                    Other -> Other
                end.
            pick_value([{key,Value}|_]) -> Value;
            pick_value([]) -> missing.
            status_nested(Saved, Entries, Key) ->
                case lists:member(Key, relay(Entries)) of
                    true -> Saved;
                    Other -> Other
                end.
            tagged_result(Input, Saved) ->
                case relay(Input) of
                    {item,Value} -> Value;
                    _ -> Saved
                end.
            mixed_result(Input, Saved) ->
                case relay(Input) of
                    {item,Value} -> Value;
                    Bytes when is_binary(Bytes) -> Bytes;
                    _ -> Saved
                end.
            nested_result(Input, Saved) ->
                case relay(Input) of
                    {item,Value} -> Value;
                    Other -> {error,{{Saved},Other}}
                end.
            tagged_tail(Input, Saved) ->
                case relay(Input) of
                    {item,Value} -> triple(Value,Saved,Value);
                    _ -> Saved
                end.
            prepend(Head) ->
                Tail = relay(tail),
                pair(marker,[Head,Tail|Tail]).
            tuple_tail(A,B,C) -> triple({tag,{B,C},A},A,A).
            list_tail(A,B,C) -> pair([A,{B,C}|A],A).
            pattern_tail([H|T],A) -> pair({H,T},A);
            pattern_tail([],A) -> A.
            grouped_tail({H},A) -> pair({H,A},A);
            grouped_tail({H,T},A) -> pair([H|T],A).
            single_tail({H,T},A) -> pair([H|T],A).
            tagged_single_tail({item,H,7},A) -> pair({H,A},A).
            tuple_fallback({item,H,7},A) -> pair({H,A},A);
            tuple_fallback(Input,_) -> Input.
            cons_fallback([H|T],A) -> pair({H,T},A);
            cons_fallback(Input,A) -> pair(A,Input).
            cons_tuple_fallback([{item,H,7}=Whole|T],A) -> pair([{H,Whole}|T],A);
            cons_tuple_fallback(Input,_) -> Input.
            forwarded_tuple(Input,Saved) ->
                Value = relay(Input),
                triple({tag,Value,Saved},marker,[]).
            embedded_tuple(A,B,Tail) ->
                Name = relay(B),
                Spec = {A,{A,B},{tag,[A,B]},relay(B),[A]++Tail},
                pair(Name,Spec).
            reused_case(A,B,C,State) ->
                Name = relay(State),
                Key = {A,B,C},
                case classify(Name,Key) of
                    ok -> pair(Name,Key);
                    Other -> Other
                end.
            classify(allow,_) -> ok;
            classify(Error,_) -> Error.
            nil_case([]) -> empty_result();
            nil_case(Input) -> {error,{badarg,Input}}.
            empty_result() -> empty.
            literal_call_case(normal,Input) ->
                Local = {local,probe},
                case probe_start(Local,probe,[Input]) of
                    {ok,Value} -> {ok,Value,{normal,Input}};
                    Error -> Error
                end;
            literal_call_case(_,_) -> {error,badarg}.
            probe_start(_,_,[allow]) -> {ok,child};
            probe_start(_,_,_) -> {error,denied}.
            tuple_or_atom(Input) ->
                case relay(Input) of
                    {value,Value} -> Value;
                    none -> undefined
                end.
            tagged_call_tuple(Input) ->
                case relay(Input) of
                    {ok,Value} -> {ok,relay(Value)};
                    Error -> Error
                end.
            nested_binary(Input) -> pair(tag,term_to_binary(Input)).
            literal_result(timeout,_) -> {stop,normal,done}.
            joined_case(active) ->
                case read_mode() of
                    {ok,_} -> ok;
                    undefined -> configure_mode()
                end,
                after_mode(),
                {ok,joined}.
            read_mode() -> undefined.
            configure_mode() -> ok.
            after_mode() -> ok.
            nested_catch(A,B,C) ->
                Result = triple(marker,relay([A,B,tail]),C),
                catch pair(tag,[Result]).
            triple(A,B,C) -> {A,B,C}.
            relay(Entries) -> Entries.
            pair(A, B) -> {A,B}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_catch_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term erlang = Term.A("erlang");
            Term reason = Term.A("probe");
            Term normal = await image.Invoke(context, "run", Term.Tuple(erlang, Term.A("abs"), Term.I(-4)));
            Term thrown = await image.Invoke(context, "run", Term.Tuple(erlang, Term.A("throw"), reason));
            Term exited = await image.Invoke(context, "run", Term.Tuple(erlang, Term.A("exit"), reason));
            Term error = await image.Invoke(context, "run", Term.Tuple(erlang, Term.A("error"), reason));
            Term selected = await image.Invoke(context, "select", erlang, Term.A("abs"), Term.I(-4), reason);
            Term caught = await image.Invoke(context, "select", erlang, Term.A("error"), reason, reason);
            Term member = await image.Invoke(context, "status", reason, Term.List(reason), Term.A("saved"));
            Term missing = await image.Invoke(context, "status", reason, Nil.Value, Term.A("saved"));
            Term tuple = Term.Tuple(reason, Term.I(7));
            Term found = await image.Invoke(context, "lookup", reason, Term.List(tuple), Term.A("saved"));
            Term absent = await image.Invoke(context, "lookup", reason, Nil.Value, Term.A("saved"));
            Term picked = await image.Invoke(context, "pick", Term.List(Term.Tuple(Term.A("key"), Term.A("c"))), reason);
            Term defaulted = await image.Invoke(context, "pick", Nil.Value, reason);
            Term nested = await image.Invoke(context, "status_nested", Term.A("saved"), Term.List(reason), reason);
            Term nestedMissing = await image.Invoke(context, "status_nested", Term.A("saved"), Nil.Value, reason);
            Term tagged = await image.Invoke(context, "tagged_result", Term.Tuple(Term.A("item"), Term.I(11)), reason);
            Term tagFallback = await image.Invoke(context, "tagged_result", Term.Tuple(Term.A("item")), reason);
            Term mixedTag = await image.Invoke(context, "mixed_result", Term.Tuple(Term.A("item"), Term.I(11)), reason);
            Term mixedBinary = await image.Invoke(context, "mixed_result", new BitString("ok"u8), reason);
            Term mixedFallback = await image.Invoke(context, "mixed_result", Nil.Value, reason);
            Term tupleTail = await image.Invoke(context, "tuple_tail", reason, Term.I(1), Term.I(2));
            Term constructed = Term.Tuple(Term.A("tag"), Term.Tuple(Term.I(1), Term.I(2)), reason);
            if (!Term.Tuple(constructed, reason, reason).Equals(tupleTail))
            {
                throw new InvalidOperationException($"Building a tail-call tuple overwrote a forwarded argument: {tupleTail}.");
            }

            Term listTail = await image.Invoke(context, "list_tail", reason, Term.I(1), Term.I(2));
            Term constructedList = new Cons(reason, new Cons(Term.Tuple(Term.I(1), Term.I(2)), reason));
            if (!Term.Tuple(constructedList, reason).Equals(listTail))
            {
                throw new InvalidOperationException($"Building a tail-call list overwrote a live input: {listTail}.");
            }

            Term head = Term.I(1);
            Term tailValue = Term.I(2);
            Term patternTail = await image.Invoke(context, "pattern_tail", new Cons(head, tailValue), reason);
            Term emptyTail = await image.Invoke(context, "pattern_tail", Nil.Value, reason);
            Term groupedTuple = await image.Invoke(context, "grouped_tail", Term.Tuple(head), reason);
            Term groupedList = await image.Invoke(context, "grouped_tail", Term.Tuple(head, tailValue), reason);
            if (!Term.Tuple(Term.Tuple(head, tailValue), reason).Equals(patternTail) || !reason.Equals(emptyTail)
                || !Term.Tuple(Term.Tuple(head, reason), reason).Equals(groupedTuple)
                || !Term.Tuple(new Cons(head, tailValue), reason).Equals(groupedList))
            {
                throw new InvalidOperationException("A constructed tail argument did not preserve the pattern bindings.");
            }

            Term singleList = await image.Invoke(context, "single_tail", Term.Tuple(head, tailValue), reason);
            Term singleTagged = await image.Invoke(context, "tagged_single_tail", Term.Tuple(Term.A("item"), head, Term.I(7)), reason);
            if (!Term.Tuple(new Cons(head, tailValue), reason).Equals(singleList)
                || !Term.Tuple(Term.Tuple(head, reason), reason).Equals(singleTagged))
            {
                throw new InvalidOperationException("A single tuple clause did not preserve the tail arguments.");
            }

            Term badLiteral = Term.Tuple(Term.A("item"), head, Term.I(8));
            Term tupleFallback = await image.Invoke(context, "tuple_fallback", badLiteral, reason);
            Term consMatch = await image.Invoke(context, "cons_fallback", new Cons(head, tailValue), reason);
            Term consFallback = await image.Invoke(context, "cons_fallback", Nil.Value, reason);
            if (!badLiteral.Equals(tupleFallback)
                || !Term.Tuple(Term.Tuple(head, tailValue), reason).Equals(consMatch)
                || !Term.Tuple(reason, Nil.Value).Equals(consFallback))
            {
                throw new InvalidOperationException("A constructed entry did not preserve the fallback inputs.");
            }

            Term whole = Term.Tuple(Term.A("item"), head, Term.I(7));
            Term consTuple = await image.Invoke(context, "cons_tuple_fallback", new Cons(whole, tailValue), reason);
            Term badHead = new Cons(badLiteral, tailValue);
            Term failedHead = await image.Invoke(context, "cons_tuple_fallback", badHead, reason);
            if (!Term.Tuple(new Cons(Term.Tuple(head, whole), tailValue), reason).Equals(consTuple)
                || !badHead.Equals(failedHead))
            {
                throw new InvalidOperationException("A tuple-head alias or the fallback list was overwritten.");
            }

            Term forwarded = await image.Invoke(context, "forwarded_tuple", head, reason);
            if (!Term.Tuple(Term.Tuple(Term.A("tag"), head, reason), Term.A("marker"), Nil.Value).Equals(forwarded))
            {
                throw new InvalidOperationException("A tuple argument did not retain the call result and saved input.");
            }

            Term embedded = await image.Invoke(context, "embedded_tuple", head, tailValue, reason);
            Term specification = Term.Tuple(
                head,
                Term.Tuple(head, tailValue),
                Term.Tuple(Term.A("tag"), Term.List(head, tailValue)),
                tailValue,
                new Cons(head, reason)
            );
            if (!Term.Tuple(tailValue, specification).Equals(embedded))
            {
                throw new InvalidOperationException("An embedded call did not preserve the constructor operands.");
            }

            Term allowed = Term.A("allow");
            Term accepted = await image.Invoke(context, "reused_case", head, tailValue, reason, allowed);
            Term rejected = await image.Invoke(context, "reused_case", head, tailValue, reason, reason);
            if (!Term.Tuple(allowed, Term.Tuple(head, tailValue, reason)).Equals(accepted) || !reason.Equals(rejected))
            {
                throw new InvalidOperationException("Reusing a tuple case frame changed the accepted or rejected result.");
            }

            Term nilResult = await image.Invoke(context, "nil_case", Nil.Value);
            Term nilError = await image.Invoke(context, "nil_case", reason);
            Term started = await image.Invoke(context, "literal_call_case", Term.A("normal"), allowed);
            Term denied = await image.Invoke(context, "literal_call_case", Term.A("normal"), reason);
            Term badMode = await image.Invoke(context, "literal_call_case", reason, allowed);
            if (!Term.A("empty").Equals(nilResult)
                || !Term.Tuple(Term.A("error"), Term.Tuple(Term.A("badarg"), reason)).Equals(nilError)
                || !Term.Tuple(Term.A("ok"), Term.A("child"), Term.Tuple(Term.A("normal"), allowed)).Equals(started)
                || !Term.Tuple(Term.A("error"), Term.A("denied")).Equals(denied)
                || !Term.Tuple(Term.A("error"), Term.A("badarg")).Equals(badMode))
            {
                throw new InvalidOperationException("The register case changed a success or fallback value.");
            }

            Term selectedValue = await image.Invoke(context, "tuple_or_atom", Term.Tuple(Term.A("value"), reason));
            Term missingValue = await image.Invoke(context, "tuple_or_atom", Term.A("none"));
            if (!reason.Equals(selectedValue) || !Term.A("undefined").Equals(missingValue))
            {
                throw new InvalidOperationException("A closed tuple and atom case returned an unexpected value.");
            }

            Term taggedCall = await image.Invoke(context, "tagged_call_tuple", Term.Tuple(Term.A("ok"), reason));
            Term taggedError = await image.Invoke(context, "tagged_call_tuple", reason);
            if (!Term.Tuple(Term.A("ok"), reason).Equals(taggedCall) || !reason.Equals(taggedError))
            {
                throw new InvalidOperationException("The tagged call tuple changed the matched or fallback result.");
            }

            Term serialized = await image.Invoke(context, "nested_binary", reason);
            Term literalReturn = await image.Invoke(context, "literal_result", Term.A("timeout"), reason);
            Term joined = await image.Invoke(context, "joined_case", Term.A("active"));
            if (serialized is not TupleTerm { Items: [Atom { Name: "tag" }, BitString] }
                || !Term.Tuple(Term.A("stop"), Term.A("normal"), Term.A("done")).Equals(literalReturn)
                || !Term.Tuple(Term.A("ok"), Term.A("joined")).Equals(joined))
            {
                throw new InvalidOperationException("A nested conversion, literal entry or joined case returned an unexpected value.");
            }

            Term caughtTuple = await image.Invoke(context, "nested_catch", head, tailValue, reason);
            Term nestedInput = Term.Tuple(Term.A("marker"), Term.List(head, tailValue, Term.A("tail")), reason);
            if (!Term.Tuple(Term.A("tag"), Term.List(nestedInput)).Equals(caughtTuple))
            {
                throw new InvalidOperationException("The nested list calls changed the caught result.");
            }

            Term list = await image.Invoke(context, "prepend", reason);
            Term tail = Term.A("tail");
            if (!Term.Tuple(Term.A("marker"), new Cons(reason, new Cons(tail, tail))).Equals(list))
            {
                throw new InvalidOperationException($"Forwarding the call result into an improper list returned {list}.");
            }

            actual = Term.Tuple(normal, thrown, exited, error, selected, caught, member, missing, found, absent, picked, defaulted, nested, nestedMissing, tagged, tagFallback, mixedTag, mixedBinary, mixedFallback,
                await image.Invoke(context, "nested_result", Term.A("other"), reason),
                await image.Invoke(context, "tagged_tail", Term.Tuple(Term.A("item"), Term.A("value")), reason),
                await image.Invoke(context, "tagged_tail", Nil.Value, reason));

            return actual;
        });
        Term exit = await process.Completion;
        if (actual is not TupleTerm
            {
                Items: [
                    Integer normal, Atom { Name: "probe" },
                    TupleTerm { Items: [Atom { Name: "EXIT" }, Atom { Name: "probe" }] },
                    TupleTerm { Items: [Atom { Name: "EXIT" }, TupleTerm { Items: [Atom { Name: "probe" }, Cons] }] },
                    Atom { Name: "probe" }, Atom { Name: "failed" },
                    Atom { Name: "saved" }, Atom { Name: "false" },
                    TupleTerm { Items: [Atom { Name: "saved" }, TupleTerm { Items: [Atom { Name: "probe" }, Integer] }] },
                    Atom { Name: "false" },
                    TupleTerm { Items: [Atom { Name: "probe" }, Atom { Name: "c" }] },
                    Atom { Name: "missing" }, Atom { Name: "saved" }, Atom { Name: "false" }, Integer { Value: var tagValue }, Atom { Name: "probe" }, Integer { Value: var mixedValue }, BitString, Atom { Name: "probe" },
                    TupleTerm { Items: [Atom { Name: "error" }, TupleTerm { Items: [TupleTerm { Items: [Atom { Name: "probe" }] }, Atom { Name: "other" }] }] },
                    TupleTerm { Items: [Atom { Name: "value" }, Atom { Name: "probe" }, Atom { Name: "value" }] },
                    Atom { Name: "probe" }
                ]
            } || normal.Value != 4 || tagValue != 11 || mixedValue != 11)
        {
            throw new InvalidOperationException($"General BEAM catch returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralRecordGuard()
    {
        const string source = """
            -module(beam_general_record_guard_probe).
            -export([run/1]).
            -record(probe, {value}).
            run({X}) when is_record(X,probe) orelse is_integer(X) -> yes;
            run(_) -> no.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_record_guard_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term goodRecord = await image.Invoke(context, "run", Term.Tuple(Term.Tuple(Term.A("probe"), Term.I(0))));
            Term wrongArity = await image.Invoke(context, "run", Term.Tuple(Term.Tuple(Term.A("probe"))));
            Term integer = await image.Invoke(context, "run", Term.Tuple(Term.I(4)));
            actual = Term.Tuple(goodRecord, wrongArity, integer);

            return actual;
        });
        Term exit = await process.Completion;
        if (!Term.Tuple(Term.A("yes"), Term.A("no"), Term.A("yes")).Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM record guard returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralBitStringReferenceAndMapKeyGuards()
    {
        const string source = """
            -module(beam_extended_guard_probe).
            -export([bitstr/1,ref/1,has/2,lacks/2]).
            bitstr(X) when is_bitstring(X) -> yes;
            bitstr(_) -> no.
            ref(X) when not is_reference(X) -> no;
            ref(_) -> yes.
            has(K,M) when is_map_key(K,M) -> yes;
            has(_,_) -> no.
            lacks(K,M) when not erlang:is_map_key(K,M) -> yes;
            lacks(_,_) -> no.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_extended_guard_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term yes = Term.A("yes");
        Term no = Term.A("no");
        Term key = Term.A("key");
        Term missing = Term.A("missing");
        var map = new MapTerm([new(key, Term.I(0))]);
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            actual = Term.List(
                await image.Invoke(context, "bitstr", new BitString([0x80], 1)),
                await image.Invoke(context, "bitstr", key),
                await image.Invoke(context, "ref", new ReferenceTerm("guard@fixture", 1)),
                await image.Invoke(context, "ref", key),
                await image.Invoke(context, "has", key, map),
                await image.Invoke(context, "has", missing, map),
                await image.Invoke(context, "has", key, key),
                await image.Invoke(context, "lacks", key, map),
                await image.Invoke(context, "lacks", missing, map),
                await image.Invoke(context, "lacks", key, key)
            );

            return actual;
        });
        Term exit = await process.Completion;
        if (!Term.List(yes, no, yes, no, yes, no, no, no, yes, no).Equals(actual))
        {
            throw new InvalidOperationException($"Extended BEAM guards returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralMapComprehensions()
    {
        const string source = """
            -module(beam_map_comprehension_probe).
            -export([run/1, strict/1]).
            run(M) ->
                {#{K => V+1 || K := V <- M, is_integer(V)},
                 #{same => V || V <- [1,2,3]},
                 [V || wanted := V <- M]}.
            strict(M) -> #{wanted => V || wanted := V <:- M}.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_map_comprehension_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term wanted = Term.A("wanted");
        Term other = Term.A("other");
        Term ignored = Term.A("ignored");
        var map = new MapTerm([new(wanted, Term.I(7)), new(other, ignored)]);
        var expectedMap = new MapTerm([new(wanted, Term.I(8))]);
        var expectedDuplicate = new MapTerm([new(Term.A("same"), Term.I(3))]);
        var largeMap = new MapTerm(
            Enumerable.Range(0, 512).Select(index => new KeyValuePair<Term, Term>(Term.I(index), ignored))
                .Append(new(wanted, Term.I(7)))
        );
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term ordinary = await image.Invoke(context, "run", map);
            Term invalid = await ErrorReason("run", ignored);
            Term mismatch = await ErrorReason("strict", map);
            Term large = await image.Invoke(context, "run", largeMap);
            actual = Term.Tuple(ordinary, invalid, mismatch, large);

            return actual;

            async ValueTask<Term> ErrorReason(string function, Term input)
            {
                try
                {
                    return await image.Invoke(context, function, input);
                }
                catch (ErlangException error)
                {
                    return error.Reason;
                }
            }
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(
            Term.Tuple(expectedMap, expectedDuplicate, Term.List(Term.I(7))),
            Term.Tuple(Term.A("bad_generator"), ignored),
            Term.Tuple(Term.A("badmatch"), Term.Tuple(other, ignored)),
            Term.Tuple(expectedMap, expectedDuplicate, Term.List(Term.I(7)))
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM map comprehensions returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralMaybeAndMapSizeGuard()
    {
        const string source = """
            -module(beam_maybe_probe).
            -export([run/3, raw/2, size_test/1]).
            run(A,B,Tag) ->
                R = maybe
                    {ok,X} ?= A,
                    {ok,Y} ?= B,
                    {X,Y}
                else
                    {error,Tag} -> handled;
                    N when is_integer(N) -> {number,N}
                end,
                {Tag,R}.
            raw(A,B) ->
                maybe
                    X ?= A,
                    {ok,X} ?= B
                end.
            size_test(M) when map_size(M) > 0 -> yes;
            size_test(_) -> no.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_maybe_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term tag = Term.A("tag");
        Term error = Term.A("error");
        Term ok = Term.A("ok");
        Term one = Term.I(1);
        Term two = Term.I(2);
        Term number = Term.I(42);
        Term first = Term.Tuple(ok, one);
        Term second = Term.Tuple(ok, two);
        Term unhandled = Term.Tuple(error, Term.A("other"));
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term unmatched;
            try
            {
                unmatched = await image.Invoke(context, "run", unhandled, tag, tag);
            }
            catch (ErlangException exception)
            {
                unmatched = exception.Reason;
            }

            actual = Term.List(
                await image.Invoke(context, "run", first, second, tag),
                await image.Invoke(context, "run", Term.Tuple(error, tag), tag, tag),
                await image.Invoke(context, "run", first, number, tag),
                unmatched,
                await image.Invoke(context, "raw", one, first),
                await image.Invoke(context, "raw", one, second),
                await image.Invoke(context, "size_test", new MapTerm([new(tag, one)])),
                await image.Invoke(context, "size_test", new MapTerm([])),
                await image.Invoke(context, "size_test", tag)
            );

            return actual;
        });
        Term exit = await process.Completion;
        Term expected = Term.List(
            Term.Tuple(tag, Term.Tuple(one, two)),
            Term.Tuple(tag, Term.A("handled")),
            Term.Tuple(tag, Term.Tuple(Term.A("number"), number)),
            Term.Tuple(Term.A(ErlangErrorReasons.ElseClause), unhandled),
            first,
            second,
            Term.A("yes"),
            Term.A("no"),
            Term.A("no")
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM maybe and map size returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralNumericAndMapLookupGuards()
    {
        const string source = """
            -module(beam_numeric_guard_probe).
            -export([absolute/1,bits/1,truncate/1,fetch/1]).
            absolute(X) when abs(X) =:= 3.5 -> yes;
            absolute(_) -> no.
            bits(X) when bit_size(X) =:= 3 -> yes;
            bits(_) -> no.
            truncate(X) when trunc(X) =:= -3 -> yes;
            truncate(_) -> no.
            fetch(X) when map_get(key,X) =:= value -> yes;
            fetch(_) -> no.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_numeric_guard_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term yes = Term.A("yes");
        Term no = Term.A("no");
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            actual = Term.List(
                await image.Invoke(context, "absolute", new FloatTerm(-3.5)),
                await image.Invoke(context, "absolute", Term.I(-3)),
                await image.Invoke(context, "absolute", no),
                await image.Invoke(context, "bits", new BitString([0xA0], 3)),
                await image.Invoke(context, "bits", new BitString([5])),
                await image.Invoke(context, "bits", no),
                await image.Invoke(context, "truncate", new FloatTerm(-3.9)),
                await image.Invoke(context, "truncate", Term.I(-4)),
                await image.Invoke(context, "truncate", no),
                await image.Invoke(context, "fetch", new MapTerm([new(Term.A("key"), Term.A("value"))])),
                await image.Invoke(context, "fetch", new MapTerm([])),
                await image.Invoke(context, "fetch", no)
            );

            return actual;
        });
        Term exit = await process.Completion;
        Term expected = Term.List(yes, no, no, yes, no, no, yes, no, no, yes, no, no);
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM numeric and map lookup guards returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralBinaryComprehensions()
    {
        const string source = """
            -module(beam_binary_comprehension_probe).
            -export([pack/1, join/1]).
            pack(Items) -> << <<1:N>> || {bit,N} <- Items, is_integer(N) >>.
            join(Items) -> << Part || Part <- Items >>.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_binary_comprehension_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term bit = Term.A("bit");
        Term no = Term.A("no");
        Term items = Term.List(Term.Tuple(bit, Term.I(1)), Term.A("skip"), Term.Tuple(bit, no), Term.Tuple(bit, Term.I(2)));
        Term parts = Term.List(new BitString([0xA0], 3), new BitString([0xC0], 2));
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            actual = Term.List(
                await image.Invoke(context, "pack", items),
                await image.Invoke(context, "join", parts),
                await image.Invoke(context, "pack", Nil.Value),
                await ErrorReason("pack", no),
                await ErrorReason("join", Term.List(no))
            );

            return actual;

            async ValueTask<Term> ErrorReason(string function, Term input)
            {
                try
                {
                    return await image.Invoke(context, function, input);
                }
                catch (ErlangException error)
                {
                    return error.Reason;
                }
            }
        });
        Term exit = await process.Completion;
        Term expected = Term.List(
            new BitString([0xA0], 3),
            new BitString([0xB8], 5),
            new BitString([]),
            Term.Tuple(Term.A("bad_generator"), no),
            Term.A(ErlangErrorReasons.BadArgument)
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM binary comprehensions returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralBinaryGenerators()
    {
        const string source = """
            -module(beam_bit_generator_probe).
            -export([bits/1, pairs/1, strict/1, prefix/1, width/2, strict_prefix/1]).
            bits(B) -> [X || <<X:1>> <= B].
            pairs(B) -> [X || <<1:1,X:2>> <= B].
            strict(B) -> [X || <<1:1,X:2>> <:= B].
            prefix(B) -> [P || <<N:8,P:N/binary>> <= B].
            width(N,B) -> [P || <<P:N/unit:8>> <= B].
            strict_prefix(B) -> [P || <<N:8,P:N/binary>> <:= B].
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_bit_generator_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term no = Term.A("no");
        Term badMatch = Term.A(ErlangErrorReasons.BadMatch);
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            actual = Term.List(
                await image.Invoke(context, "bits", new BitString([0xA0], 3)),
                await image.Invoke(context, "pairs", new BitString([0xAB, 0xC0], 10)),
                await image.Invoke(context, "strict", new BitString([0xBC], 6)),
                await ErrorReason("strict", new BitString([0xB0], 4)),
                await ErrorReason("strict", new BitString([0xAB, 0x80], 9)),
                await ErrorReason("bits", no),
                await image.Invoke(context, "prefix", new BitString([1, 97, 0, 2, 98])),
                await image.Invoke(context, "width", Term.I(2), new BitString([1, 2, 3, 4, 5])),
                await ErrorReason("strict_prefix", new BitString([1, 97, 2, 98]))
            );

            return actual;

            async ValueTask<Term> ErrorReason(string function, Term input)
            {
                try
                {
                    return await image.Invoke(context, function, input);
                }
                catch (ErlangException error)
                {
                    return error.Reason;
                }
            }
        });
        Term exit = await process.Completion;
        Term pair = Term.List(Term.I(1), Term.I(3));
        Term expected = Term.List(
            Term.List(Term.I(1), Term.I(0), Term.I(1)),
            pair,
            pair,
            Term.Tuple(badMatch, new BitString([0x80], 1)),
            Term.Tuple(badMatch, new BitString([0x5C], 6)),
            Term.Tuple(Term.A("bad_generator"), no),
            Term.List(new BitString([97]), new BitString([])),
            Term.List(Term.I(258), Term.I(772)),
            Term.Tuple(badMatch, new BitString([2, 98]))
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM binary generators returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareInlineComprehensionBindings()
    {
        const string source = """
            -module(beam_inline_comprehension).
            -compile({inline,[collect/2]}).
            -export([run/2]).
            run(Tag, Rows) -> collect(Tag, Rows).
            collect(Tag, Rows) when is_list(Rows) ->
                [{Tag, Item} || Rows <- Rows, Item <- Rows, Item > 0];
            collect(_, _) -> invalid.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_inline_comprehension.erl"));
        await using var runtime = new ProcessRuntime();
        Term tag = Term.A("tag");
        Term rows = Term.List(Term.List(Term.I(0), Term.I(1)), Term.List(Term.I(2)));
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term accepted = await image.Invoke(context, "run", tag, rows);
            Term rejected = await image.Invoke(context, "run", tag, Term.A("bad"));
            actual = Term.Tuple(accepted, rejected);
            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(
            Term.List(Term.Tuple(tag, Term.I(1)), Term.Tuple(tag, Term.I(2))),
            Term.A("invalid")
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM inline comprehension returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareInlineClosureBindings()
    {
        const string source = """
            -module(beam_inline_closure_bindings).
            -compile({inline,[make/2]}).
            -export([run/2]).
            run(Value, Shadow) ->
                F = make(Value, Shadow),
                F({inner, 7}).
            make(Value, Shadow) ->
                fun({Shadow, X}) -> {Value, Shadow, X} end.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_inline_closure_bindings.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            actual = await image.Invoke(context, "run", Term.A("capture"), Term.A("outer"));
            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(Term.A("capture"), Term.A("inner"), Term.I(7));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM inline closure returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareInlineEffectsAndReferences()
    {
        const string source = """
            -module(beam_inline_effect_probe).
            -compile({inline,[duplicate/2, identity/1]}).
            -export([run/0, ref/0]).
            run() ->
                put(trace, []),
                Pair = duplicate(mark(first), mark(second)),
                {Pair, get(trace)}.
            ref() -> fun identity/1.
            duplicate(X, _) -> {X,X}.
            identity(X) -> X.
            mark(X) ->
                put(trace, [X|get(trace)]),
                X.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_inline_effect_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term first = Term.A("first");
        Term second = Term.A("second");
        Term kept = Term.A("kept");
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term result = await image.Invoke(context, "run");
            var reference = (FunctionTerm)await image.Invoke(context, "ref");
            actual = Term.Tuple(result, await reference.Invoke(context, [kept]));

            return actual;
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(Term.Tuple(Term.Tuple(first, first), Term.List(second, first)), kept);
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Targeted BEAM inline arguments and reference returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareInlineRecordActions()
    {
        const string source = """
            -module(beam_inline_record_probe).
            -compile({inline,[reduce/1, patch/2]}).
            -export([run/2]).
            -record(node, {value, extra, rest}).
            run(Stack, V) ->
                [R|Tail] = reduce(Stack),
                {patch(R,V),Tail}.
            reduce(Stack) ->
                [A,B|Tail] = Stack,
                [begin #node{value=A, _=B} end | Tail].
            patch(R,V) -> R#node{value=V}.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_inline_record_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term other = Term.A("other");
        Term tail = Term.A("tail");
        Term updated = Term.A("new");
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            actual = await image.Invoke(context, "run", Term.List(Term.A("first"), other, tail), updated);

            return actual;
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(Term.Tuple(Term.A("node"), updated, other, other), Term.List(tail));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Targeted BEAM inline record actions returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareInlineGuardedClauses()
    {
        const string source = """
            -module(beam_inline_clauses_probe).
            -export([run/1]).
            -compile({inline,[classify/1]}).
            run(X) -> {classify(X),X}.
            classify({N}) when is_integer(N) -> {number,N};
            classify(no) -> absent.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_inline_clauses_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term seven = Term.I(7);
        Term no = Term.A("no");
        Term other = Term.A("other");
        Term? actual = null;
        Exception? failure = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            try
            {
                Term number = await image.Invoke(context, "run", Term.Tuple(seven));
                Term absent = await image.Invoke(context, "run", no);
                try
                {
                    await image.Invoke(context, "run", other);
                }
                catch (ErlangException error) when (error.StackTraceTerm is Cons
                    { Head: TupleTerm { Items: [var module, var function, var arguments, _] } })
                {
                    actual = Term.Tuple(number, absent, Term.Tuple(error.Reason, module, function, arguments));
                }
            }
            catch (Exception error)
            {
                failure = error;
            }

            return actual ?? Nil.Value;
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(
            Term.Tuple(Term.Tuple(Term.A("number"), seven), Term.Tuple(seven)),
            Term.Tuple(Term.A("absent"), no),
            Term.Tuple(Term.A("function_clause"), Term.A("beam_inline_clauses_probe"),
                Term.A("-inlined-classify/1-"), Term.List(other))
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Targeted BEAM inline branches returned {actual}, exit {exit}.", failure);
        }
    }

    private static async Task CompareListEntryFallback()
    {
        const string file = "list_entry_fallback_probe.erl";
        var dependencies = new BeamDependencySelection(["lists", "erlang"]);
        BeamModule image = BeamModule.Prepare(
            CodeGeneration.CompileBeamModule(Encoding.UTF8.GetString(Read(file)), file),
            dependencies
        );
        await using var runtime = new ProcessRuntime();
        using PreparedBeamImage lists = runtime.Modules.PrepareBeam(
            EtsTraversalOriginal416Access.Resource(EtsTraversalOriginal416Names.ListsBeam),
            dependencies
        );
        if (runtime.Modules.PublishPreparedBeam(lists).Status != BeamPublicationStatus.Published)
        {
            throw new InvalidOperationException("Could not load the OTP lists fixture for the list entry check.");
        }

        Term a = Term.A("a");
        Term b = Term.A("b");
        Term left = Term.List(a, b);
        Term right = Term.List(b, a);
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term matched = await image.Invoke(
                context,
                "scan",
                Term.List(left, right),
                Nil.Value
            );
            Term empty = await image.Invoke(
                context,
                "scan",
                Nil.Value,
                left
            );
            try
            {
                await image.Invoke(
                    context,
                    "fold",
                    Nil.Value,
                    a
                );
            }
            catch (ErlangException error)
            {
                actual = Term.Tuple(matched, empty, error.Reason);
            }

            return actual ?? Nil.Value;
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(Term.Tuple(right, Nil.Value), Term.Tuple(Nil.Value, left), Term.A("function_clause"));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM list entry fallback returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareFoldCallbackClauses()
    {
        const string fixture = "beam_fold_callback_probe.erl";
        string source = Encoding.UTF8.GetString(Read(fixture));
        var dependencies = new BeamDependencySelection(["lists", "erlang"]);
        CheckSourceInlineOptOut(dependencies);
        BeamModule image = BeamModule.Prepare(
            CodeGeneration.CompileBeamModule(source, fixture),
            dependencies
        );
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage lists = runtime.Modules.PrepareBeam(
            EtsTraversalOriginal416Access.Resource(EtsTraversalOriginal416Names.ListsBeam),
            dependencies
        );
        if (runtime.Modules.PublishPreparedBeam(lists).Status != BeamPublicationStatus.Published)
        {
            throw new InvalidOperationException("Could not load the OTP lists fixture for the fold callback check.");
        }
        Term bad = Term.A("bad");
        Term offset = Term.I(10);
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term list = await image.Invoke(context, "run", Term.List(Term.I(2), Term.A("skip"), Term.I(4)), offset);
            Term empty = await image.Invoke(context, "run", Nil.Value, offset);
            Term single = await image.Invoke(context, "single", Term.List(Term.I(2), Term.I(4)), offset);
            Term record = Term.Tuple(Term.A("fold_record"), offset, Term.List(bad));
            Term foldedRecord = await image.Invoke(context, "record_fold", record);
            Term singleFailure = Nil.Value;
            try
            {
                await image.Invoke(context, "single", Term.List(bad), offset);
            }
            catch (ErlangException error)
            {
                var callback = Cons.Items(error.StackTraceTerm).OfType<TupleTerm>().FirstOrDefault(frame =>
                    frame.Items is [Atom { Name: "beam_fold_callback_probe" }, Atom { Name: "-single/2-fun-0-" }, _, _]);
                singleFailure = Term.Tuple(error.Reason, callback?.Items[1] ?? Nil.Value);
            }

            try
            {
                await image.Invoke(context, "run", Term.List(bad), offset);
            }
            catch (ErlangException error) when (error.StackTraceTerm is Cons
                { Head: TupleTerm { Items: [var module, var function, var arguments, _] } })
            {
                actual = Term.Tuple(list, empty, single, foldedRecord, singleFailure,
                    Term.Tuple(error.Reason, module, function, arguments));
            }

            return actual ?? Nil.Value;
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(
            Term.List(Term.I(14), Term.I(12)),
            Nil.Value,
            Term.List(Term.I(14), Term.I(12)),
            Term.List(Term.Tuple(Term.A("first"), offset), Term.Tuple(Term.A("second"), offset), bad),
            Term.Tuple(Term.A("badarith"), Term.A("-single/2-fun-0-")),
            Term.Tuple(Term.A("function_clause"), Term.A("beam_fold_callback_probe"),
                Term.A("-run/2-inlined-0-"), Term.List(bad, Nil.Value))
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM fold callback returned {actual}, exit {exit}.");
        }
    }

    private static void CheckSourceInlineOptOut(BeamDependencySelection dependencies)
    {
        const string fixture = "beam_release_inline_probe.erl";
        const string helper = "fold";
        string source = Encoding.UTF8.GetString(Read(fixture));
        BeamModule enabled = Prepare("-compile(inline).");
        BeamModule disabled = Prepare("-compile([inline, no_inline]).");
        BeamModule reversed = Prepare("-compile([no_inline, inline]).");
        if (enabled.Functions.Any(function => function.Name == helper)
            || !disabled.Functions.Any(function => function.Name == helper)
            || !reversed.Functions.Any(function => function.Name == helper))
        {
            throw new InvalidOperationException("The source no_inline option must suppress automatic inlining regardless of option order.");
        }

        BeamModule Prepare(string directive) => BeamModule.Prepare(
            CodeGeneration.CompileBeamModule(
                source.Replace("-export", directive + "\n-export", StringComparison.Ordinal),
                fixture
            ),
            dependencies
        );
    }

    private static async Task CompareInlinePatterns()
    {
        const string source = """
            -module(beam_inline_patterns_probe).
            -export([run/3]).
            -compile({inline,[classify/2,bits/2,protect/1]}).
            run(Key, Input, Binary) ->
                {classify(Key, Input), bits(8, Binary), protect(Input)}.
            classify(Key, Input) ->
                case Input of #{Key := Value} = Map -> {Value, Map}; _ -> absent end.
            bits(Size, Binary) ->
                case Binary of <<N:Size, Rest/binary>> -> {N,Rest}; _ -> absent end.
            protect(Value) ->
                try element(1, Value) catch error:badarg -> absent end.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_inline_patterns_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term key = Term.A("k");
        Term absent = Term.A("absent");
        Term ok = Term.A("ok");
        var map = new MapTerm([new(key, Term.I(17))]);
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term matched = await image.Invoke(context, "run", key, map, new BitString([9, 8]));
            Term fallback = await image.Invoke(context, "run", key, Term.Tuple(ok), new BitString([]));
            actual = Term.Tuple(matched, fallback);

            return actual;
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(
            Term.Tuple(Term.Tuple(Term.I(17), map), Term.Tuple(Term.I(9), new BitString([8])), absent),
            Term.Tuple(absent, absent, ok)
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM inline pattern probe returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralNumericEquality()
    {
        const string source = """
            -module(beam_general_numeric_probe).
            -export([run/2]).
            run({A},B) when A == B -> {A /= B, A == B};
            run(_,_) -> no.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_numeric_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term equivalent = await image.Invoke(context, "run", Term.Tuple(Term.I(1)), new FloatTerm(1));
            Term different = await image.Invoke(context, "run", Term.Tuple(Term.I(1)), new FloatTerm(2));
            actual = Term.Tuple(equivalent, different);

            return actual;
        });
        Term exit = await process.Completion;
        if (!Term.Tuple(Term.Tuple(Term.A("false"), Term.A("true")), Term.A("no")).Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM numeric equality returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralSubtraction()
    {
        const string source = """
            -module(beam_general_subtract_probe).
            -export([run/2]).
            run({A}, B) when A - B > 0 -> -(A - B);
            run(_, _) -> no.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_subtract_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term number = await image.Invoke(context, "run", Term.Tuple(Term.I(5)), new FloatTerm(2.5));
            Term invalid = await image.Invoke(context, "run", Term.Tuple(Term.A("not_a_number")), Term.I(1));
            actual = Term.Tuple(number, invalid);

            return actual;
        });
        Term exit = await process.Completion;
        if (!Term.Tuple(new FloatTerm(-2.5), Term.A("no")).Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM subtraction returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareXorOperators()
    {
        const string source = """
            -module(beam_xor_probe).
            -export([bits/2,neg/1,bool/2,nested/2,g/2]).
            bits(A,B) -> A bxor B.
            neg(A) -> bnot A.
            bool(A,B) -> A xor B.
            nested({A},B) -> A xor B.
            g(A,B) when (A bxor B) =:= (bnot B) -> bits;
            g(A,B) when A xor B -> bool;
            g(_,_) -> no.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_xor_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term yes = Term.A("true");
            Term no = Term.A("false");
            Term bits = await image.Invoke(context, "bits", Term.I(5), Term.I(3));
            Term negated = await image.Invoke(context, "neg", Term.I(1));
            Term boolean = await image.Invoke(context, "bool", yes, no);
            Term nested = await image.Invoke(context, "nested", Term.Tuple(yes), yes);
            Term bitGuard = await image.Invoke(context, "g", Term.I(-1), Term.I(0));
            Term booleanGuard = await image.Invoke(context, "g", yes, no);
            Term guardFallback = await image.Invoke(context, "g", yes, Term.I(0));
            try
            {
                await image.Invoke(context, "bool", yes, Term.I(0));
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("badarg")))
            {
                actual = Term.Tuple(
                    bits,
                    negated,
                    boolean,
                    nested,
                    bitGuard,
                    booleanGuard,
                    guardFallback
                );
            }

            return actual ?? Nil.Value;
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(
            Term.I(6),
            Term.I(-2),
            Term.A("true"),
            Term.A("false"),
            Term.A("bits"),
            Term.A("bool"),
            Term.A("no")
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM xor probe returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareRangeAndNativeRecordPredicates()
    {
        const string source = """
            -module(beam_predicates_probe).
            -export([range/3,constant/1,record/1,value/3]).
            range(N,L,H) when is_integer(N,L,H) -> yes;
            range(_,_,_) -> no.
            constant(N) when is_integer(N,2,9) -> yes;
            constant(_) -> no.
            record(X) when is_record(X) -> yes;
            record(_) -> no.
            value(N,L,H) -> {is_integer(N,L,H),is_record(N)}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_predicates_probe.erl"));
        await using var runtime = new ProcessRuntime();
        var record = new NativeRecordTerm(Term.A("beam_predicates_probe"), Term.A("tag"), false, []);
        Term yes = Term.A("yes");
        Term no = Term.A("no");
        Term invalid = Term.A("invalid");
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term range = await image.Invoke(context, "range", Term.I(9), Term.I(2), Term.I(9));
            Term rejected = await image.Invoke(context, "range", Term.I(9), invalid, Term.I(9));
            Term constant = await image.Invoke(context, "constant", Term.I(2));
            Term floatRejected = await image.Invoke(context, "constant", new FloatTerm(2));
            Term native = await image.Invoke(context, "record", record);
            Term tuple = await image.Invoke(context, "record", Term.Tuple(Term.A("tag")));
            Term value = await image.Invoke(context, "value", record, Term.I(2), Term.I(9));
            try
            {
                await image.Invoke(context, "value", Term.I(9), invalid, Term.I(9));
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("badarg")))
            {
                actual = Term.Tuple(range, rejected, constant, floatRejected, native, tuple, value);
            }

            return actual ?? Nil.Value;
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(
            yes,
            no,
            yes,
            no,
            yes,
            no,
            Term.Tuple(Term.A("false"), Term.A("true"))
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM range and record predicates returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareZippedListGenerators()
    {
        const string source = """
            -module(beam_zip_list_probe).
            -export([relaxed/2,mixed/2]).
            relaxed(A,B) -> [{X,Y} || {ok,X} <- A && Y <- B, Y > 0].
            mixed(A,B) -> [X || {ok,X} <- A && X <:- B].
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_zip_list_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term one = Term.I(1);
        Term two = Term.I(2);
        Term nine = Term.I(9);
        Term eight = Term.I(8);
        Term first = Term.Tuple(Term.A("ok"), one);
        Term second = Term.Tuple(Term.A("ok"), two);
        Term skip = Term.A("skip");
        Term tail = Term.A("tail");
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            async ValueTask<Term> Invoke(string name, Term left, Term right)
            {
                try
                {
                    return await image.Invoke(context, name, left, right);
                }
                catch (ErlangException error)
                {
                    return error.Reason;
                }
            }

            actual = Term.Tuple(
                await Invoke("relaxed", Term.List(first, skip, second), Term.List(nine, eight, Term.I(0))),
                await Invoke("relaxed", Term.List(first), Term.List(nine, eight)),
                await Invoke("relaxed", new Cons(first, tail), Term.List(nine)),
                await Invoke("mixed", Term.List(skip, second), Term.List(one, two)),
                await Invoke("mixed", Term.List(first), Term.List(two)),
                await Invoke("mixed", Term.List(first, second), Term.List(one, two))
            );

            return actual;
        });
        Term exit = await process.Completion;
        static Term BadZip(Term left, Term right) => Term.Tuple(Term.A("bad_generators"), Term.Tuple(left, right));
        Term expected = Term.Tuple(
            Term.List(Term.Tuple(one, nine)),
            BadZip(Nil.Value, Term.List(eight)),
            BadZip(tail, Nil.Value),
            BadZip(Term.List(skip, second), Term.List(one, two)),
            BadZip(Term.List(first), Term.List(two)),
            Term.List(one, two)
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM list zip returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGuardExtraction()
    {
        const string source = """
            -module(beam_guard_extract).
            -export([tail/2,part/4]).
            tail({A},B) when tl(A) =:= B -> yes;
            tail(_,_) -> no.
            part({A},S,L,B) when binary_part(A,S,L) =:= B -> yes;
            part(_,_,_,_) -> no.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_guard_extract.erl"));
        await using var runtime = new ProcessRuntime();
        Term tail = Term.A("tail");
        Term yes = Term.A("yes");
        Term no = Term.A("no");
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            actual = Term.Tuple(
                await image.Invoke(context, "tail", Term.Tuple(new Cons(Term.I(1), tail)), tail),
                await image.Invoke(context, "tail", Term.Tuple(Nil.Value), Nil.Value),
                await image.Invoke(context, "part", Term.Tuple(new BitString([1, 2, 3])), Term.I(2), Term.I(-1), new BitString([2])),
                await image.Invoke(context, "part", Term.Tuple(new BitString([1, 2])), Term.I(2), Term.I(1), new BitString([]))
            );

            return actual;
        });
        Term exit = await process.Completion;
        if (!Term.Tuple(yes, no, yes, no).Equals(actual))
        {
            throw new InvalidOperationException($"BEAM guard extraction returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareInlineLocalBindings()
    {
        const string file = "beam_inline_bindings_probe.erl";
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(Encoding.UTF8.GetString(Read(file)), file));
        await using var runtime = new ProcessRuntime();
        Term? result = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            result = await image.Invoke(context, "run", Term.A("outer"), Term.List(Term.I(1)), Term.List(Term.I(2)));
            return result;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        if (!Term.Tuple(Term.I(1), Term.I(2), Term.A("outer")).Equals(result))
        {
            throw new InvalidOperationException($"Inline local bindings returned {result}, exit {exit}.");
        }
    }

    private static async Task CompareBinaryArgumentProof()
    {
        const string source = """
            -module(beam_binary_argument_proof).
            -export([run/1, mixed/1]).
            run(<<_:8, Payload/binary>>) -> forward(Payload).
            forward(Value) -> trim(Value).
            trim(<<C, Rest/binary>>) when C == 9; C == 32 -> trim(Rest);
            trim(Value) -> Value.
            mixed(Value) -> checked(Value).
            checked(<<C, Rest/binary>>) when C == 9; C == 32 -> checked(Rest);
            checked(Value) -> Value.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_binary_argument_proof.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        var other = Term.A("other");
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term trimmed = await image.Invoke(context, "run", new BitString([1, 9, 32, 65]));
            Term empty = await image.Invoke(context, "run", new BitString([1, 32]));
            Term preserved = await image.Invoke(context, "mixed", other);
            actual = Term.Tuple(trimmed, empty, preserved);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(new BitString([65]), new BitString([]), other);
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Binary argument proof returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareBinaryTailRecursion()
    {
        const string file = "beam_binary_tail_recursion.erl";
        string source = Encoding.UTF8.GetString(Read(file));
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, file));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        var unaligned = new BitString([32, 128], 9);
        Term atom = Term.A("atom");
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term trimmed = await image.Invoke(context, "trim", new BitString([9, 32, 13, 10, 65, 66]));
            Term empty = await image.Invoke(context, "trim", new BitString([32, 9]));
            Term bitstring = await image.Invoke(context, "trim", unaligned);
            Term passthrough = await image.Invoke(context, "trim", atom);
            actual = Term.Tuple(trimmed, empty, bitstring, passthrough);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(new BitString([65, 66]), new BitString([]), unaligned, atom);
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Binary tail recursion returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGuardedBinaryBodyMatch()
    {
        const string source = """
            -module(beam_guarded_body_binary).
            -export([split/2, optional/2, multiple/3]).
            split(Bin, Count) when is_binary(Bin), is_integer(Count) ->
                <<Part:Count/binary, Tail/binary>> = Bin,
                {Part,Tail}.
            optional(Bin, Count) when is_binary(Bin); is_atom(Bin) ->
                <<Part:Count/binary, Tail/binary>> = Bin,
                {Part,Tail}.
            multiple(Bin, First, Skip) when is_binary(Bin) ->
                case {First, Skip} of
                    {Count, Ignored} ->
                        <<Prefix:Count/binary, _:Ignored/binary, Middle:2/binary, Tail/binary>> = Bin,
                        {Prefix,Middle,Tail}
                end.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_guarded_body_binary.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        var input = new BitString([1, 2]);
        var shortInput = new BitString([1]);
        var atom = Term.A("atom");
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term split = await image.Invoke(context, "split", input, Term.I(1));
            Term shortFailure = await Failure("split", shortInput, 2);
            Term atomFailure = await Failure("optional", atom, 1);
            var fields = new BitString([1, 2, 3, 4, 5]);
            Term multiple = await image.Invoke(context, "multiple", fields, Term.I(1), Term.I(1));
            Term invalidSkip = await Failure("multiple", fields, 1, atom);
            Term shortSuffix = await Failure("multiple", input, 1, Term.I(1));
            actual = Term.Tuple(split, shortFailure, atomFailure, multiple, invalidSkip, shortSuffix);

            return actual;

            async Task<Term> Failure(string function, Term value, int count, Term? skip = null)
            {
                try
                {
                    return skip is null
                        ? await image.Invoke(context, function, value, Term.I(count))
                        : await image.Invoke(context, function, value, Term.I(count), skip);
                }
                catch (ErlangException error)
                {
                    return error.Reason;
                }
            }
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(
            Term.Tuple(shortInput, new BitString([2])),
            Term.Tuple(Term.A("badmatch"), shortInput),
            Term.Tuple(Term.A("badmatch"), atom),
            Term.Tuple(shortInput, new BitString([3, 4]), new BitString([5])),
            Term.Tuple(Term.A("badmatch"), new BitString([1, 2, 3, 4, 5])),
            Term.Tuple(Term.A("badmatch"), input)
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Guarded binary body match returned {actual}, exit {exit}.");
        }
    }
    private static async Task CompareGeneralFixedBitPatterns()
    {
        const string source = """
            -module(beam_general_fixed_bits_probe).
            -export([run/1, dynamic/1, scalar/1, literal/1]).
            run(<<A:4,B:12/little,R/binary>>) -> {A,B,R};
            run(_) -> no.
            dynamic(<<Width:8, Value:Width/signed, Rest/bitstring>>) -> {Value,Rest};
            dynamic(_) -> no.
            scalar(<<Code/utf8, Float:32/little-float>>) -> {Code,Float};
            scalar(_) -> no.
            literal(<<16#12345678:32,-2:8/signed,Value:8,Tail/binary>>) -> {Value,Tail};
            literal(_) -> no.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_fixed_bits_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term decoded = await image.Invoke(context, "run", new BitString([0xA2, 0x01, 0xFF]));
            Term shortInput = await image.Invoke(context, "run", new BitString([0xA2]));
            Term unaligned = await image.Invoke(context, "run", new BitString([0xA2, 0x01, 0x80], 17));
            Term dynamic = await image.Invoke(context, "dynamic", new BitString([0x03, 0xA0], 13));
            Term invalidWidth = await image.Invoke(context, "dynamic", new BitString([0xFF, 0]));
            Term scalar = await image.Invoke(context, "scalar", new BitString([0xC3, 0xA9, 0, 0, 0xC0, 0x3F]));
            Term trailing = await image.Invoke(context, "scalar", new BitString([0xC3, 0xA9, 0, 0, 0xC0, 0x3F, 0]));
            Term literal = await image.Invoke(context, "literal", new BitString([0x12, 0x34, 0x56, 0x78, 0xFE, 7, 8, 9]));
            Term wrongPrefix = await image.Invoke(context, "literal", new BitString([0x12, 0x34, 0x56, 0x79, 0xFE, 7]));
            Term wrongSign = await image.Invoke(context, "literal", new BitString([0x12, 0x34, 0x56, 0x78, 0xFD, 7]));
            actual = Term.Tuple(decoded, shortInput, unaligned, dynamic, invalidWidth, scalar, trailing, literal, wrongPrefix, wrongSign);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term no = Term.A("no");
        Term expected = Term.Tuple(
            Term.Tuple(Term.I(10), Term.I(288), new BitString([0xFF])), no, no,
            Term.Tuple(Term.I(-3), new BitString([0], 2)), no,
            Term.Tuple(Term.I(233), new FloatTerm(1.5)), no,
            Term.Tuple(Term.I(7), new BitString([8, 9])), no, no
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM fixed bit patterns returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralListQualifiers()
    {
        const string source = """
            -module(beam_general_list_qualifier_probe).
            -export([run/1, strict/1, key/2]).
            run(Rows) -> [{X,Y} || #{x := X, ys := Ys} <- Rows, Y <- Ys, Y > X].
            strict(Rows) -> [V || {ok,V} <:- Rows].
            key(K,Rows) -> [K || #{K:=K} <- Rows].
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_list_qualifier_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            var first = new MapTerm([new(Term.A("x"), Term.I(1)), new(Term.A("ys"), Term.List(Term.I(1), Term.I(2)))]);
            var second = new MapTerm([new(Term.A("x"), Term.I(3)), new(Term.A("ys"), Term.List(Term.I(4), Term.I(5)))]);
            Term values = await image.Invoke(context, "run", Term.List(first, Term.A("other"), second));
            Term mismatch;
            try
            {
                mismatch = await image.Invoke(context, "strict", Term.List(Term.A("other")));
            }
            catch (ErlangException error)
            {
                mismatch = error.Reason;
            }
            Term shadowed = await image.Invoke(context, "key", Term.A("x"), Term.List(first, second));
            actual = Term.Tuple(values, mismatch, shadowed);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term pairs = Term.List(Term.Tuple(Term.I(1), Term.I(2)), Term.Tuple(Term.I(3), Term.I(4)), Term.Tuple(Term.I(3), Term.I(5)));
        Term expected = Term.Tuple(pairs, Term.Tuple(Term.A("badmatch"), Term.A("other")), Term.List(Term.I(1), Term.I(3)));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM list qualifiers returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneratedBehaviourInfo()
    {
        const string source = "beam_callback_probe.erl";
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(Encoding.UTF8.GetString(Read(source)), source));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term callbacks = await image.Invoke(context, "behaviour_info", Term.A("callbacks"));
            Term optional = await image.Invoke(context, "behaviour_info", Term.A("optional_callbacks"));
            actual = Term.Tuple(callbacks, optional);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term first = Term.Tuple(Term.A("z"), Term.I(1));
        Term second = Term.Tuple(Term.A("a"), Term.I(0));
        Term expected = Term.Tuple(Term.List(first, second), Term.List(first));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Generated behaviour_info returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareEntryTuplePatterns()
    {
        const string source = """
            -module(beam_entry_tuple_probe).
            -export([run/3, keyfetch/2, forward/3, forward_pair/2, rebuild/3, nested/2, nested_dead/2, lookup_tag/1, projection/1, count/1, use_twice/1, retain/2, pick_closure/1, strict_closure/1, accumulate_bits/3]).
            run({tag, {Value}} = Tagged, {Other}, Options)
              when is_list(Options), is_tuple(Tagged), is_integer(Value) ->
                {Tagged, erlang:integer_to_binary(Value), Other}.
            keyfetch(Key, Entries) ->
                {Key,Value} = lists:keyfind(Key, 1, Entries),
                Value.
            forward(Key, Entries, Saved) ->
                {Key,A,B} = lists:keyfind(Key, 1, Entries),
                finish(A,Saved,B).
            forward_pair(Key, Entries) ->
                {Key,A,B} = lists:keyfind(Key, 1, Entries),
                finish_pair(A,B).
            rebuild(Key, Entries, Saved) ->
                {Key,A,B} = lists:keyfind(Key, 1, Entries),
                {B,Saved,A}.
            nested(Key, Entries) ->
                {Key,Value} = lists:keyfind(Key, 1, identity(Entries)),
                Value.
            nested_dead(Key, Entries) ->
                {_,Value} = lists:keyfind(Key, 1, identity(Entries)),
                Value.
            lookup_tag(Entries) ->
                case lists:keyfind(key, 1, identity(Entries)) of
                    false -> missing;
                    {key,Value} -> Value
                end.
            projection({gamma, _, X, _}) -> X;
            projection({beta, X, _, _}) -> X;
            projection({alpha, _, X, _}) -> X;
            projection({theta, _, X}) -> X;
            projection({delta, X, _}) -> X;
            projection({omega, X}) -> X;
            projection(_) -> missing.
            count({items, Values}) -> count(Values);
            count([]) -> 0;
            count([_|Tail]) -> 1+count(Tail).
            use_twice({repeat, Value}) -> finish_pair(Value, Value);
            use_twice(Value) -> finish_pair(Value, Value).
            retain({item, Value}, Flag) ->
                case identity(Value) of
                    yes -> finish_pair(Value, Flag);
                    no -> finish_pair(no, Flag)
                end.
            pick_closure({2,16,5}) -> fun(X) -> {five,X} end;
            pick_closure({2,16,9}) -> fun(X) -> {nine,X} end;
            pick_closure({2,16,1}) -> fun(X) -> {one,X} end;
            pick_closure(_) -> fun(X) -> {fallback,X} end.
            strict_closure({2,16,5}) -> fun() -> five end;
            strict_closure({2,16,9}) -> fun() -> nine end.
            accumulate_bits([], Trail, Acc) -> {Trail + 1, Acc};
            accumulate_bits([Bit|Rest], Trail, Acc) ->
                accumulate_bits(Rest, Trail - 1, (Bit bsl Trail) bor Acc).
            identity(Entries) -> Entries.
            finish(A,S,B) -> {A,S,B}.
            finish_pair(A,B) -> {A,B}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_entry_tuple_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term tagged = Term.Tuple(Term.A("tag"), Term.Tuple(Term.I(42)));
            Term plain = Term.Tuple(Term.A("other"));
            Term options = Term.List(Term.A("option"));
            Term expected = Term.Tuple(tagged, new BitString("42"u8), Term.A("other"));
            if (!expected.Equals(await image.Invoke(context, "run", tagged, plain, options)))
            {
                throw new InvalidOperationException("Tuple entry did not preserve bindings.");
            }

            (Term First, Term Second, Term Options)[] failures =
            [
                (Term.Tuple(Term.A("wrong"), Term.Tuple(Term.I(42))), plain, options),
                (tagged, Term.A("other"), options),
                (Term.Tuple(Term.A("tag"), Term.I(42)), plain, options),
                (Term.Tuple(Term.A("tag"), Term.Tuple(Term.A("wrong"))), plain, options),
                (tagged, plain, Term.A("bad_options"))
            ];
            foreach (var (first, second, third) in failures)
            {
                try
                {
                    await image.Invoke(context, "run", first, second, third);
                    throw new InvalidOperationException("Tuple entry accepted an invalid argument.");
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
                {
                }
            }

            Term key = Term.A("key");
            Term value = Term.I(7);
            if (!value.Equals(await image.Invoke(context, "keyfetch", key, Term.List(Term.Tuple(key, value)))))
            {
                throw new InvalidOperationException("Tuple call match did not return the projected field.");
            }

            Term longTuple = Term.Tuple(key, value, Term.A("extra"));
            foreach (var (entries, unmatched) in new[]
            {
                (Term.List(longTuple), longTuple),
                ((Term)Nil.Value, Term.A("false"))
            })
            {
                try
                {
                    await image.Invoke(context, "keyfetch", key, entries);
                    throw new InvalidOperationException("Tuple call match accepted an invalid result.");
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), unmatched)))
                {
                }
            }

            Term last = Term.A("last");
            Term retained = Term.A("saved");
            Term matchingEntries = Term.List(Term.Tuple(key, value, last));
            if (!Term.Tuple(value, retained, last).Equals(await image.Invoke(context, "forward", key, matchingEntries, retained))
                || !Term.Tuple(value, last).Equals(await image.Invoke(context, "forward_pair", key, matchingEntries)))
            {
                throw new InvalidOperationException("Tuple call match did not forward the projected fields.");
            }

            if (!Term.Tuple(last, retained, value).Equals(await image.Invoke(context, "rebuild", key, matchingEntries, retained)))
            {
                throw new InvalidOperationException("Tuple call match did not rebuild the projected tuple.");
            }

            Term pairEntries = Term.List(Term.Tuple(key, value));
            if (!value.Equals(await image.Invoke(context, "nested", key, pairEntries))
                || !value.Equals(await image.Invoke(context, "nested_dead", key, pairEntries)))
            {
                throw new InvalidOperationException("Tuple call match did not preserve its nested lookup result.");
            }

            if (!value.Equals(await image.Invoke(context, "lookup_tag", pairEntries))
                || !Term.A("missing").Equals(await image.Invoke(context, "lookup_tag", Nil.Value)))
            {
                throw new InvalidOperationException("Tagged lookup returned the wrong branch result.");
            }

            try
            {
                await image.Invoke(context, "lookup_tag", Term.List(longTuple));
                throw new InvalidOperationException("Tagged lookup accepted a tuple with the wrong arity.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("case_clause"), longTuple)))
            {
            }

            (Term Input, Term Result)[] projections =
            [
                (Term.Tuple(Term.A("gamma"), key, value, key), value),
                (Term.Tuple(Term.A("beta"), value, key, key), value),
                (Term.Tuple(Term.A("theta"), key, value), value),
                (Term.Tuple(Term.A("delta"), value, key), value),
                (Term.Tuple(Term.A("omega"), value), value),
                (Term.Tuple(Term.A("gamma"), value), Term.A("missing")),
                (Nil.Value, Term.A("missing"))
            ];
            foreach (var (input, result) in projections)
            {
                if (!result.Equals(await image.Invoke(context, "projection", input)))
                {
                    throw new InvalidOperationException("Grouped tagged tuple dispatch did not preserve the selected field.");
                }
            }

            Term items = Term.Tuple(Term.A("items"), Term.List(key, value));
            Term repeated = Term.Tuple(Term.A("repeat"), value);
            if (!Term.I(2).Equals(await image.Invoke(context, "count", items))
                || !Term.Tuple(value, value).Equals(await image.Invoke(context, "use_twice", repeated)))
            {
                throw new InvalidOperationException("Tuple projection did not preserve tail-call arguments.");
            }

            foreach (Term flag in new[] { Term.A("yes"), Term.A("no") })
            {
                Term result = await image.Invoke(
                    context,
                    "retain",
                    Term.Tuple(Term.A("item"), flag),
                    key
                );
                if (!Term.Tuple(flag, key).Equals(result))
                {
                    throw new InvalidOperationException("Tuple projection did not preserve a slot used by one branch.");
                }
            }

            Term validKey = Term.Tuple(Term.I(2), Term.I(16), Term.I(5));
            Term invalidKey = Term.Tuple(Term.I(2), Term.I(16), Term.I(7));
            var chosen = (FunctionTerm)await image.Invoke(context, "pick_closure", validKey);
            var fallback = (FunctionTerm)await image.Invoke(context, "pick_closure", invalidKey);
            if (!Term.Tuple(Term.A("five"), value).Equals(await chosen.Invoke(context, [value]))
                || !Term.Tuple(Term.A("fallback"), value).Equals(await fallback.Invoke(context, [value])))
            {
                throw new InvalidOperationException("Tuple key dispatch selected the wrong closure.");
            }

            try
            {
                await image.Invoke(context, "strict_closure", invalidKey);
                throw new InvalidOperationException("Tuple key dispatch accepted an unmatched key.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
            {
                if (error.StackTraceTerm is not Cons { Head: TupleTerm { Items: [_, _, var args, _] } }
                    || !args.Equals(Term.List(invalidKey)))
                {
                    throw new InvalidOperationException("Tuple key dispatch changed the function_clause argument.");
                }
            }

            Term bits = Term.List(Term.I(1), Term.I(0), Term.I(1));
            Term arithmeticResult = await image.Invoke(
                context,
                "accumulate_bits",
                bits,
                Term.I(2),
                Term.I(0)
            );
            if (!Term.Tuple(Term.I(0), Term.I(5)).Equals(arithmeticResult))
            {
                throw new InvalidOperationException("Frameless list arithmetic produced the wrong result.");
            }

            try
            {
                await image.Invoke(
                    context,
                    "accumulate_bits",
                    new Cons(Term.I(1), key),
                    Term.I(0),
                    Term.I(0)
                );
                throw new InvalidOperationException("Frameless list arithmetic accepted an improper tail.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
            {
                if (error.StackTraceTerm is not Cons { Head: TupleTerm { Items: [_, _, var args, _] } }
                    || !args.Equals(Term.List(key, Term.I(-1), Term.I(1))))
                {
                    throw new InvalidOperationException("Frameless list arithmetic changed the failed tail-call arguments.");
                }
            }

            actual = Term.A("ok");
            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        if (!Term.A("ok").Equals(actual))
        {
            throw new InvalidOperationException($"Tuple entry check exited with {exit}.");
        }
    }

    private static async Task CompareListCallerFacts()
    {
        const string fixture = "beam_list_caller_facts.erl";
        string source = Encoding.UTF8.GetString(Read(fixture));
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, fixture));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term known = await image.Invoke(context, "run", Term.I(3));
            Term unknown = await image.Invoke(context, "unknown", Term.List(Term.I(1)));
            Term bad = Term.A("bad");
            try
            {
                await image.Invoke(context, "unknown", new Cons(Term.I(1), bad));
                throw new InvalidOperationException("An unconstrained list caller accepted an improper tail.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
            {
                if (error.StackTraceTerm is not Cons { Head: TupleTerm { Items: [_, _, var args, _] } }
                    || !args.Equals(Term.List(bad, Term.I(1), Term.I(4))))
                {
                    throw new InvalidOperationException("List caller facts changed the failed tail-call arguments.");
                }
            }

            actual = Term.Tuple(known, unknown);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(Term.Tuple(Term.I(2), Term.I(0)), Term.Tuple(Term.I(2), Term.I(4)));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"List caller facts returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareStructuralListEntry()
    {
        const string source = """
            -module(beam_structural_list_entry).
            -export([run/2,strict/1,checked/1]).
            run([], Default) -> {empty,Default};
            run([{tag,{same,Value},Value}|Tail], Value) when is_integer(Value) ->
                Absolute = erlang:abs(Value),
                {Absolute,Tail};
            run([{tag,_,_}|Tail], _) -> {other,Tail};
            run(Input, Default) -> {fallback,Input,Default}.
            strict([{tag,[Value,Value]}|Tail]) -> {Value,Tail};
            strict([]) -> empty.
            checked(Items) -> checked_list([Item || Item <- Items]).
            checked_list([{tag,[Value,Value]}|Tail]) -> {Value,Tail};
            checked_list([]) -> empty.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_structural_list_entry.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term value = Term.I(-3);
            Term bad = Term.A("bad");
            Term tail = Term.List(Term.I(9));
            Term tag = Term.A("tag");
            Term input = new Cons(Term.Tuple(tag, Term.Tuple(Term.A("same"), value), value), tail);
            Term matched = await image.Invoke(context, "run", input, value);
            Term repeated = await image.Invoke(context, "run", input, Term.I(4));
            Term guarded = await image.Invoke(context, "run",
                Term.List(Term.Tuple(tag, Term.Tuple(Term.A("same"), bad), bad)), bad);
            Term empty = await image.Invoke(context, "run", Nil.Value, bad);
            Term fallback = await image.Invoke(context, "run", bad, value);
            Term strict = await image.Invoke(context, "strict", new Cons(Term.Tuple(tag, Term.List(value, value)), tail));
            Term wrong = new Cons(Term.Tuple(tag, Term.List(value, bad)), tail);
            try
            {
                await image.Invoke(context, "strict", wrong);
                throw new InvalidOperationException("A repeated binding accepted a different list element.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
            {
                if (error.StackTraceTerm is not Cons { Head: TupleTerm { Items: [_, _, var args, _] } }
                    || !args.Equals(Term.List(wrong)))
                {
                    throw new InvalidOperationException("The failed list clause lost its original argument.");
                }
            }

            Term checkedEmpty = await image.Invoke(context, "checked", Nil.Value);
            Term checkedValue = await image.Invoke(context, "checked",
                Term.List(Term.Tuple(tag, Term.List(value, value))));
            if (!checkedEmpty.Equals(Term.A("empty")) || !checkedValue.Equals(Term.Tuple(value, Nil.Value)))
            {
                throw new InvalidOperationException("The proper-list entry returned the wrong result.");
            }

            try
            {
                await image.Invoke(context, "checked", Term.List(Term.Tuple(tag, Term.List(value, bad))));
                throw new InvalidOperationException("A nonempty list with a failed head match took the empty-list branch.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
            {
            }

            actual = Term.Tuple(matched, repeated, guarded, empty, fallback, strict);
            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(
            Term.Tuple(Term.I(3), Term.List(Term.I(9))),
            Term.Tuple(Term.A("other"), Term.List(Term.I(9))),
            Term.Tuple(Term.A("other"), Nil.Value),
            Term.Tuple(Term.A("empty"), Term.A("bad")),
            Term.Tuple(Term.A("fallback"), Term.A("bad"), Term.I(-3)),
            Term.Tuple(Term.I(-3), Term.List(Term.I(9)))
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Structural list entry returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareConsResultEntry()
    {
        const string source = """
            -module(beam_cons_result_entry).
            -export([copy/1,mapped/1,wrapped/1]).
            copy([Head|Tail]) -> [Head|copy(Tail)];
            copy([]) -> [].
            mapped([Head|Tail]) -> [step(Head)|mapped(Tail)];
            mapped([]) -> [].
            wrapped([Head|Tail]) -> [{value,Head}|wrapped(Tail)];
            wrapped([]) -> [].
            step(Value) -> {mapped,Value}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_cons_result_entry.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        Term one = Term.I(1);
        Term two = Term.I(2);
        Term list = Term.List(one, two);
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term copied = await image.Invoke(context, "copy", list);
            Term mapped = await image.Invoke(context, "mapped", list);
            Term wrapped = await image.Invoke(context, "wrapped", list);
            Term empty = await image.Invoke(context, "copy", Nil.Value);
            actual = Term.Tuple(copied, mapped, wrapped, empty);
            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(
            list,
            Term.List(Term.Tuple(Term.A("mapped"), one), Term.Tuple(Term.A("mapped"), two)),
            Term.List(Term.Tuple(Term.A("value"), one), Term.Tuple(Term.A("value"), two)),
            Nil.Value
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Cons result entry returned {actual}, exit {exit}.");
        }
    }

    private static void CompareFixture(string module)
    {
        string source = module + ".erl";
        byte[] actual = CodeGeneration.CompileBeamModule(Encoding.UTF8.GetString(Read(source)), source);
        byte[] expected = Read(module + ".beam");
        if (!actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(Mismatch + module);
        }
    }

    internal static Task CompareAtomTailDispatch()
    {
        CompareFixture("beam_atom_tail_dispatch_probe");

        return Task.CompletedTask;
    }

    internal static async Task CompareCallResultBif()
    {
        CompareFixture("beam_call_result_bif");
        const string source = """
            -module(beam_call_result_bif).
            -export([prefix/2,details/2,unchecked/2]).
            prefix(Input, Limit) ->
                Binary = iolist_to_binary(Input),
                binary_part(Binary, 0, min(Limit, byte_size(Binary))).
            details(Input, Keep) ->
                Reversed = lists:reverse(Input),
                pack(length(Reversed), Reversed, Keep).
            unchecked(Input, Limit) ->
                Value = identity(Input),
                binary_part(Value, 0, min(Limit, byte_size(Value))).
            identity(Value) -> Value.
            pack(Length, Value, Keep) -> {Length, Value, Keep}.
            """;
        BeamModule image = BeamModule.Prepare(
            CodeGeneration.CompileBeamModule(source, "beam_call_result_bif.erl"),
            new BeamDependencySelection([ListBeamHostNames.Module])
        );
        await using var runtime = new ProcessRuntime();
        _ = ListBeamHostChecks.Select(runtime);
        Term? actual = null;
        var input = new BitString(new byte[] { 1, 2, 3 });
        var prefix = new BitString(new byte[] { 1, 2 });
        var marker = Term.A("unbounded");
        var list = Term.List(Term.I(1), Term.I(2), Term.I(3));
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term truncated = await image.Invoke(context, "prefix", input, Term.I(2));
            Term unbounded = await image.Invoke(context, "prefix", input, marker);
            Term described = await image.Invoke(context, "details", list, marker);
            Term checkedPrefix = await image.Invoke(context, "unchecked", input, Term.I(2));

            await ExpectBadarg("prefix", input, Term.I(-9));
            await ExpectBadarg("prefix", input, new FloatTerm(1.5));
            await ExpectBadarg("unchecked", marker, Term.I(1));
            actual = Term.Tuple(truncated, unbounded, described, checkedPrefix);
            return actual;

            async Task ExpectBadarg(string function, Term value, Term limit)
            {
                try
                {
                    await image.Invoke(context, function, value, limit);
                    throw new InvalidOperationException($"Call result BIF accepted invalid input in {function}.");
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A("badarg")))
                {
                }
            }
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(prefix, input,
            Term.Tuple(Term.I(3), Term.List(Term.I(3), Term.I(2), Term.I(1)), marker), prefix);
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Call result BIF returned {actual}, exit {exit}.");
        }
    }

    internal static async Task CompareEntryResultCase()
    {
        const string file = "beam_entry_result_case.erl";
        string source = Encoding.UTF8.GetString(Read(file));
        BeamModule image = BeamModule.Prepare(
            CodeGeneration.CompileBeamModule(source, file),
            new BeamDependencySelection([ListBeamHostNames.Module])
        );
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        Exception? invocationError = null;
        Term accepted = Term.A("accepted");
        Term rejected = Term.A("rejected");
        Term other = Term.A("other");
        Term tag = Term.A("tag");
        Term tuple = Term.Tuple(tag, Term.I(42));
        ProcessHandle process = runtime.Spawn(async context =>
        {
            try
            {
                Term tab = new BitString([9]);
                Term bang = new BitString("!"u8);
                Term swapped = await image.Invoke(
                    context,
                    "binary_tuple_tail",
                    tab,
                    Term.List(Term.Tuple(tag, accepted, rejected))
                );
                Term retained = await image.Invoke(
                    context,
                    "binary_tuple_tail",
                    bang,
                    Term.List(Term.Tuple(accepted, rejected))
                );
                Term skipped = await image.Invoke(
                    context,
                    "binary_tuple_tail",
                    new BitString("?"u8),
                    other
                );
                if (!swapped.Equals(Term.Tuple(rejected, accepted)) || !retained.Equals(Term.Tuple(accepted, rejected))
                    || !skipped.Equals(Term.A("skipped")))
                {
                    throw new InvalidOperationException("Binary tuple tail did not preserve argument order or branch fallback.");
                }

                try
                {
                    await image.Invoke(context, "binary_tuple_tail", tab, Term.List(other));
                    throw new InvalidOperationException("Binary tuple tail accepted a non-tuple result.");
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), other)))
                {
                }
            }
            catch (Exception error)
            {
                invocationError = error;
                throw;
            }

            Term matchedTuple = Term.Tuple(Term.Tuple(other, accepted), Term.Tuple(tag, tag));
            Term mismatchedTuple = Term.Tuple(Term.Tuple(other, accepted), Term.Tuple(tag, other));
            try
            {
                Term nestedFields = await image.Invoke(context, "nested_tuple_unused", Term.List(matchedTuple));
                if (!nestedFields.Equals(Term.Tuple(accepted, tag)))
                {
                    throw new InvalidOperationException("Removing unused nested fields changed the live bindings.");
                }

                try
                {
                    await image.Invoke(context, "nested_tuple_unused", Term.List(mismatchedTuple));
                    throw new InvalidOperationException("Removing unused nested fields skipped a repeated binding check.");
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), mismatchedTuple)))
                {
                }
            }
            catch (Exception error)
            {
                invocationError = error;
                throw;
            }

            Term chosen = await image.Invoke(context, "choose", Term.A("one"), accepted);
            Term denied = await image.Invoke(context, "choose", Term.A("two"), accepted);
            Term bound = await image.Invoke(context, "bound", other, accepted);
            Term matches = await image.Invoke(context, "preserve", tuple, tuple);
            Term fallback = await image.Invoke(context, "preserve", tuple, Term.Tuple(tag, Term.I(43)));
            Term survived = await image.Invoke(context, "survive", tuple, tuple);
            Term seen = await context.Runtime.Modules.Call(context, "erlang", "get", Term.A("seen"));
            try
            {
                await image.Invoke(context, "choose", other, accepted);
                throw new InvalidOperationException("A missing case arm accepted the selector.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("case_clause"), other)))
            {
            }

            Term nested = await image.Invoke(context, "nested", tuple, accepted);
            Term failed = Term.Tuple(Term.A("failed"), other);
            try
            {
                await image.Invoke(context, "nested", Term.Tuple(Term.A("fail"), other), accepted);
                throw new InvalidOperationException("The nested selector did not raise the branch error.");
            }
            catch (ErlangException error) when (error.Reason.Equals(failed))
            {
            }

            foreach (Term unmatched in new[] { Term.Tuple(other, Term.I(42)), Term.Tuple(tag, Term.I(42), other) })
            {
                try
                {
                    await image.Invoke(context, "nested", unmatched, accepted);
                    throw new InvalidOperationException("The tagged selector accepted an unmatched tuple.");
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("case_clause"), unmatched)))
                {
                }
            }

            Term atomChars = await image.Invoke(context, "atom_chars");
            Term atomResult = await image.Invoke(context, "atom_result", Term.A("one"));
            try
            {
                await image.Invoke(context, "atom_result", Term.A("two"));
                throw new InvalidOperationException("The atom case accepted an unmatched result.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("case_clause"), Term.A("no"))))
            {
            }

            Term boundary = new FloatTerm(64.0);
            Term above = new FloatTerm(64.5);
            Term smaller = await image.Invoke(context, "size_branch", tag, boundary);
            Term larger = await image.Invoke(context, "size_branch", tag, above);
            Term atomSize = await image.Invoke(context, "size_branch", tag, other);
            if (!smaller.Equals(Term.Tuple(tag, boundary))
                || !larger.Equals(Term.List(Term.Tuple(tag, above)))
                || !atomSize.Equals(Term.List(Term.Tuple(tag, other))))
            {
                throw new InvalidOperationException("The complementary guards returned the wrong branch result.");
            }

            try
            {
                await image.Invoke(context, "size_gap", tag, boundary);
                throw new InvalidOperationException("The comparison guards accepted a value in the gap.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
            {
            }

            Term payload = new BitString("measure"u8);
            Term measured = await image.Invoke(context, "measure", payload, above);
            if (!measured.Equals(Term.Tuple(Term.I(7), payload)))
            {
                throw new InvalidOperationException("The measured call result was not preserved.");
            }

            try
            {
                await image.Invoke(context, "measure", other, above);
                throw new InvalidOperationException("byte_size accepted an atom result.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("badarg")))
            {
            }

            Term large = await image.Invoke(context, "arithmetic_guard", Term.I(5), Term.I(1));
            Term negative = await image.Invoke(context, "arithmetic_guard", Term.I(1), Term.I(5));
            Term badArithmetic = await image.Invoke(context, "arithmetic_guard", other, Term.I(1));
            if (!large.Equals(Term.A("large")) || !negative.Equals(Term.A("negative")) || !badArithmetic.Equals(other))
            {
                throw new InvalidOperationException("Arithmetic guard comparison returned the wrong branch.");
            }

            Term prefix = await image.Invoke(context, "split_prefix", payload, Term.I(3));
            if (!prefix.Equals(Term.Tuple(Term.List(new BitString("mea"u8)), Term.I(3))))
            {
                throw new InvalidOperationException("The split result returned the wrong prefix.");
            }

            try
            {
                await image.Invoke(context, "split_prefix", payload, Term.I(8));
                throw new InvalidOperationException("split_binary accepted an out-of-range size.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("badarg")))
            {
            }

            Term tagged = await image.Invoke(context, "guarded_shape", tuple, tag);
            Term identical = await image.Invoke(context, "guarded_shape", payload, payload);
            Term distinct = await image.Invoke(context, "guarded_shape", tuple, Term.I(42));
            if (!tagged.Equals(Term.Tuple(tag, tuple)) || !identical.Equals(Term.List(payload)) || !distinct.Equals(other))
            {
                throw new InvalidOperationException("Type and equality guards selected the wrong branch.");
            }

            Term selected = await image.Invoke(context, "tuple_entry", Term.Tuple(Term.A("choose"), Term.A("one")), accepted);
            Term declined = await image.Invoke(context, "tuple_entry", Term.Tuple(Term.A("choose"), Term.A("two")), accepted);
            Term invalidShape = Term.Tuple(Term.A("choose"), Term.A("one"), other);
            Term original = await image.Invoke(context, "tuple_entry", invalidShape, accepted);
            if (!selected.Equals(Term.Tuple(Term.A("yes"), accepted))
                || !declined.Equals(Term.Tuple(other, Term.A("two"))) || !original.Equals(Term.Tuple(invalidShape, accepted)))
            {
                throw new InvalidOperationException("Tuple dispatch selected the wrong branch or lost an input.");
            }

            Term seven = Term.Tuple(Term.A("item"), Term.I(7), Term.A("one"));
            Term eight = Term.Tuple(Term.A("item"), Term.I(8), other);
            Term failedGuard = Term.Tuple(Term.A("item"), Term.I(7), Term.I(42));
            Term inexact = Term.Tuple(Term.A("item"), new FloatTerm(7.0), Term.A("one"));
            if (!(await image.Invoke(context, "tuple_alias", seven, accepted)).Equals(Term.Tuple(Term.A("yes"), seven, accepted))
                || !(await image.Invoke(context, "tuple_alias", eight, accepted)).Equals(Term.Tuple(other, eight, accepted))
                || !(await image.Invoke(context, "tuple_alias", failedGuard, accepted)).Equals(Term.Tuple(Term.I(42), failedGuard, accepted))
                || !(await image.Invoke(context, "tuple_alias", inexact, accepted)).Equals(Term.Tuple(Term.A("fallback"), inexact, accepted)))
            {
                throw new InvalidOperationException("Tuple alias dispatch lost the original value, skipped a repeated key, or accepted an inexact match.");
            }

            (Term Input, Term Expected)[] literalCases =
            [
                (Term.I(-1), new BitString([255])),
                (Term.I(0), new BitString([0])),
                (Term.I(2), Term.I(2)),
                (new FloatTerm(2.0), Term.A("float_value")),
                (new FloatTerm(-1.0), Term.A("negative")),
                (new FloatTerm(0.0), other),
                (other, other)
            ];
            foreach (var (input, expectedLiteral) in literalCases)
            {
                Term result = await image.Invoke(context, "literal_entry", input);
                if (!result.Equals(expectedLiteral))
                {
                    throw new InvalidOperationException("Literal dispatch skipped an eligible fallback or used numeric equality.");
                }
            }

            Term firstTagged = Term.Tuple(Term.A("first"), Term.A("one"), Term.I(7));
            Term rejectedTagGuard = Term.Tuple(Term.A("first"), Term.I(1), Term.I(7));
            Term secondTagged = Term.Tuple(Term.A("second"), Term.I(1), Term.I(8));
            Term wrongTagArity = Term.Tuple(Term.A("second"), Term.I(1));
            if (!(await image.Invoke(context, "tuple_tag", firstTagged, accepted)).Equals(Term.Tuple(Term.A("yes"), firstTagged, accepted))
                || !(await image.Invoke(context, "tuple_tag", rejectedTagGuard, accepted)).Equals(Term.Tuple(Term.I(7), accepted))
                || !(await image.Invoke(context, "tuple_tag", secondTagged, accepted)).Equals(Term.Tuple(Term.I(8), accepted))
                || !(await image.Invoke(context, "tuple_tag", wrongTagArity, accepted)).Equals(Term.Tuple(wrongTagArity, accepted)))
            {
                throw new InvalidOperationException("Tuple tag dispatch skipped a later clause or projected the wrong field.");
            }

            Term tupleContext = Term.Tuple(Term.A("context"), accepted);
            Term alternateContext = Term.Tuple(Term.A("alternate"), other);
            Term shorter = Term.Tuple(Term.A("first"), Term.A("one"));
            Term longer = Term.Tuple(Term.A("first"), Term.I(1), Term.I(7));
            (Term First, Term Second, Term Expected)[] arityCases =
            [
                (Term.A("probe"), tupleContext, Term.Tuple(Term.A("probe"), tupleContext)),
                (shorter, tupleContext, Term.Tuple(Term.A("yes"), accepted)),
                (shorter, alternateContext, Term.Tuple(Term.A("one"), alternateContext)),
                (Term.Tuple(Term.A("first"), Term.I(1)), tupleContext, Term.Tuple(Term.I(1), tupleContext)),
                (longer, tupleContext, Term.Tuple(Term.I(1), Term.I(7), accepted)),
                (secondTagged, alternateContext, Term.Tuple(Term.I(1), Term.I(8), other)),
                (secondTagged, tupleContext, Term.Tuple(secondTagged, tupleContext))
            ];
            foreach (var (firstInput, secondInput, expectedArity) in arityCases)
            {
                Term result = await image.Invoke(context, "tuple_arities", firstInput, secondInput);
                if (!result.Equals(expectedArity))
                {
                    throw new InvalidOperationException("Tuple arity dispatch lost a secondary test or skipped a later clause.");
                }
            }

            Term fallbackSelector = Term.Tuple(Term.A("first"), Term.I(42));
            Term swappedSelector = Term.Tuple(Term.A("third"), Term.I(1), Term.I(8));
            (Term Selector, Term Choice)[] savedCases =
            [
                (shorter, Term.Tuple(Term.A("ok"), Term.A("one"))),
                (fallbackSelector, Term.Tuple(other, Term.I(42))),
                (secondTagged, Term.Tuple(Term.I(1), Term.I(8))),
                (swappedSelector, Term.Tuple(Term.I(8), Term.I(1)))
            ];
            foreach (var (selector, choice) in savedCases)
            {
                Term result = await image.Invoke(context, "saved_tuple_case", selector, accepted);
                if (!result.Equals(Term.Tuple(choice, selector)))
                {
                    throw new InvalidOperationException("Saved tuple dispatch selected the wrong arm or lost the selector.");
                }
            }

            try
            {
                await image.Invoke(context, "saved_tuple_case", wrongTagArity, accepted);
                throw new InvalidOperationException("Saved tuple dispatch accepted an unmatched arity.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("case_clause"), wrongTagArity)))
            {
            }

            Term exactSelector = Term.Tuple(Term.A("item"), Term.I(7), accepted);
            Term floatKey = new FloatTerm(7.0);
            Term floatSelector = Term.Tuple(Term.A("item"), floatKey, accepted);
            Term valueSelector = Term.Tuple(Term.A("value"), accepted);
            (Term Selector, Term Choice)[] savedCalls =
            [
                (exactSelector, Term.Tuple(accepted, Term.I(7))),
                (floatSelector, Term.Tuple(floatKey, accepted)),
                (valueSelector, accepted)
            ];
            foreach (var (selector, choice) in savedCalls)
            {
                Term result = await image.Invoke(context, "saved_tuple_call", selector, Term.I(7));
                if (!result.Equals(Term.Tuple(choice, selector)))
                {
                    throw new InvalidOperationException("Saved tuple payload projection lost an exact test or call input.");
                }
            }

            Term taggedBinary = Term.Tuple(Term.A("item"), new BitString("AB"u8));
            Term wrongTupleArity = Term.Tuple(Term.A("item"), accepted, other);
            Term nestedTail = await image.Invoke(context, "tagged_nested_tail", Term.List(taggedBinary), accepted);
            Term unmatchedTail = await image.Invoke(context, "tagged_nested_tail", Term.List(wrongTupleArity), accepted);
            if (!nestedTail.Equals(Term.Tuple(Term.String("AB"), accepted)) || !unmatchedTail.Equals(wrongTupleArity))
            {
                throw new InvalidOperationException("A tagged case lost the field or a saved argument during the nested call.");
            }

            Term invalidBinary = Term.Tuple(Term.A("item"), rejected);
            try
            {
                await image.Invoke(context, "tagged_nested_tail", Term.List(invalidBinary), accepted);
                throw new InvalidOperationException("The nested conversion accepted a non-binary field.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("badarg")))
            {
            }

            Term taggedList = Term.List(Term.Tuple(Term.A("item"), Term.List(accepted)));
            Term found = await image.Invoke(context, "tagged_nested_case", taggedList, other, accepted);
            Term missing = await image.Invoke(context, "tagged_nested_case", taggedList, other, rejected);
            Term bypassed = await image.Invoke(context, "tagged_nested_case", Term.List(wrongTupleArity), other, accepted);
            if (!found.Equals(Term.Tuple(Term.A("found"), other))
                || !missing.Equals(Term.Tuple(Term.A("missing"), other)) || !bypassed.Equals(wrongTupleArity))
            {
                throw new InvalidOperationException("A nested case lost its saved argument or selected the wrong branch.");
            }

            Term foundWithList = await image.Invoke(context, "tagged_nested_retained", taggedList, other, accepted);
            Term missingWithList = await image.Invoke(context, "tagged_nested_retained", taggedList, other, rejected);
            if (!foundWithList.Equals(Term.Tuple(Term.A("found"), Term.List(accepted), other))
                || !missingWithList.Equals(Term.Tuple(Term.A("missing"), Term.List(accepted), other)))
            {
                throw new InvalidOperationException("A field used by a nested case did not survive the selector call.");
            }

            Term unchanged = Term.Tuple(Term.A("error"), rejected);
            Term identityInput = Term.List(Term.Tuple(Term.A("item"), unchanged));
            Term identity = await image.Invoke(context, "tagged_nested_identity", identityInput, Term.A("item"), other);
            Term identityFallback = await image.Invoke(context, "tagged_nested_identity", identityInput, rejected, other);
            Term wrongTag = Term.List(Term.Tuple(rejected, unchanged));
            Term wrongTagFallback = await image.Invoke(context, "tagged_nested_identity", wrongTag, rejected, other);
            if (!identity.Equals(unchanged) || !identityFallback.Equals(other) || !wrongTagFallback.Equals(other))
            {
                throw new InvalidOperationException("Folding a nested identity case changed the selector result.");
            }

            Term needle = Term.Tuple(accepted, Term.List(accepted));
            Term computedFound = await image.Invoke(context, "tagged_nested_computed", taggedList, Term.List(needle), accepted);
            Term computedMissing = await image.Invoke(context, "tagged_nested_computed", taggedList, Term.List(needle), rejected);
            if (!computedFound.Equals(Term.Tuple(Term.A("found"), Term.List(accepted)))
                || !computedMissing.Equals(Term.A("missing")))
            {
                throw new InvalidOperationException("A nested selector lost a field while preparing its constructed argument.");
            }

            Term values = Term.List(accepted);
            Term entryInput = Term.List(Term.Tuple(Term.A("item"), Term.Tuple(Term.A("values"), values), other), rejected);
            try
            {
                Term entryHit = await image.Invoke(context, "deferred_entry_case", entryInput, accepted);
                Term entryMiss = await image.Invoke(context, "deferred_entry_case", entryInput, other);
                Term emptyEntry = await image.Invoke(context, "deferred_entry_case", Nil.Value, accepted);
                Term unmatchedInput = Term.List(Term.Tuple(Term.A("item"), rejected, other), accepted);
                Term unmatchedEntry = await image.Invoke(context, "deferred_entry_case", unmatchedInput, accepted);
                if (!entryHit.Equals(Term.Tuple(other, values)) || !entryMiss.Equals(Term.List(rejected))
                    || !emptyEntry.Equals(Term.A("empty")) || !unmatchedEntry.Equals(Term.List(accepted)))
                {
                    throw new InvalidOperationException("Deferring an entry tuple field lost the selector operand or a later field.");
                }
            }
            catch (Exception error)
            {
                invocationError = error;
                throw;
            }

            actual = Term.Tuple(chosen, denied, bound, matches, fallback, survived, seen, nested, atomChars, atomResult);
            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(accepted, rejected, Term.Tuple(rejected, accepted), Term.I(42), tuple,
            Term.Tuple(Term.I(42), tuple), Term.I(42), Term.Tuple(Term.I(42), accepted),
            Term.String("\U0001f642"), Term.A("yes"));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Call result case returned {actual}, exit {exit}.", invocationError);
        }
    }

    private static async Task CompareGeneralBodyFallback()
    {
        const string source = """
            -module(beam_general_body_fallback_probe).
            -export([convert/1, pair/1]).
            convert(X) -> erlang:integer_to_binary(X+1).
            pair(X) -> {X,X+1}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_body_fallback_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term converted = await image.Invoke(context, "convert", Term.I(41));
            Term pair = await image.Invoke(context, "pair", Term.I(41));
            actual = Term.Tuple(converted, pair);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(new BitString("42"u8), Term.Tuple(Term.I(41), Term.I(42)));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM body fallback returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralNodeAndSizeGuards()
    {
        const string source = """
            -module(beam_general_node_size_probe).
            -export([run/3]).
            run(P,B,F) when node(P) =:= node(), is_float(F), byte_size(B) > 1 -> accepted;
            run(_,_,_) -> rejected.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_node_size_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            var bytes = new BitString([1, 2]);
            var floating = new FloatTerm(1.5);
            Term accepted = await image.Invoke(context, "run", context.Self, bytes, floating);
            Term badNode = await image.Invoke(context, "run", Term.A("other"), bytes, floating);
            Term badBinary = await image.Invoke(context, "run", context.Self, Term.A("other"), floating);
            Term badFloat = await image.Invoke(context, "run", context.Self, bytes, Term.I(1));
            actual = Term.Tuple(accepted, badNode, badBinary, badFloat);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(Term.A("accepted"), Term.A("rejected"), Term.A("rejected"), Term.A("rejected"));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM node and size guards returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareBodyBitstringSizes()
    {
        const string source = """
            -module(beam_body_size).
            -export([run/1, shadow/1]).
            -compile({no_auto_import,[byte_size/1]}).
            run(Input) -> {erlang:byte_size(Input), bit_size(Input)}.
            shadow(Input) -> {local, byte_size(Input)}.
            byte_size(Input) -> {shadow, Input}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_body_size.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            var input = new BitString([0b1010_0000], 3);
            Term sizes = await image.Invoke(context, "run", input);
            Term shadow = await image.Invoke(context, "shadow", input);
            try
            {
                await image.Invoke(context, "run", Term.A("invalid"));
                throw new InvalidOperationException("Bitstring size accepted an atom.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("badarg")))
            {
            }

            actual = Term.Tuple(sizes, shadow);
            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(Term.Tuple(Term.I(1), Term.I(3)),
            Term.Tuple(Term.A("local"), Term.Tuple(Term.A("shadow"), new BitString([0b1010_0000], 3))));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Body bitstring sizes returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralTryAfter()
    {
        const string source = """
            -module(beam_general_try_after_probe).
            -export([run/2, handled/0]).
            run(Mode,Cleanup) ->
                try body(Mode)
                after
                    erlang:put(cleanup,Cleanup),
                    case Cleanup of
                        fail -> erlang:error(cleanup_failed);
                        ok -> ignored
                    end
                end.
            body(normal) -> value;
            body(fail) -> erlang:error(original).
            handled() ->
                try erlang:error(original)
                catch error:original -> handled
                after erlang:put(cleanup,handled)
                end.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_try_after_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        Exception? unexpected = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term cleanup = Term.A("cleanup");
            async ValueTask<Term> Run(string mode, string finalizer)
            {
                Term result;
                try
                {
                    result = await image.Invoke(context, "run", Term.A(mode), Term.A(finalizer));
                }
                catch (ErlangException error)
                {
                    result = error.Reason;
                }

                return Term.Tuple(result, context.Dictionary[cleanup]);
            }

            try
            {
                Term normal = await Run("normal", "ok");
                Term original = await Run("fail", "ok");
                Term replaced = await Run("fail", "fail");
                Term handled = await image.Invoke(context, "handled");
                actual = Term.Tuple(normal, original, replaced, Term.Tuple(handled, context.Dictionary[cleanup]));

                return actual;
            }
            catch (Exception error)
            {
                unexpected = error;
                throw;
            }
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(
            Term.Tuple(Term.A("value"), Term.A("ok")),
            Term.Tuple(Term.A("original"), Term.A("ok")),
            Term.Tuple(Term.A("cleanup_failed"), Term.A("fail")),
            Term.Tuple(Term.A("handled"), Term.A("handled"))
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM try after returned {actual}, exit {exit}.", unexpected);
        }
    }

    private static async Task CompareGeneralDivisionAndListOperators()
    {
        const string source = """
            -module(beam_general_div_list_probe).
            -export([run/4]).
            run(A,B,L,R) when A / B > 0 ->
                {A div B, A / B, L -- R, A bsl B, A rem B};
            run(_,_,_,_) -> rejected.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_div_list_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term left = Term.List(Term.I(1), Term.I(2), Term.I(1));
            Term right = Term.List(Term.I(1));
            Term result = await image.Invoke(context, "run", Term.I(7), Term.I(2), left, right);
            Term zero = await image.Invoke(context, "run", Term.I(7), Term.I(0), left, right);
            actual = Term.Tuple(result, zero);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term values = Term.Tuple(Term.I(3), new FloatTerm(3.5), Term.List(Term.I(2), Term.I(1)), Term.I(28), Term.I(1));
        Term expected = Term.Tuple(values, Term.A("rejected"));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM division and list operators returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralNumberMapHeadGuards()
    {
        const string source = """
            -module(beam_general_number_map_head_probe).
            -export([run/2]).
            run(M,L) when is_map(M), is_number(hd(L)), hd(L) > 0 -> accepted;
            run(_,_) -> rejected.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_number_map_head_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            var map = new MapTerm([]);
            Term improper = new Cons(new FloatTerm(1.5), Term.A("tail"));
            Term accepted = await image.Invoke(context, "run", map, improper);
            Term wrongMap = await image.Invoke(context, "run", Term.A("other"), improper);
            Term empty = await image.Invoke(context, "run", map, Term.List());
            Term notNumber = await image.Invoke(context, "run", map, Term.List(Term.A("other")));
            actual = Term.Tuple(accepted, wrongMap, empty, notNumber);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(Term.A("accepted"), Term.A("rejected"), Term.A("rejected"), Term.A("rejected"));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM number, map and head guards returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralBitExpressions()
    {
        const string source = """
            -module(beam_general_bit_emit_probe).
            -export([run/3]).
            run(B,X,N) -> {ok, <<B/bitstring, X:N, "x">>}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_bit_emit_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            actual = await image.Invoke(context, "run", new BitString([0xA0], 4), Term.I(3), Term.I(4));

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(Term.A("ok"), new BitString([0xA3, 0x78]));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM bit expression returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareFinalBinaryCall()
    {
        const string source = """
            -module(beam_final_binary_call).
            -export([run/1, last/1]).
            run(Input) ->
                <<17:8, (last(Input))/binary>>.
            last(Input) ->
                Input.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_final_binary_call.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            actual = await image.Invoke(context, "run", new BitString([0xA0]));
            try
            {
                await image.Invoke(context, "run", Term.A("invalid"));
                throw new InvalidOperationException("Binary construction accepted an atom.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("badarg")))
            {
            }

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        if (!new BitString([17, 0xA0]).Equals(actual) || !Term.A("normal").Equals(exit))
        {
            throw new InvalidOperationException($"Final binary call returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareMeasuredRecordSegments()
    {
        const string source = """
            -module(beam_measured_record).
            -export([run/1]).
            -record(packet, {payload}).
            run(Packet) ->
                <<(byte_size(Packet#packet.payload)):16, (Packet#packet.payload)/binary>>.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_measured_record.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            var tag = Term.A("packet");
            actual = await image.Invoke(context, "run", Term.Tuple(tag, new BitString([0xAB, 0xCD])));
            try
            {
                await image.Invoke(context, "run", Term.Tuple(tag, Term.A("invalid")));
                throw new InvalidOperationException("Binary measurement accepted an atom.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("badarg")))
            {
            }
            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        if (!new BitString([0, 2, 0xAB, 0xCD]).Equals(actual) || !Term.A("normal").Equals(exit))
        {
            throw new InvalidOperationException($"Measured record construction returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareSplitBinarySingleton()
    {
        const string source = """
            -module(beam_split_singleton).
            -export([run/2]).
            run(Bin, Size) when Size < 0 ->
                [];
            run(Bin, Size) when Size >= 0 ->
                Head = identity(Bin),
                Count = byte_size(Head),
                if
                    Count > Size ->
                        note(Size),
                        {First, _Rest} = split_binary(Head, Size),
                        [First];
                    Count =:= Size ->
                        note(Size),
                        [Head];
                    Count > 0, Size =< 6 ->
                        note(Count),
                        [Head | run(Bin, Size - Count)];
                    Count > 0 ->
                        {First, _Rest} = split_binary(Head, Size),
                        [First]
                end.
            identity(Bin) -> Bin.
            note(Size) -> put(last_size, Size).
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_split_singleton.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            var input = new BitString([1, 2, 3]);
            var first = await image.Invoke(context, "run", input, Term.I(2));
            var fallback = await image.Invoke(context, "run", input, Term.I(-1));
            var exact = await image.Invoke(context, "run", input, Term.I(3));
            var recursive = await image.Invoke(context, "run", input, Term.I(5));
            try
            {
                await image.Invoke(context, "run", input, Term.I(7));
                throw new InvalidOperationException("split_binary accepted a size larger than its input.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("badarg")))
            {
            }

            try
            {
                await image.Invoke(context, "run", new BitString([]), Term.I(1));
                throw new InvalidOperationException("A terminal if accepted unmatched guards.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("if_clause")))
            {
            }

            actual = Term.Tuple(first, fallback, exact, recursive);
            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(Term.List(new BitString([1, 2])), Nil.Value,
            Term.List(new BitString([1, 2, 3])), Term.List(new BitString([1, 2, 3]), new BitString([1, 2])));
        if (!expected.Equals(actual) || !Term.A("normal").Equals(exit))
        {
            throw new InvalidOperationException($"Split binary list construction returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralLengthAndFunctionGuards()
    {
        const string source = """
            -module(beam_general_length_fun_guard_probe).
            -export([run/3, target/0]).
            run(L,F,N) when is_function(F,N), length(L) > 0 -> {ok,N};
            run(_,_,_) -> rejected.
            target() -> fun identity/1.
            identity(X) -> X.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_length_fun_guard_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term function = await image.Invoke(context, "target");
            Term list = Term.List(Term.I(17));
            Term improper = new Cons(Term.I(17), Term.A("tail"));
            Term accepted = await image.Invoke(context, "run", list, function, Term.I(1));
            Term wrongArity = await image.Invoke(context, "run", list, function, Term.I(2));
            Term badArity = await image.Invoke(context, "run", list, function, Term.I(-1));
            Term badList = await image.Invoke(context, "run", improper, function, Term.I(1));
            actual = Term.Tuple(accepted, wrongArity, badArity, badList);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(Term.Tuple(Term.A("ok"), Term.I(1)), Term.A("rejected"), Term.A("rejected"), Term.A("rejected"));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM length and function guards returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralMapExpressions()
    {
        const string source = """
            -module(beam_general_map_emit_probe).
            -export([run/3, check/2]).
            run(M,K,V) -> {ok, M#{K => first, flag := V, K => V}}.
            check(M,V) when M#{flag := V} =:= #{flag => V} -> accepted;
            check(_,_) -> rejected.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_map_emit_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term flag = Term.A("flag");
            Term key = Term.A("key");
            Term value = Term.I(42);
            var original = new MapTerm([new(flag, Term.A("old"))]);
            Term updated = await image.Invoke(context, "run", original, key, value);
            Term accepted = await image.Invoke(context, "check", original, value);
            Term missing = await image.Invoke(context, "check", new MapTerm([]), value);
            Term notMap = await image.Invoke(context, "check", Term.A("other"), value);
            actual = Term.Tuple(updated, accepted, missing, notMap);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        var updatedMap = new MapTerm([new(Term.A("flag"), Term.I(42)), new(Term.A("key"), Term.I(42))]);
        Term expected = Term.Tuple(Term.Tuple(Term.A("ok"), updatedMap), Term.A("accepted"), Term.A("rejected"), Term.A("rejected"));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM map expressions returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralElementNotAndProduct()
    {
        const string source = """
            -module(beam_general_element_not_product_probe).
            -export([run/4]).
            run(N,T,A,B) when not element(N,T) -> {ok,A*B};
            run(_,_,_,_) -> rejected.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_element_not_product_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term flags = Term.Tuple(Term.A("false"), Term.A("true"), Term.A("other"));
            Term accepted = await image.Invoke(context, "run", Term.I(1), flags, Term.I(6), Term.I(7));
            Term trueFlag = await image.Invoke(context, "run", Term.I(2), flags, Term.I(6), Term.I(7));
            Term nonBoolean = await image.Invoke(context, "run", Term.I(3), flags, Term.I(6), Term.I(7));
            Term badIndex = await image.Invoke(context, "run", Term.I(4), flags, Term.I(6), Term.I(7));
            actual = Term.Tuple(accepted, trueFlag, nonBoolean, badIndex);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(Term.Tuple(Term.A("ok"), Term.I(42)), Term.A("rejected"), Term.A("rejected"), Term.A("rejected"));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM element, not and multiplication returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralPidAndTupleSizeGuards()
    {
        const string source = """
            -module(beam_general_guard_pid_size_probe).
            -export([run/2]).
            run(P, T) when is_pid(P), tuple_size(T) > 1 -> accepted;
            run(_, _) -> rejected.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_guard_pid_size_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term pair = Term.Tuple(Term.A("left"), Term.A("right"));
            Term accepted = await image.Invoke(context, "run", context.Self, pair);
            Term notPid = await image.Invoke(context, "run", Term.A("other"), pair);
            Term notTuple = await image.Invoke(context, "run", context.Self, Term.A("other"));
            Term tooSmall = await image.Invoke(context, "run", context.Self, Term.Tuple(Term.A("single")));
            actual = Term.Tuple(accepted, notPid, notTuple, tooSmall);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(Term.A("accepted"), Term.A("rejected"), Term.A("rejected"), Term.A("rejected"));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM PID and tuple size guard returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralRecordAccessGuard()
    {
        const string source = """
            -module(beam_general_record_access_guard_probe).
            -export([run/1]).
            -record(row, {flag}).
            run(R) when R#row.flag =:= ok; R =:= fallback -> accepted;
            run(_) -> rejected.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_record_access_guard_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term correct = await image.Invoke(context, "run", Term.Tuple(Term.A("row"), Term.A("ok")));
            Term wrongTag = await image.Invoke(context, "run", Term.Tuple(Term.A("other"), Term.A("ok")));
            Term wrongArity = await image.Invoke(context, "run", Term.Tuple(Term.A("row"), Term.A("ok"), Term.A("extra")));
            Term alternative = await image.Invoke(context, "run", Term.A("fallback"));
            actual = Term.Tuple(correct, wrongTag, wrongArity, alternative);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(Term.A("accepted"), Term.A("rejected"), Term.A("rejected"), Term.A("accepted"));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM record access guard returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralFunctionValueCall()
    {
        const string source = """
            -module(beam_general_apply_probe).
            -export([run/1, direct/1]).
            run(A) -> {ok, (pick(A))(prepare(A))}.
            direct(A) ->
                {invoke(ignored, fun() -> A end, ignored),
                 apply_last(ignored, A, fun(Value) -> {argument, Value} end)}.
            invoke(_, Callback, _) -> Callback().
            apply_last(_, Value, Callback) -> Callback(Value).
            pick(Captured) -> fun(Value) -> {Captured, Value} end.
            prepare(X) -> {argument, X}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_apply_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term computed = await image.Invoke(context, "run", Term.I(17));
            Term direct = await image.Invoke(context, "direct", Term.I(17));
            actual = Term.Tuple(computed, direct);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term argument = Term.Tuple(Term.A("argument"), Term.I(17));
        Term expected = Term.Tuple(
            Term.Tuple(Term.A("ok"), Term.Tuple(Term.I(17), argument)),
            Term.Tuple(Term.I(17), argument)
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM function value call returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareConstructedTupleBinding()
    {
        const string source = """
            -module(beam_constructed_binding).
            -export([run/3]).
            run(A, B, C) ->
                {X, Y, Z} = {id(A), id(B), id(C)},
                {Z, X, Y}.
            id(X) -> {value, X}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_constructed_binding.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            actual = await image.Invoke(context, "run", Term.I(1), Term.I(2), Term.I(3));
            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(
            Term.Tuple(Term.A("value"), Term.I(3)),
            Term.Tuple(Term.A("value"), Term.I(1)),
            Term.Tuple(Term.A("value"), Term.I(2))
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Constructed tuple binding returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareGeneralSelectiveReceive()
    {
        const string source = """
            -module(beam_general_receive_probe).
            -export([run/2, remaining/0]).
            run(Key, Wait) ->
                receive
                    {Key, Value} when is_integer(Value) -> {found, Value};
                    stop -> stopped
                after Wait -> expired
                end.
            remaining() ->
                receive
                    other -> retained
                after 0 -> missing
                end.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_receive_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term key = Term.A("key");
            runtime.Send(context.Self, Term.A("other"));
            runtime.Send(context.Self, Term.Tuple(key, Term.A("rejected")));
            runtime.Send(context.Self, Term.Tuple(key, Term.I(17)));
            runtime.Send(context.Self, Term.A("stop"));
            Term found = await image.Invoke(context, "run", key, Term.I(0));
            Term stopped = await image.Invoke(context, "run", key, Term.I(0));
            Term expired = await image.Invoke(context, "run", key, Term.I(0));
            Term retained = await image.Invoke(context, "remaining");
            actual = Term.Tuple(found, stopped, expired, retained);

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(Term.Tuple(Term.A("found"), Term.I(17)), Term.A("stopped"), Term.A("expired"), Term.A("retained"));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM selective receive returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareReceiveMapAndTypeGuards()
    {
        const string source = """
            -module(beam_receive_map_probe).
            -export([take/0, next/0]).
            take() ->
                receive
                    #{value := X} when is_reference(X) orelse is_boolean(X) -> {found,X}
                after 0 -> expired
                end.
            next() ->
                receive
                    #{other := _} = X -> X;
                    #{value := _} = X -> X
                after 0 -> empty
                end.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_receive_map_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term yes = Term.A("true");
        Term key = Term.A("value");
        var missingKey = new MapTerm([new(Term.A("other"), yes)]);
        var wrongType = new MapTerm([new(key, Term.I(17))]);
        var accepted = new MapTerm([new(key, yes)]);
        Term? actual = null;
        Exception? failure = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            runtime.Send(context.Self, missingKey);
            runtime.Send(context.Self, wrongType);
            runtime.Send(context.Self, accepted);
            try
            {
                actual = Term.List(
                    await image.Invoke(context, "take"),
                    await image.Invoke(context, "take"),
                    await image.Invoke(context, "next"),
                    await image.Invoke(context, "next"),
                    await image.Invoke(context, "next")
                );

            }
            catch (Exception error)
            {
                failure = error;
            }

            return actual ?? Nil.Value;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.List(
            Term.Tuple(Term.A("found"), yes), Term.A("expired"), missingKey, wrongType, Term.A("empty")
        );
        if (failure is not null)
        {
            throw new InvalidOperationException($"Receive matcher failed: {failure.Data["BeamInstruction"]}", failure);
        }
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM receive map and type guards returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareReceiveComputedGuardsAndCatchAll()
    {
        const string source = """
            -module(beam_receive_value_probe).
            -export([take/0, next/0]).
            take() ->
                receive
                    {candidate,X,Y} when element(1,X)+1 =:= length(Y) -> {found,X}
                after 0 -> expired
                end.
            next() ->
                receive X -> X
                after 0 -> empty
                end.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_receive_value_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term candidate = Term.A("candidate");
        Term tuple = Term.Tuple(Term.I(1));
        Term a = Term.A("a");
        Term both = Term.List(a, Term.A("b"));
        Term invalid = Term.Tuple(candidate, Term.A("bad"), both);
        Term unequal = Term.Tuple(candidate, tuple, Term.List(a));
        Term? actual = null;
        Exception? failure = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            runtime.Send(context.Self, invalid);
            runtime.Send(context.Self, unequal);
            runtime.Send(context.Self, Term.Tuple(candidate, tuple, both));
            try
            {
                actual = Term.List(
                    await image.Invoke(context, "take"),
                    await image.Invoke(context, "take"),
                    await image.Invoke(context, "next"),
                    await image.Invoke(context, "next"),
                    await image.Invoke(context, "next")
                );
            }
            catch (Exception error)
            {
                failure = error;
            }

            return actual ?? Nil.Value;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.List(
            Term.Tuple(Term.A("found"), tuple), Term.A("expired"), invalid, unequal, Term.A("empty")
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM computed receive guard returned {actual}, exit {exit}.", failure);
        }
    }

    private static async Task CompareGuardExtrema()
    {
        const string source = """
            -module(beam_guard_extrema).
            -export([minimum/2, maximum/2, rounded/1, integer_rounding/2]).
            minimum(A,B) when min(A,B) =:= A -> yes;
            minimum(_,_) -> no.
            maximum(A,B) when max(A,B) =:= B -> yes;
            maximum(_,_) -> no.
            rounded(A) when float(A) =:= 1.0 -> yes;
            rounded(_) -> no.
            integer_rounding(round, X) when round(X) =:= -2 -> yes;
            integer_rounding(ceil, X) when ceil(X) =:= -1 -> yes;
            integer_rounding(floor, X) when floor(X) =:= -2 -> yes;
            integer_rounding(_, _) -> no.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_guard_extrema.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            var rounding = new List<Term>();
            foreach (string operation in new[] { "round", "ceil", "floor" })
            {
                Term kind = Term.A(operation);
                rounding.Add(await image.Invoke(context, "integer_rounding", kind, new FloatTerm(-1.5)));
                rounding.Add(await image.Invoke(context, "integer_rounding", kind, Term.A("invalid")));
            }

            Term extrema = Term.List(
                await image.Invoke(context, "minimum", Term.I(1), Term.A("a")),
                await image.Invoke(context, "minimum", Term.A("a"), Term.I(1)),
                await image.Invoke(context, "maximum", Term.I(1), Term.A("a")),
                await image.Invoke(context, "maximum", Term.A("a"), Term.I(1)),
                await image.Invoke(context, "rounded", Term.I(1)),
                await image.Invoke(context, "rounded", Term.A("invalid"))
            );
            actual = Term.Tuple(extrema, Term.List(rounding.ToArray()));

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term yes = Term.A("yes");
        Term no = Term.A("no");
        Term outcomes = Term.List(yes, no, yes, no, yes, no);
        if (!Term.Tuple(outcomes, outcomes).Equals(actual))
        {
            throw new InvalidOperationException($"BEAM guard BIFs returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareReceiveBinaryPatterns()
    {
        const string source = """
            -module(beam_receive_binary_pattern).
            -export([pick/1,next/0]).
            pick(Id) ->
                receive
                    {tag, <<Id:32, Payload/binary>>, Value} -> {Payload, Value}
                after 0 -> timeout
                end.
            next() -> receive X -> X after 0 -> empty end.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_receive_binary_pattern.erl"));
        await using var runtime = new ProcessRuntime();
        Term wrong = Term.Tuple(Term.A("tag"), new BitString([0, 0, 0, 2, 99]), Term.A("wrong"));
        Term accepted = Term.Tuple(Term.A("tag"), new BitString([0, 0, 0, 1, 97]), Term.A("accepted"));
        Term? actual = null;
        Exception? failure = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            runtime.Send(context.Self, wrong);
            runtime.Send(context.Self, accepted);
            try
            {
                Term result = await image.Invoke(context, "pick", Term.I(1));
                Term remaining = await image.Invoke(context, "next");
                actual = Term.Tuple(result, remaining);
                return actual;
            }
            catch (Exception error)
            {
                failure = error;
                return Nil.Value;
            }
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.Tuple(Term.Tuple(new BitString([97]), Term.A("accepted")), wrong);
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM receive binary pattern returned {actual}, exit {exit}.", failure);
        }
    }

    private static async Task CompareReceiveTupleAndDisjunctionGuards()
    {
        const string source = """
            -module(beam_receive_tuple_guards).
            -export([pick/1, next/0]).
            pick(Keys) ->
                receive
                    {key, A, B} when is_map_key({A,B}, Keys) -> {key,A,B};
                    {exact, V} when V =:= {ok,1} -> V;
                    {reply, V} when not is_tuple(V) orelse element(2, V) =/= stopped -> V
                after 0 -> timeout
                end.
            next() -> receive X -> X after 0 -> empty end.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_receive_tuple_guards.erl"));
        await using var runtime = new ProcessRuntime();
        Term key = Term.Tuple(Term.A("key"), Term.A("a"), Term.A("b"));
        Term exact = Term.Tuple(Term.A("ok"), Term.I(1));
        Term shortReply = Term.Tuple(Term.A("reply"), Term.Tuple(Term.A("only")));
        Term stopped = Term.Tuple(Term.A("reply"), Term.Tuple(Term.A("reply"), Term.A("stopped")));
        Term accepted = Term.Tuple(Term.A("reply"), Term.A("running"));
        var keys = new MapTerm([new(Term.Tuple(Term.A("a"), Term.A("b")), Term.A("true"))]);
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            runtime.Send(context.Self, shortReply);
            runtime.Send(context.Self, stopped);
            runtime.Send(context.Self, key);
            runtime.Send(context.Self, Term.Tuple(Term.A("exact"), exact));
            runtime.Send(context.Self, accepted);
            actual = Term.List(
                await image.Invoke(context, "pick", keys),
                await image.Invoke(context, "pick", keys),
                await image.Invoke(context, "pick", keys),
                await image.Invoke(context, "pick", keys),
                await image.Invoke(context, "next"),
                await image.Invoke(context, "next")
            );

            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.List(key, exact, Term.A("running"), Term.A("timeout"), shortReply, stopped);
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM receive tuple guards returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareReceiveGuardAlternativesAndMapKey()
    {
        const string source = """
            -module(beam_receive_more_guards).
            -export([run/1,next/0]).
            run(M) ->
                receive
                    {k,K} when is_map_key(K,M) -> {key,K};
                    {n,V} when not is_tuple(V), V =:= [1]; is_list(V), V == [2] -> {list,V};
                    {p,P} when is_port(P) -> port
                after 0 -> timeout
                end.
            next() -> receive X -> X after 0 -> empty end.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_receive_more_guards.erl"));
        await using var runtime = new ProcessRuntime();
        Term a = Term.A("a");
        Term b = Term.A("b");
        Term invalid = Term.A("invalid");
        Term one = Term.List(Term.I(1));
        Term two = Term.List(Term.I(2));
        Term unknownKey = Term.Tuple(Term.A("k"), b);
        Term rejected = Term.Tuple(Term.A("n"), invalid);
        Term nonPort = Term.Tuple(Term.A("p"), invalid);
        var map = new MapTerm([new(a, Term.I(1))]);
        Term? actual = null;
        Exception? failure = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            runtime.Send(context.Self, unknownKey);
            runtime.Send(context.Self, rejected);
            runtime.Send(context.Self, nonPort);
            runtime.Send(context.Self, Term.Tuple(Term.A("n"), one));
            runtime.Send(context.Self, Term.Tuple(Term.A("k"), a));
            runtime.Send(context.Self, Term.Tuple(Term.A("n"), two));
            try
            {
                actual = Term.List(
                    await image.Invoke(context, "run", map),
                    await image.Invoke(context, "run", map),
                    await image.Invoke(context, "run", invalid),
                    await image.Invoke(context, "run", new MapTerm([])),
                    await image.Invoke(context, "next"),
                    await image.Invoke(context, "next"),
                    await image.Invoke(context, "next"),
                    await image.Invoke(context, "next")
                );
            }
            catch (Exception error)
            {
                failure = error;
            }

            return actual ?? Nil.Value;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.List(
            Term.Tuple(Term.A("list"), one),
            Term.Tuple(Term.A("key"), a),
            Term.Tuple(Term.A("list"), two),
            Term.A("timeout"),
            unknownKey,
            rejected,
            nonPort,
            Term.A("empty")
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM receive guard alternatives returned {actual}, exit {exit}.", failure);
        }
    }

    private static async Task CompareGeneralLocalFunctionReference()
    {
        const string source = """
            -module(beam_general_reference_probe).
            -export([run/1]).
            run({X}) -> fun() -> {X, fun target/1} end.
            target(X) -> {ok, X}.
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_general_reference_probe.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            var closure = (FunctionTerm)await image.Invoke(context, "run", Term.Tuple(Term.I(17)));
            var pair = (TupleTerm)await closure.Invoke(context, []);
            var reference = (FunctionTerm)pair.Items[1];
            Term invoked = await reference.Invoke(context, [pair.Items[0]]);
            actual = invoked;

            return invoked;
        });
        Term exit = await process.Completion;
        if (!Term.Tuple(Term.A("ok"), Term.I(17)).Equals(actual))
        {
            throw new InvalidOperationException($"General BEAM local reference returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareExternalFunctionInvocation()
    {
        const string file = "beam_external_fun_probe.erl";
        byte[] bytes = CodeGeneration.CompileBeamModule(Encoding.UTF8.GetString(Read(file)), file);
        await using var runtime = new ProcessRuntime();
        using PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes);
        runtime.Modules.PublishPreparedBeam(image);
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term value = await runtime.Modules.Call(context, "beam_external_fun_probe", "converter", []);
            if (value is not FunctionTerm
                {
                    ExternalIdentity: { Module.Name: "erlang", Function.Name: "atom_to_list", Arity: 1 }
                } converter)
            {
                throw new InvalidOperationException("Expected the emitted external function reference.");
            }

            actual = await converter.Invoke(context, [Term.A("probe")]);

            return actual;
        });
        Term exitReason = await process.Completion;
        if (!Term.String("probe").Equals(actual))
        {
            throw new InvalidOperationException($"External BEAM function returned {actual}, exit {exitReason}.");
        }
    }

    private static async Task CompareBuildMacroDefinitions()
    {
        const string source = """
            -module(beam_build_definitions).
            -export([run/0]).
            -ifdef(BUILD_VERSION).
            run() -> {?BUILD_VERSION, ?mode, ?COUNT}.
            -else.
            run() -> missing.
            -endif.
            """;
        var temporary = Directory.CreateTempSubdirectory("erlang-beam-build-definitions-");
        try
        {
            string path = Path.Combine(temporary.FullName, "beam_build_definitions.erl");
            File.WriteAllText(path, source);
            var profile = new BeamReleaseBuildProfile(
                "10.0.6",
                path,
                temporary.FullName,
                [],
                [],
                RecordedCompileOptions: [],
                EmitAbstractCode: false,
                PreprocessorDefinitions: new Dictionary<string, Term>
                {
                    ["BUILD_VERSION"] = Term.String("v6.0.3"),
                    ["mode"] = Term.A("silence"),
                    ["COUNT"] = Term.I(-21)
                }
            );
            BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamFile(path, buildProfile: profile));
            await using var runtime = new ProcessRuntime();
            Term? actual = null;
            ProcessHandle process = runtime.Spawn(async context =>
            {
                actual = await image.Invoke(context, "run");

                return actual;
            });
            Term exit = await process.Completion;
            if (!Term.Tuple(Term.String("v6.0.3"), Term.A("silence"), Term.I(-21)).Equals(actual))
            {
                throw new InvalidOperationException($"BEAM build definitions returned {actual}, exit {exit}.");
            }
        }
        finally
        {
            temporary.Delete(recursive: true);
        }
    }

    private static async Task CompareMatchSpecSourceTransform()
    {
        const string source = """
            -module(beam_match_spec_transform).
            -compile({parse_transform,ms_transform}).
            -export([static/0,dynamic/1]).
            static() -> ets:fun2ms(fun({M,_}=T) when is_atom(M) -> T end).
            dynamic(Key) ->
                ets:fun2ms(fun({K,V}) when K =:= Key, is_integer(V) -> {K,V};
                              ({_,_}) -> false
                           end).
            """;
        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_match_spec_transform.erl"));
        await using var runtime = new ProcessRuntime();
        Term first = Term.A("$1");
        Term second = Term.A("$2");
        Term wildcard = Term.A("_");
        Term capture = Term.A("test");
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term staticSpec = await image.Invoke(context, "static");
            Term dynamicSpec = await image.Invoke(context, "dynamic", capture);
            actual = Term.Tuple(staticSpec, dynamicSpec);

            return actual;
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(
            Term.List(Term.Tuple(Term.Tuple(first, wildcard),
                Term.List(Term.Tuple(Term.A("is_atom"), first)), Term.List(Term.A("$_")))),
            Term.List(
                Term.Tuple(Term.Tuple(first, second),
                    Term.List(Term.Tuple(Term.A("=:="), first, Term.Tuple(Term.A("const"), capture)),
                        Term.Tuple(Term.A("is_integer"), second)),
                    Term.List(Term.Tuple(Term.Tuple(first, second)))),
                Term.Tuple(Term.Tuple(wildcard, wildcard), Nil.Value, Term.List(Term.A("false")))
            )
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM match spec transform returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareDiameterRecordSourceTransform()
    {
        const string source = """
            -module(beam_diameter_records).
            -compile({parse_transform,diameter_exprecs}).
            -export_records([sample]).
            -record(sample,{first=initial,second=2}).
            -export([run/0]).
            run() -> '#new-'(sample).
            """;
        var dependencies = new BeamDependencySelection(["lists", "erlang"]);
        BeamModule image = BeamModule.Prepare(
            CodeGeneration.CompileBeamModule(source, "beam_diameter_records.erl"),
            dependencies
        );
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage lists = runtime.Modules.PrepareBeam(
            EtsTraversalOriginal416Access.Resource(EtsTraversalOriginal416Names.ListsBeam),
            dependencies
        );
        if (runtime.Modules.PublishPreparedBeam(lists).Status != BeamPublicationStatus.Published)
        {
            throw new InvalidOperationException("Could not load the OTP lists fixture for Diameter record accessors.");
        }
        Term sample = Term.A("sample");
        Term first = Term.A("first");
        Term second = Term.A("second");
        Term replaced = Term.A("replaced");
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term record = await image.Invoke(context, "run");
            Term updated = await image.Invoke(context, "#set-", Term.Tuple(first, replaced), record);
            Term values = await image.Invoke(context, "#get-", Term.List(second, first), updated);
            Term index = await image.Invoke(context, "#info-", sample, Term.Tuple(Term.A("index"), second));
            actual = Term.Tuple(record, updated, values, index);

            return actual;
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(
            Term.Tuple(sample, Term.A("initial"), Term.I(2)),
            Term.Tuple(sample, replaced, Term.I(2)),
            Term.List(Term.I(2), replaced),
            Term.I(3)
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM Diameter record transform returned {actual}, exit {exit}.");
        }
    }

    private static async Task ComparePatternedFunctionBody()
    {
        const string source = """
            -module(beam_patterned_body).
            -export([init/1]).
            init([]) ->
                Flags = #{strategy => one_for_one},
                Worker = erlang:atom_to_list(worker),
                {ok, {Flags, [Worker]}}.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_patterned_body.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        Term? failure = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            actual = await image.Invoke(context, "init", Nil.Value);
            try
            {
                return await image.Invoke(context, "init", Term.A("invalid"));
            }
            catch (ErlangException error)
            {
                failure = error.Reason;
                return actual;
            }
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(
            Term.A("ok"),
            Term.Tuple(new MapTerm([new(Term.A("strategy"), Term.A("one_for_one"))]), Term.List(Term.String("worker")))
        );
        if (!expected.Equals(actual) || !Term.A("function_clause").Equals(failure))
        {
            throw new InvalidOperationException($"BEAM patterned function returned {actual}, failure {failure}, exit {exit}.");
        }
    }

    private static async Task CompareComprehensionFilterBindings()
    {
        const string source = """
            -module(beam_filter_bindings).
            -export([run/1]).
            run(Items) ->
                [{Item, Value} || Item <- Items,
                    case Item of
                        {keep, Values} -> true;
                        {skip, Values} -> false
                    end,
                    Value <- Values].
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_filter_bindings.erl"));
        await using var runtime = new ProcessRuntime();
        Term keep = Term.Tuple(Term.A("keep"), Term.List(Term.I(1), Term.I(2)));
        Term skip = Term.Tuple(Term.A("skip"), Term.List(Term.I(3)));
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            actual = await image.Invoke(context, "run", Term.List(skip, keep));
            return actual;
        });
        Term exit = await process.Completion.WaitAsync(TimeSpan.FromSeconds(10));
        Term expected = Term.List(Term.Tuple(keep, Term.I(1)), Term.Tuple(keep, Term.I(2)));
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM comprehension bindings returned {actual}, exit {exit}.");
        }
    }

    private static async Task CompareBranchBindings()
    {
        const string source = """
            -module(beam_case_bindings).
            -export([run/1, conditional/1, message/1, assigned/1, nested/1]).
            run(Input) ->
                case Input of
                    {first, Value, Other} -> ok;
                    {second, Other, Value} -> ok
                end,
                pair(Value, Other).
            conditional(Input) ->
                if Input =:= first -> Value = 1; true -> Value = 2 end,
                {conditional, Value}.
            message(Input) ->
                case Input of
                    send -> self() ! {value, 3};
                    timeout -> ok
                end,
                receive {value, Value} -> ok after 0 -> Value = 4 end,
                {message, Value}.
            assigned(Input) ->
                Result = case Input of
                    {first, X} -> Value = X, ok;
                    {second, X} -> Value = X, error
                end,
                {Result, Value}.
            nested(Input) ->
                Result = case Input of
                    {inner, Flag} ->
                        begin
                            if Flag -> Value = 7, yes; true -> Value = 8, no end
                        end;
                    direct -> Value = 9, other
                end,
                {Result, Value}.
            pair(A, B) -> {A, B}.
            """;

        BeamModule image = BeamModule.Prepare(CodeGeneration.CompileBeamModule(source, "beam_case_bindings.erl"));
        await using var runtime = new ProcessRuntime();
        Term? actual = null;
        ProcessHandle process = runtime.Spawn(async context =>
        {
            Term first = await image.Invoke(context, "run", Term.Tuple(Term.A("first"), Term.I(1), Term.I(2)));
            Term second = await image.Invoke(context, "run", Term.Tuple(Term.A("second"), Term.I(1), Term.I(2)));
            Term left = await image.Invoke(context, "conditional", Term.A("first"));
            Term right = await image.Invoke(context, "conditional", Term.A("second"));
            Term message = await image.Invoke(context, "message", Term.A("send"));
            Term timeout = await image.Invoke(context, "message", Term.A("timeout"));
            Term assigned = await image.Invoke(context, "assigned", Term.Tuple(Term.A("first"), Term.I(5)));
            Term alternative = await image.Invoke(context, "assigned", Term.Tuple(Term.A("second"), Term.I(6)));
            Term nested = await image.Invoke(context, "nested", Term.Tuple(Term.A("inner"), Term.A("true")));
            Term nestedFallback = await image.Invoke(context, "nested", Term.Tuple(Term.A("inner"), Term.A("false")));
            Term direct = await image.Invoke(context, "nested", Term.A("direct"));
            actual = Term.Tuple(
                first,
                second,
                left,
                right,
                message,
                timeout,
                assigned,
                alternative,
                nested,
                nestedFallback,
                direct
            );
            return actual;
        });
        Term exit = await process.Completion;
        Term expected = Term.Tuple(
            Term.Tuple(Term.I(1), Term.I(2)),
            Term.Tuple(Term.I(2), Term.I(1)),
            Term.Tuple(Term.A("conditional"), Term.I(1)),
            Term.Tuple(Term.A("conditional"), Term.I(2)),
            Term.Tuple(Term.A("message"), Term.I(3)),
            Term.Tuple(Term.A("message"), Term.I(4)),
            Term.Tuple(Term.A("ok"), Term.I(5)),
            Term.Tuple(Term.A("error"), Term.I(6)),
            Term.Tuple(Term.A("yes"), Term.I(7)),
            Term.Tuple(Term.A("no"), Term.I(8)),
            Term.Tuple(Term.A("other"), Term.I(9))
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"BEAM case bindings returned {actual}, exit {exit}.");
        }
    }

    private static void CompareWarningOptions()
    {
        const string file = "beam_compile_probe.erl";
        string source = Encoding.UTF8.GetString(Read(file)).Replace(
            "-compile([{nowarn_possibly_unsafe_function,{erlang,list_to_atom,1}}]).",
            "-compile([no_native, no_auto_import_types, nowarn_deprecated_callback, {nowarn_deprecated_function,[{erlang,now,0}]}, {nowarn_hidden_doc,[answer/0]}, {nowarn_unused_function,answer/0}]).",
            StringComparison.Ordinal
        );
        if (!CodeGeneration.CompileBeamModule(source, file).AsSpan().SequenceEqual(Read("beam_compile_probe.beam")))
        {
            throw new InvalidOperationException(Mismatch + file);
        }

        RejectCompileOption("{nowarn_hidden_doc,[answer/256]}");
        RejectCompileOption("{nowarn_deprecated_function,[{erlang,now,0}|invalid]}");
        RejectCompileOption("{parse_transform,requires_transform}");
    }

    private static void RejectCompileOption(string option)
    {
        try
        {
            CodeGeneration.CompileBeamModule($"-module(custom). -compile({option}).", "custom.erl");
        }
        catch (CompileException error) when (error.Message.StartsWith("Unsupported -compile option:", StringComparison.Ordinal))
        {
            return;
        }

        throw new InvalidOperationException(AcceptedUnsupported);
    }

    private static void RejectUnsupported(string source)
    {
        try
        {
            CodeGeneration.CompileBeamModule(source, "custom.erl");
        }
        catch (NotSupportedException)
        {
            return;
        }

        throw new InvalidOperationException(AcceptedUnsupported);
    }

    private static void CompareHeaderFixture(string module, string header, string? include = null)
    {
        var temporary = Directory.CreateTempSubdirectory("erlang-beam-header-");
        try
        {
            string source = Path.Combine(temporary.FullName, module + ".erl");
            File.WriteAllBytes(source, Read(module + ".erl"));
            string headerDirectory = include is null ? temporary.FullName : Path.Combine(temporary.FullName, include);
            Directory.CreateDirectory(headerDirectory);
            File.WriteAllBytes(Path.Combine(headerDirectory, header), Read(header));
            if (!CodeGeneration.CompileBeamFile(source).AsSpan().SequenceEqual(Read(module + ".beam")))
            {
                throw new InvalidOperationException(Mismatch + module);
            }
        }
        finally
        {
            temporary.Delete(recursive: true);
        }
    }

    private static void CompareIncludeFixture()
    {
        var temporary = Directory.CreateTempSubdirectory("erlang-beam-include-");
        try
        {
            string sourcePath = Path.Combine(temporary.FullName, IncludeFixture + ".erl");
            byte[] source = Read(IncludeFixture + ".erl");
            byte[] expected = Read(IncludeFixture + ".beam");
            File.WriteAllBytes(sourcePath, source);
            File.WriteAllBytes(Path.Combine(temporary.FullName, OuterHeader), Read(OuterHeader));
            File.WriteAllBytes(Path.Combine(temporary.FullName, InnerHeader), Read(InnerHeader));
            RequireEqual(CodeGeneration.CompileBeamFile(sourcePath), expected);

            string library = Path.Combine(temporary.FullName, "lib");
            string include = Directory.CreateDirectory(Path.Combine(library, "fixture-1.0", "include")).FullName;
            File.WriteAllBytes(Path.Combine(include, OuterHeader), Read(OuterHeader));
            File.WriteAllBytes(Path.Combine(include, InnerHeader), Read(InnerHeader));
            File.WriteAllText(sourcePath, Encoding.UTF8.GetString(source).Replace(
                "-include(\"" + OuterHeader + "\").",
                "-include_lib(\"fixture/include/" + OuterHeader + "\").",
                StringComparison.Ordinal
            ));
            RequireEqual(CodeGeneration.CompileBeamFile(sourcePath, library), expected);
        }
        finally
        {
            temporary.Delete(recursive: true);
        }
    }

    private static void RequireEqual(byte[] actual, byte[] expected)
    {
        if (!actual.AsSpan().SequenceEqual(expected))
        {
            throw new InvalidOperationException(Mismatch + IncludeFixture);
        }
    }

    internal static byte[] Read(string file)
    {
        using Stream input = typeof(BeamModuleEmissionChecks).Assembly.GetManifestResourceStream(ResourcePrefix + file)!;
        using var output = new MemoryStream();
        input.CopyTo(output);

        return output.ToArray();
    }
}
