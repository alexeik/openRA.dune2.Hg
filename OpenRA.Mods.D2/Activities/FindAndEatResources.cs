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
using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.D2.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2.Activities
{
	public class FindAndEatResources : Activity
	{
		readonly Sandworm harv;
		readonly SandwormInfo harvInfo;
		readonly Mobile mobile;
		readonly LocomotorInfo locomotorInfo;
		readonly ResourceClaimLayer claimLayer;
		readonly IPathFinder pathFinder;
		readonly DomainIndex domainIndex;
		readonly Actor deliverActor;

		CPos? orderLocation;
		CPos? lastHarvestedCell;
		bool hasDeliveredLoad;
		bool hasHarvestedCell;
		bool hasWaited;

		public FindAndEatResources(Actor self, Actor deliverActor = null)
		{
			harv = self.Trait<Sandworm>();
			harvInfo = self.Info.TraitInfo<SandwormInfo>();
			mobile = self.Trait<Mobile>();
			locomotorInfo = mobile.Info.LocomotorInfo;
			claimLayer = self.World.WorldActor.Trait<ResourceClaimLayer>();
			pathFinder = self.World.WorldActor.Trait<IPathFinder>();
			domainIndex = self.World.WorldActor.Trait<DomainIndex>();
			this.deliverActor = deliverActor;
		}

		public FindAndEatResources(Actor self, CPos orderLocation)
			: this(self, null)
		{
			this.orderLocation = orderLocation;
		}

		protected override void OnFirstRun(Actor self)
		{
			
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivityTick(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			//далее все return NextActivity означают, завершение текущей Activity,так как она выполнила свои задачи.
			//а return this означает, что остаемся внутри этой Activity

			//if (IsCanceling)
			//	return NextActivity;

			//if (NextActivity != null)
			//{
			//	// Interrupt automated harvesting after clearing the first cell.
			//	if (!harvInfo.QueueFullLoad && (hasHarvestedCell || harv.LastSearchFailed))
			//		return NextActivity;

			//	// Interrupt automated harvesting after first complete harvest cycle.
			//	if (hasDeliveredLoad || harv.IsFull)
			//		return NextActivity;
			//}

			if (!hasWaited)
			{
				//var moveTo = mobile.NearestMoveableCell(unblockCell, 1, 5);
				//QueueChild(self, mobile.MoveTo(moveTo, 1), true);
				//QueueChild(self, new Wait(200), true);
				hasWaited = true;
				return this;
			}

			hasWaited = false;

			// Scan for resources. If no resources are found near the current field, search near the refinery
			// instead. If that doesn't help, give up for now.
			var closestHarvestableCell = ClosestHarvestablePos(self);


			// If no harvestable position could be found and we are at the refinery, get out of the way
			// of the refinery entrance.
			//if (harv.LastSearchFailed)
			//{
			//	var lastproc = harv.LastLinkedProc ?? harv.LinkedProc;
			//	if (lastproc != null && !lastproc.Disposed)
			//	{
			//		var deliveryLoc = lastproc.Location + lastproc.Trait<IAcceptResources>().DeliveryOffset;
			//		if (self.Location == deliveryLoc && harv.IsEmpty)
			//		{
			//			var unblockCell = deliveryLoc + harv.Info.UnblockCell;
			//			var moveTo = mobile.NearestMoveableCell(unblockCell, 1, 5);
			//			self.SetTargetLine(Target.FromCell(self.World, moveTo), Color.Green, false);
			//			QueueChild(self, mobile.MoveTo(moveTo, 1), true);
			//		}
			//	}

			//	return this;
			//}
			if (closestHarvestableCell==null)
			{
				return this;
			}
			// If we get here, our search for resources was successful. Commence harvesting.
			QueueChild(self, new EatResource(self, closestHarvestableCell.Value), true);
			lastHarvestedCell = closestHarvestableCell.Value;
			hasHarvestedCell = true;
			return this;
		}

		/// <summary>
		/// Finds the closest harvestable pos between the current position of the harvester
		/// and the last order location
		/// </summary>
		CPos? ClosestHarvestablePos(Actor self)
		{
			// Harvesters should respect an explicit harvest order instead of harvesting the current cell.
			if (orderLocation == null)
			{
				if (harv.CanHarvestCell(self, self.Location) && claimLayer.CanClaimCell(self, self.Location))
					return self.Location;
			}
			else
			{
				if (harv.CanHarvestCell(self, orderLocation.Value) && claimLayer.CanClaimCell(self, orderLocation.Value))
					return orderLocation;

				orderLocation = null;
			}

			// Determine where to search from and how far to search:
			var searchFromLoc = lastHarvestedCell ?? GetSearchFromLocation(self);
			var searchRadius = 24;
			var searchRadiusSquared = searchRadius * searchRadius;

			// Find any harvestable resources:
			List<CPos> path;
			using (var search = PathSearch.Search(self.World, locomotorInfo, self, true, loc =>
					domainIndex.IsPassable(self.Location, loc, locomotorInfo) && Math.Abs(self.Location.X-loc.X)>5 && harv.CanHarvestCell(self, loc) && claimLayer.CanClaimCell(self, loc))
				.WithCustomCost(loc =>
				{
					if ((loc - searchFromLoc).LengthSquared > searchRadiusSquared)
						return int.MaxValue;

					return 0;
				})
				.FromPoint(searchFromLoc)
				.FromPoint(self.Location))
				path = pathFinder.FindPath(search);

			if (path.Count > 0)
				return path[0];

			return null;
		}

	

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromCell(self.World, self.Location);
		}

		CPos GetSearchFromLocation(Actor self)
		{
			
			return self.Location;
		}
	}
}
