document.addEventListener('DOMContentLoaded', function() {
  // Инициализация
  initThemeToggle();
  initTabs();
  initDatePickers();
  
  // Загрузка данных для комбобоксов
  loadDoctors();
  loadPatients();
  loadProcedures();
  
  // События поиска
  document.getElementById('txtSearchDoctor').addEventListener('input', filterDoctors);
  document.getElementById('txtSearchPatient').addEventListener('input', filterPatients);
  document.getElementById('txtSearchProcedure').addEventListener('input', filterProcedures);
  
  // События кнопок
  document.getElementById('backBtn').addEventListener('click', function() {
    window.location.href = 'chief.html';
  });
  
  document.getElementById('exitBtn').addEventListener('click', function() {
    window.location.href = 'Login.html';
  });
  
  // Кнопки вкладки "Все процедуры"
  document.getElementById('btnShowAll').addEventListener('click', showAllReport);
  document.getElementById('btnExportAll').addEventListener('click', exportAllReport);
  
  // Кнопки вкладки "Процедуры врача"
  document.getElementById('btnShowDoctor').addEventListener('click', showDoctorReport);
  document.getElementById('btnExportDoctor').addEventListener('click', exportDoctorReport);
  
  // Кнопки вкладки "Процедуры пациента"
  document.getElementById('btnShowPatient').addEventListener('click', showPatientReport);
  document.getElementById('btnExportPatient').addEventListener('click', exportPatientReport);
  
  // Кнопки вкладки "Назначения процедуры"
  document.getElementById('btnShowProcedure').addEventListener('click', showProcedureReport);
  document.getElementById('btnExportProcedure').addEventListener('click', exportProcedureReport);
});

// Глобальные переменные для хранения данных
let doctorsData = [];
let patientsData = [];
let proceduresData = [];
let reportAllData = [];
let reportDoctorData = [];
let reportPatientData = [];
let reportProcedureData = [];

// Инициализация переключателя темы
function initThemeToggle() {
  const themeToggle = document.getElementById('themeToggle');
  
  // Проверка сохраненной темы
  const savedTheme = localStorage.getItem('theme');
  if (savedTheme === 'dark') {
    document.body.classList.add('dark-theme');
    themeToggle.checked = true;
  }
  
  // Обработчик переключения темы
  themeToggle.addEventListener('change', function() {
    if (this.checked) {
      document.body.classList.add('dark-theme');
      localStorage.setItem('theme', 'dark');
    } else {
      document.body.classList.remove('dark-theme');
      localStorage.setItem('theme', 'light');
    }
  });
}

// Инициализация вкладок
function initTabs() {
  const tabButtons = document.querySelectorAll('.tab-btn');
  
  tabButtons.forEach(btn => {
    btn.addEventListener('click', function() {
      // Деактивация всех вкладок
      tabButtons.forEach(b => b.classList.remove('active'));
      document.querySelectorAll('.tab-content').forEach(tab => tab.classList.remove('active'));
      
      // Активация выбранной вкладки
      this.classList.add('active');
      const tabId = this.getAttribute('data-tab');
      document.getElementById(tabId).classList.add('active');
    });
  });
}

// Инициализация выбора дат
function initDatePickers() {
  // Устанавливаем сегодняшнюю дату для всех DatePicker'ов
  const today = new Date();
  const todayStr = today.toISOString().split('T')[0];
  
  document.getElementById('dpStart_All').value = todayStr;
  document.getElementById('dpEnd_All').value = todayStr;
  
  document.getElementById('dpStart_Doctor').value = todayStr;
  document.getElementById('dpEnd_Doctor').value = todayStr;
  
  document.getElementById('dpStart_Patient').value = todayStr;
  document.getElementById('dpEnd_Patient').value = todayStr;
  
  document.getElementById('dpStart_Procedure').value = todayStr;
  document.getElementById('dpEnd_Procedure').value = todayStr;
}

