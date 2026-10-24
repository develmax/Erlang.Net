namespace Erlang.Tests;

internal static class BeamAwaitEntryHostNames
{
    internal const string RemoteTest = "runtime/beam/await-entry387/remote-original-values-and-five-depths";
    internal const string NativeTest = "runtime/beam/await-entry387/native-original-values-and-five-depths";
    internal const string Prefix = "Erlang.Tests.BeamAwaitEntry387.";
    internal const string Remote = "Remote.";
    internal const string Native = "Native.";
    internal const string RemoteObserver = "beam_remote_observer387";
    internal const string RemoteCaller = "beam_remote_caller387";
    internal const string RemotePeer = "beam_remote_peer387";
    internal const string NativeObserver = "beam_native_entry387";
    internal const string Lists = "lists";
    internal const string Erlang = "erlang";
    internal const string Run = "run";
    internal const string Stacks = "run_stacks";
    internal const string Values = "values.etf";
    internal const string StackFile = "stacks.etf";
    internal const string BeamExtension = ".beam";
    internal const string Normal = "normal";
    internal const string Unwound = "Unwound";
    internal const string Gate = "gate";
    internal const string Executing = "executingProcesses";
    internal const string IsExecutingCode = "IsExecutingCode";
    internal const string Failure = "Original awaited-entry contract differs";
    internal const string AritySeparator = "/";
    internal const int RemoteCases = 4;
    internal const int NativeCases = 10;
    internal const int Profiles = 5;
    internal const int EtfMinorVersion = 2;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(60);
    internal static readonly string[] PrimitiveSignatures = ["keyfind/3", "keymember/3", "keysearch/3", "member/2", "reverse/2"];
    internal static readonly IReadOnlyDictionary<string, string> Digests = new Dictionary<string, string>
    {
        ["Remote.beam_remote_caller387.beam"] = "C00871E94B8E635553366DBE3799A1FC241E6E89E47F75AFC675E8DA55118E94",
        ["Remote.beam_remote_observer387.beam"] = "799F6CB0A6E9543AB200C1F135517FA55BB57033AA7FB5FFD31D9647EBABB407",
        ["Remote.beam_remote_peer387.beam"] = "55CEB76C5D65210B3A9D4D30AD9CE567AAB875346A798B78A0D97604412820EC",
        ["Remote.values.etf"] = "165B978BE9502CE1420988D98D65AE42347039F54EC39E53966B6DD9B58E835B",
        ["Remote.stacks.etf"] = "61F15D03863CF599C7882666FBD1BED5982496BCD100974B8A40B2BC2B65619E",
        ["Remote.beam_remote_caller387.erl"] = "CECBB95D8D1472272D9939890B0B5AEDDC4362C98EF213FAA577D41C0043A361",
        ["Remote.beam_remote_observer387.erl"] = "F66027B99634737793B56EDCFEEAD296CE5F253F4EC4538ECFC34101F1C760AE",
        ["Remote.beam_remote_peer387.erl"] = "4BF11326518397FBD6115306CF9ACB149DC364132B2828F264D33BD718054583",
        ["Native.beam_native_entry387.beam"] = "81B412685731DEB0760D0B95B3977242BC34CE1FB0F220DE676DB2AE5A5F62EF",
        ["Native.lists.beam"] = "010FDB0A6C1441732EDF18934061E4C9B180FE9E3DAA9A93A7458E91352F1FCC",
        ["Native.values.etf"] = "3FA64455504F9437CB7D04FCB1D21F9002676B9650D438843DA11D17A4C5B4FA",
        ["Native.stacks.etf"] = "DA75772AEA086B446D3039478A25E476EF2909F60685130DA5E1A7EA71E5149A",
        ["Native.beam_native_entry387.erl"] = "C62F2CC87BA650DF61D8E703B0FCEE984414BD86631701B7E50E2A6EBA31A0A6",
        ["Delay.beam_delay387.beam"] = "4241B6BA516677B2D2F61E2B97A3E1C04F53B8BBD80D3FF9855DBA42C257F4D4",
        ["Delay.beam_delay387.erl"] = "7CF643BFADA17DB31270F9635A2430C804D8A89932FCADEFB90BEF929DEA7374",
        ["Delay.values.etf"] = "EF693CFBAB7C54EED0D71D4D7C50759C2DFC1582B4B4DE82634B573660B1C78A",
        ["Delay.stacks.etf"] = "64F8AEF7CA09B07E61E9DDA2F4F0A5BB1049A1FEF5CDE568318131AA4D727733",
    };
}
