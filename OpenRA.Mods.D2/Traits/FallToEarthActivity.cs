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

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.D2.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.D2.MathExtention;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.D2.Activities
{
	public class FallToEarthAsteroid : Activity
	{
		readonly Asteroid asteroid;
		readonly FallsToEarthAsteroidInfo info;
		int acceleration = 0;
		int spin = 0;

		public FallToEarthAsteroid(Actor self, FallsToEarthAsteroidInfo info)
		{
			this.info = info;
			IsInterruptible = false;
			asteroid = self.Trait<Asteroid>();
			if (info.Spins)
				acceleration = self.World.SharedRandom.Next(2) * 2 - 1;
		}

		//параллельно работает с другими activity 
		public override Activity Tick(Actor self)
		{
			if (self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length <= 0)
			{
				if (info.ExplosionWeapon != null)
				{
					// Use .FromPos since this actor is killed. Cannot use Target.FromActor
					info.ExplosionWeapon.Impact(Target.FromPos(self.CenterPosition), self, Enumerable.Empty<int>());
				}

				//self.Kill(self);
				return null;
			}

			if (info.Spins)
			{
				spin += acceleration;
				asteroid.Facing = (asteroid.Facing + spin) % 256;
			}

			var move = info.Moves ? asteroid.FlyStep(asteroid.Facing) : WVec.Zero;
			move -= new WVec(WDist.Zero, WDist.Zero, info.Velocity);
			asteroid.SetPosition(self, asteroid.CenterPosition + move);

			if (self.Scale <= 0.2f)
			{
				self.Scale = 0.2f;
			}
			else
			{
				self.Scale -= info.ScaleStep;
			}
			return this;
		}
	}
}