// Загрузка списка врачей
function loadDoctors() {
  fetch('/api/report/doctors')
    .then(response => {
      if (!response.ok) {
        throw new Error('Ошибка загрузки данных врачей');
      }
      return response.json();
    })
    .then(data => {
      doctorsData = data;
      populateDoctorComboBox();
    })
    .catch(error => {
      console.error('Ошибка:', error);
      showNotification('Не удалось загрузить список врачей', 'error');
    });
}

// Загрузка списка пациентов
function loadPatients() {
  fetch('/api/report/patients')
    .then(response => {
      if (!response.ok) {
        throw new Error('Ошибка загрузки данных пациентов');
      }
      return response.json();
    })
    .then(data => {
      patientsData = data;
      populatePatientComboBox();
    })
    .catch(error => {
      console.error('Ошибка:', error);
      showNotification('Не удалось загрузить список пациентов', 'error');
    });
}

// Загрузка списка процедур
function loadProcedures() {
  fetch('/api/report/procedures')
    .then(response => {
      if (!response.ok) {
        throw new Error('Ошибка загрузки данных процедур');
      }
      return response.json();
    })
    .then(data => {
      proceduresData = data;
      populateProcedureComboBox();
    })
    .catch(error => {
      console.error('Ошибка:', error);
      showNotification('Не удалось загрузить список процедур', 'error');
    });
}

// Заполнение комбобокса врачей
function populateDoctorComboBox() {
  const comboBox = document.getElementById('cbDoctor');
  comboBox.innerHTML = '';
  
  doctorsData.forEach(doctor => {
    const option = document.createElement('option');
    option.value = doctor.doctorId || doctor.DoctorID;
    option.textContent = doctor.fullName || doctor.FullName;
    comboBox.appendChild(option);
  });
  
  if (doctorsData.length > 0) {
    comboBox.selectedIndex = 0;
  }
}

// Заполнение комбобокса пациентов
function populatePatientComboBox() {
  const comboBox = document.getElementById('cbPatient');
  comboBox.innerHTML = '';
  
  patientsData.forEach(patient => {
    const option = document.createElement('option');
    option.value = patient.patientId || patient.PatientID;
    option.textContent = patient.fullName || patient.FullName;
    comboBox.appendChild(option);
  });
  
  if (patientsData.length > 0) {
    comboBox.selectedIndex = 0;
  }
}

// Заполнение комбобокса процедур
function populateProcedureComboBox() {
  const comboBox = document.getElementById('cbProcedure');
  comboBox.innerHTML = '';
  
  proceduresData.forEach(procedure => {
    const option = document.createElement('option');
    option.value = procedure.procedureId || procedure.ProcedureID;
    option.textContent = procedure.procedureDisplay || procedure.ProcedureDisplay;
    comboBox.appendChild(option);
  });
  
  if (proceduresData.length > 0) {
    comboBox.selectedIndex = 0;
  }
}

// Фильтрация в комбобоксе врачей
function filterDoctors() {
  const searchText = document.getElementById('txtSearchDoctor').value.toLowerCase();
  const comboBox = document.getElementById('cbDoctor');
  comboBox.innerHTML = '';
  
  const filteredDoctors = doctorsData.filter(doctor => 
    (doctor.fullName || doctor.FullName).toLowerCase().includes(searchText)
  );
  
  filteredDoctors.forEach(doctor => {
    const option = document.createElement('option');
    option.value = doctor.doctorId || doctor.DoctorID;
    option.textContent = doctor.fullName || doctor.FullName;
    comboBox.appendChild(option);
  });
  
  if (filteredDoctors.length > 0) {
    comboBox.selectedIndex = 0;
  }
}

// Фильтрация в комбобоксе пациентов
function filterPatients() {
  const searchText = document.getElementById('txtSearchPatient').value.toLowerCase();
  const comboBox = document.getElementById('cbPatient');
  comboBox.innerHTML = '';
  
  const filteredPatients = patientsData.filter(patient => 
    (patient.fullName || patient.FullName).toLowerCase().includes(searchText)
  );
  
  filteredPatients.forEach(patient => {
    const option = document.createElement('option');
    option.value = patient.patientId || patient.PatientID;
    option.textContent = patient.fullName || patient.FullName;
    comboBox.appendChild(option);
  });
  
  if (filteredPatients.length > 0) {
    comboBox.selectedIndex = 0;
  }
}

