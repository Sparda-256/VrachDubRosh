// Глобальные переменные
let patientId = null;
let patientData = null;
let diagnoses = [];
let medications = [];
let selectedDiagnoses = [];
let selectedMedications = [];
let isDarkTheme = false;
let sourceScreen = null; // Для отслеживания экрана, с которого была открыта медкарта
let sourceId = null; // Для хранения ID источника (врача или главврача)
// Переменные для сортировки
let currentSortColumn = null;
let currentSortDirection = 'asc';
let doctorProceduresData = {}; // Для хранения данных о процедурах по врачам для сортировки

// При загрузке DOM
document.addEventListener('DOMContentLoaded', () => {
  // Получаем ID пациента из URL
  const urlParams = new URLSearchParams(window.location.search);
  patientId = urlParams.get('id');
  // Получаем источник перехода (doctor или chief)
  sourceScreen = urlParams.get('source') || 'chief';
  // Получаем ID источника (ID врача или главврача)
  sourceId = urlParams.get('source_id');
  
  if (!patientId) {
    showError('Не указан ID пациента');
    return;
  }
  
  // Инициализация переключателя темы
  initThemeToggle();
  
  // Инициализация вкладок
  initTabs();
  
  // Инициализация обработчиков для кнопок
  initButtons();
  
  // Инициализация модальных окон
  initModals();
  
  // Загрузка данных пациента
  loadPatientData();
});

// Инициализация переключателя темы
function initThemeToggle() {
  const themeToggle = document.getElementById('themeToggle');
  
  // Проверяем тему из параметров URL (приоритет над localStorage)
  const urlParams = new URLSearchParams(window.location.search);
  const themeParam = urlParams.get('theme');
  
  if (themeParam === 'dark') {
    isDarkTheme = true;
  } else if (themeParam === 'light') {
    isDarkTheme = false;
  } else {
    // Если нет параметра в URL, проверяем localStorage
    isDarkTheme = localStorage.getItem('darkTheme') === 'true';
  }
  
  // Устанавливаем начальное состояние
  if (isDarkTheme) {
    document.body.classList.add('dark-theme');
    themeToggle.checked = true;
  } else {
    document.body.classList.remove('dark-theme');
    themeToggle.checked = false;
  }
  
  // Обработчик изменения темы
  themeToggle.addEventListener('change', function() {
    isDarkTheme = this.checked;
    document.body.classList.toggle('dark-theme', isDarkTheme);
    localStorage.setItem('darkTheme', isDarkTheme);
  });
}

// Инициализация вкладок
function initTabs() {
  const tabBtns = document.querySelectorAll('.tab-btn');
  
  tabBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      const tabId = btn.getAttribute('data-tab');
      
      // Убираем активный класс со всех вкладок и кнопок
      document.querySelectorAll('.tab-content').forEach(tab => tab.classList.remove('active'));
      document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
      
      // Активируем выбранную вкладку и кнопку
      document.getElementById(tabId).classList.add('active');
      btn.classList.add('active');
    });
  });
}

// Инициализация обработчиков кнопок
function initButtons() {
  // Кнопка выхода
  document.getElementById('exitBtn').addEventListener('click', () => {
    // Перенаправляем пользователя в зависимости от источника
    if (sourceScreen === 'doctor') {
      window.location.href = sourceId ? `doctor.html?id=${sourceId}` : 'doctor.html';
    } else {
      window.location.href = 'chief.html';
    }
  });
  
  // Кнопка печати
  document.getElementById('printBtn').addEventListener('click', printMedCard);
  
  // Кнопка сохранения всех данных
  document.getElementById('saveAllBtn').addEventListener('click', saveAllData);
  
  // Кнопки для диагнозов
  document.getElementById('addDiagnosis').addEventListener('click', showDiagnosisModal);
  document.getElementById('removeDiagnosis').addEventListener('click', removeDiagnosis);
  
  // Кнопки для медикаментов
  document.getElementById('addMedication').addEventListener('click', () => showMedicationModal());
  document.getElementById('editMedication').addEventListener('click', editMedication);
  document.getElementById('removeMedication').addEventListener('click', removeMedication);
}

// Инициализация модальных окон
function initModals() {
  // Диагнозы
  const diagnosisModal = document.getElementById('diagnosisModal');
  const closeDiagnosis = diagnosisModal.querySelector('.close-btn');
  const cancelDiagnosis = diagnosisModal.querySelector('.cancel-btn');
  const saveDiagnosisBtn = document.getElementById('saveDiagnosisBtn');
  const searchDiagnosis = document.getElementById('searchDiagnosis');
  
  closeDiagnosis.addEventListener('click', () => diagnosisModal.style.display = 'none');
  cancelDiagnosis.addEventListener('click', () => diagnosisModal.style.display = 'none');
  saveDiagnosisBtn.addEventListener('click', saveDiagnosis);
  searchDiagnosis.addEventListener('input', filterDiagnoses);
  
  // Медикаменты
  const medicationModal = document.getElementById('medicationModal');
  const closeMedication = medicationModal.querySelector('.close-btn');
  const cancelMedication = medicationModal.querySelector('.cancel-btn');
  const saveMedicationBtn = document.getElementById('saveMedicationBtn');
  
  closeMedication.addEventListener('click', () => medicationModal.style.display = 'none');
  cancelMedication.addEventListener('click', () => medicationModal.style.display = 'none');
  saveMedicationBtn.addEventListener('click', saveMedication);
  
  // Закрытие при клике вне модального окна
  window.addEventListener('click', (e) => {
    if (e.target === diagnosisModal) diagnosisModal.style.display = 'none';
    if (e.target === medicationModal) medicationModal.style.display = 'none';
  });
}

