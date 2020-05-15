using System;
using System.Windows.Forms;

namespace TimeManagement
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            #region Подключение к БД
            var dataBase = new DbSql();
            #endregion

            //

            //dataBase.AddDependence(1,2,2,6);

            ConsoleOut(dataBase);


            //

            #region Завершение связи с БД
            dataBase.Close();
            #endregion
        }

        void ConsoleOut(DbSql dataBase, int index = 0)
        {
            var list = dataBase.GetTableStringIds(1);

            for (int i = 0; i < list.Count; i++)
            {
                listBox1.Items.Add(new string(' ', index) + dataBase.GetName(1, list[i]));

                if (!(dataBase.GetPostRegisterId(1, list[i]) is null))
                    if (dataBase.GetPostRegisterId(1, list[i]).Length > 0)
                    {
                        ConsoleOut(dataBase, 1, list[i], index+1);
                    }
            }
        }
        void ConsoleOut(DbSql dataBase, int RegisterId, int TableId, int index = 0)
        {
            int[] PostRegisterIds = dataBase.GetPostRegisterId(RegisterId, TableId);
            int[] PostPostTalbeIds = dataBase.GetPostTalbeId(RegisterId, TableId);

            for (int i = 0; i < PostRegisterIds.Length; i++)
            {
                listBox1.Items.Add(new string(' ', index) + dataBase.GetName(PostRegisterIds[i], PostPostTalbeIds[i]));

                if (!(dataBase.GetPostRegisterId(PostRegisterIds[i], PostPostTalbeIds[i]) is null))
                    if (dataBase.GetPostRegisterId(PostRegisterIds[i], PostPostTalbeIds[i]).Length > 0)
                    {
                        ConsoleOut(dataBase, PostRegisterIds[i], PostPostTalbeIds[i], index+1);
                    }
            }


        }
    }
}
