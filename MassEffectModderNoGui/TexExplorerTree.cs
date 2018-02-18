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
using System.IO;

namespace MassEffectModder
{
    public partial class TreeScan
    {
        public const uint textureMapBinTag = 0x5054454D;
        public const uint textureMapBinVersion = 2;

        public List<FoundTexture> treeScan = null;
        private bool generateBuiltinMapFiles = false; // change to true to enable map files generation

        public bool PrepareListOfTextures(CachePackageMgr cachePackageMgr, bool ipc)
        {
            treeScan = null;

            List<FoundTexture> textures = new List<FoundTexture>();
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Program.MAINEXENAME);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string filename = Path.Combine(path, "me" + (int)GameData.gameType + "map.bin");
            if (File.Exists(filename))
                File.Delete(filename);

            GameData.packageFiles.Sort();
            int lastProgress = -1;
            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                if (ipc)
                {
                    Console.WriteLine("[IPC]PROCESSING_FILE " + GameData.packageFiles[i]);
                    Console.Out.Flush();
                }
                int newProgress = i * 100 / GameData.packageFiles.Count;
                if (ipc && lastProgress != newProgress)
                {
                    Console.WriteLine("[IPC]OVERALL_PROGRESS " + newProgress);
                    Console.Out.Flush();
                    lastProgress = newProgress;
                }
                FindTextures(textures, GameData.packageFiles[i], cachePackageMgr, ipc);
            }

