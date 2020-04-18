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
using System.IO;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;


namespace OpenRA.Mods.D2.Widgets.Logic
{
	public class D2IngameCashCounterLogic : ChromeLogic
	{
		const float DisplayFracPerFrame = .07f;
		const int DisplayDeltaPerFrame = 37;

		readonly World world;
		readonly Player player;
		readonly PlayerResources playerResources;
		readonly string cashLabel;

		int nextCashTickTime = 0;
		int displayResources;
		string displayLabel;
		ISoundSource soundSource;
		
		Stream stclick;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="widget">Widget ссылка на объект виджета, у которого этот объект Logic является в свойствах</param>
		/// <param name="world"></param>
		/// <param name="worldRenderer"></param>
		[ObjectCreator.UseCtor]
		public D2IngameCashCounterLogic(Widget widget, World world, WorldRenderer worldRenderer)
		{
			
			this.world = world;
			player = world.LocalPlayer;
			playerResources = player.PlayerActor.Trait<PlayerResources>();
			playerResources.AssignDelegates(TakeCash, TakeCash, TakeCash , TakeCash);

			displayResources = playerResources.Cash + playerResources.Resources;
			cashLabel = playerResources.Resources.ToString();
			displayLabel = cashLabel.F(displayResources);

			DuneMusic.Init(44100, "", DuneMusic.DuneMusicOplEmu.kOplEmuNuked);

			IReadOnlyFileSystem fileSystem = Game.ModData.DefaultFileSystem;

			using (var stream = fileSystem.Open("DUNE1.ADL"))
			{

				DuneMusic.InsertMemoryFile("test", stream.ReadAllBytes());
				byte[] temp = new byte[1800880];

				UIntPtr temp3;
				temp3 = (UIntPtr)1000000;
				temp3 = DuneMusic.SynthesizeAudio("test", 52, -1, temp, (UIntPtr)temp.Length);
				//stclick = new MemoryStream(temp);
				soundSource = Game.Sound.soundEngine.AddSoundSourceFromMemory(temp, 2, 16, 44100);
				//ISound temp2 = Game.Sound.soundEngine.Play2D(Game.LocalTick, soundSource, false, true, WPos.Zero, 100, false);
				
			}

			//cash.GetText = () => displayLabel;
			//cash.GetTooltipText = () => "Silo Usage: {0}/{1}".F(playerResources.Resources, playerResources.ResourceCapacity);
		}
		ISound ticksnd;
		bool playingtick = false;
		
		public void TakeCash(int Cash,int Resources)
		{
			var actual = Cash + Resources;
			if (ticksnd != null)
			{
				Game.Sound.soundEngine.StopSound(ticksnd); //нужно останавливтаь звук, чтобы не занял все буфера в openal,после чего прекратится проигрывание вообщеие любой музыки.
			}
			if (displayResources < actual)
			{
				ticksnd = Game.Sound.soundEngine.Play2D(Game.LocalTick, soundSource, false, true, WPos.Zero, 100, true);
			}

			if (displayResources > actual)
			{
				ticksnd = Game.Sound.soundEngine.Play2D(Game.LocalTick, soundSource, false, true, WPos.Zero, 100, true);
			}
			displayResources = actual;
		}
		
	}
}
