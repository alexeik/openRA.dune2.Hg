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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.D2.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2.Activities
{
	public class EatResource : Activity
	{
		readonly Sandworm harv;
		readonly SandwormInfo harvInfo;
		readonly IFacing facing;
		readonly ResourceClaimLayer claimLayer;
		readonly ResourceLayer resLayer;
		readonly BodyOrientation body;
		readonly IMove move;
		readonly CPos targetCell;
		


		public EatResource(Actor self, CPos targetcell)
		{
			harv = self.Trait<Sandworm>();
			harvInfo = self.Info.TraitInfo<SandwormInfo>();
			facing = self.Trait<IFacing>();
			body = self.Trait<BodyOrientation>();
			move = self.Trait<IMove>();
			claimLayer = self.World.WorldActor.Trait<ResourceClaimLayer>();
			resLayer = self.World.WorldActor.Trait<ResourceLayer>();
			this.targetCell = targetcell;
		}

		protected override void OnFirstRun(Actor self)
		{
			// We can safely assume the claim is successful, since this is only called in the
			// same actor-tick as the targetCell is selected. Therefore no other harvester
			// would have been able to claim.
			claimLayer.TryClaimCell(self, targetCell);
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivityTick(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			//if (IsCanceling || harv.IsFull)
			//	return NextActivity;

			// Move towards the target cell
			if (self.Location != targetCell)
			{
				//foreach (var n in self.TraitsImplementing<INotifyHarvesterAction>())
				//	n.MovingToResources(self, targetCell, new FindAndDeliverResources(self));

				self.SetTargetLine(Target.FromCell(self.World, targetCell), Color.Red, false);
				QueueChild(self, move.MoveTo(targetCell, 2), true);
				return this;
			}

			if (!harv.CanHarvestCell(self, self.Location))
				return NextActivity;

			// Turn to one of the harvestable facings
			//if (harvInfo.HarvestFacings != 0)
			//{
			//	var current = facing.Facing;
			//	var desired = body.QuantizeFacing(current, harvInfo.HarvestFacings);
			//	if (desired != current)
			//	{
			//		QueueChild(self, new Turn(self, desired), true);
			//		return this;
			//	}
			//}

			var resource = resLayer.Harvest(self.Location);
			if (resource == null)
				return NextActivity;

			harv.AcceptResource(self, resource);


			// это событие ловится WithHarverAnimation классом, чтобы 1 раз проиграть анимацию сборки спайса.
			//foreach (var t in self.TraitsImplementing<INotifyHarvesterAction>())
			//	t.Harvested(self, resource);

			foreach (var t in self.TraitsImplementing<INotifyAttack>())
			{
				if (t is OpenRA.Mods.Common.Traits.Render.WithAttackOverlay) //возьмем анимацию для поедания из анимации атаки.
				{
					t.PreparingAttack(self, new Target(), null, null);
				}
				
			}
			//этой Activity делается пауза после одного съедания спайса, чтобы не сосать как пылесосом и подождать анимацию глотания спайса

			//QueueChild(self, new Wait(500), true);
			return this;
		}

		protected override void OnLastRun(Actor self)
		{
			claimLayer.RemoveClaim(self);
		}
	}
}
