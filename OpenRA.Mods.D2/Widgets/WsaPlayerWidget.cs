#region Copyright & License Information
/*
 * Copyright 2007-2019 The d2 mod Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using ManagedBass;
using OpenRA.Graphics;
using OpenRA.Mods.D2.FileFormats;
using OpenRA.Mods.D2.SpriteLoaders;
using OpenRA.Mods.D2.Widgets.Logic;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.D2.Widgets
{
    public class WsaPlayerWidget : Widget
    {
        public Hotkey CancelKey = new Hotkey(Keycode.ESCAPE, Modifiers.None);
        public float AspectRatio = 1.2f;
        public bool Skippable = true;

        public bool Paused { get { return paused; } }
        public WsaReader Video { get { return video; } }

        WsaReader video = null;
        CpsD2Loader image = null;
        ISpriteFrame[] imageSprite = null;


        //FrameLine for VOC files. WSAName + FrameNumber + VOC file.

        string cachedVideo;
        float2 videoOrigin, videoSize;
        bool stopped;
        bool paused;

        ImmutablePalette palette;
        HardwarePalette hardwarePalette;
        PaletteReference pr;

        Action onComplete;
        public Queue<string> VideoStackList;
        public List<FrameLine> frameline;


        public WsaPlayerWidget()
        {
            LoadPalette();
        }

        public void Load(string filename)
        {
            if (filename == cachedVideo)
                return;
            image = null;
            video = null;
            prevSprite = null;
            if (filename.Contains("WSA"))
            {
                var video = new WsaReader(Game.ModData.DefaultFileSystem.Open(filename));
                cachedVideo = filename;
                Open(video);
            }
            else
            {
                var video = new CpsD2Loader(filename);
                cachedVideo = filename;
                Open(video);

            }
          
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

        public void Open(WsaReader video)
        {
            this.video = video;

            stopped = true;
            paused = true;
            //onComplete = () => { };

            var size = Math.Max(video.Width, video.Height);
            var textureSize = Exts.NextPowerOf2(size);

            var scale = Math.Min((float)RenderBounds.Width / video.Width, (float)RenderBounds.Height / video.Height * AspectRatio);
            videoOrigin = new float2(
                RenderBounds.X + (RenderBounds.Width - scale * video.Width) / 2,
                RenderBounds.Y + (RenderBounds.Height - scale * video.Height * AspectRatio) / 2);

            // Round size to integer pixels. Round up to be consistent with the scale calculation.
            videoSize = new float2((int)Math.Ceiling(video.Width * scale), (int)Math.Ceiling(video.Height * AspectRatio * scale));
        }
        public void Open(CpsD2Loader video)
        {
            this.image = video;

            stopped = true;
            paused = true;
            //onComplete = () => { };
            TypeDictionary metadata;
            using (var stream = Game.ModData.DefaultFileSystem.Open(image.SpriteFilename))
            {

               
                video.TryParseSprite(stream, out imageSprite, out metadata);

            }
            var imwidth = imageSprite[0].FrameSize.Width;
            var imheight = imageSprite[0].FrameSize.Height;


            var size = Math.Max(imwidth, imheight);
            var textureSize = Exts.NextPowerOf2(size);

            var scale = Math.Min((float)RenderBounds.Width / imwidth, (float)RenderBounds.Height / imheight * AspectRatio);
            videoOrigin = new float2(
                RenderBounds.X + (RenderBounds.Width - scale * imwidth) / 2,
                RenderBounds.Y + (RenderBounds.Height - scale * imheight * AspectRatio) / 2);

            // Round size to integer pixels. Round up to be consistent with the scale calculation.
            videoSize = new float2((int)Math.Ceiling(imwidth * scale), (int)Math.Ceiling(imheight * AspectRatio * scale));
        }

        long lastDrawTime = 0;
        long CountForWaitNextFrameMs = 0;
        long CountForPause = 0;
        Sprite prevSprite = null;
        long PauseInSeconds = 5;
        long DrawPrevFrameEveryXMs = 300;
        public void PlayMp3()
        {
            Bass.Init(); // Initialize with default options.

            int handle = Bass.CreateStream(@"D:\games.dev\dune2.mod\mods\d2\audio\INTO.ogg");

            Bass.ChannelPlay(handle); // Begin Playback.

          

            //Bass.ChannelStop(handle); // Stop Playback.

            

            //Bass.Free(); // Free the device.
        }
        public override void Draw()
        {
            if (String.IsNullOrEmpty(cachedVideo ))
                return;
            if (video==null)
            {
                return;
            }
            //Game.RunTime MilliSeconds 1s=1000ms=50ms*20times
            var deltaScale = Game.RunTime - lastDrawTime;
            CountForWaitNextFrameMs += deltaScale;

            //if we need pause wait it

            if (CountForPause > PauseInSeconds * 1000)
            {
                if (VideoStackList != null)
                {//only move next, first video must be dequed from PlayVideoStack
                    if (VideoStackList.Count > 0)
                    {
                        CountForPause = 0;
                        CountForWaitNextFrameMs = 0;
                        //PlayMp3();
                        //Game.Sound.Play(SoundType.UI, "3HOUSES.VOC");
                        //Game.Sound.Play(SoundType.UI, "BEGINS.VOC");
                        Load(VideoStackList.Dequeue());
                        lastDrawTime = Game.RunTime;
                        stopped = paused = false;
                        return;
                    }
                }

                CloseVideo();
                return;
            }
            if (CountForWaitNextFrameMs < DrawPrevFrameEveryXMs) //code runs every tick before Next Video frame to fill the gap
            {


                if (prevSprite != null)
                {
                    //just draw the same frame 
                   Game.Renderer.SpriteRenderer.DrawSprite(prevSprite, videoOrigin, pr, videoSize);


                }
                return;
            }
            else
            {
                if (video != null && prevSprite != null)
                {
                    if (video.CurrentFrame >= video.Length - 1) //this code runs every DrawFrameEveryXMilliseconds when video is ended.
                    {
                        //on video last frame draw always last frame
                        Game.Renderer.SpriteRenderer.DrawSprite(prevSprite, videoOrigin, pr, videoSize);
                        CountForPause += deltaScale; //incerease CountForPause to enter at if (CountForPause > PauseInSeconds * 1000)
                        lastDrawTime = Game.RunTime;

                        return;
                    }

                    //if not last frame of video, move next frame
                    video.AdvanceFrame();
                }
                if (image!=null && prevSprite!=null)
                {
                    Game.Renderer.SpriteRenderer.DrawSprite(prevSprite, videoOrigin, pr, videoSize);
                    CountForPause += deltaScale; //incerease CountForPause to enter at if (CountForPause > PauseInSeconds * 1000)
                    lastDrawTime = Game.RunTime;
                    return;
                }
                //||
                //\/               
            }

            if (video != null)
            {
                if (frameline == null)
                {

                }

                else
                {
                    if (frameline.Contains(new FrameLine() { WSAfilename = cachedVideo, FrameNumber = video.CurrentFrame }))
                    {
                        var vocfilename = frameline.Find(x => x.WSAfilename.Contains(cachedVideo) && x.FrameNumber == video.CurrentFrame).VOCfilename;
                        Game.Sound.Play(SoundType.UI, vocfilename);
                    }
                }
            }
            var sheetBuilder = new SheetBuilder(SheetType.Indexed, 512);

            //router for WSA  frame or CPS frame
            Sprite videoSprite=null;
            if (cachedVideo.Contains("WSA"))
            {
                 videoSprite = sheetBuilder.Add(video.Frame);
            }
            else
            {
                 videoSprite = sheetBuilder.Add(imageSprite[0]);
            }
            prevSprite = videoSprite;

           
            //Game.Renderer.EnableScissor(RenderBounds);
            //Game.Renderer.RgbaColorRenderer.FillRect(
            //    new float2(RenderBounds.Left, RenderBounds.Top),
            //    new float2(RenderBounds.Right, RenderBounds.Bottom), OpenRA.Primitives.Color.Black);
            //Game.Renderer.DisableScissor();

            Game.Renderer.SpriteRenderer.DrawSprite(videoSprite, videoOrigin, pr, videoSize);


            CountForWaitNextFrameMs = 0;
            lastDrawTime = Game.RunTime;
        }

        public override bool HandleKeyPress(KeyInput e)
        {
            if (Hotkey.FromKeyInput(e) != CancelKey || e.Event != KeyInputEvent.Down || !Skippable)
                return false;

            Stop();
            return true;
        }

        public override bool HandleMouseInput(MouseInput mi)
        {
            return RenderBounds.Contains(mi.Location) && Skippable;
        }

        public override string GetCursor(int2 pos)
        {
            return null;
        }

        public void Play()
        {
            PlayThen(() => { });
        }

        public void PlayThen(Action after)
        {
            if (video == null)
                return;

            onComplete = after;

            stopped = paused = false;
        }

        public void Pause()
        {
            if (stopped || paused || video == null)
                return;

            paused = true;
        }

        public void Stop()
        {
            if (stopped || video == null)
                return;

            stopped = true;
            paused = true;
            video.Reset();
            Game.RunAfterTick(onComplete);
        }

        public void CloseVideo()
        {
            Stop();
            video = null;
        }
    }
}
