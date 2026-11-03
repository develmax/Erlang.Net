#ifndef ERLANG_NET_NIF_SNPRINTF791_H
#define ERLANG_NET_NIF_SNPRINTF791_H

#include <stdarg.h>
#include <stddef.h>
#include <stdint.h>
#include <limits.h>
#include <string.h>
#include <errno.h>

typedef int (*nif_printf_write791)(void *, const char *, size_t);
typedef int (*nif_printf_term791)(uintptr_t, int64_t, nif_printf_write791, void *);

enum {
    NIF_PRINTF_ALT791 = 1,
    NIF_PRINTF_ZERO791 = 2,
    NIF_PRINTF_LEFT791 = 4,
    NIF_PRINTF_SPACE791 = 8,
    NIF_PRINTF_SIGN791 = 16
};

enum nif_printf_length791 {
    NIF_PRINTF_DEFAULT791,
    NIF_PRINTF_SHORT791,
    NIF_PRINTF_CHAR791,
    NIF_PRINTF_LONG791,
    NIF_PRINTF_LONG_LONG791,
    NIF_PRINTF_LONG_DOUBLE791
};

#define NIF_PRINTF_TERM_BUDGET791 INT64_C(100000)

typedef struct {
    char *buffer;
    size_t remaining;
    int total;
} nif_printf_output791;

static int nif_printf_write_bytes791(void *argument, const char *bytes, size_t count)
{
    nif_printf_output791 *output = (nif_printf_output791 *)argument;
    size_t copied = count < output->remaining ? count : output->remaining;
    if (count > (size_t)(INT_MAX - output->total)) return -EINVAL;
    if (copied != 0) {
        memcpy(output->buffer, bytes, copied);
        output->buffer += copied;
        output->remaining -= copied;
    }
    output->total += (int)count;
    return (int)copied;
}

static int nif_printf_count_bytes791(void *argument, const char *bytes, size_t count)
{
    (void)argument;
    (void)bytes;
    (void)count;
    return 0;
}

static int nif_printf_fill791(nif_printf_output791 *output, char value, int count)
{
    char chunk[64];
    memset(chunk, value, sizeof(chunk));
    while (count > 0) {
        size_t length = (size_t)count < sizeof(chunk) ? (size_t)count : sizeof(chunk);
        int result = nif_printf_write_bytes791(output, chunk, length);
        if (result < 0) return result;
        count -= (int)length;
    }
    return 0;
}

static int nif_printf_field791(nif_printf_output791 *output, const char *bytes,
                              int length, int sign, int width, int precision,
                              unsigned flags, char conversion)
{
    char prefix[3];
    int prefix_length = 0;
    int zeros;
    int field;
    int padding;
    int result;
    if (conversion == 'd') {
        if (sign < 0) prefix[prefix_length++] = '-';
        else if (flags & NIF_PRINTF_SIGN791) prefix[prefix_length++] = '+';
        else if (flags & NIF_PRINTF_SPACE791) prefix[prefix_length++] = ' ';
    }
    if (flags & NIF_PRINTF_ALT791) {
        if (conversion == 'x' || conversion == 'X') {
            prefix[prefix_length++] = '0';
            prefix[prefix_length++] = conversion;
        } else if (conversion == 'o') {
            prefix[prefix_length++] = '0';
            if (precision > 1) precision--;
        }
    }
    zeros = precision > length ? precision - length : 0;
    if (length > INT_MAX - prefix_length - zeros) return -EINVAL;
    field = length + prefix_length + zeros;
    padding = width > field ? width - field : 0;
    if (!(flags & NIF_PRINTF_LEFT791) && (!(flags & NIF_PRINTF_ZERO791) || precision >= 0)) {
        result = nif_printf_fill791(output, ' ', padding);
        if (result < 0) return result;
    }
    result = nif_printf_write_bytes791(output, prefix, (size_t)prefix_length);
    if (result < 0) return result;
    if (!(flags & NIF_PRINTF_LEFT791) && (flags & NIF_PRINTF_ZERO791) && precision < 0)
        zeros = padding;
    result = nif_printf_fill791(output, '0', zeros);
    if (result < 0) return result;
    result = nif_printf_write_bytes791(output, bytes, (size_t)length);
    if (result < 0) return result;
    if (flags & NIF_PRINTF_LEFT791) return nif_printf_fill791(output, ' ', padding);
    return 0;
}

