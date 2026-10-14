
%% simplify_apply(Call0, Mod, Func, Args) -> Call
%%  Simplify an apply/3 to a call if the number of arguments
%%  are known at compile time.

simplify_apply(Call, Mod, Func, Args0) ->
    case is_atom_or_var(Mod) andalso is_atom_or_var(Func) of
	true ->
            case get_fixed_args(Args0, []) of
                error ->
                    Call;
                {ok,Args} ->
                    Call#c_call{module=Mod,name=Func,args=Args}
            end;
	false ->
            Call
    end.
is_atom_or_var(#c_literal{val=Atom}) when is_atom(Atom) -> true;
is_atom_or_var(#c_var{}) -> true;
is_atom_or_var(_) -> false.

simplify_fun_apply(#c_call{anno=Anno}=Call, Fun, Args0) ->
    case get_fixed_args(Args0, []) of
        error ->
            Call;
        {ok,Args} ->
            #c_apply{anno=Anno,op=Fun,args=Args}
    end.

get_fixed_args(#c_literal{val=MoreArgs0}, Args)
  when length(MoreArgs0) >= 0 ->
    MoreArgs = [#c_literal{val=Arg} || Arg <- MoreArgs0],
    {ok,reverse(Args, MoreArgs)};
get_fixed_args(#c_cons{hd=Arg,tl=T}, Args) ->
    get_fixed_args(T, [Arg|Args]);
get_fixed_args(_, _) -> error.

%% clause(Clause, Cepxr, Context, Sub) -> Clause.

clause(#c_clause{pats=Ps0}=Cl, Cexpr, Ctxt, Sub0) ->
