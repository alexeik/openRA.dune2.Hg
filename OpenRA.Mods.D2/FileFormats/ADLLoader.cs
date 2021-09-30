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
		public override bool TryParseSound(Stream streamFromFile, out ISoundFormatProxy sound, MusicInfo m)
		{
			try
			{
				sound = new ADLFormat(streamFromFile, m.SubSong, m.Filename);
				return true;
			}
			catch (Exception e)
			{
				// Not a (supported) WAV
			}

			sound = null;
			return false;
		}

		public override bool TryParseSound(Stream stream, out ISoundFormatProxy sound)
		{
			sound = null;
			return false;
			
		}

	}
	public sealed class ADLFormat : ISoundFormatProxy
	{
		public int SubSong;
		public override int SampleBits { get { return 16; } }
		public override int Channels { get { return 2; } }
		public override int SampleRate 
		{
			get { return 44100; }
			set { }
		}
		public override float LengthInSeconds 
		{ 
			get { 
				return (float)235; 
			}
		}
		public override Stream GetPCMInputStream() 
		{
			GetWholeChunk();
			return new MemoryStream(buffer); 
		}
		public override void Dispose() {  }

		 byte[] buffer = new byte[1];
		 byte[] buffer1 = new byte[1];
		readonly Stream stream;
		
		//Only setup track music , no generation
		public ADLFormat(Stream stream1,string SubSong1,string filename)
		{
			if (buffer.Length>10)
			{
				return;
			}
			buffer1 = stream1.ReadAllBytes();
			DuneMusic.Init(44100, Path.Combine(Platform.SupportDir,@"Content\d2"), DuneMusic.DuneMusicOplEmu.kOplEmuNuked);
			//DuneMusic.Init(44100, "", DuneMusic.DuneMusicOplEmu.kOplEmuNuked);
			SubSong = Convert.ToInt32(SubSong1);
			DuneMusic.SetMusicEnabled(true);
			DuneMusic.ChangeMusicEx(DuneMusic.DuneMusicType.kMusicIntro, filename , SubSong);
            //DuneMusic.SetKnownMusicIndex(2);
            
            //uint m = DuneMusic.GetKnownMusicCount();
            //DuneMusic.ChangeMusic(DuneMusic.DuneMusicType.kMusicIntro);
            //DuneMusic.SetKnownMusicIndex(2);
        }

		public override void GetNextChunk(byte[] byteChunkSize)
		{
            try
            {
				DuneMusic.GetSamples(byteChunkSize);
			}
            catch (Exception ex)
            {

                throw;
            }	
		}

		public override byte[] GetWholeChunk()
        {
			
            try
            {
				buffer = new byte[5000000];
				DuneMusic.InsertMemoryFile("test" + SubSong, buffer1);
				DuneMusic.SynthesizeAudio("test" + SubSong, Convert.ToInt32(SubSong), -1, buffer, (UIntPtr)buffer.Length);
			}
            catch (Exception ex)
            {

                throw;
            }
			
			return buffer;
		}
	}
}
