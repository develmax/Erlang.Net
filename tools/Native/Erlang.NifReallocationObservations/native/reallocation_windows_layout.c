#if !defined(_WIN64)
#error This layout execution proof requires actual Windows x64 configured headers.
#endif
#include "erl_nif.h"
#include "reallocation_windows_layout_names.h"
#include <stddef.h>
#include <stdio.h>
#undef enif_realloc

int main(void)
{
    size_t pointer_bytes = sizeof(void *);
    size_t first_tail = offsetof(TWinDynNifCallbacks, erts_alc_test) / pointer_bytes;
    size_t second_tail = offsetof(TWinDynNifCallbacks, erts_internal_test_ptr) / pointer_bytes;
    size_t realloc_offset = offsetof(TWinDynNifCallbacks, enif_realloc);
    printf(REALLOCATION_LAYOUT_FORMAT, pointer_bytes, sizeof(TWinDynNifCallbacks), first_tail,
           realloc_offset / pointer_bytes, realloc_offset, first_tail, second_tail);
    return 0;
}
