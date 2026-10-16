var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Пункт 2. Конечная точка /aspnetcore — приветственная страница
// WelcomePageMiddleware — терминальный компонент: формирует ответ сам,
// не вызывает next(), поэтому MapGet ниже недостижим.
app.Map("/aspnetcore", branch => branch.UseWelcomePage());

// Пункт 3 и 5. Статические файлы и стартовая страница
// UseDefaultFiles ДОЛЖЕН быть ПЕРЕД UseStaticFiles, иначе подстановка
// имени стартовой страницы (index.html) не произойдёт.
app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();
