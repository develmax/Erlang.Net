namespace NodeIdentityContrasts;

internal static class NodePendingSource
{
    internal static string Evaluate(string directory) =>
        "try V=" + NodeNames.PendingModule + ":run(),ok=file:write_file(" +
        NodeErlangString.Quote(Path.Combine(directory, NodeNames.PendingActual)) +
        ",term_to_binary(V)),ok=file:write_file(" +
        NodeErlangString.Quote(Path.Combine(directory, NodeNames.Stage)) +
        ",<<\"complete\">>),halt(0) catch C:R:S -> ok=file:write_file(" +
        NodeErlangString.Quote(Path.Combine(directory, NodeNames.PendingException)) +
        ",term_to_binary({C,R,S})),halt(1) end.";
}
