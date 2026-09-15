using System.Reflection;

namespace Lucene.Net.Store;

/// <summary>
/// Creates Lucene-compatible already-closed exceptions when the Lucene type is accessible,
/// otherwise falls back to <see cref="ObjectDisposedException"/> (also recognized by many Lucene paths).
/// </summary>
public static class LuceneCloseExceptions
{
	private static readonly Func<string?, string?, Exception> Factory = CreateFactory();
	private static readonly Func<Exception, bool> IsAlreadyClosedCore = CreateIsAlreadyClosed();

	public static Exception AlreadyClosed(string? objectName, string message) =>
		Factory(objectName, message);

	public static bool IsAlreadyClosed(Exception ex) => IsAlreadyClosedCore(ex);

	private static Func<string?, string?, Exception> CreateFactory()
	{
		Type? type = typeof(Lucene.Net.Store.FSDirectory).Assembly.GetType("Lucene.AlreadyClosedException");
		MethodInfo? create2 = type?.GetMethod(
			"Create",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
			binder: null,
			types: [typeof(string), typeof(string)],
			modifiers: null);

		if (create2 is not null)
		{
			return (name, message) =>
				(Exception)create2.Invoke(null, [name, message])!;
		}

		MethodInfo? create1 = type?.GetMethod(
			"Create",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
			binder: null,
			types: [typeof(string)],
			modifiers: null);

		if (create1 is not null)
		{
			return (name, message) =>
				(Exception)create1.Invoke(null, [message ?? name])!;
		}

		return static (name, message) => new ObjectDisposedException(name, message);
	}

	private static Func<Exception, bool> CreateIsAlreadyClosed()
	{
		Type? alreadyClosedType = typeof(Lucene.Net.Store.FSDirectory).Assembly.GetType("Lucene.AlreadyClosedException");
		MethodInfo? method = typeof(Lucene.Net.Store.FSDirectory).Assembly
			.GetType("Lucene.ExceptionExtensions")
			?.GetMethod(
				"IsAlreadyClosedException",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
				binder: null,
				types: [typeof(Exception)],
				modifiers: null);

		if (method is not null)
		{
			return ex =>
				ex is ObjectDisposedException
				|| (alreadyClosedType is not null && alreadyClosedType.IsInstanceOfType(ex))
				|| (bool)method.Invoke(null, [ex])!;
		}

		if (alreadyClosedType is not null)
		{
			return ex =>
				ex is ObjectDisposedException
				|| alreadyClosedType.IsInstanceOfType(ex)
				|| ex.GetType().Name.Contains("AlreadyClosed", StringComparison.Ordinal);
		}

		return static ex =>
			ex is ObjectDisposedException
			|| ex.GetType().Name.Contains("AlreadyClosed", StringComparison.Ordinal);
	}
}
