#include <string.h>
#include "zlib.h"

#ifdef _WIN32
#define BEAM_ZLIB_EXPORT __declspec(dllexport)
#else
#define BEAM_ZLIB_EXPORT __attribute__((visibility("default")))
#endif

/* The caller supplies the ETF body and the same bounded output buffer as ERTS. */
BEAM_ZLIB_EXPORT int erlang_beam_zlib_compress(
    const unsigned char *source, unsigned int source_size,
    unsigned char *destination, unsigned int *destination_size)
{
    z_stream stream;
    memset(&stream, 0, sizeof(stream));
    stream.next_in = (Bytef *)source;
    stream.avail_in = source_size;
    stream.next_out = destination;
    stream.avail_out = *destination_size;

    int status = deflateInit(&stream, Z_DEFAULT_COMPRESSION);
    if (status != Z_OK)
        return status;

    status = deflate(&stream, Z_FINISH);
    if (status != Z_STREAM_END) {
        deflateEnd(&stream);
        return status == Z_OK ? Z_BUF_ERROR : status;
    }

    *destination_size = (unsigned int)stream.total_out;
    return deflateEnd(&stream);
}
