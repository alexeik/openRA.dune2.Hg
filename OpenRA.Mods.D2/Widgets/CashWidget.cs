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
using OpenRA.Widgets;
using OpenRA.Primitives;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.D2.Widgets
{
    public class CashWidget : Widget
    {
        public int Size = 3;
        public Func<Color> GetColor;

        SequenceProvider sp;
        DefaultSpriteSequence dsp;
        Sprite[] numsprites;
        PaletteReference pr;
        Player player;
        PlayerResources playerResources;
        int2 prevnextoffset = new int2(0, 0);
        int2 aOffset = new int2(0, 1);
        int drawcallSkip = 0;
        bool decrease = true;
        string prevbalance;

        [ObjectCreator.UseCtor]
        public CashWidget(World world, WorldRenderer worldRenderer)
        {

            sp = world.Map.Rules.Sequences;
            dsp = (DefaultSpriteSequence)sp.GetSequence("cashnums", "nums");
            numsprites = new Sprite[10];
            numsprites[0] = dsp.GetSprite(0);
            numsprites[1] = dsp.GetSprite(1);
            numsprites[2] = dsp.GetSprite(2);
            numsprites[3] = dsp.GetSprite(3);
            numsprites[4] = dsp.GetSprite(4);
            numsprites[5] = dsp.GetSprite(5);
            numsprites[6] = dsp.GetSprite(6);
            numsprites[7] = dsp.GetSprite(7);
            numsprites[8] = dsp.GetSprite(8);
            numsprites[9] = dsp.GetSprite(9);
            pr = worldRenderer.Palette("d2");

            player = world.LocalPlayer;
            playerResources = player.PlayerActor.Trait<PlayerResources>();

        }

        protected CashWidget(CashWidget widget)
            : base(widget)
        {
            GetColor = widget.GetColor;
        }

        public override Widget Clone()
        {
            return new CashWidget(this);
        }


        public int GetIndexFromChar(char c)
        {
            int index = 0;
            switch (c)
            {
                case '1':
                    index = 1;
                    break;
                case '2':
                    index = 2;
                    break;
                case '3':
                    index = 3;
                    break;
                case '4':
                    index = 4;
                    break;
                case '5':
                    index = 5;
                    break;
                case '6':
                    index = 6;
                    break;
                case '7':
                    index = 7;
                    break;
                case '8':
                    index = 8;
                    break;
                case '9':
                    index = 9;
                    break;
                case '0':
                    index = 0;

                    break;

            }
            return index;
        }


        public override void Draw() 
        {
            string newbalance = (playerResources.Cash + playerResources.Resources).ToString().PadLeft(6,'0');
           

            if (string.IsNullOrEmpty(prevbalance ))
            {
                prevbalance = newbalance;
               
            }
            if (Convert.ToInt32(prevbalance)>Convert.ToInt32(newbalance))
            {
                decrease = true;

            }
            else

            {
                decrease = false;
            }
            if(prevbalance!=newbalance  )
            {
                aOffset += new int2(0, 1); //пока балансы разные, то продолжаем анимацию
            }
           
            if (aOffset.Y>10)
            {
                aOffset = new int2(0, 0);
                prevbalance = newbalance; //когда прошла анимация, то равняем балансы, это 8 шагов по пикселям.
           
            }

            var rb = RenderBounds;
            int Diffindex = 0;
            int previndex = 0;
            int2 offset1stnum = new int2(260 - 201, 5 - 1);
            //258x 4y
            int2 offsetXY = RenderOrigin + offset1stnum;
            int2 offsetnext=new int2(10, 0);

            
            int newindex = 0;
            bool flagDiffIndex = false;

            int2 offsetXY2= offsetXY;

            int animationOffset = 1;
           
            
            for (int i = 0; i < newbalance.Length; i++) // создаем столько полигонов, сколько цифр и рисуем каждый сразу.
            {
               
                newindex = GetIndexFromChar(newbalance[i]);
                previndex = GetIndexFromChar(prevbalance[i]);
                if (newbalance[i] != prevbalance[i])
                {
                    Game.Renderer.EnableScissor(new Rectangle(RenderOrigin + offset1stnum + new int2(0,-1), new Size(60, 8)));
                    //рисуем переходную анимацию из двух спрайтов

                    if (decrease)
                    {
                        Game.Renderer.Flush(); // делаем, это  тут, так как рисуем вне очереди, то управляем очередью.
                        Game.Renderer.sproc.AddCommand(4, 0, 0, 0, 0, new int2(0, 0), offsetXY2 + new int2(0, -8) + aOffset, numsprites[newindex].Size, numsprites[newindex], pr);
                        Game.Renderer.sproc.ExecCommandBuffer();

                        Game.Renderer.Flush(); // делаем, это  тут, так как рисуем вне очереди, то управляем очередью.
                        Game.Renderer.sproc.AddCommand(4, 0, 0, 0, 0, new int2(0, 0), offsetXY + aOffset, numsprites[previndex].Size, numsprites[previndex], pr);
                        Game.Renderer.sproc.ExecCommandBuffer();
                    }
                    else
                    {
                        Game.Renderer.Flush(); // делаем, это  тут, так как рисуем вне очереди, то управляем очередью.
                        Game.Renderer.sproc.AddCommand(4, 0, 0, 0, 0, new int2(0, 0), offsetXY - aOffset, numsprites[previndex].Size, numsprites[previndex], pr);
                        Game.Renderer.sproc.ExecCommandBuffer();

                        Game.Renderer.Flush(); // делаем, это  тут, так как рисуем вне очереди, то управляем очередью.
                        Game.Renderer.sproc.AddCommand(4, 0, 0, 0, 0, new int2(0, 0), offsetXY2 - new int2(0, -8) - aOffset, numsprites[newindex].Size, numsprites[newindex], pr);
                        Game.Renderer.sproc.ExecCommandBuffer();

                       
                    }
                    Game.Renderer.DisableScissor();
                }
                else

                {
                    //рисуем один спрайт числа
                    Game.Renderer.Flush(); // делаем, это  тут, так как рисуем вне очереди, то управляем очередью.
                    Game.Renderer.sproc.AddCommand(4, 0, 0, 0, 0, new int2(0, 0), offsetXY, numsprites[newindex].Size, numsprites[newindex], pr);

                    Game.Renderer.sproc.ExecCommandBuffer();
                }
               

                offsetXY += offsetnext;
                offsetXY2 = offsetXY;
            }

          

        }
    }
}
