namespace Erlang.Tests;

internal static class BeamLifecycleHostNames
{
    internal const string AlternatingTest = "runtime/beam/lifecycle388/original-alternating-nontail-and-full-trace";
    internal const string Alternating = "Alternating.";
    internal const string AlternatingSubject = "beam_alternating_subject388";
    internal const string AlternatingObserver = "beam_alternating_observer388";
    internal const int AlternatingCases = 2;
    internal const string DeepTest = "runtime/beam/lifecycle388/original-deep-values-and-six-depth-profiles";
    internal const string ImageTest = "runtime/beam/lifecycle388/original-three-image-generations-and-five-depths";
    internal const string PhysicalTest = "runtime/beam/lifecycle388/deep-physical-stack-and-cancellation-settlement";
    internal const string Prefix = "Erlang.Tests.BeamLifecycle388.";
    internal const string Deep = "Deep.";
    internal const string Image = "Image.";
    internal const string DeepSubject = "beam_deep_subject388";
    internal const string DeepObserver = "beam_deep_observer388";
    internal const string ImageObserver = "beam_image_observer388";
    internal const string GenerationPrefix = "generation";
    internal const string BeamExtension = ".beam";
    internal const string Run = "run";
    internal const string StackEntry = "run_stacks";
    internal const string Values = "values.etf";
    internal const string Stacks = "stacks.etf";
    internal const string Erlang = "erlang";
    internal const string Normal = "normal";
    internal const string Failure = "Original deep/image lifecycle contract differs";
    internal const int DeepCases = 6;
    internal const int ImageCases = 2;
    internal const int DeepProfiles = 6;
    internal const int ImageProfiles = 5;
    internal const int DeepRowsIndex = 2;
    internal const int ImageRowsIndex = 1;
    internal const int GenerationCount = 3;
    internal const int EtfMinorVersion = 2;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(120);
    internal static readonly IReadOnlyDictionary<string, string> Digests = new Dictionary<string, string>
    {
        ["Deep.beam_deep_observer388.erl"] = "10FEA063B55F409A818A46E4D63C1F07109F5A558F48A12CBB81B13632D4C1D4",
        ["Deep.beam_deep_subject388.erl"] = "6A776FF0C69BD26E103F51F4BD3908501989161C851A713F5707D79BA8F429C2",
        ["Deep.beam_deep_observer388.beam"] = "E4BE85C153F42EAEEB0CC7A3CBBCC433357A763DC934D609B8C471CED4C8B183",
        ["Deep.beam_deep_subject388.beam"] = "3D4E2A538785505288B4AFCD0F08F38A07129B21017FBD0546F6E48ECBFD6103",
        ["Deep.values.etf"] = "A8A7E25B3111DFE85D4BF0409E70E51DF34FBFD734C9EAA7939B6CCCDA9CB15D",
        ["Deep.stacks.etf"] = "0D6B5FFE493CADC63909AF468E4067945534AED54628C0F03DCD0C0E1CAEC8E6",
        ["Image.beam_image_observer388.erl"] = "B8121A6107C79FA36DA73A125E18FA5E61EECDA48B74143691C4FF549285C7EB",
        ["Image.image_control388.erl"] = "FA04B684CFCEDAEF3A5F70407C9DC9067E5B4D0F7A81D418B20A988314388C28",
        ["Image.generation1.erl"] = "AC321B33C7F88EF9F57EAA12A8B41CC43017B05E919BA4EE389ED172E87392FF",
        ["Image.generation2.erl"] = "C6E0CB1BB5806978C2ECB5B5EF04D612B54BD73BB8906F6FA1643FC983C4C0E4",
        ["Image.generation3.erl"] = "8A95893C57BA9B62BC18445EF9C308C8FE5E204B64225021E5376980A2774D4D",
        ["Image.beam_image_observer388.beam"] = "228DF0F4719A5B50047E1274D734A5BEB020C21453E85F5BEE70B1FFF65E6241",
        ["Image.image_control388.beam"] = "57FDBB6354F32A30C453C950E83BD7729E03D1FD41407FE0293EFEECD7183327",
        ["Image.generation1.beam"] = "0B138BE736E077E402BF649F3DF38E5C4F8918B60AB3EDBFEAABEA3EC9E02B99",
        ["Image.generation2.beam"] = "CCF172CFD6D04456D71C8C0AD963BF351C50642F3F07B16ED681C76B48719FEA",
        ["Image.generation3.beam"] = "CFC9CECC8DBC53E54C7D08283BB51A17DAAECCF5A606F79BCD08792192FAD065",
        ["Image.values.etf"] = "73567484C54CF175BB073ADBA2E23F8D4EDDA19930BC52C1FBB4A1279334459C",
        ["Image.stacks.etf"] = "865AF31A4501A86C0C737842D02633B6541557357D59B262EFD5FB4396DFBB0C",
        ["Alternating.beam_alternating_observer388.erl"] = "3DD81E6E5BEE6145328524AC9064DA30F7E54A9006C3B5C059DBE25A8258B4E7",
        ["Alternating.beam_alternating_subject388.erl"] = "1573215AE1A8CD839D9198EB3F6476DF82AA0BBD27DFDD01257F453A2F8B4E63",
        ["Alternating.beam_alternating_observer388.beam"] = "4801ECC36F35B251FE6516A667F5AE7EB33965826E087ED1BB86C6B412898DFF",
        ["Alternating.beam_alternating_subject388.beam"] = "77936727D6771DD82F4531EA495A75DFCF5DE9884B3D3B847AE2836DDDD98DC2",
        ["Alternating.values.etf"] = "4B5124072D4548EBA2255C2F8E1ABB5DA33196EA78FB4D058DB9B40544A2405B",
        ["Alternating.stacks.etf"] = "2662D388FD014710C604226C1F8C7547F35C8D4CE238D3EC0BB472168374394E",
    };
}
