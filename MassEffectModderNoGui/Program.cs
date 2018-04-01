/*
 * MassEffectModder
 *
 * Copyright (C) 2014-2018 Pawel Kolodziejski <aquadran at users.sourceforge.net>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
 *
 */

using StreamHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MassEffectModder
{
    static class Program
    {
        [DllImport("kernel32", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern int FreeLibrary(IntPtr hModule);

        [DllImport("kernel32", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static List<string> dlls = new List<string>();
        public static string dllPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        public static Misc.MD5FileEntry[] entriesME1 = null;
        public static Misc.MD5FileEntry[] entriesME2 = null;
        public static Misc.MD5FileEntry[] entriesME3 = null;
        public static byte[] tableME1 = null;
        public static byte[] tableME2 = null;
        public static byte[] tableME3 = null;
        public static List<string> tablePkgsME1 = new List<string>();
        public static List<string> tablePkgsME2 = new List<string>();
        public static List<string> tablePkgsME3 = new List<string>();
        public static string MAINEXENAME = "MassEffectModder";

        static void loadEmbeddedDlls()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resources = assembly.GetManifestResourceNames();
            for (int l = 0; l < resources.Length; l++)
            {
                if (resources[l].EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    string dllName = resources[l].Substring(resources[l].IndexOf("Dlls.") + "Dlls.".Length);
                    string dllFilePath = Path.Combine(dllPath, dllName);
                    if (!Directory.Exists(dllPath))
                        Directory.CreateDirectory(dllPath);

                    using (Stream s = Assembly.GetEntryAssembly().GetManifestResourceStream(resources[l]))
                    {
                        byte[] buf = s.ReadToBuffer(s.Length);
                        if (File.Exists(dllFilePath))
                            File.Delete(dllFilePath);
                        File.WriteAllBytes(dllFilePath, buf);
                    }

                    IntPtr handle = LoadLibrary(dllFilePath);
                    if (handle == IntPtr.Zero)
                        throw new Exception();
                    dlls.Add(dllName);
                }
                else if (resources[l].EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                {
                    if (resources[l].Contains("MD5EntriesME1.bin"))
                    {
                        using (Stream s = Assembly.GetEntryAssembly().GetManifestResourceStream(resources[l]))
                        {
                            tableME1 = s.ReadToBuffer(s.Length);
                        }
                    }
                    else if (resources[l].Contains("MD5EntriesME2.bin"))
                    {
                        using (Stream s = Assembly.GetEntryAssembly().GetManifestResourceStream(resources[l]))
                        {
                            tableME2 = s.ReadToBuffer(s.Length);
                        }
                    }
                    else if (resources[l].Contains("MD5EntriesME3.bin"))
                    {
                        using (Stream s = Assembly.GetEntryAssembly().GetManifestResourceStream(resources[l]))
                        {
                            tableME3 = s.ReadToBuffer(s.Length);
                        }
                    }
                }
                else if (resources[l].EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    if (resources[l].Contains("PermissionsGranter.exe"))
                    {
                        string exePath = Path.Combine(dllPath, "PermissionsGranter.exe");
                        if (!Directory.Exists(dllPath))
                            Directory.CreateDirectory(dllPath);

                        using (Stream s = Assembly.GetEntryAssembly().GetManifestResourceStream(resources[l]))
                        {
                            byte[] buf = s.ReadToBuffer(s.Length);
                            if (File.Exists(exePath))
                                File.Delete(exePath);
                            File.WriteAllBytes(exePath, buf);
                        }
                    }
                }
            }
        }

        static void unloadEmbeddedDlls()
        {
            for (int l = 0; l < 10; l++)
            {
                foreach (ProcessModule mod in Process.GetCurrentProcess().Modules)
                {
                    if (dlls.Contains(mod.ModuleName))
                    {
                        FreeLibrary(mod.BaseAddress);
                    }
                }
            }

            try
            {
                if (Directory.Exists(dllPath))
                    Directory.Delete(dllPath, true);
            }
            catch
            {
            }
        }

        static void loadMD5Tables()
        {
            MemoryStream tmp = new MemoryStream(tableME1);
            tmp.SkipInt32();
            byte[] decompressed = new byte[tmp.ReadInt32()];
            byte[] compressed = tmp.ReadToBuffer((uint)tableME1.Length - 8);
            if (new ZlibHelper.Zlib().Decompress(compressed, (uint)compressed.Length, decompressed) == 0)
                throw new Exception();
            tmp = new MemoryStream(decompressed);
            int count = tmp.ReadInt32();
            tablePkgsME1 = new List<string>();
            for (int l = 0; l < count; l++)
            {
                tablePkgsME1.Add(tmp.ReadStringASCIINull());
            }
            count = tmp.ReadInt32();
            entriesME1 = new Misc.MD5FileEntry[count];
            for (int l = 0; l < count; l++)
            {
                entriesME1[l].path = tablePkgsME1[tmp.ReadInt32()];
                entriesME1[l].size = tmp.ReadInt32();
                entriesME1[l].md5 = tmp.ReadToBuffer(16);
            }

            tmp = new MemoryStream(tableME2);
            tmp.SkipInt32();
            decompressed = new byte[tmp.ReadInt32()];
            compressed = tmp.ReadToBuffer((uint)tableME2.Length - 8);
            if (new ZlibHelper.Zlib().Decompress(compressed, (uint)compressed.Length, decompressed) == 0)
                throw new Exception();
            tmp = new MemoryStream(decompressed);
            count = tmp.ReadInt32();
            tablePkgsME2 = new List<string>();
            for (int l = 0; l < count; l++)
            {
                tablePkgsME2.Add(tmp.ReadStringASCIINull());
            }
            count = tmp.ReadInt32();
            entriesME2 = new Misc.MD5FileEntry[count];
            for (int l = 0; l < count; l++)
            {
                entriesME2[l].path = tablePkgsME2[tmp.ReadInt32()];
                entriesME2[l].size = tmp.ReadInt32();
                entriesME2[l].md5 = tmp.ReadToBuffer(16);
            }

            tmp = new MemoryStream(tableME3);
            tmp.SkipInt32();
            decompressed = new byte[tmp.ReadInt32()];
            compressed = tmp.ReadToBuffer((uint)tableME3.Length - 8);
            if (new ZlibHelper.Zlib().Decompress(compressed, (uint)compressed.Length, decompressed) == 0)
                throw new Exception();
            tmp = new MemoryStream(decompressed);
            count = tmp.ReadInt32();
            tablePkgsME3 = new List<string>();
            for (int l = 0; l < count; l++)
            {
                tablePkgsME3.Add(tmp.ReadStringASCIINull());
            }
            count = tmp.ReadInt32();
            entriesME3 = new Misc.MD5FileEntry[count];
            for (int l = 0; l < count; l++)
            {
                entriesME3[l].path = tablePkgsME3[tmp.ReadInt32()];
                entriesME3[l].size = tmp.ReadInt32();
                entriesME3[l].md5 = tmp.ReadToBuffer(16);
            }
        }

        static void DisplayHelp()
        {
            Console.WriteLine("Help:\n");
            Console.WriteLine("  -help\n");
            Console.WriteLine("     This help");
            Console.WriteLine("");
            Console.WriteLine("  -version\n");
            Console.WriteLine("     Display MEM version");
            Console.WriteLine("");
            Console.WriteLine("  -check-game-data-after <game id> [-ipc]\n");
            Console.WriteLine("     Check game data for mods installed after textures installation.\n");
            Console.WriteLine("");
            Console.WriteLine("  -check-game-data-mismatch <game id> [-ipc]\n");
            Console.WriteLine("     Check game data with md5 database.\n");
            Console.WriteLine("     Scan to detect mods");
            Console.WriteLine("");
            Console.WriteLine("  -check-game-data-only-vanilla <game id> [-ipc]\n");
            Console.WriteLine("     Check game data with md5 database.\n");
            Console.WriteLine("");
            Console.WriteLine("  -install-mods <game id> <input dir> [-repack] [-ipc] [-alot-mode]\n");
            Console.WriteLine("     Install MEM mods from input directory.\n");
            Console.WriteLine("");
            Console.WriteLine("  -apply-me1-laa\n");
            Console.WriteLine("     Apply LAA patch to ME1 executable.\n");
            Console.WriteLine("");
            Console.WriteLine("  -detect-mods <game id> [-ipc]\n");
            Console.WriteLine("     Detect compatibe mods.\n");
            Console.WriteLine("");
            Console.WriteLine("  -detect-bad-mods <game id> [-ipc]\n");
            Console.WriteLine("     Detect not compatibe mods.\n");
            Console.WriteLine("");
            Console.WriteLine("  -apply-lods-gfx <game id> [-limit2k] [-soft-shadows-mode] [-meuitm-mode]\n");
            Console.WriteLine("     Update LODs and GFX settings.\n");
            Console.WriteLine("");
            Console.WriteLine("  -remove-lods <game id>\n");
            Console.WriteLine("     Remove LODs settings.\n");
            Console.WriteLine("");
            Console.WriteLine("  -convert-to-mem <game id> <input dir> <output file> [-ipc]\n");
            Console.WriteLine("     game id: 1 for ME1, 2 for ME2, 3 for ME3");
            Console.WriteLine("     input dir: directory to be converted, containing following file extension(s):");
            Console.WriteLine("        MEM, MOD, TPF");
            Console.WriteLine("        BIN - package export raw data");
            Console.WriteLine("           Naming pattern used for package in DLC:");
            Console.WriteLine("             D<DLC dir length>-<DLC dir>-<pkg filename length>-<pkg filename>-E<pkg export id>.bin");
            Console.WriteLine("             example: D10-DLC_HEN_PR-23-BioH_EDI_02_Explore.pcc-E6101.bin");
            Console.WriteLine("           Naming pattern used for package in base directory:");
            Console.WriteLine("             B<pkg filename length>-<pkg filename>-E<pkg export id>.bin");
            Console.WriteLine("             example: B23-BioH_EDI_00_Explore.pcc-E5090.bin");
            Console.WriteLine("        XDELTA - package export xdelta3 patch data");
            Console.WriteLine("           Naming pattern used for package in DLC:");
            Console.WriteLine("             D<DLC dir length>-<DLC dir>-<pkg filename length>-<pkg filename>-E<pkg export id>.xdelta");
            Console.WriteLine("             example: D10-DLC_HEN_PR-23-BioH_EDI_02_Explore.pcc-E6101.xdelta");
            Console.WriteLine("           Naming pattern used for package in base directory:");
            Console.WriteLine("             B<pkg filename length>-<pkg filename>-E<pkg export id>.xdelta");
            Console.WriteLine("             example: B23-BioH_EDI_00_Explore.pcc-E5090.xdelta");
            Console.WriteLine("        DDS, BMP, TGA, PNG, JPG, JPEG");
            Console.WriteLine("           input format supported for DDS images:");
            Console.WriteLine("              DXT1, DXT3, DTX5, ATI2, V8U8, G8, RGBA, RGB");
            Console.WriteLine("           input format supported for TGA images:");
            Console.WriteLine("              uncompressed RGBA/RGB, compressed RGBA/RGB");
            Console.WriteLine("           input format supported for BMP images:");
            Console.WriteLine("              uncompressed RGBA/RGB/RGBX");
            Console.WriteLine("           Image filename must include texture CRC (0xhhhhhhhh)");
            Console.WriteLine("     ipc: turn on IPC traces");
            Console.WriteLine("");
            Console.WriteLine("  -convert-game-image <game id> <input image> <output image>\n");
            Console.WriteLine("     game id: 1 for ME1, 2 for ME2, 3 for ME3");
            Console.WriteLine("     Input file with following extension:");
            Console.WriteLine("        DDS, BMP, TGA, PNG, JPG, JPEG");
            Console.WriteLine("           input format supported for DDS images:");
            Console.WriteLine("              DXT1, DXT3, DTX5, ATI2, V8U8, G8, RGBA, RGB");
            Console.WriteLine("           input format supported for TGA images:");
            Console.WriteLine("              uncompressed RGBA/RGB, compressed RGBA/RGB");
            Console.WriteLine("           input format supported for BMP images:");
            Console.WriteLine("              uncompressed RGBA/RGB/RGBX");
            Console.WriteLine("           Image filename must include texture CRC (0xhhhhhhhh)");
            Console.WriteLine("     Output file is DDS image");
            Console.WriteLine("");
            Console.WriteLine("  -convert-game-images <game id> <input dir> <output dir>\n");
            Console.WriteLine("     game id: 1 for ME1, 2 for ME2, 3 for ME3");
            Console.WriteLine("     input dir: directory to be converted, containing following file extension(s):");
            Console.WriteLine("        Input files with following extension:");
            Console.WriteLine("        DDS, BMP, TGA, PNG, JPEG");
            Console.WriteLine("           input format supported for DDS images:");
            Console.WriteLine("              DXT1, DXT3, DTX5, ATI2, V8U8, G8, RGBA, RGB");
            Console.WriteLine("           input format supported for TGA images:");
            Console.WriteLine("              uncompressed RGBA/RGB, compressed RGBA/RGB");
            Console.WriteLine("           input pixel format supported for BMP images:");
            Console.WriteLine("              uncompressed RGBA/RGB/RGBX");
            Console.WriteLine("           Image filename must include texture CRC (0xhhhhhhhh)");
            Console.WriteLine("     output dir: directory where textures converted to DDS are placed");
            Console.WriteLine("");
            Console.WriteLine("  -extract-mod <game id> <input dir> <output dir> [-ipc]\n");
            Console.WriteLine("     game id: 1 for ME1, 2 for ME2, 3 for ME3");
            Console.WriteLine("     input dir: directory of ME3Explorer MOD file(s)");
            Console.WriteLine("     Can extract textures and package export raw data");
            Console.WriteLine("     Naming pattern used for package in DLC:");
            Console.WriteLine("        D<DLC dir length>-<DLC dir>-<pkg filename length>-<pkg filename>-E<pkg export id>.bin");
            Console.WriteLine("        example: D10-DLC_HEN_PR-23-BioH_EDI_02_Explore.pcc-E6101.bin");
            Console.WriteLine("     Naming pattern used for package in base directory:");
            Console.WriteLine("        B<pkg filename length>-<pkg filename>-E<pkg export id>.bin");
            Console.WriteLine("        example: B23-BioH_EDI_00_Explore.pcc-E5090.bin");
            Console.WriteLine("");
            Console.WriteLine("  -extract-mem <game id> <input dir> <output dir> [-ipc]\n");
            Console.WriteLine("     game id: 1 for ME1, 2 for ME2, 3 for ME3");
            Console.WriteLine("     input dir: directory of MEM mod file(s)");
            Console.WriteLine("     Can extract textures and package export raw data");
            Console.WriteLine("     Naming pattern used for package in DLC:");
            Console.WriteLine("        D<DLC dir length>-<DLC dir>-<pkg filename length>-<pkg filename>-E<pkg export id>.bin");
            Console.WriteLine("        example: D10-DLC_HEN_PR-23-BioH_EDI_02_Explore.pcc-E6101.bin");
            Console.WriteLine("     Naming pattern used for package in base directory:");
            Console.WriteLine("        B<pkg filename length>-<pkg filename>-E<pkg export id>.bin");
            Console.WriteLine("        example: B23-BioH_EDI_00_Explore.pcc-E5090.bin");
            Console.WriteLine("");
            Console.WriteLine("  -extract-tpf <input dir> <output dir> [-ipc]\n");
            Console.WriteLine("     input dir: directory containing the TPF file(s) to be extracted");
            Console.WriteLine("     Textures are extracted as they are in the TPF, no additional modifications are made.");
            Console.WriteLine("");
            Console.WriteLine("  -convert-image <output pixel format> [dxt1 alpha threshold] <input image> <output image>\n");
            Console.WriteLine("     input image file types: DDS, BMP, TGA, PNG, JPEG");
            Console.WriteLine("     output image file type: DDS");
            Console.WriteLine("     output pixel format: DXT1 (no alpha), DXT1a (alpha), DXT3, DXT5, ATI2, V8U8, G8, RGBA, RGB");
            Console.WriteLine("     For DXT1a you have to set the alpha threshold (0-255). 128 is suggested as a default value.");
            Console.WriteLine("");
            Console.WriteLine("  -extract-all-dds <game id> <output dir> [TFC filter name]\n");
            Console.WriteLine("     game id: 1 for ME1, 2 for ME2, 3 for ME3");
            Console.WriteLine("     output dir: directory where textures converted to DDS are placed");
            Console.WriteLine("     TFC filter name: it will filter only textures stored in specific TFC file.");
            Console.WriteLine("     Textures are extracted as they are in game data, only DDS header is added.");
            Console.WriteLine("");
            Console.WriteLine("  -extract-all-png <game id> <output dir>\n");
            Console.WriteLine("     game id: 1 for ME1, 2 for ME2, 3 for ME3");
            Console.WriteLine("     output dir: directory where textures converted to PNG are placed");
            Console.WriteLine("     Textures are extracted with only top mipmap.");
            Console.WriteLine("");
            Console.WriteLine("  -me3dlcmod-for-mgamerz <mem file> <tfc name> [<guid in 16 hex digits>]\n");
            Console.WriteLine("     Replace textures from <mem file> and store in new <tfc name> file.");
            Console.WriteLine("     New TFC name must be added earlier to PCC files.");
            Console.WriteLine("");
            Console.WriteLine("\n");
        }

        [STAThread]

        static void Main(string[] args)
        {
            string cmd = "";
            string game;
            string input = "";
            string output = "";
            MeType gameId = 0;
            bool ipc = false;
            bool repack = false;

            if (args.Length > 0)
                cmd = args[0];

            Console.WriteLine(Environment.NewLine + Environment.NewLine +
                "--- MEM no GUI v" + Application.ProductVersion + " command line --- " + Environment.NewLine);
            for (int i = 0; i < args.Length; i++)
                Console.Write(args[i] + " ");
            Console.WriteLine(Environment.NewLine);

            if (cmd.Equals("-help", StringComparison.OrdinalIgnoreCase))
            {
                DisplayHelp();
                Environment.Exit(0);
            }
            if (cmd.Equals("-version", StringComparison.OrdinalIgnoreCase))
            {
                Environment.Exit(0);
            }

            if (cmd.Equals("-convert-to-mem", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-convert-game-image", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-convert-game-images", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-install-mods", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-extract-mod", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-extract-mem", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-detect-mods", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-detect-bad-mods", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-apply-lods-gfx", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-remove-lods", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-check-game-data-textures", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-check-game-data-mismatch", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-check-game-data-after", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-check-game-data-only-vanilla", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-extract-all-dds", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-extract-all-png", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 1)
                {
                    Console.WriteLine("Error: wrong game id!");
                    DisplayHelp();
                    goto fail;
                }
                game = args[1];
                try
                {
                    gameId = (MeType)int.Parse(game);
                }
                catch
                {
                    gameId = 0;
                }
                if (gameId != MeType.ME1_TYPE && gameId != MeType.ME2_TYPE && gameId != MeType.ME3_TYPE)
                {
                    Console.WriteLine("Error: wrong game id!");
                    DisplayHelp();
                    goto fail;
                }
            }

            if (cmd.Equals("-convert-to-mem", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-install-mods", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-convert-game-image", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-convert-game-images", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-extract-mem", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-extract-mod", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 3)
                {
                    Console.WriteLine("Error: missing input argument!");
                    DisplayHelp();
                    goto fail;
                }
                input = args[2];
                if (!Directory.Exists(input) && !File.Exists(input))
                {
                    Console.WriteLine("Error: input file/directory doesnt exists: " + input);
                    goto fail;
                }
            }

            if (cmd.Equals("-convert-to-mem", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-convert-game-image", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-convert-game-images", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-extract-mem", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-extract-mod", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 4)
                {
                    Console.WriteLine("Error: missing output argument!");
                    DisplayHelp();
                    goto fail;
                }
                output = args[3];
            }

            if (cmd.Equals("-convert-to-mem", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-extract-mem", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-install-mods", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-extract-mod", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-extract-mem", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-detect-bad-mods", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-detect-mods", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-check-game-data-textures", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-check-game-data-mismatch", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-check-game-data-after", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-check-game-data-only-vanilla", StringComparison.OrdinalIgnoreCase))
            {
                for (int l = 0; l < args.Length; l++)
                {
                    if (args[l].ToLowerInvariant() == "-ipc")
                        ipc = true;
                }
            }

            if (cmd.Equals("-install-mods", StringComparison.OrdinalIgnoreCase) ||
                cmd.Equals("-convert-to-mem", StringComparison.OrdinalIgnoreCase))
            {
                for (int l = 0; l < args.Length; l++)
                {
                    if (args[l].ToLowerInvariant() == "-repack")
                        repack = true;
                }
            }

            if (cmd.Equals("-convert-to-mem", StringComparison.OrdinalIgnoreCase))
            {
                loadEmbeddedDlls();
                loadMD5Tables();
                if (!CmdLineTools.ConvertToMEM(gameId, input, output, ipc))
                    goto fail;
            }
            else if (cmd.Equals("-install-mods", StringComparison.OrdinalIgnoreCase))
            {
                loadEmbeddedDlls();
                loadMD5Tables();
                bool guiInstaller = false;
                for (int l = 0; l < args.Length; l++)
                {
                    if (args[l].ToLowerInvariant() == "-alot-mode" ||
                        args[l].ToLowerInvariant() == "-gui-installer")
                    {
                        guiInstaller = true;
                    }
                }
                if (!CmdLineTools.InstallMods(gameId, input, ipc, repack, guiInstaller))
                    goto fail;
            }
            else if (cmd.Equals("-apply-me1-laa", StringComparison.OrdinalIgnoreCase))
            {
                if (!CmdLineTools.ApplyME1LAAPatch())
                    goto fail;
            }
            else if (cmd.Equals("-detect-mods", StringComparison.OrdinalIgnoreCase))
            {
                loadEmbeddedDlls();
                if (!CmdLineTools.DetectMods(gameId, ipc))
                    goto fail;
            }
            else if (cmd.Equals("-detect-bad-mods", StringComparison.OrdinalIgnoreCase))
            {
                loadEmbeddedDlls();
                if (!CmdLineTools.DetectBadMods(gameId, ipc))
                    goto fail;
            }
            else if (cmd.Equals("-apply-lods-gfx", StringComparison.OrdinalIgnoreCase))
            {
                bool softShadowsME1 = false;
                bool meuitmMode = false;
                for (int l = 0; l < args.Length; l++)
                {
                    if (args[l].ToLowerInvariant() == "-soft-shadows-mode")
                        softShadowsME1 = true;
                    if (args[l].ToLowerInvariant() == "-meuitm-mode")
                        meuitmMode = true;
                }

                if (!CmdLineTools.ApplyLODAndGfxSettings(gameId, softShadowsME1, meuitmMode))
                    goto fail;
            }
            else if (cmd.Equals("-remove-lods", StringComparison.OrdinalIgnoreCase))
            {
                if (!CmdLineTools.RemoveLODSettings(gameId))
                    goto fail;
            }
            else if (cmd.Equals("-check-game-data-textures", StringComparison.OrdinalIgnoreCase))
            {
                loadEmbeddedDlls();
                if (!CmdLineTools.CheckTextures(gameId, ipc))
                    goto fail;
            }
            else if (cmd.Equals("-check-game-data-mismatch", StringComparison.OrdinalIgnoreCase))
            {
                loadEmbeddedDlls();
                if (!Misc.detectsMismatchPackagesAfter(gameId, ipc))
                    goto fail;
            }
            else if (cmd.Equals("-check-game-data-after", StringComparison.OrdinalIgnoreCase))
            {
                loadEmbeddedDlls();
                if (!Misc.checkGameFilesAfter(gameId, ipc))
                    goto fail;
            }
            else if (cmd.Equals("-check-game-data-only-vanilla", StringComparison.OrdinalIgnoreCase))
            {
                loadEmbeddedDlls();
                loadMD5Tables();
                if (!CmdLineTools.CheckGameData(gameId, ipc))
                    goto fail;
            }
            else if (cmd.Equals("-convert-game-image", StringComparison.OrdinalIgnoreCase))
            {
                if (Path.GetExtension(output).ToLowerInvariant() != ".dds")
                {
                    Console.WriteLine("Error: output file is not dds: " + output);
                    goto fail;
                }
                loadEmbeddedDlls();
                loadMD5Tables();
                if (!CmdLineTools.convertGameImage(gameId, input, output))
                    goto fail;
            }
            else if (cmd.Equals("-convert-game-images", StringComparison.OrdinalIgnoreCase))
            {
                loadEmbeddedDlls();
                loadMD5Tables();
                if (!CmdLineTools.convertGameImages(gameId, input, output))
                    goto fail;
            }
            else if (cmd.Equals("-extract-tpf", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length != 3)
                {
                    Console.WriteLine("Error: wrong arguments!");
                    DisplayHelp();
                    goto fail;
                }

                input = args[1];
                output = args[2];
                if (!Directory.Exists(input))
                {
                    Console.WriteLine("Error: input dir not exists: " + input);
                    goto fail;
                }
                else
                {
                    loadEmbeddedDlls();
                    if (!CmdLineTools.extractTPF(input, output, ipc))
                        goto fail;
                }
            }
            else if (cmd.Equals("-extract-mod", StringComparison.OrdinalIgnoreCase))
            {
                loadEmbeddedDlls();
                loadMD5Tables();
                if (!CmdLineTools.extractMOD(gameId, input, output, ipc))
                    goto fail;
            }
            else if (cmd.Equals("-extract-mem", StringComparison.OrdinalIgnoreCase))
            {
                loadEmbeddedDlls();
                if (!CmdLineTools.extractMEM(gameId, input, output, ipc))
                    goto fail;
            }
            else if (cmd.Equals("-convert-image", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 4)
                {
                    Console.WriteLine("Error: wrong arguments!");
                    DisplayHelp();
                    goto fail;
                }

                string format = args[1];
                string threshold = "128";
                if (format == "dxt1a")
                {
                    if (args.Length == 5)
                    {
                        threshold = args[2];
                        input = args[3];
                        output = args[4];
                    }
                    else
                    {
                        input = args[2];
                        output = args[3];
                    }
                }
                else
                {
                    input = args[2];
                    output = args[3];
                }

                if (!File.Exists(input))
                {
                    Console.WriteLine("Error: input file not exists: " + input);
                    goto fail;
                }
                else
                {
                    if (Path.GetExtension(output).ToLowerInvariant() != ".dds")
                    {
                        Console.WriteLine("Error: output file is not dds: " + output);
                        goto fail;
                    }
                    loadEmbeddedDlls();
                    loadMD5Tables();
                    if (!CmdLineTools.convertImage(input, output, format, threshold))
                        goto fail;
                }
            }
            else if (cmd.Equals("-extract-all-dds", StringComparison.OrdinalIgnoreCase) ||
                     cmd.Equals("-extract-all-png", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length != 3 && args.Length != 4)
                {
                    Console.WriteLine("Error: wrong arguments!");
                    DisplayHelp();
                    goto fail;
                }

                output = args[2];
                string tfcFilter = "";
                if (args.Length > 3)
                    tfcFilter = args[3];

                loadEmbeddedDlls();
                loadMD5Tables();
                if (cmd.Equals("-extract-all-dds", StringComparison.OrdinalIgnoreCase))
                    if (!CmdLineTools.extractAllTextures(gameId, output, false, tfcFilter))
                        goto fail;
                if (cmd.Equals("-extract-all-png", StringComparison.OrdinalIgnoreCase))
                    if (!CmdLineTools.extractAllTextures(gameId, output, true, ""))
                        goto fail;
            }
            else if (cmd.Equals("-me3dlcmod-for-mgamerz", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length != 3 && args.Length != 4)
                {
                    Console.WriteLine("Error: wrong arguments!");
                    DisplayHelp();
                    goto fail;
                }

                input = args[1];
                string tfcName = args[2];
                byte[] guid;
                if (args.Length == 4)
                {
                    if (args[3].Length != 32)
                    {
                        Console.WriteLine("Error: wrong guid!");
                        DisplayHelp();
                        goto fail;
                    }
                    guid = new byte[16];
                    for (int i = 0; i < 32; i += 2)
                        guid[i / 2] = Convert.ToByte(args[3].Substring(i, 2), 16);
                }
                else
                    guid = Guid.NewGuid().ToByteArray();
                loadEmbeddedDlls();
                loadMD5Tables();
                if (!CmdLineTools.applyMEMSpecialModME3(input, tfcName, guid))
                    goto fail;
            }
            else
                DisplayHelp();

            unloadEmbeddedDlls();
            Environment.Exit(0);

fail:
            unloadEmbeddedDlls();
            Environment.Exit(1);
        }

    }
}
