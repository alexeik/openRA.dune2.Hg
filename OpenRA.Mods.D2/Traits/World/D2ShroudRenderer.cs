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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Platforms.Default;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class D2ShroudRendererInfo : ITraitInfo
	{
		public readonly string Sequence = "shroud";

		[SequenceReference("Sequence")]
		public readonly string[] ShroudVariants = new[] { "shroud" };

		[SequenceReference("Sequence")]
		public readonly string[] FogVariants = new[] { "fog" };

		[PaletteReference]
		public readonly string ShroudPalette = "shroud";

		[PaletteReference]
		public readonly string FogPalette = "fog";

		[Desc("Bitfield of shroud directions for each frame. Lower four bits are",
			"corners clockwise from TL; upper four are edges clockwise from top")]
		public readonly int[] Index = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

		[Desc("Hide or not map borders under shroud")]
		public readonly bool ShroudOnMapBorders = false;

		[Desc("Override for source art that doesn't define a fully shrouded tile")]
		[SequenceReference("Sequence")]
		public readonly string OverrideFullShroud = null;

		public readonly int OverrideShroudIndex = 15;

		[Desc("Override for source art that doesn't define a fully fogged tile")]
		[SequenceReference("Sequence")]
		public readonly string OverrideFullFog = null;

		public readonly int OverrideFogIndex = 15;

		public readonly BlendMode ShroudBlend = BlendMode.Alpha;
		public object Create(ActorInitializer init) { return new D2ShroudRenderer(init.World, this); }
	}

	public sealed class D2ShroudRenderer : IRenderShroud, IWorldLoaded, INotifyActorDisposing
	{
		[Flags]
		enum Edges : byte
		{
			None = 0,
			Top = 0x1,
			Right = 0x2,
			Bottom = 0x4,
			Left = 0x8,
			All = Top | Right | Bottom | Left,
			TopLeft = Top | Left,
			TopRight = Top | Right,
			BottomLeft = Bottom | Left,
			BottomRight = Bottom | Right
		}

		struct TileInfo
		{
			public readonly float3 ScreenPosition;
			public readonly byte Variant;

			public TileInfo(float3 screenPosition, byte variant)
			{
				ScreenPosition = screenPosition;
				Variant = variant;
			}
		}

		readonly D2ShroudRendererInfo info;
		readonly Map map;
		readonly Edges notVisibleEdges;
		readonly byte variantStride;
		readonly byte[] edgesToSpriteIndexOffset;

		readonly CellLayer<TileInfo> tileInfos;
		/// <summary>
		/// Оригинальные спрайт эффекта перехода от shroud к земле - используются на граничащих с shroud тайлами земли.
		/// </summary>
		readonly Sprite[] fogSprites, shroudSprites;
		readonly HashSet<PPos> cellsDirty = new HashSet<PPos>();
		readonly HashSet<PPos> cellsAndNeighborsDirty = new HashSet<PPos>();

		Shroud currentShroud;
		Func<PPos, bool> visibleUnderShroudDelega, visibleUnderFog;
		TerrainSpriteLayer shroudLayer, fogLayer;
        private TerrainSpriteLayer blendLayer;

        public D2ShroudRenderer(World world, D2ShroudRendererInfo info)
		{
			if (info.ShroudVariants.Length != info.FogVariants.Length)
				throw new ArgumentException("ShroudRenderer must define the same number of shroud and fog variants!", "info");

			if ((info.OverrideFullFog == null) ^ (info.OverrideFullShroud == null))
				throw new ArgumentException("ShroudRenderer cannot define overrides for only one of shroud or fog!", "info");

			if (info.ShroudVariants.Length > byte.MaxValue)
				throw new ArgumentException("ShroudRenderer cannot define this many shroud and fog variants.", "info");

			if (info.Index.Length >= byte.MaxValue)
				throw new ArgumentException("ShroudRenderer cannot define this many indexes for shroud directions.", "info");

			this.info = info;
			map = world.Map;

			tileInfos = new CellLayer<TileInfo>(map);

			// Load sprite variants
			var variantCount = info.ShroudVariants.Length;
			variantStride = (byte)(info.Index.Length + (info.OverrideFullShroud != null ? 1 : 0));
			shroudSprites = new Sprite[variantCount * variantStride];
			fogSprites = new Sprite[variantCount * variantStride];

			var sequenceProvider = map.Rules.Sequences;
			for (var j = 0; j < variantCount; j++)
			{
				var shroud = sequenceProvider.GetSequence(info.Sequence, info.ShroudVariants[j]);
				var fog = sequenceProvider.GetSequence(info.Sequence, info.FogVariants[j]);
				for (var i = 0; i < info.Index.Length; i++)
				{
					shroudSprites[j * variantStride + i] = shroud.GetSprite(i);
					fogSprites[j * variantStride + i] = fog.GetSprite(i);
				}

				if (info.OverrideFullShroud != null)
				{
					var i = (j + 1) * variantStride - 1;
					shroudSprites[i] = sequenceProvider.GetSequence(info.Sequence, info.OverrideFullShroud).GetSprite(0);
					fogSprites[i] = sequenceProvider.GetSequence(info.Sequence, info.OverrideFullFog).GetSprite(0);
				}
			}

			// Mapping of shrouded directions -> sprite index
			edgesToSpriteIndexOffset = new byte[(byte)(Edges.All) + 1];
			for (var i = 0; i < info.Index.Length; i++)
				edgesToSpriteIndexOffset[info.Index[i]] = (byte)i;

			if (info.OverrideFullShroud != null)
				edgesToSpriteIndexOffset[info.OverrideShroudIndex] = (byte)(variantStride - 1);

			notVisibleEdges = Edges.All;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			// Initialize tile cache
			// This includes the region outside the visible area to cover any sprites peeking outside the map
			foreach (var uv in w.Map.AllCells.MapCoords)
			{
				var pos = w.Map.CenterOfCell(uv.ToCPos(map));
				var screen = wr.Screen3DPosition(pos - new WVec(0, 0, pos.Z));
				var variant = (byte)Game.CosmeticRandom.Next(info.ShroudVariants.Length);
				tileInfos[uv] = new TileInfo(screen, variant);
			}

			// Dirty the whole projected space
			DirtyCells(map.AllCells.MapCoords.Select(uv => (PPos)uv));

			// All tiles are visible in the editor
			if (w.Type == WorldType.Editor)
				visibleUnderShroudDelega = _ => true;
			else
				visibleUnderShroudDelega = puv => map.Contains(puv);

			visibleUnderFog = puv => map.Contains(puv);

			var shroudSheet = shroudSprites[0].Sheet2D;
			if (shroudSprites.Any(s => s.Sheet2D != shroudSheet))
				throw new InvalidDataException("Shroud sprites span multiple sheets. Try loading their sequences earlier.");

			var shroudBlend = shroudSprites[0].BlendMode;
			if (shroudSprites.Any(s => s.BlendMode != shroudBlend))
				throw new InvalidDataException("Shroud sprites must all use the same blend mode.");

			var fogSheet = fogSprites[0].Sheet2D;
			if (fogSprites.Any(s => s.Sheet2D != fogSheet))
				throw new InvalidDataException("Fog sprites span multiple sheets. Try loading their sequences earlier.");

			var fogBlend = fogSprites[0].BlendMode;
			if (fogSprites.Any(s => s.BlendMode != fogBlend))
				throw new InvalidDataException("Fog sprites must all use the same blend mode.");
			

			shroudLayer = new TerrainSpriteLayer(w, wr, shroudSheet, shroudBlend, wr.Palette(info.ShroudPalette), false, "D2ShroudRenderer.shroudLayer");
			fogLayer = new TerrainSpriteLayer(w, wr, fogSheet, fogBlend, wr.Palette(info.FogPalette), false, "D2ShroudRenderer.foglayer");
			blendLayer = new TerrainSpriteLayer(w, wr, shroudSheet, fogBlend, wr.Palette(info.ShroudPalette), false, "D2ShroudRenderer.blendlayer");
		}

		Edges GetEdges(PPos puv, Func<PPos, bool> isVisible)
		{
			if (!isVisible(puv))
				return notVisibleEdges;

			var cell = ((MPos)puv).ToCPos(map);

			var edge = Edges.None;
			if (!isVisible((PPos)(cell + new CVec(0, -1)).ToMPos(map))) edge |= Edges.Top;
			if (!isVisible((PPos)(cell + new CVec(1, 0)).ToMPos(map))) edge |= Edges.Right;
			if (!isVisible((PPos)(cell + new CVec(0, 1)).ToMPos(map))) edge |= Edges.Bottom;
			if (!isVisible((PPos)(cell + new CVec(-1, 0)).ToMPos(map))) edge |= Edges.Left;

			if (!info.ShroudOnMapBorders)
			{
				var mpos = cell.ToMPos(map);
				if (edge == Edges.Top && mpos.V <= 1) edge = Edges.None;
				if (edge == Edges.Left && mpos.U <= 1) edge = Edges.None;
				if (edge == Edges.Bottom && mpos.V >= map.Tiles.Size.Height - 2) edge = Edges.None;
				if (edge == Edges.Right && mpos.U >= map.Tiles.Size.Width - 2) edge = Edges.None;

				if (edge == Edges.TopLeft && mpos.V <= 1 && mpos.U <= 1) edge = Edges.None;
				if (edge == Edges.TopRight && mpos.V <= 1 && mpos.U >= map.Tiles.Size.Width - 2) edge = Edges.None;
				if (edge == Edges.BottomLeft && mpos.V >= map.Tiles.Size.Height - 2 && mpos.U <= 1) edge = Edges.None;
				if (edge == Edges.BottomRight && mpos.V >= map.Tiles.Size.Height - 2 && mpos.U >= map.Tiles.Size.Width - 2) edge = Edges.None;
			}

			return edge;
		}

		void DirtyCells(IEnumerable<PPos> cells)
		{
			// PERF: Many cells in the shroud change every tick. We only track the changes here and defer the real work
			// we need to do until we render. This allows us to avoid wasted work.
			cellsDirty.UnionWith(cells);
		}

		public void RenderShroud(Shroud shroud, WorldRenderer wr)
		{
			DrawShroudSprite();
			blendLayer.offsetVert = 0;

			//Console.WriteLine("both sheets are equal : " +fogLayer.Sheet.Equals(shroudLayer.Sheet));
			if (currentShroud != shroud)
			{
				if (currentShroud != null)
					currentShroud.CellsChanged -= DirtyCells;

				if (shroud != null)
					shroud.CellsChanged += DirtyCells;

				// Needs the anonymous function to ensure the correct overload is chosen
				if (shroud != null)
					visibleUnderShroudDelega = puv => currentShroud.IsExplored(puv);
				else
					visibleUnderShroudDelega = puv => map.Contains(puv);

				if (shroud != null)
					visibleUnderFog = puv => currentShroud.IsVisible(puv);
				else
					visibleUnderFog = puv => map.Contains(puv);

				currentShroud = shroud;
				DirtyCells(map.ProjectedCellBounds);
			}

			// We need to update newly dirtied areas of the shroud.
			// Expand the dirty area to cover the neighboring cells, since shroud is affected by neighboring cells.
			foreach (var uv in cellsDirty)
			{
				cellsAndNeighborsDirty.Add(uv);
				var cell = ((MPos)uv).ToCPos(map);
				foreach (var direction in CVec.Directions)
					cellsAndNeighborsDirty.Add((PPos)(cell + direction).ToMPos(map));
			}

			var tileSize = Game.worldRenderer.TileSize;

			foreach (var puv in cellsAndNeighborsDirty)
			{
				var uv = (MPos)puv;
				if (!tileInfos.Contains(uv))
					continue;

				var tileInfo = tileInfos[uv];
				Sprite shroudSprite;
				Edges ed = GetEdges(puv, visibleUnderShroudDelega);

				if (Game.worldRenderer.World.RenderPlayer!=null)
				{
					
					if (ed == Edges.None)
					{
						shroudSprite = null;
					}
					else if(ed != Edges.None && ed != Edges.All)
					{
						//тут обрабатываются условия для квадратов граничащих с покрывалом карты. SHroud -покрывало.
						shroudSprite = GetSprite(shroudSprites, GetEdges(puv, visibleUnderShroudDelega), tileInfo.Variant);

						//тут и нужно делать спрайт, с командой на шейдер, который сделает бленд этого спрайта и общего shroud спрайта.
						//shroudSprite = Game.worldRenderer.World.RenderPlayer.ShroudSprite; если нужно рендерить тут, тайл из ShroudSprite текстуры

						//тут будем на первом проходе 
						//на следующих
						//получая тайлы с переходным эффектом, нужно их нарисовать на текстуре ShroudSPrite
						//потом текстура ShroudSprite обновится и затрет переходные тайлы.
						//тогда может перенести рендер ShroudSprite сюда, вместо Game.cs?
					}
					else
					{
						//когда Edges. разные стороны то от 
						//tileInfo.ScreenPosition.X это координаты центра тайла
						//Тут, показываются столбики. Но так как спрайт столбика с прозрачными областями, то на карте видны тайл, а поверх них столбики.
						if (tileInfo.ScreenPosition.X == tileSize.Width/2) //ScreenPosition указывает на цетнр спрайта = 8. Спрайт 16 на 16.
						//if (ed == Edges.Left) //ScreenPosition указывает на цетнр спрайта = 8. Спрайт 16 на 16.
						{
							shroudSprite = ChromeProvider.GetImage("dune2stolbvert", "background"); 
						}
						else if (tileInfo.ScreenPosition.Y == tileSize.Width/2)
						{
							shroudSprite = ChromeProvider.GetImage("dune2stolbvert90", "background");
						}
						else if (tileInfo.ScreenPosition.X == ((tileSize.Width/2 * 2 * map.MapSize.X) - tileSize.Width/2))
						{
							shroudSprite = ChromeProvider.GetImage("dune2stolbvert", "background");
						}
						else if (tileInfo.ScreenPosition.Y == ((tileSize.Width/2 * 2 * map.MapSize.Y) - tileSize.Width/2))
						{
							shroudSprite = ChromeProvider.GetImage("dune2stolbvert90", "background");
						}
						else
						{
							//это ссылка на текстуру, которая рендерится в реальном времени в цикле Game.cs
							shroudSprite = Game.worldRenderer.World.RenderPlayer.ShroudSprite;
						}

					}
				
					
				}
				else

				{
					shroudSprite = GetSprite(shroudSprites, GetEdges(puv, visibleUnderShroudDelega), tileInfo.Variant);
				}
				
				
				var shroudPos = tileInfo.ScreenPosition;

				if (shroudSprite != null)
				{
					shroudPos += shroudSprite.Offset - 0.5f * shroudSprite.Size;
					if (Game.worldRenderer.World.RenderPlayer != null)
					{
						//эти IF указывает, какие тайлы от Shroud заменить на новые, ссылающиеся на новую текстуру ShroudSPrite
						if (tileInfo.ScreenPosition.X == tileSize.Width/2 || tileInfo.ScreenPosition.Y == tileSize.Height/2 || tileInfo.ScreenPosition.X == ((tileSize.Width / 2 * 2 * map.MapSize.X) - tileSize.Width / 2) || tileInfo.ScreenPosition.Y == ((tileSize.Height / 2 * 2 * map.MapSize.Y) - tileSize.Height / 2)) //ScreenPosition указывает на цетнр спрайта = 8. Спрайт 16 на 16.
						{
							
						}
						else if (ed != Edges.None && ed != Edges.All)
						{
							//можно тут изменить программу шейдера для shroud который сливается с тайлами - переходные тайлы
							//следим за переходными тайлами в коллекции blendLayer
							//blendLayer.Add(uv, shroudSprite, shroudPos);
							//этот blendLayer использует SpriteRenderer шейдер


							//shroudSprite.ReBound(shroudSprite, new Rectangle(Convert.ToInt32(shroudPos.X), Convert.ToInt32(shroudPos.Y), tileSize.Width, tileSize.Height));
							//shroudSprite.SpriteType = 16;
							//работает, но полностью заменяет. Нужна маска в качестве оригинальной текстуры


						}
						else
						{
							//Rebound нужен, так как спрайты из GetSPrite расположены в общей текстуре на 16 позициях.
							// а нам надо перенаправить их в текстуру ShroudSprite, которая динамичная.
							shroudSprite.ReBoundTextureCoords(new Rectangle(Convert.ToInt32(shroudPos.X), Convert.ToInt32(shroudPos.Y), tileSize.Width, tileSize.Height));
						}
					}
				}

				var fogSprite = GetSprite(fogSprites, GetEdges(puv, visibleUnderFog), tileInfo.Variant);
				var fogPos = tileInfo.ScreenPosition;
				if (fogSprite != null)
					fogPos += fogSprite.Offset - 0.5f * fogSprite.Size;

				shroudLayer.Update(uv, shroudSprite, shroudPos); //так же, обнуляет вертексы для рисования перехода, если Edges = NOne 
				fogLayer.Update(uv, fogSprite, fogPos);
			}

			cellsDirty.Clear();
			cellsAndNeighborsDirty.Clear();


			//1.в FB подложить текстуру с shroudSprite
			//2.вызвать по ней blendLayer с командами по бленд тайлами и стандартным шейдером, с if=16
			//3.запустить blendLayer с rebound на shroudSPrite по fb0(умолчанию)
			//ApplyOriginalShroudsForShroudSprite(wr);



			fogLayer.Draw(wr.Viewport);
			shroudLayer.Draw(wr.Viewport);
			

		}

		void ApplyOriginalShroudsForShroudSprite(WorldRenderer wr)
        {
			if (Game.worldRenderer.World.RenderPlayer != null)
			{
				Size textsize = new Size(Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height);
				FrameBuffer iternalFB = FrameBuffer.SetupOrGetOneMoreFrameBuffer(textsize, "fbShroud", 8);
				iternalFB.ReBind((ITextureInternal)Game.worldRenderer.World.RenderPlayer.ShroudSprite.Sheet.texture);

				blendLayer.DrawOffsetVerts(wr.Viewport);

				iternalFB.Unbind();
			}
		}

		void DrawShroudSprite()
        {

			//добавить на вход, коллекцию спрайтов с оригиналами
			//добавить на вход, коллекцию спрайтов с ссылкой на shroudSPrite
			//
            if (Game.worldRenderer.World.RenderPlayer != null) //используется для режима, демо карты. То есть игроков нет, а карта показывается под главным меню.
            {
                //просто фон шейдер только для площади карты
                Size textsize = new Size(Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height); //не имеет значения, так как текстура уже создана в WorldRendere по размеру карты
				FrameBuffer iternalFB = FrameBuffer.SetupOrGetOneMoreFrameBuffer(textsize, "fbShroud", 8);
				//ITexture texShroud = iternalFB.BindThisFrameBufferWithNewTexture(true, textsize);
				iternalFB.PixelDumpRenderer.SetViewportParams(textsize, 0f, 0f, 1f, int2.Zero);
				iternalFB.ReBind((ITextureInternal)Game.worldRenderer.World.RenderPlayer.ShroudSprite.Sheet.texture);
				//RenderSomeShader
				Game.Renderer.sproc.AddCommand(5, 0, 0, Game.LocalTick, 0, new int2(2048, 2048), new float3(0, 0, 0),
					new float3(textsize.Width, textsize.Height, 0),
					Game.worldRenderer.World.RenderPlayer.ShroudSprite, null);
				Game.Renderer.sproc.ExecCommandBuffer();
				iternalFB.Unbind();
            }
        }

		Sprite GetSprite(Sprite[] sprites, Edges edges, int variant)
		{
			if (edges == Edges.None)
				return null;

			return sprites[variant * variantStride + edgesToSpriteIndexOffset[(byte)edges]];
		}

		bool disposed;

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			shroudLayer.Dispose();
			fogLayer.Dispose();
			disposed = true;
		}
	}
}
