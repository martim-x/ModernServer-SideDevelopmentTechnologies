# Лекция 05. VALIDATION (валидация данных)

## Назначение
Валидация входных данных в ASP.NET Core minimal API: атрибуты **DataAnnotations** +
`IValidatableObject` с подключением `WithParameterValidation()` (пакет **MinimalApis.Extensions**),
альтернатива — **FluentValidation**, и стандарт **RFC 7807 / RFC 9457 (Problem Details for HTTP APIs)**.

> ⚠ Эта лекция почти целиком состоит из **скриншотов** (488 символов текста на 10 страниц).
> Весь код ниже снят со слайдов.

## 1. DataAnnotations, WithParameterValidation (NuGet)
### Список атрибутов со слайда (дословно)
```csharp
// [CreditCard]
// [EmailAddress]
// [StringLength(max)], [MinLength(min)]
// [Phone]
// [Range(min, max)]
// [RegularExpression(regex)]
// [Url]
// [Required]
// [Compare]
```
**Пакет NuGet:** `MinimalApis.Extensions`, версия **0.11.0** (последняя стабильная на скриншоте),
лицензия MIT, зависимости: `net6.0` -> `MiniValidation (>= 0.8.0)`;
`net7.0` -> `Microsoft.OpenApi (>= 1.4.3)` + `MiniValidation (>= 0.8.0)`.
Скриншот обозревателя решений **ANC12** показывает подключённый пакет `MinimalApis.Extensions (0.11.0)`.

## Модель с DataAnnotations + IValidatableObject — код со слайда
```csharp
public record ITPasssionary:IValidatableObject
{
    [Required]
    [StringLength(50)]
    [Display(Name = "Имя")]
    public string?  FirstName { get; set; }
    [Required]
    [StringLength(50)]
    [Display(Name = "Фамилия")]
    public string?  LastName { get; set; }
    //[Required]
    [EmailAddress]
    [Display(Name = "Почта")]
    public string?   Email { get; set; }
    //[Required]
    [Phone]
    [Display(Name = "Номер телефона")]
    public string?   Phone { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Email) &&
            string.IsNullOrEmpty(Phone))
            { yield return new ValidationResult("Должен быть указан номер телефона или почтовый адрес"); }
    }
}
```
> Имя типа на слайде размыто: записано как `ITPasssionary` / `ITPessionary` — точное написание
> [неразборчиво на скриншоте]; далее в URL Postman используется `/ITPasssionary`.

### Подключение к конечной точке — код со слайда
```csharp
app.MapPost("/ITPasssionary", (ITPasssionary passsionary) => { return passsionary; })
    .WithParameterValidation();    // Nuget: MinimalApis.Extensions
```
Тест Postman: POST `http://localhost:5214/ITPasssionary`, тело
`{"FirstName":"Donald","LastName":"Knuth","Email":"knuth@belstu.by","Phone":"+37517334455"}`
-> **200 OK**, тело возвращает объект с полями в нижнем регистре
(`firstName`, `lastName`, `email`, `phone`).

### Ответ при нарушении [Required]: 400 Bad Request
Запрос без FirstName -> **400**, тело в формате ProblemDetails:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "FirstName": [
      "The WWn field is required."
    ]
  }
}
```
(текст сообщения на скриншоте читается как «The WWn field is required.» — вероятно артефакт шрифта
вместо «The Name field is required.»)

Ниже на слайде приведён фрагмент **RFC 7231 (IETF)**, раздел **6.5.1. 400 Bad Request**:
сервер не может обработать запрос из-за ошибки клиента (неверный синтаксис, невалидный запрос,
некорректная маршрутизация). Obsoletes: 2616; Updates: 2817.

## Несколько ошибок валидации сразу — слайд (стр. 3-4)
Тело: FirstName + Email + Phone (без LastName) -> 400, ошибки по трём полям:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "LastName": [ "The Фамилия field is required." ],
    "Email":    [ "The Почта field is not a valid e-mail address." ],
    "Phone":    [ "The Номер телефона field is not a valid phone number." ]
  }
}
```
**Важно:** в сообщениях используется `[Display(Name=...)]` — имя поля подставляется из `DisplayName`.

