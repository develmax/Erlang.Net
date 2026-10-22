namespace Erlang;

internal static class BeamNifDeclarations
{
    internal static IReadOnlyList<(string Name, int Arity)>? Read(BeamLinkedCode code)
    {
        var declarations = new List<(string Name, int Arity)>();
        foreach (BeamLinkedFunction function in code.Functions)
        {
            bool declared = code.Instructions.Skip(function.FirstInstructionIndex)
                .Take(function.EndInstructionIndex - function.FirstInstructionIndex)
                .Any(instruction => instruction.Definition.Name == BeamNifNames.Start);
            if (declared)
                declarations.Add((function.Name, function.Arity));
        }

        return declarations.Count == 0 ? null : declarations;
    }
}
