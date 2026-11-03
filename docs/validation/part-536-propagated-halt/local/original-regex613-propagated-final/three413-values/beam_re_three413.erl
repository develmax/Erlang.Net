-module(beam_re_three413).
-export([run/1,run_stacks/1,worker/2,export_inputs/0]).
cases()->[inspect_no_names,inspect_sorted_names,inspect_duplicate_names,inspect_changed_small_fields,inspect_invalid_pattern,inspect_invalid_tag,inspect_large_metadata,inspect_noninteger_metadata,inspect_invalid_item,run_first_true,run_first_false,run_first_false_nomatch,run_compiled_both,run_valid_unicode_both,run_first_invalid_atom,run_first_invalid_integer,run_first_invalid_with_badoptions,run_invalid_options,run_improper_options,run_false_badsubject,run_false_badpattern,import_original_roundtrip,import_unicode_roundtrip,import_reuse_same_export,import_ignored_fallback_fields,import_extra_header,import_invalid_term,import_invalid_tag,import_short_header,import_wrong_header_title,import_bad_crc,import_corrupt_encoded_crc,import_nonbinary_encoded,import_version_fallback,import_version_ignores_encoded_type,import_fallback_unicode_options,import_fallback_bad_options,import_fallback_bad_pattern,import_fallback_nonbinary_pattern,wrong_inspect_arity,wrong_internal_arity,wrong_import_arity].
export_inputs()->
 {ok,E}=re:compile(<<"(?<word>a+)">>,[export]),
 {ok,U}=re:compile(<<"(?<word>a+)">>,[unicode,export]),
 {E,U}.
run(Input)->[{C,capture({C,Input},default)}||C<-cases()].
run_stacks(Input)->[{D,[{C,capture({C,Input},D)}||C<-cases()]}||D<-[default,0,1,2,8]].
capture(Payload,Depth)->
 {Pid,Ref}=spawn_monitor(?MODULE,worker,[Payload,Depth]),
 receive {'DOWN',Ref,process,Pid,{observation,Value}}->Value;
         {'DOWN',Ref,process,Pid,Reason}->{unexpected_down,Reason}
 after 10000->error(observer_timeout)
 end.
worker({Case,Input},Depth)->
 case Depth of default->ok;_->erlang:system_flag(backtrace_depth,Depth) end,
 Value=try probe(Case,Input) of Result->{ok,Result}
       catch Class:Reason:Stack->{exception,Class,Reason,Stack} end,
 exit({observation,Value}).
observe_import(Export)->
 P=re:import(Export),
 {re_pattern,N,U,C,R}=P,
 {{N,U,C,is_reference(R)},re:inspect(P,namelist),re:run(<<"zaa">>,P)}.