Второй пример: тело только `{"FirstName":"Donald","LastName":"Knuth"}` (без Email и Phone)
-> 400, ошибка от собственной логики `IValidatableObject` под **пустым** ключом `""`:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "": [ "Должен быть указан номер телефона или почтовый адрес" ] }
}
```

## 2. Параметры ErrorMessage, new { типов… } — код со слайда
```csharp
public record ITPasssionary:IValidatableObject
{
    [Required(ErrorMessage = "Имя обязательно должно быть задано")]
    [StringLength(50)]
    [Display(Name = "Имя")]
    public string?  FirstName { get; set; }
    [Required]
    [StringLength(50)]
    [Display(Name = "Фамилия")]
    public string?  LastName { get; set; }
    //[Required]
    [EmailAddress]
    [Display(Name = "Почта")]
    public string?   Email { get; set; }
    //[Required]
    [Phone(ErrorMessage = "Ошибка в телефонном номере")]
    [Display(Name = "Номер телефона")]
    public string?   Phone { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Email) &&
            string.IsNullOrEmpty(Phone))
        {
            yield return new ValidationResult("Должен быть указан номер телефона или почтовый адрес",
                new[] { nameof(Email), nameof(Phone) });
        }
    }
}
```
Второй аргумент `new[] { nameof(Email), nameof(Phone) }` привязывает ошибку к конкретным полям
(в ответе появляются ключи `Email` и `Phone` с русским сообщением).

Ответ на слайде: 400, `errors` -> `"FirstName": ["Имя обязательно должно быть задано"]`,
`"Phone": ["Ошибка в телефонном номере"]`.

## 3. FluentValidation — полный код со слайда
Ссылка на проекте: https://github.com/FluentValidation/...

```csharp
builder.Services.AddScoped<IValidator<ITPasssionary>, ITPasssionaryValidator>();

public record ITPasssionary
{
    public string? FirstName { get; set; }
    public string? LastName  { get; set; }
    public string? Email     { get; set; }
    public string? Phone     { get; set; }
}

public class ITPasssionaryValidator:AbstractValidator<ITPasssionary>
{
    const string msg = "Значение {PropertyName} = {PropertyValue} не корректно";
    public ITPasssionaryValidator()
    {
        RuleFor(x => x.FirstName).NotNull().NotEmpty().MinimumLength(2).MaximumLength(50)
            .WithName("Имя")
            .WithMessage(msg)
            .WithErrorCode("ERROR:001");
        RuleFor(x => x.LastName).NotNull().NotEmpty().MinimumLength(2).MaximumLength(50)
            .WithName("Фамилия")
            .WithMessage(msg)
            .WithErrorCode("ERROR:002");
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithName("Почта")
            .WithMessage(msg)
            .WithErrorCode("ERROR:003");
        RuleFor(x => x.Phone)
            .Matches(@"^\+?\d{8,12}")          // на скриншоте читается как Matches(@"^+?\d{8,12}") — регулярка телефона
            .WithName("Телефон")
            .WithMessage(msg)
            .WithErrorCode("ERROR:004");
        When(x => (x.Email == null), () =>
        {
            RuleFor(x => x.Phone).NotNull()
                .WithMessage(msg)
                .WithErrorCode("ERROR:004-01");
        });
        When(x => (x.Phone == null), () =>
        {
            RuleFor(x => x.Email).NotNull()
                .WithMessage(msg)
                .WithErrorCode("ERROR:003-01");
        });
    }
}
```

### Ручная проверка в конечной точке — код со слайда
```csharp
app.MapPost("/ITPasssionary", (IValidator<ITPasssionary> validator,
                               ITPasssionary passsionary) =>
{
    ValidationResult result = validator.Validate(passsionary);
    if (!result.IsValid) return Results.ValidationProblem(result.ToDictionary());   // RFC: 7807, 9457
    return Results.Ok(passsionary);
});
```

### Методы цепочки FluentValidation (со слайда)
`NotNull()`, `NotEmpty()`, `MinimumLength(n)`, `MaximumLength(n)`, `EmailAddress()`,
`Matches(regex)`, `WithName()`, `WithMessage()`, `WithErrorCode()`, условия `When(cond, () => {...})`.

### Результат (слайд, стр. 8)
- Корректные данные -> **200 OK**.
- `Phone = "+3751733334455"` (13 цифр, длиннее допустимых 12) -> **400 Bad Request**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "Phone": [ "Значение Телефон = +3751733334455 не корректно" ] }
}
```
Регулярка телефона на слайде допускает **8–12 цифр** после необязательного «+», поэтому 13 цифр
отклоняются.

