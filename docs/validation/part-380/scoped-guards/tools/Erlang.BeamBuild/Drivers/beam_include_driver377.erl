-module(beam_include_driver377).
-export([run/0, validate_forms/4]).
-include_lib("kernel/include/file.hrl").

run() ->
    put(driver_phase, request),
    put(driver_context, #{}),
    put(compile_invoked, false),
    try
        [RequestName] = init:get_plain_arguments(),
        {ok, Cwd} = file:get_cwd(),
        Root = normalize(Cwd),
        no_symlink_ancestors(Root),
        RequestPath = absolute_external(RequestName, Root),
        {ok, RequestBytes} = file:read_file(RequestPath),
        Request = decode_request(RequestBytes),
        exact_keys(Request, [<<"Source">>, <<"Module">>, <<"Includes">>,
                             <<"Defines">>, <<"DeclaredFiles">>, <<"Output">>]),
        Output = absolute_external(text(maps:get(<<"Output">>, Request)), Root),
        ok = file:make_dir(Output),
        put(driver_output, Output),
        put(driver_context, #{<<"RequestSha256">> => hash(RequestBytes),
                              <<"InputRoot">> => unicode:characters_to_binary(Root)}),
        Report = execute(Request, Root, Output),
        finish(Report#{<<"Status">> => <<"PASS_COMPILED_VALIDATED_FORMS">>}, 0)
    catch
        Class:Reason:Stack ->
            Failure = term_to_binary({Class, Reason, Stack}),
            finish(#{<<"Status">> => <<"FAILED_NO_ACCEPTED_BUILD">>,
                     <<"Phase">> => atom_to_binary(get(driver_phase)),
                     <<"FailureEtfBase64">> => base64:encode(Failure),
                     <<"FailureSha256">> => hash(Failure)}, 1)
    end.

