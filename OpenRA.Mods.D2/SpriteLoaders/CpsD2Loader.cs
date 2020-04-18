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
using System.Collections.Generic;
using System.IO;
using OpenRA.Graphics;
using OpenRA.Mods.Common.FileFormats;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.D2.SpriteLoaders
{


    public class CpsD2Loader : SpriteLoaderBase
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
            }
           
			s.Position = start;
			return tiles;
		}

        public static Color PremultiplyAlpha(Color c)
        {
            if (c.A == byte.MaxValue)
                return c;
            var a = c.A / 255f;
            return Color.FromArgb(c.A, (byte)(c.R * a + 0.5f), (byte)(c.G * a + 0.5f), (byte)(c.B * a + 0.5f));
        }

        public override bool TryParseSprite(Stream s, string filename, out ISpriteFrame[] frames, out TypeDictionary metadata)
        {
            return TryParseSprite(s, out frames, out metadata);
        }

        public override bool TryParseSprite(Stream s, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			metadata = null;
			if (!IsCpsD2(s))
			{
				frames = null;
				return false;
			}
            //if (CpsPalette!=null)
            //{
            //    var palettes = new Dictionary<int, uint[]>();
            //    palettes.Add(CpsPalette.colors.Length, CpsPalette.colors);
            //    metadata = new TypeDictionary { new EmbeddedSpritePalette(framePalettes: palettes) };
            //}

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
