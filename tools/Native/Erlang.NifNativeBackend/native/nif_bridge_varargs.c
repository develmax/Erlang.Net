#include "nif_bridge_api.h"
#include "nif_snprintf791.h"

NIF_BRIDGE_EXPORT int nif_bridge_read_term(NifBridgeCursor *cursor, ERL_NIF_TERM *term)
{
    if (cursor == NULL || term == NULL || cursor->remaining == 0)
        return 0;

    *term = va_arg(cursor->arguments, ERL_NIF_TERM);
    cursor->remaining--;

    return 1;
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_tuple(ErlNifEnv *env, unsigned count, ...)
{
    const NifBridgeCallbacks *callbacks = nif_bridge_callbacks();
    NifBridgeCursor cursor;
    ERL_NIF_TERM result;

    cursor.remaining = count;
    va_start(cursor.arguments, count);
    result = callbacks->make_tuple(env, count, &cursor);
    va_end(cursor.arguments);

    return result;
}

NIF_BRIDGE_EXPORT ERL_NIF_TERM enif_make_list(ErlNifEnv *env, unsigned count, ...)
{
    const NifBridgeCallbacks *callbacks = nif_bridge_callbacks();
    NifBridgeCursor cursor;
    ERL_NIF_TERM result;

    cursor.remaining = count;
    va_start(cursor.arguments, count);
    result = callbacks->make_list(env, count, &cursor);
    va_end(cursor.arguments);

    return result;
}

NIF_BRIDGE_EXPORT int enif_snprintf(char *buffer, size_t size, const char *format, ...)
{
    int result;
    va_list arguments;
    const NifBridgeCallbacks *callbacks = nif_bridge_callbacks();
    va_start(arguments, format);
    result = nif_vsnprintf791(buffer, size, format, arguments, callbacks->format_term791);
    va_end(arguments);

    return result;
}
