#!/bin/bash
export MONO_TRACE_LISTENER="Console.Error"
export UBUNTU_MENUPROXY="libappmenu.so"
bin/Debug/KeePass.exe --debug
