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

using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;
using System;
using System.Collections.Generic;

namespace OpenRA.Mods.D2.Widgets.Logic
{
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
        public string Text;
        public float2 Pos;
        public Color TextColor;
        public bool Equals(FrameTextLine other)
        {
            if (other == null) return false;

            if (this.WSAfilename == other.WSAfilename && this.FrameNumber == other.FrameNumber)
            {
                return true;
            }
            return false;
        }

    }
    public class Dune2VideoWSAPlayerLogic : ChromeLogic
    {
        //static bool promptAccepted = false;
        private ModData modData;
        private BackgroundWidget fullscreenVideoPlayer;
        
        [ObjectCreator.UseCtor]
        public Dune2VideoWSAPlayerLogic(Widget widget, World world, ModData modData)
        {
            this.modData = modData;
            fullscreenVideoPlayer = widget.Get<BackgroundWidget>("MAINMENU_PRERELEASE_NOTIFICATION");
            //fullscreenVideoPlayer = Ui.LoadWidget<BackgroundWidget>("MAINMENU_PRERELEASE_NOTIFICATION", Ui.Root, new WidgetArgs { { "world", world } });
            var fsPlayer = fullscreenVideoPlayer.Get<WsaPlayerWidget>("PLAYER");
            fullscreenVideoPlayer.Visible = true;
            fsPlayer.VideoStackList = new System.Collections.Generic.Queue<string>();
         
            fsPlayer.VideoStackList.Enqueue("WESTWOOD.WSA");
            fsPlayer.VideoStackList.Enqueue("AND.ENG");
            fsPlayer.VideoStackList.Enqueue("VIRGIN.CPS");
            //fsPlayer.VideoStackList.Enqueue("SCREEN.CPS");
            fsPlayer.VideoStackList.Enqueue("INTRO1.WSA");
            fsPlayer.VideoStackList.Enqueue("INTRO2.WSA");
            fsPlayer.VideoStackList.Enqueue("INTRO3.WSA");
            fsPlayer.VideoStackList.Enqueue("INTRO4.WSA");
            //fsPlayer.VideoStackList.Enqueue("INTRO5.WSA");
            //fsPlayer.VideoStackList.Enqueue("INTRO6.WSA");
            //fsPlayer.VideoStackList.Enqueue("INTRO7A.WSA");
            //fsPlayer.VideoStackList.Enqueue("INTRO7B.WSA");
            //fsPlayer.VideoStackList.Enqueue("INTRO8A.WSA");
            //fsPlayer.VideoStackList.Enqueue("INTRO8B.WSA");
            //fsPlayer.VideoStackList.Enqueue("INTRO8C.WSA");
            //fsPlayer.VideoStackList.Enqueue("INTRO9.WSA");
            //fsPlayer.VideoStackList.Enqueue("INTRO10.WSA");
            //fsPlayer.VideoStackList.Enqueue("INTRO11.WSA");

            List<FrameSoundLine> fl = new List<FrameSoundLine>();
            fl.Add(new FrameSoundLine() { WSAfilename = "INTRO1.WSA", FrameNumber = 0, VOCfilename = "DUNE0.ADL" });
            fl.Add(new FrameSoundLine() { WSAfilename = "INTRO1.WSA", FrameNumber = 31, VOCfilename = "DUNE.VOC" });
            fl.Add(new FrameSoundLine() { WSAfilename = "INTRO1.WSA", FrameNumber =37, VOCfilename = "BLDING.VOC" });
            fl.Add(new FrameSoundLine() { WSAfilename = "INTRO1.WSA", FrameNumber = 48, VOCfilename = "DYNASTY.VOC" });
            fsPlayer.frameSoundLine = fl;

            List<FrameTextLine> ftl = new List<FrameTextLine>();
            ftl.Add(new FrameTextLine() { WSAfilename = "INTRO1.WSA", FrameNumber = 37, Text="The Building of A Dynasty",Pos=new float2(230,560) ,TextColor=Color.FromArgb(250,0,32)});
            ftl.Add(new FrameTextLine() { WSAfilename = "INTRO2.WSA", FrameNumber = 0, Text = "", Pos = new float2(230, 560), TextColor = Color.FromArgb(250, 0, 32) });
            fsPlayer.frameTextLine = ftl;
           // PlayVideoStack(fsPlayer, () => { });
            PlayVideoStack(fsPlayer, () => ShowMainMenu(world));

        }

        void ShowMainMenu(World world)
        {
            
            //promptAccepted = true;
            Ui.ResetAll();
            Ui.CloseWindow();
            Game.LoadWidget(world, "MAINMENU", Ui.Root, new WidgetArgs());
        }
        void PlayVideo(WsaPlayerWidget player, string video,  Action onComplete = null)
        {
            if (!modData.DefaultFileSystem.Exists(video))
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
        void PlayVideoStack(WsaPlayerWidget player, Action onComplete = null)
        {
            if (player.VideoStackList.Count==0)
            {
                return;
            }
            string videowsa = player.VideoStackList.Dequeue();
            if (!modData.DefaultFileSystem.Exists(videowsa))
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
    }
}
