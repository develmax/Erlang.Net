/* Deliberately not a valid NIF: checks only OTP's _nif_init symbol fallback. */
__attribute__((visibility("default"))) void *_nif_init(void)
{
    static int marker;
    return &marker;
}
