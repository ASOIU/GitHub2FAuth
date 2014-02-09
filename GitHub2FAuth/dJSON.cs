using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;


namespace GitHub2FAuth
{
	sealed class dJSON:JavaScriptSerializer
	{
		public dJSON()
		{
			RegisterConverters(new[] { new DynamicJsonConverter() });
		}

		//
		// Summary:
		//     Converts the specified JSON string to dynamic object.
		//
		// Parameters:
		//   input:
		//     The JSON string to be deserialized.
		//
		// Returns:
		//     The deserialized object.
		//
		// Exceptions:
		//   System.ArgumentNullException:
		//     input is null.
		//
		//   System.ArgumentException:
		//     The input length exceeds the value of System.Web.Script.Serialization.JavaScriptSerializer.MaxJsonLength.
		//     -or- The recursion limit defined by System.Web.Script.Serialization.JavaScriptSerializer.RecursionLimit
		//     was exceeded. -or- input contains an unexpected character sequence. -or-
		//     input is a dictionary type and a non-string key value was encountered. -or-
		//     input includes member definitions that are not available on the target type.
		//
		//   System.InvalidOperationException:
		//     input contains a "__type" property that indicates a custom type, but the
		//     type resolver that is currently associated with the serializer cannot find
		//     a corresponding managed type. -or- input contains a "__type" property that
		//     indicates a custom type, but the result of deserializing the corresponding
		//     JSON string cannot be assigned to the expected target type. -or- input contains
		//     a "__type" property that indicates either System.Object or a non-instantiable
		//     type (for example, an abstract type or an interface).-or- An attempt was
		//     made to convert a JSON array to an array-like managed type that is not supported
		//     for use as a JSON deserialization target. -or- It is not possible to convert
		//     input to the target type.
		public dynamic Deserialize(string input)
		{
			return base.Deserialize<object>(input);
		}

		private sealed class DynamicJsonConverter:JavaScriptConverter
		{

			public override object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer)
			{
				if(dictionary == null)
					throw new ArgumentNullException("dictionary");

				return type == typeof(object) ? new DynamicJsonObject(dictionary) : null;
			}

			public override IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
			{
				throw new NotImplementedException();
			}

			public override IEnumerable<Type> SupportedTypes
			{
				get { return new ReadOnlyCollection<Type>(new List<Type>(new[] { typeof(object) })); }
			}


			private sealed class DynamicJsonObject:DynamicObject
			{
				private readonly IDictionary<string, object> _dictionary;

				public DynamicJsonObject(IDictionary<string, object> dictionary)
				{
					if(dictionary == null)
						throw new ArgumentNullException("dictionary");
					_dictionary = dictionary;
				}

				public override string ToString()
				{
					var sb = new StringBuilder("{");
					ToString(sb);
					return sb.ToString();
				}

				private void ToString(StringBuilder sb)
				{
					var firstInDictionary = true;
					foreach(var pair in _dictionary) {
						if(!firstInDictionary)
							sb.Append(",");
						firstInDictionary = false;
						var value = pair.Value;
						var name = pair.Key;
						if(value is string) {
							sb.AppendFormat("{0}:\"{1}\"", name, value);
						} else if(value is IDictionary<string, object>) {
							new DynamicJsonObject((IDictionary<string, object>)value).ToString(sb);
						} else if(value is ArrayList) {
							sb.Append(name + ":[");
							var firstInArray = true;
							foreach(var arrayValue in (ArrayList)value) {
								if(!firstInArray)
									sb.Append(",");
								firstInArray = false;
								if(arrayValue is IDictionary<string, object>)
									new DynamicJsonObject((IDictionary<string, object>)arrayValue).ToString(sb);
								else if(arrayValue is string)
									sb.AppendFormat("\"{0}\"", arrayValue);
								else
									sb.AppendFormat("{0}", arrayValue);

							}
							sb.Append("]");
						} else {
							sb.AppendFormat("{0}:{1}", name, value);
						}
					}
					sb.Append("}");
				}

				public override bool TryGetMember(GetMemberBinder binder, out object result)
				{
					if(!_dictionary.TryGetValue(binder.Name, out result)) {
						// return null to avoid exception.  caller can check for null this way...
						result = null;
						return true;
					}

					result = WrapResultObject(result);
					return true;
				}

				public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
				{
					if(indexes.Length == 1 && indexes[0] != null) {
						if(!_dictionary.TryGetValue(indexes[0].ToString(), out result)) {
							// return null to avoid exception.  caller can check for null this way...
							result = null;
							return true;
						}

						result = WrapResultObject(result);
						return true;
					}

					return base.TryGetIndex(binder, indexes, out result);
				}

				private static object WrapResultObject(object result)
				{
					var dictionary = result as IDictionary<string, object>;
					if(dictionary != null)
						return new DynamicJsonObject(dictionary);

					var arrayList = result as ArrayList;
					if(arrayList != null && arrayList.Count > 0) {
						return arrayList[0] is IDictionary<string, object> 
                    ? new List<object>(arrayList.Cast<IDictionary<string, object>>().Select(x => new DynamicJsonObject(x))) 
                    : new List<object>(arrayList.Cast<object>());
					}

					return result;
				}
			}
		}
	}
}
