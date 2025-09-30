using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text.Json;

// Personīgais finanšu plānotājs — konsoles lietotne
// Domāta palaišanai .NET Fiddle vai jebkurā .NET 6/7+ konsoles vidē.

enum Category { Food, Transport, Fun, School, Other }

record Income
{
    public DateTime Date { get; init; }
    public string Source { get; init; } = string.Empty;
    public decimal Amount { get; init; }

    public Income(DateTime date, string source, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(source)) throw new ValidationException("Ienākuma avots nevar būt tukšs.");
        if (amount <= 0) throw new ValidationException("Ienākuma summa jābūt > 0.");
        Date = date;
        Source = source;
        Amount = amount;
    }
}

record Expense
{
    public DateTime Date { get; init; }
    public Category Category { get; init; }
    public decimal Amount { get; init; }
    public string Note { get; init; } = string.Empty;

    public Expense(DateTime date, Category category, decimal amount, string note)
    {
        if (string.IsNullOrWhiteSpace(note)) throw new ValidationException("Izdevuma apraksts nevar būt tukšs.");
        if (amount <= 0) throw new ValidationException("Izdevuma summa jābūt > 0.");
        Date = date;
        Category = category;
        Amount = amount;
        Note = note;
    }
}

record Subscription
{
    public string Name { get; init; } = string.Empty;
    public decimal MonthlyPrice { get; init; }
    public DateTime StartDate { get; init; }
    public bool IsActive { get; set; }

    public Subscription(string name, decimal monthlyPrice, DateTime startDate, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Abonementa nosaukums nevar būt tukšs.");
        if (monthlyPrice <= 0) throw new ValidationException("Abonementa mēneša cena jābūt > 0.");
        Name = name;
        MonthlyPrice = monthlyPrice;
        StartDate = startDate;
        IsActive = isActive;
    }
}

static class Tools
{
    public static bool SafeParseDecimal(string? s, out decimal value)
    {
        if (!string.IsNullOrWhiteSpace(s) && decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }
        value = 0;
        return false;
    }

    public static decimal SafeDivide(decimal a, decimal b)
    {
        if (b == 0) return 0;
        return a / b;
    }

    public static string Percent(decimal part, decimal total)
    {
        if (total == 0) return "0%";
        var p = Math.Round((double)(SafeDivide(part, total) * 100), 2);
        return p.ToString(CultureInfo.InvariantCulture) + "%";
    }

    public static string ReadNonEmptyString(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var s = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
            Console.WriteLine("Ievade nevar būt tukša — mēģiniet vēlreiz.");
        }
    }

    public static DateTime ReadDate(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var s = Console.ReadLine();
            if (DateTime.TryParse(s, out var dt)) return dt.Date;
            Console.WriteLine("Nederīgs datums — izmantojiet formātu YYYY-MM-DD vai vietējo formātu.");
        }
    }

    public static decimal ReadDecimal(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var s = Console.ReadLine();
            if (SafeParseDecimal(s, out var d) && d > 0) return d;
            Console.WriteLine("Nederīga summa — ievadiet skaitli lielāku par 0 (piem., 12.34).") ;
        }
    }
}

class Storage
{
    public List<Income> Incomes { get; } = new();
    public List<Expense> Expenses { get; } = new();
    public List<Subscription> Subscriptions { get; } = new();
}

