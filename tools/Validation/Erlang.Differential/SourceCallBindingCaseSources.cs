namespace Erlang.Differential;

public static class SourceCallBindingCaseSources
{
    public const string GuardOrdinaryGuard = """
-module(oracle_guard_binding_ordinary_guard).
-export([run/0]).
run() -> yes=pick(a),no=pick(1),done.
pick(X) when is_atom(X) -> yes; pick(_) -> no.
""";

    public const string GuardLocalGuardShadow = """
-module(oracle_guard_binding_local_guard_shadow).
-export([run/0]).
run() -> pick(a).
pick(X) when is_atom(X) -> yes; pick(_) -> no.
is_atom(_) -> true.
""";

    public const string GuardLocalGuardShadowSuppressed = """
-module(oracle_guard_binding_local_guard_shadow_suppressed).
-export([run/0]).
-compile({no_auto_import,[is_atom/1]}).
run() -> pick(a).
pick(X) when is_atom(X) -> yes; pick(_) -> no.
is_atom(_) -> true.
""";

    public const string GuardQualifiedGuardLocalShadow = """
-module(oracle_guard_binding_qualified_guard_local_shadow).
-export([run/0]).
-compile({no_auto_import,[is_atom/1]}).
run() -> yes=pick(a),no=pick(1),local=is_atom(a),done.
pick(X) when erlang:is_atom(X) -> yes; pick(_) -> no.
is_atom(_) -> local.
""";

    public const string GuardSuppressedGuard = """
-module(oracle_guard_binding_suppressed_guard).
-export([run/0]).
-compile({no_auto_import,[is_atom/1]}).
run() -> pick(a).
pick(X) when is_atom(X) -> yes; pick(_) -> no.
""";

    public const string GuardSuppressedOrdinary = """
-module(oracle_guard_binding_suppressed_ordinary).
-export([run/0]).
-compile({no_auto_import,[is_atom/1]}).
run() -> is_atom(a).
""";

    public const string GuardSuppressedNamedFun = """
-module(oracle_guard_binding_suppressed_named_fun).
-export([run/0]).
-compile({no_auto_import,[is_atom/1]}).
run() -> F=fun is_atom/1,F(a).
""";

    public const string GuardErlangReimportGuard = """
-module(oracle_guard_binding_erlang_reimport_guard).
-export([run/0]).
-compile({no_auto_import,[is_atom/1]}).
-import(erlang,[is_atom/1]).
run() -> yes=pick(a),no=pick(1),true=is_atom(a),done.
pick(X) when is_atom(X) -> yes; pick(_) -> no.
""";

    public const string GuardAllSuppressedQualified = """
-module(oracle_guard_binding_all_suppressed_qualified).
-export([run/0]).
-compile(no_auto_import).
run() -> true=erlang:is_atom(a),done.
""";

    public const string GuardAllSuppressedReimport = """
-module(oracle_guard_binding_all_suppressed_reimport).
-export([run/0]).
-compile([no_auto_import,no_ssa_opt]).
-import(erlang,[is_atom/1]).
run() -> yes=pick(a),done.
pick(X) when is_atom(X) -> yes; pick(_) -> no.
""";

    public const string GuardForeignImportGuard = """
-module(oracle_guard_binding_foreign_import_guard).
-export([run/0]).
-compile({no_auto_import,[is_atom/1]}).
-import(other,[is_atom/1]).
run() -> pick(a).
pick(X) when is_atom(X) -> yes; pick(_) -> no.
""";

    public const string GuardForeignImportQualifiedGuard = """
-module(oracle_guard_binding_foreign_import_qualified_guard).
-export([run/0]).
-compile({no_auto_import,[is_atom/1]}).
-import(other,[is_atom/1]).
run() -> yes=pick(a),done.
pick(X) when erlang:is_atom(X) -> yes; pick(_) -> no.
""";

