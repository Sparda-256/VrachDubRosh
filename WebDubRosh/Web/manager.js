document.addEventListener('DOMContentLoaded', function() {
  // Переменные для хранения данных
  let patients = [];
  let accompanyingPersons = [];
  let buildings = [];
  let accommodations = [];
  let documents = [];
  let selectedPatientIDs = [];
  let selectedAccompanyingIDs = [];
  let selectedAccommodationIDs = [];
  let selectedDocumentIDs = [];

  // Элементы DOM
  const themeToggle = document.getElementById('themeToggle');
  const patientsTable = document.getElementById('patientsTable');
  const accompanyingTable = document.getElementById('accompanyingTable');
  const accommodationTable = document.getElementById('accommodationTable');
  const documentsTable = document.getElementById('documentsTable');
  const searchPatients = document.getElementById('searchPatients');
  const searchAccompanying = document.getElementById('searchAccompanying');
  const searchDocuments = document.getElementById('searchDocuments');
  const buildingFilter = document.getElementById('buildingFilter');
  const roomStatusFilter = document.getElementById('roomStatusFilter');
  const documentCategoryFilter = document.getElementById('documentCategoryFilter');
  
  // Элементы статистики размещения
  const totalRoomsElement = document.getElementById('totalRooms');
  const availableBedsElement = document.getElementById('availableBeds');
  const occupiedBedsElement = document.getElementById('occupiedBeds');

  // Кнопки
  const exitBtn = document.getElementById('exitBtn');
  
  // Кнопки управления пациентами
  const addPatientBtn = document.getElementById('addPatientBtn');
  const editPatientBtn = document.getElementById('editPatientBtn');
  const deletePatientBtn = document.getElementById('deletePatientBtn');
  const manageDocumentsBtn = document.getElementById('manageDocumentsBtn');
  
  // Кнопки управления сопровождающими
  const addAccompanyingBtn = document.getElementById('addAccompanyingBtn');
  const editAccompanyingBtn = document.getElementById('editAccompanyingBtn');
  const deleteAccompanyingBtn = document.getElementById('deleteAccompanyingBtn');
  const manageAccompanyingDocumentsBtn = document.getElementById('manageAccompanyingDocumentsBtn');
  
  // Кнопки управления размещением
  const checkOutBtn = document.getElementById('checkOutBtn');
  const refreshAccommodationBtn = document.getElementById('refreshAccommodationBtn');
  
  // Кнопки управления документами
  const uploadDocumentBtn = document.getElementById('uploadDocumentBtn');
  const viewDocumentBtn = document.getElementById('viewDocumentBtn');
  const downloadDocumentBtn = document.getElementById('downloadDocumentBtn');
  const printDocumentBtn = document.getElementById('printDocumentBtn');
  const deleteDocumentBtn = document.getElementById('deleteDocumentBtn');

  // Модальные окна
  const addEditPatientModal = document.getElementById('addEditPatientModal');
  const addEditAccompanyingModal = document.getElementById('addEditAccompanyingModal');
  const documentsModal = document.getElementById('documentsModal');
  const uploadDocumentModal = document.getElementById('uploadDocumentModal');

  // Инициализация
  init();

  // Функция инициализации
  function init() {
    // Инициализация переключателя темы
    initThemeToggle();

    // Инициализация вкладок
    initTabs();

    // Инициализация модальных окон
    initModals();

    // Загрузка данных
    loadData();

    // Инициализация обработчиков событий
    initEventListeners();
  }

  // Инициализация переключателя темы
  function initThemeToggle() {
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
    const tabButtons = document.querySelectorAll('.tab-button');
    const tabPanes = document.querySelectorAll('.tab-pane');

    tabButtons.forEach(button => {
      button.addEventListener('click', () => {
        // Удаляем активный класс со всех кнопок и вкладок
        tabButtons.forEach(btn => btn.classList.remove('active'));
        tabPanes.forEach(pane => pane.classList.remove('active'));

        // Добавляем активный класс на выбранную кнопку
        button.classList.add('active');

        // Показываем соответствующую вкладку
        const tabId = button.dataset.tab;
        document.getElementById(tabId).classList.add('active');
      });
    });
  }

  // Инициализация модальных окон
  function initModals() {
    const closeButtons = document.querySelectorAll('.close-modal');

    closeButtons.forEach(button => {
      button.addEventListener('click', () => {
        const modal = button.closest('.modal');
        modal.style.display = 'none';
      });
    });

    // Закрытие модальных окон при клике вне содержимого
    window.addEventListener('click', (event) => {
      if (event.target.classList.contains('modal')) {
        event.target.style.display = 'none';
      }
    });
  }

  // Инициализация обработчиков событий
  function initEventListeners() {
    // Обработчики поиска
    searchPatients.addEventListener('input', filterPatients);
    searchAccompanying.addEventListener('input', filterAccompanying);
    searchDocuments.addEventListener('input', filterDocuments);

    // Обработчики фильтров
    buildingFilter.addEventListener('change', filterAccommodations);
    roomStatusFilter.addEventListener('change', filterAccommodations);
    documentCategoryFilter.addEventListener('change', filterDocuments);

    // Обработчики кнопок
    exitBtn.addEventListener('click', () => {
      window.location.href = 'login.html';
    });

    // Кнопки управления пациентами
    addPatientBtn.addEventListener('click', showAddPatientModal);
    editPatientBtn.addEventListener('click', showEditPatientModal);
    deletePatientBtn.addEventListener('click', deletePatient);
    manageDocumentsBtn.addEventListener('click', managePatientDocuments);

    // Кнопки управления сопровождающими
    addAccompanyingBtn.addEventListener('click', showAddAccompanyingModal);
    editAccompanyingBtn.addEventListener('click', showEditAccompanyingModal);
    deleteAccompanyingBtn.addEventListener('click', deleteAccompanying);
    manageAccompanyingDocumentsBtn.addEventListener('click', manageAccompanyingDocuments);

    // Кнопки управления размещением
    checkOutBtn.addEventListener('click', checkOutPerson);
    refreshAccommodationBtn.addEventListener('click', loadAccommodations);

    // Кнопки управления документами
    uploadDocumentBtn.addEventListener('click', showUploadDocumentModal);
    viewDocumentBtn.addEventListener('click', viewDocument);
    downloadDocumentBtn.addEventListener('click', downloadDocument);
    printDocumentBtn.addEventListener('click', printDocument);
    deleteDocumentBtn.addEventListener('click', deleteDocument);

    // Обработчики выбора строк в таблицах
    initTableSelectionListeners();
  }

  function initTableSelectionListeners() {
    // Пациенты
    patientsTable.addEventListener('click', function(e) {
      handleTableRowSelection(e, patientsTable, selectedPatientIDs, 'PatientID');
      updatePatientButtonStates();
    });

    patientsTable.addEventListener('dblclick', function(e) {
      const row = e.target.closest('tr');
      if (!row || row.parentElement.tagName === 'THEAD') return;
      showEditPatientModal();
    });

    // Сопровождающие
    accompanyingTable.addEventListener('click', function(e) {
      handleTableRowSelection(e, accompanyingTable, selectedAccompanyingIDs, 'AccompanyingPersonID');
      updateAccompanyingButtonStates();
    });

    accompanyingTable.addEventListener('dblclick', function(e) {
      const row = e.target.closest('tr');
      if (!row || row.parentElement.tagName === 'THEAD') return;
      showEditAccompanyingModal();
    });

    // Размещение
    accommodationTable.addEventListener('click', function(e) {
      handleTableRowSelection(e, accommodationTable, selectedAccommodationIDs, 'AccommodationID');
      updateAccommodationButtonStates();
    });

    // Документы
    documentsTable.addEventListener('click', function(e) {
      handleTableRowSelection(e, documentsTable, selectedDocumentIDs, 'DocumentID');
      updateDocumentButtonStates();
    });

    documentsTable.addEventListener('dblclick', function(e) {
      const row = e.target.closest('tr');
      if (!row || row.parentElement.tagName === 'THEAD') return;
      viewDocument();
    });
  }

  function handleTableRowSelection(e, table, selectedIDs, idField) {
    const row = e.target.closest('tr');
    if (!row || row.parentElement.tagName === 'THEAD') return;
    
    const id = parseInt(row.dataset.id);
    
    // Если нажата клавиша Ctrl или Shift, добавляем/удаляем из выбранных
    if (e.ctrlKey || e.shiftKey) {
      row.classList.toggle('selected');
      if (row.classList.contains('selected')) {
        if (!selectedIDs.includes(id)) {
          selectedIDs.push(id);
        }
      } else {
        const index = selectedIDs.indexOf(id);
        if (index !== -1) {
          selectedIDs.splice(index, 1);
        }
      }
    } else {
      // Иначе выбираем только текущую строку
      table.querySelectorAll('tbody tr').forEach(r => r.classList.remove('selected'));
      row.classList.add('selected');
      selectedIDs.length = 0;
      selectedIDs.push(id);
    }
  }

  // Загрузка данных
  function loadData() {
    Promise.all([
      loadPatients(),
      loadAccompanyingPersons(),
      loadBuildings(),
      loadAccommodations(),
      loadDocuments()
    ])
    .catch(error => {
      console.error('Error loading data:', error);
      alert('Ошибка загрузки данных. Пожалуйста, обновите страницу или повторите попытку позже.');
    });
  }

  // Загрузка списка пациентов
  function loadPatients() {
    return fetch('/api/manager/patients')
      .then(response => response.json())
      .then(data => {
        patients = data;
        renderPatients();
        updatePatientButtonStates();
      });
  }

  // Загрузка списка сопровождающих лиц
  function loadAccompanyingPersons() {
    return fetch('/api/manager/accompanyingpersons')
      .then(response => response.json())
      .then(data => {
        accompanyingPersons = data;
        renderAccompanyingPersons();
        updateAccompanyingButtonStates();
      });
  }

  // Загрузка списка корпусов
  function loadBuildings() {
    return fetch('/api/manager/buildings')
      .then(response => response.json())
      .then(data => {
        buildings = data;
        renderBuildingFilter();
      });
  }

  // Загрузка данных о размещении
  function loadAccommodations() {
    const buildingId = buildingFilter.value || '';
    const status = roomStatusFilter.value || 'all';
    
    return fetch(`/api/manager/accommodations?buildingId=${buildingId}&status=${status}`)
      .then(response => response.json())
      .then(data => {
        console.log('Received accommodations data:', data);
        
        // Преобразуем и очищаем данные от объектных значений
        accommodations = data.map(item => {
          // Создаем новый объект, принудительно преобразуя все значения в примитивы
          return {
            AccommodationID: typeof item.AccommodationID === 'object' ? 0 : Number(item.AccommodationID) || 0,
            BuildingNumber: typeof item.BuildingNumber === 'object' ? '-' : String(item.BuildingNumber || '-'),
            RoomNumber: typeof item.RoomNumber === 'object' ? '-' : String(item.RoomNumber || '-'),
            BedNumber: typeof item.BedNumber === 'object' ? '-' : String(item.BedNumber || '-'),
            Status: typeof item.Status === 'object' ? 'Свободно' : String(item.Status || 'Свободно'),
            PersonName: typeof item.PersonName === 'object' ? '-' : String(item.PersonName || '-'),
            PersonType: typeof item.PersonType === 'object' ? '-' : String(item.PersonType || '-'),
            CheckInDate: typeof item.CheckInDate === 'object' ? null : item.CheckInDate
          };
        });
        
        console.log('Processed accommodations data:', accommodations);
        
        renderAccommodations();
        updateAccommodationStats();
        updateAccommodationButtonStates();
      });
  }

  // Загрузка списка документов
  function loadDocuments() {
    const category = documentCategoryFilter.value || '';
    
    return fetch(`/api/manager/documents?category=${category}`)
      .then(response => response.json())
      .then(data => {
        console.log('Received documents data:', data);
        documents = data;
        
        // Применяем поисковый фильтр, если он установлен
        filterDocuments();
      })
      .catch(error => {
        console.error('Error loading documents:', error);
        alert('Ошибка при загрузке документов.');
      });
  }

  // Отображение пациентов
  function renderPatients() {
    const tbody = patientsTable.querySelector('tbody');
    tbody.innerHTML = '';
    
    patients.forEach(patient => {
      const row = document.createElement('tr');
      row.dataset.id = patient.PatientID;
      
      row.innerHTML = `
        <td>${patient.FullName}</td>
        <td>${formatDate(new Date(patient.DateOfBirth))}</td>
        <td>${patient.Gender}</td>
        <td>${formatDate(new Date(patient.RecordDate))}</td>
        <td>${patient.DischargeDate ? formatDate(new Date(patient.DischargeDate)) : '-'}</td>
        <td>${getDocumentStatusBadge(patient.DocumentsStatus)}</td>
      `;
      
      tbody.appendChild(row);
    });
  }

  // Отображение сопровождающих лиц
  function renderAccompanyingPersons() {
    const tbody = accompanyingTable.querySelector('tbody');
    tbody.innerHTML = '';
    
    accompanyingPersons.forEach(person => {
      const row = document.createElement('tr');
      row.dataset.id = person.AccompanyingPersonID;
      
      row.innerHTML = `
        <td>${person.FullName}</td>
        <td>${person.PatientName}</td>
        <td>${person.Relationship || '-'}</td>
        <td><input type="checkbox" disabled ${person.HasPowerOfAttorney ? 'checked' : ''}></td>
        <td>${getDocumentStatusBadge(person.DocumentsStatus)}</td>
      `;
      
      tbody.appendChild(row);
    });
  }

  // Отображение корпусов в фильтре
  function renderBuildingFilter() {
    buildingFilter.innerHTML = '<option value="">Все корпуса</option>';
    
    buildings.forEach(building => {
      const option = document.createElement('option');
      option.value = building.BuildingID;
      option.textContent = `Корпус ${building.BuildingNumber}`;
      buildingFilter.appendChild(option);
    });
  }

  // Отображение данных о размещении
  function renderAccommodations() {
    const tbody = accommodationTable.querySelector('tbody');
    tbody.innerHTML = '';
    
    accommodations.forEach(item => {
      // Создаем строку
      const row = document.createElement('tr');
      
      // Устанавливаем ID если он есть
      if (item.AccommodationID) {
        row.dataset.id = item.AccommodationID;
      }
      
      // Преобразуем номер места, который, судя по ошибке, может быть объектом
      let bedNumberDisplay = item.BedNumber;
      if (typeof bedNumberDisplay === 'object') {
        bedNumberDisplay = '-';
      }
      
      // Создаем содержимое строки
      row.innerHTML = `
        <td>${item.BuildingNumber}</td>
        <td>${item.RoomNumber}</td>
        <td>${bedNumberDisplay}</td>
        <td>${getStatusBadge(item.Status)}</td>
        <td>${item.PersonName}</td>
        <td>${item.PersonType}</td>
        <td>${item.CheckInDate ? formatDate(new Date(item.CheckInDate)) : '-'}</td>
      `;
      
      // Добавляем строку в таблицу
      tbody.appendChild(row);
    });
  }

  // Обновление статистики по размещению
  function updateAccommodationStats() {
    const totalRooms = new Set(accommodations.map(a => `${a.BuildingNumber}-${a.RoomNumber}`)).size;
    const availableBeds = accommodations.filter(a => a.Status === 'Свободно').length;
    const occupiedBeds = accommodations.filter(a => a.Status === 'Занято').length;
    
    if (totalRoomsElement) totalRoomsElement.textContent = totalRooms;
    if (availableBedsElement) availableBedsElement.textContent = availableBeds;
    if (occupiedBedsElement) occupiedBedsElement.textContent = occupiedBeds;
  }

  // Отображение документов
  function renderDocuments() {
    const tbody = documentsTable.querySelector('tbody');
    tbody.innerHTML = '';
    
    documents.forEach(doc => {
      const row = document.createElement('tr');
      row.dataset.id = doc.DocumentID;
      
      row.innerHTML = `
        <td>${doc.DocumentName}</td>
        <td>${doc.Category}</td>
        <td>${doc.FileType}</td>
        <td>${doc.FileSize}</td>
        <td>${formatDateTime(new Date(doc.UploadDate))}</td>
        <td>${doc.UploadedBy || 'Система'}</td>
      `;
      
      tbody.appendChild(row);
    });
    
    // Обновляем состояние кнопок после обновления таблицы
    updateDocumentButtonStates();
  }

  // Фильтрация пациентов
  function filterPatients() {
    const searchValue = searchPatients.value.toLowerCase();
    const tbody = patientsTable.querySelector('tbody');
    const rows = tbody.querySelectorAll('tr');
    
    rows.forEach(row => {
      const text = row.textContent.toLowerCase();
      if (text.includes(searchValue)) {
        row.style.display = '';
      } else {
        row.style.display = 'none';
      }
    });
  }

  // Фильтрация сопровождающих
  function filterAccompanying() {
    const searchValue = searchAccompanying.value.toLowerCase();
    const tbody = accompanyingTable.querySelector('tbody');
    const rows = tbody.querySelectorAll('tr');
    
    rows.forEach(row => {
      const text = row.textContent.toLowerCase();
      if (text.includes(searchValue)) {
        row.style.display = '';
      } else {
        row.style.display = 'none';
      }
    });
  }

  // Фильтрация размещения
  function filterAccommodations() {
    loadAccommodations();
  }

  // Фильтрация документов
  function filterDocuments() {
    // Если есть поисковый запрос, фильтруем локально
    if (searchDocuments && searchDocuments.value.trim() !== '') {
      const searchValue = searchDocuments.value.toLowerCase().trim();
      
      // Фильтруем документы по поисковому запросу
      const filteredDocuments = documents.filter(doc => 
        doc.DocumentName.toLowerCase().includes(searchValue) ||
        doc.Category.toLowerCase().includes(searchValue) ||
        doc.FileType.toLowerCase().includes(searchValue) ||
        (doc.UploadedBy && doc.UploadedBy.toLowerCase().includes(searchValue))
      );
      
      // Сохраняем отфильтрованные документы временно для отображения
      const originalDocuments = documents;
      documents = filteredDocuments;
      
      // Отображаем результаты
      renderDocuments();
      
      // Восстанавливаем оригинальный массив
      documents = originalDocuments;
    } else {
      // Если нет поиска, просто отображаем все документы
      renderDocuments();
    }
  }

  // Обновление состояния кнопок управления пациентами
  function updatePatientButtonStates() {
    const hasSelection = selectedPatientIDs.length > 0;
    
    editPatientBtn.disabled = !hasSelection || selectedPatientIDs.length > 1;
    deletePatientBtn.disabled = !hasSelection;
    manageDocumentsBtn.disabled = !hasSelection || selectedPatientIDs.length > 1;
  }

  // Обновление состояния кнопок управления сопровождающими
  function updateAccompanyingButtonStates() {
    const hasSelection = selectedAccompanyingIDs.length > 0;
    
    editAccompanyingBtn.disabled = !hasSelection || selectedAccompanyingIDs.length > 1;
    deleteAccompanyingBtn.disabled = !hasSelection;
    manageAccompanyingDocumentsBtn.disabled = !hasSelection || selectedAccompanyingIDs.length > 1;
  }

  // Обновление состояния кнопок управления размещением
  function updateAccommodationButtonStates() {
    const hasSelection = selectedAccommodationIDs.length > 0;
    const selectedItems = accommodations.filter(a => selectedAccommodationIDs.includes(a.AccommodationID));
    const hasOccupied = selectedItems.some(a => a.Status === 'Занято');
    
    checkOutBtn.disabled = !hasSelection || !hasOccupied;
  }

  // Обновление состояния кнопок управления документами
  function updateDocumentButtonStates() {
    const hasSelection = selectedDocumentIDs.length > 0;
    const hasSingleSelection = selectedDocumentIDs.length === 1;
    
    // Убедимся, что все элементы существуют перед изменением их состояния
    if (viewDocumentBtn) viewDocumentBtn.disabled = !hasSingleSelection;
    if (downloadDocumentBtn) downloadDocumentBtn.disabled = !hasSelection;
    if (printDocumentBtn) printDocumentBtn.disabled = !hasSingleSelection;
    if (deleteDocumentBtn) deleteDocumentBtn.disabled = !hasSelection;
  }

  // Показать модальное окно добавления пациента
  function showAddPatientModal() {
    // TODO: Реализовать отображение модального окна добавления пациента
    alert('Функция добавления пациента будет реализована в будущих версиях.');
  }

  // Показать модальное окно редактирования пациента
  function showEditPatientModal() {
    if (selectedPatientIDs.length !== 1) {
      alert('Выберите одного пациента для редактирования.');
      return;
    }
    // TODO: Реализовать отображение модального окна редактирования пациента
    alert('Функция редактирования пациента будет реализована в будущих версиях.');
  }

  // Удаление пациента
  function deletePatient() {
    if (selectedPatientIDs.length === 0) {
      alert('Выберите пациента для удаления.');
      return;
    }
    
    if (!confirm(`Вы уверены, что хотите удалить ${selectedPatientIDs.length} пациента(ов)?`)) {
      return;
    }
    
    // Удаляем пациентов по очереди
    const promises = selectedPatientIDs.map(patientID => {
      return fetch(`/api/manager/patient/${patientID}`, {
        method: 'DELETE'
      })
      .then(response => response.json());
    });
    
    Promise.all(promises)
      .then(results => {
        const successCount = results.filter(r => r.success).length;
        
        if (successCount > 0) {
          alert(`Успешно удалено ${successCount} пациента(ов).`);
          loadPatients();
          selectedPatientIDs = [];
        } else {
          alert('Не удалось удалить пациентов.');
        }
      })
      .catch(error => {
        console.error('Error deleting patients:', error);
        alert('Ошибка при удалении пациентов.');
      });
  }

  // Управление документами пациента
  function managePatientDocuments() {
    if (selectedPatientIDs.length !== 1) {
      alert('Выберите одного пациента для управления документами.');
      return;
    }
    
    const patientID = selectedPatientIDs[0];
    const patient = patients.find(p => p.PatientID === patientID);
    
    if (!patient) {
      alert('Пациент не найден.');
      return;
    }
    
    // TODO: Реализовать отображение модального окна управления документами пациента
    alert('Функция управления документами пациента будет реализована в будущих версиях.');
  }

  // Показать модальное окно добавления сопровождающего
  function showAddAccompanyingModal() {
    // TODO: Реализовать отображение модального окна добавления сопровождающего
    alert('Функция добавления сопровождающего будет реализована в будущих версиях.');
  }

  // Показать модальное окно редактирования сопровождающего
  function showEditAccompanyingModal() {
    if (selectedAccompanyingIDs.length !== 1) {
      alert('Выберите одного сопровождающего для редактирования.');
      return;
    }
    // TODO: Реализовать отображение модального окна редактирования сопровождающего
    alert('Функция редактирования сопровождающего будет реализована в будущих версиях.');
  }

  // Удаление сопровождающего
  function deleteAccompanying() {
    if (selectedAccompanyingIDs.length === 0) {
      alert('Выберите сопровождающего для удаления.');
      return;
    }
    
    if (!confirm(`Вы уверены, что хотите удалить ${selectedAccompanyingIDs.length} сопровождающего(их)?`)) {
      return;
    }
    
    // Удаляем сопровождающих по очереди
    const promises = selectedAccompanyingIDs.map(id => {
      return fetch(`/api/manager/accompanyingperson/${id}`, {
        method: 'DELETE'
      })
      .then(response => response.json());
    });
    
    Promise.all(promises)
      .then(results => {
        const successCount = results.filter(r => r.success).length;
        
        if (successCount > 0) {
          alert(`Успешно удалено ${successCount} сопровождающего(их).`);
          loadAccompanyingPersons();
          selectedAccompanyingIDs = [];
        } else {
          alert('Не удалось удалить сопровождающих.');
        }
      })
      .catch(error => {
        console.error('Error deleting accompanying persons:', error);
        alert('Ошибка при удалении сопровождающих.');
      });
  }

  // Управление документами сопровождающего
  function manageAccompanyingDocuments() {
    if (selectedAccompanyingIDs.length !== 1) {
      alert('Выберите одного сопровождающего для управления документами.');
      return;
    }
    
    const id = selectedAccompanyingIDs[0];
    const person = accompanyingPersons.find(p => p.AccompanyingPersonID === id);
    
    if (!person) {
      alert('Сопровождающий не найден.');
      return;
    }
    
    // TODO: Реализовать отображение модального окна управления документами сопровождающего
    alert('Функция управления документами сопровождающего будет реализована в будущих версиях.');
  }

  // Выселение человека
  function checkOutPerson() {
    if (selectedAccommodationIDs.length === 0) {
      alert('Выберите размещение для выселения.');
      return;
    }
    
    const selectedItems = accommodations.filter(a => selectedAccommodationIDs.includes(a.AccommodationID));
    const occupiedItems = selectedItems.filter(a => a.Status === 'Занято');
    
    if (occupiedItems.length === 0) {
      alert('Среди выбранных размещений нет занятых.');
      return;
    }
    
    if (!confirm(`Вы уверены, что хотите выселить ${occupiedItems.length} человека(ов)?`)) {
      return;
    }
    
    // Выселяем людей по очереди
    const promises = occupiedItems.map(item => {
      return fetch(`/api/manager/accommodation/${item.AccommodationID}/checkout`, {
        method: 'POST'
      })
      .then(response => response.json());
    });
    
    Promise.all(promises)
      .then(results => {
        const successCount = results.filter(r => r.success).length;
        
        if (successCount > 0) {
          alert(`Успешно выселено ${successCount} человека(ов).`);
          loadAccommodations();
          selectedAccommodationIDs = [];
        } else {
          alert('Не удалось выселить людей.');
        }
      })
      .catch(error => {
        console.error('Error checking out:', error);
        alert('Ошибка при выселении.');
      });
  }

  // Показать модальное окно загрузки документа
  function showUploadDocumentModal() {
    // Очищаем значения модального окна
    document.getElementById('documentName').value = '';
    document.getElementById('documentFile').value = '';
    document.getElementById('documentDescription').value = '';
    document.getElementById('documentCategory').value = 'administrative'; // Значение по умолчанию
    
    // Показываем модальное окно
    const modal = document.getElementById('uploadDocumentModal');
    modal.style.display = 'block';
    
    // Добавляем обработчик для кнопки загрузки
    const saveButton = document.getElementById('saveDocumentBtn');
    
    // Удаляем существующие обработчики, чтобы избежать дублирования
    const newSaveButton = saveButton.cloneNode(true);
    saveButton.parentNode.replaceChild(newSaveButton, saveButton);
    
    // Добавляем новый обработчик
    newSaveButton.addEventListener('click', uploadDocument);
  }
  
  // Загрузка документа
  function uploadDocument() {
    const documentName = document.getElementById('documentName').value;
    const documentCategory = document.getElementById('documentCategory').value;
    const documentFile = document.getElementById('documentFile').files[0];
    const documentDescription = document.getElementById('documentDescription').value;
    
    // Проверка заполнения обязательных полей
    if (!documentName || !documentFile) {
      alert('Пожалуйста, заполните название документа и выберите файл.');
      return;
    }
    
    // Создаем FormData для отправки файла
    const formData = new FormData();
    formData.append('file', documentFile);
    formData.append('documentName', documentName);
    formData.append('category', documentCategory);
    formData.append('description', documentDescription);
    
    // Отправляем запрос на загрузку
    fetch('/api/manager/document/upload', {
      method: 'POST',
      body: formData
    })
    .then(response => response.json())
    .then(result => {
      if (result.success) {
        // Закрываем модальное окно
        const modal = document.getElementById('uploadDocumentModal');
        modal.style.display = 'none';
        
        // Перезагружаем список документов
        loadDocuments();
        
        alert('Документ успешно загружен.');
      } else {
        alert(`Ошибка при загрузке документа: ${result.message}`);
      }
    })
    .catch(error => {
      console.error('Error uploading document:', error);
      alert('Ошибка при загрузке документа.');
    });
  }

  // Просмотр документа
  function viewDocument() {
    if (selectedDocumentIDs.length !== 1) {
      alert('Выберите один документ для просмотра.');
      return;
    }
    
    const documentID = selectedDocumentIDs[0];
    window.open(`/api/manager/document/${documentID}/view`, '_blank');
  }

  // Скачивание документа
  function downloadDocument() {
    if (selectedDocumentIDs.length === 0) {
      alert('Выберите документ для скачивания.');
      return;
    }
    
    selectedDocumentIDs.forEach(documentID => {
      window.open(`/api/manager/document/${documentID}/download`, '_blank');
    });
  }

  // Печать документа
  function printDocument() {
    if (selectedDocumentIDs.length !== 1) {
      alert('Выберите один документ для печати.');
      return;
    }
    
    const documentID = selectedDocumentIDs[0];
    window.open(`/api/manager/document/${documentID}/print`, '_blank');
  }

  // Удаление документа
  function deleteDocument() {
    if (selectedDocumentIDs.length === 0) {
      alert('Выберите документ для удаления.');
      return;
    }
    
    if (!confirm(`Вы уверены, что хотите удалить ${selectedDocumentIDs.length} документ(ов)?`)) {
      return;
    }
    
    // Удаляем документы по очереди
    const promises = selectedDocumentIDs.map(documentID => {
      return fetch(`/api/manager/document/${documentID}`, {
        method: 'DELETE'
      })
      .then(response => response.json());
    });
    
    Promise.all(promises)
      .then(results => {
        const successCount = results.filter(r => r.success).length;
        
        if (successCount > 0) {
          alert(`Успешно удалено ${successCount} документ(ов).`);
          loadDocuments();
          selectedDocumentIDs = [];
        } else {
          alert('Не удалось удалить документы.');
        }
      })
      .catch(error => {
        console.error('Error deleting documents:', error);
        alert('Ошибка при удалении документов.');
      });
  }

  // Вспомогательные функции
  function formatDate(date) {
    if (!(date instanceof Date) || isNaN(date)) return '-';
    return date.toLocaleDateString('ru-RU');
  }
  
  function formatDateTime(date) {
    if (!(date instanceof Date) || isNaN(date)) return '-';
    return `${date.toLocaleDateString('ru-RU')} ${date.toLocaleTimeString('ru-RU', {hour: '2-digit', minute:'2-digit'})}`;
  }
  
  function getStatusBadge(status) {
    let badgeClass;
    
    switch (status) {
      case 'Занято':
        badgeClass = 'status-badge-occupied';
        break;
      case 'Свободно':
        badgeClass = 'status-badge-free';
        break;
      case 'Частично занято':
        badgeClass = 'status-badge-partial';
        break;
      default:
        badgeClass = '';
    }
    
    return `<span class="status-badge ${badgeClass}">${status}</span>`;
  }
  
  function getDocumentStatusBadge(status) {
    let icon, color;
    
    switch (status) {
      case 'Полный комплект':
        icon = 'M21,7L9,19L3.5,13.5L4.91,12.09L9,16.17L19.59,5.59L21,7Z';
        color = 'var(--green-color)';
        break;
      case 'Частично':
        icon = 'M13,13H11V7H13M13,17H11V15H13M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z';
        color = 'var(--orange-color)';
        break;
      case 'Отсутствуют':
        icon = 'M12,2C17.53,2 22,6.47 22,12C22,17.53 17.53,22 12,22C6.47,22 2,17.53 2,12C2,6.47 6.47,2 12,2M15.59,7L12,10.59L8.41,7L7,8.41L10.59,12L7,15.59L8.41,17L12,13.41L15.59,17L17,15.59L13.41,12L17,8.41L15.59,7Z';
        color = 'var(--red-color)';
        break;
      default:
        icon = 'M13,9H11V7H13M13,17H11V11H13M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z';
        color = 'var(--gray-color)';
    }
    
    return `<svg viewBox="0 0 24 24" style="width: 24px; height: 24px; fill: ${color};">
              <path d="${icon}"></path>
            </svg>`;
  }
}); 