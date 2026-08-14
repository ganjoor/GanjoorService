# GanjoorService
Ganjoor museum and ganjoor.net own backend (ASP.NET Core Web API) and frontend (Razor Pages) code

این کد وب سرویس [گنجینهٔ گنجور](https://museum.ganjoor.net) و [گنجور](https://ganjoor.net) و همچنین کد سایت گنجور است.

[فهرست توابع در دسترس](https://api.ganjoor.net)

![https://api.ganjoor.net](https://user-images.githubusercontent.com/582212/91652208-14a63c00-eaaa-11ea-89c2-5acabdfda7de.png)

## Running it locally

New to this codebase and want to run your own copy? See **[RUNNING_LOCALLY.md](RUNNING_LOCALLY.md)**
for a full step-by-step guide — cloning, database setup, and Visual Studio configuration.

The one thing that guide covers in more depth but is worth knowing up front: **the production
database is never published**, since it contains private/user-linked data. What *is* published is
a git repository of the poetry content itself (poets, categories, poems — allowlisted, no user
data): **[github.com/ganjoor/ganjoor-data](https://github.com/ganjoor/ganjoor-data)**. A fresh
local install can pull real content from there via **Admin → مالی و سایت → درون‌ریزی دادهٔ عمومی**
(also reachable automatically the first time you run the site against an empty database) instead
of starting from nothing.

