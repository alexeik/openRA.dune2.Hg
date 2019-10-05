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
        private Sprite bg_sprite;
        private Sprite but1_sprite;
        private Sprite but2_sprite;
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
                Png screen_as_png;
                screen_as_png = screen_cps_sprite.Sheet.AsPng(TextureChannel.Blue, hardwarePalette.GetPalette("chrome"));
                stolb_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(241, 142, 11,39), TextureChannel.Red);
                stolb_top_sprite= new Sprite(screen_cps_sprite.Sheet, new Rectangle(241, 136, 11, 5), TextureChannel.Red);
                stolb_bot_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(241, 182, 11, 4), TextureChannel.Red);
                stolb_shpere_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(241, 124, 11, 10), TextureChannel.Red);
                stolb_line_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(241, 135, 11, 1), TextureChannel.Red);
                stolb_horiz_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(259, 127, 55, 5), TextureChannel.Red);
                stolb_horiz_left_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(254, 127, 4, 5), TextureChannel.Red);

                bg_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(0, 0, 15, 16), TextureChannel.Red);
                but1_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(16,1, 78, 15), TextureChannel.Red);
                but2_sprite = new Sprite(screen_cps_sprite.Sheet, new Rectangle(104, 1, 78, 15), TextureChannel.Red);

                int offsetX = 768; //75%
                rect_stold_sphere = new Rectangle(offsetX, 0+50, 11, 10);
                rect_line = new Rectangle(offsetX, 10+50, 11, 1);
                rect_stold_top = new Rectangle(offsetX, 12+50, 11, 5);
                rect_stold = new Rectangle(offsetX, 18+50, 11, 558-50);
                rect_stold_bot = new Rectangle(offsetX, 572, 11, 4);
                rect_line_2 = new Rectangle(offsetX, 576, 11, 1);
                rect_stold_sphere_2 = new Rectangle(offsetX, 577, 11, 10);
                rect_stold_top_3 = new Rectangle(offsetX, 589, 11, 5);
                rect_stold_2 = new Rectangle(offsetX, 594, 11, 160);
                rect_stold_bot_2 = new Rectangle(offsetX, 594+160+1, 11, 4);
                rect_stold_sphere_3 = new Rectangle(offsetX, 594+160+4+1, 11, 10);

                rect_stold_horiz = new Rectangle(offsetX+13+4, 580 ,  444, 5);
                rect_stold_left_horiz = new Rectangle(offsetX+13, 580 , 4, 5);

                rect_bg = new Rectangle(0, 0, 1024,50);
                rect_but1 = new Rectangle(30, 15, 78, 15);
                rect_but2 = new Rectangle(30+100, 15, 78, 15);
            }
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
        Rectangle rect_stold;
        Rectangle rect_stold_top;
        private Rectangle rect_stold_bot;
        private Rectangle rect_line_2;
        private Rectangle rect_stold_sphere_2;
        private Rectangle rect_stold_top_3;
        private Rectangle rect_stold_2;
        private Rectangle rect_stold_bot_2;
        private Rectangle rect_stold_sphere_3;
        private Rectangle rect_stold_horiz;
        private Rectangle rect_stold_left_horiz;
        private Rectangle rect_bg;
        private Rectangle rect_but1;
        private Rectangle rect_but2;
        private Rectangle rect_stold_sphere;
        private Rectangle rect_line;

        public override void Draw()
        {
            Game.Renderer.SetPalette(hardwarePalette);
            WidgetUtils.FillRectWithSprite(rect_stold, stolb_sprite, pr);
            WidgetUtils.FillRectWithSprite(rect_stold_top, stolb_top_sprite, pr);
            WidgetUtils.FillRectWithSprite(rect_stold_bot, stolb_bot_sprite, pr);
            WidgetUtils.FillRectWithSprite(rect_stold_sphere, stolb_shpere_sprite, pr);
            WidgetUtils.FillRectWithSprite(rect_line, stolb_line_sprite, pr);
            WidgetUtils.FillRectWithSprite(rect_stold_sphere_2, stolb_shpere_sprite, pr);
            WidgetUtils.FillRectWithSprite(rect_stold_top_3, stolb_top_sprite, pr);
            WidgetUtils.FillRectWithSprite(rect_stold_2, stolb_sprite, pr);
            WidgetUtils.FillRectWithSprite(rect_stold_bot_2, stolb_bot_sprite, pr);
            WidgetUtils.FillRectWithSprite(rect_stold_sphere_3, stolb_shpere_sprite, pr);

            WidgetUtils.FillRectWithSprite(rect_stold_left_horiz, stolb_horiz_left_sprite, pr);
            WidgetUtils.FillRectWithSprite(rect_stold_horiz, stolb_horiz_sprite, pr);

            WidgetUtils.FillRectWithSprite(rect_bg, bg_sprite, pr);

            WidgetUtils.FillRectWithSprite(rect_but1, but1_sprite, pr);
            WidgetUtils.FillRectWithSprite(rect_but2, but2_sprite, pr);
        }
    }
}
