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

using System;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
    public class d2_buttonWidget : ButtonWidget
    {
        
        public d2_buttonWidget(ButtonWidget other) : base(other)
        {

        }

        [ObjectCreator.UseCtor]
        public d2_buttonWidget(ModData modData) : base(modData)
        {
            Background = "d2_button";
        }
        public override void DrawBackground(Rectangle rect, bool disabled, bool pressed, bool hover, bool highlighted)
        {
            //shadow shader
            Game.Renderer.Flush();
            Game.Renderer.sproc.AddCommand(3, 0, 0, 0, 0, new int2(0, 0), new float3(rect.X, rect.Y, 0), new float3(rect.Width + 2, rect.Height + 2, 0), null, null);
            Game.Renderer.sproc.ExecCommandBuffer();
            Game.Renderer.sproc.AddCommand(2, 0, 0, 0, 0, new int2(0, 0),new float3(rect.X-1,rect.Y-1,0), new float3(rect.Width+2,rect.Height+2,0), null, null);
            Game.Renderer.sproc.ExecCommandBuffer();
     

            base.DrawBackground(rect, disabled, pressed, hover, highlighted);

        }
    }
}