// Фильтрация в комбобоксе процедур
function filterProcedures() {
  const searchText = document.getElementById('txtSearchProcedure').value.toLowerCase();
  const comboBox = document.getElementById('cbProcedure');
  comboBox.innerHTML = '';
  
  const filteredProcedures = proceduresData.filter(procedure => 
    (procedure.procedureDisplay || procedure.ProcedureDisplay).toLowerCase().includes(searchText)
  );
  
  filteredProcedures.forEach(procedure => {
    const option = document.createElement('option');
    option.value = procedure.procedureId || procedure.ProcedureID;
    option.textContent = procedure.procedureDisplay || procedure.ProcedureDisplay;
    comboBox.appendChild(option);
  });
  
  if (filteredProcedures.length > 0) {
    comboBox.selectedIndex = 0;
  }
}

// Вкладка 1: Отчет по всем процедурам
function showAllReport() {
  const startDate = document.getElementById('dpStart_All').value;
  const endDate = document.getElementById('dpEnd_All').value;
  
  if (!startDate || !endDate) {
    showNotification('Укажите даты начала и конца периода', 'error');
    return;
  }
  
  fetch(`/api/report/all-procedures?startDate=${startDate}&endDate=${endDate}`)
    .then(response => {
      if (!response.ok) {
        throw new Error('Ошибка при получении данных отчета');
      }
      return response.json();
    })
    .then(data => {
      reportAllData = data;
      renderAllReport();
      calculateAllSummary();
    })
    .catch(error => {
      console.error('Ошибка:', error);
      showNotification('Не удалось загрузить данные отчета', 'error');
    });
}

function renderAllReport() {
  const tbody = document.querySelector('#dgReportAll tbody');
  tbody.innerHTML = '';
  
  if (reportAllData.length === 0) {
    tbody.innerHTML = '<tr><td colspan="6" class="text-center">Нет данных для отображения</td></tr>';
    return;
  }
  
  reportAllData.forEach(item => {
    const tr = document.createElement('tr');
    
    // Создаем ячейки для каждого столбца
    const tdDoctor = document.createElement('td');
    tdDoctor.textContent = item.Врач || item['Врач'];
    
    const tdProcedure = document.createElement('td');
    tdProcedure.textContent = item.Процедура || item['Процедура'];
    
    const tdPatient = document.createElement('td');
    tdPatient.textContent = item.Пациент || item['Пациент'];
    
    const tdDate = document.createElement('td');
    tdDate.textContent = formatDateTime(item['Дата/время']);
    
    const tdStatus = document.createElement('td');
    tdStatus.textContent = item.Статус || item['Статус'];
    
    const tdDuration = document.createElement('td');
    tdDuration.textContent = item['Длительность (мин)'];
    
    // Добавляем ячейки в строку
    tr.appendChild(tdDoctor);
    tr.appendChild(tdProcedure);
    tr.appendChild(tdPatient);
    tr.appendChild(tdDate);
    tr.appendChild(tdStatus);
    tr.appendChild(tdDuration);
    
    // Добавляем строку в таблицу
    tbody.appendChild(tr);
  });
}

function calculateAllSummary() {
  if (reportAllData.length === 0) {
    document.getElementById('txtAllSummary').textContent = 'Нет данных для отчета.';
    return;
  }
  
  // Фильтруем строки, исключая статус "Отменена"
  const validRows = reportAllData.filter(row => row.Статус !== 'Отменена');
  
  if (validRows.length === 0) {
    document.getElementById('txtAllSummary').textContent = 'Все процедуры в выборке отменены.';
    return;
  }
  
  // Уникальные врачи, процедуры, пациенты
  const distinctDoctors = new Set(validRows.map(row => row.Врач)).size;
  const distinctProcedures = new Set(validRows.map(row => row.Процедура)).size;
  const distinctPatients = new Set(validRows.map(row => row.Пациент)).size;
  
  // Суммарная длительность
  const totalDuration = validRows.reduce((sum, row) => 
    sum + (parseInt(row['Длительность (мин)']) || 0), 0);
  
  document.getElementById('txtAllSummary').textContent =
    `Общее количество врачей: ${distinctDoctors}\n` +
    `Общее количество процедур: ${distinctProcedures}\n` +
    `Общее количество пациентов: ${distinctPatients}\n` +
    `Общая длительность процедур: ${totalDuration} мин`;
}

