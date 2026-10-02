# WishBound

**A web + mobile character-collection platform built with ASP.NET Core, SQL Server and .NET MAUI.**

WishBound is a gacha-style collection game: players summon virtual characters across five rarity tiers, build their collection, level up friendships with their characters and take part in time-limited events. Administrators manage the whole platform from a dashboard, and every admin action is recorded in an audit log.

It was built as my final course project for the CET in Information Systems Technologies & Programming at CINEL (Portugal), taken from requirements and data modelling through to a finished, presented product.

> The in-app text and the detailed setup notes (`LEIA-ME.txt`) are in Portuguese.

---

## Tech stack

| Layer | Technology |
| --- | --- |
| Back end | ASP.NET Core Web API (.NET 8), Entity Framework Core, Swagger |
| Database | Microsoft SQL Server (database-first, SQL scripts + migrations) |
| Web client | ASP.NET Core MVC, HTML, CSS, JavaScript (consumes the API via HttpClient) |
| Mobile app | .NET MAUI (Android) |
| Auth | Username/password (hashed) and Google sign-in (OAuth) |
| Tools | Visual Studio 2022, SSMS, Git/GitHub |

## Architecture

```
WishBound.ClientAPI (MVC website) ─┐
                                   ├──► WishBound.WebAPI (REST API) ──► SQL Server
WishBound.Mobile (.NET MAUI app) ──┘
```

Neither client touches the database directly — both talk only to the REST API, which is protected by a shared API key header and CORS.

## Features

**For players**
- Summon characters (x1 or x10) from the permanent banner or time-limited event banners
- Five rarity tiers with a pity system: guaranteed Epic every 10 pulls, guaranteed Legendary/Mythic by pull 90, plus a 50/50 rate-up guarantee on event banners
- Collection and inventory with duplicates, releasing copies for coins and buying extra inventory space
- Friendship system: 7 levels per character with daily interactions and unlockable titles, badges and profile frames
- Character messages: greetings, reactions and daily messages unlocked by friendship level, and a chosen "companion" character on the home page
- Daily login rewards (28-day calendar), events page with countdowns and event rewards
- Email validation, password recovery and Google sign-in

**For administrators**
- Dashboard with daily stats, active banners and alerts (e.g. probabilities that don't add up to 100%)
- Full CRUD for characters, rarities, banners/events (including rate-up), currencies, notifications and character messages
- User account management: activate/deactivate, promote, give/take currency, characters and rewards
- **Audit log** of every admin action (who, to whom, what, why, when)
- Statistics (7/30/90 days or 1 year) built on SQL views
- Export reports to PDF or XML (generated without external libraries)

**Mobile app (.NET MAUI)**
- Log in with the same account as the website
- Home screen with your companion character and their greeting
- Collection and character details, daily reward and in-app notifications

## Project structure

```
WishBound.WebAPI/      REST API — controllers, EF Core data layer, services, API-key middleware
WishBound.ClientAPI/   MVC website that consumes the API
WishBound.Mobile/      .NET MAUI Android app
Database/              SQL scripts: database creation, migrations and seed data
Documentacao/          User manual and final project report (PDF, Portuguese)
```

## Running locally

**Requirements:** Visual Studio 2022 (ASP.NET and web development workload, .NET 8 SDK), SQL Server Express + SSMS. For the mobile app, the .NET MAUI workload and an Android emulator.

1. **Database** — in SSMS, run `Database/CriacaoBaseDados.sql`, then the migration scripts `Migracao01.sql` to `Migracao08.sql` in order (see `LEIA-ME.txt` for details).
2. **Run** — open `WishBound.sln`, set **multiple startup projects** (WishBound.WebAPI first, then WishBound.ClientAPI) and press F5.
   - API + Swagger: `http://localhost:5240`
   - Website: `http://localhost:5100`
3. **Mobile (optional)** — with the API running, set WishBound.Mobile as the startup project and run it on an Android emulator (it reaches the API at `http://10.0.2.2:5240`).

Secrets (API key, Google OAuth and SMTP credentials) live in `appsettings.Local.json` files that are excluded from Git. Without them the site still runs: Google sign-in is hidden and validation links are shown on screen instead of being emailed.

## Documentation

- [User manual (PDF)](Documentacao/WishBound_Manual_Utilizador.pdf)
- [Final project report (PDF)](Documentacao/WishBound_Relatorio_Final%201.pdf)
- [Detailed setup notes (Portuguese)](LEIA-ME.txt)

## Author

**Mónica Moura** — [GitHub](https://github.com/monicadfm) · [Portfolio](https://monicadfm.github.io/Portfolio/)
