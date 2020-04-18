using OpenRA;
using OpenRA.GameRules;
using OpenRA.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.D2.FileFormats
{
	public class ADLLoader : SoundLoader
	{
		public override bool TryParseSound(Stream stream, out ISoundFormat sound, MusicInfo m)
		{
			try
			{
				sound = new ADLFormat(stream, m.SubSong);
				return true;
			}
			catch
			{
				// Not a (supported) WAV
			}

			sound = null;
			return false;
		}

		public override bool TryParseSound(Stream stream, out ISoundFormat sound)
		{
		
			sound = null;
			return false;
		}
	}
	public sealed class ADLFormat : ISoundFormat
	{
		public int SubSong;
		public int SampleBits { get { return 16; } }
		public int Channels { get { return 2; } }
		public int SampleRate 
		{
			get { return 44100; }
			private set { }
		}
		public float LengthInSeconds 
		{ 
			get { 
				return (float)35; 
			}
		}
		public Stream GetPCMInputStream() 
		{ 
			return new MemoryStream(buffer); 
		}
		public void Dispose() {  }

		readonly byte[] buffer = new byte[1];
		readonly Stream stream;

		public ADLFormat(Stream stream,string SubSong)
		{
			if (buffer.Length>10)
			{
				return;
			}
			DuneMusic.Init(44100, "", DuneMusic.DuneMusicOplEmu.kOplEmuNuked);

		
				DuneMusic.InsertMemoryFile("test", stream.ReadAllBytes());
				buffer = new byte[10106880];

				UIntPtr temp3;
				temp3 = (UIntPtr)1000000;
				temp3 = DuneMusic.SynthesizeAudio("test", Convert.ToInt32(SubSong), -1, buffer, (UIntPtr)buffer.Length);
				//ISoundSource soundSource;
				//soundSource = Game.Sound.soundEngine.AddSoundSourceFromMemory(buffer, 2, 16, 44100);
				//ISound temp2 = Game.Sound.soundEngine.Play2D(Game.LocalTick, soundSource, false, true, WPos.Zero, 100, false);

			
		}
	}
}
