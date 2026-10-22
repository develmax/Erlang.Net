namespace Erlang;

internal static class ErlDriverLayoutNames422
{
    internal const string BinaryLength = "Binary_orig_size", BinaryData = "Binary_orig_bytes", BinaryWidth = "Binary_size_width";
    internal const string VectorCount = "IoVec_vsize", VectorData = "IoVec_iov";
    internal const string IoBytes = "SysIoVecBytes", IoLength = "SysIoVec_iov_len", IoData = "SysIoVec_iov_base", IoLengthWidth = "SysIoVec_length_width";
    internal const string SysDriverMajor = "SysInfo_driver_major_version", SysDriverMinor = "SysInfo_driver_minor_version";
    internal const string SysErts = "SysInfo_erts_version", SysOtp = "SysInfo_otp_release", SysThreads = "SysInfo_thread_support", SysSmp = "SysInfo_smp_support";
    internal const string SysAsync = "SysInfo_async_threads", SysSchedulers = "SysInfo_scheduler_threads", SysNifMajor = "SysInfo_nif_major_version", SysNifMinor = "SysInfo_nif_minor_version", SysDirty = "SysInfo_dirty_scheduler_support";
    internal const string NowMega = "Now_megasecs", NowSeconds = "Now_secs", NowMicro = "Now_microsecs";
}
