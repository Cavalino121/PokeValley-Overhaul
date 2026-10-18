using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Sickhead.Engine.Util;

public static class ObjToStr
{
	private struct ToStringDescription
	{
		public Type Type;

		public List<ToStringMember> Members;
	}

	private struct ToStringMember
	{
		public MemberInfo Member;

		private string _name;

		private string _format;

		public string Name
		{
			get
			{
				if (!string.IsNullOrEmpty(_name))
				{
					return _name;
				}
				return Member.Name;
			}
			set
			{
				_name = value;
			}
		}

		public string Format
		{
			get
			{
				if (!string.IsNullOrEmpty(_format))
				{
					return _format;
				}
				return "{0}";
			}
			set
			{
				_format = value;
			}
		}
	}

	public class Style
	{
		public bool ShowRootObjectType;

		public string ObjectDelimiter;

		public string MemberDelimiter;

		public string MemberNameValueDelimiter;

		public bool TrailingNewline;

		public static Style TypeAndMembersSingleLine = new Style
		{
			ShowRootObjectType = true,
			ObjectDelimiter = ":",
			MemberDelimiter = ",",
			MemberNameValueDelimiter = "="
		};

		public static Style MembersOnlyMultiline = new Style
		{
			ShowRootObjectType = false,
			ObjectDelimiter = "",
			MemberDelimiter = "\n",
			MemberNameValueDelimiter = "="
		};

		public Style()
		{
			ShowRootObjectType = true;
			ObjectDelimiter = ":";
			MemberDelimiter = ",";
			MemberNameValueDelimiter = "=";
		}
	}

	private static readonly StringBuilder _stringBuilder = new StringBuilder();

	private static readonly Dictionary<Type, ToStringDescription> _cache = new Dictionary<Type, ToStringDescription>();

	public static string Format(object obj, Style style)
	{
		Type type = obj.GetType();
		_cache.Clear();
		if (!_cache.TryGetValue(obj.GetType(), out var value))
		{
			value = new ToStringDescription
			{
				Type = type,
				Members = new List<ToStringMember>()
			};
			_cache.Add(type, value);
			BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			FieldInfo[] fields = type.GetFields(bindingAttr);
			foreach (FieldInfo fieldInfo in fields)
			{
				ToStringMember item = new ToStringMember
				{
					Member = fieldInfo,
					Name = fieldInfo.Name
				};
				Type dataType = fieldInfo.GetDataType();
				if (dataType == typeof(string))
				{
					item.Format = "\"{0}\"";
				}
				if (dataType.HasElementType)
				{
					item.Format = "{1}[{2}] {0}";
				}
				value.Members.Add(item);
			}
			value.Members.Sort(CompareToStringMembers);
		}
		lock (_stringBuilder)
		{
			_stringBuilder.Clear();
			if (style.ShowRootObjectType)
			{
				_stringBuilder.Append(value.Type.Name);
				_stringBuilder.Append(style.ObjectDelimiter);
			}
			for (int j = 0; j < value.Members.Count; j++)
			{
				ToStringMember toStringMember = value.Members[j];
				Type dataType2 = toStringMember.Member.GetDataType();
				object value2 = toStringMember.Member.GetValue(obj);
				_stringBuilder.Append(dataType2.Name);
				_stringBuilder.Append(" ");
				_stringBuilder.Append(toStringMember.Name);
				_stringBuilder.Append(style.MemberNameValueDelimiter);
				if (value2 == null)
				{
					_stringBuilder.Append("null");
				}
				else
				{
					Type type2 = value2.GetType();
					if (type2.HasElementType)
					{
						Type elementType = type2.GetElementType();
						string arg = "?";
						_stringBuilder.AppendFormat(toStringMember.Format, value2, elementType, arg);
					}
					else
					{
						_stringBuilder.AppendFormat(toStringMember.Format, value2);
					}
				}
				if (j != value.Members.Count - 1)
				{
					_stringBuilder.Append(style.MemberDelimiter);
				}
			}
			return _stringBuilder.ToString();
		}
	}

	private static int CompareToStringMembers(ToStringMember a, ToStringMember b)
	{
		return a.Name.CompareTo(b.Name);
	}
}
