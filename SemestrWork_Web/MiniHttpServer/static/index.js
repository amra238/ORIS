console.log("JavaScript файл загружен");

const API_URL = '/homecontroller/tours';

function showAuthModal() {
    document.getElementById('auth-modal').style.display = 'block';
}
function hideAuthModal() {
    document.getElementById('auth-modal').style.display = 'none';
}

async function submitAuth() {
    const username = document.getElementById('auth-username').value.trim();
    const email = document.getElementById('auth-email').value.trim();
    const password = document.getElementById('auth-password').value;

    if (!email || !password) {
        alert("Email и пароль обязательны.");
        return;
    }

    const payload = { email, password };
    if (username) payload.username = username;

    const res = await fetch('/homecontroller/auth', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });
    const data = await res.json();

    if (data.success) {
        hideAuthModal();
        document.getElementById('auth-text').textContent = `Привет, ${data.user.username}!`;

        const adminBtn = document.getElementById('admin-panel-btn');
        if (adminBtn) adminBtn.remove();

        if (data.user.role === 'admin') {
            showAdminPanelButton();
        }
    } else {
        alert(data.message);
    }
}

// Проверка сессии при загрузке страницы
async function checkAuthOnLoad() {
    const res = await fetch('/homecontroller/check-auth');
    const data = await res.json();

    if (data.authenticated) {
        document.getElementById('auth-text').textContent = `Привет, ${data.user.username}!`;

        const adminBtn = document.getElementById('admin-panel-btn');
        if (adminBtn) adminBtn.remove();

        if (data.user.role === 'admin') {
            showAdminPanelButton();
        }
    }
}

let hotTourOffset = 0;
let hotTourCards = [];

function initHotTourCarousel() {
    const sliderContainer = document.getElementById('hot-tours-slider-wrapper');
    const slider = document.getElementById('hot-tours-slider');
    const cards = Array.from(slider.querySelectorAll('.tour-card-hot'));
    const btnPrev = sliderContainer.querySelector('.nav-btn.prev');
    const btnNext = sliderContainer.querySelector('.nav-btn.next');

    const visible = 4;
    let page = 0;
    const totalPages = Math.ceil(cards.length / visible);

    cards.forEach(card => card.style.display = 'none');

    function showPage(p) {
        cards.forEach((card, i) => {
            card.style.display = (i >= p * visible && i < (p + 1) * visible)
                ? 'block'
                : 'none';
        });
    }

    showPage(page);

    if (btnNext) {
        btnNext.addEventListener('click', () => {
            page = (page + 1) % totalPages;
            showPage(page);
        });
    }

    if (btnPrev) {
        btnPrev.addEventListener('click', () => {
            page = (page - 1 + totalPages) % totalPages;
            showPage(page);
        });
    }
}

function generateRatingStars(rating) {
    let stars = '';
    for (let i = 1; i <= 5; i++) {
        stars += i <= rating ? '★' : '☆';
    }
    return stars;
}

// сбор данных из формы поиска
function getSearchFilters() {
    const filters = {};

    const departureCityInput = document.querySelector('input[placeholder="Город вылета"]');
    if (departureCityInput && departureCityInput.value.trim()) {
        filters.departure_city = departureCityInput.value.trim();
    }

    const arrivalCityInput = document.querySelector('input[placeholder="Введите страну, город или отель..."]');
    if (arrivalCityInput && arrivalCityInput.value.trim()) {
        filters.arrival_city = arrivalCityInput.value.trim();
    }

    const departureDateInput = document.querySelector('input[placeholder="Выберите даты вылета"]');
    if (departureDateInput && departureDateInput.value.trim()) {
        const dateValue = departureDateInput.value.trim();
        const parsedDate = new Date(dateValue);
        if (!isNaN(parsedDate.getTime())) {
            filters.departure_date = parsedDate.toISOString().split('T')[0];
        }
    }

    const nightsSelect = document.querySelector('input[placeholder="Выбрать"]');
    if (nightsSelect && nightsSelect.value.trim()) {
        const nightsCount = parseInt(nightsSelect.value.trim());
        if (!isNaN(nightsCount)) {
            filters.nights_count = nightsCount;
        }
    }

    const peopleSelect = document.querySelector('input[placeholder="2 взр."]');
    if (peopleSelect && peopleSelect.value.trim()) {
        const peopleCount = parseInt(peopleSelect.value.trim().replace(/\D/g, ''));
        if (!isNaN(peopleCount)) {
            filters.people_count = peopleCount;
        }
    }

    console.log('Собранные фильтры:', filters);
    return filters;
}

// Функция рендеринга тура
function renderTour(tourData) {
    const template = document.getElementById('tour-template');
    if (!template) {
        console.error('Шаблон не найден');
        return null;
    }

    const tourElement = template.content.cloneNode(true);

    const selectButton = tourElement.querySelector('.select-button');
    if (selectButton && tourData.id) {
        selectButton.addEventListener('click', (e) => {
            e.preventDefault();
            window.location.href = `/homecontrollerendpoint/details?id=${tourData.id}`;
        });
    }
    else {
        selectButton.disabled = true;
    }

    // Заполняем данные
    const elements = tourElement.querySelectorAll('[data-bind]');
    elements.forEach(element => {
        const property = element.getAttribute('data-bind');
        const prefix = element.getAttribute('data-prefix') || '';
        const suffix = element.getAttribute('data-suffix') || '';

        let value = '—';
        if (tourData && tourData[property] !== undefined && tourData[property] !== null) {
            value = tourData[property];
        }

        if (property === 'rating') {
            const ratingNum = parseInt(value) || 0;
            element.textContent = generateRatingStars(Math.max(0, Math.min(5, ratingNum)));
        } else if (property === 'people_count') {
            element.textContent = `${prefix}${value}${suffix}`;
        } else {
            element.textContent = value;
        }
    });

    const imgElement = tourElement.querySelector('.tour-image img');
    if (imgElement && tourData.image_url) {
        imgElement.src = tourData.image_url;
    }

    return tourElement;
}