static int nif_printf_integer791(nif_printf_output791 *output, uint64_t value,
                                int sign, int width, int precision,
                                unsigned flags, char conversion)
{
    const char *digits = conversion == 'X' ? "0123456789ABCDEF" : "0123456789abcdef";
    unsigned radix = conversion == 'o' ? 8 : (conversion == 'x' || conversion == 'X' ? 16 : 10);
    char buffer[64];
    char *end = buffer + sizeof(buffer);
    char *first = end;
    if (value == 0 && precision != 0) *--first = '0';
    while (value != 0) {
        *--first = digits[value % radix];
        value /= radix;
    }
    return nif_printf_field791(output, first, (int)(end - first), sign,
                              width, precision, flags, conversion);
}

static int nif_printf_decimal791(const char **format, int *value)
{
    const char *next = *format;
    int result = 0;
    while (*next >= '0' && *next <= '9') {
        int digit = *next++ - '0';
        if (result > (INT_MAX - digit) / 10) return -EINVAL;
        result = result * 10 + digit;
    }
    *format = next;
    *value = result;
    return 0;
}

/* Native va_arg always executes in C, with the platform's real promotions.
 * Scalar branches follow pinned erl_printf_format.c, not libc snprintf. */
static int nif_vsnprintf791(char *buffer, size_t size, const char *format,
                           va_list arguments, nif_printf_term791 render_term)
{
    nif_printf_output791 output;
    int result = 0;
    if (size == 0) return -EINVAL;
    errno = 0;
    if (buffer == NULL || format == NULL) return -EINVAL;
    output.buffer = buffer;
    output.remaining = size - 1;
    output.total = 0;
    while (*format != '\0') {
        const char *start = format;
        int width = -1, precision = -1;
        unsigned flags = 0;
        enum nif_printf_length791 length = NIF_PRINTF_DEFAULT791;
        char conversion;
        if (*format != '%') {
            while (*format != '\0' && *format != '%') format++;
            result = nif_printf_write_bytes791(&output, start, (size_t)(format - start));
            if (result < 0) goto failure;
            continue;
        }
        format++;
        for (;;) {
            if (*format == '#') flags |= NIF_PRINTF_ALT791;
            else if (*format == '0') flags |= NIF_PRINTF_ZERO791;
            else if (*format == '-') flags |= NIF_PRINTF_LEFT791;
            else if (*format == ' ') flags |= NIF_PRINTF_SPACE791;
            else if (*format == '+') flags |= NIF_PRINTF_SIGN791;
            else if (*format != '\'' && *format != 'I') break;
            format++;
        }
        if (*format == '*') { width = va_arg(arguments, int); format++; }
        else if (*format >= '0' && *format <= '9') {
            result = nif_printf_decimal791(&format, &width);
            if (result < 0) goto failure;
        }
        if (*format == '.') {
            format++;
            if (*format == '*') { precision = va_arg(arguments, int); format++; }
            else if (*format >= '0' && *format <= '9') {
                result = nif_printf_decimal791(&format, &precision);
                if (result < 0) goto failure;
            }
        }
        if (*format == 'h') {
            length = NIF_PRINTF_SHORT791; format++;
            if (*format == 'h') { length = NIF_PRINTF_CHAR791; format++; }
        } else if (*format == 'l') {
            length = NIF_PRINTF_LONG791; format++;
            if (*format == 'l') { length = NIF_PRINTF_LONG_LONG791; format++; }
        } else if (*format == 'L') { length = NIF_PRINTF_LONG_DOUBLE791; format++; }
        else if (*format == 'j' || *format == 't') { format++; }
        else if (*format == 'b') {
            int bits = 0;
            format++;
            if (*format == 'p' || *format == 'e') { bits = (int)(sizeof(uintptr_t) * CHAR_BIT); format++; }
            else {
                result = nif_printf_decimal791(&format, &bits);
                if (result < 0) goto failure;
            }
            if (bits == 8) length = NIF_PRINTF_CHAR791;
            else if (bits == 16) length = NIF_PRINTF_SHORT791;
            else if (bits == 32) length = NIF_PRINTF_DEFAULT791;
            else if (bits == 64) length = sizeof(long) == 8 ? NIF_PRINTF_LONG791 : NIF_PRINTF_LONG_LONG791;
            else { result = -EINVAL; goto failure; }
        }
        conversion = *format;
        if (conversion == '\0') break;
        format++;
        if (conversion == '%') {
            result = nif_printf_write_bytes791(&output, "%", 1);
        } else if (conversion == 's' || conversion == 'c') {
            char character;
            const char *text;
            size_t count;
            if (conversion == 's') {
                text = va_arg(arguments, const char *);
                if (text == NULL) { result = -EINVAL; goto failure; }
                count = 0;
                while ((precision < 0 || count < (size_t)precision) && text[count] != '\0') count++;
                if (count > INT_MAX) { result = -EINVAL; goto failure; }
            } else {
                character = (char)va_arg(arguments, int);
                text = &character;
                count = precision == 0 ? 0 : 1;
            }
            result = nif_printf_field791(&output, text, (int)count, 0, width, -1,
                                        flags & NIF_PRINTF_LEFT791, 's');
        } else if (conversion == 'T') {
            uintptr_t term = va_arg(arguments, uintptr_t);
            int64_t budget = precision < 0 ? NIF_PRINTF_TERM_BUDGET791 : precision == INT_MAX ? (int64_t)LONG_MAX : precision;
            int count;
            if (render_term == NULL) { result = -EINVAL; goto failure; }
            if (!(flags & NIF_PRINTF_LEFT791) && width > 0) {
                count = render_term(term, budget, nif_printf_count_bytes791, NULL);
                if (count < 0) { result = count; goto failure; }
                if (width > count) {
                    result = nif_printf_fill791(&output, ' ', width - count);
                    if (result < 0) goto failure;
                }
            }
            count = output.total;
            result = render_term(term, budget, nif_printf_write_bytes791, &output);
            if (result < 0) goto failure;
            if (result > INT_MAX - count) { result = -EINVAL; goto failure; }
            output.total = count + result;
            count = result;
            if ((flags & NIF_PRINTF_LEFT791) && width > count)
                result = nif_printf_fill791(&output, ' ', width - count);
        } else if (conversion == 'n') {
            if (length == NIF_PRINTF_CHAR791) *va_arg(arguments, char *) = (char)output.total;
            else if (length == NIF_PRINTF_SHORT791) *va_arg(arguments, short *) = (short)output.total;
            else if (length == NIF_PRINTF_LONG791) *va_arg(arguments, long *) = output.total;
            else if (length == NIF_PRINTF_LONG_LONG791) *va_arg(arguments, long long *) = output.total;
            else *va_arg(arguments, int *) = output.total;
        } else if (conversion == 'd' || conversion == 'i') {
            int64_t value;
            int bits = 64;
            uint64_t magnitude;
            if (length == NIF_PRINTF_CHAR791) value = (signed char)va_arg(arguments, int);
            else if (length == NIF_PRINTF_SHORT791) value = (short)va_arg(arguments, int);
            else if (length == NIF_PRINTF_LONG791) { value = va_arg(arguments, long); bits = (int)(sizeof(long) * CHAR_BIT); }
            else if (length == NIF_PRINTF_LONG_LONG791) value = va_arg(arguments, long long);
            else { value = va_arg(arguments, int); bits = 32; }
            magnitude = value < 0 ? UINT64_C(0) - (uint64_t)value : (uint64_t)value;
            /* PIN negates signed int/long before UWord conversion. Preserve
             * its supported64-bit native32-bit INT_MIN wrap/sign extension. */
            if (bits == 32 && value == INT32_MIN) magnitude = (uint64_t)(int64_t)INT32_MIN;
            result = nif_printf_integer791(&output, magnitude, value < 0 ? -1 : value != 0,
                                           width, precision, flags, 'd');
        } else if (conversion == 'u' || conversion == 'o' || conversion == 'x' || conversion == 'X' || conversion == 'p') {
            uint64_t value;
            if (conversion == 'p') {
                value = (uintptr_t)va_arg(arguments, void *);
                if (width < 0) width = (int)(2 * sizeof(void *));
                if (precision < 0) precision = (int)(2 * sizeof(void *));
                flags = NIF_PRINTF_ALT791 | NIF_PRINTF_ZERO791;
                conversion = 'x';
            } else if (length == NIF_PRINTF_CHAR791) value = (unsigned char)va_arg(arguments, int);
            else if (length == NIF_PRINTF_SHORT791) value = (unsigned short)va_arg(arguments, int);
            else if (length == NIF_PRINTF_LONG791) value = (uintptr_t)va_arg(arguments, long);
            else if (length == NIF_PRINTF_LONG_LONG791) value = va_arg(arguments, unsigned long long);
            else value = va_arg(arguments, unsigned int);
            result = nif_printf_integer791(&output, value, value != 0, width, precision, flags, conversion);
        } else if (conversion == 'e' || conversion == 'E' || conversion == 'f' || conversion == 'g' || conversion == 'G') {
            /* Explicit finite unsupported branch, never libc/C99 substitution. */
            result = -EINVAL;
        } else {
            /* PIN drops '%' and keeps an unknown conversion as literal text. */
            format--;
            result = 0;
        }
        if (result < 0) goto failure;
    }
    *output.buffer = '\0';
    return output.total;
failure:
    buffer[0] = '\0';
    return result;
}

#endif
