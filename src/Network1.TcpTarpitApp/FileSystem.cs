namespace Network1.TcpTarpitApp
{
	internal class FileSystem
	{
		public byte[] FileReadAllBytes(string path)
			=> File.ReadAllBytes(path);

		public void FileAppendAllLines(string path, IEnumerable<string> contents)
			=> File.AppendAllLines(path, contents);

		public void FileAppendAllText(string path, string contents)
			=> File.AppendAllText(path, contents);

		public bool FileExists(string? path)
			=> File.Exists(path);

		public string PathCombine(params string[] paths)
			=> Path.Combine(paths);

		public void DirectoryCreateDirectory(string path)
			=> Directory.CreateDirectory(path);

		public bool DirectoryExists(string? path)
			=> Directory.Exists(path);

		public string PathGetFullPath(string path)
			=> Path.GetFullPath(path, AppDomain.CurrentDomain.BaseDirectory);
	}
}
