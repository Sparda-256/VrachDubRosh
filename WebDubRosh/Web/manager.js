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
  let selectedPowerOfAttorneyFile = null; // Переменная для хранения выбранного файла доверенности

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
  const deleteDocumentBtn = document.getElementById('deleteDocumentBtn');

  // Модальные окна
  const addEditPatientModal = document.getElementById('addEditPatientModal');
  const addEditAccompanyingModal = document.getElementById('addEditAccompanyingModal');
  const documentsModal = document.getElementById('documentsModal');
  const uploadDocumentModal = document.getElementById('uploadDocumentModal');

  // Инициализация
  init();

  // Функция для сортировки таблиц
  function addTableSorting(tableId, dataArray, renderFunction) {
    const table = document.getElementById(tableId);
    if (!table) return;
    
    const headers = table.querySelectorAll('thead th');
    
    // Сохраняем текущее состояние сортировки, если оно есть
    const currentSortColumn = table.getAttribute('data-sort-column');
    const currentSortDirection = table.getAttribute('data-sort-direction');
    
    // Удаляем существующие обработчики путем клонирования и замены заголовков
    headers.forEach((header, index) => {
      const clonedHeader = header.cloneNode(true);
      header.parentNode.replaceChild(clonedHeader, header);
      
      // Если у заголовка есть атрибут data-sort, сохраняем его
      if (header.hasAttribute('data-sort')) {
        clonedHeader.setAttribute('data-sort', header.getAttribute('data-sort'));
      }
    });
    
    // Получаем новые заголовки после замены
    const newHeaders = table.querySelectorAll('thead th');
    
    // Добавляем иконки и обработчики для заголовков таблицы
    newHeaders.forEach((header, index) => {
      // Добавляем класс для стилизации и указатель, что можно кликнуть
      header.classList.add('sortable');
      header.style.cursor = 'pointer';
      
      // Проверяем, есть ли уже иконка сортировки
      let iconSpan = header.querySelector('.sort-icon');
      
      // Если иконки нет, создаем её
      if (!iconSpan) {
        iconSpan = document.createElement('span');
        iconSpan.className = 'sort-icon';
        header.appendChild(iconSpan);
      }
      
      // Восстанавливаем состояние сортировки, если это колонка, по которой ранее сортировали
      if (currentSortColumn === index.toString() && currentSortDirection) {
        header.setAttribute('data-sort', currentSortDirection);
        iconSpan.textContent = currentSortDirection === 'asc' ? ' ▲' : ' ▼';
      }
      
      // Добавляем обработчик клика
      header.addEventListener('click', () => {
        // Определяем направление сортировки
        const currentDirection = header.getAttribute('data-sort') || 'none';
        const newDirection = currentDirection === 'asc' ? 'desc' : 'asc';
        
        // Сбрасываем направление сортировки у других столбцов
        newHeaders.forEach(h => {
          h.setAttribute('data-sort', 'none');
          const icon = h.querySelector('.sort-icon');
          if (icon) icon.textContent = '';
        });
        
        // Устанавливаем направление сортировки и иконку
        header.setAttribute('data-sort', newDirection);
        iconSpan.textContent = newDirection === 'asc' ? ' ▲' : ' ▼';
        
        // Сохраняем информацию о текущей сортировке
        table.setAttribute('data-sort-column', index);
        table.setAttribute('data-sort-direction', newDirection);
        
        // Получаем отсортированную копию массива данных
        const sortedArray = sortTable(dataArray, index, newDirection, tableId);
        
        // Определяем, какой массив данных временно заменить для отрисовки в зависимости от таблицы
        let originalArray;
        
        if (tableId === 'patientsTable') {
          originalArray = patients;
          patients = sortedArray;
        } else if (tableId === 'accompanyingTable') {
          originalArray = accompanyingPersons;
          accompanyingPersons = sortedArray;
        } else if (tableId === 'accommodationTable') {
          originalArray = accommodations;
          accommodations = sortedArray;
        } else if (tableId === 'documentsTable') {
          originalArray = documents;
          documents = sortedArray;
        }
        
        // Перерисовываем таблицу
        renderFunction();
        
        // Восстанавливаем оригинальный массив
        if (tableId === 'patientsTable') {
          patients = originalArray;
        } else if (tableId === 'accompanyingTable') {
          accompanyingPersons = originalArray;
        } else if (tableId === 'accommodationTable') {
          accommodations = originalArray;
        } else if (tableId === 'documentsTable') {
          documents = originalArray;
        }
      });
    });
  }

  function sortTable(dataArray, columnIndex, direction, tableId) {
    // Маппинг индексов столбцов на ключи в объектах данных
    const columnMappings = {
      'patientsTable': ['FullName', 'DateOfBirth', 'Gender', 'RecordDate', 'DischargeDate', 'DocumentsStatus'],
      'accompanyingTable': ['FullName', 'PatientName', 'Relationship', 'HasPowerOfAttorney', 'DocumentsStatus'],
      'accommodationTable': ['BuildingNumber', 'RoomNumber', 'BedNumber', 'Status', 'PersonName', 'PersonType', 'CheckInDate'],
      'documentsTable': ['DocumentName', 'Category', 'FileType', 'FileSize', 'UploadDate', 'UploadedBy']
    };
    
    const keys = columnMappings[tableId];
    if (!keys || !keys[columnIndex]) return dataArray; // Выходим, если не смогли определить ключ
    
    const key = keys[columnIndex];
    
    // Создаем копию массива для сортировки
    const sortedArray = [...dataArray];
    
    sortedArray.sort((a, b) => {
      let valueA = a[key];
      let valueB = b[key];
      
      // Обрабатываем разные типы данных
      if (key.includes('Date')) {
        // Для дат
        valueA = valueA ? new Date(valueA) : new Date(0);
        valueB = valueB ? new Date(valueB) : new Date(0);
      } else if (typeof valueA === 'string' && typeof valueB === 'string') {
        // Для строк, используем локальное сравнение
        return direction === 'asc' 
          ? valueA.localeCompare(valueB, 'ru', {sensitivity: 'base'})
          : valueB.localeCompare(valueA, 'ru', {sensitivity: 'base'});
      } else if (valueA === null || valueA === undefined) {
        // Пустые значения всегда в конец
        return direction === 'asc' ? 1 : -1;
      } else if (valueB === null || valueB === undefined) {
        return direction === 'asc' ? -1 : 1;
      } else if (typeof valueA === 'boolean' && typeof valueB === 'boolean') {
        // Для булевых значений
        valueA = valueA ? 1 : 0;
        valueB = valueB ? 1 : 0;
      }
      
      // Для чисел и дат
      return direction === 'asc' ? valueA - valueB : valueB - valueA;
    });
    
    // Возвращаем отсортированный массив
    return sortedArray;
  }

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

    // Инициализация модального окна пациента
    initPatientModal();

    // Привязываем обработчики к кнопкам
    document.getElementById('addPatientBtn').addEventListener('click', showAddPatientModal);
    document.getElementById('savePatientBtn').addEventListener('click', savePatient);
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
        // Сбрасываем выбранный файл при закрытии любого модального окна
        selectedPowerOfAttorneyFile = null; 
      });
    });

    // Закрытие модальных окон при клике вне содержимого
    window.addEventListener('click', (event) => {
      if (event.target.classList.contains('modal')) {
        event.target.style.display = 'none';
        // Сбрасываем выбранный файл при закрытии любого модального окна
        selectedPowerOfAttorneyFile = null; 
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
    
    // Обработчик для фильтра категории документов
    documentCategoryFilter.addEventListener('change', function() {
      console.log('Выбрана категория:', documentCategoryFilter.value);
      loadDocuments(); // Загружаем документы с выбранной категорией
    });

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
    manageAccompanyingDocumentsBtn.addEventListener('click', manageAccompanyingPersonDocuments);

    // Кнопки управления размещением
    checkOutBtn.addEventListener('click', checkOutPerson);
    refreshAccommodationBtn.addEventListener('click', loadAccommodations);

    // Кнопки управления документами
    uploadDocumentBtn.addEventListener('click', showUploadDocumentModal);
    viewDocumentBtn.addEventListener('click', viewDocument);
    downloadDocumentBtn.addEventListener('click', downloadDocument);
    deleteDocumentBtn.addEventListener('click', deleteDocument);

    // Обработчики выбора строк в таблицах
    initTableSelectionListeners();
    
    // Инициализация обработчиков для формы сопровождающего
    initAccompanyingFormEvents();
  }

  // Инициализация обработчиков событий формы сопровождающего
  function initAccompanyingFormEvents() {
    // Обработчик для выбора отношения к пациенту
    const relationshipSelect = document.getElementById('accompanyingRelationship');
    relationshipSelect.addEventListener('change', function() {
      const relationship = this.options[this.selectedIndex].text;
      // Получаем текущий статус доверенности (нужен ли он?)
      // const hasPoA = document.getElementById('powerOfAttorneyStatus').textContent === 'Загружена';
      // Передаем false, т.к. при смене отношения статус сбрасывается, пока не загружен новый файл
      updatePowerOfAttorneyUI(relationship, false);
    });

    // Обработчик для выбора пациента
    const patientSelect = document.getElementById('accompanyingPatient');
    patientSelect.addEventListener('change', function() {
      // Проверяем тип стационара выбранного пациента
      if (this.value && this.options[this.selectedIndex]) {
        const stayType = this.options[this.selectedIndex].dataset.stayType;
        console.log('Выбран пациент со стационаром:', stayType);

        // Показываем или скрываем секцию размещения
        const accommodationSection = document.getElementById('accompanyingAccommodationSection');

        if (stayType === 'Круглосуточный') {
          accommodationSection.style.display = 'block';

          // Если список корпусов пуст, загружаем их
          const buildingSelect = document.getElementById('accompanyingBuilding');
          if (buildingSelect.options.length <= 1) { // <=1 потому что есть опция "Выберите..."
            loadBuildingsForAccompanying()
              .catch(error => {
                console.error('Ошибка при загрузке корпусов:', error);
                showNotification('Ошибка при загрузке корпусов', 'error');
              });
          }
        } else {
          accommodationSection.style.display = 'none';
        }
      } else {
        // Если пациент не выбран, скрываем секцию размещения
        document.getElementById('accompanyingAccommodationSection').style.display = 'none';
      }
    });

    // Обработчик кнопки загрузки доверенности
    const uploadPowerOfAttorneyBtn = document.getElementById('uploadPowerOfAttorneyBtn');
    const powerOfAttorneyFileInput = document.getElementById('powerOfAttorneyFileInput');

    uploadPowerOfAttorneyBtn.addEventListener('click', function() {
        // Просто открываем диалог выбора файла
        powerOfAttorneyFileInput.click(); 
    });

    // Обработчик изменения файла в скрытом input
    powerOfAttorneyFileInput.addEventListener('change', handlePowerOfAttorneyUpload);


    // Обработчик кнопки сохранения
    const saveAccompanyingBtn = document.getElementById('saveAccompanyingBtn');
    saveAccompanyingBtn.addEventListener('click', saveAccompanyingPerson);

    // Обработчик для поиска пациента
    const patientSearchInput = document.getElementById('patientSearch');
    patientSearchInput.addEventListener('input', function() {
      initPatientSearch(); // Вызываем функцию фильтрации
    });

    // Загружаем список пациентов при инициализации
    // loadPatientsList(); // Этот вызов не нужен здесь, он делается в showAdd/Edit
  }

  // Новая функция для обработки загрузки файла доверенности
  function handlePowerOfAttorneyUpload(event) {
      const file = event.target.files[0];
      
      if (!file) {
          showNotification('Файл не выбран.', 'info');
          selectedPowerOfAttorneyFile = null; // Очищаем, если отменили выбор
          // Обновляем UI обратно, если нужно (например, если ранее был выбран файл)
          const accompanyingID = document.getElementById('accompanyingPersonID').value;
          const relationshipSelect = document.getElementById('accompanyingRelationship');
          const relationship = relationshipSelect.options[relationshipSelect.selectedIndex]?.text;
          // Проверяем, загружена ли доверенность на сервер (только в режиме редактирования)
          const isUploaded = accompanyingID ? document.getElementById('powerOfAttorneyStatus').textContent === 'Загружена' : false;
          updatePowerOfAttorneyUI(relationship, isUploaded); // Обновляем UI с учетом того, был ли файл загружен ранее
          return;
      }

      // Проверка типа файла (необязательно, но рекомендуется)
      const allowedTypes = ['application/pdf', 'image/jpeg', 'image/png', 'application/msword', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
      if (!allowedTypes.includes(file.type)) {
           showNotification('Недопустимый тип файла. Разрешены: PDF, JPG, PNG, DOC, DOCX.', 'error');
           selectedPowerOfAttorneyFile = null; // Сбрасываем файл
           event.target.value = null; // Очищаем input
           // Обновляем UI обратно
           const accompanyingID = document.getElementById('accompanyingPersonID').value;
           const relationshipSelect = document.getElementById('accompanyingRelationship');
           const relationship = relationshipSelect.options[relationshipSelect.selectedIndex]?.text;
           const isUploaded = accompanyingID ? document.getElementById('powerOfAttorneyStatus').textContent === 'Загружена' : false;
           updatePowerOfAttorneyUI(relationship, isUploaded);
           return;
      }

      // Сохраняем выбранный файл в переменную
      selectedPowerOfAttorneyFile = file;
      
      // Обновляем UI, показывая, что файл выбран
      const powerOfAttorneyStatus = document.getElementById('powerOfAttorneyStatus');
      powerOfAttorneyStatus.textContent = 'Файл выбран: ' + file.name;
      powerOfAttorneyStatus.style.color = 'var(--blue-color)'; // Синий цвет для выбранного файла
      showNotification('Файл доверенности выбран. Сохраните сопровождающего.', 'info');

      // Очищаем input file, чтобы можно было выбрать тот же файл еще раз
      event.target.value = null;
  }

  // Обновление UI доверенности
  function updatePowerOfAttorneyUI(relationship, hasPowerOfAttorney = false) {
    const powerOfAttorneyContainer = document.getElementById('powerOfAttorneyContainer');
    const powerOfAttorneyStatus = document.getElementById('powerOfAttorneyStatus');
    const uploadPowerOfAttorneyBtn = document.getElementById('uploadPowerOfAttorneyBtn');

    // Определяем, требуется ли доверенность
    const isPowerOfAttorneyRequired = relationship !== 'Родитель' && relationship !== 'Опекун';

    if (isPowerOfAttorneyRequired) {
      powerOfAttorneyContainer.style.display = 'block'; // Показываем контейнер
      uploadPowerOfAttorneyBtn.style.display = 'inline-block'; // Показываем кнопку

      // Кнопка должна быть доступна только в режиме редактирования
      const accompanyingID = document.getElementById('accompanyingPersonID').value;
      uploadPowerOfAttorneyBtn.disabled = !accompanyingID; // Отключаем, если ID пуст

      if (hasPowerOfAttorney) {
        powerOfAttorneyStatus.textContent = 'Загружена';
        powerOfAttorneyStatus.style.color = 'var(--green-color)'; // Зеленый
      } else {
        powerOfAttorneyStatus.textContent = 'Требуется загрузить';
        powerOfAttorneyStatus.style.color = 'var(--red-color)'; // Красный
      }
    } else {
       powerOfAttorneyContainer.style.display = 'none'; // Скрываем весь блок
      // uploadPowerOfAttorneyBtn.style.display = 'none';
      // powerOfAttorneyStatus.textContent = 'Не требуется';
      // powerOfAttorneyStatus.style.color = 'var(--gray-color)'; // Серый
    }
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
            BedNumber: item.BedNumber === null ? '-' : (typeof item.BedNumber === 'object' ? '-' : String(item.BedNumber)),
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
    
    console.log('Загрузка документов с категорией:', category);
    console.log('URL запроса:', `/api/manager/documents?category=${encodeURIComponent(category)}`);
    
    return fetch(`/api/manager/documents?category=${encodeURIComponent(category)}`)
      .then(response => {
        console.log('Статус ответа:', response.status);
        return response.json();
      })
      .then(data => {
        console.log('Получено документов:', data.length);
        
        // Покажем структуру первого документа, если он есть
        if (data.length > 0) {
          console.log('Структура документа:');
          for (const key in data[0]) {
            console.log(`${key}: ${typeof data[0][key]} - ${data[0][key]}`);
          }
        }
        
        documents = data;
        renderDocuments();
      })
      .catch(error => {
        console.error('Ошибка при загрузке документов:', error);
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
    
    // Повторно инициализируем сортировку после обновления данных
    addTableSorting('patientsTable', patients, renderPatients);
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
    
    // Повторно инициализируем сортировку после обновления данных
    addTableSorting('accompanyingTable', accompanyingPersons, renderAccompanyingPersons);
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
      
      // Логируем данные для отладки
      console.log(`Отображение кровати: ${item.BedNumber}, тип: ${typeof item.BedNumber}`);
      
      // Создаем содержимое строки
      row.innerHTML = `
        <td>${item.BuildingNumber}</td>
        <td>${item.RoomNumber}</td>
        <td>${item.BedNumber}</td>
        <td>${getStatusBadge(item.Status)}</td>
        <td>${item.PersonName}</td>
        <td>${item.PersonType}</td>
        <td>${item.CheckInDate ? formatDate(new Date(item.CheckInDate)) : '-'}</td>
      `;
      
      // Добавляем строку в таблицу
      tbody.appendChild(row);
    });
    
    // Повторно инициализируем сортировку после обновления данных
    addTableSorting('accommodationTable', accommodations, renderAccommodations);
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
    
    // Очищаем таблицу
    tbody.innerHTML = '';
    
    console.log(`Отрисовка ${documents.length} документов`);
    
    if (documents.length === 0) {
      // Если документов нет, показываем сообщение
      const row = document.createElement('tr');
      row.innerHTML = `<td colspan="6" class="text-center">Документы не найдены</td>`;
      tbody.appendChild(row);
    } else {
      // Отображаем документы
      documents.forEach(doc => {
        const row = document.createElement('tr');
        row.dataset.id = doc.DocumentID;
        
        // Получаем отображаемый тип файла на основе фактического расширения
        const displayFileType = getDisplayFileType(doc.FileType);
        
        row.innerHTML = `
          <td>${doc.DocumentName || '-'}</td>
          <td>${doc.Category || '-'}</td>
          <td>${displayFileType}</td>
          <td>${doc.FileSize || '-'}</td>
          <td>${formatDateTime(new Date(doc.UploadDate))}</td>
          <td>${doc.UploadedBy || 'Система'}</td>
        `;
        
        tbody.appendChild(row);
      });
    }
    
    // Обновляем состояние кнопок после обновления таблицы
    updateDocumentButtonStates();
    
    // Повторно инициализируем сортировку после обновления данных
    addTableSorting('documentsTable', documents, renderDocuments);
  }

  // Функция для получения отображаемого типа файла
  function getDisplayFileType(fileExtension) {
    const extension = fileExtension.toLowerCase();
    
    switch (extension) {
      case 'pdf':
        return 'PDF документ';
      case 'doc':
      case 'docx':
        return 'Документ Word';
      case 'xls':
      case 'xlsx':
        return 'Таблица Excel';
      case 'ppt':
      case 'pptx':
        return 'Презентация';
      case 'txt':
        return 'Текстовый документ';
      case 'jpg':
      case 'jpeg':
        return 'Изображение JPEG';
      case 'png':
        return 'Изображение PNG';
      case 'gif':
        return 'Изображение GIF';
      default:
        return extension.toUpperCase();
    }
  }

  // Фильтрация пациентов
  function filterPatients() {
    const searchValue = searchPatients.value.toLowerCase();
    const filteredPatients = patients.filter(patient => {
      const patientText = `${patient.FullName} ${formatDate(new Date(patient.DateOfBirth))} ${patient.Gender} ${formatDate(new Date(patient.RecordDate))} ${patient.DischargeDate ? formatDate(new Date(patient.DischargeDate)) : ''}`.toLowerCase();
      return patientText.includes(searchValue);
    });
    
    // Временно заменяем массив patients на отфильтрованный для отрисовки
    const originalPatients = patients.slice(); // Создаем копию оригинального массива
    patients = filteredPatients;
    
    // Отрисовываем отфильтрованные данные
    renderPatients();
    
    // Восстанавливаем оригинальный массив
    patients = originalPatients;
  }

  // Фильтрация сопровождающих
  function filterAccompanying() {
    const searchValue = searchAccompanying.value.toLowerCase();
    const filteredAccompanying = accompanyingPersons.filter(person => {
      const personText = `${person.FullName} ${person.PatientName} ${person.Relationship || ''}`.toLowerCase();
      return personText.includes(searchValue);
    });
    
    // Временно заменяем массив accompanyingPersons на отфильтрованный для отрисовки
    const originalAccompanying = accompanyingPersons.slice(); // Создаем копию оригинального массива
    accompanyingPersons = filteredAccompanying;
    
    // Отрисовываем отфильтрованные данные
    renderAccompanyingPersons();
    
    // Восстанавливаем оригинальный массив
    accompanyingPersons = originalAccompanying;
  }

  // Фильтрация размещения
  function filterAccommodations() {
    loadAccommodations();
  }

  // Фильтрация документов
  function filterDocuments() {
    // Если поиск не инициализирован, выходим из функции
    if (!searchDocuments) {
      console.error('Элемент поиска документов не найден');
      return;
    }
    
    console.log('Функция filterDocuments вызвана');
    console.log('Поисковый запрос:', searchDocuments.value);
    console.log('Всего документов перед фильтрацией:', documents.length);
    
    // Получаем значение поиска
    const searchValue = (searchDocuments.value || '').toLowerCase().trim();
    
    // Если есть поисковый запрос, фильтруем локально
    if (searchValue !== '') {
      console.log('Используемый поисковый запрос:', searchValue);
      
      // Фильтруем документы по поисковому запросу
      const filteredDocuments = documents.filter(doc => {
        // Проверки на null и undefined
        const docName = (doc.DocumentName || '').toLowerCase();
        const docCategory = (doc.Category || '').toLowerCase();
        const docType = (doc.FileType || '').toLowerCase();
        const docUploader = (doc.UploadedBy || '').toLowerCase();
        
        // Проверка на совпадение
        const nameMatch = docName.includes(searchValue);
        const categoryMatch = docCategory.includes(searchValue);
        const typeMatch = docType.includes(searchValue);
        const uploaderMatch = docUploader.includes(searchValue);
        
        // Отображаем для отладки
        if (nameMatch || categoryMatch || typeMatch || uploaderMatch) {
          console.log(`Найдено совпадение: ${doc.DocumentName || 'Без имени'}`);
        }
        
        return nameMatch || categoryMatch || typeMatch || uploaderMatch;
      });
      
      console.log('Количество документов после фильтрации:', filteredDocuments.length);
      
      // Временно заменяем массив документов для отображения
      const originalDocuments = documents.slice(); // Создаем копию оригинального массива
      documents = filteredDocuments;
      
      // Отображаем результаты
      renderDocuments();
      
      // Восстанавливаем оригинальный массив
      documents = originalDocuments;
    } else {
      console.log('Поисковый запрос пустой, отображаем все документы');
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
    const hasSingleSelection = selectedAccompanyingIDs.length === 1;
    
    editAccompanyingBtn.disabled = !hasSingleSelection;
    deleteAccompanyingBtn.disabled = !hasSelection;
    if (manageAccompanyingDocumentsBtn) manageAccompanyingDocumentsBtn.disabled = !hasSingleSelection;
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
    if (deleteDocumentBtn) deleteDocumentBtn.disabled = !hasSelection;
  }

  // Показать модальное окно добавления пациента
  function showAddPatientModal() {
    // Очищаем атрибут ID пациента и устанавливаем заголовок
    document.getElementById('patientModalTitle').textContent = 'Добавление пациента';
    const saveButton = document.getElementById('savePatientBtn');
    saveButton.dataset.patientId = '';
    saveButton.textContent = 'Сохранить';
    
    // Очищаем форму
    document.getElementById('patientFullName').value = '';
    document.getElementById('patientDateOfBirth').value = '';
    document.getElementById('patientGender').value = 'Мужской';
    document.getElementById('patientStayType').value = 'Дневной';
    document.getElementById('patientRecordDate').valueAsDate = new Date();
    document.getElementById('patientDischargeDate').value = '';
    
    // Скрываем секцию размещения
    document.getElementById('accommodationSection').style.display = 'none';
    
    // Отображаем модальное окно
    document.getElementById('patientModal').style.display = 'block';
  }

  // Показать модальное окно редактирования пациента
  function showEditPatientModal() {
    if (selectedPatientIDs.length !== 1) {
      showNotification('Выберите одного пациента для редактирования', 'error');
      return;
    }
    
    const patientID = selectedPatientIDs[0];
    
    // Показываем индикатор загрузки
    document.getElementById('patientModalTitle').textContent = 'Редактирование пациента';
    document.getElementById('patientModal').style.display = 'block';
    showNotification('Загрузка данных пациента...', 'info');
    
    // Загружаем данные пациента
    loadPatientData(patientID);
  }

  // Загрузка данных пациента для редактирования
  function loadPatientData(patientID) {
    fetch(`/api/manager/patient/${patientID}`)
      .then(response => {
        if (!response.ok) {
          return response.text().then(text => {
            try {
              const errorData = JSON.parse(text);
              throw new Error(errorData.message || 'Ошибка при загрузке данных пациента');
            } catch (e) {
              throw new Error('Ошибка при загрузке данных пациента: ' + text);
            }
          });
        }
        return response.json();
      })
      .then(data => {
        if (!data.success) {
          throw new Error(data.message || 'Не удалось загрузить данные пациента');
        }
        
        const patient = data.patient;
        console.log('Загружены данные пациента:', patient);
        
        // Заполняем форму данными пациента
        document.getElementById('patientFullName').value = patient.fullName;
        document.getElementById('patientGender').value = patient.gender;
        
        // Преобразуем дату рождения в формат YYYY-MM-DD для поля input[type="date"]
        if (patient.dateOfBirth) {
          // Используем правильное форматирование даты без смещения часового пояса
          const dob = new Date(patient.dateOfBirth);
          const year = dob.getFullYear();
          const month = String(dob.getMonth() + 1).padStart(2, '0');
          const day = String(dob.getDate()).padStart(2, '0');
          const dobFormatted = `${year}-${month}-${day}`;
          document.getElementById('patientDateOfBirth').value = dobFormatted;
        }
        
        // Устанавливаем дату записи
        if (patient.recordDate) {
          const recordDate = new Date(patient.recordDate);
          const year = recordDate.getFullYear();
          const month = String(recordDate.getMonth() + 1).padStart(2, '0');
          const day = String(recordDate.getDate()).padStart(2, '0');
          const recordDateFormatted = `${year}-${month}-${day}`;
          document.getElementById('patientRecordDate').value = recordDateFormatted;
        }
        
        // Устанавливаем дату выписки, если есть
        if (patient.dischargeDate) {
          const dischargeDate = new Date(patient.dischargeDate);
          const year = dischargeDate.getFullYear();
          const month = String(dischargeDate.getMonth() + 1).padStart(2, '0');
          const day = String(dischargeDate.getDate()).padStart(2, '0');
          const dischargeDateFormatted = `${year}-${month}-${day}`;
          document.getElementById('patientDischargeDate').value = dischargeDateFormatted;
        } else {
          document.getElementById('patientDischargeDate').value = '';
        }
        
        // Устанавливаем тип стационара
        document.getElementById('patientStayType').value = patient.stayType;
        
        // Показываем или скрываем секцию размещения в зависимости от типа стационара
        const accommodationSection = document.getElementById('accommodationSection');
        if (patient.stayType === 'Круглосуточный') {
          accommodationSection.style.display = 'block';
        } else {
          accommodationSection.style.display = 'none';
        }
        
        // Если пациент в круглосуточном стационаре и есть данные о размещении
        if (patient.stayType === 'Круглосуточный' && patient.accommodationInfo) {
          showNotification('Загрузка данных размещения...', 'info');
          
          // Загружаем данные о размещении
          loadAccommodationData(patient, patientID);
        }
        
        // Меняем текст кнопки сохранения и добавляем атрибут ID пациента
        const saveButton = document.getElementById('savePatientBtn');
        saveButton.textContent = 'Сохранить изменения';
        saveButton.dataset.patientId = patientID;
        
        // Скрываем индикатор загрузки
        showNotification('Данные пациента загружены', 'success');
      })
      .catch(error => {
        console.error('Ошибка при загрузке данных пациента:', error);
        showNotification(error.message, 'error');
      });
  }

  // Новая функция для загрузки данных размещения пациента
  function loadAccommodationData(patient, patientID) {
    // Загружаем корпуса
    fetch('/api/manager/buildings')
      .then(response => response.json())
      .then(buildings => {
        console.log('Загружены данные о корпусах:', buildings);
        const buildingSelect = document.getElementById('patientBuilding');
        buildingSelect.innerHTML = '<option value="">Выберите корпус</option>';
        
        buildings.forEach(building => {
          const option = document.createElement('option');
          option.value = building.BuildingID;
          option.textContent = `Корпус ${building.BuildingNumber}`;
          buildingSelect.appendChild(option);
        });
        
        // Находим корпус для выбранной комнаты
        return fetch(`/api/manager/rooms/${patient.accommodationInfo.roomID}/building`);
      })
      .then(response => response.json())
      .then(data => {
        if (!data.success) {
          throw new Error('Не удалось получить данные о корпусе');
        }
        
        // Выбираем корпус
        const buildingID = data.buildingID;
        const buildingSelect = document.getElementById('patientBuilding');
        buildingSelect.value = buildingID;
        
        // Загружаем комнаты для выбранного корпуса, передавая ID пациента
        // чтобы на сервере учитывалось, что его кровать должна быть доступна для него
        return fetch(`/api/manager/rooms/${buildingID}?patientID=${patientID}`);
      })
      .then(response => response.json())
      .then(rooms => {
        console.log('Загружены комнаты:', rooms);
        const roomSelect = document.getElementById('patientRoom');
        roomSelect.innerHTML = '<option value="">Выберите комнату</option>';
        
        if (!rooms || rooms.length === 0) {
          document.getElementById('patientBed').innerHTML = '<option value="">Нет доступных комнат</option>';
          throw new Error('Нет доступных комнат в выбранном корпусе');
        }
        
        // Заполняем список комнат
        rooms.forEach(room => {
          const option = document.createElement('option');
          
          // Получаем ID комнаты с учетом возможных разных имен свойств
          const roomId = room.RoomID || room.roomID || room.roomId || room.id;
          
          // Получаем номер комнаты
          const roomNumber = room.RoomNumber || room.roomNumber || room.number || '';
          
          // Получаем список доступных кроватей
          const availableBeds = room.AvailableBeds || room.availableBeds || room.beds || [];
          
          if (!roomId) {
            console.error("Не удалось определить ID комнаты:", room);
            return; // Пропускаем эту комнату
          }
          
          option.value = roomId;
          option.textContent = `Комната ${roomNumber}`;
          option.dataset.availableBeds = JSON.stringify(availableBeds);
          roomSelect.appendChild(option);
        });
        
        // Выбираем комнату пациента
        roomSelect.value = patient.accommodationInfo.roomID;
        
        // Получаем доступные кровати для выбранной комнаты
        const selectedOption = roomSelect.options[roomSelect.selectedIndex];
        let availableBeds = [];
        
        try {
          if (selectedOption && selectedOption.dataset.availableBeds) {
            availableBeds = JSON.parse(selectedOption.dataset.availableBeds);
          }
        } catch (e) {
          console.error('Ошибка при парсинге JSON доступных кроватей:', e);
        }
        
        // Обновляем список кроватей
        const bedSelect = document.getElementById('patientBed');
        bedSelect.innerHTML = '<option value="">Выберите кровать</option>';
        
        // Проверяем, что availableBeds является массивом
        if (!Array.isArray(availableBeds)) {
          console.error('Ошибка: availableBeds не является массивом:', availableBeds);
          availableBeds = [];
        }
        
        // Убедимся, что текущая кровать пациента есть в списке
        const currentBedNumber = patient.accommodationInfo.bedNumber;
        if (!availableBeds.includes(currentBedNumber)) {
          availableBeds.push(currentBedNumber);
        }
        
        // Заполняем выпадающий список кроватей
        availableBeds.forEach(bedNumber => {
          const option = document.createElement('option');
          option.value = bedNumber;
          option.textContent = `Кровать ${bedNumber}`;
          bedSelect.appendChild(option);
        });
        
        // Выбираем кровать пациента
        bedSelect.value = currentBedNumber;
        
        showNotification('Данные размещения загружены', 'success');
      })
      .catch(error => {
        console.error('Ошибка при загрузке данных размещения:', error);
        showNotification('Ошибка при загрузке данных размещения: ' + error.message, 'error');
      });
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
    
    // Показываем уведомление о начале процесса
    showNotification('Выполняется удаление пациентов...', 'info');
    
    // Массив для хранения результатов удаления
    const results = {
      success: [],
      failed: []
    };
    
    // Функция для удаления одного пациента
    function deleteOnePatient(patientID) {
      console.log(`Отправка запроса на удаление пациента ID: ${patientID}`);
      
      return fetch(`/api/manager/patient/${patientID}`, {
        method: 'DELETE'
      })
      .then(response => {
        console.log(`Получен ответ для пациента ID: ${patientID}, статус: ${response.status}`);
        
        if (!response.ok) {
          return response.text().then(text => {
            console.error(`Ошибка при удалении пациента ID: ${patientID}, текст: ${text}`);
            
            try {
              const errorData = JSON.parse(text);
              throw new Error(errorData.message || `Ошибка при удалении пациента (ID: ${patientID})`);
            } catch (e) {
              throw new Error(`Ошибка при удалении пациента (ID: ${patientID}): ${text}`);
            }
          });
        }
        
        return response.json();
      })
      .then(data => {
        console.log(`Успешный ответ для пациента ID: ${patientID}:`, data);
        
        if (data.success) {
          results.success.push(patientID);
          return true;
        } else {
          results.failed.push({ id: patientID, reason: data.message || 'Неизвестная ошибка' });
          return false;
        }
      })
      .catch(error => {
        console.error(`Ошибка при удалении пациента ID: ${patientID}:`, error);
        results.failed.push({ id: patientID, reason: error.message });
        return false;
      });
    }
    
    // Удаляем пациентов последовательно, а не параллельно
    // Это поможет избежать проблем с одновременными транзакциями в БД
    let promise = Promise.resolve();
    
    selectedPatientIDs.forEach(patientID => {
      promise = promise.then(() => deleteOnePatient(patientID));
    });
    
    promise
      .then(() => {
        console.log('Все операции удаления завершены:', results);
        
        // Формируем сообщение для пользователя
        let message = '';
        
        if (results.success.length > 0) {
          message += `Успешно удалено: ${results.success.length} пациент(ов).`;
        }
        
        if (results.failed.length > 0) {
          if (message) message += '\n\n';
          message += `Не удалось удалить: ${results.failed.length} пациент(ов).`;
          
          // Добавляем причины ошибок (до 3 штук, чтобы не перегружать сообщение)
          const failReasons = results.failed.slice(0, 3).map(f => `ID ${f.id}: ${f.reason}`);
          message += '\n' + failReasons.join('\n');
          
          if (results.failed.length > 3) {
            message += '\n...и еще ' + (results.failed.length - 3) + ' пациент(ов)';
          }
        }
        
        if (results.success.length > 0) {
          showNotification(message.split('\n')[0], 'success');
          
          // Обновляем списки
          loadPatients();
          loadAccommodations(); // Обновляем также размещение, т.к. оно могло измениться
          loadAccompanyingPersons(); // Обновляем список сопровождающих
          
          // Очищаем выбранные идентификаторы
          selectedPatientIDs = [];
        } else {
          showNotification('Не удалось удалить ни одного пациента', 'error');
        }
        
        // Если были ошибки, показываем полное сообщение в диалоговом окне
        if (results.failed.length > 0) {
          alert(message);
        }
      })
      .catch(error => {
        console.error('Общая ошибка при удалении пациентов:', error);
        showNotification('Произошла ошибка при удалении пациентов', 'error');
        alert('Ошибка при удалении пациентов: ' + error.message);
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
    
    // Загрузка документов пациента
    loadPatientDocuments(patientID, patient.FullName);
  }

  // Загрузка документов пациента
  function loadPatientDocuments(patientID, patientName) {
    showNotification('Загрузка документов пациента...', 'info');
    
    // Устанавливаем заголовок модального окна
    document.getElementById('patientDocumentsModalTitle').textContent = `Документы пациента: ${patientName}`;
    
    // Показываем модальное окно
    document.getElementById('patientDocumentsModal').style.display = 'block';
    
    // Очищаем таблицу
    const tbody = document.getElementById('patientDocumentsTable').querySelector('tbody');
    tbody.innerHTML = '<tr><td colspan="4" style="text-align: center;">Загрузка данных...</td></tr>';
    
    // Загружаем документы пациента с сервера
    fetch(`/api/manager/patient/${patientID}/documents`)
      .then(response => response.json())
      .then(data => {
        if (!data.success) {
          throw new Error(data.message || 'Не удалось загрузить документы пациента');
        }
        
        // Обновляем информацию о возрасте
        document.getElementById('patientDocumentsAgeInfo').textContent = `Возраст: ${data.patientAge} лет`;
        
        // Обновляем статус документов
        const statusElement = document.getElementById('patientDocumentsStatus');
        statusElement.textContent = data.documentStatus;
        
        // Меняем цвет в зависимости от статуса
        if (data.documentStatus === 'Полный комплект') {
          statusElement.style.color = '#4CAF50'; // Зеленый
        } else if (data.documentStatus === 'Нет обязательных документов') {
          statusElement.style.color = '#757575'; // Серый
        } else {
          statusElement.style.color = '#F44336'; // Красный
        }
        
        // Очищаем таблицу
        tbody.innerHTML = '';
        
        if (data.documents.length === 0) {
          tbody.innerHTML = '<tr><td colspan="4" style="text-align: center;">Нет документов, соответствующих возрасту пациента</td></tr>';
          return;
        }
        
        // Заполняем таблицу документами
        data.documents.forEach(doc => {
          const row = document.createElement('tr');
          row.dataset.documentTypeId = doc.DocumentTypeID;
          row.dataset.documentId = doc.DocumentID !== null ? doc.DocumentID : '';
          
          // Создаем ячейки таблицы
          row.innerHTML = `
            <td>${doc.DocumentName}</td>
            <td>${doc.Status}</td>
            <td>${doc.UploadDate ? formatDate(new Date(doc.UploadDate)) : '-'}</td>
            <td>${doc.IsRequired ? 'Да' : 'Нет'}</td>
          `;
          
          // Стилизуем строку в зависимости от статуса
          if (doc.Status === 'Проверен') {
            row.style.color = '#4CAF50'; // Зеленый
          } else if (doc.Status === 'Загружен') {
            row.style.color = '#2196F3'; // Синий
          }
          
          tbody.appendChild(row);
        });
        
        // Инициализируем обработчики событий для таблицы
        initPatientDocumentsTableListeners();
        
        // Сохраняем ID пациента для дальнейшего использования
        const uploadPatientIDInput = document.getElementById('uploadPatientID');
        if (uploadPatientIDInput) {
          uploadPatientIDInput.value = patientID;
        }
        
        showNotification('Документы пациента загружены', 'success');
      })
      .catch(error => {
        console.error('Ошибка при загрузке документов пациента:', error);
        tbody.innerHTML = `<tr><td colspan="4" style="text-align: center; color: red;">Ошибка: ${error.message}</td></tr>`;
        showNotification('Ошибка при загрузке документов пациента', 'error');
      });
  }

  // Инициализация обработчиков событий таблицы документов пациента
  function initPatientDocumentsTableListeners() {
    const table = document.getElementById('patientDocumentsTable');
    
    // Массив для хранения выбранных строк
    const selectedRows = [];
    
    // Очищаем предыдущие обработчики
    const newTable = table.cloneNode(true);
    table.parentNode.replaceChild(newTable, table);
    
    // Добавляем обработчик клика по строкам таблицы
    newTable.addEventListener('click', function(e) {
      const row = e.target.closest('tr');
      if (!row) return;
      
      const isCtrlPressed = e.ctrlKey || e.metaKey;
      
      // Если не нажат Ctrl, очищаем выделение
      if (!isCtrlPressed) {
        this.querySelectorAll('tbody tr').forEach(r => r.classList.remove('selected'));
        selectedRows.length = 0; // Очищаем массив
      }
      
      // Если строка уже выбрана и нажат Ctrl, снимаем выделение
      if (row.classList.contains('selected') && isCtrlPressed) {
        row.classList.remove('selected');
        const index = selectedRows.indexOf(row);
        if (index > -1) selectedRows.splice(index, 1);
      } else {
        // Иначе добавляем выделение
        row.classList.add('selected');
        if (!selectedRows.includes(row)) {
          selectedRows.push(row);
        }
      }
      
      // Обновляем состояние кнопок на основе выбранных строк
      updatePatientDocumentButtonStates(selectedRows);
    });
    
    // Обработчик двойного клика для просмотра документа
    newTable.addEventListener('dblclick', function(e) {
      const row = e.target.closest('tr');
      if (!row) return;
      
      const documentId = row.dataset.documentId;
      if (documentId) {
        viewPatientDocument(documentId);
      }
    });
    
    // Инициализация кнопок управления документами
    document.getElementById('uploadPatientDocumentBtn').addEventListener('click', function() {
      const selectedRow = newTable.querySelector('tbody tr.selected');
      
      if (!selectedRow) {
        showNotification('Выберите тип документа для загрузки', 'error');
        return;
      }
      
      // Вместо показа модального окна сразу открываем выбор файла
      document.getElementById('uploadDocumentFile').click();
    });
    
    // Обработчик события выбора файла
    document.getElementById('uploadDocumentFile').addEventListener('change', function(e) {
      if (e.target.files.length > 0) {
        uploadPatientDocument();
      }
    });
    
    document.getElementById('viewPatientDocumentBtn').addEventListener('click', function() {
      const selectedRows = newTable.querySelectorAll('tbody tr.selected');
      
      if (selectedRows.length !== 1 || !selectedRows[0].dataset.documentId) {
        showNotification('Выберите один загруженный документ для просмотра', 'error');
        return;
      }
      
      viewPatientDocument(selectedRows[0].dataset.documentId);
    });
    
    document.getElementById('deletePatientDocumentBtn').addEventListener('click', function() {
      const selectedRows = newTable.querySelectorAll('tbody tr.selected');
      const documentsToDelete = [];
      
      selectedRows.forEach(row => {
        if (row.dataset.documentId) {
          documentsToDelete.push(row.dataset.documentId);
        }
      });
      
      if (documentsToDelete.length === 0) {
        showNotification('Выберите хотя бы один загруженный документ для удаления', 'error');
        return;
      }
      
      const confirmMessage = documentsToDelete.length === 1 
        ? 'Вы действительно хотите удалить этот документ?' 
        : `Вы действительно хотите удалить ${documentsToDelete.length} выбранных документов?`;
      
      if (confirm(confirmMessage)) {
        deletePatientDocument(documentsToDelete);
      }
    });
    
    // По умолчанию отключаем кнопки просмотра и удаления
    document.getElementById('viewPatientDocumentBtn').disabled = true;
    document.getElementById('deletePatientDocumentBtn').disabled = true;
    
    // Функция обновления состояния кнопок на основе выбранных строк
    function updatePatientDocumentButtonStates(rows) {
      const hasUploadableDoc = rows.length === 1;
      const hasViewableDoc = rows.length === 1 && rows[0].dataset.documentId;
      const hasDeletableDoc = rows.some(row => row.dataset.documentId);
      
      document.getElementById('uploadPatientDocumentBtn').disabled = !hasUploadableDoc;
      document.getElementById('viewPatientDocumentBtn').disabled = !hasViewableDoc;
      document.getElementById('deletePatientDocumentBtn').disabled = !hasDeletableDoc;
      
      // Устанавливаем текст кнопки загрузки для одной выбранной строки
      if (hasUploadableDoc) {
        const documentStatus = rows[0].cells[1].textContent.trim();
        const isUploaded = documentStatus !== 'Не загружен';
        const uploadBtn = document.getElementById('uploadPatientDocumentBtn');
        
        // Сохраняем ID типа документа для последующей загрузки
        document.getElementById('uploadDocumentTypeID').value = rows[0].dataset.documentTypeId;
        
        uploadBtn.querySelector('span').textContent = isUploaded ? 'Заменить' : 'Загрузить';
      }
    }
  }

  // Удаление документа пациента
  function deletePatientDocument(documentIds) {
    // Получаем ID пациента
    const patientId = document.getElementById('uploadPatientID').value;
    
    // Преобразуем в массив, если передан один ID
    const idsArray = Array.isArray(documentIds) ? documentIds : [documentIds];
    
    // Показываем уведомление
    const message = idsArray.length === 1 
      ? 'Удаление документа...' 
      : `Удаление ${idsArray.length} документов...`;
    
    showNotification(message, 'info');
    
    // Счетчики для отслеживания прогресса
    let successCount = 0;
    let errorCount = 0;
    let requestsCompleted = 0;
    
    // Функция для проверки завершения всех запросов
    function checkAllRequestsCompleted() {
      requestsCompleted++;
      if (requestsCompleted === idsArray.length) {
        // Все запросы выполнены, выводим итоговое сообщение
        if (errorCount === 0) {
          const successMessage = idsArray.length === 1 
            ? 'Документ успешно удален' 
            : `${successCount} документов успешно удалено`;
          showNotification(successMessage, 'success');
        } else {
          showNotification(`Удалено ${successCount} из ${idsArray.length} документов. Ошибок: ${errorCount}`, 'warning');
        }
        
        // Обновляем список документов
        loadPatientDocuments(patientId, patients.find(p => p.PatientID == patientId).FullName);
      }
    }
    
    // Отправляем запросы на удаление для каждого документа
    idsArray.forEach(documentId => {
      fetch(`/api/manager/patient/document/${documentId}`, {
        method: 'DELETE'
      })
      .then(response => response.json())
      .then(result => {
        if (result.success) {
          successCount++;
        } else {
          errorCount++;
          console.error(`Ошибка при удалении документа ${documentId}: ${result.message}`);
        }
      })
      .catch(error => {
        errorCount++;
        console.error('Ошибка при удалении документа:', error);
      })
      .finally(() => {
        checkAllRequestsCompleted();
      });
    });
  }

  // Загрузка документа пациента
  function uploadPatientDocument() {
    const documentTypeId = document.getElementById('uploadDocumentTypeID').value;
    const patientId = document.getElementById('uploadPatientID').value;
    const documentFile = document.getElementById('uploadDocumentFile').files[0];
    const notes = document.getElementById('uploadDocumentNotes').value;
    
    if (!documentFile) {
      showNotification('Выберите файл для загрузки', 'error');
      return;
    }
    
    // Проверяем тип файла
    const allowedTypes = ['image/jpeg', 'image/png', 'application/pdf'];
    if (!allowedTypes.includes(documentFile.type)) {
      showNotification('Поддерживаемые форматы файлов: JPEG, PNG, PDF', 'error');
      return;
    }
    
    // Создаем FormData для отправки файла
    const formData = new FormData();
    formData.append('file', documentFile);
    formData.append('documentTypeID', documentTypeId);
    if (notes) formData.append('notes', notes);
    
    // Показываем уведомление о загрузке
    showNotification('Загрузка документа...', 'info');
    
    // Отправляем запрос
    fetch(`/api/manager/patient/${patientId}/document`, {
      method: 'POST',
      body: formData
    })
    .then(response => response.json())
    .then(result => {
      if (result.success) {
        // Очищаем поле выбора файла
        document.getElementById('uploadDocumentFile').value = '';
        
        // Обновляем список документов
        loadPatientDocuments(patientId, patients.find(p => p.PatientID == patientId).FullName);
        
        showNotification('Документ успешно загружен', 'success');
      } else {
        showNotification(`Ошибка при загрузке документа: ${result.message}`, 'error');
      }
    })
    .catch(error => {
      console.error('Ошибка при загрузке документа:', error);
      showNotification('Ошибка при загрузке документа', 'error');
    });
  }

  // Просмотр документа пациента
  function viewPatientDocument(documentId) {
    window.open(`/api/manager/patient/document/${documentId}/view`, '_blank');
  }

  // Вспомогательные функции
  function formatDate(date) {
    if (!(date instanceof Date) || isNaN(date)) return '-';
    // Используем методы getDate, getMonth и getFullYear, чтобы избежать проблем с часовыми поясами
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}.${month}.${year}`;
  }
  
  function formatDateTime(date) {
    if (!(date instanceof Date) || isNaN(date)) return '-';
    // Используем методы getDate, getMonth и getFullYear, getHours и getMinutes
    // чтобы избежать проблем с часовыми поясами
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${day}.${month}.${year} ${hours}:${minutes}`;
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
        color = '#4CAF50'; // Зеленый цвет для галочки
        break;
      case 'Частично':
        // Изменяем восклицательный знак на красный крестик
        icon = 'M12,2C17.53,2 22,6.47 22,12C22,17.53 17.53,22 12,22C6.47,22 2,17.53 2,12C2,6.47 6.47,2 12,2M15.59,7L12,10.59L8.41,7L7,8.41L10.59,12L7,15.59L8.41,17L12,13.41L15.59,17L17,15.59L13.41,12L17,8.41L15.59,7Z';
        color = '#F44336'; // Красный цвет для крестика
        break;
      case 'Отсутствуют':
        icon = 'M12,2C17.53,2 22,6.47 22,12C22,17.53 17.53,22 12,22C6.47,22 2,17.53 2,12C2,6.47 6.47,2 12,2M15.59,7L12,10.59L8.41,7L7,8.41L10.59,12L7,15.59L8.41,17L12,13.41L15.59,17L17,15.59L13.41,12L17,8.41L15.59,7Z';
        color = '#F44336'; // Красный цвет для крестика
        break;
      default:
        icon = 'M13,9H11V7H13M13,17H11V11H13M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z';
        color = 'var(--gray-color)';
    }
    
    return `<svg viewBox="0 0 24 24" style="width: 24px; height: 24px; fill: ${color};">
              <path d="${icon}"></path>
            </svg>`;
  }

  // Инициализация обработчика смены типа стационара
  function initPatientModal() {
    // Устанавливаем текущую дату по умолчанию для даты записи
    document.getElementById('patientRecordDate').valueAsDate = new Date();
    
    // Обработчик изменения типа стационара
    document.getElementById('patientStayType').addEventListener('change', function() {
      const accommodationSection = document.getElementById('accommodationSection');
      
      if (this.value === 'Круглосуточный') {
        accommodationSection.style.display = 'block';
        
        // Загружаем список корпусов, если еще не загружены
        const buildingSelect = document.getElementById('patientBuilding');
        if (buildingSelect.options.length === 0) {
          fetch('/api/manager/buildings')
            .then(response => response.json())
            .then(buildings => {
              console.log('Загружены данные о корпусах:', buildings);
              buildingSelect.innerHTML = '<option value="">Выберите корпус</option>';
              
              buildings.forEach(building => {
                const option = document.createElement('option');
                option.value = building.BuildingID;
                option.textContent = `Корпус ${building.BuildingNumber}`;
                buildingSelect.appendChild(option);
              });
              
              // Если есть корпуса, загружаем комнаты для первого корпуса
              if (buildings.length > 0) {
                // НЕ выбираем автоматически первый корпус при добавлении
                // Пусть пользователь сам выберет нужный корпус
              }
            })
            .catch(error => {
              console.error('Ошибка загрузки корпусов:', error);
              showNotification('Не удалось загрузить список корпусов', 'error');
            });
        }
      } else {
        accommodationSection.style.display = 'none';
      }
    });
    
    // Обработчик изменения корпуса
    document.getElementById('patientBuilding').addEventListener('change', function() {
      if (this.value) {
        loadRooms(this.value);
      } else {
        // Очищаем выпадающие списки комнат и кроватей
        document.getElementById('patientRoom').innerHTML = '<option value="">Выберите комнату</option>';
        document.getElementById('patientBed').innerHTML = '<option value="">Выберите кровать</option>';
      }
    });
    
    // Обработчик изменения комнаты
    document.getElementById('patientRoom').addEventListener('change', function() {
      const selectedOption = this.options[this.selectedIndex];
      if (!selectedOption || !selectedOption.value) {
        document.getElementById('patientBed').innerHTML = '<option value="">Выберите кровать</option>';
        return;
      }
      
      try {
        let availableBeds = [];
        if (selectedOption.dataset.availableBeds) {
          try {
            availableBeds = JSON.parse(selectedOption.dataset.availableBeds);
          } catch (e) {
            console.error('Ошибка при парсинге JSON доступных кроватей:', e);
            console.log('Исходные данные:', selectedOption.dataset.availableBeds);
          }
        }
        
        // Проверяем наличие данных о кроватях
        if (!Array.isArray(availableBeds) || availableBeds.length === 0) {
          console.warn('Нет доступных кроватей для комнаты или данные некорректны');
        }
        
        updateAvailableBeds(availableBeds);
      } catch (error) {
        console.error('Ошибка при обработке изменения комнаты:', error);
        showNotification('Ошибка при загрузке кроватей', 'error');
      }
    });
  }

  // Функция для загрузки комнат выбранного корпуса
  function loadRooms(buildingId) {
    return fetch(`/api/manager/rooms/${buildingId}`)
      .then(response => response.json())
      .then(rooms => {
        const roomSelect = document.getElementById('patientRoom');
        roomSelect.innerHTML = '<option value="">Выберите комнату</option>';
        
        if (!rooms || rooms.length === 0) {
          document.getElementById('patientBed').innerHTML = '<option value="">Нет доступных кроватей</option>';
          return;
        }
        
        // Выводим в консоль первый элемент для анализа структуры
        console.log("Пример данных комнаты:", rooms[0]);
        
        rooms.forEach(room => {
          const option = document.createElement('option');
          
          // Получаем ID комнаты с учетом возможных разных имен свойств
          const roomId = room.RoomID || room.roomID || room.roomId || room.id;
          
          // Получаем номер комнаты
          const roomNumber = room.RoomNumber || room.roomNumber || room.number || '';
          
          // Получаем список доступных кроватей
          const availableBeds = room.AvailableBeds || room.availableBeds || room.beds || [];
          
          if (!roomId) {
            console.error("Не удалось определить ID комнаты:", room);
            return; // Пропускаем эту комнату
          }
          
          option.value = roomId;
          option.textContent = `Комната ${roomNumber}`;
          option.dataset.availableBeds = JSON.stringify(availableBeds);
          roomSelect.appendChild(option);
        });
        
        // Возвращаем промис для цепочки then
        return Promise.resolve();
      })
      .catch(error => {
        console.error('Ошибка при загрузке комнат:', error);
        showNotification('Ошибка при загрузке комнат: ' + error.message, 'error');
        
        // Возвращаем отклоненный промис для обработки ошибок в цепочке then
        return Promise.reject(error);
      });
  }

  // Функция для обновления списка доступных кроватей
  function updateAvailableBeds(availableBeds) {
    const bedSelect = document.getElementById('patientBed');
    bedSelect.innerHTML = '<option value="">Выберите кровать</option>';
    
    // Проверка, что availableBeds является массивом
    if (!Array.isArray(availableBeds)) {
      console.error('Ошибка: availableBeds не является массивом:', availableBeds);
      return;
    }
    
    if (availableBeds.length === 0) {
      bedSelect.innerHTML = '<option value="">Нет доступных кроватей</option>';
      return;
    }
    
    console.log("Доступные кровати:", availableBeds);
    
    availableBeds.forEach(bedNumber => {
      const option = document.createElement('option');
      option.value = bedNumber;
      option.textContent = `Кровать ${bedNumber}`;
      bedSelect.appendChild(option);
    });
  }

  // Функция для сохранения пациента
  function savePatient() {
    // Валидация формы
    const fullName = document.getElementById('patientFullName').value.trim();
    const dateOfBirth = document.getElementById('patientDateOfBirth').value;
    const gender = document.getElementById('patientGender').value;
    const stayType = document.getElementById('patientStayType').value;
    const recordDate = document.getElementById('patientRecordDate').value;
    const dischargeDate = document.getElementById('patientDischargeDate').value;
    
    if (!fullName) {
        showNotification('Пожалуйста, введите ФИО пациента', 'error');
        return;
    }
    
    if (!dateOfBirth) {
        showNotification('Пожалуйста, укажите дату рождения', 'error');
        return;
    }

    // Проверка даты рождения (должна быть не позже текущей даты)
    if (new Date(dateOfBirth) > new Date()) {
        showNotification('Дата рождения не может быть в будущем', 'error');
        return;
    }
    
    if (!recordDate) {
        showNotification('Пожалуйста, укажите дату записи', 'error');
        return;
    }

    // Проверка дат
    if (dischargeDate && new Date(dischargeDate) <= new Date(recordDate)) {
        showNotification('Дата выписки должна быть позже даты записи', 'error');
        return;
    }
    
    // Создаем объект с данными пациента
    const patient = {
        FullName: fullName,
        DateOfBirth: dateOfBirth,
        Gender: gender,
        StayType: stayType,
        RecordDate: recordDate,
        DischargeDate: dischargeDate || null
    };
    
    // Заполняем информацию о размещении только для круглосуточного стационара
    if (stayType === 'Круглосуточный') {
      const buildingSelect = document.getElementById('patientBuilding');
      const roomSelect = document.getElementById('patientRoom');
      const bedSelect = document.getElementById('patientBed');
      
      if (!buildingSelect.value) {
        showNotification('Пожалуйста, выберите корпус', 'error');
        return;
      }
      
      if (!roomSelect.value) {
        showNotification('Пожалуйста, выберите комнату', 'error');
        return;
      }
      
      if (!bedSelect.value) {
        showNotification('Пожалуйста, выберите кровать', 'error');
        return;
      }
      
      patient.AccommodationInfo = {
        RoomID: parseInt(roomSelect.value),
        BedNumber: parseInt(bedSelect.value)
      };
      
      console.log('Данные размещения для сохранения:', patient.AccommodationInfo);
    }
    
    // Проверяем, это добавление нового пациента или редактирование существующего
    const saveButton = document.getElementById('savePatientBtn');
    const isEditMode = saveButton.dataset.patientId !== undefined && saveButton.dataset.patientId !== '';
    const patientId = isEditMode ? saveButton.dataset.patientId : null;
    
    console.log('Режим редактирования:', isEditMode ? 'да' : 'нет', 'ID пациента:', patientId);
    
    // Формируем запрос в зависимости от режима
    let url = '/api/manager/patient';
    let method = 'POST';
    
    if (isEditMode) {
      url = `/api/manager/patient/${patientId}`;
      method = 'PUT';
      
      // Если в режиме редактирования, добавляем текущий ID пациента
      if (patient.AccommodationInfo) {
        patient.AccommodationInfo.CurrentPatientID = parseInt(patientId);
        console.log('Добавлен CurrentPatientID:', patient.AccommodationInfo.CurrentPatientID);
      }
    }
    
    // Логируем отправляемые данные
    console.log(`${isEditMode ? 'Обновление' : 'Создание'} пациента:`, patient);
    
    // Показываем уведомление о начале сохранения
    showNotification(`${isEditMode ? 'Обновление' : 'Добавление'} пациента...`, 'info');
    
    // Отправляем данные на сервер
    fetch(url, {
      method: method,
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(patient)
    })
    .then(response => {
      console.log('Статус ответа:', response.status);
      if (!response.ok) {
        return response.text().then(text => {
          console.log('Текст ошибки:', text);
          try {
            const data = JSON.parse(text);
            throw new Error(data.message || `Ошибка при ${isEditMode ? 'обновлении' : 'добавлении'} пациента`);
          } catch (e) {
            throw new Error(`Ошибка при ${isEditMode ? 'обновлении' : 'добавлении'} пациента: ${text}`);
          }
        });
      }
      return response.json();
    })
    .then(data => {
      showNotification(`Пациент успешно ${isEditMode ? 'обновлен' : 'добавлен'}`, 'success');
      document.getElementById('patientModal').style.display = 'none';
      
      // Сбрасываем атрибут ID пациента и текст кнопки сохранения
      saveButton.dataset.patientId = '';
      saveButton.textContent = 'Сохранить';
      
      // Обновляем список пациентов
      loadPatients();
      
      // Обновляем статистику размещения если пациент был размещен
      if (stayType === 'Круглосуточный') {
        loadAccommodations();
      }
    })
    .catch(error => {
      console.error(`Ошибка при ${isEditMode ? 'обновлении' : 'добавлении'} пациента:`, error);
      showNotification(error.message, 'error');
    });
  }

  // Функция для отображения уведомлений
  function showNotification(message, type = 'info') {
    // Удаляем предыдущие уведомления
    const existingNotifications = document.querySelectorAll('.notification');
    existingNotifications.forEach(notification => {
      notification.style.animation = 'fadeOut 0.5s';
      setTimeout(() => {
        notification.remove();
      }, 500);
    });
    
    // Создаем новое уведомление
    const notification = document.createElement('div');
    notification.className = `notification ${type}`;
    notification.textContent = message;
    
    // Добавляем уведомление на страницу
    document.body.appendChild(notification);
    
    // Автоматически удаляем уведомление через 3 секунды
    setTimeout(() => {
      notification.style.animation = 'fadeOut 0.5s';
      setTimeout(() => {
        notification.remove();
      }, 500);
    }, 3000);
  }

  // Показать модальное окно добавления сопровождающего
  function showAddAccompanyingModal() {
    // Устанавливаем заголовок модального окна
    document.getElementById('accompanyingModalTitle').textContent = 'Добавление сопровождающего';
    document.getElementById('accompanyingPersonID').value = '';
    selectedPowerOfAttorneyFile = null; // Сбрасываем выбранный файл
    
    // Очищаем форму
    document.getElementById('patientSearch').value = '';
    document.getElementById('accompanyingFullName').value = '';
    document.getElementById('accompanyingDateOfBirth').value = '';
    document.getElementById('accompanyingRelationship').selectedIndex = 0; // Родитель
    
    // Скрываем секцию размещения по умолчанию
    document.getElementById('accompanyingAccommodationSection').style.display = 'none';
    
    // Обновляем UI доверенности
    updatePowerOfAttorneyUI('Родитель');
    
    // Загружаем список пациентов
    loadPatientsList();
    
    // Загружаем список корпусов для размещения
    loadBuildingsForAccompanying();
    
    // Отображаем модальное окно
    document.getElementById('accompanyingModal').style.display = 'block';
  }
  
  // Показать модальное окно редактирования сопровождающего
  function showEditAccompanyingModal() {
    if (selectedAccompanyingIDs.length !== 1) {
      showNotification('Выберите одного сопровождающего для редактирования', 'error');
      return;
    }
    selectedPowerOfAttorneyFile = null; // Сбрасываем выбранный файл при открытии
    
    const accompanyingID = selectedAccompanyingIDs[0];
    
    // Устанавливаем заголовок модального окна
    document.getElementById('accompanyingModalTitle').textContent = 'Редактирование сопровождающего';
    document.getElementById('accompanyingPersonID').value = accompanyingID;
    
    // Показываем индикатор загрузки
    showNotification('Загрузка данных сопровождающего...', 'info');
    
    // Загружаем данные сопровождающего
    loadAccompanyingPersonData(accompanyingID);
    
    // Отображаем модальное окно
    document.getElementById('accompanyingModal').style.display = 'block';
  }

  // Загрузка списка пациентов для выбора при добавлении сопровождающего
  function loadPatientsList() {
    // Возвращаем Promise
    return fetch('/api/manager/patients/list')
      .then(response => {
        if (!response.ok) {
          throw new Error('Ошибка загрузки списка пациентов');
        }
        return response.json();
      })
      .then(data => {
        const patientSelect = document.getElementById('accompanyingPatient');
        patientSelect.innerHTML = '<option value="">Выберите пациента</option>';
        
        console.log("Данные пациентов для выпадающего списка:", data);
        
        if (Array.isArray(data)) {
          data.forEach(patient => {
            const option = document.createElement('option');
            
            // Получаем ID пациента с учетом возможных разных имен свойств
            const patientId = patient.PatientID || patient.patientID || patient.patientId || patient.id;
            const fullName = patient.FullName || patient.fullName || patient.name || '';
            const stayType = patient.StayType || patient.stayType || 'Дневной';
            
            option.value = patientId;
            option.textContent = fullName;
            option.dataset.stayType = stayType;
            
            patientSelect.appendChild(option);
          });
        }
        
        // Возвращаем данные, чтобы цепочка then могла продолжиться
        return data;
      })
      .catch(error => {
        console.error('Ошибка при загрузке списка пациентов:', error);
        showNotification('Ошибка при загрузке списка пациентов', 'error');
        // Продолжаем цепочку even при ошибке
        return Promise.resolve([]);
      });
  }
  
  // Инициализация поиска по пациентам
  function initPatientSearch() {
    const searchInput = document.getElementById('patientSearch');
    const patientSelect = document.getElementById('accompanyingPatient');
    const searchText = searchInput.value.toLowerCase().trim();
    
    // Сохраняем все опции при первом вызове, если они еще не сохранены
    if (!window.allPatientOptions || window.allPatientOptions.length === 0) {
      window.allPatientOptions = Array.from(patientSelect.options).slice(1); // Пропускаем первую опцию "Выберите пациента"
    }
    
    // Очищаем список и добавляем первую опцию
    patientSelect.innerHTML = '<option value="">Выберите пациента</option>';
    
    // Если поле поиска пустое, показываем все опции
    const options = searchText ? 
      window.allPatientOptions.filter(option => option.textContent.toLowerCase().includes(searchText)) : 
      window.allPatientOptions;
    
    // Добавляем отфильтрованные опции
    options.forEach(option => {
      patientSelect.appendChild(option.cloneNode(true));
    });
    
    // Если нет результатов поиска, показываем сообщение
    if (searchText && options.length === 0) {
      const noMatchOption = document.createElement('option');
      noMatchOption.textContent = `Пациенты не найдены: "${searchText}"`;
      noMatchOption.disabled = true;
      patientSelect.appendChild(noMatchOption);
    }
    
    // Возвращаем количество найденных пациентов
    return options.length;
  }
  
  // Загрузка данных сопровождающего для редактирования
  function loadAccompanyingPersonData(accompanyingID) {
    fetch(`/api/manager/accompanyingperson/${accompanyingID}`)
      .then(response => {
        if (!response.ok) {
          return response.text().then(text => {
            try {
              const errorData = JSON.parse(text);
              throw new Error(errorData.message || 'Ошибка при загрузке данных сопровождающего');
            } catch (e) {
              throw new Error('Ошибка при загрузке данных сопровождающего: ' + text);
            }
          });
        }
        return response.json();
      })
      .then(data => {
        console.log('Raw data from /api/manager/accompanyingperson/id:', JSON.stringify(data, null, 2)); // Отладочный вывод
        if (!data.success) {
          throw new Error(data.message || 'Не удалось загрузить данные сопровождающего');
        }
        
        // Предполагаем, что сервер мог преобразовать имена свойств в camelCase
        // Сначала проверяем, есть ли camelCase версия ключа, потом PascalCase
        const accompanying = data.accompanyingperson || data.accompanyingPerson;
        const accommodationData = data.accommodationinfo || data.accommodationInfo;

        if (!accompanying) {
          console.error("Объект accompanyingPerson не найден в ответе сервера:", data);
          throw new Error('Отсутствуют данные сопровождающего в ответе сервера.');
        }
        console.log("Accompanying person object:", JSON.stringify(accompanying, null, 2));
        if (accommodationData) {
          console.log("Accommodation info object:", JSON.stringify(accommodationData, null, 2));
        }

        // Загружаем список пациентов
        loadPatientsList().then(() => {
          let patientName = '';
          try {
            if (window.accompanyingPersons && Array.isArray(window.accompanyingPersons)) {
              const accompanyingPersonFromArray = window.accompanyingPersons.find(p => p.AccompanyingPersonID == accompanyingID);
              if (accompanyingPersonFromArray && accompanyingPersonFromArray.PatientName) {
                patientName = accompanyingPersonFromArray.PatientName;
              }
            }
            if (!patientName) {
              const patientSelect = document.getElementById('accompanyingPatient');
              const patientIdToFind = accompanying.patientID || accompanying.PatientID; // camelCase first
              for (let i = 0; i < patientSelect.options.length; i++) {
                if (patientSelect.options[i].value == patientIdToFind) {
                  patientName = patientSelect.options[i].textContent;
                  break;
                }
              }
            }
          } catch (e) {
            console.error("Ошибка при поиске имени пациента:", e);
          }
          
          // Заполняем форму данными сопровождающего, используя camelCase первым, затем PascalCase как fallback
          document.getElementById('accompanyingPatient').value = accompanying.patientID || accompanying.PatientID;
          document.getElementById('patientSearch').value = patientName;
          document.getElementById('accompanyingFullName').value = accompanying.fullName || accompanying.FullName;
          
          const dateOfBirth = accompanying.dateOfBirth || accompanying.DateOfBirth;
          if (dateOfBirth) {
            const dob = new Date(dateOfBirth);
            const year = dob.getFullYear();
            const month = String(dob.getMonth() + 1).padStart(2, '0');
            const day = String(dob.getDate()).padStart(2, '0');
            const dobFormatted = `${year}-${month}-${day}`;
            document.getElementById('accompanyingDateOfBirth').value = dobFormatted;
          } else {
            document.getElementById('accompanyingDateOfBirth').value = '';
          }
          
          const relationship = accompanying.relationship || accompanying.Relationship;
          selectRelationship(relationship);
          
          const hasPowerOfAttorney = accompanying.hasPowerOfAttorney !== undefined ? accompanying.hasPowerOfAttorney : accompanying.HasPowerOfAttorney;
          updatePowerOfAttorneyUI(relationship, hasPowerOfAttorney);
          
          const patientSelect = document.getElementById('accompanyingPatient');
          const selectedOption = patientSelect.options[patientSelect.selectedIndex];
          const stayType = selectedOption?.dataset.stayType;
          
          const accommodationSection = document.getElementById('accompanyingAccommodationSection');
          accommodationSection.style.display = stayType === 'Круглосуточный' ? 'block' : 'none';
          
          if (stayType === 'Круглосуточный') {
            loadBuildingsForAccompanying().then(() => {
              if (accommodationData) {
                loadAccompanyingAccommodation(accommodationData); // Передаем accommodationData, которая может быть camelCased
              }
            });
          }
          
          showNotification('Данные сопровождающего загружены', 'success');
        });
      })
      .catch(error => {
        console.error('Ошибка при загрузке данных сопровождающего:', error);
        showNotification(error.message, 'error');
        document.getElementById('accompanyingModal').style.display = 'none';
      });
  }
  
  // Выбор отношения к пациенту в комбобоксе
  function selectRelationship(relationship) {
    const relationshipSelect = document.getElementById('accompanyingRelationship');
    
    for (let i = 0; i < relationshipSelect.options.length; i++) {
      if (relationshipSelect.options[i].text === relationship) {
        relationshipSelect.selectedIndex = i;
        return;
      }
    }
    
    // Если не нашли точное совпадение, выбираем "Иное лицо"
    for (let i = 0; i < relationshipSelect.options.length; i++) {
      if (relationshipSelect.options[i].text === "Иное лицо") {
        relationshipSelect.selectedIndex = i;
        return;
      }
    }
  }
  
  // Обновление UI доверенности
  function updatePowerOfAttorneyUI(relationship, hasPowerOfAttorney = false) {
    const powerOfAttorneyStatus = document.getElementById('powerOfAttorneyStatus');
    const uploadPowerOfAttorneyBtn = document.getElementById('uploadPowerOfAttorneyBtn');
    
    // Определяем, требуется ли доверенность
    const isPowerOfAttorneyRequired = relationship !== 'Родитель' && relationship !== 'Опекун';
    
    if (isPowerOfAttorneyRequired) {
      uploadPowerOfAttorneyBtn.style.display = 'inline-block'; // Показываем кнопку
      uploadPowerOfAttorneyBtn.disabled = false; // Кнопка всегда активна, если требуется

      if (hasPowerOfAttorney) {
        powerOfAttorneyStatus.textContent = 'Загружена';
        powerOfAttorneyStatus.style.color = 'var(--green-color)'; // Зеленый
      } else if (selectedPowerOfAttorneyFile) {
        // Если файл выбран локально, но еще не сохранен
        powerOfAttorneyStatus.textContent = 'Файл выбран: ' + selectedPowerOfAttorneyFile.name;
        powerOfAttorneyStatus.style.color = 'var(--blue-color)'; 
      } else {
        powerOfAttorneyStatus.textContent = 'Требуется загрузить';
        powerOfAttorneyStatus.style.color = 'var(--red-color)'; // Красный
      }
    } else {
      uploadPowerOfAttorneyBtn.style.display = 'none';
      powerOfAttorneyStatus.textContent = 'Не требуется';
      powerOfAttorneyStatus.style.color = 'var(--gray-color)'; // Серый
    }
  }
  
  // Загрузка корпусов для размещения сопровождающего
  function loadBuildingsForAccompanying() {
    return fetch('/api/manager/buildings')
      .then(response => response.json())
      .then(buildings => {
        const buildingSelect = document.getElementById('accompanyingBuilding');
        buildingSelect.innerHTML = '<option value="">Выберите корпус</option>';
        
        buildings.forEach(building => {
          const option = document.createElement('option');
          option.value = building.BuildingID;
          option.textContent = `Корпус ${building.BuildingNumber}`;
          buildingSelect.appendChild(option);
        });
        
        // Добавляем обработчик события изменения корпуса
        buildingSelect.onchange = function() {
          if (this.value) {
            loadRoomsForAccompanying(this.value);
          } else {
            document.getElementById('accompanyingRoom').innerHTML = '<option value="">Выберите комнату</option>';
            document.getElementById('accompanyingBed').innerHTML = '<option value="">Выберите кровать</option>';
          }
        };
      });
  }
  
  // Загрузка комнат для размещения сопровождающего
  function loadRoomsForAccompanying(buildingID) {
    // Получаем ID сопровождающего (если мы в режиме редактирования)
    const accompanyingID = document.getElementById('accompanyingPersonID').value;
    
    // Добавляем accompanyingID в параметры запроса, если он есть
    let url = `/api/manager/rooms/${buildingID}`;
    if (accompanyingID) {
      url += `?accompanyingPersonID=${accompanyingID}`;
    }
    
    return fetch(url)
      .then(response => response.json())
      .then(rooms => {
        const roomSelect = document.getElementById('accompanyingRoom');
        roomSelect.innerHTML = '<option value="">Выберите комнату</option>';
        
        if (!rooms || rooms.length === 0) {
          document.getElementById('accompanyingBed').innerHTML = '<option value="">Нет доступных комнат</option>';
          return;
        }
        
        // Выводим в консоль первый элемент для анализа структуры
        console.log("Пример данных комнаты для сопровождающего:", rooms[0]);
        
        rooms.forEach(room => {
          const option = document.createElement('option');
          
          // Получаем ID комнаты с учетом возможных разных имен свойств
          const roomId = room.RoomID || room.roomID || room.roomId || room.id;
          
          // Получаем номер комнаты
          const roomNumber = room.RoomNumber || room.roomNumber || room.number || '';
          
          // Получаем список доступных кроватей
          const availableBeds = room.AvailableBeds || room.availableBeds || room.beds || [];
          
          if (!roomId) {
            console.error("Не удалось определить ID комнаты:", room);
            return; // Пропускаем эту комнату
          }
          
          option.value = roomId;
          option.textContent = `Комната ${roomNumber}`;
          option.dataset.availableBeds = JSON.stringify(availableBeds);
          roomSelect.appendChild(option);
        });
        
        // Добавляем обработчик события изменения комнаты
        roomSelect.onchange = function() {
          if (this.value) {
            const selectedOption = this.options[this.selectedIndex];
            let availableBeds = [];
            
            try {
              if (selectedOption.dataset.availableBeds) {
                availableBeds = JSON.parse(selectedOption.dataset.availableBeds);
              }
            } catch (e) {
              console.error('Ошибка при парсинге JSON доступных кроватей:', e);
            }
            
            updateAvailableBedsForAccompanying(availableBeds);
          } else {
            document.getElementById('accompanyingBed').innerHTML = '<option value="">Выберите кровать</option>';
          }
        };
        
        // Возвращаем промис для цепочки then
        return Promise.resolve();
      })
      .catch(error => {
        console.error('Ошибка при загрузке комнат для сопровождающего:', error);
        showNotification('Ошибка при загрузке комнат: ' + error.message, 'error');
        
        // Возвращаем отклоненный промис для обработки ошибок в цепочке then
        return Promise.reject(error);
      });
  }
  
  // Обновление списка доступных кроватей для сопровождающего
  function updateAvailableBedsForAccompanying(availableBeds) {
    const bedSelect = document.getElementById('accompanyingBed');
    bedSelect.innerHTML = '<option value="">Выберите кровать</option>';
    
    console.log("Обновление списка кроватей для сопровождающего:", availableBeds);
    
    // Проверка входных данных
    if (!availableBeds) {
      console.error('Ошибка: availableBeds не определен');
      bedSelect.innerHTML = '<option value="">Нет доступных кроватей</option>';
      return;
    }
    
    // Проверяем, если availableBeds это массив объектов вместо примитивов
    let bedNumbers = availableBeds;
    
    if (Array.isArray(availableBeds) && availableBeds.length > 0 && typeof availableBeds[0] === 'object') {
      console.log('Преобразуем массив объектов в массив номеров кроватей');
      bedNumbers = availableBeds.map(bed => {
        return bed.BedNumber || bed.bedNumber || bed.number || bed;
      });
    }
    
    // Проверка, что bedNumbers является массивом
    if (!Array.isArray(bedNumbers)) {
      console.error('Ошибка: bedNumbers не является массивом:', bedNumbers);
      bedSelect.innerHTML = '<option value="">Нет доступных кроватей</option>';
      return;
    }
    
    if (bedNumbers.length === 0) {
      bedSelect.innerHTML = '<option value="">Нет доступных кроватей</option>';
      return;
    }
    
    console.log("Номера доступных кроватей:", bedNumbers);
    
    bedNumbers.forEach(bedNumber => {
      const option = document.createElement('option');
      option.value = bedNumber;
      option.textContent = `Кровать ${bedNumber}`;
      bedSelect.appendChild(option);
    });
  }
  
  // Загрузка данных о размещении сопровождающего
  function loadAccompanyingAccommodation(accommodationInfo) {
    if (!accommodationInfo) return;
    
    console.log("Загрузка данных о размещении сопровождающего:", accommodationInfo);
    
    // Получаем ID сопровождающего
    const accompanyingID = document.getElementById('accompanyingPersonID').value;
    
    // Выбираем корпус
    const buildingSelect = document.getElementById('accompanyingBuilding');
    buildingSelect.value = accommodationInfo.BuildingID || accommodationInfo.buildingID || accommodationInfo.buildingId || '';
    
    // Проверяем, выбран ли корпус
    if (!buildingSelect.value) {
      console.error("Не удалось выбрать корпус:", accommodationInfo);
      return;
    }
    
    // Получаем ID комнаты и кровати
    const roomID = accommodationInfo.RoomID || accommodationInfo.roomID || accommodationInfo.roomId || '';
    const bedNumber = accommodationInfo.BedNumber || accommodationInfo.bedNumber || accommodationInfo.bedNum || 1;
    
    if (!roomID) {
      console.error("Не удалось определить ID комнаты для размещения:", accommodationInfo);
      return;
    }
    
    // Загружаем комнаты для выбранного корпуса, передавая ID сопровождающего
    loadRoomsForAccompanying(buildingSelect.value)
      .then(() => {
        // Выбираем комнату
        const roomSelect = document.getElementById('accompanyingRoom');
        roomSelect.value = roomID;
        
        // Проверяем, что комната выбрана
        if (!roomSelect.value) {
          console.error("Не удалось выбрать комнату:", roomID);
          showNotification('Не удалось выбрать комнату. Возможно, она больше не доступна.', 'warning');
          return;
        }
        
        // Обновляем список кроватей
        if (roomSelect.selectedIndex >= 0) {
          const selectedOption = roomSelect.options[roomSelect.selectedIndex];
          let availableBeds = [];
          
          try {
            if (selectedOption.dataset.availableBeds) {
              availableBeds = JSON.parse(selectedOption.dataset.availableBeds);
            }
          } catch (e) {
            console.error('Ошибка при парсинге JSON доступных кроватей:', e);
          }
          
          // Убедимся, что текущая кровать в списке доступных
          if (!availableBeds.includes(bedNumber)) {
            availableBeds.push(bedNumber);
          }
          
          // Обновляем список кроватей с текущей кроватью
          updateAvailableBedsForAccompanying(availableBeds);
          
          // Выбираем кровать
          setTimeout(() => {
            const bedSelect = document.getElementById('accompanyingBed');
            bedSelect.value = bedNumber.toString();
            
            // Проверяем, что кровать выбрана
            if (!bedSelect.value) {
              console.error(`Не удалось выбрать кровать ${bedNumber} из доступных:`, availableBeds);
              showNotification('Не удалось выбрать кровать. Возможно, она занята.', 'warning');
            }
          }, 100);
        }
      })
      .catch(error => {
        console.error('Ошибка при загрузке данных о размещении сопровождающего:', error);
        showNotification('Ошибка при загрузке данных о размещении', 'error');
      });
  }
  
  // Сохранение данных сопровождающего
  function saveAccompanyingPerson() {
    // Валидация формы
    const accompanyingID = document.getElementById('accompanyingPersonID').value;
    const patientID = document.getElementById('accompanyingPatient').value;
    const fullName = document.getElementById('accompanyingFullName').value.trim();
    const dateOfBirth = document.getElementById('accompanyingDateOfBirth').value;
    const relationship = document.getElementById('accompanyingRelationship').options[document.getElementById('accompanyingRelationship').selectedIndex].text;
    
    // Проверяем, требуется ли доверенность и есть ли файл (локально или уже загружен)
    const isPowerOfAttorneyRequired = relationship !== 'Родитель' && relationship !== 'Опекун';
    let hasPowerOfAttorney = false; // Флаг для отправки на сервер
    
    // В режиме редактирования проверяем, загружена ли уже
    if (accompanyingID) { 
        hasPowerOfAttorney = document.getElementById('powerOfAttorneyStatus').textContent === 'Загружена';
    }
    // Если файл выбран локально, считаем, что доверенность будет
    if (selectedPowerOfAttorneyFile) {
        hasPowerOfAttorney = true;
    }

    // Проверка наличия доверенности, если она требуется
    if (isPowerOfAttorneyRequired && !hasPowerOfAttorney) {
        showNotification('Для выбранного отношения требуется выбрать файл доверенности', 'error');
        return;
    }
    
    if (!patientID) {
        showNotification('Пожалуйста, выберите пациента', 'error');
        return;
    }
    
    if (!fullName) {
        showNotification('Пожалуйста, введите ФИО сопровождающего', 'error');
        return;
    }
    
    if (!dateOfBirth) {
        showNotification('Пожалуйста, укажите дату рождения', 'error');
        return;
    }
    
    if (!relationship) {
        showNotification('Пожалуйста, выберите степень родства', 'error');
        return;
    }

    // Проверка даты рождения (должна быть не позже текущей даты)
    if (new Date(dateOfBirth) > new Date()) {
        showNotification('Дата рождения не может быть в будущем', 'error');
        return;
    }

    // Проверяем тип стационара пациента
    const patientSelect = document.getElementById('accompanyingPatient');
    const selectedOption = patientSelect.options[patientSelect.selectedIndex];
    const stayType = selectedOption?.dataset.stayType;
    
    // Флаг, указывающий, требуется ли размещение
    const needAccommodation = stayType === 'Круглосуточный';
    
    // Данные о размещении (для круглосуточного стационара)
    let accommodationInfo = null;
    
    if (needAccommodation) {
        const buildingID = document.getElementById('accompanyingBuilding').value;
        const roomID = document.getElementById('accompanyingRoom').value;
        const bedNumber = document.getElementById('accompanyingBed').value;
        
        // Проверяем наличие значений только если нужно размещение
        if (!buildingID || !roomID || !bedNumber) {
            showNotification('Пожалуйста, выберите корпус, комнату и кровать для размещения', 'error');
            return;
        }

        accommodationInfo = {
            RoomID: parseInt(roomID),
            BedNumber: parseInt(bedNumber)
        };
    }
    
    // Формируем объект данных сопровождающего
    const accompanyingData = {
        PatientID: parseInt(patientID),
        FullName: fullName,
        DateOfBirth: dateOfBirth || null,
        Relationship: relationship,
        HasPowerOfAttorney: hasPowerOfAttorney,
        NeedAccommodation: needAccommodation,
        // Если размещение не требуется, передаем пустой объект вместо null
        AccommodationInfo: needAccommodation && accommodationInfo ? accommodationInfo : { RoomID: 0, BedNumber: 0 }
    };
    
    // Определяем, это добавление нового или редактирование существующего
    const isEditMode = !!accompanyingID;
    
    // --- Логика сохранения --- 
    
    // Функция для выполнения основного запроса (POST/PUT)
    function executeMainRequest() {
        const url = isEditMode 
            ? `/api/manager/accompanyingperson/${accompanyingID}`
            : '/api/manager/accompanyingperson';
        const method = isEditMode ? 'PUT' : 'POST';
        
        console.log('Отправка данных сопровождающего:', JSON.stringify(accompanyingData, null, 2));
        
        return fetch(url, {
            method: method,
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(accompanyingData)
        })
        .then(response => {
            if (!response.ok) {
                return response.json().then(err => { throw new Error(err.message || 'Ошибка сервера'); });
            }
            return response.json();
        });
    }

    // Функция для загрузки файла доверенности
    function executeFileUpload(personID, file) {
        const formData = new FormData();
        formData.append('file', file);

        showNotification('Загрузка файла доверенности...', 'info');

        return fetch(`/api/manager/${personID}/powerofattorney`, {
            method: 'POST',
            body: formData
        })
        .then(response => {
            if (!response.ok) {
                return response.json().then(err => { throw new Error(err.message || 'Ошибка сервера при загрузке файла доверенности'); });
            }
            return response.json();
        });
    }

    // --- Основной процесс сохранения --- 
    showNotification(`${isEditMode ? 'Обновление' : 'Добавление'} сопровождающего...`, 'info');
    
    executeMainRequest()
        .then(mainResult => {
            // Если это не режим редактирования И требуется доверенность И файл был выбран
            if (!isEditMode && isPowerOfAttorneyRequired && selectedPowerOfAttorneyFile) {
                const newAccompanyingPersonID = mainResult.accompanyingPersonID;
                if (!newAccompanyingPersonID) {
                    throw new Error('Не удалось получить ID нового сопровождающего после сохранения.');
                }
                // Выполняем загрузку файла
                return executeFileUpload(newAccompanyingPersonID, selectedPowerOfAttorneyFile)
                    .then(uploadResult => {
                        if (!uploadResult.success) {
                            // Ошибка при загрузке файла, но сопровождающий уже создан
                            throw new Error(`Сопровождающий создан (ID: ${newAccompanyingPersonID}), но не удалось загрузить доверенность: ${uploadResult.message}`);
                        }
                        return mainResult; // Возвращаем результат основного сохранения
                    });
            } else if (isEditMode && selectedPowerOfAttorneyFile) {
                // Если режим редактирования и был выбран НОВЫЙ файл для замены
                return executeFileUpload(accompanyingID, selectedPowerOfAttorneyFile)
                    .then(uploadResult => {
                        if (!uploadResult.success) {
                            throw new Error(`Данные сопровождающего обновлены, но не удалось загрузить новую доверенность: ${uploadResult.message}`);
                        }
                        return mainResult;
                    });
            }
            // Если файл не требовался или не был выбран (или режим редактирования без нового файла)
            return mainResult; 
        })
        .then(finalResult => {
            // Успешное завершение (основной запрос и, возможно, загрузка файла)
            showNotification(`Сопровождающий успешно ${isEditMode ? 'обновлен' : 'добавлен'}`, 'success');
            
            // Закрываем модальное окно
            document.getElementById('accompanyingModal').style.display = 'none';
            selectedPowerOfAttorneyFile = null; // Очищаем выбранный файл
            
            // Обновляем списки
            loadAccompanyingPersons();
            if (accompanyingData.NeedAccommodation) {
                loadAccommodations();
            }
        })
        .catch(error => {
            // Обработка любых ошибок (основной запрос или загрузка файла)
            console.error(`Ошибка при ${isEditMode ? 'обновлении' : 'добавлении'} сопровождающего:`, error);
            showNotification(error.message, 'error');
            // Не закрываем окно, если была ошибка
        });
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
    
    showNotification('Выполняется удаление...', 'info');
    
    // Удаляем сопровождающих последовательно, а не параллельно
    let successCount = 0;
    let failureMessages = [];
    
    // Создаем последовательный promise chain
    let promise = Promise.resolve();
    
    selectedAccompanyingIDs.forEach(id => {
      promise = promise.then(() => {
      return fetch(`/api/manager/accompanyingperson/${id}`, {
          method: 'DELETE',
          headers: {
            'Content-Type': 'application/json'
          }
      })
        .then(response => {
          if (!response.ok) {
            return response.text().then(text => {
              try {
                const data = JSON.parse(text);
                throw new Error(data.message || `Ошибка при удалении сопровождающего (ID: ${id})`);
              } catch (e) {
                throw new Error(`Ошибка при удалении сопровождающего (ID: ${id}): ${text}`);
              }
            });
          }
          return response.json();
        })
        .then(result => {
          if (result.success) {
            successCount++;
            return true;
          } else {
            failureMessages.push(`ID ${id}: ${result.message || 'Неизвестная ошибка'}`);
            return false;
          }
        })
        .catch(error => {
          console.error(`Ошибка при удалении сопровождающего ID ${id}:`, error);
          failureMessages.push(`ID ${id}: ${error.message}`);
          return false;
        });
      });
    });
    
    // Обработка результатов после выполнения всех операций удаления
    promise.then(() => {
        if (successCount > 0) {
        // Формируем текст уведомления
        let message = `Успешно удалено: ${successCount} сопровождающих`;
        showNotification(message, 'success');
        
        // Обновляем списки
          loadAccompanyingPersons();
        
        // Всегда обновляем таблицу размещения, так как 
        // сложно надежно определить, был ли у сопровождающего размещение
        loadAccommodations();
        
        // Очищаем выбранные ID
          selectedAccompanyingIDs = [];
        updateAccompanyingButtonStates();
        } else {
        showNotification('Не удалось удалить ни одного сопровождающего', 'error');
      }
      
      // Если были ошибки, показываем их в диалоговом окне
      if (failureMessages.length > 0) {
        let errorMessage = 'Не удалось удалить некоторых сопровождающих:\n\n';
        errorMessage += failureMessages.join('\n');
        alert(errorMessage);
        }
      })
      .catch(error => {
      console.error('Общая ошибка при удалении сопровождающих:', error);
      showNotification('Произошла ошибка при удалении сопровождающих', 'error');
      alert('Ошибка при удалении сопровождающих: ' + error.message);
      });
  }
  
  // Управление документами сопровождающего (новая функция)
  function manageAccompanyingPersonDocuments() {
    if (selectedAccompanyingIDs.length !== 1) {
      showNotification('Выберите одного сопровождающего для управления документами.', 'error');
      return;
    }

    const accompanyingPersonID = selectedAccompanyingIDs[0];
    const person = accompanyingPersons.find(p => p.AccompanyingPersonID === accompanyingPersonID);

    if (!person) {
      showNotification('Сопровождающий не найден.', 'error');
      return;
    }

    loadAccompanyingPersonDocuments(accompanyingPersonID, person.FullName, person.PatientName);
  }

  // Загрузка документов сопровождающего
  function loadAccompanyingPersonDocuments(accompanyingPersonID, accompanyingPersonName, patientName) {
    showNotification('Загрузка документов сопровождающего...', 'info');

    document.getElementById('accompanyingPersonDocumentsModalTitle').textContent = `Документы сопровождающего: ${accompanyingPersonName}`;
    document.getElementById('accompanyingPersonDocumentsInfo').textContent = `Сопровождающий: ${accompanyingPersonName}, Пациент: ${patientName}`;
    document.getElementById('accompanyingPersonDocumentsModal').style.display = 'block';

    const tbody = document.getElementById('accompanyingPersonDocumentsTable').querySelector('tbody');
    tbody.innerHTML = '<tr><td colspan="4" style="text-align: center;">Загрузка данных...</td></tr>';

    fetch(`/api/manager/accompanyingperson/${accompanyingPersonID}/documents`)
      .then(response => response.json())
      .then(data => {
        if (!data.success) {
          throw new Error(data.message || 'Не удалось загрузить документы сопровождающего');
        }

        const statusElement = document.getElementById('accompanyingPersonDocumentsStatus');
        statusElement.textContent = data.documentStatus;
        statusElement.style.color = data.documentStatus === 'Полный комплект' || data.documentStatus === 'Нет обязательных документов' ? '#4CAF50' : '#F44336';

        tbody.innerHTML = '';
        if (data.documents.length === 0) {
          tbody.innerHTML = '<tr><td colspan="4" style="text-align: center;">Нет документов для этого сопровождающего</td></tr>';
          return;
        }

        data.documents.forEach(doc => {
          const row = document.createElement('tr');
          row.dataset.documentTypeId = doc.DocumentTypeID;
          row.dataset.documentId = doc.DocumentID !== null ? doc.DocumentID : '';
          row.innerHTML = `
            <td>${doc.DocumentName}</td>
            <td>${doc.Status}</td>
            <td>${doc.UploadDate ? formatDate(new Date(doc.UploadDate)) : '-'}</td>
            <td>${doc.IsRequired ? 'Да' : 'Нет'}</td>
          `;
          if (doc.Status === 'Проверен') row.style.color = '#4CAF50';
          else if (doc.Status === 'Загружен') row.style.color = '#2196F3';
          tbody.appendChild(row);
        });

        initAccompanyingPersonDocumentsTableListeners(accompanyingPersonID);
        showNotification('Документы сопровождающего загружены', 'success');
      })
      .catch(error => {
        console.error('Ошибка при загрузке документов сопровождающего:', error);
        tbody.innerHTML = `<tr><td colspan="4" style="text-align: center; color: red;">Ошибка: ${error.message}</td></tr>`;
        showNotification('Ошибка при загрузке документов сопровождающего', 'error');
      });
  }

  // Инициализация обработчиков событий таблицы документов сопровождающего
  function initAccompanyingPersonDocumentsTableListeners(accompanyingPersonID) {
    const table = document.getElementById('accompanyingPersonDocumentsTable');
    
    // Массив для хранения выбранных строк
    const selectedRows = [];
    
    // Очищаем предыдущие обработчики
    const newTable = table.cloneNode(true);
    table.parentNode.replaceChild(newTable, table);

    // Добавляем обработчик клика по строкам таблицы
    newTable.addEventListener('click', function(e) {
      const row = e.target.closest('tr');
      if (!row) return;
      
      const isCtrlPressed = e.ctrlKey || e.metaKey;
      
      // Если не нажат Ctrl, очищаем выделение
      if (!isCtrlPressed) {
        this.querySelectorAll('tbody tr').forEach(r => r.classList.remove('selected'));
        selectedRows.length = 0; // Очищаем массив
      }
      
      // Если строка уже выбрана и нажат Ctrl, снимаем выделение
      if (row.classList.contains('selected') && isCtrlPressed) {
        row.classList.remove('selected');
        const index = selectedRows.indexOf(row);
        if (index > -1) selectedRows.splice(index, 1);
      } else {
        // Иначе добавляем выделение
        row.classList.add('selected');
        if (!selectedRows.includes(row)) {
          selectedRows.push(row);
        }
      }
      
      // Обновляем состояние кнопок на основе выбранных строк
      updateAccompanyingDocumentButtonStates(selectedRows);
    });

    // Обработчик двойного клика для просмотра документа
    newTable.addEventListener('dblclick', function(e) {
      const row = e.target.closest('tr');
      if (!row || !row.dataset.documentId) return;
      viewAccompanyingPersonDocument(row.dataset.documentId);
    });

    // Инициализация кнопок управления документами
    document.getElementById('uploadAccompanyingPersonDocumentBtn').addEventListener('click', function() {
      const selectedRow = newTable.querySelector('tbody tr.selected');
      if (!selectedRow) {
        showNotification('Выберите тип документа для загрузки', 'error');
        return;
      }
      
      // Сохраняем ID сопровождающего и типа документа
      document.getElementById('uploadAccompanyingPersonDocumentTypeID').value = selectedRow.dataset.documentTypeId;
      document.getElementById('uploadAccompanyingPersonID').value = accompanyingPersonID;
      
      // Вместо показа модального окна сразу открываем выбор файла
      document.getElementById('uploadAccompanyingPersonDocumentFile').click();
    });
    
    // Обработчик события выбора файла
    document.getElementById('uploadAccompanyingPersonDocumentFile').addEventListener('change', function(e) {
      if (e.target.files.length > 0) {
        uploadAccompanyingPersonDocument();
      }
    });

    document.getElementById('viewAccompanyingPersonDocumentBtn').addEventListener('click', function() {
      const selectedRows = newTable.querySelectorAll('tbody tr.selected');
      if (selectedRows.length !== 1 || !selectedRows[0].dataset.documentId) {
        showNotification('Выберите один загруженный документ для просмотра/скачивания', 'error');
        return;
      }
      viewAccompanyingPersonDocument(selectedRows[0].dataset.documentId);
    });

    document.getElementById('deleteAccompanyingPersonDocumentBtn').addEventListener('click', function() {
      const selectedRows = newTable.querySelectorAll('tbody tr.selected');
      const documentsToDelete = [];
      
      selectedRows.forEach(row => {
        if (row.dataset.documentId) {
          documentsToDelete.push(row.dataset.documentId);
        }
      });
      
      if (documentsToDelete.length === 0) {
        showNotification('Выберите хотя бы один загруженный документ для удаления', 'error');
        return;
      }
      
      const confirmMessage = documentsToDelete.length === 1 
        ? 'Вы действительно хотите удалить этот документ?' 
        : `Вы действительно хотите удалить ${documentsToDelete.length} выбранных документов?`;
      
      if (confirm(confirmMessage)) {
        deleteAccompanyingPersonDocument(documentsToDelete, accompanyingPersonID);
      }
    });
    
    // По умолчанию отключаем кнопки просмотра и удаления
    document.getElementById('viewAccompanyingPersonDocumentBtn').disabled = true;
    document.getElementById('deleteAccompanyingPersonDocumentBtn').disabled = true;
    
    // Функция обновления состояния кнопок на основе выбранных строк
    function updateAccompanyingDocumentButtonStates(rows) {
      const hasUploadableDoc = rows.length === 1;
      const hasViewableDoc = rows.length === 1 && rows[0].dataset.documentId;
      const hasDeletableDoc = rows.some(row => row.dataset.documentId);
      
      document.getElementById('uploadAccompanyingPersonDocumentBtn').disabled = !hasUploadableDoc;
      document.getElementById('viewAccompanyingPersonDocumentBtn').disabled = !hasViewableDoc;
      document.getElementById('deleteAccompanyingPersonDocumentBtn').disabled = !hasDeletableDoc;
      
      // Устанавливаем текст кнопки загрузки для одной выбранной строки
      if (hasUploadableDoc) {
        const documentStatus = rows[0].cells[1].textContent.trim();
        const isUploaded = documentStatus !== 'Не загружен';
        const uploadBtn = document.getElementById('uploadAccompanyingPersonDocumentBtn');
        
        // Сохраняем ID типа документа для последующей загрузки
        document.getElementById('uploadAccompanyingPersonDocumentTypeID').value = rows[0].dataset.documentTypeId;
        
        uploadBtn.querySelector('span').textContent = isUploaded ? 'Заменить' : 'Загрузить';
      }
    }
  }

  // Функция showUploadAccompanyingPersonDocumentModal больше не нужна и удаляется

  // Загрузка документа сопровождающего
  function uploadAccompanyingPersonDocument() {
    const documentTypeId = document.getElementById('uploadAccompanyingPersonDocumentTypeID').value;
    const accompanyingPersonId = document.getElementById('uploadAccompanyingPersonID').value;
    const documentFile = document.getElementById('uploadAccompanyingPersonDocumentFile').files[0];
    const notes = document.getElementById('uploadAccompanyingPersonDocumentNotes').value;

    if (!documentFile) {
      showNotification('Выберите файл для загрузки', 'error');
      return;
    }
    
    const allowedTypes = ['image/jpeg', 'image/png', 'application/pdf', 'application/msword', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
    if (!allowedTypes.includes(documentFile.type)) {
      showNotification('Поддерживаемые форматы: JPEG, PNG, PDF, DOC, DOCX', 'error');
      return;
    }

    const formData = new FormData();
    formData.append('file', documentFile);
    formData.append('documentTypeID', documentTypeId);
    if (notes) formData.append('notes', notes);

    showNotification('Загрузка документа...', 'info');
    
    fetch(`/api/manager/accompanyingperson/${accompanyingPersonId}/document`, {
      method: 'POST',
      body: formData
    })
    .then(response => response.json())
    .then(result => {
      if (result.success) {
        // Очищаем поле выбора файла
        document.getElementById('uploadAccompanyingPersonDocumentFile').value = '';
        
        // Обновляем список документов
        const person = accompanyingPersons.find(p => p.AccompanyingPersonID == accompanyingPersonId);
        if (person) {
            loadAccompanyingPersonDocuments(accompanyingPersonId, person.FullName, person.PatientName);
        } else {
            loadAccompanyingPersons(); // Fallback if person details not readily available
        }
        
        showNotification('Документ успешно загружен', 'success');
      } else {
        showNotification(`Ошибка при загрузке: ${result.message}`, 'error');
      }
    })
    .catch(error => {
      console.error('Ошибка:', error);
      showNotification('Ошибка при загрузке документа', 'error');
    });
  }

  // Просмотр/скачивание документа сопровождающего
  function viewAccompanyingPersonDocument(documentId) {
    window.open(`/api/manager/accompanyingperson/document/${documentId}/view`, '_blank');
  }

  // Удаление документа(ов) сопровождающего
  function deleteAccompanyingPersonDocument(documentIds, accompanyingPersonId) {
    // Преобразуем в массив, если передан один ID
    const idsArray = Array.isArray(documentIds) ? documentIds : [documentIds];
    
    // Показываем уведомление
    const message = idsArray.length === 1 
      ? 'Удаление документа...' 
      : `Удаление ${idsArray.length} документов...`;
    
    showNotification(message, 'info');
    
    // Счетчики для отслеживания прогресса
    let successCount = 0;
    let errorCount = 0;
    let requestsCompleted = 0;
    
    // Функция для проверки завершения всех запросов
    function checkAllRequestsCompleted() {
      requestsCompleted++;
      if (requestsCompleted === idsArray.length) {
        // Все запросы выполнены, выводим итоговое сообщение
        if (errorCount === 0) {
          const successMessage = idsArray.length === 1 
            ? 'Документ успешно удален' 
            : `${successCount} документов успешно удалено`;
          showNotification(successMessage, 'success');
        } else {
          showNotification(`Удалено ${successCount} из ${idsArray.length} документов. Ошибок: ${errorCount}`, 'warning');
        }
        
        // Обновляем список документов
        const person = accompanyingPersons.find(p => p.AccompanyingPersonID == accompanyingPersonId);
        if (person) {
            loadAccompanyingPersonDocuments(accompanyingPersonId, person.FullName, person.PatientName);
        } else {
            loadAccompanyingPersons(); // Fallback if person details not readily available
        }
      }
    }
    
    // Отправляем запросы на удаление для каждого документа
    idsArray.forEach(documentId => {
      fetch(`/api/manager/accompanyingperson/document/${documentId}`, {
        method: 'DELETE'
      })
      .then(response => response.json())
      .then(result => {
        if (result.success) {
          successCount++;
        } else {
          errorCount++;
          console.error(`Ошибка при удалении документа ${documentId}: ${result.message}`);
        }
      })
      .catch(error => {
        errorCount++;
        console.error('Ошибка при удалении документа:', error);
      })
      .finally(() => {
        checkAllRequestsCompleted();
      });
    });
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
    document.getElementById('documentCategory').value = 'Медицинские документы'; // Значение по умолчанию
    
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
}); 