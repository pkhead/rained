// This is used to interop with Bgfx's callback interface
// so that I can make it log Bgfx traces and fatal messages
#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <stddef.h>
#include <bgfx.h>

#define DLL_EXPORT __declspec(dllexport)

typedef void (*log_function_t)(const char* string);
typedef void (*fatal_function_t)(const char* file_path, uint16_t line, bgfx_fatal_t code, const char* string);

typedef struct callback_interface
{
    bgfx_callback_interface_t bgfx_interface;
    log_function_t log_func;
    fatal_function_t fatal_func;
} callback_interface_t;

void cb_fatal(bgfx_callback_interface_t* _this, const char* _filePath, uint16_t _line, bgfx_fatal_t _code, const char* _str)
{
    callback_interface_t* interface = (callback_interface_t*) _this;
    interface->fatal_func(_filePath, _line, _code, _str);
}

void cb_trace_vargs(bgfx_callback_interface_t* _this, const char* _filePath, uint16_t _line, const char* _format, va_list _argList)
{
    callback_interface_t* interface = (callback_interface_t*) _this;

    char buf[256];
    vsprintf_s(buf, 256, _format, _argList);
    interface->log_func(buf);
}

void cb_profiler_begin(bgfx_callback_interface_t* _this, const char* _name, uint32_t _abgr, const char* _filePath, uint16_t _line)
{}

void cb_profiler_begin_literal(bgfx_callback_interface_t* _this, const char* _name, uint32_t _abgr, const char* _filePath, uint16_t _line)
{}

void cb_profiler_end(bgfx_callback_interface_t* _this)
{}

uint32_t cb_cache_read_size(bgfx_callback_interface_t* _this, uint64_t _id)
{ return 0; }

bool cb_cache_read(bgfx_callback_interface_t* _this, uint64_t _id, void* _data, uint32_t _size)
{ return false; }

void cb_cache_write(bgfx_callback_interface_t* _this, uint64_t _id, const void* _data, uint32_t _size)
{}

void cb_screen_shot(bgfx_callback_interface_t* _this, const char* _filePath, uint32_t _width, uint32_t _height, uint32_t _pitch, const void* _data, uint32_t _size, bool _yflip)
{}

void cb_capture_begin(bgfx_callback_interface_t* _this, uint32_t _width, uint32_t _height, uint32_t _pitch, bgfx_texture_format_t _format, bool _yflip)
{}

void cb_capture_end(bgfx_callback_interface_t* _this)
{}

void cb_capture_frame(bgfx_callback_interface_t* _this, const void* _data, uint32_t _size)
{}

DLL_EXPORT callback_interface_t* create_bgfx_interface(log_function_t log_func, fatal_function_t fatal_func)
{
    bgfx_callback_vtbl_t* vtbl = malloc(sizeof(bgfx_callback_vtbl_t));
    callback_interface_t* interface = malloc(sizeof(callback_interface_t));
    
    interface->bgfx_interface.vtbl = vtbl;
    interface->fatal_func = fatal_func;
    interface->log_func = log_func;
    
    vtbl->fatal = cb_fatal;
    vtbl->trace_vargs = cb_trace_vargs;
    vtbl->profiler_begin = cb_profiler_begin;
    vtbl->profiler_begin_literal = cb_profiler_begin_literal;
    vtbl->profiler_end = cb_profiler_end;
    vtbl->cache_read_size = cb_cache_read_size;
    vtbl->cache_read = cb_cache_read;
    vtbl->cache_write = cb_cache_write;
    vtbl->screen_shot = cb_screen_shot;
    vtbl->capture_begin = cb_capture_begin;
    vtbl->capture_end = cb_capture_end;
    vtbl->capture_frame = cb_capture_frame;

    return interface;
}

DLL_EXPORT void destroy_bgfx_interface(callback_interface_t* interface)
{
    free((void*) interface->bgfx_interface.vtbl);
    interface->bgfx_interface.vtbl = NULL;
    free(interface);
}