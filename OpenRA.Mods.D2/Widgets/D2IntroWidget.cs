using OpenRA;
using OpenRA.Graphics;
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
    public class D2IntroWidget : Widget
    {
        Size textsize = new Size(Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height);
        public Hotkey CancelKey = new Hotkey(Keycode.ESCAPE, Modifiers.None);
        Action onComplete;
        public World world = Game.worldRenderer.World;
        FrameBuffer iternalFB;
        ITextureInternal text25;
        ITextureInternal text26;
        ITextureInternal text27;
        private ITextureInternal text28;
        Sprite sp25;
        private Sprite sp26;
        Sprite sp28;
        Animation anim1, anim2;
        PaletteReference pr;
        float2 videoOrigin, videoSize;
        public float AspectRatio = 1.1f;
        private float2 MouseLocationInWidget;

        public D2IntroWidget()
        {
            iternalFB = FrameBuffer.SetupOrGetOneMoreFrameBuffer(textsize, "fbD2Intro", 32);
            iternalFB.Texture[0].ScaleFilter = TextureScaleFilter.Linear;
            text25 = (ITextureInternal)iternalFB.Texture[0];

            text26 = (ITextureInternal)iternalFB.BindThisFrameBufferWithNewTexture(true, textsize, 32);
            text27 = (ITextureInternal)iternalFB.BindThisFrameBufferWithNewTexture(true, textsize, 32);
            text28 = (ITextureInternal)iternalFB.BindThisFrameBufferWithNewTexture(true, textsize, 32);

            Sheet sh3;
            sh3 = new Sheet(SheetType.BGRA, text25);
            sp25 = new Sprite(sh3, new Rectangle(0, 0, textsize.Width, textsize.Height), TextureChannel.RGBA);
            sp25.SpriteType = 14;

            Sheet sh4;
            sh4 = new Sheet(SheetType.BGRA, text26);
            sp26 = new Sprite(sh4, new Rectangle(0, 0, textsize.Width, textsize.Height), TextureChannel.RGBA);
            sp26.SpriteType = 14;

            Sheet sh5;
            sh5 = new Sheet(SheetType.BGRA, text28);
            sp28 = new Sprite(sh5, new Rectangle(0, 0, textsize.Width, textsize.Height), TextureChannel.RGBA);
            sp28.SpriteType = 14;

            onComplete = () => ShowMainMenu(world);

            string filename = "INTRO1.WSA";

            if (filename.Contains("WSA"))
            {
                if (filename.Contains("INTRO"))
                {
                    LoadPalette();
                }
                else
                {
                    LoadPaletteWSA();
                }
                anim1 = new Animation(world, filename);
                anim1.ChangeSequenceGroup(filename.Replace(".", ""));
                anim1.Play("play");

            }

        }
        public void SetSize(int w, int h)
        {
            var size = Math.Max(w, h);
            var textureSize = Exts.NextPowerOf2(size);

            var scale = Math.Min((float)RenderBounds.Width / w, (float)RenderBounds.Height / h * AspectRatio);
            videoOrigin = new float2(
                RenderBounds.X + (RenderBounds.Width - scale * w) / 2,
                RenderBounds.Y + (RenderBounds.Height - scale * h * AspectRatio) / 2);

            // Round size to integer pixels. Round up to be consistent with the scale calculation.
            videoSize = new float2((int)Math.Ceiling(w * scale), (int)Math.Ceiling(h * AspectRatio * scale));
        }
        void LoadPalette()
        {
            pr = Game.worldRenderer.Palette("d2"); //d2 палитра назначена в d2\rules\palettes.yaml
        }
        void LoadPaletteWSA()
        {
            pr = Game.worldRenderer.Palette("westwood"); //d2 палитра назначена в d2\rules\palettes.yaml
        }
        public override bool HandleKeyPress(KeyInput e)
        {
            if (Hotkey.FromKeyInput(e) != CancelKey || e.Event != KeyInputEvent.Down)
                return false;

            Stop();
            return true;
        }

        public override bool HandleMouseInput(MouseInput mi)
        {
            if (mi.Button == MouseButton.Left)
            {
                if (RenderBounds.Contains(mi.Location))
                {
                    MouseLocationInWidget = mi.Location; //берем относительно FB координаты, а не виджета. Хотя если windget в fullscreen то разницы 0.
                    //MouseLocationInWidget = RenderOrigin - mi.Location;
                }
            }
            return true;
            //return base.HandleMouseInput(mi);
        }

        void ShowMainMenu(World world)
        {

            //promptAccepted = true;
            Ui.ResetAll();
            Ui.CloseWindow();
            Game.LoadWidget(world, "MAINMENU", Ui.Root, new WidgetArgs());
        }

        public void Stop()
        {

            Game.RunAfterTick(onComplete);
        }

        public override void Tick()
        {


        }
        public override void Draw()
        {
            Game.Renderer.Flush();
            //не имеет значения, так как текстура уже создана в WorldRendere по размеру карты
            iternalFB.ReBind(text25);
            Game.Renderer.sproc.SetViewportParams(textsize, 0f, 0f, 1f, int2.Zero);
            Game.Renderer.sproc.AddCommand(9, 0, 0, Game.LocalTick, 0, new int2(textsize.Width, textsize.Height), new float3(0, 0, 0),
                new float3(textsize.Width, textsize.Height, 0),
                sp26, null);
            Game.Renderer.sproc.ExecCommandBuffer2();

            //iternalFB.ReBind(text26);

            anim1.Tick();
            //Game.Renderer.SpriteRenderer.DrawSprite(anim1.Image, videoOrigin, pr, videoSize);

            Game.Renderer.sproc.SetViewportParams(textsize, 0f, 0f, 1f, int2.Zero);
            iternalFB.ReBind(text27); //пишем в 27 анимацию из wsa
                                      //SetSize(anim1.Image.Size.XY.ToInt2().X, anim1.Image.Size.XY.ToInt2().Y);
                                      //Game.Renderer.SpriteRenderer.DrawSprite(anim1.Image, new float3(videoOrigin.X - 15, videoOrigin.Y, 0), Game.worldRenderer.Palette("d2"), videoSize);
                                      //Game.Renderer.Flush();
            Game.Renderer.sproc.SetTexture("Texture4", text28); //отадем точки-text28 в шейдер 11
            Game.Renderer.sproc.AddCommand(14, 0, 0, 0, 0, new int2(0, 0), new float3(0, 0, 0), new int2(textsize.Width, textsize.Height), anim1.Sprite, Game.worldRenderer.Palette("d2"));
            Game.Renderer.sproc.ExecCommandBuffer2();


            //если был клик по области, то взять text27 и записать в текстуру по нужной координате черный квадратик. Эти самым разрушив целостность картинки.
            float mouseX, mouseY;
            //if (MouseLocationInWidget.X > 0) //text27 перезаписывается всегда заново, стирая наш клик.
            {
                iternalFB.ReBind(text28, false);
                mouseX = MouseLocationInWidget.X;
                mouseY = MouseLocationInWidget.Y;
                Game.Renderer.sproc.SetMouseLocation(mouseX, 768f - mouseY, 0, 0); //zw - click, xy-mouse down
                Game.Renderer.sproc.SetViewportParams(textsize, 0f, 0f, 1f, int2.Zero);
                Game.Renderer.sproc.AddCommand(13, 0, 0, Game.LocalTick, 0, new int2(textsize.Width, textsize.Height), new float3(0, 0, 0),
                    new float3(textsize.Width, textsize.Height, 0),
                    null, null); //sp26 для примера. она будет в Texture3 слоте
                Game.Renderer.sproc.ExecCommandBuffer2();
                //Game.Renderer.sproc.SetMouseLocation(0, 0, 0, 0);
                //MouseLocationInWidget = new int2(0, 0); //handled через зануление
            }

            //{
            //    //+сюда sp28 через обычный DrawSprite нарисовать в text27 с помощью discard рисовать только помеченные пиксели, остальные не трогать.
            //    iternalFB.ReBind(text27); //пишем в 27 анимацию из wsa
            //    Game.Renderer.sproc.SetViewportParams(textsize, 0f, 0f, 1f, int2.Zero);
            //    Game.Renderer.sproc.AddCommand(14, 0, 0, 0, 0, new int2(textsize.Width, textsize.Height), new float3(0, 0, 0),
            //        new float3(textsize.Width, textsize.Height, 0), sp28, null);
            //    Game.Renderer.sproc.ExecCommandBuffer2();
            //}

            iternalFB.ReBind(text26);
            Game.Renderer.sproc.SetTexture("Texture5", text27); //отадем wsa рендер-text27 в шейдер 11
          
            Game.Renderer.sproc.AddCommand(11, 0, 0, Game.LocalTick, 0, new int2(textsize.Width, textsize.Height), new float3(0, 0, 0),
                new float3(textsize.Width, textsize.Height, 0),
                sp25, null);
            Game.Renderer.sproc.ExecCommandBuffer2();

            iternalFB.Unbind();
            //iternalFB.PixelDumpRenderer.SetViewportParams(textsize, 0f, 0f, 1f, int2.Zero);


            //пишет уже в FB0 результат
            //iternalFB.PixelDumpRenderer.SetViewportParams(textsize, 0f, 0f, 1f, int2.Zero);
        
            Game.Renderer.sproc.AddCommand(10, 0, 0, Game.LocalTick, 0, new int2(textsize.Width, textsize.Height), new float3(0, 0, 0),
               new float3(textsize.Width, textsize.Height, 0),
               sp26, null);
            Game.Renderer.sproc.ExecCommandBuffer2();
            //Game.Renderer.SpriteRenderer.DrawSprite(sp, new float3(0, 0, 0), Game.worldRenderer.Palette("d2"));

            //был видимо код для debug.
            //iternalFB.ReBind(text25);
            //Game.Renderer.sproc.SetViewportParams(textsize, 0f, 0f, 1f, int2.Zero);
            //Game.Renderer.sproc.AddCommand(12, 0, 0, Game.LocalTick, 0, new int2(textsize.Width, textsize.Height), new float3(0, 0, 0),
            //    new float3(textsize.Width, textsize.Height, 0),
            //    sp26, null);
            //Game.Renderer.sproc.ExecCommandBuffer2();


            //iternalFB.Unbind();
        }
    }
}
