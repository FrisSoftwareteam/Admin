using Clear;
using FirstReg;
using FirstReg.API;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<RequestValidationMiddleware>(s
=> new(Common.APIkey, true));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDeveloperExceptionPage();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var _service = new DataService(builder.Configuration.GetConnectionString("DefaultConnection"));

app.MapGet("/", () => "FR Api 1.0.2");

app.UseMiddleware<RequestValidationMiddleware>();

app.AddDefaultEndpoints(_service);
app.AddMobileEndpoints(_service);

app.Run();