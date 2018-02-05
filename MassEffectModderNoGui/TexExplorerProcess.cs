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
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MassEffectModder
{
    public partial class MipMaps
    {
        const uint maxBlockSize = 0x20000; // 128KB
        const int SizeOfChunkBlock = 8;
        const int SizeOfChunk = 8;
        public const uint FileTextureTag = 0x53444446;
        public const uint FileBinaryTag = 0x4E494246;

        public struct FileMod
        {
            public uint tag;
            public string name;
            public long offset;
            public long size;
        }

        static public Stream compressData(byte[] inputData)
        {
            MemoryStream ouputStream = new MemoryStream();
            uint compressedSize = 0;
            uint dataBlockLeft = (uint)inputData.Length;
            uint newNumBlocks = ((uint)inputData.Length + maxBlockSize - 1) / maxBlockSize;
            List<Package.ChunkBlock> blocks = new List<Package.ChunkBlock>();
            using (MemoryStream inputStream = new MemoryStream(inputData))
            {
                // skip blocks header and table - filled later
                ouputStream.Seek(SizeOfChunk + SizeOfChunkBlock * newNumBlocks, SeekOrigin.Begin);

                for (int b = 0; b < newNumBlocks; b++)
                {
                    Package.ChunkBlock block = new Package.ChunkBlock();
                    block.uncomprSize = Math.Min(maxBlockSize, dataBlockLeft);
                    dataBlockLeft -= block.uncomprSize;
                    block.uncompressedBuffer = inputStream.ReadToBuffer(block.uncomprSize);
                    blocks.Add(block);
                }
            }

            Parallel.For(0, blocks.Count, b =>
            {
                Package.ChunkBlock block = blocks[b];
                block.compressedBuffer = new ZlibHelper.Zlib().Compress(block.uncompressedBuffer);
                if (block.compressedBuffer.Length == 0)
                    throw new Exception("Compression failed!");
                block.comprSize = (uint)block.compressedBuffer.Length;
                blocks[b] = block;
            });

            for (int b = 0; b < blocks.Count; b++)
            {
                Package.ChunkBlock block = blocks[b];
                ouputStream.Write(block.compressedBuffer, 0, (int)block.comprSize);
                compressedSize += block.comprSize;
            }

            ouputStream.SeekBegin();
            ouputStream.WriteUInt32(compressedSize);
            ouputStream.WriteInt32(inputData.Length);
            foreach (Package.ChunkBlock block in blocks)
            {
                ouputStream.WriteUInt32(block.comprSize);
                ouputStream.WriteUInt32(block.uncomprSize);
            }

            return ouputStream;
        }

        static public byte[] decompressData(Stream stream, long compressedSize)
        {
            uint compressedChunkSize = stream.ReadUInt32();
            uint uncompressedChunkSize = stream.ReadUInt32();
            byte[] data = new byte[uncompressedChunkSize];
            uint blocksCount = (uncompressedChunkSize + maxBlockSize - 1) / maxBlockSize;
            if ((compressedChunkSize + SizeOfChunk + SizeOfChunkBlock * blocksCount) != compressedSize)
                throw new Exception("not match");

            List<Package.ChunkBlock> blocks = new List<Package.ChunkBlock>();
            for (uint b = 0; b < blocksCount; b++)
            {
                Package.ChunkBlock block = new Package.ChunkBlock();
                block.comprSize = stream.ReadUInt32();
                block.uncomprSize = stream.ReadUInt32();
                blocks.Add(block);
            }

            for (int b = 0; b < blocks.Count; b++)
            {
                Package.ChunkBlock block = blocks[b];
                block.compressedBuffer = stream.ReadToBuffer(blocks[b].comprSize);
                block.uncompressedBuffer = new byte[maxBlockSize * 2];
                blocks[b] = block;
            }

            Parallel.For(0, blocks.Count, b =>
            {
                uint dstLen = 0;
                Package.ChunkBlock block = blocks[b];
                dstLen = new ZlibHelper.Zlib().Decompress(block.compressedBuffer, block.comprSize, block.uncompressedBuffer);
                if (dstLen != block.uncomprSize)
                    throw new Exception("Decompressed data size not expected!");
            });

            int dstPos = 0;
            for (int b = 0; b < blocks.Count; b++)
            {
                Buffer.BlockCopy(blocks[b].uncompressedBuffer, 0, data, dstPos, (int)blocks[b].uncomprSize);
                dstPos += (int)blocks[b].uncomprSize;
            }

            return data;
        }

        public void extractTextureToPng(string outputFile, string packagePath, int exportID)
        {
            Package package = new Package(packagePath);
            Texture texture = new Texture(package, exportID, package.getExportData(exportID));
            package.Dispose();
            PixelFormat format = Image.getEngineFormatType(texture.properties.getProperty("Format").valueName);
            Texture.MipMap mipmap = texture.getTopMipmap();
            byte[] data = texture.getTopImageData();
            if (data == null)
                return;
            PngBitmapEncoder image = Image.convertToPng(data, mipmap.width, mipmap.height, format);
            if (File.Exists(outputFile))
                File.Delete(outputFile);
            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
            {
                image.Save(fs);
            }
        }

    }
}
