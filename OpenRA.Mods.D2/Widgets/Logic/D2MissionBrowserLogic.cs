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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.D2.Widgets.Logic
{
	public class D2MissionBrowserLogic : ChromeLogic
	{
		enum PlayingVideo { None, Info, Briefing, GameStart }

		readonly ModData modData;
		readonly Action onStart;
		readonly ScrollPanelWidget descriptionPanel;
		readonly LabelWidget description;
		readonly SpriteFontMSDF descriptionFont;
		readonly DropDownButtonWidget difficultyButton;
		readonly DropDownButtonWidget gameSpeedButton;
		readonly ButtonWidget startBriefingVideoButton;
		readonly ButtonWidget stopBriefingVideoButton;
		readonly ButtonWidget startInfoVideoButton;
		readonly ButtonWidget stopInfoVideoButton;
		readonly WsaPlayerWidget videoPlayer;
		readonly BackgroundWidget fullscreenVideoPlayer;

		readonly ScrollPanelWidget missionList;
		readonly ScrollItemWidget headerTemplate;
		readonly ScrollItemWidget template;
		public List<MapPreview> allPreviews;
		MapPreview selectedMap;
		PlayingVideo playingVideo;

		string difficulty;
		string gameSpeed;

		[ObjectCreator.UseCtor]
		public D2MissionBrowserLogic(Widget widget, ModData modData, World world, Action onStart, Action onExit)
		{
			this.modData = modData;
			this.onStart = onStart;
			Game.BeforeGameStart += OnGameStart;

			missionList = widget.Get<ScrollPanelWidget>("MISSION_LIST");

			headerTemplate = widget.Get<ScrollItemWidget>("HEADER");
			template = widget.Get<ScrollItemWidget>("TEMPLATE");

			var title = widget.GetOrNull<LabelWidget>("MISSIONBROWSER_TITLE");
			if (title != null)
				title.GetText = () => playingVideo != PlayingVideo.None ? selectedMap.Title : title.Text;

			widget.Get("MISSION_INFO").IsVisible = () => selectedMap != null;

			//var previewWidget = widget.Get<MapPreviewWidget>("MISSION_PREVIEW");
			//previewWidget.Preview = () => selectedMap;
			//previewWidget.IsVisible = () => playingVideo == PlayingVideo.None;

			videoPlayer = widget.Get<WsaPlayerWidget>("MISSION_VIDEO");
			widget.Get("MISSION_BIN").IsVisible = () => playingVideo != PlayingVideo.None;
			fullscreenVideoPlayer = Ui.LoadWidget<BackgroundWidget>("FULLSCREEN_PLAYER", Ui.Root, new WidgetArgs { { "world", world } });
			
			descriptionPanel = widget.Get<ScrollPanelWidget>("MISSION_DESCRIPTION_PANEL");

			description = descriptionPanel.Get<LabelWidget>("MISSION_DESCRIPTION");
			descriptionFont = Game.Renderer.Fonts[description.Font];

			difficultyButton = widget.Get<DropDownButtonWidget>("DIFFICULTY_DROPDOWNBUTTON");
			gameSpeedButton = widget.GetOrNull<DropDownButtonWidget>("GAMESPEED_DROPDOWNBUTTON");

			startBriefingVideoButton = widget.Get<ButtonWidget>("START_BRIEFING_VIDEO_BUTTON");
			stopBriefingVideoButton = widget.Get<ButtonWidget>("STOP_BRIEFING_VIDEO_BUTTON");
			stopBriefingVideoButton.IsVisible = () => playingVideo == PlayingVideo.Briefing;
			stopBriefingVideoButton.OnClick = () => StopVideo(videoPlayer);

			startInfoVideoButton = widget.Get<ButtonWidget>("START_INFO_VIDEO_BUTTON");
			stopInfoVideoButton = widget.Get<ButtonWidget>("STOP_INFO_VIDEO_BUTTON");
			stopInfoVideoButton.IsVisible = () => playingVideo == PlayingVideo.Info;
			stopInfoVideoButton.OnClick = () => StopVideo(videoPlayer);

			CampaignWidget= widget.Get<CampaignWidget>("campaigndune");
			CampaignWidget.OnHouseChooseDelegate = OnHouseChoose;
			CampaignWidget.OnMapRegionChooseDelegate = OnMapRegionChoose;
			CampaignWidget.DrawTextDelegate = OnShowUserHelp;
			CampaignWidget.OnMentatProceedClick = StartMissionClicked;
			CampaignWidget.OnExit = onExit;

			allPreviews = new List<MapPreview>();
			missionList.RemoveChildren();

			// Add a group for each campaign
			if (modData.Manifest.CampaignDB.Any())
			{
				var yaml = MiniYaml.Merge(modData.Manifest.CampaignDB.Select(
					m => MiniYaml.FromStream(modData.DefaultFileSystem.Open(m), m)));

				foreach (var kv in yaml)
				{
					var missionMapPaths = kv.Value.Nodes.Select(n => n.Key).ToList();

					var previews = modData.MapCache
						.Where(p => p.Status == MapStatus.Available)
						.Select(p => new
						{
							Preview = p,
							Index = missionMapPaths.IndexOf(Platform.UnresolvePath(p.Package.Name))
						})
						.Where(x => x.Index != -1)
						.OrderBy(x => x.Index)
						.Select(x => x.Preview);

					if (previews.Any())
					{
						CreateMissionGroup(kv.Key, previews);
						allPreviews.AddRange(previews);
					}
				}
			}

			// Add an additional group for loose missions
			var loosePreviews = modData.MapCache
				.Where(p => p.Status == MapStatus.Available && p.Visibility.HasFlag(MapVisibility.MissionSelector) && !allPreviews.Any(a => a.Uid == p.Uid));

			if (loosePreviews.Any())
			{
				CreateMissionGroup("Missions", loosePreviews);
				allPreviews.AddRange(loosePreviews);
			}

			if (allPreviews.Any())
				SelectMap(allPreviews.First());

			// Preload map preview and rules to reduce jank
			new Thread(() =>
			{
				foreach (var p in allPreviews)
				{
					p.GetMinimap();
					p.PreloadRules();
				}
			}).Start();

			var startButton = widget.Get<ButtonWidget>("STARTGAME_BUTTON");
			startButton.OnClick = StartMissionClicked;
			startButton.IsDisabled = () => selectedMap == null || selectedMap.InvalidCustomRules;

			widget.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				StopVideo(videoPlayer);
				Game.Disconnect();
				Ui.CloseWindow();
				onExit();
			};
			widget.Get<ButtonWidget>("StartCampaign").OnClick = () =>
			{
				CampaignWidget.ResetCampaign();
			};
			widget.Get<ButtonWidget>("CampaignNextLevel").OnClick = () =>
			{
				CampaignWidget.UpLevelDelegate();
			};
			widget.Get<ButtonWidget>("CampaignPrevLevel").OnClick = () =>
			{
				CampaignWidget.DownLevelDelegate();
			};
			//CampaignWidget.BindLevelOnMap(1);
		}

		void OnGameStart()
		{
			Ui.CloseWindow();
			onStart();
		}

		bool disposed;
		protected override void Dispose(bool disposing)
		{
			if (disposing && !disposed)
			{
				disposed = true;
				Game.BeforeGameStart -= OnGameStart;
			}

			base.Dispose(disposing);
		}

		void CreateMissionGroup(string title, IEnumerable<MapPreview> previews)
		{
			var header = ScrollItemWidget.Setup(headerTemplate, () => true, () => { });
			header.Get<LabelWidget>("LABEL").GetText = () => title;
			missionList.AddChild(header);

			foreach (var p in previews)
			{
				var preview = p;

				var item = ScrollItemWidget.Setup(template,
					() => selectedMap != null && selectedMap.Uid == preview.Uid,
					() => SelectMap(preview),
					StartMissionClicked);

				item.Get<LabelWidget>("TITLE").GetText = () => preview.Title;
				missionList.AddChild(item);
			}
		}

		void SelectMap(MapPreview preview)
		{
			selectedMap = preview;

			// Cache the rules on a background thread to avoid jank
			var difficultyDisabled = true;
			var difficulties = new Dictionary<string, string>();

			var briefingVideo = "";
			var briefingVideoVisible = false;

			var infoVideo = "";
			var infoVideoVisible = false;

			new Thread(() =>
			{
				var mapDifficulty = preview.Rules.Actors["world"].TraitInfos<ScriptLobbyDropdownInfo>()
					.FirstOrDefault(sld => sld.ID == "difficulty");

				if (mapDifficulty != null)
				{
					difficulty = mapDifficulty.Default;
					difficulties = mapDifficulty.Values;
					difficultyDisabled = mapDifficulty.Locked;
				}

				var missionData = preview.Rules.Actors["world"].TraitInfoOrDefault<MissionDataInfo>();
				if (missionData != null)
				{
					briefingVideo = missionData.BriefingVideo;
					briefingVideoVisible = briefingVideo != null;

					infoVideo = missionData.BackgroundVideo;
					infoVideoVisible = infoVideo != null;

					var briefing = WidgetUtils.WrapText(missionData.Briefing.Replace("\\n", "\n"), description.Bounds.Width, descriptionFont);
					var height = descriptionFont.Measure(briefing).Y;
					Game.RunAfterTick(() =>
					{
						if (preview == selectedMap)
						{
							description.Text = briefing;
							description.Bounds.Height = height;
							descriptionPanel.Layout.AdjustChildren();
						}
					});
				}
			}).Start();

			startBriefingVideoButton.IsVisible = () => briefingVideoVisible && playingVideo != PlayingVideo.Briefing;
			startBriefingVideoButton.OnClick = () => PlayVideo(videoPlayer, briefingVideo, PlayingVideo.Briefing);

			startInfoVideoButton.IsVisible = () => infoVideoVisible && playingVideo != PlayingVideo.Info;
			startInfoVideoButton.OnClick = () => PlayVideo(videoPlayer, infoVideo, PlayingVideo.Info);

			descriptionPanel.ScrollToTop();

			if (difficultyButton != null)
			{
				var difficultyName = new CachedTransform<string, string>(id => id == null || !difficulties.ContainsKey(id) ? "Normal" : difficulties[id]);
				difficultyButton.IsDisabled = () => difficultyDisabled;
				difficultyButton.GetText = () => difficultyName.Update(difficulty);
				difficultyButton.OnMouseDown = _ =>
				{
					var options = difficulties.Select(kv => new DropDownOption
					{
						Title = kv.Value,
						IsSelected = () => difficulty == kv.Key,
						OnClick = () => difficulty = kv.Key
					});

					Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
					{
						var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
						return item;
					};

					difficultyButton.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Count() * 30, options, setupItem);
				};
			}

			if (gameSpeedButton != null)
			{
				var speeds = modData.Manifest.Get<GameSpeeds>().Speeds;
				gameSpeed = "default";

				gameSpeedButton.GetText = () => speeds[gameSpeed].Name;
				gameSpeedButton.OnMouseDown = _ =>
				{
					var options = speeds.Select(s => new DropDownOption
					{
						Title = s.Value.Name,
						IsSelected = () => gameSpeed == s.Key,
						OnClick = () => gameSpeed = s.Key
					});

					Func<DropDownOption, ScrollItemWidget, ScrollItemWidget> setupItem = (option, template) =>
					{
						var item = ScrollItemWidget.Setup(template, option.IsSelected, option.OnClick);
						item.Get<LabelWidget>("LABEL").GetText = () => option.Title;
						return item;
					};

					gameSpeedButton.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", options.Count() * 30, options, setupItem);
				};
			}
		}

		float cachedSoundVolume;
		float cachedMusicVolume;
		private CampaignWidget CampaignWidget;

		void MuteSounds()
		{
			cachedSoundVolume = Game.Sound.SoundVolume;
			cachedMusicVolume = Game.Sound.MusicVolume;
			Game.Sound.SoundVolume = Game.Sound.MusicVolume = 0;
		}

		void UnMuteSounds()
		{
			if (cachedSoundVolume > 0)
				Game.Sound.SoundVolume = cachedSoundVolume;

			if (cachedMusicVolume > 0)
				Game.Sound.MusicVolume = cachedMusicVolume;
		}

		void PlayVideo(WsaPlayerWidget player, string video, PlayingVideo pv, Action onComplete = null)
		{
			if (!modData.DefaultFileSystem.Exists(video))
			{
				ConfirmationDialogs.ButtonPrompt(
					title: "Video not installed",
					text: "The game videos can be installed from the\n\"Manage Content\" menu in the mod chooser.",
					cancelText: "Back",
					onCancel: () => { });
			}
			else
			{
				StopVideo(player);

				playingVideo = pv;
				player.Load(video);

				// video playback runs asynchronously
				player.PlayThen(() =>
				{
					StopVideo(player);
					if (onComplete != null)
						onComplete();
				});

				// Mute other distracting sounds
				MuteSounds();
			}
		}

		void StopVideo(WsaPlayerWidget player)
		{
			if (playingVideo == PlayingVideo.None)
				return;

			UnMuteSounds();
			player.Stop();
			playingVideo = PlayingVideo.None;
		}

		void StartMissionClicked()
		{
			StopVideo(videoPlayer);

			if (selectedMap.InvalidCustomRules)
				return;

			var orders = new List<Order>();
			if (difficulty != null)
				orders.Add(Order.Command("option difficulty {0}".F(difficulty)));

			orders.Add(Order.Command("option gamespeed {0}".F(gameSpeed)));
			orders.Add(Order.Command("setcampaign {0} {1}".F(CampaignWidget.CurrentCampaignData.CampaignName, CampaignWidget.CurrentCampaignData.CurrentLevel)));
			orders.Add(Order.Command("option explored True".F(Session.ClientState.Ready)));
			orders.Add(Order.Command("state {0}".F(Session.ClientState.Ready)));
			
			var missionData = selectedMap.Rules.Actors["world"].TraitInfoOrDefault<MissionDataInfo>();
			if (1==2 && missionData != null && missionData.StartVideo != null && modData.DefaultFileSystem.Exists(missionData.StartVideo))
			{
				var fsPlayer = fullscreenVideoPlayer.Get<WsaPlayerWidget>("PLAYER");
				fullscreenVideoPlayer.Visible = true;
				PlayVideo(fsPlayer, missionData.StartVideo, PlayingVideo.GameStart, () =>
				{
					Game.CreateAndStartLocalCampaignServer(selectedMap.Uid, orders, CampaignWidget.CurrentCampaignData.CampaignName, CampaignWidget.CurrentCampaignData.CurrentLevel);
				});
			}
			else
				Game.CreateAndStartLocalCampaignServer(selectedMap.Uid, orders, CampaignWidget.CurrentCampaignData.CampaignName, CampaignWidget.CurrentCampaignData.CurrentLevel);
		}

		public void OnHouseChoose(string housename)
		{
			
			
			//String.Format("House:{0} code:{1} {2} {3}", "Harkonen", r.ToString(), g.ToString(), b.ToString());
			description.Text += Environment.NewLine + String.Format("House:{0}", housename);
			var height = descriptionFont.Measure(description.Text).Y;
			Game.RunAfterTick(() =>
			{
				
					
					description.Bounds.Height = height;
					descriptionPanel.Layout.AdjustChildren();
				descriptionPanel.ScrollToBottom();

			});

		}
		public void OnMapRegionChoose(int r, int g, int b)
		{
			//selectedMap = allPreviews.Where(f => f.Title == "scene002").ToList()[0];
			int lev = CampaignWidget.CurrentCampaignData.CurrentLevel;
			string mapname;
			mapname= CampaignWidget.CurrentCampaignData.Levels.Where(f=>f.Num==lev).ToList()[0].PickRegions.Where(d=>d.Key==new float3(r,g,b)).ToList()[0].Value;
			string mapcode = "Uknown";
			selectedMap = allPreviews.Where(f => f.Package.Name.Contains(mapname)).ToList()[0];
			//if (r == 170 && g == 0 & b == 170)
			//{
			//	mapcode = "Map0";
			//}
			//if (r == 170 && g == 85 & b == 0)
			//{
			//	mapcode = "Map1";
			//}
			//if (r == 85 && g == 85 & b == 85)
			//{
			//	mapcode = "Map2";
			//}
			//if (r == 186 && g == 190 & b == 150)
			//{
			//	mapcode = "Map3";
			//}
			mapcode = selectedMap.Title;
			description.Text += Environment.NewLine + String.Format("Map:{0} ", mapcode);
			var height = descriptionFont.Measure(description.Text).Y;
			description.Bounds.Height = height;
			descriptionPanel.Layout.AdjustChildren();
			descriptionPanel.ScrollToBottom();
			//Game.RunAfterTick(() =>
			//{


			//	description.Bounds.Height = height;
			//	descriptionPanel.Layout.AdjustChildren();

			//});
		}
		public void OnShowUserHelp(string text)
		{
			description.Text += Environment.NewLine + text;
			var height = descriptionFont.Measure(description.Text).Y;
			Game.RunAfterTick(() =>
			{


				description.Bounds.Height = height;
				descriptionPanel.Layout.AdjustChildren();
				descriptionPanel.ScrollToBottom();

			});
		}
		class DropDownOption
		{
			public string Title;
			public Func<bool> IsSelected;
			public Action OnClick;
		}
	}
}
