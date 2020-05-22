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
        private Sprite housesSprite;
        private Sprite housesmaskSprite;
        private SequenceProvider sp;
        private ITexture textemp;
        Sheet sh1;
        Sprite sp1;

        Sheet sh2;
        Sprite sp2;
        public Action<int, int, int> OnHouseChooseDelegate;
        public Action<int, int, int> OnMapRegionChooseDelegate;
        /// <summary>
        /// Если использовать [ObjectCreator.UseCtor] , то можно использовать DI для инициализации аргументов конструктора.
        /// </summary>
        /// <param name="world"></param>
        [ObjectCreator.UseCtor]
        public CampaignWidget(World world)
        {
            dunergnSprite = ChromeProvider.GetImage("dunergn", "background");
            dunergnclickSprite = ChromeProvider.GetImage("dunergnclk", "background");
            housesSprite = ChromeProvider.GetImage("housestitle", "background");
            housesmaskSprite = ChromeProvider.GetImage("heraldmask", "background");

            //тут такая механика.
            //используем DI и атрибут [ObjectCreator.UseCtor], тогда world будет заполнен . 
            //после идем в коллекцию Sequences , которая собирается из всех rules\sequences, где мы в misc.yaml прописали наш screen.cps
            //берем sprite из этих sequences и используем его Sheet, как ссылку для создания других Sprite в нашем UI.
            //video = new CpsD2Loader("SCREEN.CPS");
            sp = world.Map.Rules.Sequences;
            LoadPalette();

            //OnHouseChooseDelegate = OnHouseChoose;
            //OnMapRegionChooseDelegate = OnMapRegionChoose;
            //Layer1KeyColors = new float[255];
            Layer1KeyColors =new float[12]{ 0, 170f/255, 0, 1, 170f / 255, 0, 170f / 255, 1,  0, 170f/255, 170f /255,1};
            Layer2KeyColors = new float[12]{ 186f/255, 190f/255, 150f/255, 1, 174f / 255, 174f/255, 138f/ 255, 1, 158f/255,158f/255,121f/255,1};
            Layer3KeyColors = new float[12] { 255f / 255, 85f / 255, 85f / 255, 1, 0f / 255, 0f / 255, 0f / 255, 1, 203f / 255, 207f / 255, 162f / 255, 1 };
            Layer1Color = new float[4] { 153f/255, 0f, 0f, 1f };
            Layer2Color = new float[4] { 24f/255, 125f/255, 24f/255, 1f };
            Layer3Color = new float[4] { 40f/255, 60f/255, 153f/255, 1f };
        }

        public override void Initialize(WidgetArgs args)
        {
            base.Initialize(args);
            if (Game.Renderer.PixelDumpRenderer.fbcreated == false)
            {
                Game.Renderer.PixelDumpRenderer.Setup(new Size(1024, 768)); //widget должен быть , тогда в пределах 1024 на 512 пикселей
           
                PrepTextures();
            }
            else
            {
                //переинициализация этих переменных на N-1 раз
                sh1 = new Sheet(SheetType.BGRA, Game.Renderer.PixelDumpRenderer.fb.Texture[0]);
                sp1 = new Sprite(sh1, new Rectangle(0, 0, RenderBounds.Width, RenderBounds.Height), TextureChannel.RGBA);
                sh2 = new Sheet(SheetType.BGRA, Game.Renderer.PixelDumpRenderer.fb.Texture[3]);
                sp2 = new Sprite(sh2, new Rectangle(0, 0, RenderBounds.Width, RenderBounds.Height), TextureChannel.RGBA);
                textemp = Game.Renderer.PixelDumpRenderer.fb.Texture[4]; //ссылка на текстуру, куда будут уходить данные
                                                                         // об выбранных масках
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
            prbase = Game.worldRenderer.Palette("shroud");
            //используем shroud так как у нее нету SHadowIndex в palettes.yaml, этот ShadowIndex затирает указанный индекс в палитре черным цветом и 
            //я получал не верные цвета.
        }
        /// <summary>
        /// КОнвертирует все 1 байтовые пиксели в 4 байтовые, чтобы работать с этими картинками в шейдере в drawmode=10
        /// </summary>
        public void PrepTextures()
        {
            
            Game.Renderer.Flush();
            Game.Renderer.PixelDumpRenderer.fb.Bind();

            Game.Renderer.PixelDumpRenderer.DrawSprite(dunergnclickSprite, new float3(0, 0, 0), new float3(RenderBounds.Width, RenderBounds.Height, 0),prbase);

            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            Game.Renderer.PixelDumpRenderer.fb.Unbind();

            Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size);
            Game.Renderer.PixelDumpRenderer.DrawSprite(dunergnSprite, new float3(0, 0, 0), new float3(RenderBounds.Width, RenderBounds.Height, 0), prbase);
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            Game.Renderer.PixelDumpRenderer.fb.Unbind();

            Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size);
            Game.Renderer.PixelDumpRenderer.DrawSprite(housesSprite, new float3(0, 0, 0), new float3(RenderBounds.Width, RenderBounds.Height, 0), prbase);
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            Game.Renderer.PixelDumpRenderer.fb.Unbind();

            Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size);
            Game.Renderer.PixelDumpRenderer.DrawSprite(housesmaskSprite, new float3(0, 0, 0), new float3(RenderBounds.Width, RenderBounds.Height, 0), prbase);
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            Game.Renderer.PixelDumpRenderer.fb.Unbind();

            //переходим в размеры текстур как у игры! 
            Game.Renderer.PixelDumpRenderer.fb.Bind(true,new Size(1024,768)); 
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            Game.Renderer.PixelDumpRenderer.fb.Unbind();

            textemp = Game.Renderer.PixelDumpRenderer.fb.Texture[4]; //ссылка на текстуру, куда будут уходить данные
                                                                     // об выбранных масках

            sh1 = new Sheet(SheetType.BGRA, Game.Renderer.PixelDumpRenderer.fb.Texture[0]);
            sp1 = new Sprite(sh1, new Rectangle(0, 0, RenderBounds.Width, RenderBounds.Height), TextureChannel.RGBA);
            sh2 = new Sheet(SheetType.BGRA, Game.Renderer.PixelDumpRenderer.fb.Texture[3]);
            sp2 = new Sprite(sh2, new Rectangle(0, 0, RenderBounds.Width, RenderBounds.Height), TextureChannel.RGBA);

        }
        public void DrawHouses()
        {
            Game.Renderer.SpriteRenderer.Flush();

            //делаем спрайт по текстуре 1, в которой оригинал

            float normX, normY;
            //соблюдаем размерность между источником текстур SeqProv и фреймбуфером.
            normX = -1 * (float)MouseLocationInWidget.X / 1024; //2048 на 2048 это размер текстуры в которой хранятся пиксели от фреймбуфера
            normY = 1 + (float)MouseLocationInWidget.Y / 768;

            Game.Renderer.SpriteRenderer.SetMouseLocation(new float2(normX, normY));
            Game.Renderer.SpriteRenderer.SetAlphaFlag(true);
            Game.Renderer.SpriteRenderer.SetAlphaInit(60,0,0,0);
            Game.Renderer.SpriteRenderer.SetAlphaConstantRegion(255,255,85,255);

            //передаем вторым аргументом текстуру, где маска
            Game.Renderer.SpriteRenderer.shader.SetTexture("Texture1", Game.Renderer.PixelDumpRenderer.fb.Texture[2]); //rgnclck
            sp2.SpriteType = 7;
            WidgetUtils.FillRectWithSprite(RenderBounds, sp2, prbase); //dunergn
            //Game.Renderer.SpriteRenderer.Flush(); //записать кадр во фреймбуфер 
        }
        public bool SwitchToMap = false;
        public float[] Layer1KeyColors;
        private float[] Layer2KeyColors;
        private float[] Layer3KeyColors;
        private float[] Layer1Color;
        private float[] Layer2Color;
        private float[] Layer3Color;

        public void DrawMap()
        {
            float normX, normY;
            //соблюдаем размерность между источником текстур SeqProv и фреймбуфером.
            normX = -1 * (float)MouseLocationInWidget.X / 1024; //2048 на 2048 это размер текстуры в которой хранятся пиксели от фреймбуфера
            normY = 1 + (float)MouseLocationInWidget.Y / 768;
            //Game.Renderer.SpriteRenderer.SetAlphaFlag(false);
           // Game.Renderer.SpriteRenderer.SetAlphaConstantRegion(-1, 255, 85, 255);
            Game.Renderer.SpriteRenderer.SetMouseLocation(new float2(normX, normY));
            //Game.Renderer.SpriteRenderer.SetLayer1KeyColor(0,170,0,255);
            Game.Renderer.SpriteRenderer.shader.SetVec("Layer1KeyColor", Layer1KeyColors, 4, Layer1KeyColors.Length/4);
            Game.Renderer.SpriteRenderer.shader.SetVec("Layer1Color", Layer1Color, 4);
            Game.Renderer.SpriteRenderer.shader.SetVec("Layer2KeyColor", Layer2KeyColors, 4, Layer2KeyColors.Length / 4);
            Game.Renderer.SpriteRenderer.shader.SetVec("Layer2Color", Layer2Color, 4);
            Game.Renderer.SpriteRenderer.shader.SetVec("Layer3KeyColor", Layer3KeyColors, 4, Layer3KeyColors.Length / 4);
            Game.Renderer.SpriteRenderer.shader.SetVec("Layer3Color", Layer3Color, 4);
            //передаем вторым аргументом текстуру, где регионы для мышки
            Game.Renderer.SpriteRenderer.shader.SetTexture("Texture1", Game.Renderer.PixelDumpRenderer.fb.Texture[1]); //rgnclck
            sp1.SpriteType = 6;
            WidgetUtils.FillRectWithSprite(RenderBounds, sp1, prbase); //dunergn
           // Game.Renderer.SpriteRenderer.Flush(); //записать кадр во фреймбуфер 
        }
        public override void Draw()
        {
            if (SwitchToMap)
            {
                DrawMap();
            }
            else
            {
                DrawHouses();
            }
           
            //return;
            //Game.Renderer.SpriteRenderer.SetFrameBufferMaskMode(false);
            //DrawMap();

            Game.Renderer.SpriteRenderer.Flush();

            //делаем спрайт по текстуре 1, в которой карта с регионами
           
          
           

            if (Clicked)
            {
                Game.Renderer.PixelDumpRenderer.fb.ReBind((ITextureInternal)textemp);
                Game.Renderer.SpriteRenderer.SetFrameBufferMaskMode(true);
                if (SwitchToMap)
                {
                    DrawMap();
                }
                else
                {
                    DrawHouses();
                }
                Game.Renderer.SpriteRenderer.Flush();
                //Game.Renderer.PixelDumpRenderer.Flush();
                byte[] answer= ReadPixelUnderMouse();
                if (SwitchToMap)
                {
                    OnMapRegionChooseDelegate(answer[2], answer[1], answer[0]);
                }
                else
                {
                    OnHouseChooseDelegate(answer[2], answer[1], answer[0]);
                }
                Game.Renderer.SpriteRenderer.SetFrameBufferMaskMode(false);
                Game.Renderer.PixelDumpRenderer.fb.Unbind();
                SwitchToMap = true;
                Clicked = false;
            }
            else

            {
                //WidgetUtils.FillRectWithSprite(RenderBounds, dunergnSprite, prbase);
                //return;
            }

        }

        public byte[] ReadPixelUnderMouse()
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
            Console.WriteLine(raw[2].ToString() + "." + raw[1].ToString() + "." + raw[0].ToString() + raw[3].ToString());
            return raw;
            
        }

        public bool Depressed;
        int2 MouseLocationInWidget;
        public bool Clicked;
        public override bool HandleMouseInput(MouseInput mi)
        {
            if (RenderBounds.Contains(mi.Location))
            {
                MouseLocationInWidget = RenderOrigin - mi.Location;
            }
            Clicked = false;
            if (mi.Button != MouseButton.Left)
                return false;

            if (mi.Event == MouseInputEvent.Down && !TakeMouseFocus(mi))
                return false;
            else if (HasMouseFocus && mi.Event == MouseInputEvent.Up)
            {
                // Only fire the onMouseUp event if we successfully lost focus, and were pressed
                // if (Depressed )
                // OnMouseUp(mi);
                Clicked = true;
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
