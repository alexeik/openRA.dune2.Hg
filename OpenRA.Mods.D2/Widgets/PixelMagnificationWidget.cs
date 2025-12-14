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
using OpenRA.Widgets;
using OpenRA.Primitives;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Traits;
using OpenRA.Platforms.Default;
using OpenRA.FileFormats;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.D2.Widgets
{
    public class PixelMagnificationWidget : Widget
    {
        //Size textsize = new Size(Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height);
        Size textsize = new Size(320, 256);
        private Sprite currentSprite;
        public string currentPalette;
        World w;
        private FrameBuffer iternalFB;
        private ITextureInternal text25;
        private Size dest_dims;
        private ITextureInternal text26;
        int srcBuffer;
        int dstBuffer;
        private ssboBuf[] src1;
        public bool onceFlag;
        private Size source_dims;
        float3 groupSize = new float3(64, 1, 1);
        public bool once2 = true;
        int coof = 2;
        float scale = 1f;
        private ITextureInternal text2;
        uint[] dataOut;

        public Sprite CurrentSprite
        {
            get
            {
                return currentSprite;
            }
            set
            {
                currentSprite = value;
            }
        }
        public override void OnVisible()
        {
            if (once2)
            {
                once2 = !once2;
            }
            else
            {
                return;
            }
            source_dims = new Size(currentSprite.Size.X * scale, currentSprite.Size.Y * scale );
            //два блока if наращивают размер до кратного числу 64=8*8, 8*8 из compute.cmp шейдера
            if (source_dims.Width % (int)groupSize.X != 0)
            {
                int m = (source_dims.Width * coof) % (int)groupSize.X;

                dest_dims = new Size(source_dims.Width * coof + (int)groupSize.X - m, source_dims.Height);
            }
            if (source_dims.Height % (int)groupSize.X != 0)
            {
                int m = (source_dims.Height * coof) % (int)groupSize.X;
                dest_dims = new Size(dest_dims.Width, source_dims.Height * coof + (int)groupSize.X - m);
            }

            dest_dims = new Size(dest_dims.Width, dest_dims.Height );
            FrameBuffer.DeleteFrameBuffer("fbPixelMag");
            iternalFB = FrameBuffer.SetupOrGetOneMoreFrameBuffer(source_dims, "fbPixelMag", 32);
            iternalFB.Texture[0].ScaleFilter = TextureScaleFilter.Linear;
                text26 = (ITextureInternal)iternalFB.BindThisFrameBufferWithNewTexture(true, source_dims, 32);
            iternalFB.Unbind();
        }
        [ObjectCreator.UseCtor]
        public PixelMagnificationWidget(World world)
        {
            //Veldrid.RenderDoc.Load(out Rnd);
            w = world;
            ISpriteFrame[] spriteFrames = w.Map.Rules.Sequences.SpriteCache.ParsedFramesStorage["SHAPES.SHP"];
            srcBuffer = Game.Renderer.Context.CreateSSBOBuffer();
            dstBuffer = Game.Renderer.Context.CreateSSBOBuffer();

        }
        public override void Draw()
        {

            //если Sprite есть
            //то берем его байты или ссылку на текстуру?
            //запускаем комп.шейдер с его байтами
            //получаем спрайт на результат работы комп.шейдера
            //отдаем спрайт в spriteWidget путем присвоения
            Game.Renderer.Flush();



            //не имеет значения, так как текстура уже создана в WorldRendere по размеру карты
            //iternalFB.ReBind(text25);
            Game.Renderer.cmproc.SetViewportParams(textsize, 0f, 0f, 1f, int2.Zero);
            ISpriteFrame[] spriteFrames = w.Map.Rules.Sequences.SpriteCache.ParsedFramesStorage["SHAPES.SHP"];

            //int len = Convert.ToInt32(currentSprite.Size.X) * Convert.ToInt32(currentSprite.Size.Y) * 4 * 2 * 2;
            //int h = Convert.ToInt32(currentSprite.Size.Y);
            //int width = Convert.ToInt32(currentSprite.Size.X);



            if (!onceFlag)
            {

            }
            int len = dest_dims.Width * dest_dims.Height;
            //iternalFB.Unbind();


            iternalFB.ReBind(text26);
            DrawSprite();

            iternalFB.Unbind(new int[] { 0, 0, 1024, 768 });



            uint[] data = text26.GetData32();

            Game.Renderer.Context.BindSSBOBuffer(srcBuffer, 1);
            Game.Renderer.Context.SetSSBOData(data, 0, data.Length);
            Game.Renderer.Context.UnbindSSBO();

            Game.Renderer.Context.BindSSBOBuffer(dstBuffer, 0);
            Game.Renderer.Context.SetSSBOData(new uint[len], 0, len);
            Game.Renderer.Context.UnbindSSBO();

            Game.Renderer.cmproc.SetVec("dst_size", dest_dims.Width, dest_dims.Height);
            Game.Renderer.cmproc.SetVec("src_size", source_dims.Width, source_dims.Height);
            Game.Renderer.cmproc.SetVec("src_max", source_dims.Width - 1, source_dims.Height - 1);

            //Game.Renderer.Context.DispatchCompute(source_dims.Width / (int)groupSize.X, 1, 1);
            Game.Renderer.Context.DispatchCompute(source_dims.Width / 8, source_dims.Height / 8, 1);
            dataOut = new uint[len];
            Game.Renderer.Context.BindSSBOBuffer(dstBuffer, 0);
            Game.Renderer.Context.GetBufferData(dataOut, dstBuffer, 0, len); //ABGR
            Game.Renderer.Context.UnbindSSBO();
            //Game.Renderer.Flush();
            if (!onceFlag)
            {
                //добавляем сохранение в текстуру 
                //переделываем animations у actor.rendersprites на эту текстуру
                //Rnd.TriggerCapture();
                SavePng(dataOut, new Size(dest_dims.Width, dest_dims.Height));


                onceFlag = !onceFlag;
            }
        }
        public void SaveUintToTexture()
        {
            text2 = (ITextureInternal)iternalFB.BindThisFrameBufferWithNewTexture(true, source_dims, 32);
            text2.SetData(dataOut);
            Sheet ns = new Sheet(SheetType.BGRA, text2);
            Sprite sp = new Sprite(ns, new Rectangle(0,0,text2.Size.Width,text2.Size.Height), TextureChannel.RGBA);

            iternalFB.Unbind();

        }
        public void ReplaceRenderSprites(string sequenceNameToReplace)
        {

             List<Actor> l = w.Actors
              .Where(x =>
                  x.Info.Name == "high_tech_factory" &&
                  x.Info.HasTraitInfo<RenderSpritesInfo>()
                  ).ToList();
            Actor a = l[0];
            RenderSprites rs = a.Trait<RenderSprites>();
            
        }

        public void SavePng(uint[] raw, Size s)
        {
            //ABGR input
            var mod = Game.ModData.Manifest.Metadata;
            var directory = Platform.ResolvePath(Platform.SupportDirPrefix, "Screenshots", Game.ModData.Manifest.Id, mod.Version);
            Directory.CreateDirectory(directory);

            var filename = "1";
            var path = Path.Combine(directory, string.Concat("ssbo_" + filename, ".png"));
            //output RGBA
            var data = new byte[raw.Length * 4]; //так как тут нужно отдать в Png класс где 1 байт 1 компонента. А к нам входит 1 int 4 компоненты.
            for (var y = 0; y < s.Height; y++)
            {
                for (var x = 0; x < s.Width; x++)
                {
                    var iData = 4 * (y * s.Width + x);
                    //var iRaw = 4 * ((s.Height - y - 1) * s.Width + x);
                    var iRaw = (y * s.Width + x);
                    byte[] conv = BitConverter.GetBytes(raw[iRaw]);
                    //data[iData] = raw[iRaw];
                    data[iData] = conv[3]; //R
                    if (conv[3] != 255 && conv[3] != 0)
                    {

                    }
                    data[iData + 1] = conv[2]; //G
                    data[iData + 2] = conv[1]; //B
                    data[iData + 3] = conv[0]; //A

                    //data[iData] = (byte)raw[iRaw + 3];
                    //data[iData + 1] = (byte)raw[iRaw + 2];
                    //data[iData + 2] = (byte)raw[iRaw + 1];
                    //data[iData + 3] = (byte)raw[iRaw];
                }
            }

            var screenshot = new Png(data, s.Width, s.Height);
            screenshot.Save(path);
        }
        public override void InitializePostChildrens(WidgetArgs args)
        {
            var spriteWidget = this.GetOrNull<SpriteWidget>("SPRITE");
            if (spriteWidget != null)
            {
                spriteWidget.GetSprite = () => currentSprite;
                currentPalette = spriteWidget.Palette;
                spriteWidget.GetPalette = () => currentPalette;
                spriteWidget.IsVisible = () => true;
            }
            base.Initialize(args);
        }
        public void DrawSprite()
        {

            var palette = currentPalette;
           

            if (currentSprite == null || palette == null)
                return;
            float2 offset = float2.Zero;
            PaletteReference pr;
            pr = Game.worldRenderer.Palette(palette);
            
            var size = new float2(source_dims.Width, source_dims.Height);
            iternalFB.PixelDumpRenderer.SetViewportParams(source_dims, 0f, 0f, 1f, int2.Zero);
            iternalFB.PixelDumpRenderer.DrawSprite(currentSprite, new float3(0, 0, 0), pr, size);
            iternalFB.PixelDumpRenderer.Flush();
        }
    }
}