function exportAllReport() {
  if (reportAllData.length === 0) {
    showNotification('Нет данных для экспорта', 'error');
    return;
  }
  
  const startDate = document.getElementById('dpStart_All').value;
  const endDate = document.getElementById('dpEnd_All').value;
  const summaryText = document.getElementById('txtAllSummary').textContent;
  
  exportReport('all', reportAllData, summaryText, startDate, endDate);
}

// Вкладка 2: Отчет по процедурам врача
function showDoctorReport() {
  const doctorId = document.getElementById('cbDoctor').value;
  const startDate = document.getElementById('dpStart_Doctor').value;
  const endDate = document.getElementById('dpEnd_Doctor').value;
  
  if (!doctorId) {
    showNotification('Выберите врача из списка', 'error');
    return;
  }
  
  if (!startDate || !endDate) {
    showNotification('Укажите даты начала и конца периода', 'error');
    return;
  }
  
  fetch(`/api/report/doctor-procedures?doctorId=${doctorId}&startDate=${startDate}&endDate=${endDate}`)
    .then(response => {
      if (!response.ok) {
        throw new Error('Ошибка при получении данных отчета');
      }
      return response.json();
    })
    .then(data => {
      reportDoctorData = data;
      renderDoctorReport();
      calculateDoctorSummary();
    })
    .catch(error => {
      console.error('Ошибка:', error);
      showNotification('Не удалось загрузить данные отчета', 'error');
    });
}

function renderDoctorReport() {
  const tbody = document.querySelector('#dgReportDoctor tbody');
  tbody.innerHTML = '';
  
  if (reportDoctorData.length === 0) {
    tbody.innerHTML = '<tr><td colspan="5" class="text-center">Нет данных для отображения</td></tr>';
    return;
  }
  
  reportDoctorData.forEach(item => {
    const tr = document.createElement('tr');
    
    const tdProcedure = document.createElement('td');
    tdProcedure.textContent = item.Процедура || item['Процедура'];
    
    const tdPatient = document.createElement('td');
    tdPatient.textContent = item.Пациент || item['Пациент'];
    
    const tdDate = document.createElement('td');
    tdDate.textContent = formatDateTime(item['Дата/время']);
    
    const tdStatus = document.createElement('td');
    tdStatus.textContent = item.Статус || item['Статус'];
    
    const tdDuration = document.createElement('td');
    tdDuration.textContent = item['Длительность (мин)'];
    
    tr.appendChild(tdProcedure);
    tr.appendChild(tdPatient);
    tr.appendChild(tdDate);
    tr.appendChild(tdStatus);
    tr.appendChild(tdDuration);
    
    tbody.appendChild(tr);
  });
}

function calculateDoctorSummary() {
  if (reportDoctorData.length === 0) {
    document.getElementById('txtDoctorSummary').textContent = 'Нет данных для отчета.';
    return;
  }
  
  // Фильтруем строки, исключая статус "Отменена"
  const validRows = reportDoctorData.filter(row => row.Статус !== 'Отменена');
  
  if (validRows.length === 0) {
    document.getElementById('txtDoctorSummary').textContent = 'Все процедуры в выборке отменены.';
    return;
  }
  
  // Уникальные процедуры, пациенты
  const distinctProcedures = new Set(validRows.map(row => row.Процедура)).size;
  const distinctPatients = new Set(validRows.map(row => row.Пациент)).size;
  
  // Суммарная длительность
  const totalDuration = validRows.reduce((sum, row) => 
    sum + (parseInt(row['Длительность (мин)']) || 0), 0);
  
  document.getElementById('txtDoctorSummary').textContent =
    `Общее количество процедур: ${distinctProcedures}\n` +
    `Общее количество пациентов: ${distinctPatients}\n` +
    `Общая длительность процедур: ${totalDuration} мин`;
}

