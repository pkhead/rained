// wrapper exe to launch Rained in a console
// decided to make this in c instead of c# for a significantly smaller binary size
// this is only relevant for Windows, because of the distinction between the Windows subsytem and the console subsystem
#include <stdio.h>
#include <windows.h>
#include <string.h>

int main(int argc, const char* argv[]) {
    // call Rained.exe that is in the same directory as the called executable.
    // I thought i had to do path manipulation from argv[0], but apparently
    // this just works. I assume exe path searching works the same way as DLLS.
    char str_buf[512];
    ZeroMemory(str_buf, sizeof(str_buf));
    strcpy(str_buf, "Rained.exe ");
    int strIndex = 11;
    
    // append arguments to the command
    strcpy(str_buf + strIndex, "--console");
    strIndex += 9;

    for (int i = 1; i < argc; i++)
    {
        // check for buffer overflow
        if (strIndex + strlen(argv[i]) + 1 >= sizeof(str_buf))
        {
            printf("Arguments string is too long!\n");
            return 1;
        }

        str_buf[strIndex++] = ' ';
        strcpy(str_buf + strIndex, argv[i]);
        strIndex += strlen(argv[i]);
    }
    
    // run process
    STARTUPINFO si;
    PROCESS_INFORMATION pi;

    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    ZeroMemory(&pi, sizeof(pi));

    if (!CreateProcess(
        NULL,
        str_buf,
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
