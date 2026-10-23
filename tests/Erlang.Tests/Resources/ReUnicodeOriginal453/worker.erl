-module(beam_re_unicode453).
-export([run/0,run_stacks/0,worker/2]).

cases() -> [kernel_host_ascii,kernel_host_latin1,kernel_host_rejected,kernel_name_valid,kernel_name_rejected,
            empty_pattern,syntax_error,unicode_syntax_error,nested_pattern,improper_binary_pattern,
            scalar_pattern,invalid_pattern_scalar,surrogate_pattern,negative_pattern_scalar,
            malformed_pattern_tail,malformed_pattern_item,incomplete_pattern_binary,invalid_pattern_binary,
            nested_subject,improper_binary_subject,scalar_subject,empty_subject,no_match_subject,
            invalid_subject_scalar,surrogate_subject,negative_subject_scalar,malformed_subject_tail,
            malformed_subject_item,incomplete_subject_binary,invalid_subject_binary,nonunicode_binary_control,
            unicode_binary_control,bad_compile_option,unicode_nonlist_pattern,unicode_nonlist_subject].

run() -> [{Case,capture(Case,default)} || Case <- cases()].

run_stacks() -> [{Depth,[{Case,capture(Case,Depth)} || Case <- cases()]} || Depth <- [default,0,1,2,8]].

capture(Case,Depth) ->
    {Pid,Ref}=spawn_monitor(?MODULE,worker,[Case,Depth]),
    receive
        {'DOWN',Ref,process,Pid,{observation,Value}} -> Value;
        {'DOWN',Ref,process,Pid,Reason} -> {unexpected_down,Reason}
    after 10000 -> error(observer_timeout)
    end.

worker(Case,Depth) ->
    case Depth of default -> ok; _ -> erlang:system_flag(backtrace_depth,Depth) end,
    Value=try probe(Case) of Result -> {ok,Result}
          catch Class:Reason:Stack -> {exception,Class,Reason,Stack}
          end,
    exit({observation,Value}).

compiled(Pattern) ->
    case re:compile(Pattern,[unicode]) of
        {ok,{re_pattern,N,U,C,R}} -> {ok,{re_pattern,N,U,C,is_reference(R)}};
        Other -> Other
    end.

match(Pattern,Subject) ->
    {ok,RE}=re:compile(Pattern,[unicode]),
    re:run(Subject,RE).

probe(kernel_host_ascii) -> match("^[!-ÿ]*$","127.0.0.1");
probe(kernel_host_latin1) -> match("^[!-ÿ]*$",[255]);
probe(kernel_host_rejected) -> match("^[!-ÿ]*$",[256]);
probe(kernel_name_valid) -> match("^[0-9A-Za-z_\\-]+$","node_453-a");
probe(kernel_name_rejected) -> match("^[0-9A-Za-z_\\-]+$","bad name");
probe(empty_pattern) -> compiled([]);
probe(syntax_error) -> compiled("(");
probe(unicode_syntax_error) -> compiled([233,$(]);
probe(nested_pattern) -> compiled([<<"^">>,[233],<<"+$">>]);
probe(improper_binary_pattern) -> compiled([$^,233|<<"+$">>]);
probe(scalar_pattern) -> compiled([128512]);
probe(invalid_pattern_scalar) -> compiled([1114112]);
probe(surrogate_pattern) -> compiled([55296]);
probe(negative_pattern_scalar) -> compiled([-1]);
probe(malformed_pattern_tail) -> compiled([$a|invalid_tail]);
probe(malformed_pattern_item) -> compiled([$a,invalid_item]);
probe(incomplete_pattern_binary) -> compiled([<<195>>]);
probe(invalid_pattern_binary) -> compiled([<<255>>]);
probe(nested_subject) -> match([233,$+],[$z,[233],<<195,169>>]);
probe(improper_binary_subject) -> match([233,$+],[$z,233|<<195,169>>]);
probe(scalar_subject) -> match([128512,$+],[$z,128512,128512]);
probe(empty_subject) -> match("^.*$",[]);
probe(no_match_subject) -> match("^a+$","bbb");
probe(invalid_subject_scalar) -> match("^.*$",[1114112]);
probe(surrogate_subject) -> match("^.*$",[55296]);
probe(negative_subject_scalar) -> match("^.*$",[-1]);
probe(malformed_subject_tail) -> match("^.*$",[$a|invalid_tail]);
probe(malformed_subject_item) -> match("^.*$",[$a,invalid_item]);
probe(incomplete_subject_binary) -> match("^.*$",[<<195>>]);
probe(invalid_subject_binary) -> match("^.*$",[<<255>>]);
probe(nonunicode_binary_control) -> re:run(<<"aa">>,<<"a+">>);
probe(unicode_binary_control) -> match(<<195,169,$+>>,<<$z,195,169>>);
probe(bad_compile_option) -> re:compile("a",[unicode,invalid_option]);
probe(unicode_nonlist_pattern) -> re:compile(invalid_pattern,[unicode]);
probe(unicode_nonlist_subject) -> match("^.*$",invalid_subject).
