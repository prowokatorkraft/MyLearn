using System;
using System.Data.SqlClient;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace TimeManagement
{
    enum RegisterTableColumn : int // Хрупкая конструкция
    {
        // Порядок констант должен соблюдаться согласно столбцам в таблицах БД
        Id, TableName, Use, Name, ItemNames,
        Length = 5 // Колличество констант(изменяется вручную)
    }
    enum TableColumn : int // Хрупкая конструкция
    {
        // Порядок констант должен соблюдаться согласно столбцам в таблицах БД
        Id, PreRegisterId, PreTableId, PostRegisterId, PostTableId, Name,
        Length = 6 // Колличество констант(изменяется вручную)
    }

    class DbSql
    {
        private SqlConnection connection;
        private string connectionString;
        private SqlCommand command;

        public bool IsConnection { get => connection != null ? true : false; }

        public DbSql()
        {
            connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + new FileInfo(@".\..\..").FullName + @"\" + Rules.DataBaseName + ".mdf;Integrated Security=True";

            connection = new SqlConnection(connectionString);

            Connect(); // TODO: Добалить обработчик исключений
            AddRegister();
            AddRoot();
        }

        #region Методы работы с базой данных
        /// <summary> Подключение к базе данных </summary>
        private async void ConnectAsync()
        {
            await connection.OpenAsync();
            command = new SqlCommand("SELECT * FROM " + Rules.RootTableName, connection);
        }
        private void Connect()
        {
            connection.Open();
            command = new SqlCommand("SELECT * FROM " + Rules.RootTableName, connection);
        }
        /// <summary> Завершение связи с базой данных </summary>
        public void Close()
        {
            connection.Close();
        }
        #endregion

        #region Работа с таблицами
        /// <summary> Возвращает массив табличных имен БД. </summary>
        public string[] GetTableNames()
        {
            int Lenght = 0;
            command = new SqlCommand("SELECT TABLE_NAME FROM information_schema.TABLES", connection);

            // Подсчет таблиц в БД
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Lenght++;
            }
            reader.Close();

            // Перебор таблиц в БД
            string[] names = new string[Lenght];
            reader = command.ExecuteReader();
            for (int i = 0; reader.Read() && i < Lenght; i++)
            {
                names[i] = reader.GetValue(0).ToString();
            }
            reader.Close();

            return names;
        }
        /// <summary> Возвращает массив табличных имен БД без корневого имени. </summary>
        public string[] GetTableNamesNonRoot()
        {
            string[] names = GetTableNames();

            if (Rules.CheeckForbiddenTableNames(names) > 0)
            {
                string[] namesNonRoot = new string[names.Length - Rules.CheeckForbiddenTableNames(names)];
                for (int i = 0, iError = 0; i < names.Length; i++)
                {
                    if (Rules.CheeckForbiddenTableName(names[i]))
                    {
                        namesNonRoot[i - iError] = names[i];
                    }
                    else iError++;
                }

                return namesNonRoot;
            }

            return names;
        }
        /// <summary> Проверяет наличие таблицы в БД и возвращает логическое значение. </summary>
        /// <param name="tableName"> Табличное имя </param> <returns> true в наличии </returns>
        public bool CheckTable(string tableName)
        {
            string[] tableNames = GetTableNames();

            for (int i = 0; i < tableNames.Length; i++)
            {
                if (tableNames[i] == tableName) return true;
            }
            return false;
        }
        /// <summary> Проверяет наличие таблицы в регистре и возвращает логическое значение </summary>
        /// <param name="registerId"></param>
        /// <returns></returns>
        public bool CheckTableRegister(int registerId)
        {
            command = new SqlCommand("SELECT * FROM " + Rules.RegisterTableName + " WHERE Id = " + registerId, connection);
            SqlDataReader reader = command.ExecuteReader();

            reader.Read();
            bool IsReader = reader.HasRows;
            reader.Close();

            return IsReader;
        }

        /// <summary> Возвращает имя таблицы в регистре </summary>
        /// <param name="registerId"></param>
        /// <returns></returns>
        public string GetTableNameRegister(int registerId)
        {
            command = new SqlCommand("SELECT * FROM " + Rules.RegisterTableName + " WHERE " + RegisterTableColumn.Id + " = " + registerId, connection);
            SqlDataReader reader = command.ExecuteReader();

            reader.Read();
            string TableName = reader.GetString((int)RegisterTableColumn.TableName);
            reader.Close();

            return TableName;
        }

        /// <summary> Добавляет таблицу Регистр в БД и возвращает код ошибки. </summary>
        /// <returns> Возвращает -1 если в наличии </returns>
        private int AddRegister()
        {
            // Проверка на наличие таблицы
            if (CheckTable(Rules.RegisterTableName)) return -1;

            // Создание таблицы
            command = new SqlCommand("CREATE TABLE [dbo].[" + Rules.RegisterTableName + "] ([" + RegisterTableColumn.Id + "] INT NOT NULL PRIMARY KEY IDENTITY, [" + RegisterTableColumn.TableName + "] NVARCHAR(50) NOT NULL, [" + RegisterTableColumn.Use + "] BIT NOT NULL, [" + RegisterTableColumn.Name + "] NVARCHAR(50) NULL, [" + RegisterTableColumn.ItemNames + "] NVARCHAR(50) NULL)", connection);
            command.ExecuteNonQuery();

            return 0;
        }
        /// <summary> Добавляет таблицу Регистр в БД и возвращает id. </summary>
        /// <returns> Возвращает -1 если в наличии,</returns>
        private int AddRoot()
        {
            // Проверка на наличие таблицы
            if (CheckTable(Rules.RootTableName)) return -1;

            // Регистрация
            command = new SqlCommand("INSERT INTO [dbo].[" + Rules.RegisterTableName + "]([" + RegisterTableColumn.TableName + "], [" + RegisterTableColumn.Use + "]) VALUES('" + Rules.RootTableName + "', 1) SELECT @@IDENTITY", connection);
            int idRegister = int.Parse(command.ExecuteScalar().ToString());

            // Создание таблицы
            command = new SqlCommand("CREATE TABLE [dbo].[" + Rules.RootTableName + "] ([" + TableColumn.Id + "] INT IDENTITY (1, 1) NOT NULL, [" + TableColumn.PreRegisterId + "] INT NULL, [" + TableColumn.PreTableId + "] INT NULL, [" + TableColumn.PostRegisterId + "] NVARCHAR (50) NULL, [" + TableColumn.PostTableId + "] NVARCHAR (50) NULL, [" + TableColumn.Name + "] NVARCHAR (50) NULL, PRIMARY KEY CLUSTERED ([" + TableColumn.Id + "] ASC))", connection);
            command.ExecuteNonQuery();

            return idRegister;
        }

        /// <summary> Добавляет таблицу в БД и возвращает id </summary>
        public int AddTable()
        {
            ///// Регистрация
            command = new SqlCommand("INSERT INTO [dbo].[" + Rules.RegisterTableName + "]([" + RegisterTableColumn.TableName + "], [" + RegisterTableColumn.Use + "]) VALUES('" + Rules.TemplateTableName + "', 1) SELECT @@IDENTITY", connection);
            int idRegister = int.Parse(command.ExecuteScalar().ToString());
            string tableName = Rules.TemplateTableName + idRegister;

            command = new SqlCommand("UPDATE [dbo].[" + Rules.RegisterTableName + "] SET " + RegisterTableColumn.TableName + " = '" + tableName + "' WHERE " + RegisterTableColumn.Id + " = " + idRegister, connection);
            command.ExecuteNonQuery();

            // Создание таблицы
            command = new SqlCommand("CREATE TABLE [dbo].[" + tableName + "] ([" + TableColumn.Id + "] INT IDENTITY (1, 1) NOT NULL, [" + TableColumn.PreRegisterId + "] INT NULL, [" + TableColumn.PreTableId + "] INT NULL, [" + TableColumn.PostRegisterId + "] NVARCHAR (50) NULL, [" + TableColumn.PostTableId + "] NVARCHAR (50) NULL, [" + TableColumn.Name + "] NVARCHAR (50) NULL, PRIMARY KEY CLUSTERED ([" + TableColumn.Id + "] ASC))", connection);
            command.ExecuteNonQuery();

            return idRegister;
        }
        /// <summary> Удаляет таблицу в БД </summary>
        /// <param name="tableName"> Табличное имя </param>
        public void DelTable(int registerId)
        {
            //// Проверка на запрещенные таблицы
            if (!Rules.CheeckForbiddenTableName(GetTableNameRegister(registerId))) throw new Exception("Попытка удаления запрещенной таблицы");

            //// Регистрация
            string tableName = GetTableNameRegister(registerId); // TODO: Создать константы Enum
            command = new SqlCommand("DELETE FROM [dbo].[" + Rules.RegisterTableName + "] WHERE [" + RegisterTableColumn.Id + "] = " + registerId, connection);
            command.ExecuteNonQuery();

            // Удаление таблицы
            command = new SqlCommand("DROP TABLE [dbo].[" + tableName + "]", connection);
            command.ExecuteNonQuery();
        }
        /// <summary> Возвращает статус таблицы </summary>
        /// <param name="registerId"></param>
        public bool IsHideTable(int registerId)
        {
            command = new SqlCommand("SELECT * FROM " + Rules.RegisterTableName + " WHERE " + RegisterTableColumn.Id + " = " + registerId, connection);
            SqlDataReader reader = command.ExecuteReader();

            reader.Read();
            bool tableHide = reader.GetBoolean(2);
            reader.Close();

            return tableHide;
        }
        /// <summary> Изменяет статус таблицы на противоположный </summary>
        /// <param name="registerId"></param>
        public void HideTable(int registerId)
        {
            byte newStatusTable = (byte)(IsHideTable(registerId) == true ? 0 : 1);

            command = new SqlCommand("UPDATE [dbo].[" + Rules.RegisterTableName + "] SET [" + RegisterTableColumn.Id + "] = '" + newStatusTable + "' WHERE " + RegisterTableColumn.Id + " = " + registerId, connection);
            command.ExecuteNonQuery();
        }

        #endregion

        #region Работа со столбцами

        /// <summary>
        /// Добавляет дополнительный стоблец в таблицу и возвращает id столбца регистра
        /// </summary>
        /// <param name="registerId"> Получает табличный id в регистре </param>
        /// <param name="columnSqlType"> Получает название типа Sql </param>
        /// <param name="newColumnName"> Получает пользовательское имя столбца </param>
        /// <returns> Возвращает id столбца в регистре </returns>
        private int AddColumn(int registerId, string columnSqlType, string newColumnName)
        {
            // Регистрация столбца
            string[] itemNames = GetColumnRegisterValue(registerId);
            int Length = itemNames is null ? 0 : itemNames.Length;
            string[] newItemNames = new string[Length + 1];

            for (int i = 0; i < Length; i++)
            {
                newItemNames[i] = itemNames[i];
            }
            newItemNames[Length] = newColumnName;
            SetColumnRegisterValue(registerId, newItemNames);

            // Добавление столбца

            command = new SqlCommand("ALTER TABLE [dbo].[" + GetTableNameRegister(registerId) + "] ADD " + Rules.ColumnName + Length + DateTime.Now.ToString("fffffff") + " " + columnSqlType + " NULL", connection);
            command.ExecuteNonQuery();

            return Length;
        }
        /// <summary>
        /// Добавляет дополнительный стоблец в таблицу типа string и возвращает id столбца регистра
        /// </summary>
        /// <param name="registerId"> Получает табличный id в регистре </param>
        /// <param name="newColumnName"> Получает пользовательское имя столбца </param>
        /// <returns> Возвращает id столбца в регистре </returns>
        public int AddColumnNvarchar(int registerId, string newColumnName)
        {
            return AddColumn(registerId, "NVARCHAR(50)", newColumnName);
        }
        /// <summary>
        /// Добавляет дополнительный стоблец в таблицу типа int и возвращает id столбца регистра
        /// </summary>
        /// <param name="registerId"> Получает табличный id в регистре </param>
        /// <param name="newColumnName"> Получает пользовательское имя столбца </param>
        /// <returns> Возвращает id столбца в регистре </returns>
        public int AddColumnInt(int registerId, string newColumnName)
        {
            return AddColumn(registerId, "INT", newColumnName);
        }
        /// <summary>
        /// Добавляет дополнительный стоблец в таблицу типа float и возвращает id столбца регистра
        /// </summary>
        /// <param name="registerId"> Получает табличный id в регистре </param>
        /// <param name="newColumnName"> Получает пользовательское имя столбца </param>
        /// <returns> Возвращает id столбца в регистре </returns>
        public int AddColumnReal(int registerId, string newColumnName)
        {
            return AddColumn(registerId, "REAL", newColumnName);
        }
        /// <summary>
        /// Добавляет дополнительный стоблец в таблицу типа bool и возвращает id столбца регистра
        /// </summary>
        /// <param name="registerId"> Получает табличный id в регистре </param>
        /// <param name="newColumnName"> Получает пользовательское имя столбца </param>
        /// <returns> Возвращает id столбца в регистре </returns>
        public int AddColumnBit(int registerId, string newColumnName)
        {
            return AddColumn(registerId, "BIT", newColumnName);
        }

        /// <summary>
        /// Удаляет дополнительный столбец по индексу
        /// </summary>
        /// <param name="registerId"> Принимает табличный id в регистре </param>
        /// <param name="idColumnRetister"> Принимает id столбца в регистре </param>
        public void DelColumn(int registerId, int idColumnRetister)
        {
            // Регистрация столбца
            string[] str = GetColumnRegisterValue(registerId);
            if (str.Length > 1)
            {
                string[] newStr = new string[str.Length - 1];
                for (int i = 0; i < newStr.Length; i++)
                {
                    newStr[i] = str[i < idColumnRetister ? i : i + 1];
                }
                SetColumnRegisterValue(registerId, newStr);
            }
            else SetColumnRegisterValue(registerId);

            // Поиск названия столбца по индексу
            string nameColumn = GetColumnName(registerId, idColumnRetister);

            // Удаление столбца
            command = new SqlCommand("ALTER TABLE [dbo].[" + GetTableNameRegister(registerId) + "] DROP COLUMN " + nameColumn, connection);
            command.ExecuteNonQuery();
        }
        /// <summary>
        /// Удаляет все дополнительные столбецы
        /// </summary>
        /// <param name="registerId"> Принимает табличный id в регистре </param>
        public void DelColumn(int registerId)
        {
            if (GetColumnRegisterValue(registerId) is null) return;

            for (int i = 0, length = GetColumnRegisterLength(registerId); i < length; i++)
            {
                DelColumn(registerId, 0);
            }
        }

        /// <summary>
        /// Возвращает имя дополнительного столбца
        /// </summary>
        /// <param name="registerId"> Принимает табличный id в регистре </param>
        /// <param name="idColumnRetister"> Принимает id столбца в регистре </param>
        /// <returns> Возвращает имя дополнительного столбца </returns>
        public string GetColumnName(int registerId, int idColumnRetister)
        {
            int idColumn = (int)TableColumn.Length + idColumnRetister;
            string nameColumn = "";
            command = new SqlCommand("SELECT column_name FROM information_schema.columns WHERE table_name = '" + GetTableNameRegister(registerId) + "'", connection);
            SqlDataReader reader = command.ExecuteReader();
            for (int i = 0; i <= idColumn && reader.Read(); i++)
            {
                if (idColumn == i) nameColumn = reader.GetString(0);
            }
            reader.Close();

            return nameColumn;
        }
        /// <summary>
        /// Возвращает тип дополнительного столбца
        /// </summary>
        /// <param name="registerId"> Принимает табличный id в регистре </param>
        /// <param name="idColumnRetister"> Принимает id столбца в регистре </param>
        /// <returns> Возвращает тип дополнительного столбца </returns>
        public string GetColumnType(int registerId, int idColumnRetister)
        {
            command = new SqlCommand("SELECT DATA_TYPE FROM information_schema.COLUMNS WHERE TABLE_NAME='" + GetTableNameRegister(registerId) + "' AND COLUMN_NAME='" + GetColumnName(registerId, idColumnRetister) + "'", connection);
            SqlDataReader reader = command.ExecuteReader();
            reader.Read();
            string str = reader.GetString(0);
            reader.Close();
            return str;
        }

        #endregion

        #region Работа со строками

        // get,set,add,del

        /// <summary>
        /// Возвращает коллекцию id строк в таблице
        /// </summary>
        /// <param name="registerId"> id таблицы</param>
        /// <returns></returns>
        public List<int> GetTableStringIds(int registerId)
        {
            command = new SqlCommand("SELECT * FROM " + Rules.RootTableName, connection);
            SqlDataReader reader = command.ExecuteReader();

            List<int> list = new List<int>();

            foreach (var item in reader)
            {
                list.Add(reader.GetInt32(0));
            }

            reader.Close();

            return list;
        }

        #endregion

        #region Работа со значениями

        // Работа со значением ItemNames в Регистре
        /// <summary>
        /// Возвращает колличество имен ItemNames в регистре 
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <returns> Возвращает колличество имен ItemNames </returns>
        public int GetColumnRegisterLength(int idTableRegister)
        {
            string[] array = GetColumnRegisterValue(idTableRegister);
            if (array is null) return 0;
            else return array.Length;
        }
        /// <summary>
        /// Возвращает массив имен ItemNames в регистре 
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <returns> Возвращает массив имен ItemNames </returns>
        public string[] GetColumnRegisterValue(int idTableRegister)
        {
            command = new SqlCommand("SELECT * FROM " + Rules.RegisterTableName + " WHERE " + RegisterTableColumn.Id + " = " + idTableRegister, connection);
            SqlDataReader reader = command.ExecuteReader();

            reader.Read();
            string itemNames = null;
            if (!reader.IsDBNull((int)RegisterTableColumn.ItemNames))
            {
                itemNames = reader.GetString((int)RegisterTableColumn.ItemNames);
            }
            reader.Close();

            if (itemNames is null) return null;
            else
            {
                return ConversionStringStringArray(itemNames);
            }
        }
        /// <summary>
        /// Возвращает Имя ItemName в регистре 
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idColumnRetister"> Принимает id столбца в регистре </param>
        /// <returns> Возвращает строку ItemNames </returns>
        public string GetColumnRegisterValue(int idTableRegister, int idColumnRetister)
        {
            string[] array = GetColumnRegisterValue(idTableRegister);
            if (array is null) return null;
            return array[idColumnRetister];
        }
        /// <summary>
        /// Обнуляет ItemNames в регистре
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        public void SetColumnRegisterValue(int idTableRegister)
        {
            command = new SqlCommand("UPDATE [dbo].[" + Rules.RegisterTableName + "] SET " + RegisterTableColumn.ItemNames + " = NULL WHERE " + RegisterTableColumn.Id + " = " + idTableRegister, connection);
            command.ExecuteNonQuery();
        }
        /// <summary>
        /// Записывает массив имен ItemNames в регистр
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="itemNames"> Принимает массив имен столбцов ItemNames </param>
        public void SetColumnRegisterValue(int idTableRegister, string[] itemNames)
        {
            string str = ConversionStringArrayString(itemNames);

            command = new SqlCommand("UPDATE [dbo].[" + Rules.RegisterTableName + "] SET " + RegisterTableColumn.ItemNames + " = '" + str + "' WHERE " + RegisterTableColumn.Id + " = " + idTableRegister, connection);
            command.ExecuteNonQuery();
        }
        /// <summary>
        /// Записывает имя ItemNames в регистр
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idColumnRetister"> Принимает id столбца в регистре </param>
        /// <param name="itemName"> Принимает строку имя столбца </param>
        public void SetColumnRegisterValue(int idTableRegister, int idColumnRetister, string itemName)
        {
            string[] array = GetColumnRegisterValue(idTableRegister);

            if (array is null) array = new string[idColumnRetister + 1];

            array[idColumnRetister] = itemName;
            SetColumnRegisterValue(idTableRegister, array);
        }

        // Работа со значениями дополнительных стоблцов
        /// <summary>
        /// Возвращает значение string дополнительного столбца
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idColumnRetister"> Принимает id столбца в регистре </param>
        /// <param name="idValue"> Принимиет id строки в таблице </param>
        /// <returns> Возвращает стоку </returns>
        public string GetColumnValueString(int idTableRegister, int idColumnRetister, int idValue)
        {
            command = new SqlCommand("SELECT * FROM " + GetTableNameRegister(idTableRegister) + " WHERE " + TableColumn.Id + " = " + idValue, connection);
            SqlDataReader reader = command.ExecuteReader();

            reader.Read();
            int length = (int)TableColumn.Length + idColumnRetister;
            string itemValue = null;
            if (!reader.IsDBNull(length))
            {
                itemValue = reader.GetString(length);
            }
            reader.Close();

            return itemValue;
        }
        /// <summary>
        /// Возвращает значение int дополнительного столбца
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idColumnRetister"> Принимает id столбца в регистре </param>
        /// <param name="idValue"> Принимиет id строки в таблице </param>
        /// <returns> Возвращает стоку </returns>
        public int? GetColumnValueInt(int idTableRegister, int idColumnRetister, int idValue)
        {
            command = new SqlCommand("SELECT * FROM " + GetTableNameRegister(idTableRegister) + " WHERE " + TableColumn.Id + " = " + idValue, connection);
            SqlDataReader reader = command.ExecuteReader();

            reader.Read();
            int length = (int)TableColumn.Length + idColumnRetister;
            int? itemValue = null;
            if (!reader.IsDBNull(length))
            {
                itemValue = reader.GetInt32(length);
            }
            reader.Close();

            return itemValue;
        }
        /// <summary>
        /// Возвращает значение float дополнительного столбца
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idColumnRetister"> Принимает id столбца в регистре </param>
        /// <param name="idValue"> Принимиет id строки в таблице </param>
        /// <returns> Возвращает стоку </returns>
        public float? GetColumnValueFloat(int idTableRegister, int idColumnRetister, int idValue)
        {
            command = new SqlCommand("SELECT * FROM " + GetTableNameRegister(idTableRegister) + " WHERE " + TableColumn.Id + " = " + idValue, connection);
            SqlDataReader reader = command.ExecuteReader();

            reader.Read();
            int length = (int)TableColumn.Length + idColumnRetister;
            float? itemValue = null;
            if (!reader.IsDBNull(length))
            {
                itemValue = reader.GetFloat(length);
            }
            reader.Close();

            return itemValue;
        }
        /// <summary>
        /// Возвращает значение Bool дополнительного столбца
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idColumnRetister"> Принимает id столбца в регистре </param>
        /// <param name="idValue"> Принимиет id строки в таблице </param>
        /// <returns> Возвращает стоку </returns>
        public bool? GetColumnValueBool(int idTableRegister, int idColumnRetister, int idValue)
        {
            command = new SqlCommand("SELECT * FROM " + GetTableNameRegister(idTableRegister) + " WHERE " + TableColumn.Id + " = " + idValue, connection);
            SqlDataReader reader = command.ExecuteReader();

            reader.Read();
            int length = (int)TableColumn.Length + idColumnRetister;
            bool? itemValue = null;
            if (!reader.IsDBNull(length))
            {
                itemValue = reader.GetBoolean(length);
            }
            reader.Close();

            return itemValue;
        }
        /// <summary>
        /// Устанавливает значение string дополнительного столбца
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idColumnRetister"> Принимает id столбца в регистре </param>
        /// <param name="idValue"> Принимиет id строки в таблице </param>
        /// <param name="value"> Значение </param>
        public void SetColumnValueString(int idTableRegister, int idColumnRetister, int idValue, string value)
        {
            command = new SqlCommand("UPDATE [dbo].[" + GetTableNameRegister(idTableRegister) + "] SET " + GetColumnName(idTableRegister, idColumnRetister) + " = '" + value + "' WHERE " + TableColumn.Id + " = " + idValue, connection);
            command.ExecuteNonQuery();
        }
        /// <summary>
        /// Обнуляет значение string дополнительного столбца
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idColumnRetister"> Принимает id столбца в регистре </param>
        /// <param name="idValue"> Принимиет id строки в таблице </param>
        public void SetColumnValueString(int idTableRegister, int idColumnRetister, int idValue)
        {
            command = new SqlCommand("UPDATE [dbo].[" + GetTableNameRegister(idTableRegister) + "] SET " + GetColumnName(idTableRegister, idColumnRetister) + " = NULL WHERE " + TableColumn.Id + " = " + idValue, connection);
            command.ExecuteNonQuery();
        }
        /// <summary>
        /// Устанавливает значение int дополнительного столбца
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idColumnRetister"> Принимает id столбца в регистре </param>
        /// <param name="idValue"> Принимиет id строки в таблице </param>
        /// <param name="value"> Значение </param>
        public void SetColumnValueInt(int idTableRegister, int idColumnRetister, int idValue, int value)
        {
            command = new SqlCommand("UPDATE [dbo].[" + GetTableNameRegister(idTableRegister) + "] SET " + GetColumnName(idTableRegister, idColumnRetister) + " = '" + value + "' WHERE " + TableColumn.Id + " = " + idValue, connection);
            command.ExecuteNonQuery();
        }
        /// <summary>
        /// Обнуляет значение int дополнительного столбца
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idColumnRetister"> Принимает id столбца в регистре </param>
        /// <param name="idValue"> Принимиет id строки в таблице </param>
        public void SetColumnValueInt(int idTableRegister, int idColumnRetister, int idValue)
        {
            command = new SqlCommand("UPDATE [dbo].[" + GetTableNameRegister(idTableRegister) + "] SET " + GetColumnName(idTableRegister, idColumnRetister) + " = NULL WHERE " + TableColumn.Id + " = " + idValue, connection);
            command.ExecuteNonQuery();
        }
        /// <summary>
        /// Устанавливает значение float дополнительного столбца
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idColumnRetister"> Принимает id столбца в регистре </param>
        /// <param name="idValue"> Принимиет id строки в таблице </param>
        /// <param name="value"> Значение </param>
        public void SetColumnValueFloat(int idTableRegister, int idColumnRetister, int idValue, float value)
        {
            command = new SqlCommand("UPDATE [dbo].[" + GetTableNameRegister(idTableRegister) + "] SET " + GetColumnName(idTableRegister, idColumnRetister) + " = REPLACE('" + value + "', ',', '.') WHERE " + TableColumn.Id + " = " + idValue, connection);
            command.ExecuteNonQuery();
        }
        /// <summary>
        /// Обнуляет значение float дополнительного столбца
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idColumnRetister"> Принимает id столбца в регистре </param>
        /// <param name="idValue"> Принимиет id строки в таблице </param>
        public void SetColumnValueFloat(int idTableRegister, int idColumnRetister, int idValue)
        {
            command = new SqlCommand("UPDATE [dbo].[" + GetTableNameRegister(idTableRegister) + "] SET " + GetColumnName(idTableRegister, idColumnRetister) + " = NULL WHERE " + TableColumn.Id + " = " + idValue, connection);
            command.ExecuteNonQuery();
        }
        /// <summary>
        /// Устанавливает значение bool дополнительного столбца
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idColumnRetister"> Принимает id столбца в регистре </param>
        /// <param name="idValue"> Принимиет id строки в таблице </param>
        /// <param name="value"> Значение </param>
        public void SetColumnValueBool(int idTableRegister, int idColumnRetister, int idValue, bool value)
        {
            command = new SqlCommand("UPDATE [dbo].[" + GetTableNameRegister(idTableRegister) + "] SET " + GetColumnName(idTableRegister, idColumnRetister) + " = '" + value + "' WHERE " + TableColumn.Id + " = " + idValue, connection);
            command.ExecuteNonQuery();
        }
        /// <summary>
        /// Обнуляет значение bool дополнительного столбца
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idColumnRetister"> Принимает id столбца в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        public void SetColumnValueBool(int idTableRegister, int idColumnRetister, int idValue)
        {
            command = new SqlCommand("UPDATE [dbo].[" + GetTableNameRegister(idTableRegister) + "] SET " + GetColumnName(idTableRegister, idColumnRetister) + " = NULL WHERE " + TableColumn.Id + " = " + idValue, connection);
            command.ExecuteNonQuery();
        }

        ////// Работа с родственностью строк в БД (предки, потомки)
        // Работа со значениями PreRegisterId
        /// <summary>
        /// Возвращает id родительской таблицы
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        /// <returns> Возвращает null если родительсткая таблица отсутствует </returns>
        public int? GetPreRegisterId(int idTableRegister, int idValue)
        {
            command = new SqlCommand("SELECT * FROM " + GetTableNameRegister(idTableRegister) + " WHERE " + TableColumn.Id + " = " + idValue, connection);
            SqlDataReader reader = command.ExecuteReader();
            reader.Read();

            object value = reader.GetValue((int)TableColumn.PreRegisterId);

            reader.Close();

            return value as int?;
        }
        /// <summary>
        /// Устанавливает id родительской таблицы
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        /// <param name="value"> Принимает id родительской таблицы в регистре </param>
        private void SetPreRegisterId(int idTableRegister, int idValue, int value)
        {
            command = new SqlCommand("UPDATE [dbo].[" + GetTableNameRegister(idTableRegister) + "] SET " + TableColumn.PreRegisterId + " = '" + value + "' WHERE " + TableColumn.Id + " = " + idValue, connection);
            command.ExecuteNonQuery();
        }
        /// <summary>
        /// Обнуляет id родительской таблицы
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        private void SetPreRegisterId(int idTableRegister, int idValue)
        {
            command = new SqlCommand("UPDATE [dbo].[" + GetTableNameRegister(idTableRegister) + "] SET " + TableColumn.PreRegisterId + " = NULL WHERE " + TableColumn.Id + " = " + idValue, connection);
            command.ExecuteNonQuery();
        }
        // Работа со значениями PreTalbeId
        /// <summary>
        /// Возвращает id родительской строки
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        /// <returns> Возвращает null если родительсткая строка отсутствует </returns>
        private int? GetPreTalbeId(int idTableRegister, int idValue)
        {
            command = new SqlCommand("SELECT * FROM " + GetTableNameRegister(idTableRegister) + " WHERE " + TableColumn.Id + " = " + idValue, connection);
            SqlDataReader reader = command.ExecuteReader();
            reader.Read();

            object value = reader.GetValue((int)TableColumn.PreTableId);

            reader.Close();

            return value as int?;
        }
        /// <summary>
        /// Устанавливает id родительской строки
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        /// <param name="value"> Принимает id родительской таблицы в регистре </param>
        private void SetPreTalbeId(int idTableRegister, int idValue, int value)
        {
            command = new SqlCommand("UPDATE [dbo].[" + GetTableNameRegister(idTableRegister) + "] SET " + TableColumn.PreTableId + " = '" + value + "' WHERE " + TableColumn.Id + " = " + idValue, connection);
            command.ExecuteNonQuery();
        }
        /// <summary>
        /// Обнуляет id родительской строки
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        private void SetPreTalbeId(int idTableRegister, int idValue)
        {
            command = new SqlCommand("UPDATE [dbo].[" + GetTableNameRegister(idTableRegister) + "] SET " + TableColumn.PreTableId + " = NULL WHERE " + TableColumn.Id + " = " + idValue, connection);
            command.ExecuteNonQuery();
        }
        // Работа со значениями PostRegisterId
        /// <summary>
        /// Возвращает массив id дочерних таблиц
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        /// <returns> Возвращает null если дочернии таблицы отсутствуют </returns>
        public int[] GetPostRegisterId(int idTableRegister, int idValue)
        {
            command = new SqlCommand("SELECT * FROM " + GetTableNameRegister(idTableRegister) + " WHERE " + TableColumn.Id + " = " + idValue, connection);
            SqlDataReader reader = command.ExecuteReader();
            reader.Read();
            string values = null;
            if (!reader.IsDBNull((int)TableColumn.PostRegisterId))
            {
                values = reader.GetString((int)TableColumn.PostRegisterId);
            }
            reader.Close();

            if (values is null) return null;

            return ConversionStringIntArray(values);
        }
        /// <summary>
        /// Возвращает id дочерней таблицы
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        /// <param name="idPostValue"> Принимает порядковый номер дочерней таблицы </param>
        /// <returns> Возвращает id дочерней таблицы </returns>
        public int? GetPostRegisterId(int idTableRegister, int idValue, int idPostValue)
        {
            int[] values = GetPostRegisterId(idTableRegister, idValue);

            if (values is null) return null;
            else if (values.Length < idPostValue) return values[idPostValue];

            return null;
        }
        /// <summary>
        /// Обнуляет id дочерней таблицы
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        private void SetPostRegisterId(int idTableRegister, int idValue)
        {
            command = new SqlCommand("UPDATE [dbo].[" + GetTableNameRegister(idTableRegister) + "] SET " + TableColumn.PostRegisterId + " = NULL WHERE " + TableColumn.Id + " = " + idValue, connection);
            command.ExecuteNonQuery();
        }
        /// <summary>
        /// Устанавливает id дочерней таблицы
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        /// <param name="postRegisterId"> Принимает массив id дочерних строк </param>
        private void SetPostRegisterId(int idTableRegister, int idValue, int[] postRegisterId)
        {
            string value = ConversionIntArrayString(postRegisterId);

            command = new SqlCommand("UPDATE [dbo].[" + GetTableNameRegister(idTableRegister) + "] SET " + TableColumn.PostRegisterId + " = '" + value + "' WHERE " + TableColumn.Id + " = " + idValue, connection);
            command.ExecuteNonQuery();
        }
        /// <summary>
        /// Удаляет id дочерней таблицы
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        /// <param name="idPostValue"> Принимает порядковый номер дочерней строки </param>
        private void SetPostRegisterId(int idTableRegister, int idValue, int idPostValue)
        {
            int[] values = GetPostRegisterId(idTableRegister, idValue);

            if (values is null) return;

            int[] values1 = values;
            values = new int[values1.Length - 1];

            for (int i = 0; i < values.Length; i++)
            {
                if (i < idPostValue) values[i] = values1[i];
                else values[i] = values1[i + 1];
            }

            if (values.Length == 0) SetPostRegisterId(idTableRegister, idValue);
            else SetPostRegisterId(idTableRegister, idValue, values);
        }
        /// <summary>
        /// Устанавливает id дочерней таблицы
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        /// <param name="idPostValue"> Принимает порядковый номер дочерней строки </param>
        /// <param name="postRegisterId"> Принимает id дочерней строки </param>
        private void SetPostRegisterId(int idTableRegister, int idValue, int idPostValue, int postRegisterId)
        {
            int[] values = GetPostRegisterId(idTableRegister, idValue);

            if (values is null) values = new int[idPostValue + 1];
            else if (values.Length == idPostValue)
            {
                int[] values1 = values;
                values = new int[idPostValue + 1];

                for (int i = 0; i < values1.Length; i++)
                {
                    values[i] = values1[i];
                }
            }

            values[idPostValue] = postRegisterId;

            SetPostRegisterId(idTableRegister, idValue, values);
        }
        // Работа со значениями PostTalbeId
        /// <summary>
        /// Возвращает массив id дочерних строк
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        /// <returns> Возвращает null если дочернии таблицы отсутствуют </returns>
        public int[] GetPostTalbeId(int idTableRegister, int idValue)
        {
            command = new SqlCommand("SELECT * FROM " + GetTableNameRegister(idTableRegister) + " WHERE " + TableColumn.Id + " = " + idValue, connection);
            SqlDataReader reader = command.ExecuteReader();
            reader.Read();
            string values = null;
            if (!reader.IsDBNull((int)TableColumn.PostTableId))
            {
                values = reader.GetString((int)TableColumn.PostTableId);
            }
            reader.Close();

            if (values is null) return null;

            return ConversionStringIntArray(values);
        }
        /// <summary>
        /// Возвращает id дочерней строки
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        /// <param name="idPostValue"> Принимает порядковый номер дочерней таблицы </param>
        /// <returns> Возвращает id дочерней таблицы </returns>
        public int? GetPostTalbeId(int idTableRegister, int idValue, int idPostValue)
        {
            int[] values = GetPostTalbeId(idTableRegister, idValue);

            if (values is null) return null;
            else if (values.Length < idPostValue) return values[idPostValue];

            return null;
        }
        /// <summary>
        /// Обнуляет id дочерней строки
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        private void SetPostTalbeId(int idTableRegister, int idValue)
        {
            command = new SqlCommand("UPDATE [dbo].[" + GetTableNameRegister(idTableRegister) + "] SET " + TableColumn.PostTableId + " = NULL WHERE " + TableColumn.Id + " = " + idValue, connection);
            command.ExecuteNonQuery();
        }
        /// <summary>
        /// Устанавливает id дочерней строки
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        /// <param name="postTableId"> Принимает массив id дочерних строк </param>
        private void SetPostTalbeId(int idTableRegister, int idValue, int[] postTableId)
        {
            string value = ConversionIntArrayString(postTableId);

            command = new SqlCommand("UPDATE [dbo].[" + GetTableNameRegister(idTableRegister) + "] SET " + TableColumn.PostTableId + " = '" + value + "' WHERE " + TableColumn.Id + " = " + idValue, connection);
            command.ExecuteNonQuery();
        }
        /// <summary>
        /// Удаляет id дочерней строки
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        /// <param name="idPostValue"> Принимает порядковый номер дочерней строки </param>
        private void SetPostTalbeId(int idTableRegister, int idValue, int idPostValue)
        {
            int[] values = GetPostTalbeId(idTableRegister, idValue);

            if (values is null) return;

            int[] values1 = values;
            values = new int[values1.Length - 1];

            for (int i = 0; i < values.Length; i++)
            {
                if (i < idPostValue) values[i] = values1[i];
                else values[i] = values1[i + 1];
            }

            if (values.Length == 0) SetPostTalbeId(idTableRegister, idValue);
            else SetPostTalbeId(idTableRegister, idValue, values);
        }
        /// <summary>
        /// Устанавливает id дочерней строки
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        /// <param name="idPostValue"> Принимает порядковый номер дочерней строки </param>
        /// <param name="postTableId"> Принимает id дочерней строки </param>
        private void SetPostTalbeId(int idTableRegister, int idValue, int idPostValue, int postTableId)
        {
            int[] values = GetPostTalbeId(idTableRegister, idValue);

            if (values is null) values = new int[idPostValue + 1];
            else if (values.Length == idPostValue)
            {
                int[] values1 = values;
                values = new int[idPostValue + 1];

                for (int i = 0; i < values1.Length; i++)
                {
                    values[i] = values1[i];
                }
            }

            values[idPostValue] = postTableId;

            SetPostTalbeId(idTableRegister, idValue, values);
        }

        // Работа с зависимостями строк
        /// <summary>
        /// Добавляет зависимость дочерней строки от строки предка
        /// </summary>
        /// <param name="preRegisterId"> Принимает id таблицы предка </param>
        /// <param name="preTableId"> Принимает id строки предка </param>
        /// <param name="postRegisterId"> Принимает id дочерней таблицы </param>
        /// <param name="postTableId"> Принимает id дочерней таблицы </param>
        public void AddDependence(int preRegisterId, int preTableId, int postRegisterId, int postTableId)
        {
            command = new SqlCommand("SELECT * FROM " + GetTableNameRegister(preRegisterId) + " WHERE " + TableColumn.Id + " = " + preTableId, connection);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();
            command = new SqlCommand("SELECT * FROM " + GetTableNameRegister(postRegisterId) + " WHERE " + TableColumn.Id + " = " + postTableId, connection);
            reader = command.ExecuteReader();
            reader.Close();

            int[] temp = GetPostRegisterId(preRegisterId, preTableId);
            SetPostRegisterId(preRegisterId, preTableId, (temp is null ? 0 : temp.Length), postRegisterId);

            temp = GetPostTalbeId(preRegisterId, preTableId);
            SetPostTalbeId(preRegisterId, preTableId, (temp is null ? 0 : temp.Length), postTableId);

            SetPreRegisterId(postRegisterId, postTableId, preRegisterId);

            SetPreTalbeId(postRegisterId, postTableId, preTableId);
        }
        /// <summary>
        /// Удаляет зависимость дочерней строки от строки предка
        /// </summary>
        /// <param name="preRegisterId"> Принимает id таблицы предка </param>
        /// <param name="preTableId"> Принимает id строки предка </param>
        /// <param name="postRegisterId"> Принимает id дочерней таблицы </param>
        /// <param name="postTableId"> Принимает id дочерней таблицы </param>
        public void DelDependence(int preRegisterId, int preTableId, int postRegisterId, int postTableId)
        {
            int[] postRegisterIdArray = GetPostRegisterId(preRegisterId, preTableId);
            int[] postTableIdArray = GetPostTalbeId(preRegisterId, preTableId);
            int postPosition = 0;
            for (int i = 0; i < postRegisterIdArray.Length; i++)
            {
                if (postRegisterIdArray[i] == postRegisterId)
                    if (postTableIdArray[i] == postTableId)
                    {
                        postPosition = i;
                        break;
                    }
                if (i == postRegisterIdArray.Length - 1) throw new Exception("Нет такой зависимости");
            }

            SetPostRegisterId(preRegisterId, preTableId, postPosition);

            SetPostTalbeId(preRegisterId, preTableId, postPosition);

            SetPreRegisterId(postRegisterId, postTableId);

            SetPreTalbeId(postRegisterId, postTableId);
        }

        // Работа с пользовательским именем строки в Регистре
        /// <summary>
        /// Возвращает пользовательское имя строки
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        /// <returns> Возвращает строку </returns>
        public string GetName(int idTableRegister, int idValue)
        {
            command = new SqlCommand("SELECT * FROM " + GetTableNameRegister(idTableRegister) + " WHERE " + TableColumn.Id + " = " + idValue, connection);
            SqlDataReader reader = command.ExecuteReader();
            reader.Read();

            string value = reader.GetString((int)TableColumn.Name);

            reader.Close();

            return value;
        }
        /// <summary>
        /// Устанавливает пользовательское имя строки
        /// </summary>
        /// <param name="idTableRegister"> Принимает id таблицы в регистре </param>
        /// <param name="idValue"> Принимает id строки в таблице </param>
        /// <param name="value"> Принимает пользовательское имя строки </param>
        public void SetName(int idTableRegister, int idValue, string value)
        {
            command = new SqlCommand("UPDATE [dbo].[" + GetTableNameRegister(idTableRegister) + "] SET " + TableColumn.Name + " = '" + value + "' WHERE " + TableColumn.Id + " = " + idValue, connection);
            command.ExecuteNonQuery();
        }

        #endregion

        #region Методы конвертации с разделителем ','
        private int[] ConversionStringIntArray(string stringInt)
        {
            return stringInt.Split(',').Select(s => Convert.ToInt32(s)).ToArray();
        }
        private string ConversionIntArrayString(int[] intString)
        {
            string s = "";
            for (int i = 0; i < intString.Length; i++)
            {
                s += intString[i];
                if (i < (intString.Length - 1)) s += ',';
            }
            return s;
        }
        private string[] ConversionStringStringArray(string sString)
        {
            return sString.Split(',').Select(s => Convert.ToString(s)).ToArray();
        }
        private string ConversionStringArrayString(string[] sString)
        {
            string s = "";
            for (int i = 0; i < sString.Length; i++)
            {
                s += sString[i];
                if (i < (sString.Length - 1)) s += ',';
            }
            return s;
        }
        #endregion
    }
}
