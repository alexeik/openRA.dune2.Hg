using ImGuiNET;
using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.D2.FileFormats;
using OpenRA.Mods.D2.SpriteLoaders;
using OpenRA.Primitives;
using OpenRA.Widgets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpenRA.Mods.D2.Widgets
{
    
    public class D2IGWidget : ImGuiWidget
    {
        public override void Draw()
        {
            ImGui.NewFrame();
            ImGui.SetNextWindowPos(new Vector2(Convert.ToInt16(this.X), Convert.ToInt16(this.Y)), ImGuiCond.Once);
            ImGui.SetNextWindowSize(new Vector2(Convert.ToInt16(this.Width), Convert.ToInt16(this.Height)), ImGuiCond.Once);
            ImGui.Begin("Inspector");
            ImGui.Text("Hello Dune2");
            UpdateWidgetSize();
            ImGui.End();
            base.Draw();
        }
    }
}
