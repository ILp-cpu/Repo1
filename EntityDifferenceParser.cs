using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.ComponentModel.DataAnnotations;
using Ngn.Attributes;
using Ngn.Infrastructure;
using Ngn.Models.Media;
using Ngn.Models.References;

namespace Ngn.LogAction.DifferenceParsers
{
	public class EntityDifferenceParser 
	{
		private Dictionary<string, Dictionary<string, PropertyInfo>> _propertiesCache = new Dictionary<string, Dictionary<string, PropertyInfo>>();

		private Dictionary<string, Type> _typesCache = new Dictionary<string, Type>();

		public IEnumerable<DifferenceInfo> GetDifferences(XElement currentEntry, XElement previousEntry)
		{
			

			if (currentEntry == null)
			{
				yield break;
			}

			if (previousEntry == null)
			{
				yield break;
			}
			var modelType = GetModelType(currentEntry);
			if (modelType == null)
			{
				yield break;
			}
			foreach (var currentElement in currentEntry.Elements())
			{
				var previousElement = previousEntry.Element(currentElement.Name);
				if (!XNode.DeepEquals(currentElement, previousElement))
				{
					var item = GetDiffrenceInfo(modelType, currentElement, previousElement);
					if (item != null)
					{
						yield return item;
					}
				}
			}
			

		}

		#region Private Helpers

		private Type GetModelType(XElement currentEntry)
		{

			var rootName = currentEntry.Name.ToString();

			if (!_typesCache.ContainsKey(rootName))
			{
				IEnumerable<Type> types = ReflectionHelper.GetAllMatchedTypes(s => s.Name == rootName);
				var type = types.FirstOrDefault();
				if (type != null)
				{
					_typesCache.Add(rootName, type);
				}
				else
				{
					return null;
				}
			}
			return _typesCache[rootName];

			switch (rootName)
			{
			//	case "FirmModel":
			//		return typeof (FirmModel);
			//		break;
			//	case "BankForFirmModel":
			//		return typeof(BankForFirmModel);
			//		break;
				case "MediaItemModel":
					return typeof(MediaItemModel);
					break;
			}
			return null;
		}

		private DifferenceInfo GetDiffrenceInfo(Type type, XElement current, XElement previous)
		{
			var property = GetProperty(type, current.Name.LocalName);
			if (property == null)
			{
				return null;
			}
			if (property.GetCustomAttributes(typeof (SubPropertyIgnoreAttribute), false).Any())
			{
				return null;
			}
			var displayName = current.Name.LocalName;
			var displayAttribute = property.GetCustomAttributes(typeof (DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
			if (displayAttribute != null)
			{
				displayName = displayAttribute.Name;
			}

			var result = new DifferenceInfo()
			{
				PropertyName = displayName
			};
			var subPropertyNameAttribute = property.GetCustomAttributes(typeof(SubPropertyNameAttribute), false).FirstOrDefault() as SubPropertyNameAttribute;
			if (subPropertyNameAttribute == null)
			{
				result.Previous = previous ==null?String.Empty: previous.Value;
				result.Current = current == null ? String.Empty : current.Value;
			}
			else
			{
				if (String.IsNullOrEmpty(subPropertyNameAttribute.PropertyName))
				{
					return null;
				}
				if (!String.IsNullOrEmpty(subPropertyNameAttribute.TypeName))
				{
					var values = current.Elements(subPropertyNameAttribute.TypeName).Where(s => s.Element(subPropertyNameAttribute.PropertyName) != null).Select(s => s.Element(subPropertyNameAttribute.PropertyName).Value).ToArray();
					if (values.Length > 0)
					{
						result.Current = String.Join(" ,", values);
					}
					values = previous.Elements(subPropertyNameAttribute.TypeName).Where(s => s.Element(subPropertyNameAttribute.PropertyName) != null).Select(s => s.Element(subPropertyNameAttribute.PropertyName).Value).ToArray();
					if (values.Length > 0)
					{
						result.Previous = String.Join(" ,", values);
					}
				}
				else
				{
					result.Current = current.Element(subPropertyNameAttribute.PropertyName) == null
						? String.Empty
						: current.Element(subPropertyNameAttribute.PropertyName).Value;
					result.Previous = previous.Element(subPropertyNameAttribute.PropertyName) == null
						? String.Empty
						: previous.Element(subPropertyNameAttribute.PropertyName).Value;
				}
			}
			return result;
		}

		private PropertyInfo GetProperty(Type type, String name)
		{
			if (!_propertiesCache.ContainsKey(type.Name))
			{
				_propertiesCache.Add(type.Name, new Dictionary<string, PropertyInfo>());
			}

			if (!_propertiesCache[type.Name].ContainsKey(name))
			{
				var property = type.GetProperty(name);
				if (property == null)
				{
					return null;
				}
				_propertiesCache[type.Name].Add(name, property);
			}
			return _propertiesCache[type.Name][name];
		}
		#endregion
	}
}