function exportDoctorReport() {
  if (reportDoctorData.length === 0) {
    showNotification('Нет данных для экспорта', 'error');
    return;
  }
  
  const doctorSelect = document.getElementById('cbDoctor');
  const doctorName = doctorSelect.options[doctorSelect.selectedIndex].text;
  const startDate = document.getElementById('dpStart_Doctor').value;
  const endDate = document.getElementById('dpEnd_Doctor').value;
  const summaryText = document.getElementById('txtDoctorSummary').textContent;
  
  exportReport('doctor', reportDoctorData, summaryText, startDate, endDate, doctorName);
}

// Вкладка 3: Отчет по процедурам пациента
function showPatientReport() {
  const patientId = document.getElementById('cbPatient').value;
  const startDate = document.getElementById('dpStart_Patient').value;
  const endDate = document.getElementById('dpEnd_Patient').value;
  
  if (!patientId) {
    showNotification('Выберите пациента из списка', 'error');
    return;
  }
  
  if (!startDate || !endDate) {
    showNotification('Укажите даты начала и конца периода', 'error');
    return;
  }
  
  fetch(`/api/report/patient-procedures?patientId=${patientId}&startDate=${startDate}&endDate=${endDate}`)
    .then(response => {
      if (!response.ok) {
        throw new Error('Ошибка при получении данных отчета');
      }
      return response.json();
    })
    .then(data => {
      reportPatientData = data;
      renderPatientReport();
      calculatePatientSummary();
    })
    .catch(error => {
      console.error('Ошибка:', error);
      showNotification('Не удалось загрузить данные отчета', 'error');
    });
}

function renderPatientReport() {
  const tbody = document.querySelector('#dgReportPatient tbody');
  tbody.innerHTML = '';
  
  if (reportPatientData.length === 0) {
    tbody.innerHTML = '<tr><td colspan="5" class="text-center">Нет данных для отображения</td></tr>';
    return;
  }
  
  reportPatientData.forEach(item => {
    const tr = document.createElement('tr');
    
    const tdDoctor = document.createElement('td');
    tdDoctor.textContent = item.Врач || item['Врач'];
    
    const tdProcedure = document.createElement('td');
    tdProcedure.textContent = item.Процедура || item['Процедура'];
    
    const tdDate = document.createElement('td');
    tdDate.textContent = formatDateTime(item['Дата/время']);
    
    const tdStatus = document.createElement('td');
    tdStatus.textContent = item.Статус || item['Статус'];
    
    const tdDuration = document.createElement('td');
    tdDuration.textContent = item['Длительность (мин)'];
    
    tr.appendChild(tdDoctor);
    tr.appendChild(tdProcedure);
    tr.appendChild(tdDate);
    tr.appendChild(tdStatus);
    tr.appendChild(tdDuration);
    
    tbody.appendChild(tr);
  });
}

function calculatePatientSummary() {
  if (reportPatientData.length === 0) {
    document.getElementById('txtPatientSummary').textContent = 'Нет данных для отчета.';
    return;
  }
  
  // Фильтруем строки, исключая статус "Отменена"
  const validRows = reportPatientData.filter(row => row.Статус !== 'Отменена');
  
  if (validRows.length === 0) {
    document.getElementById('txtPatientSummary').textContent = 'Все процедуры в выборке отменены.';
    return;
  }
  
  // Уникальные врачи, процедуры
  const distinctDoctors = new Set(validRows.map(row => row.Врач)).size;
  const distinctProcedures = new Set(validRows.map(row => row.Процедура)).size;
  
  // Суммарная длительность
  const totalDuration = validRows.reduce((sum, row) => 
    sum + (parseInt(row['Длительность (мин)']) || 0), 0);
  
  document.getElementById('txtPatientSummary').textContent =
    `Общее количество врачей: ${distinctDoctors}\n` +
    `Общее количество процедур: ${distinctProcedures}\n` +
    `Общая длительность процедур: ${totalDuration} мин`;
}

