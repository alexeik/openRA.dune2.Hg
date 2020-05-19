using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.D2.FileFormats;
using OpenRA.Mods.D2.SpriteLoaders;
using OpenRA.Platforms.Default;
using OpenRA.Primitives;
using OpenRA.Widgets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.D2.Widgets
{
    public class CampaignWidget : Widget
    {
        CpsD2Loader image = null;
        TypeDictionary metadata;
        ImmutablePalette cpspalette;
        CpsD2Loader video;
        ISpriteFrame[] imageSprite = null;
        ImmutablePalette palette;
        HardwarePalette hardwarePalette;
        PaletteReference pr;
        private PaletteReference prbase;
        Sprite stolb_sprite;
        Sprite stolb_top_sprite;
        private Sprite stolb_bot_sprite;
        private Sprite stolb_shpere_sprite;
        private Sprite stolb_line_sprite;
        private Sprite stolb_horiz_sprite;
        private Sprite stolb_horiz_left_sprite;
        private Sprite stolb_horiz_right_sprite;
        private Sprite bg_sprite;
        private Sprite but1_sprite;
        private Sprite but2_sprite;
        private Sprite credits_sprite;
        private Sprite status_l_sprite;
        private Sprite status_horiz_sprite;
        private Sprite status_r_sprite;
        private Sprite vert_line_sprite;
        private Sprite vertbord_line_sprite;
        private Sprite houseSprite;
        private Sprite dunergnSprite;
        private Sprite dunergnclickSprite;
        /// <summary>
        /// Если использовать [ObjectCreator.UseCtor] , то можно использовать DI для инициализации аргументов конструктора.
        /// </summary>
        /// <param name="world"></param>
        [ObjectCreator.UseCtor]
        public CampaignWidget(World world)
        {
            dunergnSprite = ChromeProvider.GetImage("dunergn", "background");
            dunergnclickSprite = ChromeProvider.GetImage("dunergnclk", "background");
           

            //тут такая механика.
            //используем DI и атрибут [ObjectCreator.UseCtor], тогда world будет заполнен . 
            //после идем в коллекцию Sequences , которая собирается из всех rules\sequences, где мы в misc.yaml прописали наш screen.cps
            //берем sprite из этих sequences и используем его Sheet, как ссылку для создания других Sprite в нашем UI.
            //video = new CpsD2Loader("SCREEN.CPS");

            LoadPalette();
        }
        public override void Initialize(WidgetArgs args)
        {
            base.Initialize(args);
            if (Game.Renderer.PixelDumpRenderer.fbcreated == false)
            {
                Game.Renderer.PixelDumpRenderer.Setup(new Size(1024, 512));
           
                PrepTextures();
            }
        }
        void LoadPalette(ImmutablePalette cpspalette, string customname)
        {
            Game.worldRenderer.AddPalette("dune2widget", cpspalette, false, false);
            pr = Game.worldRenderer.Palette("dune2widget");
        }
        void LoadPalette()
        {
            //pr = Game.worldRenderer.Palette("player" + Game.worldRenderer.World.LocalPlayer.InternalName); //d2 палитра назначена в d2\rules\palettes.yaml
            prbase = Game.worldRenderer.Palette("d2");
        }
        public void PrepTextures()
        {
            Game.Renderer.Flush();
            Game.Renderer.PixelDumpRenderer.fb.Bind();
            //Sheet seqsheet;
            //seqsheet = Game.ModData.DefaultSequences["arrakis2"].SpriteCache.SheetBuilder2D.Current;
            //seqsheet = Game.SheetBuilder2D.Current;
            //Sprite sp = new Sprite(seqsheet, RenderBounds, TextureChannel.Red); //чтобы прочитать все 4 канала seqsheet
            //                                                                                                                           //нужно использовать 4 итерации, где нужно менять канал в спрайте.

            //Game.Renderer.PixelDumpRenderer.shader.SetTexture("Texture2D0", dunergnSprite.Sheet2D.texture);
            //Game.Renderer.PixelDumpRenderer.shader.SetTexture("Texture2D1", dunergnclickSprite.Sheet2D.texture);
            //Game.Renderer.PixelDumpRenderer.DrawSprite(dunergnSprite, new float3(0, 0, 0));
            Game.Renderer.PixelDumpRenderer.DrawSprite(dunergnclickSprite, new float3(0, 0, 0), new float3(RenderBounds.Width, RenderBounds.Height, 0));
            //Game.Renderer.PixelDumpRenderer.SetMouseLocation(MouseLocationInWidget);

            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            Game.Renderer.PixelDumpRenderer.fb.Unbind();

            Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size);
            Game.Renderer.PixelDumpRenderer.DrawSprite(dunergnSprite, new float3(0, 0, 0), new float3(RenderBounds.Width, RenderBounds.Height, 0));
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            Game.Renderer.PixelDumpRenderer.fb.Unbind();

        }

        public override void Draw()
        {
           
            //Game.Renderer.SetPalette(hardwarePalette);
            int offsetY = 0;
            int offsetX = 0;
            Rectangle temprect = new Rectangle();
            Rectangle mouserect = GetEventBounds();
            //Console.WriteLine("mouse loc:" + MouseLocationInWidget);
            //dunergnclickSprite.SpriteType = 0; //fill rect with palette image


            Game.Renderer.SpriteRenderer.Flush();

            //делаем спрайт по текстуре 1, в которой карта с регионами
            Sheet sh1 = new Sheet(SheetType.BGRA, Game.Renderer.PixelDumpRenderer.fb.Texture[0]);
            Sprite sp2 = new Sprite(sh1, new Rectangle(0, 0, RenderBounds.Width, RenderBounds.Height), TextureChannel.RGBA);
            float normX, normY;
            //соблюдаем размерность между источником текстур SeqProv и фреймбуфером.
            normX = -1* (float)MouseLocationInWidget.X / 1024; //2048 на 2048 это размер текстуры в которой хранятся пиксели от фреймбуфера
            normY = 1+(float)MouseLocationInWidget.Y / 512;
          
            Game.Renderer.SpriteRenderer.SetMouseLocation(new float2(normX, normY));
            
            //передаем вторым аргументом текстуру, где регионы для мышки
            Game.Renderer.SpriteRenderer.shader.SetTexture("Texture1", Game.Renderer.PixelDumpRenderer.fb.Texture[1]); //rgnclck
            sp2.SpriteType = 6;
            WidgetUtils.FillRectWithSprite(RenderBounds, sp2, prbase); //dunergn
            Game.Renderer.SpriteRenderer.Flush(); //записать кадр во фреймбуфер 

            if (HasMouseFocus)
            {
                ReadPixelUnderMouse();
            }
            else

            {
                //WidgetUtils.FillRectWithSprite(RenderBounds, dunergnSprite, prbase);
                //return;
            }

        }
        public void ReadPixelUnderMouse()
        {
            Size s = Game.Renderer.Resolution;
            s = new Size(1, 1);
            var raw = new byte[s.Width * s.Height * 4];

            unsafe
            {
                fixed (byte* pRaw = raw)
                    OpenGL.glReadPixels(RenderOrigin.X  - 1 * MouseLocationInWidget.X , Game.Renderer.Resolution.Height - RenderOrigin.Y + 1 * MouseLocationInWidget.Y , s.Width, s.Height,
                        OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, (IntPtr)pRaw);
            }
            Console.WriteLine(raw[2].ToString() + "." + raw[1].ToString() + "." + raw[0].ToString());
        }

        public bool Depressed;
        int2 MouseLocationInWidget;

        public override bool HandleMouseInput(MouseInput mi)
        {
            if (RenderBounds.Contains(mi.Location))
            {
                MouseLocationInWidget = RenderOrigin - mi.Location;
            }
            if (mi.Button != MouseButton.Left)
                return false;

            if (mi.Event == MouseInputEvent.Down && !TakeMouseFocus(mi))
                return false;
            else if (HasMouseFocus && mi.Event == MouseInputEvent.Up)
            {
                // Only fire the onMouseUp event if we successfully lost focus, and were pressed
                // if (Depressed )
                // OnMouseUp(mi);
                return YieldMouseFocus(mi);
            }

            if (mi.Event == MouseInputEvent.Down)
            {
                // OnMouseDown returns false if the button shouldn't be pressed

                // OnMouseDown(mi);
                Depressed = true;
            }
            else if (mi.Event == MouseInputEvent.Move && HasMouseFocus)
                Depressed = RenderBounds.Contains(mi.Location);

            return Depressed;
        }
    }
}
