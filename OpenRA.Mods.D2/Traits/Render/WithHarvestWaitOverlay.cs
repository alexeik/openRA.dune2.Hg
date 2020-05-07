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

using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2.Traits.Render
{
    [Desc("Displays an overlay whenever resources are harvested by the actor.")]
    class WithHarvestWaitOverlayInfo : ITraitInfo, Requires<RenderSpritesInfo>, Requires<BodyOrientationInfo>
    {
        [SequenceReference]
        [Desc("Sequence name to use")]
        public readonly string Sequence = "harvest";

        [SequenceReference]
        [Desc("Sequence name to use")]
        public readonly string SequenceWaitDelivery = "harvestwaitdelivery";

        [Desc("Position relative to body")]
        public readonly WVec LocalOffset = WVec.Zero;

        [PaletteReference]
        public readonly string Palette = null;

        public object Create(ActorInitializer init) { return new WithHarvestWaitOverlay(init.Self, this); }
    }

    class WithHarvestWaitOverlay : INotifyHarvesterAction
    {
        readonly WithHarvestWaitOverlayInfo info;
        readonly AnimationWithOffset anim;
        readonly Animation animwait;
        bool visible;
        bool visiblewait;

        public WithHarvestWaitOverlay(Actor self, WithHarvestWaitOverlayInfo info)
        {
            this.info = info;
            var rs = self.Trait<RenderSprites>();
            var body = self.Trait<BodyOrientation>();


            animwait = new Animation(self.World, rs.GetImage(self), RenderSprites.MakeFacingFunc(self));
            animwait.IsDecoration = true;

            animwait.Play(info.SequenceWaitDelivery);

            anim = new AnimationWithOffset(animwait,
            () => body.LocalToWorld(info.LocalOffset.Rotate(body.QuantizeOrientation(self, self.Orientation))),
            () => !visiblewait);

            rs.Add(anim, info.Palette, false);

        }

        void INotifyHarvesterAction.Harvested(Actor self, ResourceType resource)
        {

        }
        void PlayWaitOverlay()
        {
            if (visiblewait)
                anim.Animation.PlayThen(info.SequenceWaitDelivery, PlayWaitOverlay);
        }

        void INotifyHarvesterAction.MovingToResources(Actor self, CPos targetCell, Activity next) { }
        void INotifyHarvesterAction.MovingToRefinery(Actor self, Actor targetRefinery, Activity next)
        {
            visiblewait = true;
            PlayWaitOverlay();
        }
        void INotifyHarvesterAction.Docked()
        {
            visiblewait = false;
        }

        void INotifyHarvesterAction.MovementCancelled(Actor self) { }
        void INotifyHarvesterAction.Undocked() { }

    }
}