// Загрузка данных пациента
function loadPatientData() {
  // Сначала проверим, работает ли API
  fetch('/api/chief/test')
    .then(response => {
      if (!response.ok) {
        throw new Error('API недоступен');
      }
      return response.json();
    })
    .then(data => {
      console.log('API test successful:', data);
      // Теперь продолжаем загрузку данных пациента
      loadMainPatientData();
    })
    .catch(error => {
      console.error('API test failed:', error);
      showError('Ошибка соединения с API: ' + error.message);
    });
}

// Основная загрузка данных пациента
function loadMainPatientData() {
  fetch(`/api/chief/patient/${patientId}/medcard`)
    .then(response => {
      if (!response.ok) {
        throw new Error('Ошибка загрузки данных пациента');
      }
      return response.json();
    })
    .then(data => {
      patientData = data;
      
      // Отображаем данные пациента
      displayPatientInfo();
      
      // Загружаем диагнозы
      loadDiagnoses();
      
      // Загружаем измерения
      loadMeasurements();
      
      // Загружаем медикаменты
      loadMedications();
      
      // Загружаем процедуры по врачам
      loadDoctorProcedures();
    })
    .catch(error => showError(error.message));
}

// Отображение информации о пациенте
function displayPatientInfo() {
  // Заголовки
  document.getElementById('patientTitle').textContent = `Санаторная книжка - ${patientData.fullName}`;
  document.title = `Санаторная книжка - ${patientData.fullName} - Дубовая Роща`;
  
  // Данные на титульном листе
  document.getElementById('patientFullName').textContent = patientData.fullName;
  document.getElementById('patientDateOfBirth').textContent = formatDate(patientData.dateOfBirth);
  document.getElementById('patientRecordDate').textContent = formatDate(patientData.recordDate);
  document.getElementById('patientDischargeDate').textContent = formatDate(patientData.dischargeDate);
  document.getElementById('stayType').textContent = patientData.stayType || 'Не определен';
  document.getElementById('accommodation').textContent = patientData.accommodation || 'Не определено';
}

// Загрузка диагнозов пациента
function loadDiagnoses() {
  fetch(`/api/chief/patient/${patientId}/diagnoses`)
    .then(response => {
      if (!response.ok) {
        throw new Error('Ошибка загрузки диагнозов');
      }
      return response.json();
    })
    .then(data => {
      diagnoses = data;
      displayDiagnoses();
      
      // Загружаем список всех диагнозов для модального окна
      loadAllDiagnoses();
    })
    .catch(error => showError(error.message));
}

// Загрузка измерений
function loadMeasurements() {
  fetch(`/api/chief/patient/${patientId}/measurements`)
    .then(response => {
      if (!response.ok) {
        throw new Error('Ошибка загрузки антропометрических данных');
      }
      return response.json();
    })
    .then(data => {
      displayMeasurements(data);
    })
    .catch(error => showError(error.message));
}

// Загрузка медикаментов
function loadMedications() {
  fetch(`/api/chief/patient/${patientId}/medications`)
    .then(response => {
      if (!response.ok) {
        throw new Error('Ошибка загрузки медикаментов');
      }
      return response.json();
    })
    .then(data => {
      medications = data;
      displayMedications();
    })
    .catch(error => showError(error.message));
}

// Загрузка процедур по врачам
function loadDoctorProcedures() {
  const url = `/api/chief/patient/${patientId}/doctor-procedures`;
  console.log(`Fetching doctor procedures from URL: ${url}`);
  
  fetch(url)
    .then(response => {
      console.log(`Response status: ${response.status} ${response.statusText}`);
      
      // Clone the response for debugging
      const responseClone = response.clone();
      
      // Try to read the response text for debugging
      responseClone.text().then(text => {
        console.log('Raw response:', text);
        try {
          // Try to parse as JSON to get detailed error
          const data = JSON.parse(text);
          if (data && data.message) {
            console.error('Error message from server:', data.message);
          }
        } catch (e) {
          console.log('Response is not JSON');
        }
      }).catch(err => console.error('Could not read response text:', err));
      
      if (!response.ok) {
        throw new Error(`Ошибка загрузки процедур по врачам: ${response.status} ${response.statusText}`);
      }
      return response.json();
    })
    .then(data => {
      console.log('Doctor procedures data received:', data);
      displayDoctorProcedures(data);
    })
    .catch(error => {
      console.error('Failed to load doctor procedures:', error);
      alert(`Подробная ошибка: ${error.message}`);
      showError(error.message);
    });
}

// Отображение диагнозов
function displayDiagnoses() {
  const tbody = document.querySelector('#diagnosesTable tbody');
  tbody.innerHTML = '';
  
  if (diagnoses.length === 0) {
    const row = document.createElement('tr');
    row.innerHTML = '<td colspan="1">Нет диагнозов</td>';
    tbody.appendChild(row);
    return;
  }
  
  diagnoses.forEach(diagnosis => {
    const row = document.createElement('tr');
    row.innerHTML = `<td>${diagnosis.diagnosisName}</td>`;
    row.dataset.id = diagnosis.diagnosisId;
    
    row.addEventListener('click', () => {
      row.classList.toggle('selected');
      
      if (row.classList.contains('selected')) {
        selectedDiagnoses.push(diagnosis.diagnosisId);
      } else {
        selectedDiagnoses = selectedDiagnoses.filter(id => id !== diagnosis.diagnosisId);
      }
    });
    
    tbody.appendChild(row);
  });

  // Инициализируем сортировку для таблицы диагнозов
  initTableSorting(document.getElementById('diagnosesTable'));
}

