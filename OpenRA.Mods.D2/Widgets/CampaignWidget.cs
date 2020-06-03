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
        private Sprite fameroomSprite;
        private SequenceProvider sp;
        private ITexture textemp;
        Sheet sh1;
        /// <summary>
        /// <para/>Спрайт содержит маску регионов карты для пользователя
        /// <para/>Он перерисован в текстуру с разрешением экрана и увеличен до размеров widget.RenderBounds.
        /// </summary>
        Sprite MapRegionMaskSprite;

        Sheet sh2;

        Sprite HousesMaskSprite;
        private Sheet sh3;
        private Sprite sp3;
        public Action<string> OnHouseChooseDelegate;
        public Action<int, int, int> OnMapRegionChooseDelegate;
        public Action<string> DrawTextDelegate;
        public Action<int> BindLevelDelegate;
        public Action UpLevelDelegate;
        public Action DownLevelDelegate;
        public Action OnExit;
        public int CurrentLevel;

        List<FactionInfo> selectableFactions;
        FactionInfo CurrentFaction;
        public byte[] lastanswer;
        private ModData modData;
        private World world;
        public Dictionary<string, List<string>> TextDB=new Dictionary<string, List<string>>();
        /// <summary>
        /// Если использовать [ObjectCreator.UseCtor] , то можно использовать DI для инициализации аргументов конструктора.
        /// </summary>
        /// <param name="world"></param>
        [ObjectCreator.UseCtor]
        public CampaignWidget(World world, ModData modData, OpenRA.Network.OrderManager orderManager)
        {
            this.world = world;
            this.modData = modData;
            dunergnSprite = ChromeProvider.GetImage("dunergn", "background");
            dunergnclickSprite = ChromeProvider.GetImage("dunergnclk", "background");
            housesSprite = ChromeProvider.GetImage("housestitle", "background");
            housesmaskSprite = ChromeProvider.GetImage("heraldmask", "background");
            maproomSprite = ChromeProvider.GetImage("maproom", "background");
            maproommaskSprite = ChromeProvider.GetImage("maproommask", "background");
            fameroomSprite = ChromeProvider.GetImage("fame", "background");


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

            float3 cc1 = CampaignData.Players[0].RegionColor;
            Layer1Color = new float[4] { cc1.X / 255, cc1.Y / 255, cc1.Z / 255, 1 };
            cc1 = CampaignData.Players[1].RegionColor;
            Layer2Color = new float[4] { cc1.X / 255, cc1.Y / 255, cc1.Z / 255, 1 };
            cc1 = CampaignData.Players[2].RegionColor;
            Layer3Color = new float[4] { cc1.X / 255, cc1.Y / 255, cc1.Z / 255, 1 };


            //BindLevelOnMap(1);

            BindLevelDelegate = BindLevelOnMap;
            UpLevelDelegate = UpLevel;
            DownLevelDelegate = DownLevel;

            if (world.IsGameOver)
            {
                CurrentLevel = orderManager.LobbyInfo.GlobalSettings.CampaignLevel;
                CurrentFaction = world.LocalPlayer.Faction;
                UpLevel();
                //SwitchToMap = true;
                DrawFrame = DrawFrameEnum.Map;

            }
            else
            {
                BindLevelOnMap(1);
            }
            TextDB.Add("TEXTA.ENG",LoadTextDB("TEXTA.ENG"));


        }
        public void UpLevel()
        {
            if (CurrentLevel + 1 >= 1 && CurrentLevel + 1 <= CampaignData.Levels.Count)
            {
                CurrentLevel += 1;
                BindLevelOnMap(CurrentLevel);
            }
        }
        public void DownLevel()
        {
            if (CurrentLevel - 1 >= 1 && CurrentLevel - 1 <= CampaignData.Levels.Count)
            {
                CurrentLevel -= 1;
                BindLevelOnMap(CurrentLevel);
            }
        }
        public void BindLevelOnMap(int Level)
        {
            if (world != null)
            {
                if (world.IsGameOver)
                {

                }
                else
                {

                }
            }
            CurrentLevel = Level;
            CampaignData.CurrentLevel = CurrentLevel;
            Level -= 1;
            if (DrawTextDelegate != null)
            {
                DrawTextDelegate(String.Format("Level switched to {0}", CurrentLevel));
            }
            Layer2KeyColors = new float[10 * 4];
            Layer1KeyColors = new float[10 * 4];
            Layer4PickKeyColors = new float[10 * 4];
            Layer3KeyColors = new float[10 * 4];
            int k = 0;

            Dictionary<float3, string> pr = CampaignData.Levels[Level].PickRegions;

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





            List<ReignRegion> rr = CampaignData.Levels[Level].PlayersRegions[0].ReignRegions;
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


            rr = CampaignData.Levels[Level].PlayersRegions[1].ReignRegions;
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
            rr = CampaignData.Levels[Level].PlayersRegions[2].ReignRegions;
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
        public CampaignData CampaignData;
        public void ShowBrief()
        {

        }

        public void LoadData()
        {
            CampaignData = new CampaignData();

            List<MiniYamlNode> topnode;
            topnode = MiniYaml.Merge(modData.Manifest.CampaignDB.Select(
                y => MiniYaml.FromStream(modData.DefaultFileSystem.Open(y), y)));
            foreach (MiniYamlNode n in topnode)
            {
                foreach (MiniYamlNode j in n.Value.Nodes)
                {
                    if (j.Key == "CampaignName")
                    {
                        CampaignData.CampaignName = j.Value.Value;
                    }
                    if (j.Key == "CampaignDesc")
                    {
                        CampaignData.CampaignDesc = j.Value.Value;
                    }
                    if (j.Key == "CampaignForFractionCode")
                    {
                        CampaignData.CampaignForFractionCode = FieldLoader.GetValue<float3>("CampaignForFractionCode", j.Value.Value);

                    }
                    if (j.Key == "Players")
                    {
                        CampaignData.Players = new List<CampaignPlayers>();
                        foreach (MiniYamlNode k in j.Value.Nodes)
                        {
                            if (selectableFactions.Select(a => a.InternalName == k.Key).Any())
                            {
                                CampaignPlayers item = new CampaignPlayers();
                                CampaignData.Players.Add(item);
                                item.Color = FieldLoader.GetValue<float3>("CampaignPlayersColor", k.Value.Value);
                                item.Name = k.Key;
                                item.RegionColor = FieldLoader.GetValue<float3>("CampaignPlayersRegionColor", k.Value.Nodes[0].Value.Value);
                            }
                        }

                    }
                    if (j.Key == "Levels")
                    {
                        CampaignData.Levels = new List<CampaignLevel>();
                        foreach (MiniYamlNode k in j.Value.Nodes)
                        {
                            CampaignLevel cl = new CampaignLevel();
                            CampaignData.Levels.Add(cl);
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
                                if (h.Key == "Brief")
                                {
                                    Brief br = new Brief();
                                    foreach (MiniYamlNode d in h.Value.Nodes)
                                    {
                                        if (d.Key == "Background")
                                        {
                                            br.Background = d.Value.Value;
                                        }
                                        if (d.Key == "SubBkgSequence")
                                        {
                                            br.SubBkgSequence = d.Value.Value;
                                        }
                                        if (d.Key == "SubBkgSequenceGroup")
                                        {
                                            br.SubBkgSequenceGroup = d.Value.Value;
                                        }
                                        if (d.Key == "DbIndex")
                                        {
                                            br.DbIndex = FieldLoader.GetValue<int>("DbIndex", d.Value.Value); 
                                        }
                                    }
                                    cl.Brief = br;
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
                Game.Renderer.PixelDumpRenderer.Setup(new Size(Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height)); 

                PrepTextures();
            }
            else
            {
                //переинициализация этих переменных происходит только  на 2+N раз
                housesT = Game.Renderer.PixelDumpRenderer.fb.Texture[2];
                dunergnT = Game.Renderer.PixelDumpRenderer.fb.Texture[1];
                sh1 = new Sheet(SheetType.BGRA, Game.Renderer.PixelDumpRenderer.fb.Texture[0]);
                MapRegionMaskSprite = new Sprite(sh1, new Rectangle(0, 0, RenderBounds.Width, RenderBounds.Height), TextureChannel.RGBA);
                sh2 = new Sheet(SheetType.BGRA, Game.Renderer.PixelDumpRenderer.fb.Texture[3]);
                HousesMaskSprite = new Sprite(sh2, new Rectangle(0, 0, RenderBounds.Width, RenderBounds.Height), TextureChannel.RGBA);
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
        /// <summary>
        /// <para>Эта текстура содержит спрайт регионов карты для пользователя</para> , с масштабом RenderBounds(widget в котором мы рисуем)
        /// </summary>
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
        private int EffectCycleInSteps = 60;
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
            textemp = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size);
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            Game.Renderer.PixelDumpRenderer.fb.Unbind();

            // textemp ссылка на текстуру, куда будут уходить данные об выбранных масках






            maproomMaskT = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size);
            Game.Renderer.PixelDumpRenderer.DrawSprite(maproommaskSprite, new float3(0, 0, 0), new float3(RenderBounds.Width, RenderBounds.Height, 0), prbase);
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            Game.Renderer.PixelDumpRenderer.fb.Unbind();



            sh1 = new Sheet(SheetType.BGRA, rgnclickT);
            MapRegionMaskSprite = new Sprite(sh1, new Rectangle(0, 0, RenderBounds.Width, RenderBounds.Height), TextureChannel.RGBA);
            sh2 = new Sheet(SheetType.BGRA, housesMaskT);
            HousesMaskSprite = new Sprite(sh2, new Rectangle(0, 0, RenderBounds.Width, RenderBounds.Height), TextureChannel.RGBA);



            mapchamhark = ChromeProvider.GetImage("patched", "mapchamhark");
            if (mapchamhark == null)
            {
                PatchHarkMapChamber();
                mapchamhark = ChromeProvider.GetImage("patched", "mapchamhark");
            }


            mapchamatr = ChromeProvider.GetImage("patched", "mapchamatr");
            if (mapchamatr == null)
            {
                PatchAtrMapChamber();
                mapchamatr = ChromeProvider.GetImage("patched", "mapchamatr");
            }

            mapchamord = ChromeProvider.GetImage("patched", "mapchamord");
            if (mapchamord == null)
            {
                PatchOrdosMapChamber();
                mapchamord = ChromeProvider.GetImage("patched", "mapchamord");
            }

            fameatr = ChromeProvider.GetImage("patched", "fameatr");
            if (fameatr == null)
            {
                PatchStatFameAtrWindow();
                fameatr = ChromeProvider.GetImage("patched", "fameatr");
            }

            famehark = ChromeProvider.GetImage("patched", "famehark");
            if (famehark == null)
            {
                PatchStatFameHarkWindow();
                famehark = ChromeProvider.GetImage("patched", "famehark");
            }

            fameordos = ChromeProvider.GetImage("patched", "fameordos");
            if (fameordos == null)
            {
                PatchStatFameOrdosWindow();
                fameordos = ChromeProvider.GetImage("patched", "fameordos");
            }


            Game.Renderer.PixelDumpRenderer.fb.Unbind();

            mentatAtrSprite = ChromeProvider.GetImage("mentata", "background");
            mentatHarkSprite = ChromeProvider.GetImage("mentath", "background");
            mentatOrdosSprite = ChromeProvider.GetImage("mentato", "background");
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
            maproomHarkT = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size); //патчим оригинальную текстуру знаками дома
            //maproomHarkSizedT = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size);
            //Game.Renderer.PixelDumpRenderer.fb.Unbind();
            //hark map
            //Game.Renderer.PixelDumpRenderer.fb.ReBind((ITextureInternal)maproomHarkT);
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


        }
        public void PatchAtrMapChamber()
        {

            //maproomAtrSizedT = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size);
            //Game.Renderer.PixelDumpRenderer.fb.Unbind();
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
            //maproomOrdosSizedT = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size);
            //Game.Renderer.PixelDumpRenderer.fb.Unbind();
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
        public void PatchStatFameOrdosWindow()
        {
            fameroomT = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size); //патчим оригинальную текстуру знаками дома
            Game.Renderer.PixelDumpRenderer.DrawSprite(fameroomSprite, new float3(0, 0, 0), new float3(320, 200, 0), prbase);
            Game.Renderer.PixelDumpRenderer.DrawSprite(ChromeProvider.GetImage("ordossign", "background"), new float3(2, 9, 0), new float3(53, 54, 0), prbase);
            Game.Renderer.PixelDumpRenderer.DrawSprite(ChromeProvider.GetImage("ordossign", "background"), new float3(266, 9, 0), new float3(53, 54, 0), prbase);
            Game.Renderer.RgbaColorRenderer.FillRect(new float3(9, 136, 0), new float3(167 + 9, 55 + 136, 0), Color.FromArgb(255, 182, 125, 12)); //пишет в SPriteRenderer
            Game.Renderer.SpriteRenderer.Flush();
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.

            Sheet shtemp = new Sheet(SheetType.BGRA, fameroomT); //делаем спрайт по патченной текстуре.
            Sprite stemp = new Sprite(shtemp, new Rectangle(0, 0, 320, 200), TextureChannel.RGBA); //указываем оригинальный размер спрайта, чтобы потом он адаптировался под разные рразрешения

            ChromeProvider.AddSprite("patched", "fameordos", Game.SheetBuilder2D.AddSprite(stemp));

        }
        public void PatchStatFameHarkWindow()
        {
            fameroom2T = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size); //патчим оригинальную текстуру знаками дома
            Game.Renderer.PixelDumpRenderer.DrawSprite(fameroomSprite, new float3(0, 0, 0), new float3(320, 200, 0), prbase);
            Game.Renderer.PixelDumpRenderer.DrawSprite(ChromeProvider.GetImage("harksign", "background"), new float3(2, 9, 0), new float3(53, 54, 0), prbase);
            Game.Renderer.PixelDumpRenderer.DrawSprite(ChromeProvider.GetImage("harksign", "background"), new float3(266, 9, 0), new float3(53, 54, 0), prbase);
            Game.Renderer.RgbaColorRenderer.FillRect(new float3(9, 136, 0), new float3(167 + 9, 55 + 136, 0), Color.FromArgb(255, 182, 125, 12)); //пишет в SPriteRenderer
            Game.Renderer.SpriteRenderer.Flush();
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.

            Sheet shtemp = new Sheet(SheetType.BGRA, fameroom2T); //делаем спрайт по патченной текстуре.
            Sprite stemp = new Sprite(shtemp, new Rectangle(0, 0, 320, 200), TextureChannel.RGBA); //указываем оригинальный размер спрайта, чтобы потом он адаптировался под разные рразрешения

            ChromeProvider.AddSprite("patched", "famehark", Game.SheetBuilder2D.AddSprite(stemp));

        }
        public void PatchStatFameAtrWindow()
        {
            fameroom3T = Game.Renderer.PixelDumpRenderer.fb.Bind(true, Game.Renderer.PixelDumpRenderer.fb.size); //патчим оригинальную текстуру знаками дома
            Game.Renderer.PixelDumpRenderer.DrawSprite(fameroomSprite, new float3(0, 0, 0), new float3(320, 200, 0), prbase);
            Game.Renderer.PixelDumpRenderer.DrawSprite(ChromeProvider.GetImage("atrsign", "background"), new float3(2, 9, 0), new float3(53, 54, 0), prbase);
            Game.Renderer.PixelDumpRenderer.DrawSprite(ChromeProvider.GetImage("atrsign", "background"), new float3(266, 9, 0), new float3(53, 54, 0), prbase);
            Game.Renderer.RgbaColorRenderer.FillRect(new float3(9, 136, 0), new float3(167 + 9, 55 + 136, 0), Color.FromArgb(255, 182, 125, 12)); //пишет в SPriteRenderer
            Game.Renderer.SpriteRenderer.Flush();
            Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.

            Sheet shtemp = new Sheet(SheetType.BGRA, fameroom3T); //делаем спрайт по патченной текстуре.
            Sprite stemp = new Sprite(shtemp, new Rectangle(0, 0, 320, 200), TextureChannel.RGBA); //указываем оригинальный размер спрайта, чтобы потом он адаптировался под разные рразрешения

            ChromeProvider.AddSprite("patched", "fameatr", Game.SheetBuilder2D.AddSprite(stemp));

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
            HousesMaskSprite.SpriteType = 7;

            //передаем вторым аргументом текстуру, где маска
            WidgetUtils.FillRectWithSprite(RenderBounds, HousesMaskSprite, prbase);
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
            if (EffectCycleInSteps >= 255)
            {

                EffectBackward = false;
            }
            //оригинальная карта
            Game.Renderer.SpriteRenderer.shader.SetTexture("Texture1", dunergnT); //dunergn

            //передаем вторым аргументом текстуру, где регионы для мышки
            MapRegionMaskSprite.SpriteType = 6;
            WidgetUtils.FillRectWithSprite(RenderBounds, MapRegionMaskSprite, prbase); //rgnclck //это такой способ установить в Texture0 MapRegionMaskSprite с маской регионов
            //так как MapRegionMaskSprite сделат из Sheet, то уйдет в textureX, а не в Texture2DX

            // Game.Renderer.SpriteRenderer.Flush(); //записать кадр во фреймбуфер 
        }
        public void DrawFame()
        {
            if (CurrentFaction.Name == "Harkonnen")
            {
                WidgetUtils.FillRectWithSprite(RenderBounds, famehark, prbase); //отрисуем спрайт комнаты для карты
            }
            if (CurrentFaction.Name == "Atreides")
            {
                WidgetUtils.FillRectWithSprite(RenderBounds, fameatr, prbase); //отрисуем спрайт комнаты для карты
            }
            if (CurrentFaction.Name == "Ordos")
            {
                WidgetUtils.FillRectWithSprite(RenderBounds, fameordos, prbase); //отрисуем спрайт комнаты для карты
            }
            // Game.Renderer.Flush(); //нужен, так как текстура используется внешняя и на один раз, ниже она уже замениться другой dunergnT
        }
        public CampaignLevel cLevel { get { return CampaignData.Levels[CurrentLevel - 1]; } set { } }

        public void DrawMentat()
        {
            if (CurrentFaction.Name == "Harkonnen")
            {
                DrawMentatSub(cLevel.Brief.SubBkgSequence, cLevel.Brief.SubBkgSequenceGroup, AnimationDirectionEnum.Repeat);
                WidgetUtils.FillRectWithSprite(RenderBounds, mentatHarkSprite, prbase); //отрисуем спрайт комнаты для карты


            }
            if (CurrentFaction.Name == "Atreides")
            {
                DrawMentatSub(cLevel.Brief.SubBkgSequence, cLevel.Brief.SubBkgSequenceGroup, AnimationDirectionEnum.Repeat);
                WidgetUtils.FillRectWithSprite(RenderBounds, mentatAtrSprite, prbase); //отрисуем спрайт комнаты для карты
            }
            if (CurrentFaction.Name == "Ordos")
            {
                DrawMentatSub(cLevel.Brief.SubBkgSequence, cLevel.Brief.SubBkgSequenceGroup, AnimationDirectionEnum.Repeat);
                WidgetUtils.FillRectWithSprite(RenderBounds, mentatOrdosSprite, prbase); //отрисуем спрайт комнаты для карты
            }
            //DrawMentatSubButton(); сделано через widget
        }
        public enum AnimationDirectionEnum
        {
            Repeat, Forward
        }
        int ratio = 2; //чтобы попасть в теже координаты, нужно учесть увеличение, относительно оригинального спрайта. ориг 320 на 200, а используется 640 на 400

        public void DrawMentatSub(string seqname, string subseqname, AnimationDirectionEnum AnimationDirection)
        {       //for wsa



            Game.Renderer.SpriteRenderer.DrawSprite(AnimList[0].Image, new float3(RenderBounds.X + 128 * ratio, RenderBounds.Y + 48 * ratio, 0), 0, new float3(184 * ratio, 112 * ratio, 0));
        }
        public void SetupSubMentat(string seqname, string subseqname, int dbIndex, AnimationDirectionEnum AnimationDirection)
        {

            Widget w = this.Get<ContainerWidget>("mentatstage");
            w.Visible = true;
            LabelWidget widgetMissionInfo = w.Get<LabelWidget>("mentatinfo");

            widgetMissionInfo.GetText = () => { return AnimStringList[0].Text; };

            SetupSubMentat_Back(seqname, subseqname, AnimationDirection);

            List<string> l;
            l = TextDB["TEXTA.ENG"][dbIndex].Split('.').ToList<string>();
            l.RemoveAt(l.Count - 1);
            SetupSubMentat_MissionInfo(seqname, l, AnimationDirection);

        }
        public void SetupSubMentat_Back(string seqname, string subseqname, AnimationDirectionEnum AnimationDirection)
        {
            AnimList.Clear();
            Animation animation1 = new Animation(world, seqname);
            AnimList.Add(animation1);
            if (AnimationDirection == AnimationDirectionEnum.Forward)
            {
                animation1.Play(subseqname);
            }
            if (AnimationDirection == AnimationDirectionEnum.Repeat)
            {
                animation1.PlayRepeating(subseqname);
            }
            animation1.Tick(); //первый тик делаем тут
        }

        public List<string> LoadTextDB(string filename)
        {
            List<string> textdb = new List<string>();
            using (var stream = modData.DefaultFileSystem.Open(filename))
            {
                var start = stream.Position;

                //stream.Position += 2;

                var format = stream.ReadUInt16();
                int numIndexedStrings = format / 2 - 1;
                List<int> offsets = new List<int>();

                offsets.Add(format);
                for (int k = 0; k < numIndexedStrings; k++)
                {
                    UInt16 nextbyte;
                    nextbyte = stream.ReadUInt16();
                    offsets.Add(nextbyte);
                }
                for (int k = 0; k < numIndexedStrings; k++)
                {
                    stream.Position = offsets[k];
                    int len = offsets[k + 1] - offsets[k];
                    byte[] texttodecode = new byte[len];

                    stream.Read(texttodecode, 0, len);
                    string temp = StringDecompress(texttodecode);
                    textdb.Add(temp);
                }

            }
            return textdb;
        }
        public void SetupSubMentat_MissionInfo(string seqname, List<string> MissionInfoStrings, AnimationDirectionEnum AnimationDirection)
        {
          
            AnimStringList.Clear();
            AnimationString animation1 = new AnimationString(world, seqname);
            animation1.DefineTick = 40*60* 3;
            AnimStringList.Add(animation1);

            if (AnimationDirection == AnimationDirectionEnum.Forward)
            {
                animation1.Play(MissionInfoStrings);
            }
            if (AnimationDirection == AnimationDirectionEnum.Repeat)
            {
                animation1.PlayRepeating(MissionInfoStrings);
            }
            animation1.Tick(); //первый тик делаем тут
        }
        public string StringDecompress(byte[] str)
        {
            char[] decodeTable1 = { ' ', 'e', 't', 'a', 'i', 'n', 'o', 's', 'r', 'l', 'h', 'c', 'd', 'u', 'p', 'm' };
            char[,] decodeTable2 ={ { 't','a','s','i','o',' ','w','b' },
                                    { ' ','r','n','s','d','a','l','m' },
                                    { 'h',' ','i','e','o','r','a','s' },
                                    { 'n','r','t','l','c',' ','s','y' },
                                    { 'n','s','t','c','l','o','e','r' },
                                    { ' ','d','t','g','e','s','i','o' },
                                    { 'n','r',' ','u','f','m','s','w' },
                                    { ' ','t','e','p','.','i','c','a' },
                                    { 'e',' ','o','i','a','d','u','r' },
                                    { ' ','l','a','e','i','y','o','d' },
                                    { 'e','i','a',' ','o','t','r','u' },
                                    { 'e','t','o','a','k','h','l','r' },
                                    { ' ','e','i','u',',','.','o','a' },
                                    { 'n','s','r','c','t','l','a','i' },
                                    { 'l','e','o','i','r','a','t','p' },
                                    { 'e','a','o','i','p',' ','b','m' } };
            string ret="";
            char temp;
            for (int i = 0; i < str.Length - 1; i++)
            {
                temp = (char)str[i];

                if ( (str[i] & 0x80)!=0  )
                {
                    char index1 =(char)((str[i] >> 3) & 0xF);
                    char index2 = (char)(str[i] & 0x7);
                     ret += decodeTable1[index1];
                     ret += decodeTable2[index1,index2];
                }
                else
                {
                    ret += (char)str[i];
                }
            }
            UInt16 count;
            return ret;
        }
        public void DrawMentatSubButton()
        {
            //если изменить SriteType=6 DrawMode=11, то нужно переложить их в Texture0 , которая по размерности равна игровому окну.
            //Изначально эти спрайты лежат в размерности 2048 на 2048.
            //что если держать размерность одинаковую с игровым экраном.
            //патченные так и будут идти через фб1 и сохраняться обратно в 2048 на 2048

            //нужно брать оригинал с тех же координат, что и маска.
            //маска должна лежать по координатам игрового мира. ПОэтому маска должна быть в отдельном текстуре размером с игровой мир, чтобы в шейдере
            //можно было ее использовать, но не рисовать.
            //Game.Renderer.SpriteRenderer.DrawSprite(mentatbtnNextSprite, new float3(RenderBounds.X + 171 * ratio, RenderBounds.Y + 170 * ratio, 0), 0, new float3(63 * ratio, 23 * ratio, 0));
            //Game.Renderer.SpriteRenderer.DrawSprite(mentatbtnRepeatSprite, new float3(RenderBounds.X + 242 * ratio, RenderBounds.Y + 170 * ratio, 0), 0, new float3(63 * ratio, 23 * ratio, 0));
        }

        public List<Animation> AnimList = new List<Animation>();
        public List<AnimationString> AnimStringList = new List<AnimationString>();

        public void SendTickToAnimations()
        {
            foreach (Animation a in AnimList)
            {
                a.Tick();
            }
            foreach (AnimationString a in AnimStringList)
            {
                a.Tick();
            }
        }

        public enum DrawFrameEnum
        {
            Map, Houses, Fame, Mentat
        }

        public DrawFrameEnum DrawFrame = DrawFrameEnum.Houses;

        public override void Draw()
        {
            //тестовый код для debug framebuffer`а
            //Game.Renderer.PixelDumpRenderer.fb.ReBind((ITextureInternal)maproomT);
            //sp3.SpriteType = 2;
            //sp3.Stretched = true;
            //Game.Renderer.PixelDumpRenderer.DrawSprite(sp3, new float3(0, 0, 0), new float3(RenderBounds.Width, RenderBounds.Height, 0), prbase);
            //Game.Renderer.PixelDumpRenderer.Flush(); // тут произойдет сброс всех пикселей в текстуру у FB1.
            //Game.Renderer.PixelDumpRenderer.fb.Unbind();

            //return;
            switch (DrawFrame)
            {
                case DrawFrameEnum.Map:
                    DrawMap();
                    break;
                case DrawFrameEnum.Houses:
                    DrawHouses();
                    break;
                case DrawFrameEnum.Fame:
                    DrawFame();
                    break;
                case DrawFrameEnum.Mentat:
                    DrawMentat();
                    break;

                default:
                    break;
            }


        }

        public int AnimationStep = 5;
        bool flaghousepicked = false;

        public void ResetCampaign()
        {
            CurrentLevel = 1;
            //SwitchToMap = false;
            DrawFrame = DrawFrameEnum.Houses;
            this.Get<ContainerWidget>("mentatstage").Visible = false;
            BindLevelOnMap(CurrentLevel);

        }

        public override void TickOuter()
        {
            newframe = true;
            if (Clicked)
            {
                if (DrawFrame == DrawFrameEnum.Mentat || DrawFrame == DrawFrameEnum.Fame) //так как не нужно вычислять через fb цвет, то для этогосостояния нечего делать- простовыходим.
                {
                    return;
                }

                Game.Renderer.PixelDumpRenderer.fb.ReBind((ITextureInternal)textemp);
                Game.Renderer.SpriteRenderer.SetFrameBufferMaskMode(true);

                if (DrawFrame == DrawFrameEnum.Map)
                {
                    DrawMap();
                }
                if (DrawFrame == DrawFrameEnum.Houses)
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
                if (DrawFrame == DrawFrameEnum.Map)
                {
                    OnMapRegionChooseDelegate(lastanswer[2], lastanswer[1], lastanswer[0]);
                    DrawFrame = DrawFrameEnum.Mentat;
                }

                if (DrawFrame == DrawFrameEnum.Houses)
                {
                    if (CampaignData.Players[0].Color == feedbackcolor)
                    {
                        CurrentFaction = selectableFactions.Where(f => f.InternalName == CampaignData.Players[0].Name).First();
                        flaghousepicked = true;
                    }
                    if (CampaignData.Players[1].Color == feedbackcolor)
                    {
                        CurrentFaction = selectableFactions.Where(f => f.InternalName == CampaignData.Players[1].Name).First();
                        flaghousepicked = true;
                    }
                    if (CampaignData.Players[2].Color == feedbackcolor)
                    {
                        CurrentFaction = selectableFactions.Where(f => f.InternalName == CampaignData.Players[2].Name).First();
                        flaghousepicked = true;
                    }


                }
                if (flaghousepicked)
                {
                    //SwitchToMap = true;
                    DrawFrame = DrawFrameEnum.Map;
                    OnHouseChooseDelegate(CurrentFaction.Name);
                    flaghousepicked = false;
                }
                if (DrawFrame == DrawFrameEnum.Fame)
                {
                    OnMapRegionChooseDelegate(lastanswer[2], lastanswer[1], lastanswer[0]);
                }
                if (DrawFrame == DrawFrameEnum.Mentat)
                {
                    SetupSubMentat(cLevel.Brief.SubBkgSequence, cLevel.Brief.SubBkgSequenceGroup,cLevel.Brief.DbIndex, AnimationDirectionEnum.Repeat);
                    OnMapRegionChooseDelegate(lastanswer[2], lastanswer[1], lastanswer[0]);
                }
                Clicked = false;

            }
            SendTickToAnimations();
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
        private ITexture fameroomT;
        private ITexture fameroom2T;
        private ITexture fameroom3T;
        private Sprite fameatr;
        private Sprite famehark;
        private Sprite fameordos;
        private Sprite mentatHarkSprite;
        private Sprite mentatAtrSprite;
        private Sprite mentatOrdosSprite;

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
