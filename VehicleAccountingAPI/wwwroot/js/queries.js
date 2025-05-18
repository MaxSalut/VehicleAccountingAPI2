document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('currentYear').textContent = new Date().getFullYear();

    const queryCategorySelect = document.getElementById('queryCategorySelect');
    const querySelect = document.getElementById('querySelect');
    const queryDetailsContainer = document.getElementById('queryDetailsContainer');
    const queryTitle = document.getElementById('queryTitle');
    const queryDescription = document.getElementById('queryDescription');
    const queryParametersForm = document.getElementById('queryParametersForm');
    const executeQueryButton = document.getElementById('executeQueryButton');
    const resultsContainer = document.getElementById('resultsContainer');
    const resultsTableHead = document.getElementById('resultsTableHead');
    const resultsTableBody = document.getElementById('resultsTableBody');
    const resultsSeparator = document.getElementById('resultsSeparator');
    const noResultsMessage = document.getElementById('noResultsMessage');
    const errorMessage = document.getElementById('errorMessage');

    const baseApiUrl = 'https://localhost:44322/api/Queries'; // Перевірте, чи це ваш правильний URL

    // Об'єкт для перекладу заголовків таблиці
    const headerTranslations = {
        "driverId": "ID Водія",
        "name": "Ім'я",
        "email": "Email",
        "licenseNumber": "Номер посвідчення",
        "vehicleId": "ID ТЗ",
        "licensePlate": "Номерний знак",
        "model": "Модель",
        "year": "Рік",
        "vehicleTypeName": "Тип ТЗ",
        "assignmentId": "ID Призначення",
        "startDate": "Дата початку",
        "endDate": "Дата завершення",
        "maintenanceRecordId": "ID Запису ТО",
        "maintenanceDate": "Дата ТО",
        "description": "Опис",
        "cost": "Вартість",
        "tripId": "ID Поїздки",
        "routeDescription": "Маршрут",
        "cargoDescription": "Вантаж",
        "plannedStartDateTime": "План. початок",
        "plannedEndDateTime": "План. завершення",
        "actualStartDateTime": "Факт. початок",
        "actualEndDateTime": "Факт. завершення",
        "status": "Статус",
        "tripLogId": "ID Журналу",
        "logDate": "Дата запису",
        "driverName": "Водій",
        "vehicleLicensePlate": "Номер ТЗ",
        "tripRoute": "Маршрут поїздки",
        "startMileage": "Поч. пробіг (км)",
        "endMileage": "Кінц. пробіг (км)",
        "calculatedMileage": "Пробіг (км)",
        "fuelConsumedLiters": "Пальне (л)",
        "notes": "Примітки",
        "driver1_Id": "ID Водія 1",
        "driver1_Name": "Ім'я Водія 1",
        "driver2_Id": "ID Водія 2",
        "driver2_Name": "Ім'я Водія 2"
        // Додавайте сюди інші ключі, які повертає ваш API, та їх українські відповідники
    };

    function translateHeader(headerKey) {
        return headerTranslations[headerKey] || headerKey.replace(/([A-Z])/g, ' $1').trim();
    }

    const queries = {
        simple: [
            {
                id: 'S1',
                name: 'Водії за типом ТЗ',
                description: 'Знайти всіх водіїв, які були призначені на транспортні засоби вказаного типу.',
                endpoint: `${baseApiUrl}/drivers-by-vehicle-type`,
                params: [
                    { id: 'vehicleTypeName', label: 'Назва типу ТЗ', type: 'text', required: true, placeholder: 'Наприклад, Вантажівка' }
                ]
            },
            {
                id: 'S2',
                name: 'ТЗ за вартістю та датою ТО',
                description: 'Вивести ТЗ, що проходили ТО після вказаної дати, і вартість ТО яких перевищила певну суму.',
                endpoint: `${baseApiUrl}/vehicles-by-maintenance`,
                params: [
                    { id: 'minCost', label: 'Мінімальна вартість ТО (грн)', type: 'number', step: '0.01', required: true, placeholder: 'Наприклад, 1000.00' },
                    { id: 'dateAfter', label: 'ТО після дати', type: 'date', required: true }
                ]
            },
            {
                id: 'S3',
                name: 'Поїздки водія за період',
                description: 'Знайти поїздки певного водія, що фактично розпочалися у вказаний період.',
                endpoint: `${baseApiUrl}/trips-by-driver-period`,
                params: [
                    { id: 'driverId', label: 'ID Водія', type: 'number', required: true, placeholder: 'Наприклад, 1' },
                    { id: 'periodStartDate', label: 'Дата початку періоду', type: 'datetime-local', required: true },
                    { id: 'periodEndDate', label: 'Дата кінця періоду', type: 'datetime-local', required: true }
                ]
            },
            {
                id: 'S4',
                name: 'Активно призначені ТЗ (старші за рік)',
                description: 'Вивести ТЗ з активними призначеннями, старші за вказаний рік випуску.',
                endpoint: `${baseApiUrl}/active-vehicles-older-than`,
                params: [
                    { id: 'yearOlderThan', label: 'Рік випуску до (не включно)', type: 'number', required: true, placeholder: 'Наприклад, 2020 (покаже ТЗ до 2019)' }
                ]
            },
            {
                id: 'S5',
                name: 'Журнал поїздок (пробіг > X для ТЗ)',
                description: 'Записи журналу для ТЗ, де пробіг (EndMileage - StartMileage) перевищив X км.',
                endpoint: `${baseApiUrl}/triplogs-by-mileage`,
                params: [
                    { id: 'vehicleId', label: 'ID Транспортного засобу', type: 'number', required: true, placeholder: 'Наприклад, 1' },
                    { id: 'minMileage', label: 'Мінімальний пробіг (км)', type: 'number', required: true, placeholder: 'Наприклад, 100' }
                ]
            }
        ],
        complex: [
            {
                id: 'C1',
                name: 'Водії, що керували ВСІМА ТЗ типу',
                description: 'Знайти водіїв, які мали призначення на кожен ТЗ вказаного типу.',
                endpoint: `${baseApiUrl}/drivers-all-vehicles-of-type`,
                params: [
                    { id: 'vehicleTypeName', label: 'Назва типу ТЗ', type: 'text', required: true, placeholder: 'Наприклад, Автобус' }
                ]
            },
            {
                id: 'C2',
                name: 'ТЗ з ТО ТІЛЬКИ у вказаному році',
                description: 'Знайти ТЗ, всі записи ТО яких припадають виключно на вказаний рік.',
                endpoint: `${baseApiUrl}/vehicles-maintenance-only-in-year`,
                params: [
                    { id: 'maintenanceYear', label: 'Рік ТО', type: 'number', required: true, placeholder: 'Наприклад, 2024' }
                ]
            },
            {
                id: 'C3',
                name: 'Пари водіїв з однаковими наборами ТЗ',
                description: 'Знайти унікальні пари водіїв, призначених на точно таку саму множину ТЗ.',
                endpoint: `${baseApiUrl}/driver-pairs-same-vehicles`,
                params: []
            }
        ]
    };

    queryCategorySelect.addEventListener('change', function () {
        const selectedCategory = this.value;
        querySelect.innerHTML = '<option value="">Оберіть запит...</option>';
        querySelect.disabled = true;
        queryDetailsContainer.style.display = 'none';
        hideResults();

        if (selectedCategory && queries[selectedCategory]) {
            queries[selectedCategory].forEach(query => {
                const option = document.createElement('option');
                option.value = query.id;
                option.textContent = query.name;
                querySelect.appendChild(option);
            });
            querySelect.disabled = false;
        }
    });

    querySelect.addEventListener('change', function () {
        const selectedQueryId = this.value;
        const selectedCategory = queryCategorySelect.value;
        hideResults();

        if (selectedQueryId && selectedCategory && queries[selectedCategory]) {
            const selectedQuery = queries[selectedCategory].find(q => q.id === selectedQueryId);
            if (selectedQuery) {
                queryTitle.textContent = selectedQuery.name;
                queryDescription.innerHTML = selectedQuery.description.replace(/\n/g, '<br>');
                queryParametersForm.innerHTML = '';

                if (selectedQuery.params.length > 0) {
                    selectedQuery.params.forEach(param => {
                        const paramGroup = document.createElement('div');
                        paramGroup.className = 'param-group mb-3'; // Додано mb-3 для відступу

                        const label = document.createElement('label');
                        label.htmlFor = param.id;
                        label.className = 'form-label';
                        label.textContent = param.label + (param.required ? ' *' : '');
                        paramGroup.appendChild(label);

                        const input = document.createElement('input');
                        input.type = param.type;
                        input.id = param.id;
                        input.name = param.id;
                        input.className = 'form-control';
                        if (param.required) input.required = true;
                        if (param.placeholder) input.placeholder = param.placeholder;
                        if (param.type === 'number' && param.step) input.step = param.step;
                        if (param.type === 'date' || param.type === 'datetime-local') {
                            input.min = "2000-01-01" + (param.type === 'datetime-local' ? "T00:00" : "");
                            // Обмеження максимальної дати (можна налаштувати)
                            // if(param.type === 'date') input.max = new Date().toISOString().split('T')[0];
                        }
                        paramGroup.appendChild(input);
                        queryParametersForm.appendChild(paramGroup);
                    });
                } else {
                    queryParametersForm.innerHTML = '<p>Для цього запиту параметри не потрібні.</p>';
                }
                queryDetailsContainer.style.display = 'block';
            }
        } else {
            queryDetailsContainer.style.display = 'none';
        }
    });

    executeQueryButton.addEventListener('click', async function () {
        const selectedQueryId = querySelect.value;
        const selectedCategory = queryCategorySelect.value;
        if (!selectedQueryId || !selectedCategory) {
            alert('Будь ласка, оберіть запит.');
            return;
        }

        const selectedQuery = queries[selectedCategory].find(q => q.id === selectedQueryId);
        if (!selectedQuery) {
            alert('Обраний запит не знайдено.');
            return;
        }

        hideResults();
        let queryParams = new URLSearchParams();
        let formIsValid = true;

        // Очищення попередніх повідомлень про помилки валідації для полів
        selectedQuery.params.forEach(param => {
            const errorField = document.getElementById(param.id + 'Error'); // Якщо у вас є такі поля
            if (errorField) errorField.textContent = '';
            const inputField = document.getElementById(param.id);
            if (inputField) inputField.classList.remove('is-invalid');
        });


        for (const param of selectedQuery.params) { // Змінено на for...of для коректної роботи return з formIsValid
            const inputElement = document.getElementById(param.id);
            if (inputElement) {
                if (param.required && !inputElement.value) {
                    alert(`Будь ласка, заповніть обов'язкове поле: ${param.label}`);
                    inputElement.focus();
                    inputElement.classList.add('is-invalid'); // Показати візуально помилку
                    formIsValid = false;
                    break;
                }
                if (inputElement.value) {
                    queryParams.append(param.id, inputElement.value);
                }
            }
        }

        if (!formIsValid) return;

        const fullUrl = selectedQuery.params.length > 0 ? `${selectedQuery.endpoint}?${queryParams.toString()}` : selectedQuery.endpoint;

        resultsTitle.textContent = `Результати запиту: "${selectedQuery.name}"`; // Показуємо заголовок одразу
        resultsContainer.style.display = 'block';
        resultsSeparator.style.display = 'block';

        try {
            const response = await fetch(fullUrl);

            if (response.status === 404) {
                noResultsMessage.textContent = `Даних за запитом "${selectedQuery.name}" не знайдено.`;
                noResultsMessage.style.display = 'block';
                resultsTableHead.innerHTML = ''; // Очищаємо заголовки
                resultsTableBody.innerHTML = ''; // Очищаємо тіло таблиці
                return;
            }

            if (!response.ok) {
                let errorData;
                try {
                    errorData = await response.json();
                } catch (e) {
                    errorData = { message: `HTTP помилка! Статус: ${response.status} ${response.statusText}` };
                }
                throw new Error(errorData.message || errorData.title || `Статус: ${response.status}`);
            }

            const data = await response.json();
            displayResults(data, selectedQuery.name); // Передаємо queryName для узгодженості, хоча він вже встановлений

        } catch (err) {
            console.error('Помилка виконання запиту:', err);
            errorMessage.textContent = `Помилка: ${err.message}`;
            errorMessage.style.display = 'block';
            resultsTableHead.innerHTML = '';
            resultsTableBody.innerHTML = '';
            noResultsMessage.style.display = 'none';
        }
    });

    function hideResults() {
        resultsContainer.style.display = 'none';
        resultsTableHead.innerHTML = '';
        resultsTableBody.innerHTML = '';
        noResultsMessage.style.display = 'none';
        errorMessage.style.display = 'none';
        resultsSeparator.style.display = 'none';
    }

    function displayResults(data, queryName) {
        // resultsTitle вже встановлено перед fetch
        resultsTableHead.innerHTML = ''; // Очищаємо попередні заголовки
        resultsTableBody.innerHTML = ''; // Очищаємо попередні дані

        if (!data || data.length === 0) {
            noResultsMessage.textContent = `Даних за запитом "${queryName}" не знайдено.`;
            noResultsMessage.style.display = 'block';
            return; // Виходимо, якщо даних немає
        }
        noResultsMessage.style.display = 'none'; // Ховаємо повідомлення, якщо дані є

        const headers = Object.keys(data[0]);
        const headerRow = document.createElement('tr');
        headers.forEach(headerText => {
            const th = document.createElement('th');
            th.textContent = translateHeader(headerText); // Використовуємо функцію перекладу
            headerRow.appendChild(th);
        });
        resultsTableHead.appendChild(headerRow);

        data.forEach(item => {
            const row = document.createElement('tr');
            headers.forEach(header => {
                const td = document.createElement('td');
                let value = item[header];

                if (typeof value === 'string' && (value.includes('T') || value.match(/^\d{4}-\d{2}-\d{2}$/))) {
                    try {
                        const dateObj = new Date(value);
                        if (!isNaN(dateObj)) {
                            // Перевірка, чи це тільки дата (без значущого часу)
                            const isOnlyDate = (dateObj.getUTCHours() === 0 && dateObj.getUTCMinutes() === 0 && dateObj.getUTCSeconds() === 0 && dateObj.getUTCMilliseconds() === 0) || value.length === 10;
                            if (isOnlyDate) {
                                value = dateObj.toLocaleDateString('uk-UA');
                            } else {
                                value = dateObj.toLocaleString('uk-UA', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' });
                            }
                        }
                    } catch (e) { /* залишити як є */ }
                } else if (typeof value === 'number' && !Number.isInteger(value)) {
                    value = value.toFixed(2);
                }
                td.textContent = (value === null || value === undefined) ? 'N/A' : value;
                row.appendChild(td);
            });
            resultsTableBody.appendChild(row);
        });
    }
});