using System.Reflection;

namespace Erlang.Tests;

internal static class BeamStoredFunction406Names
{
    internal const string GraphTest = "runtime/library406/beam-fun-detached-env-resource-and-authenticity";
    internal const string PurgeTest = "runtime/library406/beam-fun-stored-original-image-purge-entry";
    internal const string Failure = "Original BEAM stored function ownership control failed";
    internal const string FactoryType = "Erlang.BeamClosureFactory";
    internal const string CaptureBinding = "CaptureStoredBinding";
    internal const string RequireKnown = "RequireKnownTarget";
    internal const string Code = "Code";
    internal const string Execution = "Execution";
    internal const string ExecutionIdentity = "ExecutionIdentity";
    internal const string NativePrimitives = "NativePrimitives";
    internal const string Owner = "Owner";
    internal const string TableIndex = "TableIndex";
    internal const string ActiveCode = "activeCode";
    internal const string Image = "Image";
    internal const string PreparePurge = "PreparePurge";
    internal const string FinishPurge = "FinishPurge";
    internal const int CollectionAttempts = 8;
    internal static readonly byte[] Bytes = [1, 2, 3];
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
}
