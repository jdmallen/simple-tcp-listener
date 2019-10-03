using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SimpleTCPListener
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			TcpListener server = null;
			var ipAddress = IPAddress.Parse("127.0.0.1");
			const int port = 4000;
			var nl = Environment.NewLine;
			try
			{
				server = new TcpListener(ipAddress, port);
				server.Start();

				var buffer = new byte[256];

				while (true)
				{
					Console.Write("Waiting for connection... ");

					var client = server.AcceptTcpClient();
					Console.WriteLine($"Connected!{nl}");

					string data = null;

					var stream = client.GetStream();

					int i;
					while ((i = stream.Read(buffer, 0, buffer.Length)) != 0)
					{
						data += Encoding.UTF8.GetString(buffer, 0, i);
						Console.Write($"\rReceived: {data.Length} characters");
					}
					Console.WriteLine($"{nl}Disconnected!{nl}");
					Console.WriteLine($"Received:{nl}{data}{nl}");

					client.Close();
					data += Environment.NewLine;
					await File.AppendAllTextAsync(
						Path.Combine(
							Environment.CurrentDirectory,
							"LogOutput.txt"),
						data,
						Encoding.UTF8);
				}
			}
			catch (SocketException e)
			{
				Console.WriteLine($"Socket exception: {e}");
			}
			finally
			{
				server?.Stop();
			}

			Console.WriteLine("Press any key to exit...");
			Console.ReadKey(false);
		}
	}
}
