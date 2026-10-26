namespace Erlang.Tests;

internal static class AtomContract492Names
{
    internal const string TestPrefix = "runtime/bif-contract492/";
    internal const string Prefix = "Erlang.Tests.AtomContract492.";
    internal const string Failure = "Atom contract resource is missing or differs";
    internal const string IdentityGroup = "identity";
    internal const string Manifest = "manifest.json";
    internal const string ManifestSha256 = "5BA3CE7CBCDA68ECD67F90A801E2BAB25184980C93E2934BB66FFD3ED67DA409";
    internal const string BeamExtension = ".beam";
    internal const string ValuesFile = "values.etf";
    internal const string StacksFile = "stacks.etf";
    internal const string Separator = ".";
    internal static readonly (string Group, string Subject, int Values, int Profiles)[] Groups =
    [
        ("atom_to_list1_p0", "atom_contract492_atom_to_list1_p0", 31, 155),
        ("list_to_atom1_p0", "atom_contract492_list_to_atom1_p0", 36, 180),
        ("list_to_existing_atom1_p0", "atom_contract492_list_to_existing_atom1_p0", 42, 210),
        ("atom_to_binary1_p0", "atom_contract492_atom_to_binary1_p0", 31, 155),
        ("atom_to_binary2_p0", "atom_contract492_atom_to_binary2_p0", 80, 400),
        ("atom_to_binary2_p1", "atom_contract492_atom_to_binary2_p1", 20, 100),
        ("binary_to_atom1_p0", "atom_contract492_binary_to_atom1_p0", 36, 180),
        ("binary_to_atom2_p0", "atom_contract492_binary_to_atom2_p0", 80, 400),
        ("binary_to_atom2_p1", "atom_contract492_binary_to_atom2_p1", 35, 175),
        ("binary_to_existing_atom1_p0", "atom_contract492_binary_to_existing_atom1_p0", 41, 205),
        ("binary_to_existing_atom2_p0", "atom_contract492_binary_to_existing_atom2_p0", 80, 400),
        ("binary_to_existing_atom2_p1", "atom_contract492_binary_to_existing_atom2_p1", 48, 240),
        ("identity", "atom_contract492_identity", 9, 45)
    ];
}
