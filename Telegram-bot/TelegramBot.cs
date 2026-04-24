using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Console;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Threading.Tasks;

namespace Telegram_bot
{
	public class TelegramBot
	{
        //Токен бота
		private const string token = "8753839732:AAGPrIKho4MSNeJJthwdYzflAa3_uR0l1IY";

        //Путь к файлу Json
        private static string JsonPath = "tasks.json";

        //Библиотека задач c ключом ID пользователя
        private static Dictionary<long, List<string>> tasks = new();

        /// <summary>
        /// Инициализирут работу бота до нажатия любой клавиши
        /// </summary>
        public async Task Run()
		{
            var bot = new TelegramBotClient(token);

            using var cts = new CancellationTokenSource();
            bot.StartReceiving(Update, Error, cancellationToken: cts.Token);

            WriteLine("Запуск бота...");
            ReadLine();
        }

        /// <summary>
        /// Загружает из json файла библиотеку типа <em>UserID|tasks</em>
        /// </summary>
        static void LoadTask()
        {
            try
            {
                if (File.Exists(JsonPath))
                {
                    var json = File.ReadAllText(JsonPath);
                    tasks = JsonSerializer.Deserialize<Dictionary<long, List<string>>>(json);
                }
            }
            catch
            {
                tasks = new Dictionary<long, List<string>>();
            }

        }

        /// <summary>
        /// Процедура, сохраняющая задачу в файл json в виде библиотеки <em>userID|tasks</em>
        /// </summary>
        static void SaveTask()
        {
            var json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(JsonPath, json);
        }

        /// <summary>
        /// Проверяет и обрабатывает входящее сообщение пользователя
        /// </summary>
        static async Task Update(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            if (update.Type != UpdateType.Message || update.Message!.Text == null)
            {
                return;
            }
            var msg = update.Message;
            var userID = msg.Chat.Id;
            var text = msg.Text;
            var text_low = text.ToLower();
            Log($"{userID}: {text_low}");
            var kb = new ReplyKeyboardMarkup(new[]
            {
            new KeyboardButton[] {"Добавить задачу", "Мои задачи"},
            new KeyboardButton[] {"Удалить задачу", "Очистить задачи"}
        })
            {
                ResizeKeyboard = true
            };

            if (text_low == "/start")
            {
                await botClient.SendMessage(userID,
                    "Привет! Добро пожаловать в DailyPlanner❤️\nЯ помогу тебе спланировать день 📅",
                    replyMarkup: kb);
                return;
            }

            if ((text_low.Contains("добавить") || text_low.Contains("создать")) && text_low.Contains("задачу"))
            {
                await botClient.SendMessage(userID, "Напиши название задачи:");
                return;
            }

            if (text_low.Contains("мои") || text_low.Contains("список задач"))
            {
                if (!tasks.ContainsKey(userID) || tasks[userID].Count == 0)
                {
                    await botClient.SendMessage(userID, "Ваш список задач пуст🫗");
                    return;
                }

                string list = "Твои задачи:\n";
                for (int i = 0; i < tasks[userID].Count; i++)
                {
                    list += $"{i + 1}. {tasks[userID][i]}\n";
                }

                await botClient.SendMessage(userID, list);
                return;
            }

            if ((text_low.Contains("удалить") || text_low.Contains("убрать")) && text_low.Contains("задачу"))
            {
                await botClient.SendMessage(userID, "Введи номер задачи для удаления:");
                return;
            }

            if (text_low.Contains("очистить"))
            {
                tasks[userID] = new List<string>();
                SaveTask();
                await botClient.SendMessage(userID, "Список задач очищен🗑");
                return;
            }

            if (Regex.IsMatch(text_low, @"^\d+$"))
            {
                int i = int.Parse(text) - 1;

                if (tasks.ContainsKey(userID) && i >= 0 && i < tasks[userID].Count)
                {
                    string removed = tasks[userID][i];
                    tasks[userID].RemoveAt(i);
                    SaveTask();

                    await botClient.SendMessage(userID, $"Удалена задача: {removed}🗑");
                }
                else
                {
                    await botClient.SendMessage(userID, "Неверный номер задачи❌");
                }
                return;
            }

            if (!text_low.StartsWith("/"))
            {
                if (!tasks.ContainsKey(userID))
                {
                    tasks[userID] = new List<string>();
                }
                tasks[userID].Add(text);
                SaveTask();

                await botClient.SendMessage(userID, $"Задача добавлена✅\n{text}");
                return;
            }

            await botClient.SendMessage(userID, "Не совсем вас понял...🤔");
        }

        /// <summary>
        /// Функция, обрабатывающая ошибки и записывающие их в logs.txt
        /// </summary>
        /// <param name="e">Ошибка</param>
        static Task Error(ITelegramBotClient botClient, Exception e, CancellationToken ct)
        {
            Log("ERROR: " + e.Message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Записывает любое действие в logs.txt
        /// </summary>
        /// <param name="log">Текст действия</param>
        static void Log(string log)
        {
            File.AppendAllText("logs.txt", $"{DateTime.Now}: {log}\n");
        }
    }
}

