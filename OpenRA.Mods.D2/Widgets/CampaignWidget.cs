using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.D2.FileFormats;
using OpenRA.Mods.D2.SpriteLoaders;
using OpenRA.Platforms.Default;
using OpenRA.Primitives;
using OpenRA.Traits;
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

   
        private Sprite houseSprite;
        private Sprite dunergnSprite;
        private Sprite dunergnclickSprite;
        private Sprite housesSprite;
        private Sprite housesmaskSprite;
        private Sprite maproomSprite;
        private Sprite maproommaskSprite;
        private SequenceProvider sp;
        private ITexture textemp;
        Sheet sh1;
        Sprite sp1;

        Sheet sh2;
        Sprite sp2;
        private Sheet sh3;
        private Sprite sp3;
        public Action<string> OnHouseChooseDelegate;
        public Action<int, int, int> OnMapRegionChooseDelegate;
        public Action<string> DrawTextDelegate;
        public Action<int> BindLevelDelegate;
        public Action UpLevelDelegate;
        public Action DownLevelDelegate;
        public int CurrentLevel;

        List<FactionInfo> selectableFactions;
        FactionInfo CurrentFaction;
        public byte[] lastanswer;
        private ModData modData;
        /// <summary>
        /// Если использовать [ObjectCreator.UseCtor] , то можно использовать DI для инициализации аргументов конструктора.
        /// </summary>
        /// <param name="world"></param>
        [ObjectCreator.UseCtor]
        public CampaignWidget(World world, ModData modData)
        {
            this.modData = modData;
            dunergnSprite = ChromeProvider.GetImage("dunergn", "background");
            dunergnclickSprite = ChromeProvider.GetImage("dunergnclk", "background");
            housesSprite = ChromeProvider.GetImage("housestitle", "background");
            housesmaskSprite = ChromeProvider.GetImage("heraldmask", "background");
            maproomSprite = ChromeProvider.GetImage("maproom", "background");
            maproommaskSprite = ChromeProvider.GetImage("maproommask", "background");
            //тут такая механика.
            //используем DI и атрибут [ObjectCreator.UseCtor], тогда world будет заполнен . 
            //после идем в коллекцию Sequences , которая собирается из всех rules\sequences, где мы в misc.yaml прописали наш screen.cps
            //берем sprite из этих sequences и используем его Sheet, как ссылку для создания других Sprite в нашем UI.
            //video = new CpsD2Loader("SCREEN.CPS");
            sp = world.Map.Rules.Sequences;
            selectableFactions = world.Map.Rules.Actors["world"].TraitInfos<FactionInfo>()
                 .Where(f => f.Selectable == true)
                 .ToList();

            LoadPalette();

            //OnHouseChooseDelegate = OnHouseChoose;
            //OnMapRegionChooseDelegate = OnMapRegionChoose;
            //Layer1KeyColors = new float[255];
            Layer1KeyColors = new float[12];
            Layer2KeyColors = new float[12];
            Layer3KeyColors = new float[12];
            Layer4PickKeyColors = new float[12];
            //Layer1Color = new float[4] { 153f / 255, 0f, 0f, 1f };
            //Layer2Color = new float[4] { 24f / 255, 125f / 255, 24f / 255, 1f };
            //Layer3Color = new float[4] { 40f / 255, 60f / 255, 153f / 255, 1f };

            LoadData();

            float3 cc1 = cd.Players[0].RegionColor;
            Layer1Color = new float[4] { cc1.X / 255, cc1.Y / 255, cc1.Z / 255, 1 };
            cc1 = cd.Players[1].RegionColor;
            Layer2Color = new float[4] { cc1.X / 255, cc1.Y / 255, cc1.Z / 255, 1 };
            cc1 = cd.Players[2].RegionColor;
            Layer3Color = new float[4] { cc1.X / 255, cc1.Y / 255, cc1.Z / 255, 1 };


            //BindLevelOnMap(1);

            BindLevelDelegate = BindLevelOnMap;
            UpLevelDelegate = UpLevel;
            DownLevelDelegate = DownLevel;

        }
        public void UpLevel()
        {
            if (CurrentLevel+1>=1 && CurrentLevel+1 <= cd.Levels.Count)
            {
                CurrentLevel += 1;
                BindLevelOnMap(CurrentLevel);
            }
        }
        public void DownLevel()
        {
            if (CurrentLevel-1 >= 1 && CurrentLevel-1 <= cd.Levels.Count)
            {
                CurrentLevel -= 1;
                BindLevelOnMap(CurrentLevel);
            }
        }
        public void BindLevelOnMap(int Level)
        {
            CurrentLevel = Level;
            Level -= 1;
            DrawTextDelegate(String.Format("Level switched to {0}", CurrentLevel));

            Layer1KeyColors = new float[12];
            Layer2KeyColors = new float[12];
            Layer3KeyColors = new float[12];
            Layer4PickKeyColors = new float[12];
            int k = 0;

            Dictionary<float3, string> pr = cd.Levels[Level].PickRegions;
            foreach (var r in pr)
            {
                for (int i = 0; i < 1; i++, k += 4)
                {
                    Layer4PickKeyColors[k] = r.Key.X / 255;
                    Layer4PickKeyColors[k + 1] = r.Key.Y / 255;
                    Layer4PickKeyColors[k + 2] = r.Key.Z / 255;
                    Layer4PickKeyColors[k + 3] = 1;
                }
            }





            List<ReignRegion> rr = cd.Levels[Level].PlayersRegions[0].ReignRegions;
            k = 0;
            foreach (ReignRegion r in rr)
            {
                for (int i = 0; i < 1; i++, k += 4)
                {
                    Layer1KeyColors[k] = r.Color.X / 255;
                    Layer1KeyColors[k + 1] = r.Color.Y / 255;
                    Layer1KeyColors[k + 2] = r.Color.Z / 255;
                    Layer1KeyColors[k + 3] = 1;
                }
            }


            rr = cd.Levels[Level].PlayersRegions[1].ReignRegions;
            k = 0;
            foreach (ReignRegion r in rr)
            {
                for (int i = 0; i < 1; i++, k += 4)
                {
                    Layer2KeyColors[k] = r.Color.X / 255;
                    Layer2KeyColors[k + 1] = r.Color.Y / 255;
                    Layer2KeyColors[k + 2] = r.Color.Z / 255;
                    Layer2KeyColors[k + 3] = 1;
                }
            }
            rr = cd.Levels[Level].PlayersRegions[2].ReignRegions;
            k = 0;
            foreach (ReignRegion r in rr)
            {
                for (int i = 0; i < 1; i++, k += 4)
                {
                    Layer3KeyColors[k] = r.Color.X / 255;
                    Layer3KeyColors[k + 1] = r.Color.Y / 255;
                    Layer3KeyColors[k + 2] = r.Color.Z / 255;
                    Layer3KeyColors[k + 3] = 1;
                }
            }
        }
        public CampaignData cd;

        public void LoadData()
        {
            cd = new CampaignData();

            List<MiniYamlNode> topnode;
            topnode = MiniYaml.Merge(modData.Manifest.CampaignDB.Select(
                y => MiniYaml.FromStream(modData.DefaultFileSystem.Open(y), y)));
            foreach (MiniYamlNode n in topnode)
            {
                foreach (MiniYamlNode j in n.Value.Nodes)
                {
                    if (j.Key == "CampaignName")
                    {
                        cd.CampaignName = j.Value.Value;
                    }
                    if (j.Key == "CampaignDesc")
                    {
                        cd.CampaignDesc = j.Value.Value;
                    }
                    if (j.Key == "CampaignForFractionCode")
                    {
                        cd.CampaignForFractionCode = FieldLoader.GetValue<float3>("CampaignForFractionCode", j.Value.Value);

                    }
                    if (j.Key == "Players")
                    {
                        cd.Players = new List<CampaignPlayers>();
                        foreach (MiniYamlNode k in j.Value.Nodes)
                        {
                            if (selectableFactions.Select(a => a.InternalName == k.Key).Any())
                            {
                                CampaignPlayers item = new CampaignPlayers();
                                cd.Players.Add(item);
                                item.Color = FieldLoader.GetValue<float3>("CampaignPlayersColor", k.Value.Value);
                                item.Name = k.Key;
                                item.RegionColor = FieldLoader.GetValue<float3>("CampaignPlayersRegionColor", k.Value.Nodes[0].Value.Value);
                            }
                        }

                    }
                    if (j.Key == "Levels")
                    {
                        cd.Levels = new List<CampaignLevel>();
                        foreach (MiniYamlNode k in j.Value.Nodes)
                        {
                            CampaignLevel cl = new CampaignLevel();
                            cd.Levels.Add(cl);
                            cl.Num = FieldLoader.GetValue<int>("CampaignLevelNum", k.Key);

                            foreach (MiniYamlNode h in k.Value.Nodes)
                            {
                                if (h.Key == "PlayersRegions")
                                {
                                    cl.PlayersRegions = new List<LevelPlayers>();

                                    foreach (MiniYamlNode f in h.Value.Nodes) //player faction name
                                    {
                                        if (selectableFactions.Select(a => a.InternalName == f.Key).Any())
                                        {
                                            foreach (MiniYamlNode v in f.Value.Nodes) //list of reign regions
                                            {
                                                if (v.Key == "ReignRegions")
                                                {
                                                    LevelPlayers lp = new LevelPlayers();
                                                    lp.Name = f.Key;
                                                    cl.PlayersRegions.Add(lp);

                                                    foreach (MiniYamlNode d in v.Value.Nodes)
                                                    {

                                                        lp.ReignRegions.Add(new ReignRegion() { Color = FieldLoader.GetValue<float3>("ReignRegions", d.Key) });

                                                    }


                                                }
                                            }
                                        }
                                    }
                                }
                                if (h.Key == "PickRegions")
                                {
                                    foreach (MiniYamlNode d in h.Value.Nodes)
                                    {
                                        float3 regcolor;
                                        regcolor = FieldLoader.GetValue<float3>("ReignRegions", d.Key);
                                        cl.PickRegions.Add(regcolor, d.Value.Value);
                                    }
                                }
                            }
                        }
                    }
                }

            }
            // = MiniYaml.FromString(modData.Manifest.CampaignDB.ToString());

        }
        public override void Initialize(WidgetArgs args)
        {
            base.Initialize(args);
            if (Game.Renderer.PixelDumpRenderer.fbcreated == false)
            {
                Game.Renderer.PixelDumpRenderer.Setup(new Size(1024, 768)); //widget должен быть в пределах 1024 на 512 пикселей

                PrepTextures();
            }
            else
            {
                //переинициализация этих переменных происходит только  на 2+N раз
                housesT = Game.Renderer.PixelDumpRenderer.fb.Texture[2];
                dunergnT = Game.Renderer.PixelDumpRenderer.fb.Texture[1];
                sh1 = new Sheet(SheetType.BGRA, Game.Renderer.PixelDumpRenderer.fb.Texture[0]);
                sp1 = new Sprite(sh1, new Rectangle(0, 0, RenderBounds.Width, RenderBounds.Height), TextureChannel.RGBA);
                sh2 = new Sheet(SheetType.BGRA, Game.Renderer.PixelDumpRenderer.fb.Texture[3]);
                sp2 = new Sprite(sh2, new Rectangle(0, 0, RenderBounds.Width, RenderBounds.Height), TextureChannel.RGBA);
                textemp = Game.Renderer.PixelDumpRenderer.fb.Texture[4]; //ссылка на текстуру, куда будут уходить данные
                                                                         // об выбранных масках

                mapchamord = ChromeProvider.GetImage("patched", "mapchamord");
                mapchamhark = ChromeProvider.GetImage("patched", "mapchamhark");
                mapchamatr = ChromeProvider.GetImage("patched", "mapchamatr");
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

        private ITexture rgnclickT;
        private ITexture dunergnT;
        private ITexture housesT;
        private ITexture housesMaskT;
        private ITexture maproomHarkSizedT;
        public bool SwitchToMap = false;
        public float[] Layer1KeyColors;
        private float[] Layer2KeyColors;
        private float[] Layer3KeyColors;
        private float[] Layer4PickKeyColors;
        private float[] Layer1Color;
        private float[] Layer2Color;
        private float[] Layer3Color;
        private int EffectCycleInSteps=60;
        private bool EffectBackward;
        private Sprite mapchamhark, mapchamatr, mapchamord;

        /// <summary>
        /// КОнвертирует все 1 байтовые пиксели в 4 байтовые, чтобы работать с этими картинками в шейдере в drawmode=10
        /// </summary>
        public void PrepTextures()
        {

            Game.Renderer.Flush();
            rgnclickT = Game.Renderer.PixelDumpRenderer.fb.Bind(true);

            Game.Renderer.PixelDumpRenderer.DrawSprite(dunergnclickSprite, new float3(0, 0, 0), new float3(RenderBounds.Width, RenderBounds.Height, 0), prbase);

            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            Game.Renderer.PixelDumpRenderer.fb.Unbind();

            dunergnT = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size);
            Game.Renderer.PixelDumpRenderer.DrawSprite(dunergnSprite, new float3(0, 0, 0), new float3(RenderBounds.Width, RenderBounds.Height, 0), prbase);
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            Game.Renderer.PixelDumpRenderer.fb.Unbind();

            housesT = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size);
            Game.Renderer.PixelDumpRenderer.DrawSprite(housesSprite, new float3(0, 0, 0), new float3(RenderBounds.Width, RenderBounds.Height, 0), prbase);
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            Game.Renderer.PixelDumpRenderer.fb.Unbind();

            housesMaskT = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size);
            Game.Renderer.PixelDumpRenderer.DrawSprite(housesmaskSprite, new float3(0, 0, 0), new float3(RenderBounds.Width, RenderBounds.Height, 0), prbase);
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            Game.Renderer.PixelDumpRenderer.fb.Unbind();

            //переходим в размеры текстур как у игры! 
            textemp = Game.Renderer.PixelDumpRenderer.fb.Bind(true, new Size(1024, 768));
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            Game.Renderer.PixelDumpRenderer.fb.Unbind();

            // textemp ссылка на текстуру, куда будут уходить данные об выбранных масках






            maproomMaskT = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size);
            Game.Renderer.PixelDumpRenderer.DrawSprite(maproommaskSprite, new float3(0, 0, 0), new float3(RenderBounds.Width, RenderBounds.Height, 0), prbase);
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            Game.Renderer.PixelDumpRenderer.fb.Unbind();



            sh1 = new Sheet(SheetType.BGRA, rgnclickT);
            sp1 = new Sprite(sh1, new Rectangle(0, 0, RenderBounds.Width, RenderBounds.Height), TextureChannel.RGBA);
            sh2 = new Sheet(SheetType.BGRA, housesMaskT);
            sp2 = new Sprite(sh2, new Rectangle(0, 0, RenderBounds.Width, RenderBounds.Height), TextureChannel.RGBA);

            maproomHarkT = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size); //патчим оригинальную текстуру знаками дома

            mapchamhark = ChromeProvider.GetImage("patched", "mapchamhark");
            if (mapchamhark == null)
            {
                PatchHarkMapChamber();
            }
            mapchamhark = ChromeProvider.GetImage("patched", "mapchamhark");

            mapchamatr = ChromeProvider.GetImage("patched", "mapchamatr");
            if (mapchamatr == null)
            {
                PatchAtrMapChamber();
            }
            mapchamatr = ChromeProvider.GetImage("patched", "mapchamatr");
            mapchamord = ChromeProvider.GetImage("patched", "mapchamord");
            if (mapchamord == null)
            {
                PatchOrdosMapChamber();
            }
            mapchamord = ChromeProvider.GetImage("patched", "mapchamord");




        }
        Rectangle FlipRectangle(Rectangle rect, bool flipX, bool flipY)
        {
            var left = flipX ? rect.Right : rect.Left;
            var top = flipY ? rect.Bottom : rect.Top;
            var right = flipX ? rect.Left : rect.Right;
            var bottom = flipY ? rect.Top : rect.Bottom;

            return Rectangle.FromLTRB(left, top, right, bottom);
        }

        public void PatchHarkMapChamber()
        {
            //maproomHarkSizedT = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size);
            //Game.Renderer.PixelDumpRenderer.fb.Unbind();
            //hark map
            Game.Renderer.PixelDumpRenderer.fb.ReBind((ITextureInternal)maproomHarkT);
            //maproomSprite.FlipY = 1;
            Game.Renderer.PixelDumpRenderer.DrawSprite(maproomSprite, new float3(0, 0, 0), new float3(320, 200, 0), prbase);
            Game.Renderer.PixelDumpRenderer.DrawSprite(ChromeProvider.GetImage("harksign", "background"), new float3(2, 145, 0), new float3(53, 54, 0), prbase);
            Game.Renderer.PixelDumpRenderer.DrawSprite(ChromeProvider.GetImage("harksign", "background"), new float3(266, 145, 0), new float3(53, 54, 0), prbase);
            Game.Renderer.RgbaColorRenderer.FillRect(new float3(7, 24, 0), new float3(304 + 8, 119 + 24, 0), Color.Black); //пишет в SPriteRenderer
            Game.Renderer.SpriteRenderer.Flush();
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            Game.Renderer.PixelDumpRenderer.fb.Unbind();

            //FlipRectangle(s.Bounds, flipX, flipY)

            sh3 = new Sheet(SheetType.BGRA, maproomHarkT); //делаем спрайт по патченной текстуре.
            sp3 = new Sprite(sh3, new Rectangle(0, 0, 320, 200), TextureChannel.RGBA); //указываем оригинальный размер спрайта, чтобы потом он адаптировался под разные рразрешения

            ChromeProvider.AddSprite("patched", "mapchamhark", Game.SheetBuilder2D.AddSprite(sp3));

            //var d = new Dictionary<string, Sprite>();
            //d.Add("mapchamhark", Game.SheetBuilder2D.AddSprite(sp3));
            //ChromeProvider.cachedSprites.Add("patched", d);
            //Game.Renderer.PixelDumpRenderer.fb.ReBind((ITextureInternal)maproomHarkSizedT); //печатаем спрайт нужного размера в другую текстур и делаем с нее спрайт.
            //sp3.SpriteType = 8;
            //sp3.Stretched = true;
            //Game.Renderer.PixelDumpRenderer.DrawSprite(sp3, new float3(0, 0, 0), new float3(RenderBounds.Width, RenderBounds.Height, 0), prbase);
            //Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            //Game.Renderer.PixelDumpRenderer.fb.Unbind();

            //sp3.Sheet.Dispose();

            //sh3 = new Sheet(SheetType.BGRA, maproomHarkSizedT);
            //sp3 = new Sprite(sh3, new Rectangle(0, 0, RenderBounds.Width, RenderBounds.Height), TextureChannel.RGBA);
            //sp3.SpriteType = 8;//режим растягивания спрайт по вертексам

        }
        public void PatchAtrMapChamber()
        {

            maproomAtrSizedT = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size);
            Game.Renderer.PixelDumpRenderer.fb.Unbind();
            //atr map
            maproomAtrT = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size); //патчим оригинальную текстуру знаками дома
            Game.Renderer.PixelDumpRenderer.DrawSprite(maproomSprite, new float3(0, 0, 0), new float3(320, 200, 0), prbase);
            Game.Renderer.PixelDumpRenderer.DrawSprite(ChromeProvider.GetImage("atrsign", "background"), new float3(2, 145, 0), new float3(53, 54, 0), prbase);
            Game.Renderer.PixelDumpRenderer.DrawSprite(ChromeProvider.GetImage("atrsign", "background"), new float3(266, 145, 0), new float3(53, 54, 0), prbase);
            Game.Renderer.RgbaColorRenderer.FillRect(new float3(7, 24, 0), new float3(304 + 8, 119 + 24, 0), Color.Black); //пишет в SPriteRenderer
            Game.Renderer.SpriteRenderer.Flush();
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.

            Game.Renderer.PixelDumpRenderer.fb.Unbind();

            sh4 = new Sheet(SheetType.BGRA, maproomAtrT); //делаем спрайт по патченной текстуре.
            sp4 = new Sprite(sh4, new Rectangle(0, 0, 320, 200), TextureChannel.RGBA); //указываем оригинальный размер спрайта, чтобы потом он адаптировался под разные рразрешения

            ChromeProvider.AddSprite("patched", "mapchamatr", Game.SheetBuilder2D.AddSprite(sp4));

            //var d = new Dictionary<string, Sprite>();
            //d.Add("mapchamatr", Game.SheetBuilder2D.AddSprite(sp4));
            //ChromeProvider.cachedSprites.Add("patched", d);

            //sh4 = new Sheet(SheetType.BGRA, maproomAtrT); //делаем спрайт по патченной текстуре.
            //sp4 = new Sprite(sh4, new Rectangle(0, 0, 320, 200), TextureChannel.RGBA); //указываем оригинальный размер спрайта, чтобы потом он адаптировался под разные рразрешения
            //                                                                           //---
            //Game.Renderer.PixelDumpRenderer.fb.ReBind((ITextureInternal)maproomAtrSizedT); //печатаем спрайт нужного размера в другую текстур и делаем с нее спрайт.
            //sp4.SpriteType = 8;
            //sp4.Stretched = true;
            //Game.Renderer.PixelDumpRenderer.DrawSprite(sp4, new float3(0, 0, 0), new float3(RenderBounds.Width, RenderBounds.Height, 0), prbase);
            //Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            //Game.Renderer.PixelDumpRenderer.fb.Unbind();

            //sp4.Sheet.Dispose();

            //sh4 = new Sheet(SheetType.BGRA, maproomAtrSizedT);
            //sp4 = new Sprite(sh4, new Rectangle(0, 0, RenderBounds.Width, RenderBounds.Height), TextureChannel.RGBA);
            //sp4.SpriteType = 8;//режим растягивания спрайт по вертексам


        }
        public void PatchOrdosMapChamber()
        {
            maproomOrdosSizedT = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size);
            Game.Renderer.PixelDumpRenderer.fb.Unbind();
            //atr map
            maproomOrdosT = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size); //патчим оригинальную текстуру знаками дома
            Game.Renderer.PixelDumpRenderer.DrawSprite(maproomSprite, new float3(0, 0, 0), new float3(320, 200, 0), prbase);
            Game.Renderer.PixelDumpRenderer.DrawSprite(ChromeProvider.GetImage("ordossign", "background"), new float3(2, 145, 0), new float3(53, 54, 0), prbase);
            Game.Renderer.PixelDumpRenderer.DrawSprite(ChromeProvider.GetImage("ordossign", "background"), new float3(266, 145, 0), new float3(53, 54, 0), prbase);
            Game.Renderer.RgbaColorRenderer.FillRect(new float3(7, 24, 0), new float3(304 + 8, 119 + 24, 0), Color.Black); //пишет в SPriteRenderer
            Game.Renderer.SpriteRenderer.Flush();
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.

            Game.Renderer.PixelDumpRenderer.fb.Unbind();

            sh5 = new Sheet(SheetType.BGRA, maproomOrdosT); //делаем спрайт по патченной текстуре.
            sp5 = new Sprite(sh5, new Rectangle(0, 0, 320, 200), TextureChannel.RGBA); //указываем оригинальный размер спрайта, чтобы потом он адаптировался под разные рразрешения

            ChromeProvider.AddSprite("patched", "mapchamord", Game.SheetBuilder2D.AddSprite(sp5));

            //var d = new Dictionary<string, Sprite>();
            //d.Add("mapchamord", Game.SheetBuilder2D.AddSprite(sp5));
            //ChromeProvider.cachedSprites.Add("patched", d);

            //sh5 = new Sheet(SheetType.BGRA, maproomOrdosT); //делаем спрайт по патченной текстуре.
            //sp5 = new Sprite(sh5, new Rectangle(0, 0, 320, 200), TextureChannel.RGBA); //указываем оригинальный размер спрайта, чтобы потом он адаптировался под разные рразрешения
            //                                                                           //---
            //Game.Renderer.PixelDumpRenderer.fb.ReBind((ITextureInternal)maproomOrdosSizedT); //печатаем спрайт нужного размера в другую текстур и делаем с нее спрайт.
            //sp5.SpriteType = 8;
            //sp5.Stretched = true;
            //Game.Renderer.PixelDumpRenderer.DrawSprite(sp5, new float3(0, 0, 0), new float3(RenderBounds.Width, RenderBounds.Height, 0), prbase);
            //Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            //Game.Renderer.PixelDumpRenderer.fb.Unbind();

            //sp5.Sheet.Dispose();

            //sh5 = new Sheet(SheetType.BGRA, maproomOrdosSizedT);
            //sp5 = new Sprite(sh5, new Rectangle(0, 0, RenderBounds.Width, RenderBounds.Height), TextureChannel.RGBA);
            //sp5.SpriteType = 8;//режим растягивания спрайт по вертексам
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
            Game.Renderer.SpriteRenderer.SetAlphaInit(60, 0, 0, 0);
            Game.Renderer.SpriteRenderer.SetAlphaConstantRegion(255, 255, 85, 255);

            //оригинал выбора домов
            Game.Renderer.SpriteRenderer.shader.SetTexture("Texture1", housesT);
            sp2.SpriteType = 7;

            //передаем вторым аргументом текстуру, где маска
            WidgetUtils.FillRectWithSprite(RenderBounds, sp2, prbase);
            //Game.Renderer.SpriteRenderer.Flush(); //записать кадр во фреймбуфер 
        }


        public void DrawMap()
        {
            //PatchHarkMapChamber();
            //    return;
            if (CurrentFaction.Name == "Harkonnen")
            {
                WidgetUtils.FillRectWithSprite(RenderBounds, mapchamhark, prbase); //отрисуем спрайт комнаты для карты
            }
            if (CurrentFaction.Name == "Atreides")
            {
                WidgetUtils.FillRectWithSprite(RenderBounds, mapchamatr, prbase); //отрисуем спрайт комнаты для карты
            }
            if (CurrentFaction.Name == "Ordos")
            {
                WidgetUtils.FillRectWithSprite(RenderBounds, mapchamord, prbase); //отрисуем спрайт комнаты для карты
            }
            //а после запускаем патч
            Game.Renderer.SpriteRenderer.shader.SetTexture("Texture1", maproomMaskT); //dunergn
            //Game.Renderer.SpriteRenderer.shader.SetTexture("Texture2", maproomT); //dunergn
            Game.Renderer.Flush(); //нужен, так как текстура используется внешняя и на один раз, ниже она уже замениться другой dunergnT

            float normX, normY;
            //соблюдаем размерность между источником текстур SeqProv и фреймбуфером.
            normX = -1 * (float)MouseLocationInWidget.X / 1024; //2048 на 2048 это размер текстуры в которой хранятся пиксели от фреймбуфера
            normY = 1 + (float)MouseLocationInWidget.Y / 768;
            //Game.Renderer.SpriteRenderer.SetAlphaFlag(false);
            // Game.Renderer.SpriteRenderer.SetAlphaConstantRegion(-1, 255, 85, 255);
            Game.Renderer.SpriteRenderer.SetMouseLocation(new float2(normX, normY));
            //Game.Renderer.SpriteRenderer.SetLayer1KeyColor(0,170,0,255);
            Game.Renderer.SpriteRenderer.shader.SetVec("Layer1KeyColors", Layer1KeyColors, 4, Layer1KeyColors.Length / 4);
            Game.Renderer.SpriteRenderer.shader.SetVec("Layer1Color", Layer1Color, 4);
            Game.Renderer.SpriteRenderer.shader.SetVec("Layer2KeyColors", Layer2KeyColors, 4, Layer2KeyColors.Length / 4);
            Game.Renderer.SpriteRenderer.shader.SetVec("Layer2Color", Layer2Color, 4);
            Game.Renderer.SpriteRenderer.shader.SetVec("Layer3KeyColors", Layer3KeyColors, 4, Layer3KeyColors.Length / 4);
            Game.Renderer.SpriteRenderer.shader.SetVec("Layer3Color", Layer3Color, 4);
            Game.Renderer.SpriteRenderer.shader.SetVec("Layer4PickKeyColors", Layer4PickKeyColors, 4, Layer4PickKeyColors.Length / 4);

            if (newframe)
            {
                if (EffectBackward)
                {
                    EffectCycleInSteps = EffectCycleInSteps + AnimationStep;
                }
                else
                {
                    EffectCycleInSteps = EffectCycleInSteps - AnimationStep;
                }
                newframe = false;
            }
            Game.Renderer.SpriteRenderer.shader.SetVec("iTime", EffectCycleInSteps);
            if (EffectCycleInSteps <= 0)
            {
               
                EffectBackward = true;
            }
            if ( EffectCycleInSteps >= 255)
            {

                EffectBackward = false;
            }
            //оригинальная карта
            Game.Renderer.SpriteRenderer.shader.SetTexture("Texture1", dunergnT); //dunergn

            //передаем вторым аргументом текстуру, где регионы для мышки
            sp1.SpriteType = 6;
            WidgetUtils.FillRectWithSprite(RenderBounds, sp1, prbase); //rgnclck //это такой способ установить в Texture0 sp1 с маской регионов

            // Game.Renderer.SpriteRenderer.Flush(); //записать кадр во фреймбуфер 
        }
        public override void Draw()
        {
            //Game.Renderer.PixelDumpRenderer.fb.ReBind((ITextureInternal)maproomT);
            //sp3.SpriteType = 2;
            //sp3.Stretched = true;
            //Game.Renderer.PixelDumpRenderer.DrawSprite(sp3, new float3(0, 0, 0), new float3(RenderBounds.Width, RenderBounds.Height, 0), prbase);
            //Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            //Game.Renderer.PixelDumpRenderer.fb.Unbind();

            //return;
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

            // Game.Renderer.SpriteRenderer.Flush();

            //делаем спрайт по текстуре 1, в которой карта с регионами

        }

        public int AnimationStep = 5;
        public override void TickOuter()
        {
            newframe = true;
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
                lastanswer = ReadPixelUnderMouse();

                Game.Renderer.SpriteRenderer.SetFrameBufferMaskMode(false);
                Game.Renderer.PixelDumpRenderer.fb.Unbind();


                //int r, g, b;
                //r = lastanswer[2];
                //g = lastanswer[1];
                //b = lastanswer[0];
                float3 feedbackcolor = new float3(lastanswer[2], lastanswer[1], lastanswer[0]);
                if (SwitchToMap)
                {
                    OnMapRegionChooseDelegate(lastanswer[2], lastanswer[1], lastanswer[0]);
                }
                else
                {
                    if (cd.Players[0].Color == feedbackcolor)
                    {
                        CurrentFaction = selectableFactions.Where(f => f.InternalName == cd.Players[0].Name).First();
                    }
                    if (cd.Players[1].Color == feedbackcolor)
                    {
                        CurrentFaction = selectableFactions.Where(f => f.InternalName == cd.Players[1].Name).First();
                    }
                    if (cd.Players[2].Color == feedbackcolor)
                    {
                        CurrentFaction = selectableFactions.Where(f => f.InternalName == cd.Players[2].Name).First();

                    }

                    OnHouseChooseDelegate(CurrentFaction.Name);
                }
                SwitchToMap = true;
                Clicked = false;

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
                    OpenGL.glReadPixels(RenderOrigin.X - 1 * MouseLocationInWidget.X, Game.Renderer.Resolution.Height - RenderOrigin.Y + 1 * MouseLocationInWidget.Y, s.Width, s.Height,
                        OpenGL.GL_BGRA, OpenGL.GL_UNSIGNED_BYTE, (IntPtr)pRaw);
            }
            Console.WriteLine(raw[2].ToString() + "." + raw[1].ToString() + "." + raw[0].ToString() + raw[3].ToString());
            return raw;

        }

        public bool Depressed;
        int2 MouseLocationInWidget;
        public bool Clicked;
        private ITexture maproomMaskT;
        private ITexture maproomHarkT;
        private Sprite mapharkSprite;
        private ITexture maproomAtrSizedT;
        private ITexture maproomAtrT;
        private Sheet sh4;
        private Sprite sp4;
        private ITexture maproomOrdosSizedT;
        private ITexture maproomOrdosT;
        private Sheet sh5;
        private Sprite sp5;
        private bool newframe;

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