function exportPatientReport() {
  if (reportPatientData.length === 0) {
    showNotification('Нет данных для экспорта', 'error');
    return;
  }
  
  const patientSelect = document.getElementById('cbPatient');
  const patientName = patientSelect.options[patientSelect.selectedIndex].text;
  const startDate = document.getElementById('dpStart_Patient').value;
  const endDate = document.getElementById('dpEnd_Patient').value;
  const summaryText = document.getElementById('txtPatientSummary').textContent;
  
  exportReport('patient', reportPatientData, summaryText, startDate, endDate, patientName);
}

// Вкладка 4: Отчет по назначениям процедуры
function showProcedureReport() {
  const procedureId = document.getElementById('cbProcedure').value;
  const startDate = document.getElementById('dpStart_Procedure').value;
  const endDate = document.getElementById('dpEnd_Procedure').value;
  
  if (!procedureId) {
    showNotification('Выберите процедуру из списка', 'error');
    return;
  }
  
  if (!startDate || !endDate) {
    showNotification('Укажите даты начала и конца периода', 'error');
    return;
  }
  
  fetch(`/api/report/procedure-appointments?procedureId=${procedureId}&startDate=${startDate}&endDate=${endDate}`)
    .then(response => {
      if (!response.ok) {
        throw new Error('Ошибка при получении данных отчета');
      }
      return response.json();
    })
    .then(data => {
      reportProcedureData = data;
      renderProcedureReport();
      calculateProcedureSummary();
    })
    .catch(error => {
      console.error('Ошибка:', error);
      showNotification('Не удалось загрузить данные отчета', 'error');
    });
}

function renderProcedureReport() {
  const tbody = document.querySelector('#dgReportProcedure tbody');
  tbody.innerHTML = '';
  
  if (reportProcedureData.length === 0) {
    tbody.innerHTML = '<tr><td colspan="4" class="text-center">Нет данных для отображения</td></tr>';
    return;
  }
  
  reportProcedureData.forEach(item => {
    const tr = document.createElement('tr');
    
    const tdPatient = document.createElement('td');
    tdPatient.textContent = item.Пациент || item['Пациент'];
    
    const tdDate = document.createElement('td');
    tdDate.textContent = formatDateTime(item['Дата/время']);
    
    const tdStatus = document.createElement('td');
    tdStatus.textContent = item.Статус || item['Статус'];
    
    const tdDuration = document.createElement('td');
    tdDuration.textContent = item['Длительность (мин)'];
    
    tr.appendChild(tdPatient);
    tr.appendChild(tdDate);
    tr.appendChild(tdStatus);
    tr.appendChild(tdDuration);
    
    tbody.appendChild(tr);
  });
}

function calculateProcedureSummary() {
  if (reportProcedureData.length === 0) {
    document.getElementById('txtProcedureSummary').textContent = 'Нет данных для отчета.';
    return;
  }
  
  // Фильтруем строки, исключая статус "Отменена"
  const validRows = reportProcedureData.filter(row => row.Статус !== 'Отменена');
  
  if (validRows.length === 0) {
    document.getElementById('txtProcedureSummary').textContent = 'Все процедуры в выборке отменены.';
    return;
  }
  
  // Уникальные пациенты
  const distinctPatients = new Set(validRows.map(row => row.Пациент)).size;
  
  // Суммарная длительность
  const totalDuration = validRows.reduce((sum, row) => 
    sum + (parseInt(row['Длительность (мин)']) || 0), 0);
  
  document.getElementById('txtProcedureSummary').textContent =
    `Общее количество пациентов: ${distinctPatients}\n` +
    `Общая длительность процедуры: ${totalDuration} мин`;
}