// Отображение антропометрических данных
function displayMeasurements(data) {
  data.forEach(measurement => {
    if (measurement.measurementType === 'При поступлении') {
      document.getElementById('admissionHeight').value = measurement.height || '';
      document.getElementById('admissionWeight').value = measurement.weight || '';
      document.getElementById('admissionBP').value = measurement.bloodPressure || '';
      if (measurement.measurementDate) {
        document.getElementById('admissionDate').value = formatDateForInput(measurement.measurementDate);
      }
    } else if (measurement.measurementType === 'В процессе лечения') {
      document.getElementById('processHeight').value = measurement.height || '';
      document.getElementById('processWeight').value = measurement.weight || '';
      document.getElementById('processBP').value = measurement.bloodPressure || '';
      if (measurement.measurementDate) {
        document.getElementById('processDate').value = formatDateForInput(measurement.measurementDate);
      }
    } else if (measurement.measurementType === 'При выписке') {
      document.getElementById('dischargeHeight').value = measurement.height || '';
      document.getElementById('dischargeWeight').value = measurement.weight || '';
      document.getElementById('dischargeBP').value = measurement.bloodPressure || '';
      if (measurement.measurementDate) {
        document.getElementById('dischargeDate').value = formatDateForInput(measurement.measurementDate);
      }
    }
  });
}

// Отображение медикаментов
function displayMedications() {
  const tbody = document.querySelector('#medicationsTable tbody');
  tbody.innerHTML = '';
  
  if (medications.length === 0) {
    const row = document.createElement('tr');
    row.innerHTML = '<td colspan="4">Нет назначенных медикаментов</td>';
    tbody.appendChild(row);
    return;
  }
  
  medications.forEach(medication => {
    const row = document.createElement('tr');
    row.innerHTML = `
      <td>${medication.medicationName}</td>
      <td>${medication.dosage || ''}</td>
      <td>${medication.instructions || ''}</td>
      <td>${formatDate(medication.prescribedDate)}</td>
    `;
    row.dataset.id = medication.medicationId;
    
    row.addEventListener('click', () => {
      row.classList.toggle('selected');
      
      if (row.classList.contains('selected')) {
        selectedMedications.push(medication.medicationId);
      } else {
        selectedMedications = selectedMedications.filter(id => id !== medication.medicationId);
      }
    });
    
    tbody.appendChild(row);
  });

  // Инициализируем сортировку для таблицы медикаментов
  initTableSorting(document.getElementById('medicationsTable'));
}

// Отображение процедур по врачам
function displayDoctorProcedures(data) {
  // Очищаем динамические вкладки
  const tabHeader = document.querySelector('.tab-header');
  const tabContainer = document.querySelector('.tab-container');
  const existingTabs = document.querySelectorAll('.tab-btn:not([data-tab="title-page"]):not([data-tab="anthropometry"])');
  
  existingTabs.forEach(tab => tab.remove());
  
  // Создаем новые вкладки для каждого врача
  data.forEach((doctorData, index) => {
    // Используем безопасный доступ к свойствам, учитывая возможные разные форматы ответа API
    const doctorId = doctorData.doctorId || doctorData.DoctorId;
    const fullName = doctorData.fullName || doctorData.FullName || "";
    const specialty = doctorData.specialty || doctorData.Specialty || "";
    const generalName = doctorData.generalName || doctorData.GeneralName || "";
    const officeNumber = doctorData.officeNumber || doctorData.OfficeNumber || "";
    const procedures = doctorData.procedures || doctorData.Procedures || [];
    
    // Сохраняем процедуры для сортировки
    doctorProceduresData[doctorId] = procedures;
    
    // Используем generalName если доступно, иначе specialty
    const doctorName = generalName || specialty;
    const tabId = `doctor-${doctorId}`;
    
    // Создаем кнопку для вкладки
    const tabButton = document.createElement('button');
    tabButton.className = 'tab-btn';
    tabButton.setAttribute('data-tab', tabId);
    tabButton.textContent = doctorName;
    tabHeader.appendChild(tabButton);
    
    // Создаем контент вкладки
    const tabContent = document.createElement('div');
    tabContent.id = tabId;
    tabContent.className = 'tab-content';
    
    // Заголовок
    const title = document.createElement('h3');
    title.className = 'section-title centered-title';
    title.textContent = `${doctorName.toUpperCase()}, кабинет ${officeNumber}`;
    
    // Подзаголовок
    const subtitle = document.createElement('h4');
    subtitle.className = 'section-title';
    subtitle.textContent = 'НАЗНАЧЕННЫЕ ПРОЦЕДУРЫ';
    
    // Таблица процедур
    const tableContainer = document.createElement('div');
    tableContainer.className = 'procedures-table-container'; // Используем новый класс
    
    const table = document.createElement('table');
    table.className = 'data-table';
    table.dataset.doctorId = doctorId;
    
    const thead = document.createElement('thead');
    thead.innerHTML = `
      <tr>
        <th data-sort="procedureName">Наименование</th>
        <th data-sort="appointmentDateTime">Дата и время</th>
        <th data-sort="status">Статус</th>
      </tr>
    `;
    
    const tbody = document.createElement('tbody');
    
    if (procedures && procedures.length > 0) {
      procedures.forEach(procedure => {
        const procedureName = procedure.procedureName || procedure.ProcedureName || "";
        const appointmentDateTime = procedure.appointmentDateTime || procedure.AppointmentDateTime;
        const status = procedure.status || procedure.Status || "";
        
        const row = document.createElement('tr');
        row.innerHTML = `
          <td>${procedureName}</td>
          <td>${formatDateTime(appointmentDateTime)}</td>
          <td>${status}</td>
        `;
        tbody.appendChild(row);
      });
    } else {
      const emptyRow = document.createElement('tr');
      emptyRow.innerHTML = '<td colspan="3">Нет назначенных процедур</td>';
      tbody.appendChild(emptyRow);
    }
    
    // Собираем таблицу
    table.appendChild(thead);
    table.appendChild(tbody);
    tableContainer.appendChild(table);
    
    // Добавляем элементы во вкладку
    tabContent.appendChild(title);
    tabContent.appendChild(subtitle);
    tabContent.appendChild(tableContainer);
    
    // Добавляем вкладку в контейнер
    tabContainer.appendChild(tabContent);
    
    // Добавляем обработчик клика на вкладку
    tabButton.addEventListener('click', () => {
      document.querySelectorAll('.tab-content').forEach(tab => tab.classList.remove('active'));
      document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
      tabContent.classList.add('active');
      tabButton.classList.add('active');
    });

    // Инициализируем сортировку для таблицы
    initTableSorting(table);
  });
}