class Program
{
    static Storage storage = new();

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8; // ļaut € simbolu
        bool running = true;
        while (running)
        {
            try
            {
                ShowMainMenu();
                Console.Write("Izvēle: ");
                var choice = Console.ReadLine();
                switch (choice)
                {
                    case "1": IncomesMenu(); break;
                    case "2": ExpensesMenu(); break;
                    case "3": SubscriptionsMenu(); break;
                    case "4": ShowAllLists(); break;
                    case "5": FiltersMenu(); break;
                    case "6": MonthReport(); break;
                    case "7": ImportExportMenu(); break;
                    case "0": running = false; break;
                    case "h": Help(); break;
                    default: Console.WriteLine("Nederīga izvēle. Izmantojiet izvēlni (0-7).\n"); break;
                }
            }
            catch (ValidationException ve)
            {
                Console.WriteLine($"Validācijas kļūda: {ve.Message}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Radās kļūda: {ex.Message}\n");
            }
        }
        Console.WriteLine("Uz redzēšanos!");
    }

    static void ShowMainMenu()
    {
        Console.WriteLine("================ Personīgais finanšu plānotājs ================\n");
        Console.WriteLine("1) Ienākumi\n2) Izdevumi\n3) Abonementi\n4) Saraksti\n5) Filtri\n6) Mēneša pārskats\n7) Import/Export JSON\n0) Iziet\n");
        Console.WriteLine("'h' — Palīdzība\n");
    }

    #region Incomes
    static void IncomesMenu()
    {
        while (true)
        {
            Console.WriteLine("-- IENĀKUMI --\n1) Pievienot  2) Parādīt  3) Dzēst  0) Atpakaļ");
            Console.Write("Izvēle: ");
            var ch = Console.ReadLine();
            if (ch == "1") AddIncome();
            else if (ch == "2") ShowIncomes();
            else if (ch == "3") DeleteIncome();
            else if (ch == "0") break;
            else Console.WriteLine("Nederīga izvēle.");
        }
    }

    static void AddIncome()
    {
        var date = Tools.ReadDate("Datums (YYYY-MM-DD): ");
        var source = Tools.ReadNonEmptyString("Avots: ");
        var amount = Tools.ReadDecimal("Summа (€): ");
        var inc = new Income(date, source, amount);
        storage.Incomes.Add(inc);
        Console.WriteLine("Ienākums pievienots.\n");
    }

    static void ShowIncomes()
    {
        var sorted = storage.Incomes.OrderByDescending(i => i.Date).ToList();
        if (!sorted.Any()) { Console.WriteLine("Nav ienākumu.\n"); return; }
        Console.WriteLine("Nr | Datums       | Avots                 | Summa");
        Console.WriteLine("---+--------------+-----------------------+---------");
        for (int i = 0; i < sorted.Count; i++)
        {
            var r = sorted[i];
            Console.WriteLine($"{i+1,2} | {r.Date:yyyy-MM-dd} | {r.Source,-21} | €{r.Amount:0.00}");
        }
        Console.WriteLine();
    }

    static void DeleteIncome()
    {
        ShowIncomes();
        Console.Write("Ieraksta nr. dzēšanai (0 — atcelt): ");
        var s = Console.ReadLine();
        if (int.TryParse(s, out var idx) && idx > 0 && idx <= storage.Incomes.Count)
        {
            var sorted = storage.Incomes.OrderByDescending(i => i.Date).ToList();
            var item = sorted[idx - 1];
            storage.Incomes.Remove(item);
            Console.WriteLine("Dzēsts.\n");
        }
        else Console.WriteLine("Atcelts vai nederīgs numurs.\n");
    }
    #endregion

    #region Expenses
    static void ExpensesMenu()
    {
        while (true)
        {
            Console.WriteLine("-- IZDEVUMI --\n1) Pievienot  2) Parādīt  3) Dzēst  4) Filtrēt  0) Atpakaļ");
            Console.Write("Izvēle: ");
            var ch = Console.ReadLine();
            if (ch == "1") AddExpense();
            else if (ch == "2") ShowExpenses();
            else if (ch == "3") DeleteExpense();
            else if (ch == "4") ExpensesFilter();
            else if (ch == "0") break;
            else Console.WriteLine("Nederīga izvēle.");
        }
    }

    static void AddExpense()
    {
        var date = Tools.ReadDate("Datums (YYYY-MM-DD): ");
        Console.WriteLine("Kategorija: 0-Food 1-Transport 2-Fun 3-School 4-Other");
        Console.Write("Izvēlieties kategoriju (0-4): ");
        var catInput = Console.ReadLine();
        if (!int.TryParse(catInput, out var ci) || ci < 0 || ci > 4) { Console.WriteLine("Nederīga kategorija.\n"); return; }
        var cat = (Category)ci;
        var amount = Tools.ReadDecimal("Summа (€): ");
        var note = Tools.ReadNonEmptyString("Piezīme: ");
        var exp = new Expense(date, cat, amount, note);
        storage.Expenses.Add(exp);
        Console.WriteLine("Izdevums pievienots.\n");
    }

    static void ShowExpenses(List<Expense>? list = null)
    {
        var display = (list ?? storage.Expenses).OrderByDescending(e => e.Date).ToList();
        if (!display.Any()) { Console.WriteLine("Nav izdevumu.\n"); return; }
        Console.WriteLine("Nr | Datums       | Kategorija | Summa    | Piezīme");
        Console.WriteLine("---+--------------+------------+----------+----------------");
        for (int i = 0; i < display.Count; i++)
        {
            var r = display[i];
            Console.WriteLine($"{i+1,2} | {r.Date:yyyy-MM-dd} | {r.Category,-10} | €{r.Amount,7:0.00} | {r.Note}");
        }
        Console.WriteLine();
    }

    static void DeleteExpense()
    {
        ShowExpenses();
        Console.Write("Ieraksta nr. dzēšanai (0 — atcelt): ");
        var s = Console.ReadLine();
        if (int.TryParse(s, out var idx) && idx > 0 && idx <= storage.Expenses.Count)
        {
            var sorted = storage.Expenses.OrderByDescending(i => i.Date).ToList();
            var item = sorted[idx - 1];
            storage.Expenses.Remove(item);
            Console.WriteLine("Dzēsts.\n");
        }
        else Console.WriteLine("Atcelts vai nederīgs numurs.\n");
    }

    static void ExpensesFilter()
    {
        Console.WriteLine("Filtrs: 1) Pēc datuma diapazona  2) Pēc kategorijas  0) Atpakaļ");
        Console.Write("Izvēle: ");
        var ch = Console.ReadLine();
        if (ch == "1")
        {
            var from = Tools.ReadDate("Sākuma datums: ");
            var to = Tools.ReadDate("Beigu datums: ");
            var res = storage.Expenses.Where(e => e.Date >= from && e.Date <= to).ToList();
            ShowExpenses(res);
            Console.WriteLine($"Kopā: €{res.Sum(r => r.Amount):0.00}\n");
        }
        else if (ch == "2")
        {
            Console.WriteLine("Kategorija: 0-Food 1-Transport 2-Fun 3-School 4-Other");
            Console.Write("Izvēlieties kategoriju (0-4): ");
            var cin = Console.ReadLine();
            if (int.TryParse(cin, out var ci) && ci >= 0 && ci <= 4)
            {
                var cat = (Category)ci;
                var res = storage.Expenses.Where(e => e.Category == cat).ToList();
                ShowExpenses(res);
                Console.WriteLine($"Kopā: €{res.Sum(r => r.Amount):0.00}\n");
            }
            else Console.WriteLine("Nederīga kategorija.\n");
        }
    }
    #endregion

    #region Subscriptions
    static void SubscriptionsMenu()
    {
        while (true)
        {
            Console.WriteLine("-- ABONEMENTI --\n1) Pievienot  2) Parādīt  3) Aktivizēt/Deaktivizēt  4) Dzēst  0) Atpakaļ");
            Console.Write("Izvēle: ");
            var ch = Console.ReadLine();
            if (ch == "1") AddSubscription();
            else if (ch == "2") ShowSubscriptions();
            else if (ch == "3") ToggleSubscription();
            else if (ch == "4") DeleteSubscription();
            else if (ch == "0") break;
            else Console.WriteLine("Nederīga izvēle.");
        }
    }

    static void AddSubscription()
    {
        var name = Tools.ReadNonEmptyString("Nosaukums: ");
        var price = Tools.ReadDecimal("Mēneša cena (€): ");
        var start = Tools.ReadDate("Sākuma datums: ");
        var sub = new Subscription(name, price, start, true);
        storage.Subscriptions.Add(sub);
        Console.WriteLine("Abonements pievienots.\n");
    }

    static void ShowSubscriptions()
    {
        var sorted = storage.Subscriptions.OrderByDescending(s => s.StartDate).ToList();
        if (!sorted.Any()) { Console.WriteLine("Nav abonementu.\n"); return; }
        Console.WriteLine("Nr | Nosaukums               | Cena/mēn | Sākums      | Aktīvs");
        Console.WriteLine("---+-------------------------+----------+-------------+-------");
        for (int i = 0; i < sorted.Count; i++)
        {
            var r = sorted[i];
            Console.WriteLine($"{i+1,2} | {r.Name,-23} | €{r.MonthlyPrice,7:0.00} | {r.StartDate:yyyy-MM-dd} | {(r.IsActive?"Jā":"Nē")}");
        }
        Console.WriteLine();
    }

    static void ToggleSubscription()
    {
        ShowSubscriptions();
        Console.Write("Ieraksta nr. aktivizēt/deaktivizēt (0 — atcelt): ");
        var s = Console.ReadLine();
        if (int.TryParse(s, out var idx) && idx > 0 && idx <= storage.Subscriptions.Count)
        {
            var sorted = storage.Subscriptions.OrderByDescending(i => i.StartDate).ToList();
            var item = sorted[idx - 1];
            item.IsActive = !item.IsActive;
            Console.WriteLine($"Abonements '{item.Name}' tagad {(item.IsActive?"aktīvs":"neaktīvs")}.\n");
        }
        else Console.WriteLine("Atcelts vai nederīgs numurs.\n");
    }

    static void DeleteSubscription()
    {
        ShowSubscriptions();
        Console.Write("Ieraksta nr. dzēšanai (0 — atcelt): ");
        var s = Console.ReadLine();
        if (int.TryParse(s, out var idx) && idx > 0 && idx <= storage.Subscriptions.Count)
        {
            var sorted = storage.Subscriptions.OrderByDescending(i => i.StartDate).ToList();
            var item = sorted[idx - 1];
            storage.Subscriptions.Remove(item);
            Console.WriteLine("Dzēsts.\n");
        }
        else Console.WriteLine("Atcelts vai nederīgs numurs.\n");
    }
    #endregion

    #region Lists & Filters
    static void ShowAllLists()
    {
        Console.WriteLine("-- VISI IERAKSTI (sakārtoti pēc datuma) --\n");
        var combined = new List<(DateTime Date, string Type, string Summary)>();
        combined.AddRange(storage.Incomes.Select(i => (i.Date, "Ienākums", $"{i.Source} €{i.Amount:0.00}")));
        combined.AddRange(storage.Expenses.Select(e => (e.Date, "Izdevums", $"{e.Category} €{e.Amount:0.00} ({e.Note})")));
        combined.AddRange(storage.Subscriptions.Select(s => (s.StartDate, "Abonements", $"{s.Name} €{s.MonthlyPrice:0.00} {(s.IsActive?"aktīvs":"neaktīvs")}")));
        var sorted = combined.OrderByDescending(x => x.Date).ToList();
        if (!sorted.Any()) { Console.WriteLine("Nav ierakstu.\n"); return; }
        Console.WriteLine("Datums       | Tips       | Pārskats");
        Console.WriteLine("-------------+-----------+------------------------------------------");
        foreach (var r in sorted)
        {
            Console.WriteLine($"{r.Date:yyyy-MM-dd} | {r.Type,-10} | {r.Summary}");
        }
        Console.WriteLine();
    }

    static void FiltersMenu()
    {
        while (true)
        {
            Console.WriteLine("-- FILTRI --\n1) Pēc datuma diapazona  2) Pēc kategorijas (izdevumi)  0) Atpakaļ");
            Console.Write("Izvēle: ");
            var ch = Console.ReadLine();
            if (ch == "1")
            {
                var from = Tools.ReadDate("Sākuma datums: ");
                var to = Tools.ReadDate("Beigu datums: ");
                var inc = storage.Incomes.Where(i => i.Date >= from && i.Date <= to).ToList();
                var exp = storage.Expenses.Where(e => e.Date >= from && e.Date <= to).ToList();
                Console.WriteLine("== Ienākumi =="); ShowIncomesList(inc);
                Console.WriteLine("== Izdevumi =="); ShowExpenses(exp);
                Console.WriteLine($"Ienākumi kopā: €{inc.Sum(i => i.Amount):0.00}");
                Console.WriteLine($"Izdevumi kopā: €{exp.Sum(e => e.Amount):0.00}\n");
            }
            else if (ch == "2")
            {
                Console.WriteLine("Kategorija: 0-Food 1-Transport 2-Fun 3-School 4-Other");
                Console.Write("Izvēlieties kategoriju (0-4): ");
                var cin = Console.ReadLine();
                if (int.TryParse(cin, out var ci) && ci >= 0 && ci <= 4)
                {
                    var cat = (Category)ci;
                    var res = storage.Expenses.Where(e => e.Category == cat).ToList();
                    ShowExpenses(res);
                    Console.WriteLine($"Kopā: €{res.Sum(r => r.Amount):0.00}\n");
                }
                else Console.WriteLine("Nederīga kategorija.\n");
            }
            else if (ch == "0") break;
            else Console.WriteLine("Nederīga izvēle.");
        }
    }

    static void ShowIncomesList(List<Income> list)
    {
        if (!list.Any()) { Console.WriteLine("Nav ienākumu."); return; }
        foreach (var i in list.OrderByDescending(x => x.Date))
        {
            Console.WriteLine($"{i.Date:yyyy-MM-dd} | {i.Source,-20} | €{i.Amount:0.00}");
        }
    }
    #endregion

    #region Month Report
    static void MonthReport()
    {
        Console.WriteLine("-- MĒNEŠA PĀRSKATS (ievadiet YYYY-MM, piem., 2025-09) --");
        var input = Tools.ReadNonEmptyString("Gads-mēnesis: ");
        if (!DateTime.TryParseExact(input + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var firstDay))
        {
            Console.WriteLine("Nederīgs formāts.\n");
            return;
        }
        var year = firstDay.Year; var month = firstDay.Month;
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var incs = storage.Incomes.Where(i => i.Date >= start && i.Date <= end).ToList();
        var exps = storage.Expenses.Where(e => e.Date >= start && e.Date <= end).ToList();
        var activeSubs = storage.Subscriptions.Where(s => s.IsActive && s.StartDate <= end).ToList();

        var incomeSum = incs.Sum(i => i.Amount);
        var expenseSum = exps.Sum(e => e.Amount);
        var subsSum = activeSubs.Sum(s => s.MonthlyPrice);
        var net = incomeSum - expenseSum - subsSum;

        Console.WriteLine($"Pārskata periods: {start:yyyy-MM-dd} līdz {end:yyyy-MM-dd}");
        Console.WriteLine($"Ienākumi kopā: €{incomeSum:0.00}");
        Console.WriteLine($"Izdevumi kopā: €{expenseSum:0.00}");
        Console.WriteLine($"Aktīvo abonementu summa (mēn): €{subsSum:0.00}");
        Console.WriteLine($"Neto: €{net:0.00}\n");

        // kategoriju procenti
        Console.WriteLine("Kategoriju sadalījums (izdevumi):");
        var byCat = exps.GroupBy(e => e.Category).Select(g => (Category: g.Key, Sum: g.Sum(x => x.Amount))).ToList();
        foreach (var c in byCat)
        {
            Console.WriteLine($"{c.Category,-10}: €{c.Sum:0.00} ({Tools.Percent(c.Sum, expenseSum)})");
        }
        if (!byCat.Any()) Console.WriteLine("Nav izdevumu šajā mēnesī.");

        // lielākais izdevums
        if (exps.Any())
        {
            var largest = exps.OrderByDescending(e => e.Amount).First();
            Console.WriteLine($"\nLielākais izdevums: €{largest.Amount:0.00} — {largest.Category} ({largest.Note}) datums {largest.Date:yyyy-MM-dd}");
        }
        // vidējais dienas tēriņš
        var days = (end - start).Days + 1;
        var avgDaily = Tools.SafeDivide(expenseSum, days);
        Console.WriteLine($"Vidējais dienas tēriņš: €{avgDaily:0.00} (dienu skaits: {days})\n");
    }
    #endregion

    #region Import/Export
    static void ImportExportMenu()
    {
        while (true)
        {
            Console.WriteLine("-- IMPORT / EXPORT JSON --\n1) Eksportēt  2) Importēt  0) Atpakaļ");
            Console.Write("Izvēle: ");
            var ch = Console.ReadLine();
            if (ch == "1") ExportJson();
            else if (ch == "2") ImportJson();
            else if (ch == "0") break;
            else Console.WriteLine("Nederīga izvēle.");
        }
    }

    class ExportModel
    {
        public List<Income> Incomes { get; set; } = new();
        public List<Expense> Expenses { get; set; } = new();
        public List<Subscription> Subscriptions { get; set; } = new();
    }

    static void ExportJson()
    {
        var model = new ExportModel { Incomes = storage.Incomes, Expenses = storage.Expenses, Subscriptions = storage.Subscriptions };
        var opts = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(model, opts);
        Console.WriteLine("=== JSON eksportēts zemāk ===");
        Console.WriteLine(json);
        Console.WriteLine("=== Beigas ===\n");
    }

    static void ImportJson()
    {
        Console.WriteLine("Ielīmējiet JSON tekstu (vienā rindā vai vairākās) un pēc tam rakstiet 'END' atsevišķā rindā.");
        var lines = new List<string>();
        while (true)
        {
            var l = Console.ReadLine();
            if (l == null) break;
            if (l.Trim().ToUpper() == "END") break;
            lines.Add(l);
        }
        var text = string.Join('\n', lines);
        if (string.IsNullOrWhiteSpace(text)) { Console.WriteLine("Nav ievadīta JSON.\n"); return; }

        try
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var model = JsonSerializer.Deserialize<ExportModel>(text, opts);
            if (model == null) throw new ValidationException("Nevarēja nolasīt JSON vai neatbilst modelim.");

            // validācija pirms aizvietošanas
            foreach (var i in model.Incomes) _ = new Income(i.Date, i.Source, i.Amount);
            foreach (var e in model.Expenses) _ = new Expense(e.Date, e.Category, e.Amount, e.Note);
            foreach (var s in model.Subscriptions) _ = new Subscription(s.Name, s.MonthlyPrice, s.StartDate, s.IsActive);

            // ja viss kārtībā — aizvietojam
            storage = new Storage();
            storage.Incomes.AddRange(model.Incomes);
            storage.Expenses.AddRange(model.Expenses);
            storage.Subscriptions.AddRange(model.Subscriptions);
            Console.WriteLine("Importēts veiksmīgi.\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Importa kļūda: {ex.Message}. Dati netika mainīti.\n");
        }
    }
    #endregion

    static void Help()
    {
        Console.WriteLine("Palīdzība — īss ceļvedis:");
        Console.WriteLine("- Ievades datumi: YYYY-MM-DD keš, bet darbotos arī vietējā formātā.");
        Console.WriteLine("- Visi summas lauki prasa skaitļus > 0 (piem., 12.50).\n- Importēšanas laikā ielīmējiet JSON un beigās ierakstiet 'END' rindā.");
        Console.WriteLine("- Eksportēšana parādīs JSON, ko var saglabāt ārpus programmas.\n");
    }
}
