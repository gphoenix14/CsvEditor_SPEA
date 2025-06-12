using System;
using System.IO;
using System.Collections.Generic;
namespace ChunkLoader
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length == 0) { Console.WriteLine("Usage: ChunkLoader <file> [chunkSize]"); return; }
			string path = args[0];
			int chunkSize = args.Length > 1 ? int.Parse(args[1]) : 10000;
			using (StreamReader sr = new StreamReader(path))
			{
				string header = sr.ReadLine();
				if (header == null) { Console.WriteLine("Empty file."); return; }
				Console.WriteLine("Header: " + header);
				List<string> chunk = new List<string>(chunkSize);
				string line;
				long total = 0;
				while ((line = sr.ReadLine()) != null)
				{
					chunk.Add(line);
					if (chunk.Count == chunkSize)
					{
						Process(chunk);
						total += chunk.Count;
						chunk.Clear();
					}
				}
				if (chunk.Count > 0) { Process(chunk); total += chunk.Count; }
				Console.WriteLine($"Rows processed: {total}");
			}
		}
		static void Process(List<string> rows)
		{
			Console.WriteLine($"Processing {rows.Count} rows");
		}
	}
}
