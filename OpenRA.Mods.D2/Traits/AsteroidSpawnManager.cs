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

using System.Collections.Generic;
using System.Linq;
using OpenRA;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
    [Desc("Controls the spawning of asteroid actor types. Attach this to the world actor.")]
    public class AsteroidSpawnManagerInfo : ConditionalTraitInfo, Requires<MapCreepsInfo>
    {
        [Desc("Minimum number of actors.")]
        public readonly int Minimum = 0;

        [Desc("Maximum number of actors.")]
        public readonly int Maximum = 4;

        [Desc("RemoveActorBeforeSpawn.")]
        public readonly bool RemoveActorBeforeSpawn = false;

        [Desc("NeedsActorSpawner.")]
        public readonly bool NeedsActorSpawner = true;

        [Desc("Time (in ticks) between actor spawn.")]
        public readonly int SpawnInterval = 6000;

        [Desc("LocationInit.")]
        public readonly CPos LocationInit;

        [Desc("Altitude.")]
        public readonly int Altitude = 0;

        [FieldLoader.Require]
        [ActorReference]
        [Desc("Name of the actor that will be randomly picked to spawn.")]
        public readonly string[] Actors = { };

        public readonly string Owner = "Creeps";

        [Desc("Type of ActorSpawner with which it connects.")]
        public readonly HashSet<string> Types = new HashSet<string>() { };

        public override object Create(ActorInitializer init) { return new AsteroidSpawnManager(init.Self, this, init.World); }
    }

    public class AsteroidSpawnManager : ConditionalTrait<AsteroidSpawnManagerInfo>, ITick, INotifyCreated
    {
        readonly AsteroidSpawnManagerInfo info;
        List<Actor> CreatedActors = new List<Actor>();
        bool enabled;
        int spawnCountdown;
        int actorsPresent;
        World world;

        public AsteroidSpawnManager(Actor self, AsteroidSpawnManagerInfo info, World world) : base(info)
        {
            this.info = info;
            this.world = world;
        }

        void INotifyCreated.Created(Actor self)
        {
            enabled = self.Trait<MapCreeps>().Enabled;
        }

        void ITick.Tick(Actor self)
        {


            if (IsTraitDisabled || !enabled)
                return;

            if (info.Maximum < 1 || actorsPresent >= info.Maximum)
            {
                return;
            }


            if (--spawnCountdown > 0 && actorsPresent >= info.Minimum)
                return;
            Actor spawnPoint = null;
            if (info.NeedsActorSpawner)
            {
                spawnPoint = GetRandomSpawnPoint(self.World, self.World.SharedRandom);

                if (spawnPoint == null)
                    return;
            }
            spawnCountdown = info.SpawnInterval;

            do
            {
                // Always spawn at least one actor, plus
                // however many needed to reach the minimum.
                if (Info.RemoveActorBeforeSpawn)
                {
                    RemoveActor(self); // самоудаление у астероида, так как он стреляет через warhead
                }
                if (info.NeedsActorSpawner)
                {
                    SpawnActor(self, spawnPoint);
                }
                else

                {
                    SpawnActor(self, null);
                }
            } while (actorsPresent < info.Minimum);
        }
        public CPos StartLoc;
        public CPos CrushLoc;
        private CPos EndLoc;

        WPos SpawnActor(Actor self, Actor spawnPoint)
        {
            CalculateAsteroidVec();

            self.World.AddFrameEndTask(w =>
            {
                if (info.NeedsActorSpawner) //это если задан SpawnLocation , которые встроены в карту.
                {
                    CreatedActors.Add(w.CreateActor(info.Actors.Random(self.World.SharedRandom), new TypeDictionary
            {
                new OwnerInit(w.Players.First(x => x.PlayerName == info.Owner)),
                new LocationInit(spawnPoint.Location)
            }));
                }
                else
                {
                    //это тех, у кого spawnlocation задано через yaml.
                    CreatedActors.Add(w.CreateActor(info.Actors.Random(self.World.SharedRandom), new TypeDictionary
                        {       new OwnerInit(w.Players.First(x => x.PlayerName == info.Owner)),
                                new LocationOfCrush(CrushLoc),new LocationOfEnd (EndLoc),
                                new CenterPositionInit(self.World.Map.CenterOfCell(StartLoc) + new WVec(0, 0, info.Altitude)) })) ;
                        }
            });

            actorsPresent++;

            if (info.NeedsActorSpawner)
            {
                return spawnPoint.CenterPosition;
            }
            else
            {
                return new WPos();
            }
        }
        public void RemoveActor(Actor self)
        {
            CreatedActors.Clear();
            DecreaseActorCount();
            return;
            if (CreatedActors.Count > 0)
            {
                foreach (Actor a in CreatedActors)
                {
                    if (a.IsInWorld)
                    {
                        self.World.Remove(a);
                        DecreaseActorCount();
                    }
                    else

                    {

                        DecreaseActorCount();
                    }
                }
                //CreatedActors.Remove(a);
            }

        }

        Actor GetRandomSpawnPoint(World world, MersenneTwister random)
        {

            var spawnPointActors = world.ActorsWithTrait<ActorSpawner>()
                .Where(x => !x.Trait.IsTraitDisabled && (info.Types.Overlaps(x.Trait.Types) || !x.Trait.Types.Any()))
                .ToArray();

            return spawnPointActors.Any() ? spawnPointActors.Random(random).Actor : null;

        }

        public void CalculateAsteroidVec()
        {
            Rectangle mapbounds = world.Map.Bounds;
            int mapcenter = mapbounds.Height / 2;
            int leftvert = world.SharedRandom.Next(mapcenter - 10, mapcenter + 10);
            int rightvert = world.SharedRandom.Next(mapcenter - 10, mapcenter + 10);

            int crushdist= world.SharedRandom.Next(1, mapbounds.Width-1);
            StartLoc = new CPos(1, leftvert);
            CrushLoc = new CPos(crushdist, rightvert);
            EndLoc = new CPos(mapbounds.Width - 1, rightvert);
            //число в интервале 10 mapcenter -10 
        }
        public void DecreaseActorCount()
        {

            actorsPresent--;
        }
    }
}
