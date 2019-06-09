using EngineFramework.Setting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace EngineFramework.Storage
{
    public static class StorageManager
    {
        public static T GetSetting<T>(string section, string property)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(AppSettings.GetConnectionString("AgentController")))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand(@"SELECT ID,
       Section,
       Property,
       Value FROM dbo.Settings WHERE Section = @Section AND Property = @Property", connection);
                    command.Parameters.AddWithValue("@Section", section);
                    command.Parameters.AddWithValue("@Property", property);
                    string value = null;
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                        value = (string)reader["Value"];
                    reader.Close();
                    connection.Close();

                    if (string.IsNullOrWhiteSpace(value))
                        return default(T);
                    else
                        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(value);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Can't Get Setting", ex);
            }
        }

        public static void SaveSetting<T>(string section, string property, T value)
        {
            int numRecordAffect = 0;
            try
            {
                using (SqlConnection connection = new SqlConnection(AppSettings.GetConnectionString("AgentController")))
                {
                    connection.Open();

                    SqlCommand command = new SqlCommand(@"MERGE  dbo.Settings AS target
	USING  (SELECT @Section AS SectionName, @Property AS PropertyName) AS source
	ON target.Section = source.SectionName AND target.Property = source.PropertyName
	WHEN MATCHED THEN
		UPDATE SET target.Value = @value
	WHEN NOT MATCHED THEN
	INSERT (Section, Property, Value) VALUES (source.SectionName, source.PropertyName, @value);", connection);
                    command.Parameters.AddWithValue("@Section", section);
                    command.Parameters.AddWithValue("@Property", property);
                    command.Parameters.AddWithValue("@value", JsonConvert.SerializeObject(value));
                    
                    numRecordAffect = command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Can't Save Setting, See Inner Exception", ex);
            }

            if (numRecordAffect != 1)
                throw new Exception("Can't Save Setting");
        }
    }
}
