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

shared_library(
    'bgfx_interop',
    sources: sources,
    include_directories: incdirs,
    c_args: ['/Zc:preprocessor']
)