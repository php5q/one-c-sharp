﻿using System;
using System.Data;
using System.Data.SqlClient;
using Zhichkin.ORM;

namespace Zhichkin.Metadata.Model
{
    public partial class Property
    {
        public sealed class DataMapper : IDataMapper
        {
            # region " SQL "
            private const string SelectCommandText = @"SELECT [entity], [name], [purpose], [version], [ordinal], [is_abstract], [is_read_only], [is_primary_key] FROM [metadata].[properties] WHERE [key] = @key";
            private const string InsertCommandText =
                @"DECLARE @result table([version] binary(8)); " +
                @"INSERT [metadata].[properties] ([key], [entity], [name], [purpose], [ordinal], [is_abstract], [is_read_only], [is_primary_key]) " +
                @"OUTPUT inserted.[version] INTO @result " +
                @"VALUES (@key, @entity, @name, @purpose, @ordinal, @is_abstract, @is_read_only, @is_primary_key); " +
                @"IF @@ROWCOUNT > 0 SELECT [version] FROM @result;";
            private const string UpdateCommandText =
                @"DECLARE @rows_affected int; DECLARE @result table([version] binary(8)); " +
                @"UPDATE [metadata].[properties] SET [entity] = @entity, [name] = @name, [purpose] = @purpose, [ordinal] = @ordinal, " +
                @"[is_abstract] = @is_abstract, [is_read_only] = @is_read_only, [is_primary_key] = @is_primary_key " +
                @"OUTPUT inserted.[version] INTO @result" +
                @" WHERE [key] = @key AND [version] = @version; " +
                @"SET @rows_affected = @@ROWCOUNT; " +
                @"IF (@rows_affected = 0) " +
                @"BEGIN " +
                @"  INSERT @result ([version]) SELECT [version] FROM [metadata].[properties] WHERE [key] = @key; " +
                @"END " +
                @"SELECT @rows_affected, [version] FROM @result;";
            private const string DeleteCommandText =
                @"DELETE [metadata].[properties] WHERE [key] = @key " +
                @"   AND ([version] = @version OR @version = 0x00000000); " + // taking into account deletion of the entities having virtual state
                @"SELECT @@ROWCOUNT;";
            # endregion
            
            private readonly string ConnectionString;
            private readonly IReferenceObjectFactory Factory;

            private DataMapper() { }
            public DataMapper(string connectionString, IReferenceObjectFactory factory)
            {
                ConnectionString = connectionString;
                Factory = factory;
            }

            void IDataMapper.Select(IPersistent entity)
            {
                Property e = (Property)entity;

                bool ok = false;

                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    SqlCommand command  = connection.CreateCommand();
                    command.CommandType = CommandType.Text;
                    command.CommandText = SelectCommandText;

                    SqlParameter parameter = null;

                    parameter = new SqlParameter("key", SqlDbType.UniqueIdentifier);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = e.identity;
                    command.Parameters.Add(parameter);

                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        e.entity  = Factory.New<Entity>((Guid)reader[0]);
                        e.name    = (string)reader[1];
                        e.purpose = (PropertyPurpose)reader[2];
                        e.version = (byte[])reader[3];
                        e.ordinal = reader.GetInt32(4);
                        e.isAbstract = reader.GetBoolean(5);
                        e.isReadOnly = reader.GetBoolean(6);
                        e.isPrimaryKey = reader.GetBoolean(7);

                        ok = true;
                    }

                    reader.Close(); connection.Close();
                }

                if (!ok) throw new ApplicationException("Error executing select command.");
            }
            void IDataMapper.Insert(IPersistent entity)
            {
                Property e = (Property)entity;

                bool ok = false;

                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    SqlCommand command  = connection.CreateCommand();
                    command.CommandType = CommandType.Text;
                    command.CommandText = InsertCommandText;

                    SqlParameter parameter = null;

                    parameter = new SqlParameter("key", SqlDbType.UniqueIdentifier);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = e.identity;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("entity", SqlDbType.UniqueIdentifier);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = (e.entity == null) ? Guid.Empty : e.entity.Identity;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("name", SqlDbType.NVarChar);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = (e.name == null) ? string.Empty : e.name;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("purpose", SqlDbType.Int);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = (int)e.purpose;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("ordinal", SqlDbType.Int);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = e.ordinal;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("is_abstract", SqlDbType.Bit);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = e.isAbstract;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("is_read_only", SqlDbType.Bit);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = e.isReadOnly;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("is_primary_key", SqlDbType.Bit);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = e.isPrimaryKey;
                    command.Parameters.Add(parameter);

                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        e.version = (byte[])reader[0]; ok = true;
                    }

                    reader.Close(); connection.Close();
                }

                if (!ok) throw new ApplicationException("Error executing insert command.");
            }
            void IDataMapper.Update(IPersistent entity)
            {
                Property e = (Property)entity;

                bool ok = false; int rows_affected = 0;

                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    SqlCommand command  = connection.CreateCommand();
                    command.CommandType = CommandType.Text;
                    command.CommandText = UpdateCommandText;

                    SqlParameter parameter = null;

                    parameter = new SqlParameter("key", SqlDbType.UniqueIdentifier);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = e.identity;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("version", SqlDbType.Timestamp);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = e.version;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("entity", SqlDbType.UniqueIdentifier);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = (e.entity == null) ? Guid.Empty : e.entity.Identity;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("name", SqlDbType.NVarChar);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = (e.name == null) ? string.Empty : e.name;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("purpose", SqlDbType.Int);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = (int)e.purpose;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("ordinal", SqlDbType.Int);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = e.ordinal;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("is_abstract", SqlDbType.Bit);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = e.isAbstract;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("is_read_only", SqlDbType.Bit);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = e.isReadOnly;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("is_primary_key", SqlDbType.Bit);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = e.isPrimaryKey;
                    command.Parameters.Add(parameter);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            rows_affected = reader.GetInt32(0);
                            e.version = (byte[])reader[1];
                            if (rows_affected == 0)
                            {
                                e.state = PersistentState.Changed;
                            }
                            else
                            {
                                ok = true;
                            }
                        }
                        else
                        {
                            e.state = PersistentState.Deleted;
                        }
                    }
                }

                if (!ok) throw new OptimisticConcurrencyException(e.state.ToString());
            }
            void IDataMapper.Delete(IPersistent entity)
            {
                Property e = (Property)entity;

                bool ok = false;

                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    SqlCommand command  = connection.CreateCommand();
                    command.CommandType = CommandType.Text;
                    command.CommandText = DeleteCommandText;

                    SqlParameter parameter = null;

                    parameter = new SqlParameter("key", SqlDbType.UniqueIdentifier);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = e.identity;
                    command.Parameters.Add(parameter);

                    parameter = new SqlParameter("version", SqlDbType.Timestamp);
                    parameter.Direction = ParameterDirection.Input;
                    parameter.Value = e.version;
                    command.Parameters.Add(parameter);

                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read()) { ok = (int)reader[0] > 0; }

                    reader.Close(); connection.Close();
                }

                if (!ok) throw new ApplicationException("Error executing delete command.");
            }
        }
    }
}