// загрузка туров 
async function loadToursFromApi(filters = {}) {
    try {
        console.log('Загрузка туров с API...');
        let url = new URL(API_URL, window.location.origin);
        console.log('что-то прошло')

        Object.keys(filters).forEach(key => {
            if (filters[key] !== undefined && filters[key] !== null) {
                url.searchParams.append(key, filters[key]);
            }
        });

        console.log('URL запроса:', url.toString());

        const response = await fetch(url, {
            method: 'GET',
            headers: {
                'Accept': 'application/json'
            }
        });

        console.log('Статус ответа:', response.status);
        console.log('URL ответа:', response.url);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        console.log('Ответ от API:', result);

        if (result.success && result.tours) {
            console.log(`Получено ${result.tours.length} туров`);

            const toursWithComputed = result.tours.map(tour => {
                return {
                    ...tour,
                    formatted_date: new Date(tour.departure_date).toLocaleDateString('ru-RU'),
                    nights_display: `${tour.nights_count} ночей`,
                    people_display: `${tour.people_count} чел.`,
                    price_display: `${tour.tour_price.toLocaleString('ru-RU')} ₽`,
                    image_url: tour.image_url
                };
            });

            return toursWithComputed;
        } else {
            throw new Error(result.error || 'сервер барахлит');
        }
    } catch (error) {
        console.error('Ошибка загрузки туров:', error);
        throw error;
    }
}

//  ошибки окно
function showError(message) {
    const container = document.getElementById('tours-container');
    container.innerHTML = `
            <div class="error-message">
                <h3>Ошибка загрузки туров</h3>
                <p>${message}</p>
                <p><strong>Проверьте:</strong></p>                
                <button onclick="initTours()" style="margin-top: 10px; padding: 8px 16px; background: #f44336; color: white; border: none; border-radius: 4px; cursor: pointer;">
                    Попробовать снова
                </button>
            </div>
        `;
}

// инициализации туров
async function initTours(filters = {}) {
    console.log('Инициализация туров...');
    const container = document.getElementById('tours-container');
    if (!container) {
        console.error('Контейнер не найден');
        return;
    }
    try {
        container.innerHTML = '<div class="loading">Загрузка туров...</div>';

        const toursData = await loadToursFromApi(filters);

        container.innerHTML = '';
        if (toursData.length === 0) {
            container.innerHTML = '<div class="loading">Туры по вашему запросу не найдены</div>';
            return;
        }

        toursData.forEach((tour, index) => {
            console.log(`Рендерим тур ${index + 1}:`, tour.HotelName);
            const tourElement = renderTour(tour);
            if (tourElement) {
                container.appendChild(tourElement);
            }
        });
        console.log(`Успешно отображено ${toursData.length} туров`);
    } catch (error) {
        console.error('ошибка:', error);
        showError(`Не удалось загрузить туры: ${error.message}`);
    }
}

function showAdminPanelButton() {
    const searchBtn = document.querySelector('.search-btn');
    if (!searchBtn || document.getElementById('admin-panel-btn')) return;

    const btn = document.createElement('button');
    btn.id = 'admin-panel-btn';
    btn.textContent = 'Административная панель';
    btn.style.cssText = `
        margin-bottom: 10px;
        padding: 8px 16px;
        background-color: #4CAF50;
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-size: 14px;
    `;
    btn.onclick = () => {
        document.getElementById('admin-panel').style.display = 'block';
    };
    searchBtn.parentNode.insertBefore(btn, searchBtn);
}

document.addEventListener('DOMContentLoaded', () => {
    checkAuthOnLoad();
    initHotTourCarousel();

    const searchButton = document.querySelector('.search-btn');
    const closeBtn = document.getElementById('admin-close-btn');
    const submitBtn = document.getElementById('admin-submit-btn');

    if (closeBtn) {
        closeBtn.onclick = () => {
            document.getElementById('admin-panel').style.display = 'none';
        };
    }

    if (submitBtn) {
        submitBtn.onclick = async () => {
            const form = document.getElementById('admin-tour-form');
            const formData = new FormData(form);
            const tourData = {};

            for (let [key, value] of formData.entries()) {
                if (key === 'tour_price') {
                    tourData[key] = parseFloat(value);
                } else if (['nights_count', 'people_count', 'rating', 'adult_pools_count', 'children_pools_count'].includes(key)) {
                    tourData[key] = parseInt(value);
                } else {
                    tourData[key] = value;
                }
            }

            try {
                const res = await fetch('/homecontroller/add-tour', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(tourData)
                });

                const result = await res.json();
                if (result.success) {
                    alert('Тур успешно добавлен');
                    document.getElementById('admin-panel').style.display = 'none';
                    initTours();
                } else {
                    alert('Ошибка: ' + (result.message || 'неизвестная ошибка'));
                }
            } catch (e) {
                console.error(e);
                alert('Ошибка при добавлении тура');
            }
        };
    }

    if (searchButton) {
        searchButton.addEventListener('click', (e) => {
            e.preventDefault();
            console.log('Нажата кнопка "Поиск"');

            const filters = getSearchFilters();

            initTours(filters);
        });
    } else {
        console.warn('фигни нету');
    }

    initTours();
    initHotTourCarousel();
});