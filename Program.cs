using Microsoft.EntityFrameworkCore;
using LojaCupCake.Data;
using LojaCupCake.Models;
using LojaCupCake.Hubs;
using Microsoft.AspNetCore.Identity;


var builder = WebApplication.CreateBuilder(args);


//----- AddControllerWithViews


builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection String 'DefaultConnection' não encontrada.");

//----- AddDbContext

builder.Services.AddDbContext<ApplicationDbContext>(options =>

    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    )
);

//----- AdddIdentity


builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Regras de senha
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;

        // Regras de usuário
        options.User.RequireUniqueEmail = true;

        // Bloqueio contra tentativas repetidas
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(10);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization();

//----- ConfigureApplicationCookie

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Conta/Login";
    options.AccessDeniedPath = "/Conta/AcessoNegado";
});

//----- Add Authirization

// SignalR
builder.Services.AddSignalR();

// Sessões HTTP
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Acesso ao contexto HTTP
builder.Services.AddHttpContextAccessor();

// Serviços da aplicação
builder.Services.AddScoped<LojaCupCake.Services.CarrinhoService>();

builder.Services.AddScoped<LojaCupCake.Services.AutorizacaoFuncionarioService>();

var app = builder.Build();



using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var roleManager = services.GetRequiredService<
        RoleManager<IdentityRole>>();

    var userManager = services.GetRequiredService<
        UserManager<ApplicationUser>>();

    string[] roles =
    {
        "Cliente",
        "Funcionario",
        "Administrador"
    };

    // Criar funções
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var resultado = await roleManager.CreateAsync(
                new IdentityRole(role));

            if (!resultado.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Erro ao criar a função {role}: " +
                    string.Join(", ",
                        resultado.Errors.Select(e => e.Description))
                );
            }
        }
    }

    // Recuperar credenciais do User Secrets
    var adminEmail = builder.Configuration["AdminSeed:Email"];
    var adminPassword = builder.Configuration["AdminSeed:Password"];

    if (string.IsNullOrWhiteSpace(adminEmail) ||
        string.IsNullOrWhiteSpace(adminPassword))
    {
        throw new InvalidOperationException(
            "Configure AdminSeed:Email e AdminSeed:Password."
        );
    }

    // Verificar se o administrador já existe
    var admin = await userManager.FindByEmailAsync(adminEmail);

    if (admin == null)
    {
        admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var resultado = await userManager.CreateAsync(
            admin,
            adminPassword);

        if (!resultado.Succeeded)
        {
            throw new InvalidOperationException(
                "Erro ao criar administrador: " +
                string.Join(", ",
                    resultado.Errors.Select(e => e.Description))
            );
        }
    }

    // Garantir que possui a função Administrador
    if (!await userManager.IsInRoleAsync(admin, "Administrador"))
    {
        var resultado = await userManager.AddToRoleAsync(
            admin,
            "Administrador");

        if (!resultado.Succeeded)
        {
            throw new InvalidOperationException(
                "Erro ao atribuir função Administrador."
            );
        }
    }
}





if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Habilitar sessões HTTP
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Endpoint do chat em tempo real por pedido.
app.MapHub<ChatPedidoHub>("/hubs/chat-pedido").RequireAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
