
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PlannerApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class PlannerController : ControllerBase
    {
        private readonly string _connString;
        private readonly string _sqlDumpPath;

        public PlannerController(IConfiguration configuration)
        {
            // Попытка подхватить возможные имена connection string
            _connString = configuration.GetConnectionString("PlannerDb")
                          ?? configuration.GetConnectionString("DefaultConnection")
                          ?? configuration["ConnectionStrings:PlannerDb"]
                          ?? configuration["ConnectionStrings:DefaultConnection"];

            // Дамп (опционально)
            _sqlDumpPath = configuration["SqlDumpPath"];
            if (string.IsNullOrWhiteSpace(_sqlDumpPath))
            {
                var possible = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "planner_250609.sql"),
                    Path.Combine(AppContext.BaseDirectory, "Data", "planner_250609.sql"),
                    Path.Combine(Directory.GetCurrentDirectory(), "planner_250609.sql"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Data", "planner_250609.sql")
                };
                foreach (var p in possible)
                {
                    if (System.IO.File.Exists(p))
                    {
                        _sqlDumpPath = p;
                        break;
                    }
                }
            }
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var meta = await GatherMetadataAsync();

            // Формируем HTML (без блока "Пользователи")
            var sb = new StringBuilder();
            sb.Append("<!doctype html><html lang='ru'><head><meta charset='utf-8'/><meta name='viewport' content='width=device-width,initial-scale=1'/>");
            sb.Append("<title>Planner System</title>");
            sb.Append("<style>");
            sb.Append(@":root{--bg1:#0f172a;--card:#0b1220;--accent:#6d28d9;--accent2:#4f46e5}*{box-sizing:border-box}body{margin:0;font-family:Inter,system-ui,-apple-system,Segoe UI,Roboto;background:linear-gradient(180deg,#071035 0%, #0b1220 100%);color:#e6eef8} .header{display:flex;align-items:center;justify-content:space-between;padding:28px 36px}.brand{display:flex;align-items:center;gap:14px}.logo{width:56px;height:56px;border-radius:12px;background:linear-gradient(135deg,var(--accent),var(--accent2));display:inline-flex;align-items:center;justify-content:center;font-weight:800;font-size:22px;box-shadow:0 12px 30px rgba(79,70,229,0.18)}.title{font-size:20px;font-weight:700;color:#fff}.subtitle{color:#c7d2fe;font-size:13px}.controls{display:flex;gap:12px;align-items:center}.btn{background:transparent;border:1px solid rgba(255,255,255,0.06);padding:10px 14px;border-radius:10px;color:#e6eef8;cursor:pointer;backdrop-filter:blur(6px);transition:transform .12s}.btn:hover{transform:translateY(-3px);box-shadow:0 8px 26px rgba(79,70,229,0.12)}.grid{display:grid;grid-template-columns:repeat(12,1fr);gap:20px;padding:20px 36px}.card{background:linear-gradient(180deg,rgba(255,255,255,0.02),rgba(255,255,255,0.01));grid-column:span 4;border-radius:14px;padding:18px;border:1px solid rgba(255,255,255,0.03);box-shadow:0 8px 30px rgba(2,6,23,0.6);transition:transform .18s}.card:hover{transform:translateY(-6px)}.card h3{margin:0;font-size:14px;color:#dbeafe}.stat{font-size:28px;font-weight:800;margin-top:10px}.small{color:#9aa8c9;font-size:13px;margin-top:8px}.list{grid-column:span 8;background:linear-gradient(180deg,rgba(255,255,255,0.02),rgba(255,255,255,0.01));padding:18px;border-radius:14px;border:1px solid rgba(255,255,255,0.03)}.item{display:flex;align-items:center;justify-content:space-between;padding:12px 8px;border-bottom:1px dashed rgba(255,255,255,0.03)}.item:last-child{border-bottom:none}.item .name{font-weight:600;color:#fff}.item .meta{color:#9aa8c9;font-size:13px}.footer{padding:22px 36px;color:#9aa8c9;font-size:13px}.tag{display:inline-block;padding:6px 10px;background:linear-gradient(90deg,#6d28d9,#4f46e5);border-radius:999px;color:white;font-weight:700;font-size:13px}@media(max-width:980px){.grid{grid-template-columns:repeat(6,1fr)}.card{grid-column:span 6}.list{grid-column:span 6}}@media(max-width:640px){.grid{grid-template-columns:repeat(1,1fr)}.card{grid-column:span 1}.list{grid-column:span 1}}.fadein{animation:fadeIn .6s ease both}@keyframes fadeIn{from{opacity:0;transform:translateY(10px)}to{opacity:1;transform:none}}.logout{background:linear-gradient(90deg,#ef4444,#dc2626);border:none}");
            sb.Append("</style></head><body>");

            // header
            sb.Append("<div class='header'><div class='brand'><div class='logo'>P</div><div><div class='title'>Planner System</div>");
            sb.Append("<div class='subtitle'>Добро пожаловать, " + HtmlEncode(User?.Identity?.Name ?? "пользователь") + "</div></div></div>");
            sb.Append("<div class='controls'><form method='post' action='/Account/Logout' style='display:inline'><button class='btn logout'>Выйти</button></form></div></div>");

            // grid
            sb.Append("<div class='grid'>");
            sb.Append("<div class='card fadein'><h3>Статус Базы данных</h3>");
            sb.Append("<div class='stat'>" + (meta.DbOk ? "Подключено" : "Оффлайн") + "</div>");
            sb.Append("<div class='small'>" + HtmlEncode(meta.DbInfoMessage) + "</div></div>");

            // Таблицы
            sb.Append("<div class='card fadein'><h3>Таблицы (public)</h3>");
            sb.Append("<div class='stat'>" + meta.Tables.Count + "</div>");
            sb.Append("<div class='small'>Найденные таблицы: ");
            if (meta.Tables.Any())
            {
                // Ссылки на таблицы
                var links = meta.Tables.Take(20).Select(t => $"<a href='/Planner/Table/{HtmlEncode(t)}' style='color:#c7d2fe;text-decoration:none'>{HtmlEncode(t)}</a>");
                sb.Append(string.Join(", ", links));
                if (meta.Tables.Count > 20) sb.Append(" и ещё " + (meta.Tables.Count - 20));
            }
            else
            {
                sb.Append("—");
            }
            sb.Append("</div></div>");

            // Ресурсы / Примеры + Ближайшие задачи (показываем title/name чисто человекочитаемо)
            sb.Append("<div class='list fadein'><h3 style='margin-top:0'>Ресурсы / Примеры</h3>");
            foreach (var r in meta.SampleResources)
            {
                sb.Append("<div class='item'><div><div class='name'>" + HtmlEncode(r.name) + "</div><div class='meta'>" + HtmlEncode(r.extra) + "</div></div><div class='tag'>Ресурс</div></div>");
            }

            sb.Append("<div style='height:10px'></div><h3 style='margin-top:12px'>Ближайшие задачи</h3>");
            foreach (var t in meta.SampleTasks)
            {
                var display = string.IsNullOrWhiteSpace(t.title) ? "(без названия)" : t.title;
                sb.Append("<div class='item'><div><div class='name'>" + HtmlEncode(display) + "</div><div class='meta'>" + HtmlEncode(t.due) + "</div></div><div class='tag'>Задача</div></div>");
            }
            sb.Append("</div>"); // list

            sb.Append("</div>"); // grid
            sb.Append("<div class='footer'>Planner System · Пример интерфейса. Данные показаны частично и только для ознакомления.</div>");
            sb.Append("</body></html>");

            return Content(sb.ToString(), "text/html; charset=utf-8");
        }

        // Просмотр таблицы с пагинацией: /Planner/Table/{tableName}?page=1&pageSize=50
        [HttpGet("Table/{tableName}")]
        public async Task<IActionResult> Table(string tableName, int page = 1, int pageSize = 50)
        {
            if (string.IsNullOrWhiteSpace(tableName) || !Regex.IsMatch(tableName, @"^[A-Za-z0-9_]+$"))
            {
                return BadRequest("Неверное имя таблицы.");
            }

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 500) pageSize = 500;

            var meta = await GatherMetadataAsync();

            // Проверяем, что таблица есть в найденном списке
            if (!meta.Tables.Any(t => t.Equals(tableName, StringComparison.OrdinalIgnoreCase)))
            {
                return NotFound($"Таблица '{tableName}' не найдена.");
            }

            long totalRows = 0;
            var rows = new List<IDictionary<string, object>>();
            bool fromDb = false;
            string error = null;

            int offset = (page - 1) * pageSize;

            if (meta.DbOk && !string.IsNullOrWhiteSpace(_connString))
            {
                try
                {
                    using var conn = new NpgsqlConnection(_connString);
                    await conn.OpenAsync();

                    // Общее количество (без LIMIT/OFFSET)
                    totalRows = await conn.QueryFirstOrDefaultAsync<long>($"select count(*) from public.\"{tableName}\";");

                    // Берём порцию
                    var dynRows = await conn.QueryAsync($"select * from public.\"{tableName}\" limit @Limit offset @Offset;", new { Limit = pageSize, Offset = offset });
                    foreach (var r in dynRows)
                    {
                        if (r is IDictionary<string, object> dict)
                            rows.Add(dict);
                        else
                        {
                            // Попытка привести dynamic к словарю
                            try
                            {
                                var dict2 = new Dictionary<string, object>();
                                foreach (var kv in (IDictionary<string, object>)r) dict2[kv.Key] = kv.Value;
                                rows.Add(dict2);
                            }
                            catch
                            {
                                // fallback: просто строковое представление
                                rows.Add(new Dictionary<string, object> { ["value"] = r?.ToString() ?? "" });
                            }
                        }
                    }

                    fromDb = true;
                }
                catch (Exception ex)
                {
                    error = "Ошибка при выборке из БД: " + ex.Message;
                    fromDb = false;
                    totalRows = 0;
                }
            }

            if (!fromDb)
            {
                // Попытаемся получить все строки из дампа и постранично отобразить
                var dumpRows = new List<IDictionary<string, object>>(); // все найденные строки
                try
                {
                    if (!string.IsNullOrWhiteSpace(_sqlDumpPath) && System.IO.File.Exists(_sqlDumpPath))
                    {
                        var sqlText = await System.IO.File.ReadAllTextAsync(_sqlDumpPath, Encoding.UTF8);
                        var insertRegex = new Regex(@"INSERT\s+INTO\s+(?:public\.)?""?(" + Regex.Escape(tableName) + @")""?\s*\(([^)]*)\)\s*VALUES\s*(.+?);", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                        var matches = insertRegex.Matches(sqlText);
                        foreach (Match m in matches)
                        {
                            var colsRaw = m.Groups[2].Value;
                            var valuesRaw = m.Groups[3].Value.Trim();
                            var cols = colsRaw.Split(',').Select(c => c.Trim().Trim('"')).ToArray();
                            var tuples = SplitValueTuples(valuesRaw);
                            foreach (var tuple in tuples)
                            {
                                var fields = SplitSqlValues(tuple);
                                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                                for (int i = 0; i < Math.Min(cols.Length, fields.Count); i++)
                                    dict[cols[i]] = fields[i];
                                dumpRows.Add(dict);
                            }
                        }
                    }

                    totalRows = dumpRows.Count;
                    // Возьмём нужную страницу
                    var pageSlice = dumpRows.Skip(offset).Take(pageSize);
                    rows.AddRange(pageSlice);
                }
                catch (Exception ex)
                {
                    error = (error ?? "") + " Ошибка при парсинге дампа: " + ex.Message;
                }
            }

            // Формируем HTML с навигацией
            var sb = new StringBuilder();
            sb.Append("<!doctype html><html lang='ru'><head><meta charset='utf-8'/><meta name='viewport' content='width=device-width,initial-scale=1'/><title>Таблица " + HtmlEncode(tableName) + "</title>");
            sb.Append("<style>");
            sb.Append("body{font-family:Inter,system-ui,-apple-system,Segoe UI,Roboto;background:#071035;color:#e6eef8;padding:18px} .back{display:inline-block;margin-bottom:12px;color:#c7d2fe;text-decoration:none} .controls{margin:12px 0} .controls a{color:#c7d2fe;margin-right:8px;text-decoration:none} table{width:100%;border-collapse:collapse;background:linear-gradient(180deg,rgba(255,255,255,0.02),rgba(255,255,255,0.01));border:1px solid rgba(255,255,255,0.03);border-radius:8px;overflow:hidden} th,td{padding:10px;border-bottom:1px solid rgba(255,255,255,0.03);text-align:left;font-size:13px} th{background:rgba(255,255,255,0.02);font-weight:700} .note{color:#9aa8c9;margin:8px 0} .error{color:#ffb4b4;margin:8px 0} .pager{margin-top:12px} .pager a{padding:8px 10px;background:rgba(255,255,255,0.03);color:#e6eef8;border-radius:8px;margin-right:6px;text-decoration:none} .pager .current{background:linear-gradient(90deg,#6d28d9,#4f46e5);font-weight:700}");
            sb.Append("</style></head><body>");
            sb.Append("<a class='back' href='/Planner'>&larr; Назад</a>");
            sb.Append("<h2>Таблица: " + HtmlEncode(tableName) + "</h2>");
            sb.Append("<div class='note'>Источник: " + (meta.DbOk ? "База данных" : "SQL-дамп") + (meta.DbOk ? (" (соединение: " + HtmlEncode(MaskConnectionString(_connString)) + ")") : "") + "</div>");
            if (!string.IsNullOrEmpty(error)) sb.Append("<div class='error'>" + HtmlEncode(error) + "</div>");

            sb.Append("<div class='controls'>");
            sb.Append("<form method='get' style='display:inline-block;margin-right:12px'>");
            sb.Append("<label style='margin-right:6px'>На странице:</label>");
            sb.Append($"<select id='pageSize' name='pageSize' onchange='this.form.submit()'>");
            var opts = new[] { 10, 25, 50, 100, 200 };
            foreach (var o in opts) sb.Append($"<option value='{o}'{(o == pageSize ? " selected" : "")}>{o}</option>");
            sb.Append("</select>");
            sb.Append($"<input type='hidden' name='page' value='1'/>"); // при изменении pageSize возвращаемся на 1
            sb.Append("</form>");
            sb.Append("</div>");

            if (totalRows <= 0)
            {
                sb.Append("<div class='note'>Данные не найдены (возможно, нет INSERT'ов в дампе или таблица пустая).</div>");
            }
            else
            {
                // вычислим колонки
                var allCols = rows.SelectMany(r => r.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (!allCols.Any())
                {
                    // Если в текущей странице нет колонок (например, формат), покажем текстовое представление
                    sb.Append("<div class='note'>Нет распознанных столбцов. Отображается строковое представление:</div>");
                    sb.Append("<table><thead><tr><th>value</th></tr></thead><tbody>");
                    foreach (var r in rows)
                    {
                        var val = r.Values.FirstOrDefault()?.ToString() ?? "";
                        sb.Append("<tr><td>" + HtmlEncode(val) + "</td></tr>");
                    }
                    sb.Append("</tbody></table>");
                }
                else
                {
                    sb.Append("<table><thead><tr>");
                    foreach (var c in allCols) sb.Append("<th>" + HtmlEncode(c) + "</th>");
                    sb.Append("</tr></thead><tbody>");
                    foreach (var row in rows)
                    {
                        sb.Append("<tr>");
                        foreach (var c in allCols)
                        {
                            object val = row.ContainsKey(c) ? row[c] : null;
                            sb.Append("<td>" + HtmlEncode(val?.ToString() ?? "") + "</td>");
                        }
                        sb.Append("</tr>");
                    }
                    sb.Append("</tbody></table>");
                }

                // Пагинация
                var totalPages = (int)Math.Ceiling((double)totalRows / pageSize);
                sb.Append("<div class='pager'>");
                string baseUrl = $"/Planner/Table/{Uri.EscapeDataString(tableName)}?pageSize={pageSize}&page=";
                if (page > 1) sb.Append($"<a href='{baseUrl + (page - 1)}'>&larr; Пред.</a>");
                // Покажем максимум 9 номеров вокруг текущей страницы
                int start = Math.Max(1, page - 4);
                int end = Math.Min(totalPages, page + 4);
                for (int p = start; p <= end; p++)
                {
                    if (p == page) sb.Append($"<a class='current' href='{baseUrl + p}'>{p}</a>");
                    else sb.Append($"<a href='{baseUrl + p}'>{p}</a>");
                }
                if (page < totalPages) sb.Append($"<a href='{baseUrl + (page + 1)}'>След. &rarr;</a>");
                sb.Append($" <span style='margin-left:12px;color:#9aa8c9'>Страница {page} из {totalPages} · Всего записей: {totalRows}</span>");
                sb.Append("</div>");
            }

            sb.Append("</body></html>");
            return Content(sb.ToString(), "text/html; charset=utf-8");
        }

        // ----------------- Вспомогательные функции ----------------------

        private async Task<(bool DbOk, List<string> Tables, List<(string name, string extra)> SampleResources, List<(string title, string due)> SampleTasks, string DbInfoMessage)> GatherMetadataAsync()
        {
            bool dbOk = false;
            List<string> tables = new List<string>();
            var sampleResources = new List<(string name, string extra)>();
            var sampleTasks = new List<(string title, string due)>();
            string dbInfoMessage = "Данные из БД недоступны. Показаны демонстрационные данные.";

            if (!string.IsNullOrWhiteSpace(_connString))
            {
                try
                {
                    using var conn = new NpgsqlConnection(_connString);
                    await conn.OpenAsync();
                    dbOk = true;

                    var pgTables = await conn.QueryAsync<string>("select tablename from pg_tables where schemaname = 'public';");
                    tables = pgTables?.ToList() ?? new List<string>();

                    if (tables.Any(t => t.Equals("resource", StringComparison.OrdinalIgnoreCase)))
                    {
                        var items = await conn.QueryAsync("select name from public.resource limit 20;");
                        foreach (var it in items)
                        {
                            string nm = GetFirstDynamicValue(it) ?? "(без имени)";
                            sampleResources.Add((nm, ""));
                        }
                    }

                    // задачи: ищем таблицу с task/todo в названии
                    var taskTable = tables.FirstOrDefault(t => t.IndexOf("task", StringComparison.OrdinalIgnoreCase) >= 0)
                                    ?? tables.FirstOrDefault(t => t.IndexOf("todo", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!string.IsNullOrEmpty(taskTable))
                    {
                        var sample = await conn.QueryAsync($"select * from public.\"{taskTable}\" limit 20;");
                        foreach (var r in sample)
                        {
                            if (r is IDictionary<string, object> dict)
                            {
                                var titleKey = dict.Keys.FirstOrDefault(k => k.IndexOf("title", StringComparison.OrdinalIgnoreCase) >= 0)
                                               ?? dict.Keys.FirstOrDefault(k => k.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0)
                                               ?? dict.Keys.FirstOrDefault();
                                var dueKey = dict.Keys.FirstOrDefault(k => k.IndexOf("due", StringComparison.OrdinalIgnoreCase) >= 0);

                                string titleVal = titleKey != null && dict.ContainsKey(titleKey) && dict[titleKey] != null ? dict[titleKey].ToString() : "";
                                string dueVal = dueKey != null && dict.ContainsKey(dueKey) && dict[dueKey] != null ? dict[dueKey].ToString() : "";
                                sampleTasks.Add((titleVal, dueVal));
                            }
                        }
                    }

                    dbInfoMessage = "Данные получены из базы данных (соединение: " + MaskConnectionString(_connString) + ").";
                }
                catch (Exception ex)
                {
                    dbOk = false;
                    dbInfoMessage = "Не удалось подключиться к БД: " + ex.Message;
                }
            }
            else
            {
                dbInfoMessage = "Connection string не задан. Попытка парсинга локального дампа.";
            }

            // Если БД недоступна — парсим дамп
            if (!dbOk && !string.IsNullOrWhiteSpace(_sqlDumpPath) && System.IO.File.Exists(_sqlDumpPath))
            {
                try
                {
                    var sqlText = await System.IO.File.ReadAllTextAsync(_sqlDumpPath, Encoding.UTF8);
                    var createTableRegex = new Regex(@"CREATE\s+TABLE\s+(?:public\.)?""?([A-Za-z0-9_]+)""?\s*\(", RegexOptions.IgnoreCase);
                    var createMatches = createTableRegex.Matches(sqlText);
                    tables = createMatches.Select(m => m.Groups[1].Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                    var insertRegex = new Regex(@"INSERT\s+INTO\s+(?:public\.)?""?([A-Za-z0-9_]+)""?\s*\(([^)]*)\)\s*VALUES\s*(.+?);", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    var insertMatches = insertRegex.Matches(sqlText);

                    foreach (Match im in insertMatches)
                    {
                        var table = im.Groups[1].Value;
                        var colsRaw = im.Groups[2].Value;
                        var valuesRaw = im.Groups[3].Value.Trim();
                        var cols = colsRaw.Split(',').Select(c => c.Trim().Trim('"')).ToArray();
                        var tuples = SplitValueTuples(valuesRaw);

                        foreach (var tuple in tuples.Take(20))
                        {
                            var fields = SplitSqlValues(tuple);
                            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            for (int i = 0; i < Math.Min(cols.Length, fields.Count); i++)
                                dict[cols[i]] = fields[i];

                            if (table.Equals("resource", StringComparison.OrdinalIgnoreCase))
                            {
                                var nameKey = dict.Keys.FirstOrDefault(k => k.Equals("name", StringComparison.OrdinalIgnoreCase)) ?? dict.Keys.FirstOrDefault();
                                var nameVal = nameKey != null ? dict[nameKey] : "(без имени)";
                                sampleResources.Add((nameVal, ""));
                            }

                            if (table.IndexOf("task", StringComparison.OrdinalIgnoreCase) >= 0
                                || table.IndexOf("todo", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                var titleKey = dict.Keys.FirstOrDefault(k => k.IndexOf("title", StringComparison.OrdinalIgnoreCase) >= 0)
                                               ?? dict.Keys.FirstOrDefault(k => k.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0)
                                               ?? dict.Keys.FirstOrDefault();
                                var dueKey = dict.Keys.FirstOrDefault(k => k.IndexOf("due", StringComparison.OrdinalIgnoreCase) >= 0);

                                var title = titleKey != null && dict.ContainsKey(titleKey) ? dict[titleKey] : "";
                                var due = dueKey != null && dict.ContainsKey(dueKey) ? dict[dueKey] : "";
                                sampleTasks.Add((title, due));
                            }
                        }
                    }

                    if (tables.Any())
                        dbInfoMessage = "Данные собраны из локального SQL-дампа (" + Path.GetFileName(_sqlDumpPath) + ").";
                    else
                        dbInfoMessage += " Дамп найден, но таблиц не обнаружено.";
                }
                catch (Exception ex)
                {
                    dbInfoMessage += " Ошибка при разборе дампа: " + ex.Message;
                }
            }

            // Демо-данные если пусто
            if (!sampleResources.Any())
            {
                sampleResources = new List<(string, string)>
                {
                    ("Настройка сервера", ""),
                    ("Резервное копирование БД", ""),
                    ("Подготовить отчет для менеджера", "")
                };
            }
            if (!sampleTasks.Any())
            {
                sampleTasks = new List<(string, string)>
                {
                    ("Проверить логи","2026-02-01"),
                    ("Обновить зависимости","2026-02-03"),
                    ("Созвон с командой","2026-02-05")
                };
            }

            return (dbOk, tables, sampleResources, sampleTasks, dbInfoMessage);
        }

        private static string GetFirstDynamicValue(object dyn)
        {
            try
            {
                if (dyn is IDictionary<string, object> dict && dict.Values.Any())
                {
                    return dict.Values.FirstOrDefault()?.ToString();
                }
                return dyn?.ToString();
            }
            catch { return null; }
        }

        private static List<string> SplitValueTuples(string valuesRaw)
        {
            var s = valuesRaw.Trim();
            if (s.EndsWith(";")) s = s.Substring(0, s.Length - 1);

            var tuples = new List<string>();
            int len = s.Length;
            int depth = 0;
            var current = new StringBuilder();
            for (int i = 0; i < len; i++)
            {
                char c = s[i];
                if (c == '(')
                {
                    depth++;
                    if (depth == 1) continue;
                }
                else if (c == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        tuples.Add(current.ToString());
                        current.Clear();
                        continue;
                    }
                }
                if (depth >= 1) current.Append(c);
            }

            if (!tuples.Any())
            {
                var m = Regex.Match(s, @"^\s*\((.*)\)\s*$", RegexOptions.Singleline);
                if (m.Success) tuples.Add(m.Groups[1].Value);
                else tuples.Add(s);
            }

            return tuples;
        }

        private static List<string> SplitSqlValues(string tuple)
        {
            var res = new List<string>();
            if (string.IsNullOrEmpty(tuple)) return res;

            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < tuple.Length; i++)
            {
                char c = tuple[i];
                if (c == '\'')
                {
                    if (inQuotes && i + 1 < tuple.Length && tuple[i + 1] == '\'')
                    {
                        sb.Append('\'');
                        i++;
                        continue;
                    }
                    inQuotes = !inQuotes;
                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    res.Add(CleanField(sb.ToString()));
                    sb.Clear();
                    continue;
                }

                sb.Append(c);
            }

            if (sb.Length > 0) res.Add(CleanField(sb.ToString()));
            return res;
        }

        private static string CleanField(string field)
        {
            if (field == null) return "";
            var s = field.Trim();
            if (string.Equals(s, "NULL", StringComparison.OrdinalIgnoreCase)) return "";
            return s;
        }

        private static string HtmlEncode(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return System.Net.WebUtility.HtmlEncode(s);
        }

        private static string MaskConnectionString(string cs)
        {
            if (string.IsNullOrEmpty(cs)) return cs;
            try
            {
                var parts = cs.Split(';').Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
                for (int i = 0; i < parts.Count; i++)
                {
                    if (parts[i].StartsWith("Password", StringComparison.OrdinalIgnoreCase) ||
                        parts[i].StartsWith("Pwd", StringComparison.OrdinalIgnoreCase))
                    {
                        parts[i] = parts[i].Split('=')[0] + "=***";
                    }
                }
                return string.Join(';', parts);
            }
            catch { return "(masked)"; }
        }
    }
}
