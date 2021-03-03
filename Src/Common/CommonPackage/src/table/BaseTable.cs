using CommonPackage.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommonPackage.Tables
{
	public class TableMeta
	{
		public string TableName { get; set; } = "";
		public string DbName { get; set; } = "";
		public string ClientDbName { get; set; } = "";
	}

	public partial class TblBase
	{
		public static long ConvertKey(object key)
		{
			if (key is int)
			{
				return ConvertKey((int)key);
			}
			else if (key is uint)
			{
				return ConvertKey((uint)key);
			}
			else if (key is string)
			{
				return ConvertKey((string)key);
			}
			else if (key.GetType().IsEnum == true)
			{
				return ConvertKey(Convert.ToInt32(key));
			}

			return 0;
		}

		public static long ConvertKey(int key)
		{
			return (long)key;
		}

		public static long ConvertKey(uint key)
		{
			return (long)key;
		}

		public static long ConvertKey(string key)
		{
			return (long)key.GetHashCode();
		}

		public static long ConvertKey<T>(T key) where T : Enum
		{
			return (long)Convert.ToInt32(key);
		}
	}

	public partial class TblBase
	{
		private long _queryPrimarykey = 0;
		private long _querySecondaryKey = 0;
		public long queryPrimaryKey { get => _queryPrimarykey; }
		public long querySecondaryKey { get => _querySecondaryKey; }

		public virtual int PropertyCount { get => 0; }

		public IEnumerable<string> Properties
		{
			get
			{
				for (int i = 0; i < PropertyCount; i++)
				{
					var propertyInfo = GetPropertyInfo(i);
					if (propertyInfo == null)
						continue;

					yield return propertyInfo?.propertyName;
				}
			}
		}

		public override bool Equals(object obj)
		{
			return obj is TblBase @base &&
				   queryPrimaryKey == @base.queryPrimaryKey &&
				   querySecondaryKey == @base.querySecondaryKey;
		}

		public override int GetHashCode()
		{
			int hashCode = -281792184;
			hashCode = hashCode * -1521134295 + queryPrimaryKey.GetHashCode();
			hashCode = hashCode * -1521134295 + querySecondaryKey.GetHashCode();
			return hashCode;
		}

		public virtual (string propertyName, Type type)? GetPropertyInfo(int index)
		{
			return null;
		}

		public bool SetPropertyValue(int index, object value)
		{
			var propertyInfo = GetPropertyInfo(index);
			if (propertyInfo == null)
				return false;

			return SetPropertyValue(propertyInfo?.propertyName, value);
		}

		public virtual bool SetPropertyValue(string propertyName, object value)
		{
			return false;
		}

		protected bool CheckProprtyType<T>(T targetProperty, object value)
		{
			return typeof(T) == value.GetType() ? true : false;
		}

		protected void ConvertKey(int key, bool primaryKey)
		{
			if (primaryKey == true)
				_queryPrimarykey = TblBase.ConvertKey(key);
			else
				_querySecondaryKey = TblBase.ConvertKey(key);
		}

		protected void ConvertKey(uint key, bool primaryKey)
		{
			if (primaryKey == true)
				_queryPrimarykey = TblBase.ConvertKey(key);
			else
				_querySecondaryKey = TblBase.ConvertKey(key);
		}

		protected void ConvertKey(string key, bool primaryKey)
		{
			key = key != null ? key : "";

			if (primaryKey == true)
				_queryPrimarykey = TblBase.ConvertKey(key);
			else
				_querySecondaryKey = TblBase.ConvertKey(key);
		}

		protected void ConvertKey<T>(T key, bool primaryKey) where T : Enum
		{
			if (primaryKey == true)
				_queryPrimarykey = TblBase.ConvertKey(key);
			else
				_querySecondaryKey = TblBase.ConvertKey(key);
		}
	}


	/// Example Table
	public class TblTable1 : TblBase
	{
		private uint _primarykey = 0;
		private uint _secondarkey = 0;

		public uint primarykey
		{
			get => _primarykey;
			set
			{
				_primarykey = value;
				ConvertKey(_primarykey, true);
			}
		}
		public uint secondarykey
		{
			get => _secondarkey;
			set
			{
				_secondarkey = value;
				ConvertKey(_secondarkey, false);
			}
		}

		public int Id { get; set; }
		public string Name { get; set; }

		public override int PropertyCount { get => 4; }

		public override (string propertyName, Type type)? GetPropertyInfo(int index)
		{
			switch (index)
			{
				case 0: return (nameof(primarykey), primarykey.GetType());
				case 1: return (nameof(secondarykey), secondarykey.GetType());
				case 2: return (nameof(Id), Id.GetType());
				case 3: return (nameof(Name), Name.GetType());
			}

			return null;
		}

		public override bool SetPropertyValue(string propertyName, object value)
		{
			if (base.SetPropertyValue(propertyName, value) == true)
				return true;

			if (propertyName.Equals("Id", StringComparison.OrdinalIgnoreCase))
			{
				if (CheckProprtyType(Id, (int)value) == false)
					return false;

				Id = (int)value;
				return true;
			}

			if (propertyName.Equals("Name", StringComparison.OrdinalIgnoreCase))
			{
				if (CheckProprtyType(Name, (string)value) == false)
					return false;

				Name = (string)value;
				return true;
			}

			return false;
		}
	}

	public class TblTable2 : TblBase
	{
		private TestEnum _primarykey = TestEnum.None;
		public TestEnum primarykey
		{
			get => _primarykey;
			set
			{
				_primarykey = value;
				ConvertKey(_primarykey, true);
			}
		}

		private string _secondaryKey = "";
		public string secondarykey
		{
			get => _secondaryKey;
			set
			{
				_secondaryKey = value;
				ConvertKey(_secondaryKey, false);
			}
		}

		public int IntField1 { get; set; } = 0;
		public string StringField2 { get; set; } = "";
		public float FloatField3 { get; set; } = 0;
		public TestEnum EnumField4 { get; set; } = TestEnum.None;

		public override int PropertyCount { get => 6; }

		public override (string propertyName, Type type)? GetPropertyInfo(int index)
		{
			switch (index)
			{
				case 0: return (nameof(primarykey), primarykey.GetType());
				case 1: return (nameof(secondarykey), secondarykey.GetType());
				case 2: return (nameof(IntField1), IntField1.GetType());
				case 3: return (nameof(StringField2), StringField2.GetType());
				case 4: return (nameof(FloatField3), FloatField3.GetType());
				case 5: return (nameof(EnumField4), EnumField4.GetType());
			}

			return null;
		}

		public override bool SetPropertyValue(string propertyName, object value)
		{
			if (propertyName.Equals("primarykey", StringComparison.OrdinalIgnoreCase))
			{
				if (CheckProprtyType(primarykey, (TestEnum)value) == false)
					return false;

				primarykey = (TestEnum)value;
				return true;
			}

			if (propertyName.Equals("secondarykey", StringComparison.OrdinalIgnoreCase))
			{
				if (CheckProprtyType(secondarykey, (string)value) == false)
					return false;

				secondarykey = (string)value;
				return true;
			}

			if (propertyName.Equals("IntField1", StringComparison.OrdinalIgnoreCase))
			{
				if (CheckProprtyType(IntField1, (int)value) == false)
					return false;

				IntField1 = (int)value;
				return true;
			}

			if (propertyName.Equals("StringField2", StringComparison.OrdinalIgnoreCase))
			{
				if (CheckProprtyType(StringField2, (string)value) == false)
					return false;

				StringField2 = (string)value;
				return true;
			}

			if (propertyName.Equals("FloatField3", StringComparison.OrdinalIgnoreCase))
			{
				if (CheckProprtyType(FloatField3, (float)value) == false)
					return false;

				FloatField3 = (float)value;
				return true;
			}

			if (propertyName.Equals("EnumField4", StringComparison.OrdinalIgnoreCase))
			{
				if (CheckProprtyType(EnumField4, (TestEnum)value) == false)
					return false;

				EnumField4 = (TestEnum)value;
				return true;
			}

			return false;
		}

	}

}
