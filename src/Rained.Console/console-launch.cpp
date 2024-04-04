// wrapper exe to launch Rained in a console
// decided to make this in c++ instead of c# for a significantly smaller binary size
// this is only relevant for Windows, because of the distinction between the Windows subsytem and the console subsystem
#include <stdio.h>
#include <windows.h>
#include <string>
#include <filesystem>

int main(int argc, const char* argv[]) {
    std::filesystem::path path = std::filesystem::path(argv[0]).parent_path() / "Rained.exe";
    
    STARTUPINFO si;
    PROCESS_INFORMATION pi;

    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    ZeroMemory(&pi, sizeof(pi));

    std::string args = "\"" + path.u8string() + "\" --console";
    for (int i = 1; i < argc; i++)
    {
        args += ' ';
        args += argv[i];
    }

    if (!CreateProcess(
        NULL,
        args.data(),
        NULL,
        NULL,
        TRUE,
        0,
        NULL,
        NULL,
        &si,
        &pi
    ))
    {
        printf("Failed to launch Rained.exe. Error code: %lu\n", GetLastError());
        return 1;
    }

    // wait until child process exits
    WaitForSingleObject(pi.hProcess, INFINITE);

    // close process and thread handles
    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);
    
    return 0;
}
