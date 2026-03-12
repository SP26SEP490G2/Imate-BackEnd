using System.Collections;
using System.Text.Json;

namespace Imate.API.Business.Helper
{
	public static class AuditHelper
	{
		public static (Dictionary<string, object> oldData, Dictionary<string, object> newData)
				GetChanges(object oldObj, object newObj)
		{
			var oldChanges = new Dictionary<string, object>();
			var newChanges = new Dictionary<string, object>();

			var properties = oldObj.GetType().GetProperties();

			foreach (var prop in properties)
			{
				var oldVal = prop.GetValue(oldObj);
				var newVal = newObj.GetType().GetProperty(prop.Name)?.GetValue(newObj);

				if (IsList(oldVal) && IsList(newVal))
				{
					if (!ListEquals((IEnumerable)oldVal!, (IEnumerable)newVal!))
					{
						oldChanges[prop.Name] = oldVal!;
						newChanges[prop.Name] = newVal!;
					}
				}
				else
				{
					if (!Equals(oldVal, newVal))
					{
						oldChanges[prop.Name] = oldVal!;
						newChanges[prop.Name] = newVal!;
					}
				}
			}

			return (oldChanges, newChanges);
		}

		private static bool IsList(object? obj)
		{
			return obj is IEnumerable && obj is not string;
		}

		private static bool ListEquals(IEnumerable oldList, IEnumerable newList)
		{
			var oldJson = JsonSerializer.Serialize(oldList);
			var newJson = JsonSerializer.Serialize(newList);

			return oldJson == newJson;
		}
	}
}