// Инициализация сортировки для таблицы
function initTableSorting(table) {
  const headers = table.querySelectorAll('th[data-sort]');
  const doctorId = table.dataset.doctorId || null;
  const tableId = table.id;
  
  headers.forEach(header => {
    header.addEventListener('click', function() {
      const sortBy = this.getAttribute('data-sort');
      
      // Если сортируем по тому же столбцу, меняем направление
      if (currentSortColumn === sortBy) {
        currentSortDirection = currentSortDirection === 'asc' ? 'desc' : 'asc';
      } else {
        currentSortColumn = sortBy;
        currentSortDirection = 'asc';
      }
      
      // Удаляем индикаторы сортировки со всех заголовков в этой таблице
      headers.forEach(h => {
        h.classList.remove('sort-asc', 'sort-desc');
      });
      
      // Добавляем индикатор на текущий заголовок
      this.classList.add(currentSortDirection === 'asc' ? 'sort-asc' : 'sort-desc');
      
      // Сортируем и обновляем таблицу в зависимости от её типа
      if (doctorId) {
        // Таблица процедур врача
        sortAndRenderProcedures(doctorId, sortBy);
      } else if (tableId === 'diagnosesTable') {
        // Таблица диагнозов
        sortAndRenderDiagnoses(sortBy);
      } else if (tableId === 'medicationsTable') {
        // Таблица медикаментов
        sortAndRenderMedications(sortBy);
      }
    });
  });
}

// Сортировка и отображение процедур
function sortAndRenderProcedures(doctorId, sortBy) {
  const procedures = doctorProceduresData[doctorId];
  if (!procedures || procedures.length === 0) return;
  
  procedures.sort((a, b) => {
    // Используем безопасный доступ к свойствам с учетом разных форматов данных
    let aValue, bValue;
    
    switch(sortBy) {
      case 'procedureName':
        aValue = a.procedureName || a.ProcedureName || "";
        bValue = b.procedureName || b.ProcedureName || "";
        break;
      case 'appointmentDateTime':
        aValue = new Date(a.appointmentDateTime || a.AppointmentDateTime);
        bValue = new Date(b.appointmentDateTime || b.AppointmentDateTime);
        return currentSortDirection === 'asc' ? aValue - bValue : bValue - aValue;
      case 'status':
        aValue = a.status || a.Status || "";
        bValue = b.status || b.Status || "";
        break;
      default:
        aValue = a[sortBy] || "";
        bValue = b[sortBy] || "";
    }
    
    if (currentSortDirection === 'asc') {
      return String(aValue).localeCompare(String(bValue));
    } else {
      return String(bValue).localeCompare(String(aValue));
    }
  });
  
  // Обновляем таблицу с отсортированными данными
  const tableBody = document.querySelector(`table[data-doctor-id="${doctorId}"] tbody`);
  tableBody.innerHTML = '';
  
  procedures.forEach(procedure => {
    const procedureName = procedure.procedureName || procedure.ProcedureName || "";
    const appointmentDateTime = procedure.appointmentDateTime || procedure.AppointmentDateTime;
    const status = procedure.status || procedure.Status || "";
    
    const row = document.createElement('tr');
    row.innerHTML = `
      <td>${procedureName}</td>
      <td>${formatDateTime(appointmentDateTime)}</td>
      <td>${status}</td>
    `;
    tableBody.appendChild(row);
  });
}

