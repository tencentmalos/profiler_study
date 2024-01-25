using System.Collections.Generic;
using System.IO;

namespace SCLCoreCLR;

public class SCLDirectory
{
	private static void GetFilesRecursive(string path, Wildcard wildcard, List<string> files)
	{
		string[] files2 = Directory.GetFiles(path);
		foreach (string text in files2)
		{
			if (wildcard.IsMatch(text))
			{
				files.Add(Path.Combine(path, text));
			}
		}
		files2 = Directory.GetDirectories(path);
		for (int i = 0; i < files2.Length; i++)
		{
			GetFilesRecursive(files2[i], wildcard, files);
		}
	}

	public static string[] GetFilesRecursive(string path)
	{
		return GetFilesRecursive(path, "*");
	}

	public static string[] GetFilesRecursive(string path, string wildcard)
	{
		List<string> list = new List<string>();
		GetFilesRecursive(path, new Wildcard(wildcard), list);
		return list.ToArray();
	}
}
