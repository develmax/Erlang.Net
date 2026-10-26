namespace Erlang.Differential;

public static class IdentifierOrderCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        new(
            "identifier-order-pid-serial-before-node",
            "-module(identifier_order_contract). -export([run/0]). run()->A=binary_to_term(<<131,88,119,6,122,64,104,111,115,116,0,0,0,0,0,0,0,0,0,0,0,9>>),B=binary_to_term(<<131,88,119,6,97,64,104,111,115,116,0,0,0,0,0,0,0,1,0,0,0,0>>),true=A<B,true=B>A,false=A==B,false=A=:=B,true={A}<{B},true=[A]<[B],M=#{B=>2,A=>1},2=map_size(M),1=maps:get(A,M),2=maps:get(B,M),true=lists:sort(maps:keys(M))=:=[A,B],true={A,B}=:=binary_to_term(term_to_binary({A,B})),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "identifier-order-pid-number-before-node",
            "-module(identifier_order_contract). -export([run/0]). run()->A=binary_to_term(<<131,88,119,6,122,64,104,111,115,116,0,0,0,1,0,0,0,0,0,0,0,9>>),B=binary_to_term(<<131,88,119,6,97,64,104,111,115,116,0,0,0,2,0,0,0,0,0,0,0,0>>),true=A<B,true=B>A,false=A==B,false=A=:=B,true={A}<{B},true=[A]<[B],M=#{B=>2,A=>1},2=map_size(M),1=maps:get(A,M),2=maps:get(B,M),true=lists:sort(maps:keys(M))=:=[A,B],true={A,B}=:=binary_to_term(term_to_binary({A,B})),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "identifier-order-pid-serial-before-number",
            "-module(identifier_order_contract). -export([run/0]). run()->A=binary_to_term(<<131,88,119,6,122,64,104,111,115,116,255,255,255,255,0,0,0,0,0,0,0,0>>),B=binary_to_term(<<131,88,119,6,97,64,104,111,115,116,0,0,0,0,0,0,0,1,0,0,0,0>>),true=A<B,true=B>A,false=A==B,false=A=:=B,true={A}<{B},true=[A]<[B],M=#{B=>2,A=>1},2=map_size(M),1=maps:get(A,M),2=maps:get(B,M),true=lists:sort(maps:keys(M))=:=[A,B],true={A,B}=:=binary_to_term(term_to_binary({A,B})),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "identifier-order-pid-number-before-creation",
            "-module(identifier_order_contract). -export([run/0]). run()->A=binary_to_term(<<131,88,119,6,97,64,104,111,115,116,0,0,0,1,0,0,0,2,0,0,0,9>>),B=binary_to_term(<<131,88,119,6,97,64,104,111,115,116,0,0,0,2,0,0,0,2,0,0,0,0>>),true=A<B,true=B>A,false=A==B,false=A=:=B,true={A}<{B},true=[A]<[B],M=#{B=>2,A=>1},2=map_size(M),1=maps:get(A,M),2=maps:get(B,M),true=lists:sort(maps:keys(M))=:=[A,B],true={A,B}=:=binary_to_term(term_to_binary({A,B})),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "identifier-order-pid-node-after-payload",
            "-module(identifier_order_contract). -export([run/0]). run()->A=binary_to_term(<<131,88,119,6,97,64,104,111,115,116,0,0,0,3,0,0,0,4,0,0,0,9>>),B=binary_to_term(<<131,88,119,6,122,64,104,111,115,116,0,0,0,3,0,0,0,4,0,0,0,0>>),true=A<B,true=B>A,false=A==B,false=A=:=B,true={A}<{B},true=[A]<[B],M=#{B=>2,A=>1},2=map_size(M),1=maps:get(A,M),2=maps:get(B,M),true=lists:sort(maps:keys(M))=:=[A,B],true={A,B}=:=binary_to_term(term_to_binary({A,B})),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "identifier-order-pid-creation-after-payload",
            "-module(identifier_order_contract). -export([run/0]). run()->A=binary_to_term(<<131,88,119,6,97,64,104,111,115,116,0,0,0,3,0,0,0,4,0,0,0,0>>),B=binary_to_term(<<131,88,119,6,97,64,104,111,115,116,0,0,0,3,0,0,0,4,255,255,255,255>>),true=A<B,true=B>A,false=A==B,false=A=:=B,true={A}<{B},true=[A]<[B],M=#{B=>2,A=>1},2=map_size(M),1=maps:get(A,M),2=maps:get(B,M),true=lists:sort(maps:keys(M))=:=[A,B],true={A,B}=:=binary_to_term(term_to_binary({A,B})),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "identifier-order-pid-unicode-node",
            "-module(identifier_order_contract). -export([run/0]). run()->A=binary_to_term(<<131,88,119,8,238,128,128,64,104,111,115,116,0,0,0,3,0,0,0,4,0,0,0,0>>),B=binary_to_term(<<131,88,119,9,240,144,128,128,64,104,111,115,116,0,0,0,3,0,0,0,4,0,0,0,0>>),true=A<B,true=B>A,false=A==B,false=A=:=B,true={A}<{B},true=[A]<[B],M=#{B=>2,A=>1},2=map_size(M),1=maps:get(A,M),2=maps:get(B,M),true=lists:sort(maps:keys(M))=:=[A,B],true={A,B}=:=binary_to_term(term_to_binary({A,B})),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "identifier-order-port-unicode-node",
            "-module(identifier_order_contract). -export([run/0]). run()->A=binary_to_term(<<131,120,119,8,238,128,128,64,104,111,115,116,0,0,0,0,0,0,0,9,0,0,0,9>>),B=binary_to_term(<<131,120,119,9,240,144,128,128,64,104,111,115,116,0,0,0,0,0,0,0,0,0,0,0,0>>),true=A<B,true=B>A,false=A==B,false=A=:=B,true={A}<{B},true=[A]<[B],M=#{B=>2,A=>1},2=map_size(M),1=maps:get(A,M),2=maps:get(B,M),true=lists:sort(maps:keys(M))=:=[A,B],true={A,B}=:=binary_to_term(term_to_binary({A,B})),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "identifier-order-port-node-before-id",
            "-module(identifier_order_contract). -export([run/0]). run()->A=binary_to_term(<<131,120,119,6,97,64,104,111,115,116,255,255,255,255,255,255,255,255,0,0,0,9>>),B=binary_to_term(<<131,120,119,6,122,64,104,111,115,116,0,0,0,0,0,0,0,0,0,0,0,0>>),true=A<B,true=B>A,false=A==B,false=A=:=B,true={A}<{B},true=[A]<[B],M=#{B=>2,A=>1},2=map_size(M),1=maps:get(A,M),2=maps:get(B,M),true=lists:sort(maps:keys(M))=:=[A,B],true={A,B}=:=binary_to_term(term_to_binary({A,B})),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "identifier-order-port-creation-before-id",
            "-module(identifier_order_contract). -export([run/0]). run()->A=binary_to_term(<<131,120,119,6,97,64,104,111,115,116,0,0,0,0,0,0,0,9,0,0,0,0>>),B=binary_to_term(<<131,120,119,6,97,64,104,111,115,116,0,0,0,0,0,0,0,0,0,0,0,9>>),true=A<B,true=B>A,false=A==B,false=A=:=B,true={A}<{B},true=[A]<[B],M=#{B=>2,A=>1},2=map_size(M),1=maps:get(A,M),2=maps:get(B,M),true=lists:sort(maps:keys(M))=:=[A,B],true={A,B}=:=binary_to_term(term_to_binary({A,B})),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "identifier-order-pid-equal-payload-node-creation",
            "-module(identifier_order_contract). -export([run/0]). run()->A=binary_to_term(<<131,88,119,6,97,64,104,111,115,116,0,0,0,3,0,0,0,4,0,0,0,5>>),B=binary_to_term(<<131,88,119,6,97,64,104,111,115,116,0,0,0,3,0,0,0,4,0,0,0,5>>),true=A=:=B,false=A<B,false=A>B,M=#{A=>1,B=>2},1=map_size(M),2=maps:get(A,M),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
    ];
}
