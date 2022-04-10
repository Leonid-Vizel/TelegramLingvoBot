# PARK - тренажер для переводчика. [LingvoHack.](https://kpfu.ru/zrk/spikery-hakatona-lingvohack-411306.html)

## О проекте

[PARK Bot](https://t.me/LingvoHackBot)

Наш Бот поможет повысишь технику письменного перевода 🔥

📚 Выбирай темы, в которых хочешь прокачаться.

✍🏻 Большая библиотека вручную подобранных специализированных текстов.

🔔 Не забудь включить уведомления - мы напомним выполнить задание.

✅ Получи обратную связь с оценкой качества твоего перевода.

👨🏻‍🎓 Ты сам можешь стать экспертам, оставив заявку, мы рассмотрим ее в течение суток.




## To-do list

- [x] Архитектура бота
- [x] Архитектура базы данных 
- [x] Модель для проверки грамматики
- [x] Добавление магазина
- [x] Наполнение контентом
- [x] Оценка работы
- [x] Просмотр проверенных работ 
- [x] Устранение багов
- [ ] Админ панель 
- [ ] Автоматизация выплат

## Как запустить:
1. Install and run Docker
2. Build Docker image using `docker build . -t [IMAGE NAME]`
3. Run Docker container using `docker run --rm -it -p 8080:8080 [IMAGE NAME]`
4. Go to `http://localhost:8080/`

## Исходный код
* [Answer.cs](https://github.com/Leonid-Vizel/TelegramLingvoBot/blob/master/TelegramLingvoBot/Answer.cs) - Ответы пользователя.
* [AwaitingAnswer.cs](https://github.com/Leonid-Vizel/TelegramLingvoBot/blob/master/TelegramLingvoBot/AwaitingAsnwer.cs) - Ожидание пока пользователь переведёт текст 
* [ButtonBank.cs](https://github.com/Leonid-Vizel/TelegramLingvoBot/blob/master/TelegramLingvoBot/ButtonBank.cs) - Шаблононы кнопок
* [DatabaseInteractions.cs](https://github.com/Leonid-Vizel/TelegramLingvoBot/blob/master/TelegramLingvoBot/DataBaseInteractions.cs) - Взаимодействия с базой данных
* [FileTXTInteractions.cs](https://github.com/Leonid-Vizel/TelegramLingvoBot/blob/master/TelegramLingvoBot/FileTXTInteractions.cs) - Взаимодействия с текстовыми файлами
* [Program.cs](https://github.com/Leonid-Vizel/TelegramLingvoBot/blob/master/TelegramLingvoBot/Program.cs) - Запуск бота, модели и работа с сообщениями
* [Question.sc](https://github.com/Leonid-Vizel/TelegramLingvoBot/blob/master/TelegramLingvoBot/Question.cs) - Переводы и их параметры
* [Theme.cs](https://github.com/Leonid-Vizel/TelegramLingvoBot/blob/master/TelegramLingvoBot/Theme.cs) - Темы переводов
* [User.cs](https://github.com/Leonid-Vizel/TelegramLingvoBot/blob/master/TelegramLingvoBot/User.cs) - Файл с классами проверяющих и пользователей
* [TelegramLingvoBot/model](TelegramLingvoBot/model/) - модель для проверки грамматики

## Демо
<iframe width="560" height="315" src="https://www.youtube.com/embed/mboeaEYdNSA" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
