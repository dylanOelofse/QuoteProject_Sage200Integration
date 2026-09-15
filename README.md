# Sage 200 Evolution Integration — Quotes Manager

An ASP.NET Core MVC app (.NET 10) and a Windows service that keep sales quotes in sync **both
ways** with Sage 200 Evolution through the Evolution SDK (`Pastel.Evolution`).

A quote lives in Sage as a **sales order** — `InvNum` with `DocType = 4` and `DocState = 1`. The
web app owns capture, review, PDF statements and Excel export. The service owns every SDK call and
runs on a ten-second timer.

---

## Project description

**C#, .NET 10, ASP.NET Core MVC, Windows Service (Topshelf), Sage Evolution SDK, SQL Server,
ADO.NET, DevExpress Reporting, ClosedXML**

- Built a web app and a Windows service that keep sales quotes in sync both ways with Sage 200
  Evolution. The app handles capture, review, PDF statements and Excel export; the service does all
  the ERP work on a timer.
- The web app never calls Sage. Edits and deletes write a flag against the quote instead, and the
  service picks the work up on its next pass, so nothing is lost when Sage is down and no user
  waits on an ERP call.
- The polling service runs through Topshelf, five sync passes every ten seconds. Each tick takes a
  lock and skips itself if the previous one is still working, so a slow SDK call cannot overlap and
  post the same quote twice. Sage company, credentials, serial and auth key come from a settings
  file read once at startup, so pointing it at another company needs no rebuild.
- A staging table maps the app's quote ID to Sage's document ID and records the flag and result of
  every sync attempt. New quotes are pushed through the SDK as sales orders with the returned Sage
  ID and order number written back onto the record, and existing Sage quotes are pulled in with a
  join back to the app's own quotes so polling cannot create duplicates.
- Edited quotes are reopened in Sage by ID and their lines rebuilt from the app's current lines, so
  added, edited and deleted lines all travel one path.
- A reconciliation pass treats Sage as the source of truth for whether a quote is still open.
  Anything deleted in Sage, or converted into a sales order, is flagged so it drops out of the
  app's list. Runs in pure SQL with no SDK call, so it keeps working even when the SDK cannot
  connect.
- Cookie authentication with role claims, admin-only write endpoints, salted PBKDF2 password
  hashing and antiforgery tokens on posts.
- DevExpress PDF statements (one file, or a zip when several quotes are selected) and Excel export
  through ClosedXML.

---

## 1. Layering

```
Browser JS (fetch)  →  QuoteController      →  QuoteService      →  DatabaseEngine  →  App DB
                       QuoteLineController     QuoteLineService     (ADO.NET)          (SQL Server)
                       [Authorize],            validates,                                 ↕
                       model binding           writes Flag                          QuoteStaging2
                                                                                          ↕
                                                    Service (Topshelf, 10s timer)  ←──────┘
                                                            ↓
                                                    SyncSageQuotes  →  Pastel.Evolution SDK
                                                            ↓                  ↓
                                                    DatabaseEngine     Sage company DB
                                                    (cross-DB SQL)
```

Two processes, one database, no HTTP between them. The web app writes rows and flags; the service
reads flags and talks to Sage. Neither knows the other is running.

| | Role |
|---|---|
| `QuotesProject/Controllers/` | Routes, model binding, `[Authorize]`, status-code mapping |
| `QuotesProject/Services/` | Validation, `DataTable` → model mapping |
| `QuotesProject/Data/DatabaseEngine.cs` | Every SQL statement the web app runs |
| `QuotesProject/Reports/QuoteReport.cs` | DevExpress `XtraReport` bound to `QuoteReportModel` |
| `QuotesProject/Views/` | Quotes list, quote-lines page, login, create-user |
| `IntegrationPollingService/Service.cs` | Timer, lock, five passes per tick |
| `IntegrationPollingService/SyncSageQuotes.cs` | The only class that touches the SDK |
| `IntegrationPollingService/DatabaseEngine.cs` | Cross-database SQL: app DB ↔ Sage company DB |
| `IntegrationService/` | WinForms harness — same passes behind buttons, for stepping through one at a time |

## 2. Settings

Both settings files are **gitignored** — they hold live credentials and are not in the repo.

