using ImGuiNET;
using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.D2.FileFormats;
using OpenRA.Mods.D2.SpriteLoaders;
using OpenRA.Primitives;
using OpenRA.Widgets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpenRA.Mods.D2.Widgets
{

    public class D2IGWidget : ImGuiWidget
    {
        int index1 = 100;
        public override void Draw()
        {
            ImGui.NewFrame();
            ImGui.SetNextWindowPos(new Vector2(Convert.ToInt16(this.X), Convert.ToInt16(this.Y)), ImGuiCond.Once);
            ImGui.SetNextWindowSize(new Vector2(Convert.ToInt16(this.Width), Convert.ToInt16(this.Height)), ImGuiCond.Once);
            ImGui.Begin("Inspector");
            ImGui.Text("Hello Dune2");

            List<SpriteRenderable> sr=new List<SpriteRenderable>();
            Actor ar;
            ar = Game.worldRenderer.World.Actors.Where(x => { if (x.Info.Name == "harvester") { return true; } return false; }).FirstOrDefault();

            if (ImGui.Button("Dump Sequence Texture"))
            {
                {
                    Game.Renderer.PixelDumpRenderer.Setup();
                    DumpTextureChannel(TextureChannel.Red);
                    DumpTextureChannel(TextureChannel.Blue);
                    DumpTextureChannel(TextureChannel.Green);
                    DumpTextureChannel(TextureChannel.Alpha);
                }


            }

            if (ar == null)
            { }
            else
            {
                foreach (SpriteRenderable ss in ar.Render(Game.worldRenderer))
                {
                    sr.Add(ss);
                }

                this.BufferSpriteRenderable[index1] = sr;
                if (sr != null)
                {
                    ImGui.Text("This is " + ar.Info.Name + " at " + ar.Owner.PlayerName);
                    ImGui.Image(new IntPtr(index1), new Vector2(16 * 4, 16 * 3));

                }
            }
            UpdateWidgetSize();
            ImGui.End();
            base.Draw();
        }

        public void DumpTextureChannel(TextureChannel channel)
        {
            Game.Renderer.PixelDumpRenderer.fb.Bind();
            Sheet2D seqsheet;
            seqsheet = Game.ModData.DefaultSequences["arrakis2"].SpriteCache.SheetBuilder2D.Current;
            Sprite sp = new Sprite(seqsheet, new Rectangle() { Width = seqsheet.Size.Width, Height = seqsheet.Size.Height }, channel); //чтобы прочитать все 4 канала seqsheet
                                                                                                                                                  //нужно использовать 4 итерации, где нужно менять канал в спрайте.

            Game.Renderer.PixelDumpRenderer.DrawSprite(sp, new float3(0, 0, 0));
            Game.Renderer.PixelDumpRenderer.Flush();
            Game.Renderer.PixelDumpRenderer.fb.Unbind();
            //нарисовали в текстуру в свой фреймбуфер.


            //теперь нужно запустить еще раз рендер, где эта текстура будет как аргумент у шейдера и он нарисует все пиксели в фреймбуфер главный.

            Sheet sh1 = new Sheet(SheetType.BGRA, Game.Renderer.PixelDumpRenderer.fb.Texture);
            Sprite sp2 = new Sprite(sh1, new Rectangle(0, 0, 2048, 2048), TextureChannel.Red); //тут канал не важен, он будет подавлен через SPriteType
            sp2.SpriteType = 5;
            Game.Renderer.PixelDumpRenderer.DrawSprite(sp2, new float3(0, 0, 0));


            sp.SpriteType = 0;
            Game.Renderer.PixelDumpRenderer.Flush();

            Game.TakeTextureInner(Game.Renderer.PixelDumpRenderer.fb.Texture);
        }
    }

}
