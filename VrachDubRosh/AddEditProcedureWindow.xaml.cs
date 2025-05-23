﻿using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;

namespace VrachDubRosh
{
    public partial class AddEditProcedureWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int _doctorID;
        private int? _procedureID = null;

        // Конструктор для добавления
        public AddEditProcedureWindow(int doctorID)
        {
            InitializeComponent();
            _doctorID = doctorID;
            this.Title = "Добавить процедуру";
            this.Loaded += AddEditProcedureWindow_Loaded;
        }

        // Конструктор для редактирования
        public AddEditProcedureWindow(int doctorID, int procedureID) : this(doctorID)
        {
            _procedureID = procedureID;
            this.Title = "Редактировать процедуру";
            LoadProcedureData();
        }

        private void AddEditProcedureWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Проверяем тему родительского окна и применяем её
            if (this.Owner != null)
            {
                if (this.Owner is GlavDoctorWindow glavWindow && glavWindow.isDarkTheme)
                {
                    ApplyDarkTheme();
                }
                else if (this.Owner is DoctorWindow doctorWindow && doctorWindow.isDarkTheme)
                {
                    ApplyDarkTheme();
                }
            }
        }

        private void ApplyDarkTheme()
        {
            // Применяем темную тему
            ResourceDictionary resourceDict = new ResourceDictionary();
            resourceDict.Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative);
            Application.Current.Resources.MergedDictionaries[0] = resourceDict;
        }

        private void LoadProcedureData()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT ProcedureName, Duration FROM Procedures WHERE ProcedureID = @ProcedureID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ProcedureID", _procedureID.Value);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtProcedureName.Text = reader["ProcedureName"].ToString();
                                txtDuration.Text = reader["Duration"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных процедуры: " + ex.Message);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProcedureName.Text) || string.IsNullOrWhiteSpace(txtDuration.Text))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            // Проверка: наименование должно содержать только буквы (и пробелы)
            string procedureName = txtProcedureName.Text.Trim();
            if (!Regex.IsMatch(procedureName, @"^[A-Za-zА-Яа-яЁё\s]+$"))
            {
                MessageBox.Show("Наименование процедуры должно содержать только буквы.");
                return;
            }

            if (!int.TryParse(txtDuration.Text.Trim(), out int duration))
            {
                MessageBox.Show("Длительность должна быть числом (в минутах).");
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // Проверка наличия процедуры с таким же наименованием
                    string checkQuery = "SELECT TOP 1 Duration FROM Procedures WHERE ProcedureName = @ProcedureName";
                    if (_procedureID != null)
                    {
                        checkQuery += " AND ProcedureID <> @ProcedureID";
                    }
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@ProcedureName", procedureName);
                        if (_procedureID != null)
                        {
                            checkCmd.Parameters.AddWithValue("@ProcedureID", _procedureID.Value);
                        }
                        object result = checkCmd.ExecuteScalar();
                        if (result != null)
                        {
                            int existingDuration = Convert.ToInt32(result);
                            if (existingDuration == duration)
                            {
                                MessageBox.Show("Идентичная процедура уже имеется");
                                return;
                            }
                            else
                            {
                                MessageBox.Show("Процедура с данным названием уже имеется, пожалуйста переименуйте процедуру.");
                                return;
                            }
                        }
                    }

                    // Если проверки пройдены, производим вставку или обновление процедуры
                    if (_procedureID == null)
                    {
                        // Добавление процедуры
                        string insertQuery = @"INSERT INTO Procedures (ProcedureName, Duration, DoctorID)
                                       VALUES (@ProcedureName, @Duration, @DoctorID)";
                        using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@ProcedureName", procedureName);
                            cmd.Parameters.AddWithValue("@Duration", duration);
                            cmd.Parameters.AddWithValue("@DoctorID", _doctorID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Редактирование процедуры
                        string updateQuery = @"UPDATE Procedures 
                                       SET ProcedureName = @ProcedureName, Duration = @Duration
                                       WHERE ProcedureID = @ProcedureID";
                        using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@ProcedureName", procedureName);
                            cmd.Parameters.AddWithValue("@Duration", duration);
                            cmd.Parameters.AddWithValue("@ProcedureID", _procedureID.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения данных процедуры: " + ex.Message);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