// Сортировка и отображение диагнозов
function sortAndRenderDiagnoses(sortBy) {
  if (!diagnoses || diagnoses.length === 0) return;
  
  diagnoses.sort((a, b) => {
    let aValue = a[sortBy] || '';
    let bValue = b[sortBy] || '';
    
    if (currentSortDirection === 'asc') {
      return String(aValue).localeCompare(String(bValue));
    } else {
      return String(bValue).localeCompare(String(aValue));
    }
  });
  
  // Обновляем таблицу с отсортированными данными
  const tbody = document.querySelector('#diagnosesTable tbody');
  tbody.innerHTML = '';
  
  diagnoses.forEach(diagnosis => {
    const row = document.createElement('tr');
    row.innerHTML = `<td>${diagnosis.diagnosisName}</td>`;
    row.dataset.id = diagnosis.diagnosisId;
    
    row.addEventListener('click', () => {
      row.classList.toggle('selected');
      
      if (row.classList.contains('selected')) {
        selectedDiagnoses.push(diagnosis.diagnosisId);
      } else {
        selectedDiagnoses = selectedDiagnoses.filter(id => id !== diagnosis.diagnosisId);
      }
    });
    
    tbody.appendChild(row);
  });
}

// Сортировка и отображение медикаментов
function sortAndRenderMedications(sortBy) {
  if (!medications || medications.length === 0) return;
  
  medications.sort((a, b) => {
    let aValue = a[sortBy];
    let bValue = b[sortBy];
    
    // Особая обработка для дат
    if (sortBy === 'prescribedDate') {
      aValue = new Date(aValue);
      bValue = new Date(bValue);
      return currentSortDirection === 'asc' ? aValue - bValue : bValue - aValue;
    }
    
    // Для текстовых полей
    aValue = aValue || '';
    bValue = bValue || '';
    
    if (currentSortDirection === 'asc') {
      return String(aValue).localeCompare(String(bValue));
    } else {
      return String(bValue).localeCompare(String(aValue));
    }
  });
  
  // Обновляем таблицу с отсортированными данными
  const tbody = document.querySelector('#medicationsTable tbody');
  tbody.innerHTML = '';
  
  medications.forEach(medication => {
    const row = document.createElement('tr');
    row.innerHTML = `
      <td>${medication.medicationName}</td>
      <td>${medication.dosage || ''}</td>
      <td>${medication.instructions || ''}</td>
      <td>${formatDate(medication.prescribedDate)}</td>
    `;
    row.dataset.id = medication.medicationId;
    
    row.addEventListener('click', () => {
      row.classList.toggle('selected');
      
      if (row.classList.contains('selected')) {
        selectedMedications.push(medication.medicationId);
      } else {
        selectedMedications = selectedMedications.filter(id => id !== medication.medicationId);
      }
    });
    
    tbody.appendChild(row);
  });
}

// Загрузка всех доступных диагнозов для модального окна
function loadAllDiagnoses() {
  fetch('/api/chief/diagnoses')
    .then(response => {
      if (!response.ok) {
        throw new Error('Ошибка загрузки списка диагнозов');
      }
      return response.json();
    })
    .then(data => {
      const diagnosisList = document.getElementById('diagnosisList');
      diagnosisList.innerHTML = '';
      
      data.forEach(diagnosis => {
        const option = document.createElement('option');
        option.value = diagnosis.diagnosisId;
        option.textContent = diagnosis.diagnosisName;
        diagnosisList.appendChild(option);
      });
    })
    .catch(error => showError(error.message));
}

// Фильтрация диагнозов в модальном окне
function filterDiagnoses() {
  const searchText = document.getElementById('searchDiagnosis').value.toLowerCase();
  const diagnosisList = document.getElementById('diagnosisList');
  
  Array.from(diagnosisList.options).forEach(option => {
    const diagnosisName = option.textContent.toLowerCase();
    if (diagnosisName.includes(searchText)) {
      option.style.display = '';
    } else {
      option.style.display = 'none';
    }
  });
}

// Показать модальное окно добавления диагноза
function showDiagnosisModal() {
  document.getElementById('diagnosisModal').style.display = 'block';
}

// Сохранить диагноз
function saveDiagnosis() {
  const diagnosisList = document.getElementById('diagnosisList');
  const diagnosisId = diagnosisList.value;
  
  if (!diagnosisId) {
    showError('Пожалуйста, выберите диагноз');
    return;
  }
  
  const data = {
    patientId: patientId,
    diagnosisId: diagnosisId,
    diagnosisType: "Сопутствующий" // Добавляем тип диагноза
  };
  
  console.log('Отправка данных диагноза:', data);
  
  fetch('/api/chief/patient-diagnosis', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(data)
  })
    .then(response => {
      // Клонируем ответ для возможности чтения тела ответа при ошибке
      const responseClone = response.clone();
      
      if (!response.ok) {
        // Пытаемся прочитать детальное сообщение ошибки из тела ответа
        return responseClone.json().then(errorData => {
          const errorMessage = errorData.message || 'Неизвестная ошибка при добавлении диагноза';
          throw new Error(errorMessage);
        }).catch(error => {
          // Если не удалось получить детальное сообщение, используем стандартное
          throw new Error(`Ошибка ${response.status}: Не удалось добавить диагноз`);
        });
      }
      
      return response.json();
    })
    .then(data => {
      console.log('Ответ сервера:', data);
      // Закрываем модальное окно
      document.getElementById('diagnosisModal').style.display = 'none';
      // Перезагружаем диагнозы
      loadDiagnoses();
      // Показываем сообщение об успехе
      showError('Диагноз успешно добавлен', 'success');
    })
    .catch(error => {
      console.error('Ошибка при добавлении диагноза:', error);
      showError(error.message);
    });
}

