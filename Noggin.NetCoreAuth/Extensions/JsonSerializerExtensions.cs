using System.Text.Json;

namespace Noggin.NetCoreAuth.Extensions;

internal static class JsonSerializerExtensions
{
	public static T TryDeserialize<T>(string jsonString)
	{
		try
		{
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
			return JsonSerializer.Deserialize<T>(jsonString, options);
		}
		catch (JsonException)
		{
			return default;
		}
	}
}