            if (GameData.gameType == MeType.ME1_TYPE)
            {
                for (int k = 0; k < textures.Count; k++)
                {
                    for (int t = 0; t < textures[k].list.Count; t++)
                    {
                        uint mipmapOffset = textures[k].list[t].mipmapOffset;
                        if (textures[k].list[t].slave)
                        {
                            MatchedTexture slaveTexture = textures[k].list[t];
                            string basePkgName = slaveTexture.basePackageName;
                            if (basePkgName == Path.GetFileNameWithoutExtension(slaveTexture.path).ToUpperInvariant())
                                throw new Exception();
                            for (int j = 0; j < textures[k].list.Count; j++)
                            {
                                if (!textures[k].list[j].slave &&
                                   textures[k].list[j].mipmapOffset == mipmapOffset &&
                                   textures[k].list[j].packageName == basePkgName)
                                {
                                    slaveTexture.linkToMaster = j;
                                    textures[k].list[t] = slaveTexture;
                                    break;
                                }
                            }
                        }
                    }
                    if (!textures[k].list.Exists(s => s.slave) &&
                        textures[k].list.Exists(s => s.weakSlave))
                    {
                        List<MatchedTexture> texList = new List<MatchedTexture>();
                        for (int t = 0; t < textures[k].list.Count; t++)
                        {
                            MatchedTexture tex = textures[k].list[t];
                            if (tex.weakSlave)
                                texList.Add(tex);
                            else
                                texList.Insert(0, tex);
                        }
                        FoundTexture f = textures[k];
                        f.list = texList;
                        textures[k] = f;
                        if (textures[k].list[0].weakSlave)
                            continue;

                        for (int t = 0; t < textures[k].list.Count; t++)
                        {
                            if (textures[k].list[t].weakSlave)
                            {
                                MatchedTexture slaveTexture = textures[k].list[t];
                                string basePkgName = slaveTexture.basePackageName;
                                if (basePkgName == Path.GetFileNameWithoutExtension(slaveTexture.path).ToUpperInvariant())
                                    throw new Exception();
                                for (int j = 0; j < textures[k].list.Count; j++)
                                {
                                    if (!textures[k].list[j].weakSlave &&
                                       textures[k].list[j].packageName == basePkgName)
                                    {
                                        slaveTexture.linkToMaster = j;
                                        textures[k].list[t] = slaveTexture;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                MemoryStream mem = new MemoryStream();
                mem.WriteUInt32(textureMapBinTag);
                mem.WriteUInt32(textureMapBinVersion);
                mem.WriteInt32(textures.Count);
                for (int i = 0; i < textures.Count; i++)
                {
                    mem.WriteInt32(textures[i].name.Length);
                    mem.WriteStringASCII(textures[i].name);
                    mem.WriteUInt32(textures[i].crc);
                    if (generateBuiltinMapFiles)
                    {
                        mem.WriteInt32(textures[i].width);
                        mem.WriteInt32(textures[i].height);
                        mem.WriteInt32((int)textures[i].pixfmt);
                        mem.WriteInt32(textures[i].alphadxt1 ? 1 : 0);
                        mem.WriteInt32(textures[i].numMips);
                    }
                    mem.WriteInt32(textures[i].list.Count);
                    for (int k = 0; k < textures[i].list.Count; k++)
                    {
                        mem.WriteInt32(textures[i].list[k].exportID);
                        mem.WriteInt32(textures[i].list[k].linkToMaster);
                        mem.WriteInt32(textures[i].list[k].path.Length);
                        mem.WriteStringASCII(textures[i].list[k].path);
                    }
                }
                if (!generateBuiltinMapFiles)
                {
                    mem.WriteInt32(GameData.packageFiles.Count);
                    for (int i = 0; i < GameData.packageFiles.Count; i++)
                    {
                        string s = GameData.RelativeGameData(GameData.packageFiles[i]);
                        mem.WriteInt32(s.Length);
                        mem.WriteStringASCII(s);
                    }
                }
                mem.SeekBegin();

                if (generateBuiltinMapFiles)
                {
                    fs.WriteUInt32(0x504D5443);
                    fs.WriteUInt32((uint)mem.Length);
                    byte[] compressed = new ZlibHelper.Zlib().Compress(mem.ToArray(), 9);
                    fs.WriteUInt32((uint)compressed.Length);
                    fs.WriteFromBuffer(compressed);
                }
                else
                {
                    fs.WriteFromStream(mem, mem.Length);
                }
            }

            treeScan = textures;
            return true;
        }

        private void FindTextures(List<FoundTexture> textures, string packagePath, CachePackageMgr cachePackageMgr, bool ipc)
        {
            Package package = null;

            try
            {
                if (cachePackageMgr != null)
                    package = cachePackageMgr.OpenPackage(packagePath);
                else
                    package = new Package(packagePath);
            }
            catch (Exception e)
            {
                if (ipc)
                {
                    Console.WriteLine("[IPC]ERROR Issue opening package file: " + packagePath);
                    Console.Out.Flush();
                }
                else
                {
                    string err = "";
                    err += "---- Start --------------------------------------------" + Environment.NewLine;
                    err += "Issue opening package file: " + packagePath + Environment.NewLine;
                    err += e.Message + Environment.NewLine + Environment.NewLine;
                    err += e.StackTrace + Environment.NewLine + Environment.NewLine;
                    err += "---- End ----------------------------------------------" + Environment.NewLine + Environment.NewLine;
                    Console.WriteLine(err);
                }
                return;
            }
            for (int i = 0; i < package.exportsTable.Count; i++)
            {
                int id = package.getClassNameId(package.exportsTable[i].classId);
                if (id == package.nameIdTexture2D ||
                    id == package.nameIdLightMapTexture2D ||
                    id == package.nameIdShadowMapTexture2D ||
                    id == package.nameIdTextureFlipBook)
                {
                    Texture texture = new Texture(package, i, package.getExportData(i));
                    if (!texture.hasImageData())
                        continue;

                    Texture.MipMap mipmap = texture.getTopMipmap();
                    string name = package.exportsTable[i].objectName;
                    MatchedTexture matchTexture = new MatchedTexture();
                    matchTexture.exportID = i;
                    matchTexture.path = GameData.RelativeGameData(packagePath);
                    matchTexture.packageName = texture.packageName;
                    if (GameData.gameType == MeType.ME1_TYPE)
                    {
                        matchTexture.basePackageName = texture.basePackageName;
                        matchTexture.slave = texture.slave;
                        matchTexture.weakSlave = texture.weakSlave;
                        matchTexture.linkToMaster = -1;
                        if (matchTexture.slave)
                            matchTexture.mipmapOffset = mipmap.dataOffset;
                        else
                            matchTexture.mipmapOffset = package.exportsTable[i].dataOffset + (uint)texture.properties.propertyEndOffset + mipmap.internalOffset;
                    }

                    uint crc = 0;
                    try
                    {
                        crc = texture.getCrcTopMipmap();
                    }
                    catch
                    {
                    }
                    if (crc == 0)
                    {
                        if (ipc)
                        {
                            Console.WriteLine("[IPC]ERROR Texture " + package.exportsTable[i].objectName + " is broken in package: " + packagePath + ", skipping...");
                            Console.Out.Flush();
                        }
                        else
                        {
                            Console.WriteLine("Error: Texture " + package.exportsTable[i].objectName + " is broken in package: " + packagePath + ", skipping..." + Environment.NewLine);
                        }
                        continue;
                    }

                    FoundTexture foundTexName = textures.Find(s => s.crc == crc);
                    if (foundTexName.crc != 0)
                    {
                        if (matchTexture.slave || GameData.gameType != MeType.ME1_TYPE)
                            foundTexName.list.Add(matchTexture);
                        else
                            foundTexName.list.Insert(0, matchTexture);
                    }
                    else
                    {
                        FoundTexture foundTex = new FoundTexture();
                        foundTex.list = new List<MatchedTexture>();
                        foundTex.list.Add(matchTexture);
                        foundTex.name = name;
                        foundTex.crc = crc;
                        if (generateBuiltinMapFiles)
                        {
                            foundTex.width = texture.getTopMipmap().width;
                            foundTex.height = texture.getTopMipmap().height;
                            foundTex.pixfmt = Image.getEngineFormatType(texture.properties.getProperty("Format").valueName);
                            if (foundTex.pixfmt == PixelFormat.DXT1 &&
                                texture.properties.exists("CompressionSettings") &&
                                texture.properties.getProperty("CompressionSettings").valueName == "TC_OneBitAlpha")
                            {
                                foundTex.alphadxt1 = true;
                            }
                        }
                        textures.Add(foundTex);
                    }
                }
            }

            if (cachePackageMgr == null)
            {
                package.Dispose();
            }
            else
            {
                package.DisposeCache();
            }
        }
    }
}
