using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Widgets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.D2.Widgets
{
    public class TextSequenceWidget : Widget
    {
        public readonly string SeqGroup;
        public readonly string SeqSubGroup;
        public readonly string AnimationDirection;
        public readonly string PaletteNameFromYaml;

        Animation animation1;

        World World;
        SequenceProvider sp;
        PaletteReference pr;


        public override void Initialize(WidgetArgs args)
        {
            base.Initialize(args);
            World = (World)args["world"];
            animation1 = new Animation(World, SeqGroup);
            if (AnimationDirection == "Forward")
            {
                animation1.Play(SeqSubGroup);
            }
            if (AnimationDirection == "Repeat")
            {
                animation1.PlayRepeating(SeqSubGroup);
            }
            pr = Game.worldRenderer.Palette(PaletteNameFromYaml);

        }
        public override void Draw()
        {
            animation1.Tick();
            Game.Renderer.SpriteRenderer.DrawSprite(animation1.Image,new float3(RenderBounds.X,RenderBounds.Y,0), pr,new float3(RenderBounds.Width,RenderBounds.Height,0));
        }

    }
}
