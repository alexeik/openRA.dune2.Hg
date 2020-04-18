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
using System.Runtime.InteropServices;
using OpenRA;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.D2.FileFormats;
using OpenRA.Mods.D2.SpriteLoaders;
using OpenRA.Mods.D2.Widgets.Logic;
using OpenRA.Primitives;
using OpenRA.Traits;
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
        public List<FrameSoundLine> frameSoundLine;
        public List<FrameTextLine> frameTextLine;
        Animation anim1, anim2;
        World world;

        public WsaPlayerWidget() //этот виджет, загрузиться, когда загрузиться shellmap, поэтому метод WorldLoaded заполнит нам world
        {
            LoadPalette();
            world = Game.worldRenderer.World;
            //anim1 = new Animation(world, "video1");
            //anim1.Play("play");
            //  anim1 = new Animation(null, ""); // нужно иметь заполненым переменную world.Map.Rules.Sequences;

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
                if (filename.Contains("INTRO"))
                {
                    LoadPalette();
                }
                else
                {
                    LoadPaletteWSA();
                }
                anim1.ChangeSequenceGroup(filename.Replace(".", ""));
                anim1.Play("play");
                SetSize(anim1.Image.Size.XY.ToInt2().X, anim1.Image.Size.XY.ToInt2().Y);
            }
            else
            {
                //var video1 = new CpsD2Loader(filename);
                //cachedVideo = filename;
                //Open(video1);

            }

        }

        void LoadPalette()
        {
            pr = Game.worldRenderer.Palette("d2"); //d2 палитра назначена в d2\rules\palettes.yaml
        }
        void LoadPaletteWSA()
        {
            pr = Game.worldRenderer.Palette("westwood"); //d2 палитра назначена в d2\rules\palettes.yaml
        }
        void LoadPalette(ImmutablePalette cpspalette, string customname)
        {
            try
            {
                pr = Game.worldRenderer.Palette(customname);
                return;
            }
            catch (Exception)
            {
                pr = null;

            }

            if (pr == null)
            {
                Game.worldRenderer.AddPalette(customname, cpspalette, false, false);
                pr = Game.worldRenderer.Palette(customname);
            }
            //palette = cpspalette;
            //hardwarePalette = new HardwarePalette();
            //hardwarePalette.AddPalette(customname, palette, false);
            //hardwarePalette.Initialize();
            //Game.Renderer.SetPalette(hardwarePalette);
            //var pal = hardwarePalette.GetPalette(customname);
            //pr = new PaletteReference(customname+"ref", hardwarePalette.GetPaletteIndex(customname), pal, hardwarePalette);
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
        public void Open(CpsD2Loader video)
        {
            this.image = video;

            stopped = true;
            paused = true;
            //onComplete = () => { };
            TypeDictionary metadata;
            ImmutablePalette cpspalette;
            using (var stream = Game.ModData.DefaultFileSystem.Open(image.SpriteFilename))
            {


                video.TryParseSpritePlusPalette(stream, out imageSprite, out metadata, out cpspalette);
                if (cpspalette != null)
                {
                    LoadPalette(cpspalette, image.SpriteFilename);
                }
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
        FrameTextLine prevText = null;
        long PauseInSeconds = 5;
        long DrawPrevFrameEveryXMs = 300;


        public void DrawWsaText(FrameTextLine ftl)
        {
            var textSize = Game.Renderer.Fonts["Original"].Measure(ftl.Text);
            Game.Renderer.Fonts["Original"].DrawText(ftl.Text, ftl.Pos, ftl.TextColor);
            // Game.Renderer.Fonts["Original"].DrawTextWithShadow(ftl.Text, ftl.Pos, ftl.TextColor, Color.FromArgb(150, 0, 0), Color.FromArgb(100,100, 0, 0), 2);
            //Game.Renderer.Fonts["Original"].DrawTextWithContrast(ftl.Text, ftl.Pos, ftl.TextColor, Color.FromArgb(150, 0, 0), Color.FromArgb(100, 255, 255, 255), 1);
        }

        public override void Draw()
        {
            if (anim1.CurrentSequence.Length == anim1.CurrentFrame + 1)
            {
                if (VideoStackList.Count == 0)
                {
                    Exit();
                   
                }
                if (VideoStackList.Count > 0)
                {
                    Load(VideoStackList.Dequeue());
                }

            }

            anim1.Tick();
            Game.Renderer.SpriteRenderer.DrawSprite(anim1.Image, videoOrigin, pr, videoSize);
          
        }
        public void Draw2()
        {
            //вызовы SetPalette в Draw для UI элементов, конкурируют с RefreshPalette в World.Draw.
            //Game.Renderer.SetPalette(hardwarePalette); //теперь не нужно, так как обнаружен файл palettes.yaml, в котором все палитры есть и сделано через него.

            if (String.IsNullOrEmpty(cachedVideo))
                return;
            //if (video==null)
            //{
            //    return;
            //}
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
                        Load(VideoStackList.Dequeue());
                        lastDrawTime = Game.RunTime;
                        stopped = paused = false;
                        return;
                    }
                    //stop video 
                }
                //нужно остановить медиа=сцену и передать контроль дальше
                cachedVideo = null;
                Exit();
                return;
            }
            if (CountForWaitNextFrameMs < DrawPrevFrameEveryXMs) //code runs every tick before Next Video frame to fill the gap
            {


                if (prevSprite != null)
                {

                    //just draw the same frame 
                    Game.Renderer.SpriteRenderer.DrawSprite(prevSprite, videoOrigin, pr, videoSize);


                }
                if (prevText != null)
                {
                    DrawWsaText(prevText);
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
                        if (prevText != null)
                        {
                            DrawWsaText(prevText);
                        }

                        return;
                    }

                    //if not last frame of video, move next frame
                    video.AdvanceFrame();
                }
                if (image != null && prevSprite != null)
                {


                    Game.Renderer.SpriteRenderer.DrawSprite(prevSprite, videoOrigin, pr, videoSize);
                    CountForPause += deltaScale; //incerease CountForPause to enter at if (CountForPause > PauseInSeconds * 1000)
                    lastDrawTime = Game.RunTime;
                    return;
                }

                if (prevText != null)
                {
                    DrawWsaText(prevText);
                }
                //||
                //\/               
            }

            if (video != null)
            {
                if (frameSoundLine == null)
                {

                }

                else
                {
                    if (frameSoundLine.Contains(new FrameSoundLine() { WSAfilename = cachedVideo, FrameNumber = video.CurrentFrame }))
                    {

                        var vocfilename = frameSoundLine.Find(x => x.WSAfilename.Contains(cachedVideo) && x.FrameNumber == video.CurrentFrame).VOCfilename;
                        if (vocfilename.Contains("ADL"))
                        {
                            IReadOnlyFileSystem fileSystem = Game.ModData.DefaultFileSystem;

                            if (!fileSystem.Exists(vocfilename))
                            {
                                Log.Write("sound", "LoadSound, file does not exist: {0}", vocfilename);

                            }
                            DuneMusic.Quit();
                            DuneMusic.Init(44100, "", DuneMusic.DuneMusicOplEmu.kOplEmuNuked);

                            using (var stream = fileSystem.Open(vocfilename))
                            {

                                DuneMusic.InsertMemoryFile("test", stream.ReadAllBytes());
                                byte[] temp = new byte[28106880];

                                UIntPtr temp3;
                                temp3 = (UIntPtr)1000000;
                                temp3 = DuneMusic.SynthesizeAudio("test", 52, -1, temp, (UIntPtr)temp.Length);
                                ISoundSource soundSource;
                                soundSource = Game.Sound.soundEngine.AddSoundSourceFromMemory(temp, 2, 16, 44100);
                                ISound temp2 = Game.Sound.soundEngine.Play2D(Game.LocalTick, soundSource, false, true, WPos.Zero, 100, false);

                            }



                        }
                        else
                        {
                            Game.Sound.Play(SoundType.UI, vocfilename);
                        }

                    }
                }
                if (frameTextLine == null)
                {
                    prevText = new FrameTextLine() { Text = "" };
                    DrawWsaText(prevText);

                }

                else
                {
                    if (frameTextLine.Contains(new FrameTextLine() { WSAfilename = cachedVideo, FrameNumber = video.CurrentFrame }))
                    {
                        FrameTextLine ft = frameTextLine.Find(x => x.WSAfilename.Contains(cachedVideo) && x.FrameNumber == video.CurrentFrame);

                        DrawWsaText(ft);
                        prevText = ft;
                    }

                }
            }
            var sheetBuilder = new SheetBuilder(SheetType.Indexed, 512);

            //router for WSA  frame or CPS frame
            Sprite videoSprite = null;
            if (cachedVideo.Contains("WSA"))
            {

                videoSprite = sheetBuilder.Add(video.Frame);
            }
            else
            {

                //videoSprite = new Sprite(sheetBuilder.Current, new Rectangle(0, 0, 320, 200), TextureChannel.RGBA);
                videoSprite = sheetBuilder.Add(imageSprite[0]);
                //дампинг ресурсов игры в png
                //videoSprite.Sheet.CreateBuffer();
                //videoSprite.Sheet.ReleaseBuffer();
                ////videoSprite.Sheet.AsPng().Save("VIRGIN.png");
                //IPalette exppal;
                //try
                //{
                //    exppal = hardwarePalette.GetPalette(cachedVideo);
                //}
                //catch (Exception)
                //{

                //    exppal = null;
                //}

                //if (exppal==null)
                //{
                //    LoadPalette();
                //    videoSprite.Sheet.AsPng(TextureChannel.Blue, hardwarePalette.GetPalette("chrome")).Save(cachedVideo + ".png");
                //}
                //else
                //{
                //    videoSprite.Sheet.AsPng(TextureChannel.Blue, exppal).Save(cachedVideo + ".png");
                //}

            }
            prevSprite = videoSprite;


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
        public void Exit()
        {
            if (onComplete == null)
            {
                return;
            }
            Game.RunAfterTick(onComplete);
        }
        public void CloseVideo()
        {
            Stop();
            video = null;
        }


    }
}
