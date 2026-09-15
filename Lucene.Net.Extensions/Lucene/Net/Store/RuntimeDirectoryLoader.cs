using System.Reflection;

namespace Lucene.Net.Store;

internal static class RuntimeDirectoryLoader
{
	internal const string RuntimeAssemblyName = "Biz.Bizadm.Lucene.Net.Extensions.Runtime";

	internal static object CreateInstance(string runtimeTypeFullName, params object?[] args)
	{
		string assemblyQualifiedName = $"{runtimeTypeFullName}, {RuntimeAssemblyName}";
		Type? runtimeType = Type.GetType(assemblyQualifiedName, throwOnError: false);
		if (runtimeType is null)
			throw new InvalidOperationException(
				$"Runtime directory implementation '{assemblyQualifiedName}' is not available. " +
				"Restore the package for a supported runtime identifier.");

		return Activator.CreateInstance(
			runtimeType,
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
			binder: null,
			args,
			culture: null)
			?? throw new InvalidOperationException($"Failed to create '{assemblyQualifiedName}'.");
	}
}
