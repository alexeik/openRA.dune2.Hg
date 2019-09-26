#region Copyright & License Information
/*
 * Copyright 2007-2019 The d2 mod Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using OpenRA.Graphics;
using OpenRA.Mods.Common.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Mods.D2.SpriteLoaders
{


    public class CpsD2Loader : ISpriteLoader
	{
		public const int TileWidth = 320;
		public const int TileHeight = 200;
        public string SpriteFilename = "";

		public const int TileSize = TileWidth * TileHeight;
		const int NumTiles = 1;
		uint palSize;
        bool HasEmbeddedPalette = false;
        ImmutablePalette CpsPalette;
        Color[] paldataColor;
        byte[] paldataByte;


        public class CpsD2Tile : ISpriteFrame
        {
            public Size Size { get; private set; }
            public Size FrameSize { get { return Size; } }
            public float2 Offset { get { return float2.Zero; } }
            public byte[] Data { get; set; }
            public bool DisableExportPadding { get { return false; } }

            public CpsD2Tile(Stream s)
            {
                Size = new Size(TileWidth, TileHeight);
                var tempData = StreamExts.ReadBytes(s, (int)(s.Length - s.Position));
                Data = new byte[TileSize];
                LCWCompression.DecodeInto(tempData, Data);

       

            }
        }

        public CpsD2Loader(string spriteFilename)
        {
            SpriteFilename = spriteFilename;
        }
        public CpsD2Loader()
        { }
        bool IsCpsD2(Stream s)
		{
			if (s.Length < 10)
				return false;

			var start = s.Position;

			s.Position += 2;

			var format = s.ReadUInt16();
			if (format != 0x0004)
			{
				s.Position = start;
				return false;
			}

			var sizeXTimeSizeY = s.ReadUInt16();
			sizeXTimeSizeY += s.ReadUInt16();
            //4 и 5 байт кодируют 32 битное число. Это число произведение ширины на длину=320*200=64000
			if (sizeXTimeSizeY != TileSize)
			{
				s.Position = start;
				return false;
			}

			palSize = s.ReadUInt16();
            if (palSize==768)
            {
                ReadEmbeddedPalette(s);
                HasEmbeddedPalette = true;
            }
			s.Position = start;
			return true;
		}
        public void ReadEmbeddedPalette(Stream s)
        {
            //int n = 768;
            //paldata = new byte[n]; 
            //for (var i = 0; i < n; i++)
            //    paldata[i] = s.ReadUInt8();


            //paldata = new Color[256];
            //for (var i = 0; i < 768 / 3; i++)
            //{
            //    var r = s.ReadByte(); var g = s.ReadByte(); var b = s.ReadByte();
            //    paldata[i] = Color.FromArgb(r, g, b);
            //}


            paldataByte = StreamExts.ReadBytes(s, 768);

            Stream stream = new MemoryStream(paldataByte);
            CpsPalette = new ImmutablePalette(stream, new int[] { });

        }
        CpsD2Tile[] ParseFrames(Stream s)
		{
			var start = s.Position;

			s.Position += 10;
			s.Position += palSize;

			var tiles = new CpsD2Tile[NumTiles];
            for (var i = 0; i < tiles.Length; i++)
            {
                tiles[i] = new CpsD2Tile(s);
                if (HasEmbeddedPalette)
                {
                    //ApplyPalette2(tiles[i], paldata);
                   // FastCopyIntoSprite(null, paldata, tiles[i]);
                }
            }
           
			s.Position = start;
			return tiles;
		}
        public void ApplyPalette(CpsD2Tile image,byte[] palette)
        {
            byte[] newimage=new byte[64000*4];
            int k = 0;
            byte palcolor;
            byte sourcePalIndex;

            //for (var j = 0; j < image.Data.Length; j++)
            //    image.Data[j] = palette[image.Data[j]];


            for (int i = 0; i < image.Data.Length; i += 3)
            {
                sourcePalIndex = image.Data[i];

                //red
                //palcolor = paldata[sourcePalIndex];
                //newimage[k] = (byte)(palcolor * 4);

                //// green
                //palcolor = paldata[sourcePalIndex + 1];
                //newimage[k + 1] = (byte)(palcolor * 4);

                ////blue
                //palcolor = paldata[sourcePalIndex + 2];
                //newimage[k + 2] = (byte)(palcolor * 4);
                k += 3;//for next new pixel



            }
            image.Data = newimage;
        }
        public void ApplyPalette2(CpsD2Tile image, byte[] palette)
        {
            // Byte[] data = image data
            //Byte[] palette = 6 - bit palette
            Int32 stride = 320 * 4; //number of bytes on one line of the image
             Int32 width = 320;
            Int32 height = 200;



            Int32 lineOffset = 0;
            Int32 lineOffsetQuad = 0;
            Int32 strideQuad = width * 3;

            Byte[] dataArgb = new Byte[strideQuad * height];
            for (Int32 y = 0; y < height; ++y)
            {
                Int32 offset = lineOffset;
                Int32 outOffset = lineOffsetQuad;
                for (Int32 x = 0; x < width; ++x)
                {
                    // get colour index, then get the correct location in the palette array
                    // by multiplying it by 3 (the length of one full colour)
                    Int32 colIndex = image.Data[offset++] * 3;
                    dataArgb[outOffset++] = palette[colIndex + 2]; // Blue
                    dataArgb[outOffset++] = palette[colIndex + 1]; // Green
                    dataArgb[outOffset++] = palette[colIndex]; // Red
                    //dataArgb[outOffset++] = (colIndex == 0 ? (Byte)0 : (Byte)255); // Alpha: set to 0 for background black
                }
                lineOffset += stride;
                lineOffsetQuad += strideQuad;
            }
            image.Data = dataArgb;
        }
        public static void FastCopyIntoSprite(Sprite dest, Color[] pal,CpsD2Tile srccps)
        {
           
       

            byte[] destData = new byte[4 * 320 * 200];
            var destStride = 320;
            var width =320;
            var height =200;

            unsafe
            {
                // Cast the data to an int array so we can copy the src data directly
                fixed (byte* bd = &destData[0])
                {
                    var data = (int*)bd;
                    var x = 0;
                    var y = 0;

                    var k = 0;
                    for (var yy = 0; yy < height; yy++)
                    {
                        for (var xx = 0; xx < width; xx++)
                        {
                            Color cc;
                            if (pal == null)
                            {
                                var r = srccps.Data[k++];
                                var g = srccps.Data[k++];
                                var b = srccps.Data[k++];
                                var a = srccps.Data[k++];
                                cc = Color.FromArgb(a, r, g, b);
                            }
                            else
                                cc = pal[srccps.Data[k++]];

                            data[(y + yy) * destStride + x + xx] = PremultiplyAlpha(cc).ToArgb();
                        }
                    }
                }
            }
            srccps.Data = destData;
        }
        public static Color PremultiplyAlpha(Color c)
        {
            if (c.A == byte.MaxValue)
                return c;
            var a = c.A / 255f;
            return Color.FromArgb(c.A, (byte)(c.R * a + 0.5f), (byte)(c.G * a + 0.5f), (byte)(c.B * a + 0.5f));
        }
        public bool TryParseSprite(Stream s, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			metadata = null;
			if (!IsCpsD2(s))
			{
				frames = null;
				return false;
			}

			s.Position = 0;
			frames = ParseFrames(s);
			return true;
		}
        public bool TryParseSpritePlusPalette(Stream s, out ISpriteFrame[] frames, out TypeDictionary metadata, out ImmutablePalette Palette)
        {
            metadata = null;
           

            if (!IsCpsD2(s))
            {
                Palette = null;
                frames = null;
                return false;
            }
            Palette = CpsPalette;
            s.Position = 0;
            frames = ParseFrames(s);
            return true;
        }
    }
}
