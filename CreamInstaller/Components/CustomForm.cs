﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CreamInstaller.Components;

internal class CustomForm : Form
{
    internal CustomForm() : base()
    {
        Icon = Properties.Resources.Icon;
        KeyPreview = true;
        KeyPress += OnKeyPress;
        ResizeRedraw = true;
        HelpButton = true;
        HelpButtonClicked += OnHelpButtonClicked;
    }

    internal void OnHelpButtonClicked(object sender, EventArgs args)
    {
        using DialogForm helpDialog = new(this);
        helpDialog.HelpButton = false;
        _ = helpDialog.Show(SystemIcons.Information,
            "Automatically finds all installed Steam, Epic and Ubisoft games with their respective DLC-related DLL locations on the user's computer,\n"
          + "parses SteamCMD, Steam Store and Epic Games Store for user-selected games' DLCs, then provides a very simple graphical interface\n"
          + "utilizing the gathered information for the maintenance of DLC unlockers.\n"
          + "\n"
          + "The program utilizes the latest versions of [Koaloader](https://github.com/acidicoala/Koaloader), [SmokeAPI](https://github.com/acidicoala/SmokeAPI), [ScreamAPI](https://github.com/acidicoala/ScreamAPI), [Uplay R1 Unlocker](https://github.com/acidicoala/UplayR1Unlocker) and [Uplay R2 Unlocker](https://github.com/acidicoala/UplayR2Unlocker), all by\n"
          + "the wonderful [acidicoala](https://github.com/acidicoala), and all downloaded and embedded into the program itself; no further downloads necessary on your part!\n"
          + "\n"
          + "NOTE: This program does not automatically download nor install actual DLC files for you. As the title of the program says, it's\n"
          + "only a DLC Unlocker installer. Should the game you wish to unlock DLC for not already come with the DLCs installed (very many\n"
          + "do not), you have to find, download, and install those yourself. Preferably, you should be referring to the proper cs.rin.ru post for\n"
          + "the game(s) you're tinkering with; you'll usually find any answer to your problems there.\n"
          + "\n"
          + "USAGE:\n"
          + "    1. Choose which programs and/or games the program should scan for DLC.\n"
          + "            The program automatically gathers all installed games from Steam, Epic and Ubisoft directories.\n"
          + "    2. Wait for the program to download and install SteamCMD (if you chose a Steam game).\n"
          + "    3. Wait for the program to gather and cache the chosen games' information && DLCs.\n"
          + "             May take some time on the first run; depends on how many DLCs the games you chose have.\n"
          + "    4. CAREFULLY select which games' DLCs you wish to unlock.\n"
          + "            Obviously none of the DLC unlockers are tested for every single game!\n"
          + "    5. Choose whether or not to install with Koaloader, and if so then also pick the proxy DLL to use.\n"
          + "            If the default \'version.dll\' doesn't work, then see [here](https://cs.rin.ru/forum/viewtopic.php?p=2552172#p2552172) to find one that does.\n"
          + "    6. Click the \"Generate and Install\" button.\n"
          + "    7. Click the \"OK\" button to close the program.\n"
          + "    8. If any of the DLC unlockers cause problems with any of the games you installed them on, simply go back\n"
          + "        to step 5 and select what games you wish you revert changes to, and instead click the \"Uninstall\" button this time.\n"
          + "\n"
          + "For reliable and quick assistance, all bugs, crashes and other issues should be referred to the [GitHub Issues](https://github.com/pointfeev/CreamInstaller/issues) page!\n"
          + "\n"
          + "SteamCMD installation and appinfo cache can be found at [C:\\ProgramData\\CreamInstaller]().\n"
          + "The program automatically and very quickly updates from [GitHub](https://github.com/pointfeev/CreamInstaller) using [Onova](https://github.com/Tyrrrz/Onova). (updates can be ignored)\n"
          + "The program source and other information can be found on [GitHub](https://github.com/pointfeev/CreamInstaller).");
    }

    internal CustomForm(IWin32Window owner) : this()
    {
        if (owner is Form form)
        {
            Owner = form;
            InheritLocation(form);
            SizeChanged += (s, e) => InheritLocation(form);
            form.Activated += OnActivation;
            FormClosing += (s, e) => form.Activated -= OnActivation;
            TopLevel = true;
        }
    }

    internal void OnActivation(object sender, EventArgs args) => Activate();

    internal static readonly IntPtr HWND_NOTOPMOST = new(-2);
    internal static readonly IntPtr HWND_TOPMOST = new(-1);
    internal const short SWP_NOACTIVATE = 0x0010;
    internal const short SWP_SHOWWINDOW = 0x0040;
    internal const short SWP_NOMOVE = 0x0002;
    internal const short SWP_NOSIZE = 0x0001;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern void SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    internal void BringToFrontWithoutActivation()
    {
        bool topMost = TopMost;
        SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_NOMOVE | SWP_NOSIZE);
        if (!topMost)
            SetWindowPos(Handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOACTIVATE | SWP_SHOWWINDOW | SWP_NOMOVE | SWP_NOSIZE);
    }

    protected override CreateParams CreateParams // Double buffering for all controls
    {
        get
        {
            CreateParams handleParam = base.CreateParams;
            handleParam.ExStyle |= 0x02; // WS_EX_COMPOSITED       
            return handleParam;
        }
    }

    internal void InheritLocation(Form fromForm)
    {
        if (fromForm is null)
            return;
        int X = fromForm.Location.X + fromForm.Size.Width / 2 - Size.Width / 2;
        int Y = fromForm.Location.Y + fromForm.Size.Height / 2 - Size.Height / 2;
        Location = new(X, Y);
    }

    private void OnKeyPress(object s, KeyPressEventArgs e)
    {
        if (e.KeyChar != 'S') return; // Shift + S
        UpdateBounds();
        Rectangle bounds = Bounds;
        using Bitmap bitmap = new(Size.Width - 14, Size.Height - 7);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        using EncoderParameters encoding = new(1);
        using EncoderParameter encoderParam = new(Encoder.Quality, 100L);
        encoding.Param[0] = encoderParam;
        graphics.CopyFromScreen(new(bounds.Left + 7, bounds.Top), Point.Empty, new(Size.Width - 14, Size.Height - 7));
        Clipboard.SetImage(bitmap);
        e.Handled = true;
    }
}