## 4-5. RFC 7807, 9457 — Problem Details for HTTP APIs
Формат **problem details** — машиночитаемое описание ошибки в HTTP API (используется в ответах
400/404/500 во всех лабораторных работах: 4, 5, 6).

## Ключевые API и типы (сводно)

| Имя | Назначение |
|---|---|
| `MinimalApis.Extensions` (NuGet, 0.11.0) | `WithParameterValidation()` для minimal API |
| `WithParameterValidation()` | включает валидацию параметров по атрибутам |
| `IValidatableObject` | интерфейс пользовательской логики валидации |
| `Validate(ValidationContext)` | метод логики валидации, возвращает `IEnumerable<ValidationResult>` |
| `ValidationResult(message)` | ошибка без привязки к полю (ключ `""`) |
| `ValidationResult(message, new[]{nameof(...)})` | ошибка с привязкой к полям |
| `ValidationContext` | контекст валидации |
| `Required`, `StringLength`, `MinLength`, `Range`, `EmailAddress`, `Phone`, `Url`, `CreditCard`, `RegularExpression`, `Compare` | атрибуты DataAnnotations |
| `Display(Name="Имя")` | подстановка имени поля в сообщения об ошибках |
| `ErrorMessage = "..."` | пользовательский текст ошибки для атрибута |
| `AbstractValidator<T>`, `IValidator<T>`, `RuleFor`, `When` | FluentValidation |
| `AddScoped<IValidator<T>, TValidator>()` | регистрация валидатора (Scoped) |
| `ValidationResult result = validator.Validate(x)` | ручная проверка |
| `Results.ValidationProblem(dict)` | ответ 400 с ошибками (RFC 7807/9457) |
| `Results.Ok(x)` | ответ 200 |
| `HttpLoggingFields`, `AddHttpLogging` | журналирование (Лекция 01) |
| RFC 7231 (6.5.1), RFC 7807, RFC 9457 | стандарты описания ошибок HTTP |

## Сравнение двух подходов (по материалу лекции)
| Аспект | DataAnnotations + MinimalApis.Extensions | FluentValidation |
|---|---|---|
| Где логика | Атрибуты на модели + `IValidatableObject` | Класс-валидатор `AbstractValidator<T>` |
| Регистрация | пакет NuGet + `.WithParameterValidation()` | `AddScoped<IValidator<T>, TValidator>()` |
| Вызов | автоматически при вызове endpoint'а | ручной `validator.Validate(x)` |
| Цепочки правил | ограниченный набор атрибутов | богатый fluent API, условия `When` |
| Коды ошибок | нет | `WithErrorCode("ERROR:001")` |
| Ответ | одинаковый ProblemDetails 400 | одинаковый ProblemDetails 400 |

## Источник
- `lectures/Лекция_05_ASPNETCORE_VALIDATION.pdf` — стр. 1 (слайд 1: список атрибутов DataAnnotations,
  карточка пакета MinimalApis.Extensions 0.11.0, обозреватель решений ANC12),
  стр. 2 (код модели `ITPasssionary` с атрибутами и `IValidatableObject`, регистрация
  `WithParameterValidation()`, тест Postman 200 OK),
  стр. 3 (код модели + текст RFC 7231 раздела 6.5.1 «400 Bad Request»),
  стр. 4 (несколько ошибок валидации сразу: ключи LastName/Email/Phone),
  стр. 5 (слайд 2: параметры `ErrorMessage`, код модели с привязкой ошибок к полям),
  стр. 6 (слайд 3: FluentValidation, ссылка на github),
  стр. 7 (код `ITPasssionaryValidator` и регистрация `AddScoped<IValidator<...>>`),
  стр. 8 (код endpoint'а с `validator.Validate` и `Results.ValidationProblem`; результат 400
  для 13-значного телефона),
  стр. 9 (слайды 4-5: RFC 7807, 9457 Problem Details for HTTP APIs),
  стр. 10 (конец).