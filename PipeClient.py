# -*- coding: utf-8 -*-

import win32pipe, win32file
import time
import sys

PIPE_NAME = r'\\.\pipe\GevjonCore'

file_handle = win32file.CreateFile(PIPE_NAME,
                                   win32file.GENERIC_READ | win32file.GENERIC_WRITE,
                                   win32file.FILE_SHARE_WRITE, None,
                                   win32file.OPEN_EXISTING, 0, None)
try:
    msg = str(sys.argv[1])
    print(msg)
    win32file.WriteFile(file_handle, str.encode(msg))
finally:
    try:
        win32file.CloseHandle(file_handle)
    except:
        pass
print("done")