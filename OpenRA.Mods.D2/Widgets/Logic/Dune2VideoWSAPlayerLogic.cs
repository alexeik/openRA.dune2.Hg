#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.D2.Widgets.Logic
{
    public class FrameVideoLine : IEquatable<FrameVideoLine>
    {
        public string WSAfilename;
        public int PauseInTicks;
        public bool Equals(FrameVideoLine other)
        {
            if (other == null) return false;

            if (this.WSAfilename == other.WSAfilename)
            {
                return true;
            }
            return false;
        }

    }
    public class FrameSoundLine : IEquatable<FrameSoundLine>
    {
        public string WSAfilename;
        public int FrameNumber;
        public string VOCfilename;
        public bool Equals(FrameSoundLine other)
        {
            if (other == null) return false;

            if (this.WSAfilename==other.WSAfilename && this.FrameNumber==other.FrameNumber)
            {
                return true;
            }
            return false;
        }

    }
    public class FrameTextLine : IEquatable<FrameTextLine>
    {
        public string WSAfilename;
        public int FrameNumber;
        public int Duration;
        public string Text;
        public float2 Pos;
        public Color TextColor;

        public int FrameDuration;
        public bool Equals(FrameTextLine other)
        {
            if (other == null) return false;
            if (other.FrameNumber ==0)
            {
                if (this.WSAfilename == other.WSAfilename)
                {
                    return true;
                }
            }
            else
            {
                if (this.WSAfilename == other.WSAfilename && this.FrameNumber == other.FrameNumber)
                {
                    return true;
                }
            }
           

            return false;
        }

    }
    public class Dune2VideoWSAPlayerLogic : ChromeLogic
    {
        //static bool promptAccepted = false;
        private ModData modData;
        private BackgroundWidget fullscreenVideoPlayer;
        List<MiniYamlNode> ppnodes;
        List<FrameTextLine> ftl = new List<FrameTextLine>();

        [ObjectCreator.UseCtor]
        public Dune2VideoWSAPlayerLogic(Widget widget, World world, ModData modData)
        {
            this.modData = modData;
            fullscreenVideoPlayer = widget.Get<BackgroundWidget>("MAINMENU_PRERELEASE_NOTIFICATION");
            //fullscreenVideoPlayer = Ui.LoadWidget<BackgroundWidget>("MAINMENU_PRERELEASE_NOTIFICATION", Ui.Root, new WidgetArgs { { "world", world } });
            var fsPlayer = fullscreenVideoPlayer.Get<WsaPlayerWidget>("PLAYER");
            fullscreenVideoPlayer.Visible = true;
            fsPlayer.VideoStackList = new System.Collections.Generic.Queue<FrameVideoLine>();

            LoadYaml();

            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "WESTWOOD.WSA" });
            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "AND.ENG" });
            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "VIRGIN.CPS" });
            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "SCREEN.CPS" });
            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "INTRO1.WSA" });
            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "INTRO2.WSA" });
            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "INTRO3.WSA" });
            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "INTRO9.WSA" });
            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "INTRO10.WSA" });
            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "INTRO11.WSA" });
            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "INTRO4.WSA" });
            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "INTRO6.WSA" });
            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "INTRO7A.WSA" });
            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "INTRO7B.WSA" });
            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "INTRO8A.WSA" });
            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "INTRO8B.WSA" });
            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "INTRO8C.WSA" });
            fsPlayer.VideoStackList.Enqueue(new FrameVideoLine() { WSAfilename = "INTRO5.WSA" });


            List<FrameSoundLine> fl = new List<FrameSoundLine>();
            fl.Add(new FrameSoundLine() { WSAfilename = "INTRO1.WSA", FrameNumber = 1, VOCfilename = "DUNE0.ADL" });
            fl.Add(new FrameSoundLine() { WSAfilename = "INTRO2.WSA", FrameNumber = 1, VOCfilename = "123.WAV" });
            fl.Add(new FrameSoundLine() { WSAfilename = "INTRO1.WSA", FrameNumber = 30, VOCfilename = "DUNE.VOC" });
            fl.Add(new FrameSoundLine() { WSAfilename = "INTRO1.WSA", FrameNumber =37, VOCfilename = "BLDING.VOC" });
            fl.Add(new FrameSoundLine() { WSAfilename = "INTRO1.WSA", FrameNumber = 40, VOCfilename = "DYNASTY.VOC" });
            fsPlayer.frameSoundLine = fl;

            TraitPair<MusicPlaylist> playList = world.ActorsWithTrait<MusicPlaylist>().FirstEnabledTraitOrDefault();
            //playList.Trait.Play(playList.Trait.AvailablePlaylist()[0]);

            foreach (var i in fl)
            {
                if (!i.VOCfilename.Contains(".ADL"))
                {
                    Game.Sound.PrecacheSound(i.VOCfilename);
                }
                if (i.VOCfilename.Contains(".ADL"))
                {
                    Game.Sound.PrecacheMusic(playList.Trait.AvailablePlaylist()[0]);
                }
            }

            //ftl.Add(new FrameTextLine() { WSAfilename = "INTRO1.WSA", FrameNumber = 37, FrameDuration=15 , Text="The Building of A Dynasty",Pos=new float2(400,560) ,TextColor=Color.FromArgb(250,0,32)});
            //ftl.Add(new FrameTextLine() { WSAfilename = "INTRO2.WSA", FrameNumber = 0, Text = "", Pos = new float2(230, 560), TextColor = Color.FromArgb(250, 0, 32) });
            fsPlayer.frameTextLine = ftl;
            // PlayVideoStack(fsPlayer, () => { });
            PlayVideoStack(fsPlayer, () => ShowMainMenu(world));

        }
        void LoadYaml()
        {
            var fileSystem = Game.worldRenderer.World.Map;
            try
            {
                ppnodes = MiniYaml.Merge(modData.Manifest.Timelines
               .Select(s => MiniYaml.FromStream(fileSystem.Open(s), s)));
            }
            catch (Exception)
            {


            }

            foreach (MiniYamlNode kvp in ppnodes)
            {
               
                Dictionary<string,MiniYaml> timeline = kvp.Value.ToDictionary();
                int2 Resolution = LoadField<int2>(timeline, "Resolution",new int2(0,0));
               

                foreach (MiniYamlNode fr2 in timeline["Sequences"].Nodes)
                {
                    string WSA = fr2.Key;
                    Dictionary<string, MiniYaml> frames = fr2.Value.ToDictionary();
                    foreach (MiniYamlNode fr in frames["Frames"].Nodes)
                    {
                        Dictionary<string, MiniYaml> item = fr.Value.ToDictionary();
                        float2 Pos = LoadField<float2>(item, "TextPosition", new float2(0, 0));
                        int Duration = LoadField<int>(item, "Duration", 0);
                        ftl.Add(new FrameTextLine() { WSAfilename = WSA, Duration = Duration, FrameNumber = Convert.ToInt32(fr.Key), FrameDuration = 15, Text = item["Text"].Value, Pos = Pos, TextColor = Color.FromArgb(250, 0, 32) });
                    }

                }

               
               
            }
        }

        protected static T LoadField<T>(Dictionary<string, MiniYaml> d, string key, T fallback)
        {
            MiniYaml value;
            if (d.TryGetValue(key, out value))
                return FieldLoader.GetValue<T>(key, value.Value);

            return fallback;
        }

        void ShowMainMenu(World world)
        {
            
            //promptAccepted = true;
            Ui.ResetAll();
            Ui.CloseWindow();
            Game.LoadWidget(world, "MAINMENU", Ui.Root, new WidgetArgs());
        }
        
        void PlayVideoStack(WsaPlayerWidget player, Action onComplete = null)
        {
            if (player.VideoStackList.Count==0)
            {
                onComplete();
                return;
            }
            FrameVideoLine videowsa = player.VideoStackList.Dequeue();
            if (!modData.DefaultFileSystem.Exists(videowsa.WSAfilename))
            {
                ConfirmationDialogs.ButtonPrompt(
                    title: "Video not installed",
                    text: "The game videos can be installed from the\n\"Manage Content\" menu in the mod chooser.",
                    cancelText: "Back",
                    onCancel: () => { });
            }
            else
            {
                //StopVideo(player);

                // = pv;
                player.Load(videowsa);

                // video playback runs asynchronously
                player.PlayThen(() =>
                {
                    //StopVideo(player);
                    if (onComplete != null)
                        onComplete();
                });

                // Mute other distracting sounds
                //MuteSounds();
            }
        }
        void StopVideo(WsaPlayerWidget player)
        {
            //if (playingVideo == PlayingVideo.None)
            //    return;

            //UnMuteSounds();
            player.Stop();
            //playingVideo = PlayingVideo.None;
        }
        void PlayVideo(WsaPlayerWidget player, FrameVideoLine video, Action onComplete = null)
        {
            if (!modData.DefaultFileSystem.Exists(video.WSAfilename))
            {
                ConfirmationDialogs.ButtonPrompt(
                    title: "Video not installed",
                    text: "The game videos can be installed from the\n\"Manage Content\" menu in the mod chooser.",
                    cancelText: "Back",
                    onCancel: () => { });
            }
            else
            {
                StopVideo(player);

                // = pv;
                player.Load(video);

                // video playback runs asynchronously
                player.PlayThen(() =>
                {
                    //StopVideo(player);
                    if (onComplete != null)
                        onComplete();
                });

                // Mute other distracting sounds
                //MuteSounds();
            }
        }
    }
}
