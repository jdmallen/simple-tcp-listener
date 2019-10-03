using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Console = System.Console;

namespace SimpleTCPListener
{
	public class Program
	{
		// ReSharper disable once InconsistentNaming
		public static readonly string nl = Environment.NewLine;
		public const string DefaultAddress = "127.0.0.1";
		public const int DefaultPort = 4000;
		public const string DefaultOutputPath = "./tcp_output.log";

		// public static CommandLineApplication Application;

		public static async Task<int> Main(string[] args)
		{
			try
			{
				var application = new CommandLineApplication
				{
					Name = "Simple TCP Listener"
				};
				application.HelpOption("-?|-h|--help");
				var optionIp = application.Option<string>(
					"-a|--address <ADDRESS>",
					$"The IP address or hostname from which to listen (default: {DefaultAddress})",
					CommandOptionType.SingleValue);
				var optionPort = application.Option<int>(
					"-p|--port <PORT>",
					$"The port on which to listen (default: {DefaultPort})",
					CommandOptionType.SingleValue);
				var optionLogOutputPath = application.Option<string>(
					"-o|--output <OUTPUT>",
					$"The path of the file where output is written (default: {DefaultOutputPath})",
					CommandOptionType.SingleValue);

				application.OnExecuteAsync(
					async token => await Run(
						optionIp,
						optionPort,
						optionLogOutputPath,
						token));

				return await application.ExecuteAsync(args);
			}
			catch (CommandParsingException ex)
			{
				Console.WriteLine(
					$"Argument not recognized.{nl}{ex.Message}{nl}Please try again.");
				return 1;
			}
			catch (InvalidOperationException ex)
			{
				Console.WriteLine(
					$"Command not recognized.{nl}{ex.Message}{nl}Please try again.");
				return 1;
			}
		}

		public static async Task<int> Run(
			CommandOption<string> ipOption,
			CommandOption<int> portOption,
			CommandOption<string> outputPathOption,
			CancellationToken token)
		{
			var (ipAddress, port, outputPath) = ParseOptions(
				ipOption,
				portOption,
				outputPathOption);

			Console.WriteLine($"Listening to {ipAddress} on port {port}...");
			TcpListener server = null;
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
						outputPath,
						data,
						Encoding.UTF8,
						token);
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
			return 0;
		}

		public static (IPAddress, int, string) ParseOptions(
			CommandOption<string> ipOption,
			CommandOption<int> portOption,
			CommandOption<string> outputPathOption)
		{
			IPAddress ipAddress;
			if (ipOption.HasValue())
			{
				try
				{
					ipAddress = IPAddress.Parse(ipOption.ParsedValue);
				}
				catch
				{
					try
					{
						var entries = Dns.GetHostEntry(ipOption.ParsedValue);
						if (entries.AddressList.Length == 0)
						{
							Console.WriteLine(
								$"Invalid IP Address/hostname: {ipOption.ParsedValue}. Using default: {DefaultAddress}");
							ipAddress = IPAddress.Parse("127.0.0.1");
						}
						else
						{
							ipAddress = entries.AddressList.First();
						}
					}
					catch
					{
						Console.WriteLine(
							$"Invalid IP Address/hostname: {ipOption.ParsedValue}. Using default: {DefaultAddress}");
						ipAddress = IPAddress.Parse("127.0.0.1");
					}
				}
			}
			else
			{
				ipAddress = IPAddress.Parse("127.0.0.1");
			}

			var port = portOption.HasValue()
				? portOption.ParsedValue
				: 4000;

			string outputPath;
			if (outputPathOption.HasValue())
			{
				outputPath = outputPathOption.ParsedValue;
				try
				{
					outputPath = Path.GetFullPath(outputPath);
				}
				catch
				{
					Console.WriteLine(
						$"Invalid path \"{outputPath}\". Writing to default: {DefaultOutputPath}");
					outputPath = DefaultOutputPath;
				}
			}
			else
			{
				outputPath = DefaultOutputPath;
			}

			return (ipAddress, port, outputPath);
		}
	}
}
