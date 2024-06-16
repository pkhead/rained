# simple makefile to build console-launch.c
# assumes a GCC-compatible compiler (like clang, or gcc itself) confiigured to build for windows
# can be called from rained root directory by writing
# 	make -C src/Rained.Console
#
# (personally, i prefer using clang/gcc rather than msvc)

CC := cc

OUT = ../../dist/Rained.Console.exe
SRC = console-launch.c

$(OUT): $(SRC)
	$(CC) -static -Os $(SRC) -o $(OUT)