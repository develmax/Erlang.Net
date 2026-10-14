namespace Erlang.Differential;

public static class AtomScalarValidationCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        new(
            "atom-scalar-validation-supplementary-max",
            "-module(atom_scalar_validation_contract). -export([run/0]). run()->L=lists:duplicate(255,65536),A=list_to_atom(L),L=atom_to_list(A),B=atom_to_binary(A),1020=byte_size(B),A=binary_to_atom(B,utf8),A=binary_to_existing_atom(B,unicode),A=list_to_existing_atom(L),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "atom-scalar-validation-supplementary-over",
            "-module(atom_scalar_validation_contract). -export([run/0]). run()->L=lists:duplicate(256,65536),B=list_to_binary(lists:flatten(lists:duplicate(256,[240,144,128,128]))),ok=try list_to_atom(L) of _->unexpected catch error:system_limit->ok end,ok=try list_to_existing_atom(L) of _->unexpected catch error:badarg->ok end,ok=try binary_to_atom(B,utf8) of _->unexpected catch error:system_limit->ok end,ok=try binary_to_existing_atom(B,unicode) of _->unexpected catch error:badarg->ok end,done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "atom-scalar-validation-surrogate-list-no-pollution",
            "-module(atom_scalar_validation_contract). -export([run/0]). run()->C=erlang:system_info(atom_count),ok=try list_to_atom([55296]) of _->unexpected catch error:badarg->ok end,ok=try list_to_atom([56319]) of _->unexpected catch error:badarg->ok end,ok=try list_to_atom([56320]) of _->unexpected catch error:badarg->ok end,ok=try list_to_atom([57343]) of _->unexpected catch error:badarg->ok end,C=erlang:system_info(atom_count),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "atom-scalar-validation-surrogate-utf8-no-pollution",
            "-module(atom_scalar_validation_contract). -export([run/0]). run()->C=erlang:system_info(atom_count),ok=try binary_to_atom(<<237,160,128>>,utf8) of _->unexpected catch error:badarg->ok end,ok=try binary_to_atom(<<237,175,191>>,utf8) of _->unexpected catch error:badarg->ok end,ok=try binary_to_atom(<<237,176,128>>,utf8) of _->unexpected catch error:badarg->ok end,ok=try binary_to_atom(<<237,191,191>>,utf8) of _->unexpected catch error:badarg->ok end,C=erlang:system_info(atom_count),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "atom-scalar-validation-replacement-and-supplementary-distinct",
            "-module(atom_scalar_validation_contract). -export([run/0]). run()->A=list_to_atom([65533]),B=list_to_atom([65536]),false=A=:=B,M=#{A=>1,B=>2},2=map_size(M),1=maps:get(A,M),2=maps:get(B,M),A=binary_to_atom(<<239,191,189>>),B=binary_to_atom(<<240,144,128,128>>),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "atom-scalar-validation-null-and-noncharacters",
            "-module(atom_scalar_validation_contract). -export([run/0]). run()->L=[0,65534,65535,1114111],A=list_to_atom(L),L=atom_to_list(A),B=atom_to_binary(A),A=binary_to_atom(B),A=binary_to_existing_atom(B),A=list_to_existing_atom(L),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "atom-scalar-validation-list-limit-before-invalid-tail",
            "-module(atom_scalar_validation_contract). -export([run/0]). run()->L=lists:duplicate(255,97)++[55296],ok=try list_to_atom(L) of _->unexpected catch error:system_limit->ok end,ok=try list_to_existing_atom(L) of _->unexpected catch error:badarg->ok end,done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "atom-scalar-validation-binary-limit-before-invalid-tail",
            "-module(atom_scalar_validation_contract). -export([run/0]). run()->B=list_to_binary(lists:duplicate(255,97)++[237,160,128]),ok=try binary_to_atom(B) of _->unexpected catch error:system_limit->ok end,ok=try binary_to_existing_atom(B) of _->unexpected catch error:badarg->ok end,done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "atom-scalar-validation-real-count-insertion-and-existing-deltas",
            "-module(atom_scalar_validation_contract). -export([run/0]). run()->L=\"atom_scalar_delta_native_272_\"++integer_to_list(erlang:unique_integer([positive])),C=erlang:system_info(atom_count),A=list_to_atom(L),D=erlang:system_info(atom_count),true=(D=:=C+1),A=list_to_atom(L),D=erlang:system_info(atom_count),A=binary_to_atom(list_to_binary(L)),D=erlang:system_info(atom_count),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "atom-scalar-validation-lexer-valid-quoted",
            "-module(atom_scalar_validation_contract). -export([run/0]). run()->'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "atom-scalar-validation-lexer-valid-bare",
            "-module(atom_scalar_validation_contract). -export([run/0]). run()->aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa,done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "atom-scalar-validation-lexer-valid-variable",
            "-module(atom_scalar_validation_contract). -export([run/0]). run()->AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=done,AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "atom-scalar-validation-lexer-valid-quoted-supplementary",
            "-module(atom_scalar_validation_contract). -export([run/0]). run()->'\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00\ud800\udc00',done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
    ];
}