`QuotesProject/appsettings.json` for the web app:

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=QuoteProjectOriginal;User Id=sa;Password=...;TrustServerCertificate=True;"
  }
}
```

`IntegrationPollingService/settings1.json` for the service (copied to the output folder on build):

```jsonc
{
  "CompanyDB":    "companyDatabase",  // Sage company database
  "CommonDB":     "SageCommon",
  "Server":       "DYLANOELOFSE",
  "UserName":     "sa",
  "Password":     "",
  "SerialNumber": "",                          // from the Evolution licence
  "AuthKey":      ""                           // SDK auth key
}
```

A static constructor on `SyncSageQuotes` reads that file once at startup, and every pass that needs
the SDK opens with `DatabaseContext.Initialise(companyDB, commonDB, server, userName, password,
serialNumber, authKey)`. Pointing the service at another Sage company is a text edit and a restart,
not a rebuild.

Run `QuotesProject/Scripts/CreateUsersTable.sql` once to create `Users` and seed `admin` / `user`
(both password `1234`, stored as PBKDF2 hashes). The script is safe to re-run.

## 3. The flag protocol

The whole integration hangs on one `nvarchar` column, `Quote.Flag`, plus a second on `QuoteLine`.
The web app writes a flag and returns immediately; the service reads flags and does the ERP work.

| Flag | Set by | Means |
|---|---|---|
| `NULL` | web app on create | New, nothing pushed yet |
| `p` | service (`QuoteStaging2.Flag`) | Queued for push to Sage |
| `y` | service (`QuoteStaging2.Flag`) | Pushed, `SageQuoteId` written back |
| `up` | web app on any edit | Update pending — header, or any line added, edited or deleted |
| `us` | service after a successful update | Update synced |
| `dp` | web app on delete (quote **and** its lines) | Delete pending |
| `ds` | service after a delete in Sage | Deleted |
| `cv` | reconciliation pass | Gone from Sage: deleted there, or converted to an order/invoice |

The list page selects `WHERE Flag IS NULL OR Flag = 'us' OR Flag = 'up'`, a whitelist rather than a
blacklist, so a quote only appears while it is live. `dp`, `ds` and `cv` all drop out of the list
without anything being deleted from the database.

Every line-level write in `DatabaseEngine` sets the parent quote to `up` in the same statement, so
one flag covers the whole document and the service never has to diff lines.

## 4. The five passes

`Service.TimerElapsed` runs these in order, every ten seconds:

| Pass | SDK? | What it does |
|---|---|---|
| `SageToCrm()` | read | Pulls open Sage quotes the app has never seen, with their lines |
| `crmToStaging()` | no | Gives every new app quote a staging row, flag `p` |
| `stagingToSage()` | write | Builds a `SalesOrder` from flag `p` rows and saves it |
| `updateQuotes()` | write | Reopens flag `up` quotes by Sage ID and rebuilds their lines |
| `reconcileSageQuotes()` | no | Flags `cv` anything that has left Sage |

The timer holds a `Monitor.TryEnter` on a private lock and returns straight away if the previous
tick is still running. Ten seconds is shorter than a slow SDK round trip, and without the lock two
ticks would both see the same flag `p` row and post the quote twice.

**Push** — `stagingToSage` maps the app's row onto the SDK object, then writes Sage's answer back:

```csharp
SalesOrder sageQuote = new SalesOrder();
sageQuote.Customer        = new Customer(header["Customer"].ToString());
sageQuote.DeliverTo       = new Address(header["Address"].ToString(), "", "");
sageQuote.ExternalOrderNo = header["ExternalOrderNumber"].ToString();
// … dates …

foreach (var line in quoteLines.AsEnumerable())
    sageQuote.Detail.Add(new OrderDetail
    {
        InventoryItem    = new InventoryItem(line.Field<string>("Item")),
        Quantity         = Convert.ToDouble(line.Field<decimal>("Quantity")),
        UnitSellingPrice = Convert.ToDouble(line.Field<decimal>("Price")),
        DiscountPercent  = Convert.ToDouble(line.Field<decimal>("Discount"))
    });

sageQuote.Save();

