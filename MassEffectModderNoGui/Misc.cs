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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace MassEffectModder
{
    public enum MeType
    {
        ME1_TYPE = 1,
        ME2_TYPE,
        ME3_TYPE
    }

    struct TFCTexture
    {
        public byte[] guid;
        public string name;
    }

    struct TextureGroup
    {
        public string name;
        public int value;
    }

    public struct MatchedTexture
    {
        public int exportID;
        public string packageName; // only used while texture scan for ME1
        public string basePackageName; // only used while texture scan for ME1
        public bool weakSlave;
        public bool slave;
        public string path;
        public int linkToMaster;
        public uint mipmapOffset;
        public List<uint> crcs;
    }

    public struct FoundTexture
    {
        public string name;
        public uint crc;
        public List<MatchedTexture> list;
        public PixelFormat pixfmt;
        public bool alphadxt1;
        public int width, height;
        public int numMips;
    }

    static class LODSettings
    {
        static public void readLOD(MeType gameId, ConfIni engineConf, ref string log)
        {
            if (gameId == MeType.ME1_TYPE)
            {
                log += "TEXTUREGROUP_World=" + engineConf.Read("TEXTUREGROUP_World", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_WorldNormalMap=" + engineConf.Read("TEXTUREGROUP_WorldNormalMap", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_AmbientLightMap=" + engineConf.Read("TEXTUREGROUP_AmbientLightMap", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_LightAndShadowMap=" + engineConf.Read("TEXTUREGROUP_LightAndShadowMap", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_64=" + engineConf.Read("TEXTUREGROUP_Environment_64", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_128=" + engineConf.Read("TEXTUREGROUP_Environment_128", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_256=" + engineConf.Read("TEXTUREGROUP_Environment_256", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_512=" + engineConf.Read("TEXTUREGROUP_Environment_512", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_1024=" + engineConf.Read("TEXTUREGROUP_Environment_1024", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_64=" + engineConf.Read("TEXTUREGROUP_VFX_64", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_128=" + engineConf.Read("TEXTUREGROUP_VFX_128", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_256=" + engineConf.Read("TEXTUREGROUP_VFX_256", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_512" + engineConf.Read("TEXTUREGROUP_VFX_512", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_1024=" + engineConf.Read("TEXTUREGROUP_VFX_1024", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_128=" + engineConf.Read("TEXTUREGROUP_APL_128", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_256=" + engineConf.Read("TEXTUREGROUP_APL_256", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_512=" + engineConf.Read("TEXTUREGROUP_APL_512", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_1024=" + engineConf.Read("TEXTUREGROUP_APL_1024", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_GUI=" + engineConf.Read("TEXTUREGROUP_GUI", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Promotional=" + engineConf.Read("TEXTUREGROUP_Promotional", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_1024=" + engineConf.Read("TEXTUREGROUP_Character_1024", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Diff=" + engineConf.Read("TEXTUREGROUP_Character_Diff", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Norm=" + engineConf.Read("TEXTUREGROUP_Character_Norm", "TextureLODSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Spec=" + engineConf.Read("TEXTUREGROUP_Character_Spec", "TextureLODSettings") + Environment.NewLine;
            }
            else if (gameId == MeType.ME2_TYPE)
            {
                log += "TEXTUREGROUP_World=" + engineConf.Read("TEXTUREGROUP_World", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_WorldNormalMap=" + engineConf.Read("TEXTUREGROUP_WorldNormalMap", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_AmbientLightMap=" + engineConf.Read("TEXTUREGROUP_AmbientLightMap", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_LightAndShadowMap=" + engineConf.Read("TEXTUREGROUP_LightAndShadowMap", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_RenderTarget=" + engineConf.Read("TEXTUREGROUP_RenderTarget", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_64=" + engineConf.Read("TEXTUREGROUP_Environment_64", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_128=" + engineConf.Read("TEXTUREGROUP_Environment_128", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_256=" + engineConf.Read("TEXTUREGROUP_Environment_256", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_512=" + engineConf.Read("TEXTUREGROUP_Environment_512", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_1024=" + engineConf.Read("TEXTUREGROUP_Environment_1024", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_64=" + engineConf.Read("TEXTUREGROUP_VFX_64", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_128=" + engineConf.Read("TEXTUREGROUP_VFX_128", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_256=" + engineConf.Read("TEXTUREGROUP_VFX_256", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_512=" + engineConf.Read("TEXTUREGROUP_VFX_512", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_1024=" + engineConf.Read("TEXTUREGROUP_VFX_1024", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_128=" + engineConf.Read("TEXTUREGROUP_APL_128", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_256=" + engineConf.Read("TEXTUREGROUP_APL_256", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_512=" + engineConf.Read("TEXTUREGROUP_APL_512", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_1024=" + engineConf.Read("TEXTUREGROUP_APL_1024", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_UI=" + engineConf.Read("TEXTUREGROUP_UI", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Promotional=" + engineConf.Read("TEXTUREGROUP_Promotional", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_1024=" + engineConf.Read("TEXTUREGROUP_Character_1024", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Diff=" + engineConf.Read("TEXTUREGROUP_Character_Diff", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Norm=" + engineConf.Read("TEXTUREGROUP_Character_Norm", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Spec=" + engineConf.Read("TEXTUREGROUP_Character_Spec", "SystemSettings") + Environment.NewLine;
            }
            else if (gameId == MeType.ME3_TYPE)
            {
                log += "TEXTUREGROUP_World=" + engineConf.Read("TEXTUREGROUP_World", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_WorldSpecular=" + engineConf.Read("TEXTUREGROUP_WorldSpecular", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_WorldNormalMap=" + engineConf.Read("TEXTUREGROUP_WorldNormalMap", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_AmbientLightMap=" + engineConf.Read("TEXTUREGROUP_AmbientLightMap", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_LightAndShadowMap=" + engineConf.Read("TEXTUREGROUP_LightAndShadowMap", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_RenderTarget=" + engineConf.Read("TEXTUREGROUP_RenderTarget", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_64=" + engineConf.Read("TEXTUREGROUP_Environment_64", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_128=" + engineConf.Read("TEXTUREGROUP_Environment_128", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_256=" + engineConf.Read("TEXTUREGROUP_Environment_256", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_512=" + engineConf.Read("TEXTUREGROUP_Environment_512", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Environment_1024=" + engineConf.Read("TEXTUREGROUP_Environment_1024", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_64=" + engineConf.Read("TEXTUREGROUP_VFX_64", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_128=" + engineConf.Read("TEXTUREGROUP_VFX_128", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_256=" + engineConf.Read("TEXTUREGROUP_VFX_256", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_512=" + engineConf.Read("TEXTUREGROUP_VFX_512", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_VFX_1024=" + engineConf.Read("TEXTUREGROUP_VFX_1024", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_128=" + engineConf.Read("TEXTUREGROUP_APL_128", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_256=" + engineConf.Read("TEXTUREGROUP_APL_256", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_512=" + engineConf.Read("TEXTUREGROUP_APL_512", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_APL_1024=" + engineConf.Read("TEXTUREGROUP_APL_1024", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_UI=" + engineConf.Read("TEXTUREGROUP_UI", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Promotional=" + engineConf.Read("TEXTUREGROUP_Promotional", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_1024=" + engineConf.Read("TEXTUREGROUP_Character_1024", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Diff=" + engineConf.Read("TEXTUREGROUP_Character_Diff", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Norm=" + engineConf.Read("TEXTUREGROUP_Character_Norm", "SystemSettings") + Environment.NewLine;
                log += "TEXTUREGROUP_Character_Spec=" + engineConf.Read("TEXTUREGROUP_Character_Spec", "SystemSettings") + Environment.NewLine;
            }
            else
            {
                throw new Exception("");
            }
        }

        static public void updateLOD(MeType gameId, ConfIni engineConf, bool limitME1Lods = false)
        {
            if (gameId == MeType.ME1_TYPE)
            {
                engineConf.Write("TEXTUREGROUP_World", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_WorldNormalMap", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_AmbientLightMap", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_LightAndShadowMap", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_64", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_128", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_256", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_512", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_1024", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_64", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_128", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_256", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_512", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_1024", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_128", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_256", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_512", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_1024", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_GUI", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Promotional", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                if (limitME1Lods)
                {
                    engineConf.Write("TEXTUREGROUP_Character_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                    engineConf.Write("TEXTUREGROUP_Character_Diff", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                    engineConf.Write("TEXTUREGROUP_Character_Norm", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                    engineConf.Write("TEXTUREGROUP_Character_Spec", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                }
                else
                {
                    engineConf.Write("TEXTUREGROUP_Character_1024", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                    engineConf.Write("TEXTUREGROUP_Character_Diff", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                    engineConf.Write("TEXTUREGROUP_Character_Norm", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                    engineConf.Write("TEXTUREGROUP_Character_Spec", "(MinLODSize=4096,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                }
            }
            else if (gameId == MeType.ME2_TYPE)
            {
                engineConf.Write("TEXTUREGROUP_World", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_WorldNormalMap", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_AmbientLightMap", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_LightAndShadowMap", "(MinLODSize=1024,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_RenderTarget", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_64", "(MinLODSize=128,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_64", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_128", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_256", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_512", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_1024", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_UI", "(MinLODSize=64,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Promotional", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Diff", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Norm", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Spec", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
            }
            else if (gameId == MeType.ME3_TYPE)
            {
                engineConf.Write("TEXTUREGROUP_World", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_WorldSpecular", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_WorldNormalMap", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_AmbientLightMap", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_LightAndShadowMap", "(MinLODSize=1024,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_RenderTarget", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_64", "(MinLODSize=128,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Environment_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_64", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_128", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_256", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_512", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_VFX_1024", "(MinLODSize=32,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_128", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_256", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_512", "(MinLODSize=1024,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_APL_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_UI", "(MinLODSize=64,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Promotional", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_1024", "(MinLODSize=2048,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Diff", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Norm", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
                engineConf.Write("TEXTUREGROUP_Character_Spec", "(MinLODSize=512,MaxLODSize=4096,LODBias=0)", "SystemSettings");
            }
            else
            {
                throw new Exception("");
            }
        }

        static public void removeLOD(MeType gameId, ConfIni engineConf)
        {
            if (gameId == MeType.ME1_TYPE)
            {
                engineConf.Write("TEXTUREGROUP_World", "(MinLODSize=16,MaxLODSize=4096,LODBias=2)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_WorldNormalMap", "(MinLODSize=16,MaxLODSize=4096,LODBias=2)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_AmbientLightMap", "(MinLODSize=32,MaxLODSize=512,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_LightAndShadowMap", "(MinLODSize=256,MaxLODSize=4096,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_64", "(MinLODSize=32,MaxLODSize=64,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_128", "(MinLODSize=32,MaxLODSize=128,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_256", "(MinLODSize=32,MaxLODSize=256,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_512", "(MinLODSize=32,MaxLODSize=512,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Environment_1024", "(MinLODSize=32,MaxLODSize=1024,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_64", "(MinLODSize=8,MaxLODSize=64,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_128", "(MinLODSize=8,MaxLODSize=128,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_256", "(MinLODSize=8,MaxLODSize=256,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_512", "(MinLODSize=8,MaxLODSize=512,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_VFX_1024", "(MinLODSize=8,MaxLODSize=1024,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_128", "(MinLODSize=32,MaxLODSize=128,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_256", "(MinLODSize=32,MaxLODSize=256,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_512", "(MinLODSize=32,MaxLODSize=512,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_APL_1024", "(MinLODSize=32,MaxLODSize=1024,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_GUI", "(MinLODSize=8,MaxLODSize=1024,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Promotional", "(MinLODSize=32,MaxLODSize=2048,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_1024", "(MinLODSize=32,MaxLODSize=1024,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_Diff", "(MinLODSize=32,MaxLODSize=512,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_Norm", "(MinLODSize=32,MaxLODSize=512,LODBias=0)", "TextureLODSettings");
                engineConf.Write("TEXTUREGROUP_Character_Spec", "(MinLODSize=32,MaxLODSize=256,LODBias=0)", "TextureLODSettings");
            }
            else if (gameId == MeType.ME2_TYPE)
            {
                engineConf.DeleteKey("TEXTUREGROUP_World", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_WorldNormalMap", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_AmbientLightMap", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_LightAndShadowMap", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_RenderTarget", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_64", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_128", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_256", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_512", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_1024", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_64", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_128", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_256", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_512", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_1024", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_128", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_256", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_512", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_1024", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_UI", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Promotional", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_1024", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_Diff", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_Norm", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_Spec", "SystemSettings");
            }
            else if (gameId == MeType.ME3_TYPE)
            {
                engineConf.DeleteKey("TEXTUREGROUP_World", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_WorldSpecular", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_WorldNormalMap", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_AmbientLightMap", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_LightAndShadowMap", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_RenderTarget", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_64", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_128", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_256", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_512", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Environment_1024", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_64", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_128", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_256", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_512", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_VFX_1024", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_128", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_256", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_512", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_APL_1024", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_UI", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Promotional", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_1024", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_Diff", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_Norm", "SystemSettings");
                engineConf.DeleteKey("TEXTUREGROUP_Character_Spec", "SystemSettings");
            }
            else
            {
                throw new Exception("");
            }
        }

        static public void updateGFXSettings(MeType gameId, ConfIni engineConf, bool softShadowsME1, bool meuitmMode)
        {
            if (gameId == MeType.ME1_TYPE)
            {
                engineConf.Write("MaxShadowResolution", "2048", "Engine.Engine");
                engineConf.Write("MaxShadowResolution", "2048", "Engine.GameEngine");
                if (softShadowsME1)
                {
                    engineConf.Write("MinShadowResolution", "16", "Engine.Engine");
                    engineConf.Write("MinShadowResolution", "16", "Engine.GameEngine");
                }
                else
                {
                    engineConf.Write("MinShadowResolution", "64", "Engine.Engine");
                    engineConf.Write("MinShadowResolution", "64", "Engine.GameEngine");
                }
                engineConf.Write("DynamicShadows", "True", "SystemSettings");
                engineConf.Write("EnableDynamicShadows", "True", "WinDrv.WindowsClient");
                if (softShadowsME1 && meuitmMode)
                {
                    engineConf.Write("DepthBias", "0.006000", "Engine.Engine");
                    engineConf.Write("DepthBias", "0.006000", "Engine.GameEngine");
                }
                else
                {
                    engineConf.Write("DepthBias", "0.030000", "Engine.Engine");
                    engineConf.Write("DepthBias", "0.030000", "Engine.GameEngine");
                }
                engineConf.Write("ShadowFilterQualityBias", "2", "SystemSettings");
                if (softShadowsME1)
                {
                    engineConf.Write("ShadowFilterRadius", "2", "Engine.Engine");
                    engineConf.Write("ShadowFilterRadius", "2", "Engine.GameEngine");
                }
                else
                {
                    engineConf.Write("ShadowFilterRadius", "5", "Engine.Engine");
                    engineConf.Write("ShadowFilterRadius", "5", "Engine.GameEngine");
                }
                engineConf.Write("bEnableBranchingPCFShadows", "True", "Engine.Engine");
                engineConf.Write("bEnableBranchingPCFShadows", "True", "Engine.GameEngine");
                engineConf.Write("MaxAnisotropy", "16", "SystemSettings");
                engineConf.Write("TextureLODLevel", "3", "WinDrv.WindowsClient");
                engineConf.Write("FilterLevel", "2", "WinDrv.WindowsClient");
                engineConf.Write("Trilinear", "True", "SystemSettings");
                engineConf.Write("MotionBlur", "True", "SystemSettings");
                engineConf.Write("DepthOfField", "True", "SystemSettings");
                engineConf.Write("Bloom", "True", "SystemSettings");
                engineConf.Write("QualityBloom", "True", "SystemSettings");
                engineConf.Write("ParticleLODBias", "0", "SystemSettings");
                engineConf.Write("SkeletalMeshLODBias", "0", "SystemSettings");
                engineConf.Write("DetailMode", "2", "SystemSettings");
                engineConf.Write("PoolSize", "1536", "TextureStreaming");
                engineConf.Write("MinTimeToGuaranteeMinMipCount", "0", "TextureStreaming");
                engineConf.Write("MaxTimeToGuaranteeMinMipCount", "0", "TextureStreaming");
            }
            else if (gameId == MeType.ME2_TYPE)
            {
                engineConf.Write("MaxShadowResolution", "4096", "SystemSettings");
                engineConf.Write("MinShadowResolution", "64", "SystemSettings");
                engineConf.Write("ShadowFilterQualityBias", "2", "SystemSettings");
                engineConf.Write("ShadowFilterRadius", "5", "SystemSettings");
                engineConf.Write("bEnableBranchingPCFShadows", "True", "SystemSettings");
                engineConf.Write("MaxAnisotropy", "16", "SystemSettings");
                engineConf.Write("Trilinear", "True", "SystemSettings");
                engineConf.Write("MotionBlur", "True", "SystemSettings");
                engineConf.Write("DepthOfField", "True", "SystemSettings");
                engineConf.Write("Bloom", "True", "SystemSettings");
                engineConf.Write("QualityBloom", "True", "SystemSettings");
                engineConf.Write("ParticleLODBias", "0", "SystemSettings");
                engineConf.Write("SkeletalMeshLODBias", "0", "SystemSettings");
                engineConf.Write("DetailMode", "2", "SystemSettings");
            }
            else if (gameId == MeType.ME3_TYPE)
            {
                engineConf.Write("MaxShadowResolution", "4096", "SystemSettings");
                engineConf.Write("MinShadowResolution", "64", "SystemSettings");
                engineConf.Write("ShadowFilterQualityBias", "2", "SystemSettings");
                engineConf.Write("ShadowFilterRadius", "5", "SystemSettings");
                engineConf.Write("bEnableBranchingPCFShadows", "True", "SystemSettings");
                engineConf.Write("MaxAnisotropy", "16", "SystemSettings");
                engineConf.Write("MotionBlur", "True", "SystemSettings");
                engineConf.Write("DepthOfField", "True", "SystemSettings");
                engineConf.Write("Bloom", "True", "SystemSettings");
                engineConf.Write("QualityBloom", "True", "SystemSettings");
                engineConf.Write("ParticleLODBias", "0", "SystemSettings");
                engineConf.Write("SkeletalMeshLODBias", "0", "SystemSettings");
                engineConf.Write("DetailMode", "2", "SystemSettings");
            }
            else
            {
                throw new Exception("");
            }

        }
    }

    static partial class Misc
    {
        public static bool generateModsMd5Entries = false;

        public struct MD5FileEntry
        {
            public string path;
            public byte[] md5;
        }

        public struct MD5ModFileEntry
        {
            public string path;
            public byte[] md5;
            public string modName;
        }

        static public bool VerifyME1Exe(GameData gameData)
        {
            if (File.Exists(GameData.GameExePath))
            {
                using (FileStream fs = new FileStream(GameData.GameExePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    fs.JumpTo(0x3C); // jump to offset of COFF header
                    uint offset = fs.ReadUInt32() + 4; // skip PE signature too
                    fs.JumpTo(offset + 0x12); // jump to flags entry
                    ushort flag = fs.ReadUInt16(); // read flags
                    if ((flag & 0x20) != 0x20) // check for LAA flag
                    {
                        Console.WriteLine("Patching ME1 for LAA: " + GameData.GameExePath);
                        flag |= 0x20;
                        fs.Skip(-2);
                        fs.WriteUInt16(flag); // write LAA flag
                    }
                    else
                    {
                        Console.WriteLine("File already has LAA flag enabled: " + GameData.GameExePath);
                    }
                }
                // search for "ProductName Mass Effect"
                byte[] pattern = { 0x50, 0, 0x72, 0, 0x6F, 0, 0x64, 0, 0x75, 0, 0x63, 0, 0x74, 0, 0x4E, 0, 0x61, 0, 0x6D, 0, 0x65, 0, 0, 0, 0, 0,
                                   0x4D, 0, 0x61, 0, 0x73, 0, 0x73, 0, 0x20, 0, 0x45, 0, 0x66, 0, 0x66, 0, 0x65, 0, 0x63, 0, 0x74, 0 };
                byte[] buffer = File.ReadAllBytes(GameData.GameExePath);
                int pos = -1;
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] == pattern[0])
                    {
                        bool found = true;
                        for (int l = 1; l < pattern.Length; l++)
                        {
                            if (buffer[i + l] != pattern[l])
                            {
                                found = false;
                                break;
                            }
                        }
                        if (found)
                        {
                            pos = i;
                            break;
                        }
                    }
                }
                if (pos != -1)
                {
                    // replace to "Mass_Effect"
                    buffer[pos + 34] = 0x5f;
                    File.WriteAllBytes(GameData.GameExePath, buffer);
                    Console.WriteLine("Patching ME1 for Product Name: " + GameData.GameExePath);
                }
                else
                {
                    Console.WriteLine("Specific Product Name not found or already changed: " + GameData.GameExePath);
                }
                return true;
            }
            else
            {
                Console.WriteLine("File not found: " + GameData.GameExePath);
                return false;
            }
        }

        static public FoundTexture ParseLegacyMe3xScriptMod(List<FoundTexture> textures, string script, string textureName)
        {
            Regex parts = new Regex("pccs.Add[(]\"[A-z,0-9/,..]*\"");
            Match match = parts.Match(script);
            if (match.Success)
            {
                string packageName = match.ToString().Replace('/', '\\').Split('\"')[1].Split('\\').Last().Split('.')[0].ToLowerInvariant();
                parts = new Regex("IDs.Add[(][0-9]*[)];");
                match = parts.Match(script);
                if (match.Success)
                {
                    int exportId = int.Parse(match.ToString().Split('(')[1].Split(')')[0]);
                    if (exportId != 0)
                    {
                        textureName = textureName.ToLowerInvariant();
                        for (int i = 0; i < textures.Count; i++)
                        {
                            if (textures[i].name.ToLowerInvariant() == textureName)
                            {
                                for (int l = 0; l < textures[i].list.Count; l++)
                                {
                                    if (textures[i].list[l].exportID == exportId)
                                    {
                                        string pkg = textures[i].list[l].path.Split('\\').Last().Split('.')[0].ToLowerInvariant();
                                        if (pkg == packageName)
                                        {
                                            return textures[i];
                                        }
                                    }
                                }
                            }
                        }
                        // search again but without name match
                        for (int i = 0; i < textures.Count; i++)
                        {
                            for (int l = 0; l < textures[i].list.Count; l++)
                            {
                                if (textures[i].list[l].exportID == exportId)
                                {
                                    string pkg = textures[i].list[l].path.Split('\\').Last().Split('.')[0].ToLowerInvariant();
                                    if (pkg == packageName)
                                    {
                                        return textures[i];
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    textureName = textureName.ToLowerInvariant();
                    for (int i = 0; i < textures.Count; i++)
                    {
                        if (textures[i].name.ToLowerInvariant() == textureName)
                        {
                            for (int l = 0; l < textures[i].list.Count; l++)
                            {
                                string pkg = textures[i].list[l].path.Split('\\').Last().Split('.')[0].ToLowerInvariant();
                                if (pkg == packageName)
                                {
                                    return textures[i];
                                }
                            }
                        }
                    }
                }
            }

            return new FoundTexture();
        }

        static public void ParseME3xBinaryScriptMod(string script, ref string package, ref int expId, ref string path)
        {
            Regex parts = new Regex("int objidx = [0-9]*");
            Match match = parts.Match(script);
            if (match.Success)
            {
                expId = int.Parse(match.ToString().Split(' ').Last());

                parts = new Regex("string filename = \"[A-z,0-9,.]*\";");
                match = parts.Match(script);
                if (match.Success)
                {
                    package = match.ToString().Split('\"')[1].Replace("\\\\", "\\");

                    parts = new Regex("string pathtarget = ME3Directory.cookedPath;");
                    match = parts.Match(script);
                    if (match.Success)
                    {
                        path = @"\BioGame\CookedPCConsole";
                        return;
                    }
                    else
                    {
                        parts = new Regex("string pathtarget = Path.GetDirectoryName[(]ME3Directory[.]cookedPath[)];");
                        match = parts.Match(script);
                        if (match.Success)
                        {
                            path = @"\BioGame";
                            return;
                        }
                        else
                        {
                            parts = new Regex("string pathtarget = new DirectoryInfo[(]ME3Directory[.]cookedPath[)][.]Parent.FullName [+] \"[A-z,0-9,_,.]*\";");
                            match = parts.Match(script);
                            if (match.Success)
                            {
                                path = Path.GetDirectoryName(@"\BioGame\" + match.ToString().Split('\"')[1]);
                                return;
                            }
                        }
                    }
                }
            }
        }

        static public byte[] calculateSHA1(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (SHA1 sha1 = SHA1.Create())
                {
                    sha1.Initialize();
                    return sha1.ComputeHash(fs);
                }
            }
        }

        static public byte[] calculateMD5(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (MD5 md5 = MD5.Create())
                {
                    md5.Initialize();
                    return md5.ComputeHash(fs);
                }
            }
        }

        static public List<string> detectBrokenMod(MeType gameType)
        {
            List<string> mods = new List<string>();

            for (int l = 0; l < badMOD.Count(); l++)
            {
                if (!File.Exists(GameData.GamePath + badMOD[l].path))
                    continue;
                byte[] md5 = calculateMD5(GameData.GamePath + badMOD[l].path);
                if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, badMOD[l].md5))
                {
                    if (!mods.Exists(s => s == badMOD[l].modName))
                        mods.Add(badMOD[l].modName);
                }
            }

            return mods;
        }

        static public List<string> detectMods(MeType gameType)
        {
            List<string> mods = new List<string>();

            for (int l = 0; l < modsEntries.Count(); l++)
            {
                if (!File.Exists(GameData.GamePath + modsEntries[l].path))
                    continue;
                byte[] md5 = calculateMD5(GameData.GamePath + modsEntries[l].path);
                if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, modsEntries[l].md5))
                {
                    if (!mods.Exists(s => s == modsEntries[l].modName))
                        mods.Add(modsEntries[l].modName);
                }
            }

            return mods;
        }

        static public bool checkGameFiles(MeType gameType, ref string errors, ref List<string> mods,
            bool ipc = false, bool withoutSfars = false, bool onlyVanilla = false, bool backupMode = false, bool generateMd5Entries = false)
        {
            bool vanilla = true;
            List<string> packageMainFiles = null;
            List<string> packageDLCFiles = null;
            List<string> sfarFiles = null;
            List<string> tfcFiles = null;
            MD5FileEntry[] entries = null;

            if (gameType == MeType.ME1_TYPE)
            {
                packageMainFiles = Directory.GetFiles(GameData.MainData, "*.*",
                SearchOption.AllDirectories).Where(s => s.EndsWith(".upk",
                    StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".u", StringComparison.OrdinalIgnoreCase) ||
                    s.EndsWith(".sfm", StringComparison.OrdinalIgnoreCase)).ToList();
                if (Directory.Exists(GameData.DLCData))
                {
                    packageDLCFiles = Directory.GetFiles(GameData.DLCData, "*.*",
                    SearchOption.AllDirectories).Where(s => s.EndsWith(".upk",
                        StringComparison.OrdinalIgnoreCase) ||
                        s.EndsWith(".u", StringComparison.OrdinalIgnoreCase) ||
                        s.EndsWith(".sfm", StringComparison.OrdinalIgnoreCase)).ToList();
                }
                packageMainFiles.RemoveAll(s => s.ToLowerInvariant().Contains("localshadercache-pc-d3d-sm3.upk"));
                packageMainFiles.RemoveAll(s => s.ToLowerInvariant().Contains("refshadercache-pc-d3d-sm3.upk"));
                entries = Program.entriesME1;
            }
            else if (gameType == MeType.ME2_TYPE)
            {
                packageMainFiles = Directory.GetFiles(GameData.MainData, "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)).ToList();
                tfcFiles = Directory.GetFiles(GameData.MainData, "*.tfc", SearchOption.AllDirectories).Where(item => item.EndsWith(".tfc", StringComparison.OrdinalIgnoreCase)).ToList();
                if (Directory.Exists(GameData.DLCData))
                {
                    packageDLCFiles = Directory.GetFiles(GameData.DLCData, "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)).ToList();
                    tfcFiles.AddRange(Directory.GetFiles(GameData.DLCData, "*.tfc", SearchOption.AllDirectories).Where(item => item.EndsWith(".tfc", StringComparison.OrdinalIgnoreCase)).ToList());
                }
                entries = Program.entriesME2;
            }
            else if (gameType == MeType.ME3_TYPE)
            {
                packageMainFiles = Directory.GetFiles(GameData.MainData, "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)).ToList();
                tfcFiles = Directory.GetFiles(GameData.MainData, "*.tfc", SearchOption.AllDirectories).Where(item => item.EndsWith(".tfc", StringComparison.OrdinalIgnoreCase)).ToList();
                if (Directory.Exists(GameData.DLCData))
                {
                    packageDLCFiles = Directory.GetFiles(GameData.DLCData, "*.pcc", SearchOption.AllDirectories).Where(item => item.EndsWith(".pcc", StringComparison.OrdinalIgnoreCase)).ToList();
                    sfarFiles = Directory.GetFiles(GameData.DLCData, "Default.sfar", SearchOption.AllDirectories).ToList();
                    for (int i = 0; i < sfarFiles.Count; i++)
                    {
                        if (File.Exists(Path.Combine(Path.GetDirectoryName(sfarFiles[i]), "Mount.dlc")))
                            sfarFiles.RemoveAt(i--);
                    }
                    sfarFiles.Add(GameData.bioGamePath + "\\Patches\\PCConsole\\Patch_001.sfar");
                    packageDLCFiles.RemoveAll(s => s.ToLowerInvariant().Contains("guidcache"));
                    tfcFiles.AddRange(Directory.GetFiles(GameData.DLCData, "*.tfc", SearchOption.AllDirectories).Where(item => item.EndsWith(".tfc", StringComparison.OrdinalIgnoreCase)).ToList());
                }
                packageMainFiles.RemoveAll(s => s.ToLowerInvariant().Contains("guidcache"));
                entries = Program.entriesME3;
            }

            packageMainFiles.Sort();
            int allFilesCount = packageMainFiles.Count();
            int progress = 0;
            if (packageDLCFiles != null)
            {
                packageDLCFiles.Sort();
                allFilesCount += packageDLCFiles.Count();
            }
            if (sfarFiles != null && !withoutSfars)
            {
                sfarFiles.Sort();
                allFilesCount += sfarFiles.Count();
            }
            if (tfcFiles != null)
            {
                tfcFiles.Sort();
                allFilesCount += tfcFiles.Count();
            }

            mods.Clear();
            FileStream fs = null;
            if (generateMd5Entries)
                fs = new FileStream("MD5ModFileEntry" + (int)gameType + ".cs", FileMode.Create, FileAccess.Write);

            for (int l = 0; l < packageMainFiles.Count; l++)
            {
                if (ipc)
                {
                    Console.WriteLine("[IPC]PROCESSING_FILE " + packageMainFiles[l]);
                    Console.WriteLine("[IPC]OVERALL_PROGRESS " + ((l + progress) * 100 / allFilesCount));
                    Console.Out.Flush();
                }
                byte[] md5 = calculateMD5(packageMainFiles[l]);
                bool found = false;
                for (int p = 0; p < entries.Count(); p++)
                {
                    if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, entries[p].md5))
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                    continue;

                if (!onlyVanilla)
                {
                    found = false;
                    for (int p = 0; p < modsEntries.Count(); p++)
                    {
                        if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, modsEntries[p].md5))
                        {
                            found = true;
                            if (!mods.Exists(s => s == modsEntries[p].modName))
                                mods.Add(modsEntries[p].modName);
                            break;
                        }
                    }
                    if (found)
                        continue;

                    found = false;
                    for (int p = 0; p < badMOD.Count(); p++)
                    {
                        if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, badMOD[p].md5))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        continue;
                }

                int index = -1;
                for (int p = 0; p < entries.Count(); p++)
                {
                    if (GameData.RelativeGameData(packageMainFiles[l]).ToLowerInvariant() == entries[p].path.ToLowerInvariant())
                    {
                        index = p;
                        break;
                    }
                }
                if (index == -1 && !backupMode)
                    continue;

                vanilla = false;

                if (generateMd5Entries)
                {
                    fs.WriteStringASCII("new MD5ModFileEntry\n{\npath = @\"" + GameData.RelativeGameData(packageMainFiles[l]) + "\",\nmd5 = new byte[] { ");
                    for (int i = 0; i < md5.Length; i++)
                    {
                        fs.WriteStringASCII(string.Format("0x{0:X2}, ", md5[i]));
                    }
                    fs.WriteStringASCII("},\nmodName = \"\",\n},\n");
                }

                if (!backupMode)
                    errors += "File " + packageMainFiles[l] + " has wrong MD5 checksum: ";
                else
                    errors += "File " + packageMainFiles[l] + " not found in database, MD5 checksum: ";
                for (int i = 0; i < md5.Count(); i++)
                {
                    errors += string.Format("{0:x2}", md5[i]);
                }
                if (!backupMode)
                {
                    errors += "\n, expected: ";
                    for (int i = 0; i < entries[index].md5.Count(); i++)
                    {
                        errors += string.Format("{0:x2}", entries[index].md5[i]);
                    }
                }
                errors += Environment.NewLine;

                if (ipc)
                {
                    Console.WriteLine("[IPC]ERROR " + packageMainFiles[l]);
                    Console.Out.Flush();
                }
            }
            progress += packageMainFiles.Count();

            if (packageDLCFiles != null)
            {
                for (int l = 0; l < packageDLCFiles.Count; l++)
                {
                    if (ipc)
                    {
                        Console.WriteLine("[IPC]PROCESSING_FILE " + packageDLCFiles[l]);
                        Console.WriteLine("[IPC]OVERALL_PROGRESS " + ((l + progress) * 100 / allFilesCount));
                        Console.Out.Flush();
                    }
                    byte[] md5 = calculateMD5(packageDLCFiles[l]);
                    bool found = false;
                    for (int p = 0; p < entries.Count(); p++)
                    {
                        if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, entries[p].md5))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        continue;

                    if (!onlyVanilla)
                    {
                        found = false;
                        for (int p = 0; p < modsEntries.Count(); p++)
                        {
                            if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, modsEntries[p].md5))
                            {
                                found = true;
                                if (!mods.Exists(s => s == modsEntries[p].modName))
                                    mods.Add(modsEntries[p].modName);
                                break;
                            }
                        }
                        if (found)
                            continue;

                        found = false;
                        for (int p = 0; p < badMOD.Count(); p++)
                        {
                            if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, badMOD[p].md5))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found)
                            continue;
                    }

                    int index = -1;
                    for (int p = 0; p < entries.Count(); p++)
                    {
                        if (GameData.RelativeGameData(packageDLCFiles[l]).ToLowerInvariant() == entries[p].path.ToLowerInvariant())
                        {
                            index = p;
                            break;
                        }
                    }
                    if (index == -1 && !backupMode)
                        continue;

                    vanilla = false;

                    if (generateMd5Entries)
                    {
                        fs.WriteStringASCII("new MD5ModFileEntry\n{\npath = @\"" + GameData.RelativeGameData(packageDLCFiles[l]) + "\",\nmd5 = new byte[] { ");
                        for (int i = 0; i < md5.Length; i++)
                        {
                            fs.WriteStringASCII(string.Format("0x{0:X2}, ", md5[i]));
                        }
                        fs.WriteStringASCII("},\nmodName = \"\",\n},\n");
                    }

                    if (!backupMode)
                        errors += "File " + packageDLCFiles[l] + " has wrong MD5 checksum: ";
                    else
                        errors += "File " + packageDLCFiles[l] + " not found in database, MD5 checksum: ";
                    for (int i = 0; i < md5.Count(); i++)
                    {
                        errors += string.Format("{0:x2}", md5[i]);
                    }
                    if (!backupMode)
                    {
                        errors += "\n, expected: ";
                        for (int i = 0; i < entries[index].md5.Count(); i++)
                        {
                            errors += string.Format("{0:x2}", entries[index].md5[i]);
                        }
                    }
                    errors += Environment.NewLine;

                    if (ipc)
                    {
                        Console.WriteLine("[IPC]ERROR " + packageDLCFiles[l]);
                        Console.Out.Flush();
                    }
                }
                progress += packageDLCFiles.Count();
            }

            if (sfarFiles != null && !withoutSfars)
            {
                for (int l = 0; l < sfarFiles.Count; l++)
                {
                    if (ipc)
                    {
                        Console.WriteLine("[IPC]PROCESSING_FILE " + sfarFiles[l]);
                        Console.WriteLine("[IPC]OVERALL_PROGRESS " + ((l + progress) * 100 / allFilesCount));
                        Console.Out.Flush();
                    }
                    byte[] md5 = calculateMD5(sfarFiles[l]);
                    bool found = false;
                    for (int p = 0; p < entries.Count(); p++)
                    {
                        if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, entries[p].md5))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        continue;
                    int index = -1;
                    for (int p = 0; p < entries.Count(); p++)
                    {
                        if (GameData.RelativeGameData(sfarFiles[l]).ToLowerInvariant() == entries[p].path.ToLowerInvariant())
                        {
                            index = p;
                            break;
                        }
                    }
                    if (index == -1 && !backupMode)
                        continue;

                    vanilla = false;

                    if (!backupMode)
                        errors += "File " + sfarFiles[l] + " has wrong MD5 checksum: ";
                    else
                        errors += "File " + sfarFiles[l] + " not found in database, MD5 checksum: ";
                    for (int i = 0; i < md5.Count(); i++)
                    {
                        errors += string.Format("{0:x2}", md5[i]);
                    }
                    if (!backupMode)
                    {
                        errors += "\n, expected: ";
                        for (int i = 0; i < entries[index].md5.Count(); i++)
                        {
                            errors += string.Format("{0:x2}", entries[index].md5[i]);
                        }
                    }
                    errors += Environment.NewLine;

                    if (ipc)
                    {
                        Console.WriteLine("[IPC]ERROR " + sfarFiles[l]);
                        Console.Out.Flush();
                    }
                }
                progress += sfarFiles.Count();
            }

            if (tfcFiles != null)
            {
                for (int l = 0; l < tfcFiles.Count; l++)
                {
                    if (ipc)
                    {
                        Console.WriteLine("[IPC]PROCESSING_FILE " + tfcFiles[l]);
                        Console.WriteLine("[IPC]OVERALL_PROGRESS " + ((l + progress) * 100 / allFilesCount));
                        Console.Out.Flush();
                    }
                    byte[] md5 = calculateMD5(tfcFiles[l]);
                    bool found = false;
                    for (int p = 0; p < entries.Count(); p++)
                    {
                        if (StructuralComparisons.StructuralEqualityComparer.Equals(md5, entries[p].md5))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        continue;
                    int index = -1;
                    for (int p = 0; p < entries.Count(); p++)
                    {
                        if (GameData.RelativeGameData(tfcFiles[l]).ToLowerInvariant() == entries[p].path.ToLowerInvariant())
                        {
                            index = p;
                            break;
                        }
                    }
                    if (index == -1 && !backupMode)
                        continue;

                    vanilla = false;

                    if (!backupMode)
                        errors += "File " + tfcFiles[l] + " has wrong MD5 checksum: ";
                    else
                        errors += "File " + tfcFiles[l] + " not found in database, MD5 checksum: ";
                    for (int i = 0; i < md5.Count(); i++)
                    {
                        errors += string.Format("{0:x2}", md5[i]);
                    }
                    if (!backupMode)
                    {
                        errors += "\n, expected: ";
                        for (int i = 0; i < entries[index].md5.Count(); i++)
                        {
                            errors += string.Format("{0:x2}", entries[index].md5[i]);
                        }
                    }
                    errors += Environment.NewLine;

                    if (ipc)
                    {
                        Console.WriteLine("[IPC]ERROR " + tfcFiles[l]);
                        Console.Out.Flush();
                    }
                }
                progress += tfcFiles.Count();
            }
            if (generateMd5Entries)
                fs.Close();

            return vanilla;
        }

        static public bool detectsMismatchPackagesAfter(MeType gameType, bool ipc = false)
        {
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData(gameType, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

            gameData.getPackages();

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Program.MAINEXENAME);
            string mapFile = Path.Combine(path, "me" + (int)gameType + "map.bin");
            if (!File.Exists(mapFile))
                return false;
            using (FileStream fs = new FileStream(mapFile, FileMode.Open, FileAccess.Read))
            {
                uint tag = fs.ReadUInt32();
                uint version = fs.ReadUInt32();
                if (tag != CmdLineTools.textureMapBinTag || version != CmdLineTools.textureMapBinVersion)
                {
                    Console.WriteLine("Detected wrong or old version of textures scan file!" + Environment.NewLine);
                    return false;
                }

                uint countTexture = fs.ReadUInt32();
                for (int i = 0; i < countTexture; i++)
                {
                    int len = fs.ReadInt32();
                    fs.ReadStringASCII(len);
                    fs.ReadUInt32();
                    uint countPackages = fs.ReadUInt32();
                    for (int k = 0; k < countPackages; k++)
                    {
                        fs.ReadInt32();
                        fs.ReadInt32();
                        len = fs.ReadInt32();
                        fs.ReadStringASCII(len);
                    }
                }

                List<string> packages = new List<string>();
                int numPackages = fs.ReadInt32();
                for (int i = 0; i < numPackages; i++)
                {
                    int len = fs.ReadInt32();
                    string pkgPath = fs.ReadStringASCII(len);
                    pkgPath = GameData.GamePath + pkgPath;
                    packages.Add(pkgPath);
                }
                Console.WriteLine("\nChecking for removed files since last game data scan...");
                for (int i = 0; i < packages.Count; i++)
                {
                    if (GameData.packageFiles.Find(s => s.Equals(packages[i], StringComparison.OrdinalIgnoreCase)) == null)
                    {
                        Console.WriteLine("File: " + GameData.RelativeGameData(packages[i]));
                        if (ipc)
                        {
                            Console.WriteLine("[IPC]ERROR_REMOVED_FILE " + GameData.RelativeGameData(packages[i]));
                            Console.Out.Flush();
                        }
                    }
                }
                Console.WriteLine("Finished checking for removed files since last game data scan.");

                Console.WriteLine("\nChecking for additional files since last game data scan...");
                for (int i = 0; i < GameData.packageFiles.Count; i++)
                {
                    if (packages.Find(s => s.Equals(GameData.packageFiles[i], StringComparison.OrdinalIgnoreCase)) == null)
                    {
                        Console.WriteLine("File: " + GameData.RelativeGameData(GameData.packageFiles[i]));
                        if (ipc)
                        {
                            Console.WriteLine("[IPC]ERROR_ADDED_FILE " + GameData.RelativeGameData(GameData.packageFiles[i]));
                            Console.Out.Flush();
                        }
                    }
                }
                Console.WriteLine("Finished checking for additional files since last game data scan.");
            }

            return true;
        }

        static public bool checkGameFilesAfter(MeType gameType, bool ipc = false)
        {
            ConfIni configIni = new ConfIni();
            GameData gameData = new GameData(gameType, configIni);
            if (GameData.GamePath == null || !Directory.Exists(GameData.GamePath))
            {
                Console.WriteLine("Error: Could not found the game!");
                return false;
            }

            gameData.getPackages();

            Console.WriteLine("\nChecking for vanilla files after textures installation...");
            string path = "";
            if (GameData.gameType == MeType.ME1_TYPE)
            {
                path = @"\BioGame\CookedPC\testVolumeLight_VFX.upk".ToLowerInvariant();
            }
            if (GameData.gameType == MeType.ME2_TYPE)
            {
                path = @"\BioGame\CookedPC\BIOC_Materials.pcc".ToLowerInvariant();
            }
            List<string> filesToUpdate = new List<string>();
            for (int i = 0; i < GameData.packageFiles.Count; i++)
            {
                if (path != "" && GameData.packageFiles[i].ToLowerInvariant().Contains(path))
                    continue;
                filesToUpdate.Add(GameData.packageFiles[i].ToLowerInvariant());
            }
            for (int i = 0; i < filesToUpdate.Count; i++)
            {
                if (ipc)
                {
                    Console.WriteLine("[IPC]PROCESSING_FILE " + filesToUpdate[i]);
                    Console.WriteLine("[IPC]OVERALL_PROGRESS " + ((i + 1) * 100 / filesToUpdate.Count));
                    Console.Out.Flush();
                }
                try
                {
                    using (FileStream fs = new FileStream(filesToUpdate[i], FileMode.Open, FileAccess.Read))
                    {
                        fs.SeekEnd();
                        fs.Seek(-Package.MEMendFileMarker.Length, SeekOrigin.Current);
                        string marker = fs.ReadStringASCII(Package.MEMendFileMarker.Length);
                        if (marker != Package.MEMendFileMarker)
                        {
                            Console.WriteLine("Replaced file: " + filesToUpdate[i]);
                            if (ipc)
                            {
                                Console.WriteLine("[IPC]ERROR_VANILLA_MOD_FILE " + filesToUpdate[i]);
                                Console.Out.Flush();
                            }
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("The file could not be opened, skipped: " + filesToUpdate[i]);
                    if (ipc)
                    {
                        Console.WriteLine("[IPC]ERROR The file could not be opened: " + filesToUpdate[i]);
                        Console.Out.Flush();
                    }
                }
            }

            Console.WriteLine("Finished checking for vanilla files after textures installation");

            return true;
        }
    }
}
