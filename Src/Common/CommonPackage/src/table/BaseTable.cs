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

	public class TblBase
	{
		public virtual uint primarykey { get; set; } = 0;
		public virtual uint secondarykey { get; set; } = 0;

		public virtual int PropertyCount { get => 2; }

		public IEnumerable<string> Properties
		{
			get
			{
				for(int i = 0; i < PropertyCount; i++)
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
				   primarykey == @base.primarykey &&
				   secondarykey == @base.secondarykey;
		}

		public override int GetHashCode()
		{
			int hashCode = -281792184;
			hashCode = hashCode * -1521134295 + primarykey.GetHashCode();
			hashCode = hashCode * -1521134295 + secondarykey.GetHashCode();
			return hashCode;
		}

		public virtual (string propertyName, Type type)? GetPropertyInfo(int index)
		{
			switch (index)
			{
				case 0: return (nameof(primarykey), primarykey.GetType());
				case 1: return (nameof(secondarykey), secondarykey.GetType());
			}

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
			if (propertyName.Equals("primaryKey", StringComparison.OrdinalIgnoreCase))
			{
				if (CheckProprtyType(primarykey, (uint)value) == false)
					return false;

				primarykey = (uint)value;
				return true;
			}

			if (propertyName.Equals("secondarykey", StringComparison.OrdinalIgnoreCase))
			{
				if (CheckProprtyType(secondarykey, (uint)value) == false)
					return false;

				secondarykey = (uint)value;
				return true;
			}

			return false;
		}

		protected bool CheckProprtyType<T>(T targetProperty, object value)
		{
			return typeof(T) == value.GetType() ? true : false;
		}
	}


	/// Example Table
	public class TblTable1 : TblBase
	{
		public override uint primarykey { get; set; }
		public override uint secondarykey { get; set; }

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
		public override uint primarykey { get; set; } = 0;
		public override uint secondarykey { get; set; } = 0;
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
			if (base.SetPropertyValue(propertyName, value) == true)
				return true;

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
