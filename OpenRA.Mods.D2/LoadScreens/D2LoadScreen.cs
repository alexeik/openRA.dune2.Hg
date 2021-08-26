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
using System.Diagnostics;
using OpenRA.Graphics;
using OpenRA.Mods.Common.LoadScreens;
using OpenRA.Mods.D2.ImportData;
using OpenRA.Mods.D2.SpriteLoaders;
using OpenRA.Primitives;

namespace OpenRA.Mods.D2
{
	public sealed class D2LoadScreen : BlankLoadScreen
	{
		Stopwatch lastUpdate = Stopwatch.StartNew();
		Renderer r;

		public int srcBuffer = Game.Renderer.Context.CreateSSBOBuffer();
		public int dstBuffer = Game.Renderer.Context.CreateSSBOBuffer();

		float2 logoPos;
		SheetBuilder sheetBuilder;
		ISpriteFrame[] frames;
		Sprite logo;
		string[] messages = { "Loading..." };
		ImmutablePalette palette;
		HardwarePalette hardwarePalette;
		PaletteReference pr;

		ModData modData;
		Dictionary<string, string> info;

		void ImportOriginalMaps()
		{
			D2ImportOriginalMaps.ImportOriginalMaps(modData, info);

			/* run only once */
			Game.OnShellmapLoaded -= ImportOriginalMaps;
		}

