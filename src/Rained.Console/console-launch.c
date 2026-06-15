// wrapper exe to launch Rained in a console. decided to make this in c instead
// of c# for a significantly smaller binary size.
// 
// this is only relevant for Windows, because of the distinction between the
// Windows subsytem and the console subsystem

#ifndef UNICODE
#   define UNICODE
#endif

#include <stdio.h>
#include <windows.h>
#include <string.h>
#include <stdlib.h>

static const wchar_t cmdPrefix[] = L"Rained.exe --console ";
#define cmdPrefixLen (sizeof(cmdPrefix) / sizeof(wchar_t) - 1)

int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance,
                    PWSTR pCmdLine, int nCmdShow)
{
    size_t cmdLineLen = wcslen(pCmdLine);
    size_t outCmdLineSz = (cmdPrefixLen + cmdLineLen + 1) * sizeof(wchar_t);
    wchar_t *outCmdLine = malloc(outCmdLineSz);
    if (!outCmdLine)
    {
        fprintf(stderr, "error\n");
        return 1;
    }

    // append command prefix and user-given command line to outCmdLine
    memcpy(outCmdLine, cmdPrefix, cmdPrefixLen * sizeof(wchar_t));
    memcpy(outCmdLine + cmdPrefixLen, pCmdLine, cmdLineLen * sizeof(wchar_t));
    outCmdLine[cmdPrefixLen + cmdLineLen] = 0;
    
    // run process
    STARTUPINFO si;
    PROCESS_INFORMATION pi;

    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    ZeroMemory(&pi, sizeof(pi));

    si.dwFlags = STARTF_USESTDHANDLES;
    si.hStdInput = GetStdHandle(STD_INPUT_HANDLE);
    si.hStdOutput = GetStdHandle(STD_OUTPUT_HANDLE);
    si.hStdError = GetStdHandle(STD_ERROR_HANDLE);

    if (!CreateProcess(
        NULL,
        outCmdLine,
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
        fprintf(stderr, "Failed to launch Rained.exe. Error code: %lu\n",
                GetLastError());
        free(outCmdLine);
        return 1;
    }

    free(outCmdLine);

    // wait until child process exits
    WaitForSingleObject(pi.hProcess, INFINITE);

    // close process and thread handles
    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);
    
    return 0;
}
