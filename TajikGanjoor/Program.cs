using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(
                 HtmlEncoder.Create(allowedRanges: [ UnicodeRanges.BasicLatin,
                    UnicodeRanges.Arabic, UnicodeRanges.Cyrillic ]));

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/index", "{*url}");
});

builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