		public override void Init(ModData modData, Dictionary<string, string> info)
		{
			base.Init(modData, info);

			this.modData = modData;
			this.info = info;

			/*
			 * Unpack files needed, because in some PAK files, some VOC files can have prefix 'Z'
			 * Unpacking files will unpack such files and rename. so no modifications in yaml needed.
			 * LoadScreen.Init, possibly not the best place to do this, but need to do that early, before
			 * data will be used. and do this in LoadScreen.Init just works fine.
			 */
			if (D2UnpackContent.UnpackFiles(modData, info) > 0)
			{
				// Some files unpacked. need to reload mod packages
				modData.ModFiles.LoadFromManifest(modData.Manifest);
			}

			/*
			 * Like for unpack files, OpenRA engine do not have proper place for import maps.
			 * And can't import in LoadScreen.Init, because engine not ready.
			 * but Game.OnShellmapLoaded just works.
			 */
			Game.OnShellmapLoaded += ImportOriginalMaps;

			// Avoid standard loading mechanisms so we
			// can display the loadscreen as early as possible
			r = Game.Renderer;
			if (r == null)
				return;

			if (info.ContainsKey("Text"))
				messages = info["Text"].Split(',');

			if (info.ContainsKey("Palette"))
			{
				using (var stream = modData.DefaultFileSystem.Open(info["Palette"]))
				{
					palette = new ImmutablePalette(stream, new int[] { });
				}

				hardwarePalette = new HardwarePalette();
				hardwarePalette.AddPalette("loadscreen", palette, false);
				hardwarePalette.Initialize();
				r.SetPalette(hardwarePalette);
				var pal = hardwarePalette.GetPalette("loadscreen");
				pr = new PaletteReference("loadscreenref", hardwarePalette.GetPaletteIndex("loadscreen"), pal, hardwarePalette);
			}

			if (info.ContainsKey("Image"))
			{
				using (var stream = modData.DefaultFileSystem.Open(info["Image"]))
				{
					CpsD2Loader loader = new CpsD2Loader();
					TypeDictionary metadata;

					if (!loader.TryParseSprite(stream, out frames, out metadata))
						return;
				}

				if (frames.Length == 0)
					return;

				sheetBuilder = new SheetBuilder(SheetType.Indexed, 1024);
				frames[0].Data = ResizeByMMPX(frames[0]);
				logo = sheetBuilder.Add(frames[0]);

				logoPos = new float2((r.Resolution.Width - logo.Size.X) / 2, (r.Resolution.Height - logo.Size.Y) / 2);
			}
		}
		private byte[] ResizeByMMPX(ISpriteFrame inputSprite)
		{
			float3 groupSize = new float3(64, 1, 1);
			float scale = 1f;
			int outputCoof = 2;
			Size dest_dims = new Size();

			Size source_dims = new Size(inputSprite.Size.Width * scale, inputSprite.Size.Height * scale);
			//два блока if наращивают размер до кратного числу 64=8*8, 8*8 из compute.cmp шейдера
			if (source_dims.Width % (int)groupSize.X != 0)
			{
				int m = (source_dims.Width * outputCoof) % (int)groupSize.X;
				dest_dims = new Size(source_dims.Width * outputCoof + (int)groupSize.X - m, source_dims.Height);

			}
			else
			{
				dest_dims = new Size(source_dims.Width * outputCoof, source_dims.Height);
			}
			if (source_dims.Height % (int)groupSize.X != 0)
			{
				int m = (source_dims.Height * outputCoof) % (int)groupSize.X;
				dest_dims = new Size(dest_dims.Width, source_dims.Height * outputCoof + (int)groupSize.X - m);
			}
			else
			{
				dest_dims = new Size(dest_dims.Width, source_dims.Height * outputCoof);
			}


			dest_dims = new Size(dest_dims.Width, dest_dims.Height);

			int len = dest_dims.Width * dest_dims.Height;

			//expand input data to ARGB

			var dataex = new uint[inputSprite.Size.Width * inputSprite.Size.Height];
			for (var y = 0; y < inputSprite.Size.Height; y++)
			{
				for (var x = 0; x < inputSprite.Size.Width; x++)
				{
					var iData = (y * inputSprite.Size.Width + x);
					var iRaw = (y * inputSprite.Size.Width + x);
					dataex[iData] = BitConverter.ToUInt32(new byte[] { 0, 0, 0, inputSprite.Data[iRaw] }, 0); //ABGR

				}
			}


			Game.Renderer.cmproc.SetViewportParams(new Size(), 0f, 0f, 1f, int2.Zero); // Size тут не важен, так как CS шейдер

			Game.Renderer.Context.BindSSBOBuffer(srcBuffer, 1);
			Game.Renderer.Context.SetSSBOData(dataex, 0, dataex.Length);
			Game.Renderer.Context.UnbindSSBO();

			Game.Renderer.Context.BindSSBOBuffer(dstBuffer, 0);
			Game.Renderer.Context.SetSSBOData(new uint[len], 0, len);
			Game.Renderer.Context.UnbindSSBO();

			Game.Renderer.cmproc.SetVec("dst_size", dest_dims.Width, dest_dims.Height);
			Game.Renderer.cmproc.SetVec("src_size", source_dims.Width, source_dims.Height);
			Game.Renderer.cmproc.SetVec("src_max", source_dims.Width - 1, source_dims.Height - 1);

			Game.Renderer.Context.DispatchCompute(source_dims.Width / 8, source_dims.Height / 8, 1);

			uint[] dataOut = new uint[len];
			Game.Renderer.Context.BindSSBOBuffer(dstBuffer, 0);
			Game.Renderer.Context.GetBufferData(dataOut, dstBuffer, 0, len); //ABGR
			Game.Renderer.Context.UnbindSSBO();



			int destSkipX = (dest_dims.Width - source_dims.Width * 2);
			int destTakeX = source_dims.Width * 2;
			int destSkipY = dest_dims.Height - source_dims.Height * 2;
			int destTakeY = source_dims.Height * 2;
			int destCursor = 0;
			int destCursorStep = 0;

			var data = new byte[source_dims.Height * 2 * source_dims.Width * 2];

			for (var y = 0; y < destTakeY; y++)
			{
				for (destCursor = 0; destCursor < destTakeX; destCursor++) //тут надо ограничить физ.размерами картинки
				{
					var iData = (y * destTakeX + destCursor);
					//var iRaw = 4 * ((s.Height - y - 1) * s.Width + x);
					var iRaw = (y * dest_dims.Width + destCursor);
					byte[] conv = BitConverter.GetBytes(dataOut[iRaw]);

					data[iData] = conv[3]; //R оставим тоже массив из одного байта в BGRA структуре.
										   //data[iData + 1] = 0; //G
										   //data[iData + 2] = conv[1]; //R
										   //data[iData + 3] = 0; //A
				}
				//*после достижения ограничения физ.размеров, нужно сделать пропуск до следующего ряда пикселей.
				//destCursorStep += destSkipX;
			}
			if (data.Length == 0)
			{

			}

			//inputSprite.Size = dest_dims;
			inputSprite.Size = new Size(source_dims.Width * 2, source_dims.Height * 2);
			return data;
		}
		public override void Display()
		{
			if (r == null)
				return;

			// Update text at most every 0.2 seconds
			if (lastUpdate.Elapsed.TotalSeconds < 0.2)
				return;

			if (r.Fonts == null)
				return;

			lastUpdate.Restart();
			var text = messages.Random(Game.CosmeticRandom);
			var textSize = r.Fonts["Original"].Measure(text);

			r.BeginFrame(int2.Zero, 1f);

			if (logo != null)
				r.SpriteRenderer.DrawSprite(logo, logoPos, pr, logo.Size);
            Color textcolor=Color.White;
           if  (this.info.ContainsKey("TextColor"))
                {
                Color ss ;
               ss= FieldLoader.GetValue<Color>("(value)", info["TextColor"]);
                textcolor =ss;
            }
           // r.Flush();
            //r.SetViewportParams(int2.Zero, 1f);
			r.Fonts["Original"].DrawText(text, new float2(r.Resolution.Width - textSize.X - 20, r.Resolution.Height - textSize.Y - 20), textcolor);
            //r.Fonts["Original"].DrawTextWithShadow(text, new float2(r.Resolution.Width - textSize.X - 20, r.Resolution.Height - textSize.Y - 20), textcolor, Color.FromArgb(150, 0, 0), Color.FromArgb(100, 100, 0, 0), 3);
            r.EndFrame(new NullInputHandler());
		}

		protected override void Dispose(bool disposing)
		{
			//if (disposing && sheetBuilder != null)
			//	sheetBuilder.Dispose();

			//if (disposing && hardwarePalette != null)
			//	hardwarePalette.Dispose();

			//base.Dispose(disposing);
		}
	}
}
