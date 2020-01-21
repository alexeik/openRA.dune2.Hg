using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.D2.FileFormats;
using OpenRA.Mods.D2.SpriteLoaders;
using OpenRA.Primitives;
using OpenRA.Widgets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.D2.Widgets
{
    public class D2GameScreenWidget : Widget
    {
        CpsD2Loader image = null;
        TypeDictionary metadata;
        ImmutablePalette cpspalette;
        CpsD2Loader video;
        ISpriteFrame[] imageSprite = null;
        ImmutablePalette palette;
        HardwarePalette hardwarePalette;
        PaletteReference pr;
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
        private Sprite bu1_sprite;

        public D2GameScreenWidget()
        {
            video = new CpsD2Loader("SCREEN.CPS");
          
       
            using (var stream = Game.ModData.DefaultFileSystem.Open(video.SpriteFilename))
            {

               
                video.TryParseSpritePlusPalette(stream, out imageSprite, out metadata, out cpspalette);
                if (cpspalette != null)
                {
                    LoadPalette(cpspalette, image.SpriteFilename);
                }
                else
                {
                    LoadPalette();
                }

                var sheetBuilder = new SheetBuilder(SheetType.Indexed, 512);
                Sprite screen_cps_sprite = null;
                screen_cps_sprite = sheetBuilder.Add(imageSprite[0]);
                screen_cps_sprite.Sheet.CreateBuffer();
                screen_cps_sprite.Sheet.ReleaseBuffer();
                //Png screen_as_png;
                //screen_as_png = screen_cps_sprite.Sheet.AsPng(TextureChannel.Blue, hardwarePalette.GetPalette("chrome"));

                stolb_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(241, 142, 12,39), TextureChannel.Red);
                stolb_top_sprite= new Sprite(screen_cps_sprite.Sheet, new Rectangle(241, 136, 12, 5), TextureChannel.Red);
                stolb_bot_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(241, 182, 12, 4), TextureChannel.Red);
                stolb_shpere_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(241, 124, 12, 11), TextureChannel.Red);
                stolb_line_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(241, 135, 12, 1), TextureChannel.Red);

                stolb_horiz_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(259, 127, 55, 6), TextureChannel.Red);
                stolb_horiz_left_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(254, 127, 5, 6), TextureChannel.Red);
                stolb_horiz_right_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(315, 127, 5, 6), TextureChannel.Red);

                bg_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(0, 0, 15, 16), TextureChannel.Red);
                but1_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(16,1, 78, 15), TextureChannel.Red);
                but2_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(104, 1, 78, 15), TextureChannel.Red);
                credits_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(201, 1, 118, 15), TextureChannel.Red);

                status_l_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(0, 17, 8, 22), TextureChannel.Red);
                status_horiz_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(8, 17,303, 22), TextureChannel.Red);
                status_r_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(312, 17, 8, 22), TextureChannel.Red);

                vert_line_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(240, 40, 240, 123), TextureChannel.Red);
                vertbord_line_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(254, 134, 1,65), TextureChannel.Red);

                int screenwidth = 0;
                int screenH = 0;
                screenH = Game.Settings.Graphics.WindowedSize.Y;
                screenwidth = Game.Settings.Graphics.WindowedSize.X;
 
                int offsetX; // 776 + 17; //75%
                offsetX = screenwidth - 231;//
  
                luiComs = new List<uiCom>();
                uiCom uic = new uiCom(new Rectangle(offsetX,31,12,11), stolb_shpere_sprite);
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(offsetX, 0, 12, 1), stolb_line_sprite); //12
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(offsetX, 0, 12, 5), stolb_top_sprite); //17
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(offsetX, 0, 12, 538), stolb_sprite); //555
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(offsetX, 0, 12, 5), stolb_bot_sprite); //560
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(offsetX, 0, 12, 1), stolb_line_sprite); //561 
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(offsetX, 0, 12, 11), stolb_shpere_sprite); //572
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(offsetX, 0, 12, 5), stolb_top_sprite); //577
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(offsetX, 0, 12,screenH-47-577), stolb_sprite); 
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(offsetX, 0, 12, 5), stolb_bot_sprite); //17
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(offsetX, 0, 12, 1), stolb_line_sprite); //12
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(offsetX, 0, 12, 11), stolb_shpere_sprite);
                luiComs.Add(uic);

                uic = new uiCom(new Rectangle(offsetX+12, 595, 5, 6), stolb_horiz_left_sprite, false,true);
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(0, 595, 233, 6), stolb_horiz_sprite, false, true);
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(0, 595, 5, 6), stolb_horiz_right_sprite, false, true);
                luiComs.Add(uic);

                uic = new uiCom(new Rectangle(0, 0, screenwidth, 30), bg_sprite, false, false);
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(screenwidth-474, 8, 78, 15), but1_sprite, false, false);
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(screenwidth-374, 8, 78, 15), but2_sprite, false, false);
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(screenwidth-174, 8, 118, 15), credits_sprite, false, false);
                luiComs.Add(uic);

                uic = new uiCom(new Rectangle(5, 5, 8, 22), status_l_sprite, false, true,true);
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(0, 5, screenwidth - 574, 22), status_horiz_sprite, false, true);
                luiComs.Add(uic);
                uic = new uiCom(new Rectangle(0, 5, 8, 22), status_r_sprite, false, true);
                luiComs.Add(uic);
            }

        }
        public List<uiCom> luiComs;
        public class uiCom
        {
            public uiCom(Rectangle rect,Sprite spr)
            {
                this.rect = rect;
                this.spr = spr;
            }
            public uiCom(Rectangle rect, Sprite spr,bool moveY,bool moveX)
            {
                this.rect = rect;
                this.spr = spr;
                this.MoveY= moveY;
                this.MoveX = moveX;
            }
            public uiCom(Rectangle rect, Sprite spr, bool moveY, bool moveX,bool resetX)
            {
                this.rect = rect;
                this.spr = spr;
                this.MoveY = moveY;
                this.MoveX = moveX;
                this.ResetOffsetX = resetX;
            }
            public Rectangle rect;
            public Sprite spr;
            public bool MoveY=true;
            public bool MoveX = false;
            public bool ResetOffsetY = false;
            public bool ResetOffsetX = false;
        }

        void LoadPalette(ImmutablePalette cpspalette, string customname)
        {

            palette = cpspalette;
            hardwarePalette = new HardwarePalette();
            hardwarePalette.AddPalette(customname, palette, false);
            hardwarePalette.Initialize();
            Game.Renderer.SetPalette(hardwarePalette);
            var pal = hardwarePalette.GetPalette(customname);
            pr = new PaletteReference(customname + "ref", hardwarePalette.GetPaletteIndex(customname), pal, hardwarePalette);
        }
        void LoadPalette()
        {
            using (var stream = Game.ModData.DefaultFileSystem.Open("IBM.PAL"))
            {
                palette = new ImmutablePalette(stream, new int[] { });
            }

            hardwarePalette = new HardwarePalette();
            hardwarePalette.AddPalette("chrome", palette, false);
            hardwarePalette.Initialize();
            Game.Renderer.SetPalette(hardwarePalette);
            var pal = hardwarePalette.GetPalette("chrome");
            pr = new PaletteReference("chromeref", hardwarePalette.GetPaletteIndex("chrome"), pal, hardwarePalette);
        }


        public override void Draw()
        {
            Game.Renderer.SetPalette(hardwarePalette);
            int offsetY = 0;
            int offsetX = 0;
            Rectangle temprect = new Rectangle();
            foreach (uiCom u in luiComs)
            {

                temprect = u.rect;
                if (u.ResetOffsetX)
                {
                    offsetX = 0;
                }
                if (u.MoveX)
                {
                   
                    temprect.X += offsetX;
                }
                if (u.MoveY)
                {
                    temprect.Y += offsetY;
                }
                WidgetUtils.FillRectWithSprite(temprect, u.spr, pr);
                if (u.MoveY)
                {
                    offsetY = offsetY + u.rect.Height + u.rect.Y;
                }
                if (u.MoveX)
                {
                    offsetX = offsetX + u.rect.Width + u.rect.X;
                }
            }
           
        }
    }
}
