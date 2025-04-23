using System;
using System.Data.SqlClient;
using System.Windows;
using VrachDubRosh;

namespace VrachDubRosh
{
    public partial class MainWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private bool isDarkTheme = false;

        public MainWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;
            
            // Проверяем текущую тему при запуске
            ResourceDictionary currentDict = Application.Current.Resources.MergedDictionaries[0];
            if (currentDict.Source.ToString().Contains("DarkTheme"))
            {
                isDarkTheme = true;
                themeToggle.IsChecked = true;
                this.Title = "Врач ДубРощ - Авторизация (Темная тема)";
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Пожалуйста, введите логин и пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // Проверка в таблице ChiefDoctors
                    string queryChief = "SELECT COUNT(*) FROM ChiefDoctors WHERE FullName = @login AND Password = @password";
                    using (SqlCommand cmd = new SqlCommand(queryChief, con))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", password);
                        int count = (int)cmd.ExecuteScalar();
                        if (count > 0)
                        {
                            // Если найден главный врач, открываем окно главврача
                            GlavDoctorWindow glavDoctorWindow = new GlavDoctorWindow();
                            glavDoctorWindow.Show();
                            this.Close();
                            return;
                        }
                    }

                    // Проверка в таблице Managers
                    string queryManager = "SELECT COUNT(*) FROM Managers WHERE FullName = @login AND Password = @password";
                    using (SqlCommand cmd = new SqlCommand(queryManager, con))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", password);
                        int count = (int)cmd.ExecuteScalar();
                        if (count > 0)
                        {
                            // Если найден менеджер, открываем окно менеджера
                            ManagerWindow managerWindow = new ManagerWindow();
                            managerWindow.Show();
                            this.Close();
                            return;
                        }
                    }

                    // Проверка в таблице Doctors — получаем DoctorID
                    string queryDoctor = "SELECT DoctorID FROM Doctors WHERE FullName = @login AND Password = @password";
                    using (SqlCommand cmd = new SqlCommand(queryDoctor, con))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", password);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            int doctorID = Convert.ToInt32(result);
                            // Если найден врач, открываем окно врача с параметром doctorID
                            DoctorWindow doctorWindow = new DoctorWindow(doctorID);
                            doctorWindow.Show();
                            this.Close();
                            return;
                        }
                    }

                    // Если ни одна из проверок не прошла
                    MessageBox.Show("Неверный логин или пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при подключении к базе данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ThemeToggle_Checked(object sender, RoutedEventArgs e)
        {
            ChangeTheme(true);
        }

        private void ThemeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ChangeTheme(false);
        }

        private void ChangeTheme(bool isDark)
        {
            isDarkTheme = isDark;
            // Получаем доступ к ресурсам приложения
            ResourceDictionary resourceDict = new ResourceDictionary();
            
            // Меняем источник ресурсов в зависимости от выбранной темы
            if (isDark)
            {
                resourceDict.Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative);
                this.Title = "Врач ДубРощ - Авторизация (Темная тема)";
            }
            else
            {
                resourceDict.Source = new Uri("/Themes/LightTheme.xaml", UriKind.Relative);
                this.Title = "Врач ДубРощ - Авторизация";
            }

            // Заменяем текущие ресурсы приложения на новые
            var appResources = Application.Current.Resources.MergedDictionaries;
            if (appResources.Count > 0)
            {
                appResources[0] = resourceDict;
            }
            else
            {
                appResources.Add(resourceDict);
            }
        }
    }
}