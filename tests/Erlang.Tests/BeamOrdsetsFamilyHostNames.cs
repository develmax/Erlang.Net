namespace Erlang.Tests;

internal static class BeamOrdsetsFamilyHostNames
{
    internal const string Separator = ".";
    internal const string Prefix = "Erlang.Tests.BeamOrdsets382.";
    internal const string Failure = "Original compiled ordsets family asset identity differs";
    internal static readonly (string Group, string Subject, int Values, int Profiles, string Case)[] Groups =
    [
        ("ordsets_set_operations", "ordsets371_set_operations", 195, 975, "runtime/beam/ordsets382/ordsets_set_operations"),
        ("ordsets_callbacks", "ordsets371_callbacks", 115, 575, "runtime/beam/ordsets382/ordsets_callbacks"),
        ("map_fun_keys", "ordsets371_map_fun_keys", 14, 70, "runtime/beam/ordsets382/map_fun_keys"),
    ];
    internal static readonly IReadOnlyDictionary<string, string> Digests = new Dictionary<string, string>
    {
        ["ordsets_set_operations.ordsets371_set_operations.beam"] = "E38F0FC835ABCD6CFEADCEC6BB4F0A9BD0949A4D9CC9D791F0513B80763A7D87",
        ["ordsets_set_operations.values.etf"] = "C1907B805A6B4CBB805754B4922A58808F62BD535EF9F0CE6082E8598F1FE7A0",
        ["ordsets_set_operations.stacks.etf"] = "0C92DBA4A330430B1FE28261D0C22032795F66B211FDD544C4D65455FA1E1282",
        ["ordsets_callbacks.ordsets371_callbacks.beam"] = "49467C8D425250C32D8F39B2F49BF8AACA1F378BEA7BAE219E9F0104B2617B20",
        ["ordsets_callbacks.values.etf"] = "9DAA7ED62764527A6E428937FC94E90E2DA06FB1C270C0C0E56B869F5957C211",
        ["ordsets_callbacks.stacks.etf"] = "CB8FC85D0AD5A0E3F0D525864EAFA2C6B176801146EB66A6FA80F4ED6A5C5F5B",
        ["map_fun_keys.ordsets371_map_fun_keys.beam"] = "5EBBE2090574C450D5734E23216CB2A98CAE07D10A7D94E6B5CA365F0072CD6A",
        ["map_fun_keys.values.etf"] = "7989DDCFBF67D7E33218615FD0D34EE8C610C46D634F31B696A7CED127E94F0E",
        ["map_fun_keys.stacks.etf"] = "D5102C7F99759C998DC5A4043FA3B3AFA75AEF62E71EBA1A4414529CC646E794",
    };
}