function exportProcedureReport() {
  if (reportProcedureData.length === 0) {
    showNotification('Нет данных для экспорта', 'error');
    return;
  }
  
  const procedureSelect = document.getElementById('cbProcedure');
  const procedureName = procedureSelect.options[procedureSelect.selectedIndex].text;
  const startDate = document.getElementById('dpStart_Procedure').value;
  const endDate = document.getElementById('dpEnd_Procedure').value;
  const summaryText = document.getElementById('txtProcedureSummary').textContent;
  
  exportReport('procedure', reportProcedureData, summaryText, startDate, endDate, procedureName);
}

// Общая функция экспорта отчета в Excel
function exportReport(reportType, data, summaryText, startDate, endDate, entityName = '') {
  const formattedStartDate = formatDate(startDate);
  const formattedEndDate = formatDate(endDate);
  
  let reportName = '';
  let reportHeader = '';
  
  switch (reportType) {
    case 'all':
      reportName = 'ВсеПроцедуры';
      reportHeader = `Все процедуры за период ${formattedStartDate} - ${formattedEndDate}`;
      break;
    case 'doctor':
      reportName = 'ПроцедурыВрача';
      reportHeader = `Процедуры врача ${entityName} за период ${formattedStartDate} - ${formattedEndDate}`;
      break;
    case 'patient':
      reportName = 'ПроцедурыПациента';
      reportHeader = `Процедуры пациента ${entityName} за период ${formattedStartDate} - ${formattedEndDate}`;
      break;
    case 'procedure':
      reportName = 'НазначенияПроцедуры';
      reportHeader = `Назначения процедуры ${entityName} за период ${formattedStartDate} - ${formattedEndDate}`;
      break;
  }
  
  // Подготавливаем данные для экспорта
  const exportData = {
    reportType,
    reportName,
    reportHeader,
    summaryText,
    data
  };
  
  // Отправляем запрос на сервер для создания Excel-файла
  fetch('/api/report/export', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(exportData)
  })
  .then(response => {
    if (!response.ok) {
      return response.json().then(errorData => {
        throw new Error(errorData.message || 'Ошибка при экспорте отчета');
      });
    }
    return response.blob();
  })
  .then(blob => {
    // Создаем ссылку для скачивания файла
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.style.display = 'none';
    a.href = url;
    a.download = `${reportName}_${new Date().toISOString().split('T')[0]}.xlsx`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
    
    showNotification('Отчет успешно экспортирован', 'success');
  })
  .catch(error => {
    console.error('Ошибка при экспорте отчета:', error);
    showNotification('Не удалось экспортировать отчет: ' + error.message, 'error');
  });
}

// Вспомогательные функции
function formatDate(dateStr) {
  if (!dateStr) return '—';
  
  const date = new Date(dateStr);
  if (isNaN(date.getTime())) return '—';
  
  const day = String(date.getDate()).padStart(2, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const year = date.getFullYear();
  
  return `${day}.${month}.${year}`;
}

function formatDateTime(dateTimeStr) {
  if (!dateTimeStr) return '—';
  
  const date = new Date(dateTimeStr);
  if (isNaN(date.getTime())) return '—';
  
  const day = String(date.getDate()).padStart(2, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const year = date.getFullYear();
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  
  return `${day}.${month}.${year} ${hours}:${minutes}`;
}

// Отображение уведомлений
function showNotification(message, type = 'info') {
  // Проверяем, есть ли уже контейнер для уведомлений
  let notificationContainer = document.querySelector('.notification-container');
  
  if (!notificationContainer) {
    notificationContainer = document.createElement('div');
    notificationContainer.className = 'notification-container';
    document.body.appendChild(notificationContainer);
  }
  
  // Создаем уведомление
  const notification = document.createElement('div');
  notification.className = `notification ${type}`;
  notification.textContent = message;
  
  notificationContainer.appendChild(notification);
  
  // Анимация появления
  setTimeout(() => {
    notification.classList.add('show');
  }, 10);
  
  // Автоматически скрываем через 3 секунды
  setTimeout(() => {
    notification.classList.remove('show');
    setTimeout(() => {
      notification.remove();
    }, 300);
  }, 3000);
} 