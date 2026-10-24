using System.Reflection;

namespace Erlang.Tests;

internal static class BeamImageGenerationNames
{
    internal const string Module = "beam_image388";
    internal const string Controller = "image_control388";
    internal const string Load = "load";
    internal const string SoftPurge = "soft_purge";
    internal const string PreparePurge = "PreparePurgeAsync";
    internal const string FinishPurge = "FinishPurgeAsync";
    internal const string CheckImage = "CheckProcessCodeImage";
    internal const string IsExecuting = "IsExecutingCode";
    internal const string Unwound = "Unwound";
    internal const string Image = "Image";
    internal const string Gate = "gate";
    internal const string Processes = "processes";
    internal const string Executing = "executingProcesses";
    internal const string Purger = "codePurger";
    internal const string AsTask = "AsTask";
    internal const string Result = "Result";
    internal const string ErLang = "erlang";
    internal const string True = "true";
    internal const string False = "false";
    internal const string ModuleTag = "module";
    internal const string Error = "error";
    internal const string NotPurged = "not_purged";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    internal const string ExactGenerationBinaryRequired = "Test controller requires exact original generation bytes";
    internal const string UnboundGeneration = "Unbound generation image";
    internal const string InvalidGenerationModule = "Unexpected generation module or on_load";
    internal const string GenerationOwnerMismatch = "Generation source owner mismatch";
    internal const string ReusedImageIdentity = "Publication reused physical image identity";
    internal const string UnexpectedPublication = "Unexpected generation publication result";
    internal const string PurgeFenceNotSettled = "Production purge fence did not settle as requested";
    internal const string RetainedGenerationLease = "A physical generation image lease remains after observer settlement";
    internal const string OldGenerationNotPurged = "Old generation remains after final successful soft policy purge";
}
