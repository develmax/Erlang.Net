namespace Erlang.Differential;

public static class IndirectLoadNifCaseNames
{
    public const string Function = "load_nif";
    public const string MfaFixture = "erlang-load-nif-indirect-apply-invalid-path";
    public const string Module = "load_nif_indirect_contract";
    public const string Error = "error";
    public const string BadLibrary = "bad_lib";
    public const string Message = "load_nif/2 must be explicitly called from the NIF module. It cannot be called through apply/3.";
    public const string Prefix = "erlang-load-nif-indirect-";
    public const string ApplyHelper = "dynamic_apply(Module,Function,Arguments)->erlang:apply(Module,Function,Arguments).";
    public const string ApplyFunctionSignature = "dynamic_apply/3";
    public const string ApplyInvocationPrefix = "dynamic_apply(";
    public const string QualifiedApplyInvocationPrefix = ":dynamic_apply(";
    public const string ModuleDeclarationPrefix = "-module(";
    public const string RunAndHelperExportPrefix = "). -export([run/0,";
    public const string RunBodyPrefix = "]). run()->";
    public const string HelperSeparator = ". ";
    public const string ApplyInvalidPathCase = "apply-invalid-path";
    public const string ApplyInvalidPathBody = "dynamic_apply(erlang,load_nif,[42,[]])";
    public const string ApplyMissingPathCase = "apply-missing-path";
    public const string ApplyMissingPathBody = "dynamic_apply(erlang,load_nif,[\"missing_nif_allocator_audit\",[]])";
    public const string OpaqueArgumentsCase = "apply-opaque-map-and-fun-arguments";
    public const string OpaqueArgumentsBody = "dynamic_apply(erlang,load_nif,[#{bad=>path},fun()->error(indirect_info_invoked) end])";
    public const string ImproperPathCase = "apply-improper-path-before-validation";
    public const string ImproperPathBody = "dynamic_apply(erlang,load_nif,[[97|not_a_list],#{info=>ignored}])";
    public const string FirstEffectAtom = "first";
    public const string SecondEffectAtom = "second";
    public const string ApplyOrderCase = "apply-argument-order";
    public const string ApplyOrderBody = "Result=dynamic_apply(erlang,load_nif,[begin put(indirect_order,first),42 end,begin put(indirect_order,{get(indirect_order),second}),[] end]),{Result,get(indirect_order)}";
    public const string ApplyFailureCase = "apply-argument-failure-before-bif";
    public const string ApplyFailureBody = "try dynamic_apply(erlang,load_nif,[begin put(indirect_order,first),42 end,begin put(indirect_order,{get(indirect_order),second}),error(second_argument_failed) end]) of _->unexpected catch error:second_argument_failed->{failed,get(indirect_order)} end";
    public const string FailedBeforeBifAtom = "failed";
    public const string FunReferenceCase = "fun-reference-invalid-path";
    public const string FunReferenceBody = "load_nif_indirect_contract:dynamic_fun(fun erlang:load_nif/2,42,[])";
    public const string FunFunctionSignature = ",dynamic_fun/3";
    public const string FunHelperSeparator = " ";
    public const string FunHelper = "dynamic_fun(F,Path,Info)->F(Path,Info).";
}
