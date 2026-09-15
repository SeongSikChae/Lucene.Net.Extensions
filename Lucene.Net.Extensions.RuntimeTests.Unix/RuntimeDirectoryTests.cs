using System.Runtime.Versioning;

namespace Lucene.Net.Store;

[TestClass]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public sealed class RuntimeDirectoryTests
{
	[TestMethod]
	public void RuntimeDirectoryReadsWrittenBytes()
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
			return;

		string path = Path.Combine(Path.GetTempPath(), "LuceneRuntimeTests_" + Guid.NewGuid().ToString("N"));
		System.IO.Directory.CreateDirectory(path);
		try
		{
			using var directory = new HybridDirectory(path);
			byte[] expected = [1, 2, 3, 5, 8, 13];
			using (IndexOutput output = directory.CreateOutput("data.bin", IOContext.DEFAULT))
				output.WriteBytes(expected, 0, expected.Length);

			using IndexInput input = directory.OpenInput("data.bin", IOContext.DEFAULT);
			byte[] actual = new byte[expected.Length];
			input.ReadBytes(actual, 0, actual.Length);
			CollectionAssert.AreEqual(expected, actual);
		}
		finally
		{
			System.IO.Directory.Delete(path, recursive: true);
		}
	}
}
