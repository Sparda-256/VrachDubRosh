using System;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace VrachDubRosh
{
    public partial class UploadDocumentWindow : Window, INotifyPropertyChanged
    {
        private string _originalFileName;
        private string _documentName;
        private string _fileType;

        public event PropertyChangedEventHandler PropertyChanged;

        public string OriginalFileName 
        { 
            get => _originalFileName; 
            set
            {
                _originalFileName = value;
                OnPropertyChanged();
            }
        }

        public string DocumentName 
        { 
            get => _documentName; 
            set
            {
                _documentName = value;
                OnPropertyChanged();
            }
        }

        public string FileType 
        { 
            get => _fileType; 
            set
            {
                _fileType = value;
                OnPropertyChanged();
            }
        }

        public string SelectedCategory
        {
            get
            {
                if (cmbCategory.SelectedItem is ComboBoxItem selectedItem)
                {
                    return selectedItem.Content.ToString();
                }
                return "Прочее";
            }
        }

        public string Description
        {
            get => txtDescription.Text;
        }

        public UploadDocumentWindow(string fileName, string fileType)
        {
            InitializeComponent();
            
            OriginalFileName = fileName;
            DocumentName = fileName; // По умолчанию используем имя файла
            FileType = fileType;
            
            DataContext = this;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(DocumentName))
            {
                MessageBox.Show("Пожалуйста, укажите название документа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDocumentName.Focus();
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 