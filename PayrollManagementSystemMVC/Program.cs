var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// No app.UseHttpsRedirection() here: when running behind Railway (or any reverse
// proxy that already terminates HTTPS for us), forcing an internal redirect can
// create an infinite redirect loop. Railway's edge already serves https:// to the
// browser, so this app only needs to speak plain http internally.
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();