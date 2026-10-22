namespace Erlang.Tests;

internal static class LibraryMapProtocolNames
{
    internal const string Test = "runtime/library390/maps-proplists-state-independent-complete-terms";
    internal const string OwnState = "runtime/library390/maps-own-atom-layout-and-hamt-chunks";
    internal const string Prefix = "Erlang.Tests.LibraryMapsProplists390.";
    internal const string Observer = "beam_maps_props_observer390";
    internal const string Subject = "beam_maps_props_subject390";
    internal const string Maps = "maps";
    internal const string Properties = "proplists";
    internal const string Sets = "sets";
    internal const string Lists = "lists";
    internal const string Internal = "erts_internal";
    internal const string Next = "map_next";
    internal const string IteratorMode = "iterator";
    internal const string None = "none";
    internal const string Keys = "keys";
    internal const string Values = "values";
    internal const string MapToList = "to_list";
    internal const string PropertiesFromMap = "from_map";
    internal const string PropertiesKeys = "get_keys";
    internal const string FirstAtom = "zz_own_map_protocol390";
    internal const string SecondAtom = "aa_own_map_protocol390";
    internal const string AccumulatorSeed = "own_map_accumulator_seed";
    internal const string AccumulatorTail = "own_map_accumulator_tail";
    internal const string Run = "run";
    internal const string Stacks = "run_stacks";
    internal const string ValuesFile = "values.etf";
    internal const string StacksFile = "stacks.etf";
    internal const string BeamExtension = ".beam";
    internal const string Failure = "Original map protocol observation failed";
    internal const int CaseCount = 165;
    internal const int IndependentCount = 154;
    internal const int Profiles = 5;
    internal const int FlatMapLimit = 32;
    internal static readonly string[] Modules = [Maps, Properties, Sets, Lists];
    internal static readonly HashSet<string> StateDependentCases = ["maps_keys", "maps_to_list", "maps_values", "props_get_keys", "props_from_map", "maps_next_offset_one", "primitive_list_mode", "primitive_improper_accumulator", "primitive_iterator_mode", "primitive_flat_offset", "primitive_flat_end"];
    internal static readonly IReadOnlyDictionary<string, string> Digests = new Dictionary<string, string>
    {
        ["beam_maps_props_observer390.beam"] = "32E9621AECDB822446C0BDEF86915C829E9107BA8549DD0DEDC8222EFBEA0F16",
        ["beam_maps_props_observer390.erl"] = "79A4EAAB801A30ABA6F4D92250245F27A679EF33A4394EBC8009A6C42405846E",
        ["beam_maps_props_subject390.beam"] = "C8C0319E012319F5BDA491D51F0BB58702BBE01BF4EB1E266756280C3B60F7DD",
        ["beam_maps_props_subject390.erl"] = "A7762C4EEE08191F6D491F44FBEB6145AB7D9E5BD411ED5C3A7E71B694586C11",
        ["stacks.etf"] = "0B4276ED88556DFBD83879161C2B7F2BD2D317E5BB61102EE0BD46BFE790EB78",
        ["values.etf"] = "A7C2119C095B480F1C92EE1464BC3CEC60A77969547E7679DE03C79E6E4D193D",
    };
}
