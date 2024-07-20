project(
    'bgfx-interop',
    'c',
    default_options : ['b_vscrt=mt', 'buildtype=release']
)

sources = files('main.c')
incdirs = [
    get_option('bgfx_include'),
    get_option('bx_include')
]

c_args = []

compiler = meson.get_compiler('c')
if compiler.get_id() == 'msvc'
  c_args = ['/Zc:preprocessor']
endif

shared_library(
    'bgfx_interop',
    sources: sources,
    include_directories: incdirs,
    c_args: c_args
)
