Code Vault - C# TOTP generator
===============================

What this is
-------------
A small Windows Forms app that generates RFC 6238 TOTP codes from a Base32
secret. Unlike the earlier HTA version, this uses:

  - System.Security.Cryptography.HMACSHA1 for the actual TOTP math
    (the framework's real implementation, not hand-rolled crypto)
  - Windows DPAPI (ProtectedData, CurrentUser scope) to encrypt the secret
    before it's written to disk. The encrypted bytes are only decryptable
    under the same Windows user account on the same machine that saved
    them - copying CodeVault.dat elsewhere, or another account reading
    it, yields nothing usable.

Files
-----
CodeVault.csproj   project file (targets net8.0-windows, WinForms)
Program.cs         the UI (settings view + big-code view, same layout
                    and behavior as the HTA version)
Totp.cs            Base32 decode + TOTP generation
ConfigStore.cs      DPAPI save/load/delete for CodeVault.dat
CodeVault.ico       app icon (title bar / taskbar)

Building
--------
You've already installed the SDK via dotnet-install.ps1, so dotnet.exe is
under:

    %LOCALAPPDATA%\dotnet-sdk\dotnet.exe

Open a terminal in this folder (CodeVault-csharp) and run:

    "%LOCALAPPDATA%\dotnet-sdk\dotnet.exe" publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

(If you added the SDK folder to your PATH, you can drop the quoted prefix
and just run `dotnet publish ...`.)

That produces a single portable EXE here:

    bin\Release\net8.0-windows\win-x64\publish\CodeVault.exe

That one file is everything - no .NET runtime needs to be installed on
whatever machine you run it on, since --self-contained bundles the
runtime into the exe. Copy CodeVault.exe (and CodeVault.ico, if you want
the icon to keep showing - it's only needed at runtime if you rebuild
with a different ApplicationIcon setup, otherwise the icon is already
baked into the compiled exe itself) wherever you want to run it from.

First run
---------
On first launch it shows the setup form: paste the Base32 secret, pick
digits/period, hit Save. That writes CodeVault.dat next to the exe and
switches straight to the big-code view. Every launch after that skips
setup and shows the code immediately. The gear icon in the corner brings
setup back up to replace or clear the saved secret.

Note on the data file
----------------------
CodeVault.dat needs to live in a folder your Windows account can write
to - same caveat as before, so keep the exe out of a read-only or
redirected folder if saving fails.
