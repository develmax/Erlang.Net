namespace Erlang;

internal static class DetsFileReasons
{
    internal const string NotDets = "not_a_dets_file";
    internal const string BadVersion = "bad_version";
    internal const string BadType = "invalid_type_code";
    internal const string NotClosed = "not_closed";
    internal const string BadHash = "bad_hash_bif";
    internal const string BadFreelists = "bad_freelists";
    internal const string BadObject = "bad_object";
    internal const string NoSpace = "no_more_space_on_file";
    internal const string InvalidStream = "DETS requires a readable, seekable stream.";
    internal const string InvalidOutput = "DETS requires a writable, seekable stream.";
}