execute(Request, Root, Output) ->
    put(driver_phase, input_admission),
    Source = relative_text(maps:get(<<"Source">>, Request)),
    ModuleName = maps:get(<<"Module">>, Request),
    true = is_binary(ModuleName),
    match = re:run(ModuleName, <<"^[a-z][a-zA-Z0-9_@]*$">>, [{capture, none}]),
    Module = binary_to_atom(ModuleName, utf8),
    true = filename:extension(Source) =:= ".erl",
    Includes = [relative_text(Value) || Value <- array(maps:get(<<"Includes">>, Request))],
    lists:foreach(fun(Name) -> require_directory(inside(Name, Root)) end, Includes),
    Defines = definitions(array(maps:get(<<"Defines">>, Request))),
    Declared = declarations(array(maps:get(<<"DeclaredFiles">>, Request)), Root),
    SourceKey = key(inside(Source, Root)),
    true = maps:is_key(SourceKey, Declared),
    true = maps:size(Declared) > 0,
    Before = inventory(Root),
    admit_inventory(Before, Declared),
    lists:foreach(fun(Row) -> scan_policy(Row, Root, Includes, Declared) end, Before),
    put(driver_context, maps:merge(get(driver_context),
        #{<<"Source">> => unicode:characters_to_binary(Source),
          <<"Module">> => ModuleName,
          <<"Includes">> => [unicode:characters_to_binary(I) || I <- Includes],
          <<"InitialEffectiveRoots">> => [unicode:characters_to_binary(I) || I <- [filename:dirname(Source)|Includes]],
          <<"InputFiles">> => Before,
          <<"DefinesEtfBase64">> => base64:encode(term_to_binary(Defines))})),
    put(driver_phase, epp),
    Epp = epp:parse_file(Source, [{includes, Includes}, {macros, Defines},
                                  {source_name, Source}, {default_encoding, utf8}]),
    EppFile = write_etf(Output, "epp-result.etf", Epp),
    put(driver_context, maps:merge(get(driver_context), #{<<"EppResult">> => EppFile})),
    {ok, Forms} = Epp,
    Diagnostics = [F || F <- Forms, element(1, F) =:= error orelse element(1, F) =:= warning],
    put(driver_context, maps:merge(get(driver_context),
        #{<<"EppDiagnosticsEtfBase64">> => base64:encode(term_to_binary(Diagnostics))})),
    put(driver_phase, expanded_form_policy),
    FileAnnotations = validate_forms(Forms, Module, Root, Declared),
    FormsFile = write_etf(Output, "validated-forms.etf", Forms),
    put(driver_context, maps:merge(get(driver_context),
        #{<<"ValidatedForms">> => FormsFile,
          <<"FileAnnotationsEtfBase64">> => base64:encode(term_to_binary(FileAnnotations))})),
    Before = inventory(Root),
    put(driver_phase, compile),
    put(compile_invoked, true),
    Compile = compile:noenv_forms(Forms, [binary, return_errors, return_warnings]),
    CompileFile = write_etf(Output, "compile-result.etf", Compile),
    CompileDiagnostics = case Compile of
        {ok, _, _, Ws0} -> {[], Ws0};
        {ok, _, _} -> {[], []};
        {error, Es0, Ws0} -> {Es0, Ws0};
        _ -> {unexpected_compile_result, Compile}
    end,
    put(driver_context, maps:merge(get(driver_context),
        #{<<"CompileResult">> => CompileFile,
          <<"CompileDiagnosticsEtfBase64">> => base64:encode(term_to_binary(CompileDiagnostics))})),
    {Beam, Warnings} = case Compile of
        {ok, Module, Binary, Ws} when is_binary(Binary) -> {Binary, Ws};
        {ok, Module, Binary} when is_binary(Binary) -> {Binary, []};
        Other -> error({compile_failed, Other})
    end,
    put(driver_phase, settlement),
    Before = inventory(Root),
    BeamName = binary_to_list(ModuleName) ++ ".beam",
    ok = file:write_file(filename:join(Output, BeamName), Beam, [exclusive]),
    #{<<"BeamFile">> => unicode:characters_to_binary(BeamName),
      <<"BeamSha256">> => hash(Beam), <<"BeamBytes">> => byte_size(Beam),
      <<"CompileWarningsEtfBase64">> => base64:encode(term_to_binary(Warnings)),
      <<"FormsPassedUnchanged">> => true, <<"InputClosureUnchanged">> => true}.

decode_request(Bytes) ->
    Push = fun(K, V, Acc) ->
        false = lists:keymember(K, 1, Acc),
        [{K, V}|Acc]
    end,
    {Value, ok, Rest} = json:decode(Bytes, ok, #{object_push => Push}),
    true = lists:all(fun(C) -> lists:member(C, " \t\r\n") end, binary_to_list(Rest)),
    Value.

exact_keys(Map, Keys) when is_map(Map) ->
    true = lists:sort(maps:keys(Map)) =:= lists:sort(Keys).

array(Value) when is_list(Value) -> Value.

text(Value) when is_binary(Value) ->
    Chars = unicode:characters_to_list(Value, utf8),
    true = is_list(Chars),
    true = Chars =/= [],
    false = lists:member(0, Chars),
    Chars.

relative_text(Value) ->
    Chars = text(Value),
    relative = filename:pathtype(Chars),
    false = lists:member($\\, Chars),
    false = lists:member($:, Chars),
    Chars.

definitions(Rows) ->
    Names = [maps:get(<<"Name">>, Row) || Row <- Rows],
    true = length(Names) =:= length(lists:usort(Names)),
    [definition(Row) || Row <- Rows].

definition(Row) ->
    Name = maps:get(<<"Name">>, Row),
    true = is_binary(Name),
    NameText = unicode_string(Name),
    true = length(NameText) > 0 andalso length(NameText) =< 255,
    Macro = binary_to_atom(Name, utf8),
    case maps:is_key(<<"Value">>, Row) of
        false -> exact_keys(Row, [<<"Name">>]), Macro;
        true ->
            exact_keys(Row, [<<"Name">>, <<"Value">>]),
            case maps:get(<<"Value">>, Row) of
                null -> Macro;
                Value -> {Macro, define_value(Value)}
            end
    end.

define_value(#{<<"Kind">> := <<"Atom">>} = Value) ->
    exact_keys(Value, [<<"Kind">>, <<"Text">>]),
    Atom = unicode_string(maps:get(<<"Text">>, Value)),
    true = length(Atom) =< 255,
    binary_to_atom(unicode:characters_to_binary(Atom), utf8);
define_value(#{<<"Kind">> := <<"String">>} = Value) ->
    exact_keys(Value, [<<"Kind">>, <<"Text">>]),
    unicode_string(maps:get(<<"Text">>, Value));
define_value(#{<<"Kind">> := <<"Integer">>, <<"Text">> := Number} = Value) when is_binary(Number) ->
    exact_keys(Value, [<<"Kind">>, <<"Text">>]),
    match = re:run(Number, <<"^(0|-?[1-9][0-9]*)$">>, [{capture, none}]),
    binary_to_integer(Number).

unicode_string(Binary) when is_binary(Binary) ->
    Value = unicode:characters_to_list(Binary, utf8),
    true = is_list(Value),
    Value.

declarations(Rows, Root) ->
    lists:foldl(fun(Row, Acc) ->
        exact_keys(Row, [<<"Path">>, <<"Sha256">>]),
        Name = relative_text(maps:get(<<"Path">>, Row)),
        true = lists:member(filename:extension(Name), [".erl", ".hrl"]),
        Digest = maps:get(<<"Sha256">>, Row),
        true = is_binary(Digest),
        match = re:run(Digest, <<"^[0-9A-F]{64}$">>, [{capture, none}]),
        PathKey = key(inside(Name, Root)),
        false = maps:is_key(PathKey, Acc),
        Acc#{PathKey => Digest}
    end, #{}, Rows).

inventory(Root) ->
    lists:sort(inventory_directory(Root, Root)).

inventory_directory(Directory, Root) ->
    require_directory(Directory),
    {ok, Names} = file:list_dir(Directory),
    lists:append([begin
        Path = filename:join(Directory, Name),
        {ok, Info} = file:read_link_info(Path),
        case Info#file_info.type of
            directory -> inventory_directory(Path, Root);
            regular ->
                {ok, Bytes} = file:read_file(Path),
                [#{<<"Path">> => unicode:characters_to_binary(relative_path(Path, Root)),
                   <<"Bytes">> => byte_size(Bytes), <<"Sha256">> => hash(Bytes)}];
            Type -> error({unsupported_input_file_type, Type, Path})
        end
    end || Name <- Names]).

admit_inventory(Rows, Declared) ->
    Actual = maps:from_list([{key(normalize(text(maps:get(<<"Path">>, Row)))), maps:get(<<"Sha256">>, Row)} || Row <- Rows]),
    true = Actual =:= Declared.

scan_policy(Row, Root, Includes, Declared) ->
    Name = text(maps:get(<<"Path">>, Row)),
    {ok, Bytes} = file:read_file(inside(Name, Root)),
    {ok, Tokens, _} = erl_scan:string(unicode_string(Bytes)),
    Forms = token_forms(Tokens, [], []),
    lists:foreach(fun(Form) -> raw_form(Form, Name, Root, Includes, Declared) end, Forms).

token_forms([], [], Acc) -> lists:reverse(Acc);
token_forms([], Partial, _Acc) -> error({unterminated_source_form, Partial});
token_forms([{dot, _} = Dot|Rest], Current, Acc) ->
    token_forms(Rest, [], [lists:reverse([Dot|Current])|Acc]);
token_forms([Token|Rest], Current, Acc) -> token_forms(Rest, [Token|Current], Acc).

raw_form([{'-', _}, {atom, _, include}|Rest], Name, Root, Includes, Declared) ->
    [{'(', _}|Tail] = Rest,
    {Strings, [{')', _}, {dot, _}]} = lists:splitwith(fun(T) -> element(1, T) =:= string end, Tail),
    true = Strings =/= [],
    Include = lists:append([S || {string, _, S} <- Strings]),
    false = lists:member($$, Include),
    Relative = relative_text(unicode:characters_to_binary(Include)),
    resolve_include(Relative, [filename:dirname(Name)|Includes], Root, Declared);
raw_form([{'-', _}, {atom, _, Attribute}|_], _Name, _Root, _Includes, _Declared) ->
    true = lists:member(Attribute, [module, export, define, undef, ifdef, ifndef, 'if', elif, 'else', endif]);
raw_form([{'-', _}, {'if', _}|_], _Name, _Root, _Includes, _Declared) -> ok;
raw_form([{'-', _}, {'else', _}|_], _Name, _Root, _Includes, _Declared) -> ok;
raw_form([{'-', _}|_], _Name, _Root, _Includes, _Declared) -> error(macro_or_invalid_attribute_name);
raw_form([{'?', _}|_], _Name, _Root, _Includes, _Declared) -> error(top_level_macro_form);
raw_form(_Function, _Name, _Root, _Includes, _Declared) -> ok.

resolve_include(_Name, [], _Root, _Declared) -> error(undeclared_or_missing_include);
resolve_include(Name, [Directory|Rest], Root, Declared) ->
    Path = inside(filename:join(Directory, Name), Root),
    case maps:is_key(key(Path), Declared) of
        true -> ok;
        false -> resolve_include(Name, Rest, Root, Declared)
    end.

validate_forms(Forms, Module, Root, Declared) ->
    true = is_list(Forms) andalso Forms =/= [],
    {eof, _} = lists:last(Forms),
    State = lists:foldl(fun(Form, Acc) -> validate_form(Form, Module, Root, Declared, Acc) end,
                        #{modules => 0, eof => 0, files => []}, Forms),
    1 = maps:get(modules, State),
    1 = maps:get(eof, State),
    lists:reverse(maps:get(files, State)).

validate_form({attribute, _, module, Module}, Module, _Root, _Declared, State) ->
    State#{modules := maps:get(modules, State)+1};
validate_form({attribute, _, export, Exports}, _Module, _Root, _Declared, State) when is_list(Exports) ->
    true = lists:all(fun({F, A}) -> is_atom(F) andalso is_integer(A) andalso A >= 0 andalso A =< 255; (_) -> false end, Exports),
    State;
validate_form({attribute, Anno, file, {Name, Line}} = Original, _Module, Root, Declared, State) when is_list(Name), is_integer(Line), Line > 0 ->
    Path = normalize(Name),
    true = under(Path, Root),
    true = maps:is_key(key(Path), Declared),
    _ = Anno,
    State#{files := [Original|maps:get(files, State)]};
validate_form({function, _, Name, Arity, Clauses}, _Module, _Root, _Declared, State) when is_atom(Name), is_integer(Arity), Arity >= 0, Arity =< 255, is_list(Clauses) -> State;
validate_form({eof, _}, _Module, _Root, _Declared, State) -> State#{eof := maps:get(eof, State)+1};
validate_form(Form, _Module, _Root, _Declared, _State) -> error({unsupported_expanded_form, Form}).

require_directory(Path) ->
    {ok, #file_info{type = directory}} = file:read_link_info(Path),
    ok.

inside(Name, Root) ->
    Path = normalize(filename:join(Root, Name)),
    true = under(Path, Root),
    Path.

absolute_external(Name, Root) ->
    absolute = filename:pathtype(Name),
    Path = normalize(Name),
    false = under(Path, Root),
    no_symlink_ancestors(Path),
    Path.

no_symlink_ancestors(Path) ->
    no_symlink_parts(filename:split(Path), []).

no_symlink_parts([], _Previous) -> ok;
no_symlink_parts([Part|Rest], Previous) ->
    Parts = Previous ++ [Part],
    case file:read_link_info(filename:join(Parts)) of
        {ok, #file_info{type = symlink}} -> error(external_path_symlink);
        {ok, _} -> no_symlink_parts(Rest, Parts);
        {error, enoent} -> ok;
        Other -> error({external_path_admission, Other})
    end.

normalize(Name) ->
    filename:join(lists:reverse(normal_parts(filename:split(filename:absname(Name)), []))).

normal_parts([], Acc) -> Acc;
normal_parts(["."|Rest], Acc) -> normal_parts(Rest, Acc);
normal_parts([".."|Rest], [_Parent|Acc]) when Acc =/= [] -> normal_parts(Rest, Acc);
normal_parts([".."|_], _Acc) -> error(path_above_root);
normal_parts([Part|Rest], Acc) -> normal_parts(Rest, [Part|Acc]).

key(Path) ->
    case os:type() of {win32, _} -> string:casefold(Path); _ -> Path end.

under(Path, Root) -> lists:prefix(filename:split(key(Root)), filename:split(key(Path))).

relative_path(Path, Root) ->
    filename:join(lists:nthtail(length(filename:split(Root)), filename:split(Path))).

write_etf(Output, Name, Value) ->
    Bytes = term_to_binary(Value),
    ok = file:write_file(filename:join(Output, Name), Bytes, [exclusive]),
    #{<<"Path">> => unicode:characters_to_binary(Name), <<"Sha256">> => hash(Bytes), <<"Bytes">> => byte_size(Bytes)}.

hash(Bytes) -> binary:encode_hex(crypto:hash(sha256, Bytes)).

finish(Report, Exit) ->
    Final = maps:merge(get(driver_context), Report#{<<"CompileInvoked">> => get(compile_invoked),
        <<"FixedCompileOptions">> => [<<"binary">>, <<"return_errors">>, <<"return_warnings">>],
        <<"CompilerEntry">> => <<"compile:noenv_forms/2">>,
        <<"NoAnnotationRewrite">> => true}),
    Json = json:encode(Final),
    case get(driver_output) of
        undefined -> ok;
        Output -> ok = file:write_file(filename:join(Output, "report.json"), Json, [exclusive])
    end,
    io:put_chars([Json, "\n"]),
    erlang:halt(Exit).
