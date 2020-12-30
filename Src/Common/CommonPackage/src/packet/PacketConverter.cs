using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Google.Protobuf;
using System.Reflection;

using pb = Google.Protobuf;
using System.Linq.Expressions;
using Newtonsoft.Json;

namespace CommonPackage.Packet
{
	public class PacketDescription
	{
		public string Name { get; set; }

		public string Context { get; set; }
	}

	public class PacketDescription<T>
	{
		public PacketDescription(string context)
		{
			Name = typeof(T).FullName;
			Context = context;
		}

		public string Name { get; set; }

		public string Context { get; set; }
		public override string ToString()
		{
			return $"{{\"{nameof(Name)}\": \"{Name}\", \"{nameof(Context)}\": \"{Context}\"}}";
		}
	}


	public class PacketConverter
	{
		public static Type GetPacketType(string typeName)
		{
			var asmName = typeof(PacketConverter).Assembly.GetName().Name;
			return Type.GetType($"{typeName}, {asmName}");
		}

		public static string SerializePacket<T>(T packet) where T: pb.IMessage<T>
		{
			try 
			{
				return Convert.ToBase64String(packet.ToByteArray());
			}
			catch
			{
				return null;
			}
		}

		public static string SerializePacketDescriptionJson<T>(T packet) where T : pb.IMessage<T>
		{
			try
			{
				return JsonConvert.SerializeObject(SerializePacketDescription(packet));
			}
			catch
			{
				return null;
			}
		}

		public static PacketDescription<T> SerializePacketDescription<T>(T packet) where T : pb.IMessage<T>
		{
			try
			{
				var serialize = SerializePacket(packet);
				return new PacketDescription<T>(serialize);
			}
			catch
			{
				return null;
			}
		}


		public static T DeserializePacket<T>(string base64) where T : pb.IMessage<T>, new()
		{
			try
			{
				var bytes = Convert.FromBase64String(base64);

				MessageParser<T> parser = new MessageParser<T>(() => { return new T(); });

				return parser.ParseFrom(bytes);
			}
			catch
			{
				return default(T);
			}
		}

		// Packet 생성
		// 아래 각 시스템에 처리 (is TYPE 검사)로 해당 타입 처리
		public static object DeserializePacket(PacketDescription packetDesc)
		{
			if(string.IsNullOrEmpty(packetDesc.Name) == true ||
				string.IsNullOrEmpty(packetDesc.Context) == true)
			{
				return null;
			}

			var packetType = PacketConverter.GetPacketType(packetDesc.Name);
			if (packetType == null)
				return null;

			var bytes = Convert.FromBase64String(packetDesc.Context);

			try
			{
				// MessageParser<T> parser = new MessageParser<T>(() => { return new T(); });
				var parserType = typeof(MessageParser<>).MakeGenericType(packetType);
				var funcLamda = Expression.Lambda(Expression.New(packetType)).Compile();
				var parser = Activator.CreateInstance(parserType, funcLamda);

				// return parser.ParseFrom(bytes);
				var parseFormMethod = parserType.GetMethod("ParseFrom", new[] { typeof(byte[]) });
				return parseFormMethod.Invoke(parser, new[] { bytes });
			}
			catch
			{
				return null;
			}
		}

		public static object DeserializePacketFromJson(string jsonContent)
		{
			var packetDesc = DeserializePacketDescFromJson(jsonContent);
			return DeserializePacket(packetDesc);
		}

		public static PacketDescription DeserializePacketDescFromJson(string jsonContent)
		{
			jsonContent = jsonContent.Trim('"');
			jsonContent = jsonContent.Replace("\\", "");

			return JsonConvert.DeserializeObject<PacketDescription>(jsonContent);
		}
	}
}
