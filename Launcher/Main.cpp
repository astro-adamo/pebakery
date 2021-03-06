/*
	Copyright (C) 2016-2020 Hajin Jang
	Licensed under MIT License.

	MIT License

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in all
	copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
	SOFTWARE.
*/

// Constants
#include "Var.h"

// Windows SDK Headers
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <shlwapi.h>
#include <shellapi.h>

// C++ Runtime Headers
#include <string>
#include <sstream>

// Resource Headers
#include "resource.h"

// Local Headers
#include "Helper.h"
#include "Version.h"
#include "NetDetector.h"
#include "PEParser.h"

using namespace std;

// Prototypes
void GetPEBakeryPath(wstring& baseDir, wstring& exePath, wstring& dllPath);

int APIENTRY wWinMain(_In_ HINSTANCE hInstance,
	_In_opt_ HINSTANCE hPrevInstance,
	_In_ LPWSTR    lpCmdLine,
	_In_ int       nCmdShow)
{
	UNREFERENCED_PARAMETER(hPrevInstance);
	UNREFERENCED_PARAMETER(lpCmdLine);

#if BUILD_MODE == BUILD_NETFX
	// Check if required version of .NET Framework is installed.
	Version fxVer = Version(4, 7, 2);
	NetFxDetector fxDetector = NetFxDetector(fxVer);
	if (!fxDetector.IsInstalled())
		fxDetector.DownloadRuntime(true);
#elif BUILD_MODE == BUILD_NETCORE_RT_DEPENDENT
	// Check if required version of .NET Core is installed.
	Version coreVer = Version(3, 1, 5);
	NetCoreDetector coreDetector = NetCoreDetector(coreVer, true);
	if (!coreDetector.IsInstalled())
		coreDetector.DownloadRuntime(true);
#endif

	// Get absolute path of PEBakery.exe.
	wstring baseDir;
	wstring pebExePath;
	wstring pebDllPath;
	GetPEBakeryPath(baseDir, pebExePath, pebDllPath);

	// Run PEBakery
	bool launched = false;
	bool archMatch = true;

	if (PathFileExistsW(pebExePath.c_str()))
	{ // Run if PEBakery.exe exists.
		// This check will prevent mixing .NET Framework build and .NET Core build.
#if BUILD_MODE == BUILD_NETFX
		PEParser parser = PEParser();
		if (parser.ParseFile(pebExePath.c_str()) == false)
			Helper::PrintError(L"PEBakery.exe is corrupted.", true);

		// PEBakery.exe published by .NET Framework must be a .NET binary.
		if (!parser.IsNet())
			Helper::PrintError(L"PEBakery.exe is corrupted.", true);
#elif BUILD_MODE == BUILD_NETCORE_RT_DEPENDENT
		PEParser parser = PEParser();
		if (parser.ParseFile(pebExePath.c_str()) == false)
			Helper::PrintError(L"PEBakery.exe is corrupted.", true);

		// PEBakery.exe published by .NET Core must be a native binary.
		if (parser.IsNet())
			Helper::PrintError(L"PEBakery.exe is corrupted.", true);

		// Check if PEBakery.exe matches the current processor architecture.
		if (Helper::GetCpuArch() == parser.GetArch())
#endif
		{
			wchar_t* params = Helper::GetParameters(GetCommandLineW());
			// According to MSDN, ShellExecute's return value can be casted only to int.
			// In mingw, size_t casting should be used to evade [-Wpointer-to-int-cast] warning.
			int hRes = (int)(size_t)ShellExecuteW(NULL, NULL, pebExePath.c_str(), params, baseDir.c_str(), SW_SHOWNORMAL);
			if (hRes <= 32)
				Helper::PrintError(L"Unable to launch PEBakery.", true);
			else
				launched = true;
		}
#if BUILD_MODE == BUILD_NETCORE_RT_DEPENDENT
		else
		{
			archMatch = false;
		}
#endif
	}

#if BUILD_MODE == BUILD_NETCORE_RT_DEPENDENT
	if (!launched && PathFileExistsW(pebDllPath.c_str()))
	{ // Run if PEBakery.dll exists.
		wstring paramStr = pebDllPath;
		wchar_t* params = Helper::GetParameters(GetCommandLineW());
		if (params != nullptr)
		{
			paramStr.append(L" ");
			paramStr.append(params);
		}
		// Run `dotnet <PEBakery.dll> <params>` as Administrator
		int hRes = (int)(size_t)ShellExecuteW(NULL, L"runas", L"dotnet", paramStr.c_str(), baseDir.c_str(), SW_HIDE);
		if (hRes <= 32)
			Helper::PrintError(L"Unable to launch PEBakery.", true);
		else
			launched = true;
	}
#endif
	
	if (!launched)
	{
		if (!archMatch)
			Helper::PrintError(L"PEBakery.exe is corrupted.", true);
		else
			Helper::PrintError(L"Unable to find PEBakery.", true);
	}

	return 0;
}

// Constants
constexpr size_t MAX_PATH_LONG = 32768;

void GetPEBakeryPath(wstring& baseDir, wstring& exePath, wstring& dllPath)
{
	auto wstrDeleter = [](wchar_t* ptr) { delete[] ptr; };
	unique_ptr<wchar_t[], decltype(wstrDeleter)> absPathPtr(new wchar_t[MAX_PATH_LONG], wstrDeleter);
	wchar_t* buffer = absPathPtr.get();

	// Get absolute path of PEBakeryLauncher.exe
	DWORD absPathLen = GetModuleFileNameW(NULL, buffer, MAX_PATH_LONG);
	if (absPathLen == 0)
		Helper::PrintError(L"Unable to query absolute path of PEBakeryLauncher.exe", true);
	buffer[MAX_PATH_LONG - 1] = '\0'; // NULL guard for Windows XP

	// Build baseDir
	wchar_t* lastDirSepPos = StrRChrW(buffer, NULL, L'\\');
	if (lastDirSepPos == NULL)
	{
		Helper::PrintError(L"Unable to find base directory.", true);
		return;
	}
	lastDirSepPos[0] = '\0';
	baseDir = wstring(buffer);

	// Build pebakeryPath
	exePath = baseDir + L"\\Binary\\PEBakery.exe";
	dllPath = baseDir + L"\\Binary\\PEBakery.dll";
}
