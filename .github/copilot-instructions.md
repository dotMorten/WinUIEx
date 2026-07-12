# WinUIEx repository instructions

## Build, test, and lint commands

This repo is a Windows-only .NET / WinUI 3 solution. The main solution file is `src\WinUIExtensions.sln`.

### Main package build / pack

```powershell
msbuild /restore /t:Build,Pack src\WinUIEx\WinUIEx.csproj /p:Configuration=Release
```

### WinUI UI tests

`src\WinUIEx.Tests` is a packaged WinUI test app, not a plain test library. **Do not use `dotnet test` for this project**; run the generated `.appxrecipe` with `vstest.console.exe`.

Build the packaged test app first:

```powershell
msbuild /restore /t:Build src\WinUIEx.Tests\WinUIEx.Tests.csproj /p:Platform=x64 /p:Configuration=Release
```

If `vstest.console.exe` is not already on `PATH`, locate it first:

```powershell
& "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.VisualStudio.PackageGroup.TestTools.Core -find "Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
```

Run the full UI test suite:

```powershell
vstest.console.exe src\WinUIEx.Tests\bin\x64\Release\net8.0-windows10.0.19041.0\WinUIEx.Tests.build.appxrecipe --logger:"console;verbosity=normal" /InIsolation
```

Run a single UI test:

```powershell
vstest.console.exe src\WinUIEx.Tests\bin\x64\Release\net8.0-windows10.0.19041.0\WinUIEx.Tests.build.appxrecipe --logger:"console;verbosity=normal" /InIsolation /Tests:WinUIUnitTests.WindowManagerTests.SetWidth
```

### Analyzer tests

Build the analyzer test project:

```powershell
msbuild /restore /t:Build src\WinUIEx.Analyzers\WinUIEx.Analyzers.Test\WinUIEx.Analyzers.Test.csproj /p:Configuration=Release
```

Run the full analyzer test suite:

```powershell
vstest.console.exe src\WinUIEx.Analyzers\WinUIEx.Analyzers.Test\bin\Release\net8.0-windows10.0.19041.0\WinUIEx.Analyzers.Test.dll --logger:"console;verbosity=normal" /InIsolation
```

Run a single analyzer test:

```powershell
vstest.console.exe src\WinUIEx.Analyzers\WinUIEx.Analyzers.Test\bin\Release\net8.0-windows10.0.19041.0\WinUIEx.Analyzers.Test.dll --logger:"console;verbosity=normal" /InIsolation /Tests:WinUIEx.Analyzers.Test.WinUIExFrameNavigateAnalyzerTests.Frame_Navigate_With_NonPage_Type
```

### Linting

There is no separate lint command in the repo. Treat compiler/analyzer output as the lint signal, and note that `src\WinUIEx\WinUIEx.csproj` enables `TreatWarningsAsErrors` in `Release`.

## High-level architecture

- `src\WinUIEx` is the shipping library. It targets `net8.0-windows10.0.19041.0` and packages Roslyn analyzers from `src\WinUIEx.Analyzers` into the main NuGet package via a project reference plus the `_AddAnalyzersToOutput` target.
- The windowing stack has three layers that should usually evolve together:
  - `WindowEx` is the convenience `Window` subclass.
  - `WindowManager` owns most behavior: window state/presenter tracking, persistence, tray integration, and backdrop management.
  - `WindowExtensions` and `HwndExtensions` expose the same functionality as extension helpers over `Window`, `AppWindow`, and raw HWND/Win32 APIs.
- `WindowManager` is intentionally split across partial files:
  - `WindowManager.cs` for lifecycle, presenter/state handling, native message handling, and persistence.
  - `WindowManager.Backdrop.cs` for backdrop/controller setup.
  - `WindowManager.TrayIcon.cs` for tray icon behavior.
  It depends on `Messaging\WindowMessageMonitor`, which subclasses the native window procedure and drives message-based features such as min/max constraints, DPI restore behavior, state changes, and tray icon updates.
- `NumberBox<T>` is a multi-file custom control. Template behavior lives in `NumberBox.cs`, dependency properties in `NumberBox.Properties.cs`, parsing in `NumberBoxParser.cs`, and accessibility support in `NumberBoxAutomationPeer.cs`. Changes to value/text behavior usually span more than one of those files.
- `src\WinUIEx.Tests` is not a plain unit-test library; it is a packaged WinUI test app. `App.xaml.cs` boots a `WindowEx`, then hands control to `Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient`. Most tests use `UITestHelper` to enqueue work onto the app window's `DispatcherQueue`.
- `src\WinUIEx.Analyzers` contains the Roslyn analyzers, code fixes, and verifier-based MSTest suite. Public analyzer behavior is documented in `docs\rules`.
- `src\WinUIExSample` and `src\WinUIExMauiSample` are the best usage references for startup-sensitive features such as `WebAuthenticator`, splash screens, persistence, tray support, and MAUI integration.

## Key conventions

- Preserve the two public API surfaces for window features: WinUI-level methods usually exist in `WindowExtensions`, while low-level HWND implementations live in `HwndExtensions`. If behavior changes in one layer, check the other layer and its tests.
- `WindowEx` should stay a thin wrapper over `WindowManager`, not a second independent implementation of the same window logic.
- Keep compatibility shims unless there is an explicit repo-wide decision to remove them. Several members are intentionally obsolete or retained for binary compatibility while forwarding to newer WinAppSDK APIs.
- When a change adds a user-visible library feature or fixes a shipped library bug since the last release, add a matching bullet to `PackageReleaseNotes` in `src\WinUIEx\WinUIEx.csproj`.
- For startup OAuth changes, keep the current activation pattern intact: `WebAuthenticator.CheckOAuthRedirectionActivation(...)` must run before normal app initialization in the WinUI and MAUI sample entry points.
- For persistence changes, remember that `PersistenceId` must be set before first activation. Unpackaged apps must also provide `WindowManager.PersistenceStorage`; the sample app shows the expected pattern.
- WinUI UI tests should continue to use `UITestHelper` and dispatcher-queued execution instead of manipulating windows directly from the test thread.
- Feature documentation lives under `docs\concepts`. When changing public behavior of core features like `WindowManager`, `WindowEx`, `WebAuthenticator`, tray support, or `NumberBox`, check whether the matching concept doc should be updated too.