    public const string GuardImportedCall = """
-module(oracle_guard_binding_imported_call).
-export([run/0]).
-import(lists,[reverse/1]).
run() -> 3=length([a,b,c]),done.
length(X) -> erlang:length(reverse(X)).
""";

    public const string GuardImportedFunForbidden = """
-module(oracle_guard_binding_imported_fun_forbidden).
-export([run/0]).
-import(lists,[reverse/1]).
run() -> F=fun reverse/1,F([]).
""";

    public const string GuardErlangImportedFunForbidden = """
-module(oracle_guard_binding_erlang_imported_fun_forbidden).
-export([run/0]).
-import(erlang,[is_atom/1]).
run() -> F=fun is_atom/1,F(a).
""";

    public const string GuardImportLocalConflict = """
-module(oracle_guard_binding_import_local_conflict).
-export([run/0]).
-import(lists,[reverse/1]).
run() -> done.
reverse(X)->X.
""";

    public const string GuardDuplicateImport = """
-module(oracle_guard_binding_duplicate_import).
-export([run/0]).
-import(lists,[reverse/1]).
-import(other,[reverse/1]).
run() -> done.
""";

    public const string GuardDuplicateWithinForm = """
-module(oracle_guard_binding_duplicate_within_form).
-export([run/0]).
-import(lists,[reverse/1,reverse/1]).
run() -> []=reverse([]),done.
""";

    public const string GuardLocalFilterIsExpression = """
-module(oracle_guard_binding_local_filter_is_expression).
-export([run/0]).
-compile({no_auto_import,[is_atom/1]}).
run() -> [a]=[X || X <- [a], is_atom(X)],done.
is_atom(_)->true.
""";

    public const string GuardLocalFilterFailurePropagates = """
-module(oracle_guard_binding_local_filter_failure_propagates).
-export([run/0]).
-compile({no_auto_import,[is_atom/1]}).
run() -> try [X || X <- [a], is_atom(X)] of _ -> wrong catch error:filter_failed -> done end.
is_atom(_)->erlang:error(filter_failed).
""";

    public const string GuardSuppressedFilter = """
-module(oracle_guard_binding_suppressed_filter).
-export([run/0]).
-compile({no_auto_import,[is_atom/1]}).
run() -> [X || X <- [a],is_atom(X)].
""";

    public const string GuardSuppressedBitSize = """
-module(oracle_guard_binding_suppressed_bit_size).
-export([run/0]).
-compile({no_auto_import,[length/1]}).
run() -> pick(<<1>>).
pick(<<X:(length([a]))>>) -> X.
""";

    public const string GuardSuppressedMapKey = """
-module(oracle_guard_binding_suppressed_map_key).
-export([run/0]).
-compile({no_auto_import,[is_atom/1]}).
run() -> pick(#{true=>yes}).
pick(#{(is_atom(a)):=X}) -> X.
""";

    public const string GuardQualifiedPatternKeysSizes = """
-module(oracle_guard_binding_qualified_pattern_keys_sizes).
-export([run/0]).
-compile({no_auto_import,[is_atom/1,length/1]}).
run() -> yes=pick(#{true=>yes}),1=bits(<<1:1>>),done.
pick(#{(erlang:is_atom(a)):=X}) -> X.
bits(<<X:(erlang:length([a]))>>) -> X.
""";

    public const string ImportModuleNameNonLatin1 = """
-module(import_name_non_latin1). -import('λ',[run/0]). -export([main/0]). main()->done.
""";

    public const string ImportModuleNameEmpty = """
-module(import_name_empty). -import('',[run/0]). -export([main/0]). main()->done.
""";

    public const string ImportModuleNameBlank = """
-module(import_name_blank). -import(' ',[run/0]). -export([main/0]). main()->done.
""";

    public const string ImportModuleNameControl = """
-module(import_name_control). -import('a\n',[run/0]). -export([main/0]). main()->done.
""";

    public const string ImportModuleNameLatin1 = """
-module(import_name_latin1). -import('mód',[run/0]). -export([main/0]). main()->done.
""";

}
