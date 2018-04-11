/*
 * MassEffectModder
 *
 * Copyright (C) 2014-2017 Pawel Kolodziejski <aquadran at users.sourceforge.net>
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

using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime;

namespace MassEffectModder
{
    public class CachePackageMgr
    {
        public List<Package> packages;

        public CachePackageMgr()
        {
            packages = new List<Package>();
        }

        public Package OpenPackage(string path)
        {
            if (!packages.Exists(p => p.packagePath.ToLowerInvariant() == path.ToLowerInvariant()))
            {
                Package pkg = new Package(path);
                packages.Add(pkg);
                return pkg;
            }
            else
            {
                return packages.Find(p => p.packagePath.ToLowerInvariant() == path.ToLowerInvariant());
            }
        }

        public void ClosePackageWithoutSave(Package package)
        {
            int index = packages.IndexOf(package);
            packages[index].Dispose();
            packages.RemoveAt(index);
        }

        public void CloseAllWithoutSave()
        {
            foreach (Package pkg in packages)
            {
                pkg.Dispose();
            }
            packages.Clear();
        }

        public void CloseAllWithSave(bool repack = false, bool ipc = false)
        {
            int lastProgress = -1;
            int skipCounter = 0;
            bool lowMemoryMode = false;
            ulong memorySize = ((new ComputerInfo().TotalPhysicalMemory / 1024 / 1024) + 1023) / 1024;
            if (memorySize <= 12)
                lowMemoryMode = true;

            for (int i = 0; i < packages.Count; i++)
            {
                Package pkg = packages[i];
                int newProgress = i * 100 / packages.Count;
                if (ipc && lastProgress != newProgress)
                {
                    Console.WriteLine("[IPC]TASK_PROGRESS " + newProgress);
                    Console.Out.Flush();
                    lastProgress = newProgress;
                }
                if (skipCounter > 10 && lowMemoryMode)
                {
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect();
                    skipCounter = 0;
                }
                skipCounter++;

                pkg.SaveToFile(repack);
                pkg.Dispose();
                if (repack && CmdLineTools.pkgsToRepack != null)
                    CmdLineTools.pkgsToRepack.Remove(pkg.packagePath);
            }

            if (GameData.gameType == MeType.ME3_TYPE)
                TOCBinFile.UpdateAllTOCBinFiles();

            packages.Clear();
        }
    }
}