flip(Binary,Offset)-><<Prefix:Offset/binary,B:8,Rest/binary>>=Binary,<<Prefix/binary,(B bxor 1):8,Rest/binary>>.
fallback(E)->setelement(2,E,flip(element(2,E),12)).
probe(inspect_no_names,_Input)->begin {ok,P}=re:compile(<<"(a)">>,[]),re:inspect(P,namelist) end;
probe(inspect_sorted_names,_Input)->begin {ok,P}=re:compile(<<"(?<z>a)(?<a>b)">>,[]),re:inspect(P,namelist) end;
probe(inspect_duplicate_names,_Input)->begin {ok,P}=re:compile(<<"(?J)(?<z>a)|(?<a>b)(?<z>c)">>,[]),re:inspect(P,namelist) end;
probe(inspect_changed_small_fields,_Input)->begin {ok,P}=re:compile(<<"(?<word>a)">>,[]),Q=setelement(4,setelement(3,setelement(2,P,-1),-7),999),re:inspect(Q,namelist) end;
probe(inspect_invalid_pattern,_Input)->re:inspect(invalid,namelist);
probe(inspect_invalid_tag,_Input)->re:inspect({wrong_pattern,0,0,0,notref},namelist);
probe(inspect_large_metadata,_Input)->re:inspect({re_pattern,1 bsl 80,0,0,notref},namelist);
probe(inspect_noninteger_metadata,_Input)->re:inspect({re_pattern,none,0,0,notref},namelist);
probe(inspect_invalid_item,_Input)->re:inspect({re_pattern,0,0,0,notref},unknown_item);
probe(run_first_true,_Input)->re:internal_run(<<"zab">>,<<"(a)(b)">>,[],true);
probe(run_first_false,_Input)->re:internal_run(<<"zab">>,<<"(a)(b)">>,[],false);
probe(run_first_false_nomatch,_Input)->re:internal_run(<<"zzz">>,<<"ab">>,[],false);
probe(run_compiled_both,_Input)->begin {ok,P}=re:compile(<<"a+">>,[]),{re:internal_run(<<"zaa">>,P,[],true),re:internal_run(<<"zaa">>,P,[],false)} end;
probe(run_valid_unicode_both,_Input)->begin {ok,P}=re:compile(<<195,169,"+">>,[unicode]),{re:internal_run(<<"z",195,169>>,P,[],true),re:internal_run(<<"z",195,169>>,P,[],false)} end;
probe(run_first_invalid_atom,_Input)->re:internal_run(<<"a">>,<<"a">>,[],first);
probe(run_first_invalid_integer,_Input)->re:internal_run(<<"a">>,<<"a">>,[],1);
probe(run_first_invalid_with_badoptions,_Input)->re:internal_run(invalid_subject,invalid_pattern,[invalid_option],first);
probe(run_invalid_options,_Input)->re:internal_run(<<"a">>,<<"a">>,[invalid_option],true);
probe(run_improper_options,_Input)->re:internal_run(<<"a">>,<<"a">>,[unicode|invalid_tail],true);
probe(run_false_badsubject,_Input)->re:internal_run(invalid_subject,<<"a">>,[],false);
probe(run_false_badpattern,_Input)->re:internal_run(<<"a">>,invalid_pattern,[],false);
probe(import_original_roundtrip,{E,_U})->observe_import(E);
probe(import_unicode_roundtrip,{_E,U})->observe_import(U);
probe(import_reuse_same_export,{E,_U})->{observe_import(E),observe_import(E)};
probe(import_ignored_fallback_fields,{E,_U})->observe_import(setelement(4,setelement(3,E,<<"[">>),invalid_options));
probe(import_extra_header,{E,_U})->begin H=element(2,E),observe_import(setelement(2,E,<<H/binary,0,1,2>>)) end;
probe(import_invalid_term,_Input)->re:import(invalid);
probe(import_invalid_tag,{E,_U})->re:import(setelement(1,E,invalid_export));
probe(import_short_header,{E,_U})->re:import(setelement(2,E,<<"re-PCRE2">>));
probe(import_wrong_header_title,{E,_U})->re:import(setelement(2,E,flip(element(2,E),0)));
probe(import_bad_crc,{E,_U})->re:import(setelement(2,E,flip(element(2,E),8)));
probe(import_corrupt_encoded_crc,{E,_U})->re:import(setelement(5,E,flip(element(5,E),0)));
probe(import_nonbinary_encoded,{E,_U})->re:import(setelement(5,E,invalid_encoded));
probe(import_version_fallback,{E,_U})->observe_import(fallback(E));
probe(import_version_ignores_encoded_type,{E,_U})->observe_import(setelement(5,fallback(E),invalid_encoded));
probe(import_fallback_unicode_options,{E,_U})->observe_import(setelement(4,fallback(E),[unicode]));
probe(import_fallback_bad_options,{E,_U})->re:import(setelement(4,fallback(E),[invalid_option]));
probe(import_fallback_bad_pattern,{E,_U})->re:import(setelement(3,fallback(E),<<"[">>));
probe(import_fallback_nonbinary_pattern,{E,_U})->re:import(setelement(3,fallback(E),invalid_pattern));
probe(wrong_inspect_arity,_Input)->apply(re,inspect,[invalid]);
probe(wrong_internal_arity,_Input)->apply(re,internal_run,[<<"a">>,<<"a">>,[]]);
probe(wrong_import_arity,_Input)->apply(re,import,[]).