// Удалить выбранные диагнозы
function removeDiagnosis() {
  if (selectedDiagnoses.length === 0) {
    showError('Пожалуйста, выберите диагнозы для удаления');
    return;
  }
  
  if (!confirm(`Вы уверены, что хотите удалить ${selectedDiagnoses.length === 1 ? 'выбранный диагноз' : 'выбранные диагнозы'}?`)) {
    return;
  }
  
  const requests = selectedDiagnoses.map(diagnosisId =>
    fetch(`/api/chief/patient-diagnosis/${patientId}/${diagnosisId}`, {
      method: 'DELETE'
    })
  );
  
  Promise.all(requests)
    .then(() => {
      selectedDiagnoses = [];
      loadDiagnoses();
    })
    .catch(error => showError('Ошибка при удалении диагнозов: ' + error.message));
}

// Показать модальное окно добавления/редактирования медикамента
function showMedicationModal(medication = null) {
  const modal = document.getElementById('medicationModal');
  const modalTitle = document.getElementById('medicationModalTitle');
  const medicationId = document.getElementById('medicationId');
  const medicationName = document.getElementById('medicationName');
  const dosage = document.getElementById('dosage');
  const instructions = document.getElementById('instructions');
  const prescribedDate = document.getElementById('prescribedDate');
  
  if (medication) {
    modalTitle.textContent = 'Редактирование медикамента';
    medicationId.value = medication.medicationId;
    medicationName.value = medication.medicationName;
    dosage.value = medication.dosage || '';
    instructions.value = medication.instructions || '';
    prescribedDate.value = formatDateForInput(medication.prescribedDate);
  } else {
    modalTitle.textContent = 'Добавление медикамента';
    medicationId.value = '';
    medicationName.value = '';
    dosage.value = '';
    instructions.value = '';
    prescribedDate.value = formatDateForInput(new Date());
  }
  
  modal.style.display = 'block';
}

// Редактировать выбранный медикамент
function editMedication() {
  if (selectedMedications.length !== 1) {
    showError('Пожалуйста, выберите один медикамент для редактирования');
    return;
  }
  
  const medicationId = selectedMedications[0];
  const medication = medications.find(med => med.medicationId === medicationId);
  
  if (medication) {
    showMedicationModal(medication);
  }
}

// Сохранить медикамент
function saveMedication() {
  const medicationId = document.getElementById('medicationId').value;
  const medicationName = document.getElementById('medicationName').value;
  const dosage = document.getElementById('dosage').value;
  const instructions = document.getElementById('instructions').value;
  const prescribedDate = document.getElementById('prescribedDate').value;
  
  if (!medicationName) {
    showError('Пожалуйста, укажите наименование медикамента');
    return;
  }
  
  const data = {
    medicationId: medicationId || 0,
    patientId: patientId,
    medicationName: medicationName,
    dosage: dosage,
    instructions: instructions,
    prescribedDate: prescribedDate
  };
  
  const method = medicationId ? 'PUT' : 'POST';
  const url = '/api/chief/patient-medication';
  
  fetch(url, {
    method: method,
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(data)
  })
    .then(response => {
      if (!response.ok) {
        throw new Error('Ошибка при сохранении медикамента');
      }
      return response.json();
    })
    .then(() => {
      // Закрываем модальное окно
      document.getElementById('medicationModal').style.display = 'none';
      // Перезагружаем медикаменты
      loadMedications();
      // Сбрасываем выбранные медикаменты
      selectedMedications = [];
    })
    .catch(error => showError(error.message));
}

// Удалить выбранные медикаменты
function removeMedication() {
  if (selectedMedications.length === 0) {
    showError('Пожалуйста, выберите медикаменты для удаления');
    return;
  }
  
  if (!confirm(`Вы уверены, что хотите удалить ${selectedMedications.length === 1 ? 'выбранный медикамент' : 'выбранные медикаменты'}?`)) {
    return;
  }
  
  const requests = selectedMedications.map(medicationId =>
    fetch(`/api/chief/patient-medication/${medicationId}`, {
      method: 'DELETE'
    })
  );
  
  Promise.all(requests)
    .then(() => {
      selectedMedications = [];
      loadMedications();
    })
    .catch(error => showError('Ошибка при удалении медикаментов: ' + error.message));
}

