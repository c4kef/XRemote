using System;
using Network.Attributes;
using Network.Packets;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;

namespace XRemote.ShareStructure
{
	[PacketRequest(typeof(SharedClass))]
	public class SharedResponse : ResponsePacket
	{
		public SharedResponse(SharedClass Result, RequestPacket request)
			: base(request)
		{
			this.Result = Result;
		}

		public SharedClass Result { get; set; }
	}

	[Serializable]
	public class FilesInfo
	{
		public FilesInfo() { data = new List<FileStruct>(); }
		private FilesInfo(List<FileStruct> binFiles) { data = binFiles; }

		[Serializable]
		public struct FileStruct
		{
			public byte[] Icon;
			public byte[] File;
			public string Path;
		}

		public List<FileStruct> data { get; private set; }

		public void Add([Optional] byte[] Icon, [Optional] byte[] File, [Optional] string Path) => data.Add(new FileStruct() { Icon = (Icon == null) ? new byte[10] : Icon, File = (File == null) ? new byte[10] : File, Path = (String.IsNullOrEmpty(Path)) ? "null" : Path });

		public byte[] ToArray()
		{
			var binFormatter = new BinaryFormatter();
			var mStream = new MemoryStream();
			binFormatter.Serialize(mStream, data);
			return mStream.ToArray();
		}

		public static FilesInfo FromBin(byte[] bin)
		{
			MemoryStream mStream = new MemoryStream();
			BinaryFormatter binFormatter = new BinaryFormatter();
			mStream.Write(bin, 0, bin.Length);
			mStream.Position = 0;
			return new FilesInfo(binFormatter.Deserialize(mStream) as List<FileStruct>);
		}
	}

	public class SharedClass : RequestPacket
	{
		public SharedClass()
		{
			Command = Value = "null";
			Files = new byte[10];
		}

		public string Command { get; set; }
		public string Value { get; set; }
		public byte[] Files { get; set; }
	}
}
