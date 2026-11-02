namespace Erlang.Tests;

// A validated literal empty-map image using the existing modeled BEAM recipe.
// No zero-pair put_map_assoc or unreachable empty template is invented.
internal static class EmptyTuple803Bytes
{
    internal static byte[] Image()
        => BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(new MapTerm([])));
}
