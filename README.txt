Code Vault - TOTP generator
============================

A small Windows Forms app that generates RFC 6238 TOTP codes from a
Base32 secret.

  - System.Security.Cryptography.HMACSHA1 does the actual TOTP math.
  - Windows DPAPI (ProtectedData, CurrentUser scope) encrypts the secret
    before it's written to disk. The encrypted bytes are only
    decryptable under the same Windows user account on the same
    machine that saved them.

Files
-----
CodeVault.csproj    project file (targets net8.0-windows, WinForms)
Program.cs          UI - settings view and big-code view
Totp.cs             Base32 decode + TOTP generation
ConfigStore.cs       DPAPI save/load/delete for CodeVault.dat
CodeVault.ico        app icon (title bar / taskbar)

Building
--------
Requires the .NET 8 SDK.

From this folder, run:

    dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

This produces a single portable EXE:

    bin\Release\net8.0-windows\win-x64\publish\CodeVault.exe

No .NET runtime needs to be installed wherever you run it - the
runtime is bundled into the exe. Copy CodeVault.exe wherever you want.

First run
---------
On first launch it shows the setup form: paste the Base32 secret, pick
digits/period, hit Save. That writes CodeVault.dat next to the exe and
switches to the big-code view. Every launch after that skips setup and
shows the code immediately. The gear icon in the corner brings setup
back up to replace or clear the saved secret.

Note: CodeVault.dat needs to live in a folder your Windows account can
write to - keep the exe out of a read-only or redirected folder.
