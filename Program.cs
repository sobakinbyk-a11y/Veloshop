using System;
using System.Windows.Forms;

namespace VeloShop
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Инициализация базы данных SQLite перед запуском UI
            try
            {
                Database.Init();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации базы данных:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new MainForm());
        }
    }
}