// Сохранить все данные
function saveAllData() {
  // Собираем данные измерений
  const admissionHeight = document.getElementById('admissionHeight').value.trim();
  const admissionWeight = document.getElementById('admissionWeight').value.trim();
  const admissionBP = document.getElementById('admissionBP').value.trim();
  const admissionDate = document.getElementById('admissionDate').value.trim();
  
  const processHeight = document.getElementById('processHeight').value.trim();
  const processWeight = document.getElementById('processWeight').value.trim();
  const processBP = document.getElementById('processBP').value.trim();
  const processDate = document.getElementById('processDate').value.trim();
  
  const dischargeHeight = document.getElementById('dischargeHeight').value.trim();
  const dischargeWeight = document.getElementById('dischargeWeight').value.trim();
  const dischargeBP = document.getElementById('dischargeBP').value.trim();
  const dischargeDate = document.getElementById('dischargeDate').value.trim();
  
  // Проверяем, чтобы в каждой группе либо все поля были заполнены, либо все пустые
  const admissionHasValue = admissionHeight || admissionWeight || admissionBP || admissionDate;
  const processHasValue = processHeight || processWeight || processBP || processDate;
  const dischargeHasValue = dischargeHeight || dischargeWeight || dischargeBP || dischargeDate;
  
  // Проверка групп с неполными данными
  if (admissionHasValue && (!admissionHeight || !admissionWeight || !admissionBP || !admissionDate)) {
    showError("Пожалуйста, заполните все поля в группе 'При поступлении' или оставьте все поля пустыми");
    return;
  }
  
  if (processHasValue && (!processHeight || !processWeight || !processBP || !processDate)) {
    showError("Пожалуйста, заполните все поля в группе 'В процессе лечения' или оставьте все поля пустыми");
    return;
  }
  
  if (dischargeHasValue && (!dischargeHeight || !dischargeWeight || !dischargeBP || !dischargeDate)) {
    showError("Пожалуйста, заполните все поля в группе 'При выписке' или оставьте все поля пустыми");
    return;
  }

  // Подготовка измерений с корректным приведением типов
  const measurements = [];
  
  if (admissionHasValue) {
    measurements.push({
      measurementType: 'При поступлении',
      height: parseFloat(admissionHeight),
      weight: parseFloat(admissionWeight),
      bloodPressure: admissionBP,
      measurementDate: admissionDate
    });
  }
  
  if (processHasValue) {
    measurements.push({
      measurementType: 'В процессе лечения',
      height: parseFloat(processHeight),
      weight: parseFloat(processWeight),
      bloodPressure: processBP,
      measurementDate: processDate
    });
  }
  
  if (dischargeHasValue) {
    measurements.push({
      measurementType: 'При выписке',
      height: parseFloat(dischargeHeight),
      weight: parseFloat(dischargeWeight),
      bloodPressure: dischargeBP,
      measurementDate: dischargeDate
    });
  }
  
  // Если есть измерения для сохранения
  if (measurements.length > 0) {
    const data = {
      patientId: patientId,
      measurements: measurements
    };
    
    fetch('/api/chief/patient-measurements', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(data)
    })
      .then(response => {
        // Клонируем ответ для возможности чтения тела ответа при ошибке
        const responseClone = response.clone();
        
        if (!response.ok) {
          // Пытаемся прочитать детальное сообщение ошибки из тела ответа
          return responseClone.json().then(errorData => {
            const errorMessage = errorData.message || 'Неизвестная ошибка при сохранении данных';
            throw new Error(errorMessage);
          }).catch(error => {
            // Если не удалось получить детальное сообщение, используем стандартное
            throw new Error(`Ошибка ${response.status}: Не удалось сохранить антропометрические данные`);
          });
        }
        
        return response.json();
      })
      .then(data => {
        showError('Данные успешно сохранены', 'success');
      })
      .catch(error => {
        console.error('Error saving measurements:', error);
        showError(error.message);
      });
  } else {
    showError('Нет данных для сохранения. Пожалуйста, введите хотя бы один полный набор измерений.', 'info');
  }
}

