namespace Erlang.Differential;

public static class SystemAllocatorQueryCaseNames
{
    public const string Prefix = "erlang-system-allocator-query-";
    public const string ModulePrefix = "-module(oracle_sys_alloc_query). -export([run/0]). run()->";
    public const string ModuleSuffix = ".";
    public const int GlobalInstance = 0;
    public const string ErrorClass = "error";
    public const string BadArgument = "badarg";
    public const string FunctionClause = "function_clause";
    public const string SystemAllocator = "sys_alloc";
    public const string Instance = "instance";
    public const string Enabled = "e";
    public const string Backend = "m";
    public const string True = "true";
    public const string SdkBackend = "dotnet_native_memory";
    public const string FullOptionsTag = "sys_alloc_full_options";
    public const string FullOptionsObservationName = "erlang-system-allocator-query-full-options-backend-adaptation";
    public const string FullOptionsSource = "-module(oracle_sys_alloc_full_options). -export([run/0]). run()->[{instance,0,[{options,[{e,true},{m,M}|Rest]}]}]=erlang:alloc_info(sys_alloc),[{sys_alloc,[{instance,0,[{options,[{e,true},{m,ML}|ListRest]}]}]}]=erlang:alloc_info([sys_alloc]),{sys_alloc_full_options,{e,true},{m,M},Rest,{e,true},{m,ML},ListRest}.";
    public const string SizesAtomName = "sizes-atom";
    public const string SizesAtomBody = "erlang:alloc_sizes(sys_alloc)";
    public const string SizesListName = "sizes-singleton-list";
    public const string SizesListBody = "erlang:alloc_sizes([sys_alloc])";
    public const string InfoEmptyName = "info-empty-list";
    public const string InfoEmptyBody = "erlang:alloc_info([])";
    public const string SizesDuplicateName = "sizes-duplicate";
    public const string SizesDuplicateBody = "erlang:alloc_sizes([sys_alloc,sys_alloc])";
    public const string SizesImproperName = "sizes-improper";
    public const string SizesImproperBody = "erlang:alloc_sizes([sys_alloc|tail])";
    public const string InfoOuterName = "info-wrong-outer";
    public const string InfoOuterBody = "erlang:alloc_info(42)";
    public const string SizesUnknownLaterName = "sizes-known-unsupported-before-unknown";
    public const string SizesUnknownLaterBody = "erlang:alloc_sizes([eheap_alloc,unknown_allocator_sys_profile316])";
    public const string InfoUnknownName = "info-unknown-atom";
    public const string InfoUnknownBody = "erlang:alloc_info(unknown_allocator_sys_profile316)";
    public const string InfoNonatomName = "info-nonatom-list-member";
    public const string InfoNonatomBody = "erlang:alloc_info([sys_alloc,42])";
}
