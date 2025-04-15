using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VrachDubRosh
{
    // Вспомогательный класс для привязки данных в ListView
    public class DoctorAssignment
    {
        public int DoctorID { get; set; }
        public string FullName { get; set; }
        public bool IsAssigned { get; set; }
    }

    public partial class PatientAssignmentWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int patientID;
        public bool isDarkTheme { get; private set; } = false;

        public PatientAssignmentWindow(int patientID, string patientName)
        {
            InitializeComponent();
            this.patientID = patientID;
            txtPatientInfo.Text = $"Пациент: {patientName} (ID: {patientID})";
            this.Loaded += PatientAssignmentWindow_Loaded;
        }

        // Обработчик клика по элементу списка
        private void ListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item && item.DataContext is DoctorAssignment assignment)
            {
                // Инвертируем состояние IsAssigned
                assignment.IsAssigned = !assignment.IsAssigned;
                
                // Обновляем представление
                if (lvDoctors.ItemsSource != null)
                {
                    lvDoctors.Items.Refresh();
                }
                
                // Помечаем событие как обработанное
                e.Handled = true;
            }
        }

        private void PatientAssignmentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDoctorsWithAssignment();
            
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
            isDarkTheme = true;
            ResourceDictionary resourceDict = new ResourceDictionary();
            resourceDict.Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative);
            Application.Current.Resources.MergedDictionaries[0] = resourceDict;
        }

        /// <summary>
        /// Загружаем список всех врачей и отмечаем тех, кто уже назначен пациенту.
        /// </summary>
        private void LoadDoctorsWithAssignment()
        {
            var list = new List<DoctorAssignment>();

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // 1. Получаем список всех врачей
                    string queryAllDocs = "SELECT DoctorID, FullName FROM Doctors";
                    SqlDataAdapter da = new SqlDataAdapter(queryAllDocs, con);
                    DataTable dtAll = new DataTable();
                    da.Fill(dtAll);

                    // 2. Получаем список назначенных врачу ID
                    string queryAssigned = "SELECT DoctorID FROM PatientDoctorAssignments WHERE PatientID = @PatientID";
                    SqlCommand cmd = new SqlCommand(queryAssigned, con);
                    cmd.Parameters.AddWithValue("@PatientID", patientID);
                    SqlDataAdapter da2 = new SqlDataAdapter(cmd);
                    DataTable dtAssigned = new DataTable();
                    da2.Fill(dtAssigned);

                    // Преобразуем dtAssigned в набор doctorIDs
                    var assignedSet = new HashSet<int>();
                    foreach (DataRow row in dtAssigned.Rows)
                    {
                        assignedSet.Add(Convert.ToInt32(row["DoctorID"]));
                    }

                    // 3. Формируем список DoctorAssignment
                    foreach (DataRow row in dtAll.Rows)
                    {
                        int docID = Convert.ToInt32(row["DoctorID"]);
                        string fullName = row["FullName"].ToString();

                        var item = new DoctorAssignment
                        {
                            DoctorID = docID,
                            FullName = fullName,
                            IsAssigned = assignedSet.Contains(docID)  // уже назначен?
                        };
                        list.Add(item);
                    }
                }

                lvDoctors.ItemsSource = list;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки списка врачей: " + ex.Message);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Собираем список врачей, у которых IsAssigned = true
                var toAssign = new List<int>();
                foreach (DoctorAssignment da in lvDoctors.Items)
                {
                    if (da.IsAssigned)
                    {
                        toAssign.Add(da.DoctorID);
                    }
                }

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            // Удаляем все предыдущие назначения
                            string deleteQuery = "DELETE FROM PatientDoctorAssignments WHERE PatientID = @PatientID";
                            using (SqlCommand cmdDelete = new SqlCommand(deleteQuery, con, tran))
                            {
                                cmdDelete.Parameters.AddWithValue("@PatientID", patientID);
                                cmdDelete.ExecuteNonQuery();
                            }

                            // Вставляем новые (отмеченные) назначения
                            string insertQuery = "INSERT INTO PatientDoctorAssignments (PatientID, DoctorID) VALUES (@PatientID, @DoctorID)";
                            foreach (int docID in toAssign)
                            {
                                using (SqlCommand cmdInsert = new SqlCommand(insertQuery, con, tran))
                                {
                                    cmdInsert.Parameters.AddWithValue("@PatientID", patientID);
                                    cmdInsert.Parameters.AddWithValue("@DoctorID", docID);
                                    cmdInsert.ExecuteNonQuery();
                                }
                            }

                            tran.Commit();
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }

                MessageBox.Show("Назначения врачей сохранены.");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения назначений: " + ex.Message);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
