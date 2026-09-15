using System.Reflection;

namespace Lucene.Net.Store;

internal static class RuntimeDirectoryLoader
{
	// Runtime.* 프로젝트들은 패키징/배포 시 동일 AssemblyName으로 묶여 로드됩니다.
	internal const string RuntimeAssemblyName = "Biz.Bizadm.Lucene.Net.Extensions.Runtime";

	internal static object CreateInstance(string runtimeTypeFullName, params object?[] args)
	{
		string assemblyQualified = $"{runtimeTypeFullName}, {RuntimeAssemblyName}";

		Type? runtimeType = Type.GetType(assemblyQualified, throwOnError: false);
		if (runtimeType is null)
			throw new InvalidOperationException(
				$"Runtime directory implementation '{assemblyQualified}' is not available. " +
				$"Ensure OS-specific runtime package is restored for '{RuntimeAssemblyName}'.");

		return Activator.CreateInstance(runtimeType, args)
			?? throw new InvalidOperationException($"Failed to create '{assemblyQualified}'.");
	}
}