// Функция печати санаторной карты
function printMedCard() {
  const printWindow = window.open('', '_blank');
  
  if (!printWindow) {
    alert('Пожалуйста, разрешите всплывающие окна для этого сайта');
    return;
  }
  
  // Подготовка контента для печати
  let printContent = `
    <!DOCTYPE html>
    <html>
    <head>
      <title>Печать санаторной карты - ${patientData.fullName}</title>
      <style>
        body {
          font-family: Arial, sans-serif;
          margin: 20px;
          color: #000;
        }
        h1, h2, h3 {
          color: #4285f4;
          text-align: center;
        }
        .page-break {
          page-break-after: always;
        }
        .info-group {
          margin-bottom: 20px;
        }
        .info-row {
          display: flex;
          margin-bottom: 10px;
        }
        .info-label {
          min-width: 150px;
          font-weight: bold;
        }
        table {
          width: 100%;
          border-collapse: collapse;
        }
        th, td {
          border: 1px solid #000;
          padding: 8px;
          text-align: left;
        }
        th {
          background-color: #f2f2f2;
        }
        .section {
          margin-bottom: 30px;
        }
      </style>
    </head>
    <body>
      <h1>САНАТОРНАЯ КНИЖКА</h1>
      <h2>${patientData.fullName}</h2>
      
      <div class="section">
        <div class="info-group">
          <div class="info-row">
            <div class="info-label">Дата рождения:</div>
            <div>${formatDate(patientData.dateOfBirth)}</div>
          </div>
          <div class="info-row">
            <div class="info-label">Срок лечения:</div>
            <div>с ${formatDate(patientData.recordDate)} по ${formatDate(patientData.dischargeDate)}</div>
          </div>
          <div class="info-row">
            <div class="info-label">Тип размещения:</div>
            <div>${patientData.stayType || 'Не определен'}</div>
          </div>
          <div class="info-row">
            <div class="info-label">Проживание:</div>
            <div>${patientData.accommodation || 'Не определено'}</div>
          </div>
        </div>
        
        <h3>ДИАГНОЗЫ</h3>
        <table>
          <thead>
            <tr>
              <th>Наименование</th>
            </tr>
          </thead>
          <tbody>
  `;
  
  if (diagnoses.length > 0) {
    diagnoses.forEach(diagnosis => {
      printContent += `<tr><td>${diagnosis.diagnosisName}</td></tr>`;
    });
  } else {
    printContent += `<tr><td>Нет диагнозов</td></tr>`;
  }
  
  printContent += `
          </tbody>
        </table>
      </div>
      
      <div class="page-break"></div>
      
      <div class="section">
        <h3>АНТРОПОМЕТРИЧЕСКИЕ ИЗМЕРЕНИЯ</h3>
        
        <h4>При поступлении</h4>
        <div class="info-group">
          <div class="info-row">
            <div class="info-label">Рост (см):</div>
            <div>${document.getElementById('admissionHeight').value || '-'}</div>
          </div>
          <div class="info-row">
            <div class="info-label">Вес (кг):</div>
            <div>${document.getElementById('admissionWeight').value || '-'}</div>
          </div>
          <div class="info-row">
            <div class="info-label">Давление:</div>
            <div>${document.getElementById('admissionBP').value || '-'}</div>
          </div>
          <div class="info-row">
            <div class="info-label">Дата:</div>
            <div>${document.getElementById('admissionDate').value ? formatDate(document.getElementById('admissionDate').value) : '-'}</div>
          </div>
        </div>
        
        <h4>В процессе лечения</h4>
        <div class="info-group">
          <div class="info-row">
            <div class="info-label">Рост (см):</div>
            <div>${document.getElementById('processHeight').value || '-'}</div>
          </div>
          <div class="info-row">
            <div class="info-label">Вес (кг):</div>
            <div>${document.getElementById('processWeight').value || '-'}</div>
          </div>
          <div class="info-row">
            <div class="info-label">Давление:</div>
            <div>${document.getElementById('processBP').value || '-'}</div>
          </div>
          <div class="info-row">
            <div class="info-label">Дата:</div>
            <div>${document.getElementById('processDate').value ? formatDate(document.getElementById('processDate').value) : '-'}</div>
          </div>
        </div>
        
        <h4>При выписке</h4>
        <div class="info-group">
          <div class="info-row">
            <div class="info-label">Рост (см):</div>
            <div>${document.getElementById('dischargeHeight').value || '-'}</div>
          </div>
          <div class="info-row">
            <div class="info-label">Вес (кг):</div>
            <div>${document.getElementById('dischargeWeight').value || '-'}</div>
          </div>
          <div class="info-row">
            <div class="info-label">Давление:</div>
            <div>${document.getElementById('dischargeBP').value || '-'}</div>
          </div>
          <div class="info-row">
            <div class="info-label">Дата:</div>
            <div>${document.getElementById('dischargeDate').value ? formatDate(document.getElementById('dischargeDate').value) : '-'}</div>
          </div>
        </div>
        
        <h3>МЕДИКАМЕНТЫ</h3>
        <table>
          <thead>
            <tr>
              <th>Наименование</th>
              <th>Дозировка</th>
              <th>Инструкции</th>
              <th>Дата назначения</th>
            </tr>
          </thead>
          <tbody>
  `;
  
  if (medications.length > 0) {
    medications.forEach(medication => {
      printContent += `
        <tr>
          <td>${medication.medicationName}</td>
          <td>${medication.dosage || '-'}</td>
          <td>${medication.instructions || '-'}</td>
          <td>${formatDate(medication.prescribedDate)}</td>
        </tr>
      `;
    });
  } else {
    printContent += `<tr><td colspan="4">Нет назначенных медикаментов</td></tr>`;
  }
  
  printContent += `
          </tbody>
        </table>
      </div>
    </body>
    </html>
  `;
  
  printWindow.document.open();
  printWindow.document.write(printContent);
  printWindow.document.close();
  
  setTimeout(() => {
    printWindow.print();
  }, 500);
}

// Вспомогательная функция для форматирования даты
function formatDate(dateStr) {
  if (!dateStr) return '—';
  
  try {
    const date = new Date(dateStr);
    if (isNaN(date.getTime())) return '—';
    return date.toLocaleDateString('ru-RU');
  } catch (e) {
    return '—';
  }
}

// Форматирование даты и времени
function formatDateTime(dateStr) {
  if (!dateStr) return '—';
  
  try {
    const date = new Date(dateStr);
    if (isNaN(date.getTime())) return '—';
    return `${date.toLocaleDateString('ru-RU')} ${date.toLocaleTimeString('ru-RU', {hour: '2-digit', minute: '2-digit'})}`;
  } catch (e) {
    return '—';
  }
}

// Форматирование даты для input type="date"
function formatDateForInput(dateStr) {
  if (!dateStr) return '';
  
  try {
    const date = new Date(dateStr);
    if (isNaN(date.getTime())) return '';
    return date.toISOString().split('T')[0];
  } catch (e) {
    return '';
  }
}

// Показать уведомление
function showError(message, type = 'error') {
  // Создаем контейнер для уведомлений, если его еще нет
  let notificationContainer = document.querySelector('.notification-container');
  
  if (!notificationContainer) {
    notificationContainer = document.createElement('div');
    notificationContainer.className = 'notification-container';
    document.body.appendChild(notificationContainer);
    
    // Добавляем стили для контейнера уведомлений
    const style = document.createElement('style');
    style.textContent = `
      .notification-container {
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 9999;
      }
      .notification {
        padding: 15px;
        margin-bottom: 10px;
        border-radius: 4px;
        color: white;
        opacity: 0;
        transform: translateX(50px);
        transition: all 0.3s ease;
        max-width: 300px;
        box-shadow: 0 2px 5px rgba(0,0,0,0.2);
      }
      .notification.show {
        opacity: 1;
        transform: translateX(0);
      }
      .notification.success { background-color: #4caf50; }
      .notification.error { background-color: #f44336; }
      .notification.info { background-color: #2196f3; }
      .dark-theme .notification {
        box-shadow: 0 2px 5px rgba(0,0,0,0.5);
      }
    `;
    document.head.appendChild(style);
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