using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace Network1.TcpTarpitApp
{
	public class AppService
	{
		public AppService() : this(new FileSystem()) { }

		internal AppService(FileSystem fileSystem)
		{
			_fileSystem = fileSystem;
		}

		private readonly AppOptions _appOptions = new AppOptions();
		private readonly FileSystem _fileSystem;
		private TcpListener[]? _listeners;
		private CancellationTokenSource? _cancellationTokenSource;
		private static readonly object _lock = new object();
		private byte[] _responseDataBytes = new byte[0];
		const string _dateTimeFormat = "HH:mm:ss";
		const string _dateDailyFormat = "yyyy-MM-dd";

		public string? Error { get; set; }
		public AppOptions AppOptions { get { return _appOptions; } }

		public void Start(string[] args)
		{
			SetOptionsFromArguments(args);
			Error = ValidateOptions(_appOptions);
			if (Error is not null) return;

			if (_appOptions.ConnectionLogDirectory is not null)
			{
				_appOptions.ConnectionLogDirectory = _fileSystem.PathGetFullPath(_appOptions.ConnectionLogDirectory);
				_fileSystem.DirectoryCreateDirectory(_appOptions.ConnectionLogDirectory);
			}

			_responseDataBytes = GetResponseDataBytes();
			IPAddress ipAddress = GetIpAddressFromString(_appOptions.ListenAddress!)!;
			_cancellationTokenSource = new CancellationTokenSource();
			_cancellationTokenSource.Token.Register(() =>
			{
				StopListeners();
			});
			_listeners = _appOptions.Ports.Select(x => GetStartedTcpListener(ipAddress, x)).ToArray();
		}

		public void Stop()
		{
			_cancellationTokenSource?.Cancel();
		}

		public string GetHelpText()
		{
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{GetType().Namespace}.help.txt")!;
			using StreamReader reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}

		private void StopListeners()
		{
			foreach (TcpListener listener in _listeners!)
			{
				listener.Stop();
			}
		}

		private byte[] GetResponseDataBytes()
		{
			if (_appOptions.ResponseDataFile is not null)
			{
				return _fileSystem.FileReadAllBytes(_fileSystem.PathGetFullPath(_appOptions.ResponseDataFile));
			}
			byte[] bytes = new byte[1024 * 1024];
			for (int i = 0; i < bytes.Length; i++)
			{
				bytes[i] = (byte)Random.Shared.Next(97, 122);
			}
			return bytes;
		}

		private TcpListener GetStartedTcpListener(IPAddress ipAddress, ushort port)
		{
			TcpListener listener = new TcpListener(ipAddress, port);
			listener.Start();
			Task.Run(async () =>
			{
				while (!_cancellationTokenSource!.IsCancellationRequested)
				{
					TcpClient client = await listener.AcceptTcpClientAsync();
					OnClientAccept(client).Wait(0, _cancellationTokenSource.Token);
				}
			}, _cancellationTokenSource!.Token);
			return listener;
		}

		private async Task OnClientAccept(TcpClient client)
		{
			using NetworkStream networkStream = client.GetStream();
			DateTime start = DateTime.Now;

			string localEndpoint = ((IPEndPoint)client.Client.LocalEndPoint!).ToString();
			string remoteEndpoint = ((IPEndPoint)client.Client.RemoteEndPoint!).ToString();

			int byteIndex = 0;
			while (client.Connected
				&& networkStream.CanWrite
				&& !_cancellationTokenSource!.IsCancellationRequested
				&& (_appOptions.MaxConnectionTimeInSeconds == 0 || _appOptions.MaxConnectionTimeInSeconds > (DateTime.Now - start).TotalSeconds)
			)
			{
				int byteCount = byteIndex + _appOptions.BytesPerIteration >= _responseDataBytes.Length ?
					_responseDataBytes.Length - byteIndex - 1 : _appOptions.BytesPerIteration;

				try
				{
					networkStream.Write(_responseDataBytes, byteIndex, byteCount);
					networkStream.Flush();
				}
				catch
				{
					break;
				}
				byteIndex += byteCount;
				if (byteIndex >= _responseDataBytes.Length) byteIndex = 0;

				await Task.Delay(_appOptions.IterationDelayInMs);
			}

			if (client.Connected) client.Close();

			string[] logItems = new[]
			{
				DateTime.Now.ToString(_dateTimeFormat),
				remoteEndpoint,
				localEndpoint,
				((int)(DateTime.Now - start).TotalSeconds).ToString()
			};

			ConnectionLogWrite(string.Join('\t', logItems));
		}

		private string? ValidateOptions(AppOptions options)
		{
			if (options.ListenAddress is null) return "Listen address not set";
			if (GetIpAddressFromString(options.ListenAddress) is null) return "Invalid ip address";
			if (options.Ports.Length == 0) return "Ports not set";
			if (options.IterationDelayInMs == 0) return "Invalid iteration delay";
			if (options.BytesPerIteration == 0) return "Invalid bytes per iteration";
			if (options.ResponseDataFile is not null && !_fileSystem.FileExists(_fileSystem.PathGetFullPath(options.ResponseDataFile)))
				return "Response data file does not exists";

			return null;
		}

		private void SetOptionsFromArguments(string[] args)
		{
			for (int i = 0; i < args.Length; i += 2)
			{
				switch (args[i])
				{
					case "-a":
						_appOptions.ListenAddress = args[i + 1];
						break;
					case "-p":
						_appOptions.Ports = GetPortsFromString(args[i + 1]);
						break;
					case "-d":
						_appOptions.IterationDelayInMs = GetIntFromString(args[i + 1]);
						break;
					case "-b":
						_appOptions.BytesPerIteration = GetIntFromString(args[i + 1]);
						break;
					case "-l":
						_appOptions.ConnectionLogDirectory = args[i + 1];
						break;
					case "-r":
						_appOptions.ResponseDataFile = args[i + 1];
						break;
					case "-m":
						_appOptions.MaxConnectionTimeInSeconds = GetIntFromString(args[i + 1]);
						break;
				}
			}
		}

		private ushort[] GetPortsFromString(string portsString)
		{
			return portsString.Split(',').SelectMany(x => GetPortsFromSegmentString(x.Trim())).Where(x => x != 0).ToArray();
		}

		private ushort[] GetPortsFromSegmentString(string segmentString)
		{
			if (segmentString.IndexOf('-') != -1)
			{
				ushort[] parts = segmentString.Split('-').Select(x => GetUshortFromString(x)).ToArray();
				if (parts.Length != 2) return new ushort[0];
				return Enumerable.Range(parts[0], 1 + parts[1] - parts[0]).Select(x => (ushort)x).ToArray();
			}
			return new[] { GetUshortFromString(segmentString) };
		}

		private ushort GetUshortFromString(string ushortString)
		{
			ushort n;
			return ushort.TryParse(ushortString, out n) ? n : default;
		}

		private int GetIntFromString(string intString)
		{
			int n;
			return int.TryParse(intString, out n) ? n : default;
		}

		private IPAddress? GetIpAddressFromString(string address)
		{
			IPAddress? ip;
			return IPAddress.TryParse(address, out ip) ? ip : null;
		}

		private void ConnectionLogWrite(string data)
		{
			if (_appOptions.ConnectionLogDirectory is null) return;
			lock (_lock)
			{
				string fileName = $"{DateTime.Now.ToString(_dateDailyFormat)}.txt";
				fileName = _fileSystem.PathCombine(_appOptions.ConnectionLogDirectory!, fileName);
				_fileSystem.FileAppendAllText(fileName, data + "\r\n");
			}
		}
	}
}