// Sage owns the number. Write ID and OrderNo back, flag the staging row 'y'.
DatabaseEngine.updateCreatedQuotes(sageQuote.ID, sageQuote.OrderNo, crmQuoteId, "y", "Success");
```

**Pull** — `SageToCrm` queries `InvNum` for `DocType = 4 AND DocState = 1` and left-joins back to
the app's `Quote` on `OrderNum = QuoteNumber`, keeping only rows where the join misses. That join is
the entire duplicate guard: drop it and every tick re-imports the same quotes.

**Update** — there is no line-level update. `updateQuotes` opens the order by ID, clears the whole
detail collection and rebuilds it from the app's current lines:

```csharp
SalesOrder sageQuote = new SalesOrder(sageQuoteId);
sageQuote.Detail.Clear();   // added, edited and deleted lines all travel one path
```

**Reconcile** — pure SQL, no SDK. It looks for app quotes that carry a Sage number but no longer
have a matching open `InvNum` row, and flags them `cv`. Because it never touches the SDK it keeps
working when Evolution is down, and it is what makes Sage the source of truth for whether a quote
is still open.

## 5. Things learned the hard way

- **Sales orders cannot be deleted through the SDK**, only inside Evolution itself. `deleteQuote()`
  is written and commented out in the timer for exactly that reason. Deleting is a soft delete:
  flag `dp`, drop out of the list, and let the reconciliation pass mark it `cv` once someone
  removes it in Sage.
- **Cross-database joins need `COLLATE DATABASE_DEFAULT`.** The Sage company database and the app
  database do not share a collation, so `i.OrderNum = q.QuoteNumber` fails with a collation
  conflict until both sides are collated explicitly.
- **Reconciliation has to skip quotes that have not been pushed yet.** A quote created in the app
  has no `QuoteNumber` and no Sage row, which looks exactly like one deleted in Sage. The query
  excludes blank quote numbers and staging flag `p`; without those two clauses a brand-new quote
  gets flagged `cv` and disappears seconds after someone captures it.
- **Sage owns the quote number.** The app inserts with `QuoteNumber` empty and the service fills it
  in on the next tick, so anything keyed off the number has to survive it being blank for a few
  seconds.
- **The staging table has to be written before the SDK call, not after.** `crmToStaging` inserts
  the flag `p` row in its own pass, so a crash inside `stagingToSage` leaves the quote queued
  rather than lost.
- **An empty quote must not reach Sage.** `GetNewQuotes` inner-joins `QuoteLine`, so a quote only
  gets a staging row once it has at least one live line. Capture the header, walk away, and nothing
  is posted.
- **Deleted lines are filtered on the way out, not on the way in.** `GetQuoteLines` excludes flag
  `dp` in both the web app and the service, so the rebuild in `updateQuotes` sees the same lines
  the user sees.
- **DevExpress needs fonts inside the container.** Rendering a PDF on Linux fails until the image
  installs `fontconfig` and `fonts-liberation`, and the runtime licence comes from the
  `DevExpress_License` environment variable.
- **Quote numbers make bad file names.** Statement PDFs are named from the Sage number, run through
  `Path.GetInvalidFileNameChars()` first.
- **Topshelf gives you both modes from one exe.** Run it from a terminal to watch a tick happen,
  or `QuoteProjectIntegrationPollingService.exe install` and `start` to register it as a Windows
  service running as Local System.

## 6. Statements and export

`QuoteController.BuildStatement` fills the report's `ObjectDataSource` with a `QuoteReportModel`,
calls `CreateDocument()` and exports to a `MemoryStream`. One selected quote returns a PDF; several
return a flat zip built in memory, PDFs at the root with no folders. The post carries an
antiforgery token in the `RequestVerificationToken` header.

Excel goes through ClosedXML: `export/{quoteId}` writes a header block and a line table for one
quote, `exportAll` writes one row per quote.

## 7. Running it

```bash
# web app
dotnet run --project QuotesProject

# service, as a console app — every tick prints to the terminal
dotnet run --project IntegrationPollingService

# service, registered with Windows
IntegrationPollingService.exe install
IntegrationPollingService.exe start
```

The web app also ships as a container. `docker-compose.yml` builds it and overrides the connection
string through `ConnectionStrings__DefaultConnection`, and `deploy/` holds a self-contained install
kit (image tar, compose file, `.env`) for a machine with nothing but Docker on it. See
[deploy/README.md](deploy/README.md).

The service stays on Windows: the Evolution SDK is a Windows-only assembly and needs a licensed
Sage installation to talk to.
