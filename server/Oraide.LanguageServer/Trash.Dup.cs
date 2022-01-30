using System;
using System.IO;
using System.Text;

namespace Oraide.LanguageServer
{
	class Dup : Stream
	{
		string _name;

		private Dup() { }

		public Dup(string name) { _name = name; }

		public override bool CanRead { get { return false; } }

		public override bool CanSeek { get { return false; } }

		public override bool CanWrite { get { return true; } }

		public override long Length => throw new NotImplementedException();

		public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			var now = DateTime.Now;
			var sb = new StringBuilder();
			sb.AppendLine("\r\n------BEFORE------\r\nRaw message from " + _name + " " + now.ToString("MM/dd/yyyy hh:mm:ss.fff tt"));

			var truncatedArray = new byte[count];
			for (var i = offset; i < offset + count; ++i)
				truncatedArray[i - offset] = buffer[i];

			var str = Encoding.Default.GetString(truncatedArray);
			sb.AppendLine("data (length " + str.Length + ")= '" + str + "'");
			System.Console.Error.WriteLine(sb.ToString());
			System.Console.Error.WriteLine("------AFTER------\r\n");
		}
	}
}
