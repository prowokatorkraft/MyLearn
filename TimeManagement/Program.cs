using System;
using System.Windows.Forms;

namespace TimeManagement
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }


    static class Rules
    {
        /// <summary> Выводит название корневой таблицы БД. </summary>
        public static string RegisterTableName { get; } = "Register";
        /// <summary> Выводит название корневой таблицы БД. </summary>
        public static string RootTableName { get; } = "Root";
        /// <summary> Выводит название шаблонной таблицы БД. </summary>
        public static string TemplateTableName { get; } = "Template";
        /// <summary> Выводит название дополнительных столбцов таблицы БД. </summary>
        public static string ColumnName { get; } = "Column";
        /// <summary> Выводит массив запрещенных табличных именов. </summary>
        public static string[] ForbiddenTableNames { get; } = { Rules.RegisterTableName, Rules.RootTableName };
        /// <summary> Выводит название БД </summary>
        public static string DataBaseName { get; } = "DatabaseTM";

        /// <summary> Проверяет строку на запрещенное табличное имя и возвращает логическое значение. </summary>
        /// <param name="name"> Табличное имя </param> <returns> Возвращает false если имя запрещенное </returns>
        public static bool CheeckForbiddenTableName(string name)
        {
            for (int iFTN = 0; iFTN < Rules.ForbiddenTableNames.Length; iFTN++)
                if (name == Rules.ForbiddenTableNames[iFTN]) return false;

            return true;
        }
        /// <summary> Проверяет массив строк на запрещенное табличное имя и возвращает колличество ошибок. </summary>
        /// <param name="name"> Массив табличных имен </param> <returns> Возвращает колличество ошибок. </returns>
        public static int CheeckForbiddenTableNames(string[] names)
        {
            int numberErrors = 0;

            for (int i = 0; i < names.Length; i++)
                if (!CheeckForbiddenTableName(names[i]))
                    numberErrors++;

            return numberErrors;
        }
    }
}