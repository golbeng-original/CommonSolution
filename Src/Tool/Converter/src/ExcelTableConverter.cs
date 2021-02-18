using System;
using System.Collections.Generic;
using System.Text;
using GolbengFramework.Serialize;
using GolbengFramework.Parser;
using System.IO;
using Microsoft.Data.Sqlite;
using System.Linq;

namespace GolbengFramework.Converter
{
	public class ExcelTableConverter
	{
		private string _extractRootPath = "";

		private List<ExcelDataTable> _excelDataTables = new List<ExcelDataTable>();
		private EnumsDefines _enumDefines = null;

		public ExcelTableConverter(string extractRootPath, ExcelDataTable excelDataTable, EnumsDefines enumDefine)
		{
			_extractRootPath = extractRootPath;
			_excelDataTables.Add(excelDataTable);
			_enumDefines = enumDefine;
		}

		public ExcelTableConverter(string extractRootPath, List<ExcelDataTable> excelDataTables, EnumsDefines enumDefine)
		{
			_extractRootPath = extractRootPath;
			_excelDataTables = excelDataTables;
			_enumDefines = enumDefine;
		}

		public void Convter(bool useClient = false)
		{
			if (_excelDataTables.Count == 0)
				throw new ArgumentNullException($"{nameof(ExcelDataTable)} is null");

			SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_winsqlite3());

			string dbDname = useClient ? _excelDataTables[0].SchemaData.ClientUseDbName : _excelDataTables[0].SchemaData.DbName;

			string dbPath = $@"{_extractRootPath}\{dbDname}";
			using (var conn = new SqliteConnection($"Data Source={dbPath}"))
			{
				conn.Open();

				try
				{
					DeleteTable(conn, _excelDataTables[0].SchemaData);
					CreateTable(conn, _excelDataTables[0].SchemaData);

					foreach(var excelDataTable in _excelDataTables)
					{
						InsertTable(conn, excelDataTable);
					}
				}
				catch(Exception e)
				{
					throw e;
				}
			}
		}

		private void DeleteTable(SqliteConnection conn, ExcelSchemaData excelSchemaData)
		{
			using (var sqliteCommand = conn.CreateCommand())
			{
				sqliteCommand.CommandText = $"DROP TABLE IF EXISTS {excelSchemaData.TableName}";
				sqliteCommand.ExecuteNonQuery();
			}
		}

		private void CreateTable(SqliteConnection conn, ExcelSchemaData excelSchemaData)
		{
			StringBuilder query = new StringBuilder();
			query.Append($"CREATE TABLE IF NOT EXISTS \"{excelSchemaData.TableName}\" ");
			query.Append("(");


			List<string> primaryFields = new List<string>();

			List<string> tableFields = new List<string>();
			foreach (var field in excelSchemaData.SchemaFields)
			{
				var sqliteType = field.GetSqliteType();
				if (string.IsNullOrEmpty(sqliteType))
					throw new Exception($"{field.Name} 필드 sqliteType 변환 오류");

				StringBuilder fieldBuilder = new StringBuilder();
				fieldBuilder.Append($"\"{field.Name}\"");
				fieldBuilder.Append($" {sqliteType}");
				
				if(field.Primary == true)
				{
					fieldBuilder.Append(" NOT NULL");
					primaryFields.Add($"\"{field.Name}\"");
				}

				tableFields.Add(fieldBuilder.ToString());
			}

			var fieldsDefine = string.Join(",\n", tableFields.ToArray());
			query.Append(fieldsDefine);
			if(primaryFields.Count > 0)
			{
				query.Append(",");

				string primaryDefine = string.Join(",", primaryFields.ToArray());
				primaryDefine = $"PRIMARY KEY({primaryDefine})";
				query.Append(primaryDefine);
			}

			query.Append(");");

			using (var sqliteCommand = conn.CreateCommand())
			{
				sqliteCommand.CommandText = query.ToString();
				sqliteCommand.ExecuteNonQuery();
			}
		}

		private void InsertTable(SqliteConnection conn, ExcelDataTable excelDataTable)
		{
			var tableName = excelDataTable.SchemaData.TableName;

			var schemaList = from schema in excelDataTable.SchemaData.SchemaFields
							 select new {
								 Name = schema.Name,
								 Type = GetSqliteType(schema.GetSqliteType()),
								 Default = GetSqliteDefaultValue(schema)
							 };

			foreach(var schema in schemaList)
			{
				if (schema.Type == null)
					throw new Exception($"InsertTable Excpetion {schema.Name}가 Type 정의가 잘못 되었습니다.");
			}

			using (var transaction = conn.BeginTransaction())
			{
				var command = conn.CreateCommand();

				var parameterDefines = string.Join(",", schemaList.Select(s => s.Name).ToArray());
				var parameters = string.Join(",", schemaList.Select(s => $"@{s.Name}").ToArray());

				string insertQuery = $@"INSERT INTO {tableName}({parameterDefines}) VALUES({parameters})";
				command.CommandText = insertQuery;

				foreach (var schema in schemaList)
				{
					command.Parameters.Add($"@{schema.Name}", schema.Type.Value);
				}

				try
				{
					foreach (var row in MakeSqliteRow(excelDataTable))
					{
						foreach (var element in row)
						{
							command.Parameters[$"@{element.ColumnName}"].Value = element.Value;
						}

						command.ExecuteNonQuery();
					}

					transaction.Commit();
				}
				catch (Exception e)
				{
					transaction.Rollback();
					throw e;
				}
			}
		}

		private IEnumerable<List<(string ColumnName, object Value)>> MakeSqliteRow(ExcelDataTable excelDataTable)
		{
			foreach (var row in excelDataTable.GetMappingRows())
			{
				List<(string ColumnName, object Value)> sqliteRow = new List<(string ColumnName, object Value)>();
				foreach (var element in row)
				{
					var schemaField = excelDataTable.SchemaData.FindExcelSchemaField(element.ColumnName);
					if (schemaField == null)
						throw new Exception($"{element.ColumnName}이름 SchemaField 정보가 없습니다.");

					if(schemaField.GetNativeType() == typeof(Enum))
					{
						sqliteRow.Add((element.ColumnName, GetSqliteEnumValue(schemaField.Type, element.Value as string)));
						continue;
					}

					sqliteRow.Add((element.ColumnName, element.Value));
				}

				yield return sqliteRow;
			}
		}

		private SqliteType? GetSqliteType(string sqliteTypeStr)
		{
			SqliteType result;
			if (Enum.TryParse(sqliteTypeStr, out result) == false)
				return null;

			return result;
		}

		private object GetSqliteDefaultValue(ExcelSchemaField excelSchemaField)
		{
			if(excelSchemaField.GetNativeType() == typeof(Enum))
			{
				return GetSqliteEnumValue(excelSchemaField.Type, excelSchemaField.Default);
			}

			return excelSchemaField.GetNativeDefault();
		}

		private object GetSqliteEnumValue(string Type, string Value)
		{
			var enumValue = _enumDefines.ParseEnumvalue(Type, Value);
			if (enumValue == null)
				throw new Exception($"GetSqliteEnumValue Exception enum Value ({Type}.{Value}) \n Need Sync Format");

			return enumValue.Value;
		}
	}
}
