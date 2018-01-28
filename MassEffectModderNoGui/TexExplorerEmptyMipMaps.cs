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

using StreamHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MassEffectModder
{
    public partial class MipMaps
    {
        public void AddMarkerToPackages(string packagePath)
        {
            string path = "";
            if (GameData.gameType == MeType.ME1_TYPE)
            {
                path = @"\BioGame\CookedPC\testVolumeLight_VFX.upk".ToLowerInvariant();
            }
            if (GameData.gameType == MeType.ME2_TYPE)
            {
                path = @"\BioGame\CookedPC\BIOC_Materials.pcc".ToLowerInvariant();
            }
            if (path != "" && packagePath.ToLowerInvariant().Contains(path))
                return;
            try
            {
                using (FileStream fs = new FileStream(packagePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    fs.SeekEnd();
                    fs.Seek(-Package.MEMendFileMarker.Length, SeekOrigin.Current);
                    string marker = fs.ReadStringASCII(Package.MEMendFileMarker.Length);
                    if (marker != Package.MEMendFileMarker)
                    {
                        fs.SeekEnd();
                        fs.WriteStringASCII(Package.MEMendFileMarker);
                    }
                }
            }
            catch
            {
            }
        }

        public void removeMipMapsME1(int phase, List<FoundTexture> textures, CachePackageMgr cachePackageMgr,
            bool ipc, bool forceZlib = false)
        {
            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                bool modified = false;
                Package package = null;
                if (ipc)
                {
                    Console.WriteLine("[IPC]OVERALL_PROGRESS " + ((GameData.packageFiles.Count * (phase - 1) + i + 1) * 100 / (GameData.packageFiles.Count * 2)));
                    Console.Out.Flush();
                }

                try
                {
                    if (cachePackageMgr != null)
                        package = cachePackageMgr.OpenPackage(GameData.packageFiles[i]);
                    else
                        package = new Package(GameData.packageFiles[i], true);
                }
                catch (Exception e)
                {
                    string err = "";
                    err += "---- Start --------------------------------------------" + Environment.NewLine;
                    err += "Issue with open package file: " + GameData.packageFiles[i] + Environment.NewLine;
                    err += e.Message + Environment.NewLine + Environment.NewLine;
                    err += e.StackTrace + Environment.NewLine + Environment.NewLine;
                    err += "---- End ----------------------------------------------" + Environment.NewLine + Environment.NewLine;
                    Console.WriteLine(err);
                    if (ipc)
                    {
                        Console.WriteLine("[IPC]ERROR Issue with open package file: " + GameData.packageFiles[i]);
                        Console.Out.Flush();
                    }
                    continue;
                }

                for (int l = 0; l < package.exportsTable.Count; l++)
                {
                    int id = package.getClassNameId(package.exportsTable[l].classId);
                    if (id == package.nameIdTexture2D ||
                        id == package.nameIdTextureFlipBook)
                    {
                        using (Texture texture = new Texture(package, l, package.getExportData(l), false))
                        {
                            if (!texture.hasImageData() ||
                                !texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
                            {
                                continue;
                            }
                            do
                            {
                                texture.mipMapsList.Remove(texture.mipMapsList.First(s => s.storageType == Texture.StorageTypes.empty));
                            } while (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty));
                            texture.properties.setIntValue("SizeX", texture.mipMapsList.First().width);
                            texture.properties.setIntValue("SizeY", texture.mipMapsList.First().height);
                            texture.properties.setIntValue("MipTailBaseIdx", texture.mipMapsList.Count() - 1);

                            FoundTexture foundTexture = new FoundTexture();
                            int foundListEntry = -1;
                            string pkgName = GameData.RelativeGameData(package.packagePath).ToLowerInvariant();
                            for (int k = 0; k < textures.Count; k++)
                            {
                                for (int t = 0; t < textures[k].list.Count; t++)
                                {
                                    if (textures[k].list[t].exportID == l &&
                                        textures[k].list[t].path.ToLowerInvariant() == pkgName)
                                    {
                                        foundTexture = textures[k];
                                        foundListEntry = t;
                                        break;
                                    }
                                }
                            }
                            if (foundListEntry == -1)
                            {
                                Console.WriteLine("Error: Texture " + package.exportsTable[l].objectName + " not found in package: " + GameData.packageFiles[i] + ", skipping..." + Environment.NewLine);
                                if (ipc)
                                {
                                    Console.WriteLine("[IPC]ERROR Texture " + package.exportsTable[l].objectName + " not found in package: " + GameData.packageFiles[i] + ", skipping...");
                                    Console.Out.Flush();
                                }
                                goto skip;
                            }

                            if (foundTexture.list[foundListEntry].linkToMaster != -1)
                            {
                                if (phase == 1)
                                    continue;

                                MatchedTexture foundMasterTex = foundTexture.list[foundTexture.list[foundListEntry].linkToMaster];
                                Package masterPkg = null;
                                if (cachePackageMgr != null)
                                    masterPkg = cachePackageMgr.OpenPackage(GameData.GamePath + foundMasterTex.path);
                                else
                                    masterPkg = new Package(GameData.GamePath + foundMasterTex.path);
                                int masterExportId = foundMasterTex.exportID;
                                byte[] masterData = masterPkg.getExportData(masterExportId);
                                masterPkg.DisposeCache();
                                using (Texture masterTexture = new Texture(masterPkg, masterExportId, masterData, false))
                                {
                                    if (texture.mipMapsList.Count != masterTexture.mipMapsList.Count)
                                    {
                                        Console.WriteLine("Error: Texture " + package.exportsTable[l].objectName + " in package: " + GameData.packageFiles[i] + " has wrong reference, skipping..." + Environment.NewLine);
                                        if (ipc)
                                        {
                                            Console.WriteLine("[IPC]ERROR Texture " + package.exportsTable[l].objectName + " in package: " + GameData.packageFiles[i] + " has wrong reference, skipping..." + Environment.NewLine);
                                            Console.Out.Flush();
                                        }
                                        goto skip;
                                    }
                                    for (int t = 0; t < texture.mipMapsList.Count; t++)
                                    {
                                        Texture.MipMap mipmap = texture.mipMapsList[t];
                                        if (mipmap.storageType == Texture.StorageTypes.extLZO ||
                                            mipmap.storageType == Texture.StorageTypes.extZlib ||
                                            mipmap.storageType == Texture.StorageTypes.extUnc)
                                        {
                                            mipmap.dataOffset = masterPkg.exportsTable[masterExportId].dataOffset + (uint)masterTexture.properties.propertyEndOffset + masterTexture.mipMapsList[t].internalOffset;
                                            texture.mipMapsList[t] = mipmap;
                                        }
                                    }
                                }
                                if (cachePackageMgr == null)
                                    masterPkg.Dispose();
                            }
skip:
                            using (MemoryStream newData = new MemoryStream())
                            {
                                newData.WriteFromBuffer(texture.properties.toArray());
                                newData.WriteFromBuffer(texture.toArray(package.exportsTable[l].dataOffset + (uint)newData.Position));
                                package.setExportData(l, newData.ToArray());
                            }
                            modified = true;
                        }
                    }
                }
                if (cachePackageMgr == null)
                {
                    if (modified)
                    {
                        if (package.compressed && package.compressionType != Package.CompressionType.Zlib)
                            package.SaveToFile(forceZlib);
                        else
                            package.SaveToFile();
                    }
                    else
                    {
                        package.Dispose();
                        AddMarkerToPackages(GameData.packageFiles[i]);
                    }
                    package.Dispose();
                }
                else
                {
                    package.DisposeCache();
                }
            }
        }

        public void removeMipMapsME2ME3(List<FoundTexture> textures, CachePackageMgr cachePackageMgr,
            bool ipc, bool forceZlib = false)
        {
            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                bool modified = false;
                Package package = null;
                if (ipc)
                {
                    Console.WriteLine("[IPC]OVERALL_PROGRESS " + (i * 100 / GameData.packageFiles.Count));
                    Console.Out.Flush();
                }

                try
                {
                    if (cachePackageMgr != null)
                        package = cachePackageMgr.OpenPackage(GameData.packageFiles[i]);
                    else
                        package = new Package(GameData.packageFiles[i], true);
                }
                catch (Exception e)
                {
                    string err = "";
                    err += "---- Start --------------------------------------------" + Environment.NewLine;
                    err += "Issue with open package file: " + GameData.packageFiles[i] + Environment.NewLine;
                    err += e.Message + Environment.NewLine + Environment.NewLine;
                    err += e.StackTrace + Environment.NewLine + Environment.NewLine;
                    err += "---- End ----------------------------------------------" + Environment.NewLine + Environment.NewLine;
                    Console.WriteLine(err);
                    if (ipc)
                    {
                        Console.WriteLine("[IPC]ERROR Issue with open package file: " + GameData.packageFiles[i]);
                        Console.Out.Flush();
                    }
                    continue;
                }
                
                for (int l = 0; l < package.exportsTable.Count; l++)
                {
                    int id = package.getClassNameId(package.exportsTable[l].classId);
                    if (id == package.nameIdTexture2D ||
                        id == package.nameIdTextureFlipBook)
                    {
                        using (Texture texture = new Texture(package, l, package.getExportData(l), false))
                        {
                            if (!texture.hasImageData() ||
                                !texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty))
                            {
                                continue;
                            }
                            do
                            {
                                texture.mipMapsList.Remove(texture.mipMapsList.First(s => s.storageType == Texture.StorageTypes.empty));
                            } while (texture.mipMapsList.Exists(s => s.storageType == Texture.StorageTypes.empty));
                            texture.properties.setIntValue("SizeX", texture.mipMapsList.First().width);
                            texture.properties.setIntValue("SizeY", texture.mipMapsList.First().height);
                            texture.properties.setIntValue("MipTailBaseIdx", texture.mipMapsList.Count() - 1);

                            using (MemoryStream newData = new MemoryStream())
                            {
                                newData.WriteFromBuffer(texture.properties.toArray());
                                newData.WriteFromBuffer(texture.toArray(package.exportsTable[l].dataOffset + (uint)newData.Position));
                                package.setExportData(l, newData.ToArray());
                            }
                            modified = true;
                        }
                    }
                }
                if (cachePackageMgr == null)
                {
                    if (modified)
                    {
                        if (package.compressed && package.compressionType != Package.CompressionType.Zlib)
                            package.SaveToFile(forceZlib);
                        else
                            package.SaveToFile();
                    }
                    else
                    {
                        package.Dispose();
                        AddMarkerToPackages(GameData.packageFiles[i]);
                    }
                    package.Dispose();
                }
                else
                {
                    package.DisposeCache();
                }
            }
            if (GameData.gameType == MeType.ME3_TYPE)
            {
                TOCBinFile.UpdateAllTOCBinFiles();
            }
        }

    }
}
