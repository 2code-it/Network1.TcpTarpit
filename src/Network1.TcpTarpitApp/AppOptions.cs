namespace Network1.TcpTarpitApp
{
	public class AppOptions
	{
		public string? ListenAddress { get; set; } //-l
		public ushort[] Ports { get; set; } = new ushort[0]; //-p
		public int IterationDelayInMs { get; set; } = 200; //-i
		public int BytesPerIteration { get; set; } = 1; //-b
		public string? ConnectionLogDirectory { get; set; } //-c
		public string? ResponseDataFile { get; set; } //-d
		public int MaxConnectionTimeInSeconds { get; set; } //-m
	}
}
