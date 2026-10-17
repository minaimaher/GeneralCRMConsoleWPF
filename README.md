# GeneralCRMConsoleWPF

**CRM Function Runner** is a Windows (WPF) app for running the `MyConsole` production-fix functions safely, without editing and recompiling `Program.cs` for every run. It follows the controls in IT-CRM-SOP-001 (CRM-DAT-01, CRM-INC-05 and the Part J fix index).

```
src/
  GeneralConsoleForCRM.sln
  MyConsole/            the existing console app (still works as before)
  CrmFunctionRunner/    the WPF app; compiles the same Applications.cs and Helpers as linked files
tests/
  RecorderChecks/       checks for the dry-run / evidence wrapper (runs on .NET 8, any OS)
```

## How a run works

Each run goes through five steps:

1. **Inputs.** Choose a function from the catalog, grouped as in the SOP. You can search by name, console method or SOP number. Each function shows:
   - its description, SOP reference and status (*Ready*, *Use with care*, *Read only*, *Needs fix*)
   - a form for its inputs: record IDs, lists of IDs, an Excel file with a preview of its first rows, yes/no, FetchXML
   - a **Jira ticket number**, which is required for every run
2. **Dry run.** The function runs against CRM, but nothing is saved:
   - Reads work normally.
   - Every create, update, delete, status change, assignment, workflow and transaction is recorded instead of sent.
   - You see every planned change, field by field, with **before → after** values, plus the console output.
3. **Confirm.** Shows a summary, the name of the approver, and *“I reviewed the dry run”*. In **production** you must also type the number of planned changes.
4. **Run.** The live run, with the same change grid and output. **Stop** ends it at the next CRM call.
5. **Result.** Shows the outcome and counts, with these actions:
   - **Copy closure comment**, in the CRM-INC-05 service request format, ready to paste into Jira
   - **Open evidence folder**

Further safeguards:
- **Needs fix:** functions marked *needs fix* in the SOP's fix index can be viewed but not run. A setting can unlock them for test environments.
- **Outside systems:** functions that call an outside system (the Waseet gateway, RabbitMQ) have no dry run, because it would still send real requests. They go straight to confirmation, with an extra acknowledgement.
- **Production:** the app shows a red banner while connected to production.
- **Language:** the interface is in English and Arabic (right to left). Use the language button.

## Evidence and audit

Every dry run and live run creates a folder under `Documents\CRM Function Runner\Runs` (or the `EvidenceFolder` setting). The folder holds:

| File | Contents |
|---|---|
| `inputs.json` | Function, environment, ticket, approver, CRM user, Windows user, machine, inputs |
| `input_<file>.xlsx` | A copy of the Excel file that was actually used |
| `output.log` | Everything the function printed |
| `changes.csv` | One row per changed field: operation, entity, record ID, field, before, after, status, error (opens in Excel; Arabic is kept) |
| `summary.json` | Outcome, counts, duration, result |
| *trace files* | Any log files the function writes (they land in this folder) |

`Runs\audit.log` gets one line per run.

## Build and run

1. Open `src/GeneralConsoleForCRM.sln` in Visual Studio 2022. You need the .NET desktop development workload, which includes .NET Framework 4.7.2.
2. Set **CrmFunctionRunner** as the startup project and build. NuGet restores the CRM SDK packages, at the same versions as MyConsole.
3. Edit `src/CrmFunctionRunner/App.config`. After a build it becomes `CrmFunctionRunner.exe.config` next to the exe.
   - **Environments:** one `Environment:<name>` entry per CRM, with the `Organization.svc` address. Add `|production` to production.
   - **No passwords in this file.** Users sign in with their Windows account or type their password when they connect. The password is kept in memory for that session only.
   - The engine settings (`Take`, `Skip`, licence workflow IDs) have the same meaning as in MyConsole.
4. Run it, choose the environment, sign in, pick a function.

**First use:** try it against Development or Pre-production. For each function you plan to use, check the dry run and the evidence before using it on production.

## What changed in MyConsole

All changes are backward compatible. When the console runs with nothing supplied, it behaves as before. The diffs are in commit *“Let a host pass inputs and a connection to the console functions”*.

- **`Helpers/RunInputs.cs`** lets the runner pass an Excel path, Skip/Take and its own connection.
- **`GetIdsFromExcel` now returns every column.** Before, it returned only column A, so functions that read `ids[1]`/`ids[2]` (`RenewLicense`, `UpdateLoanRequests`, `UpdateContactWithDOB`, `UpdateContactWithIndvNo`) could not work.
- **Optional parameters** replace hardcoded IDs. The default is the old value. Affected functions: `SettleLoans`, `CloseJanaLoans`, `DeleteBPFRecords`, `StopDuplicationCase`, `UpdateLoanPartner`, `TestSMSWithRMQueue`.
- **`SettleLoans` reconnection.** Every 10 loans it reconnected with `CRMHelper.ConnectToCRM`, which has placeholder credentials. It now reconnects with the App.config credentials.
- **`DeleteBPFRecords`** says so when a case has no process record, instead of failing.

## Known limits

- **Dry run with creates.** A dry run cannot fully simulate a function that creates a record and then reads it back, because the record doesn't exist yet. `SettleLoans` is one example. The dry run may stop early, and the evidence shows where.
- **Fixed values in the code.** Some functions still have values set inside the code: the role in `AssociateRoleToUser/Team`, the reasons in `AddTicketReasons`, the scripts under *Configuration scripts*. Their screens say so. Turning those values into inputs is the next step, function by function.
- **Not run on Windows yet.** The app was built and type-checked on Linux. The dry-run wrapper is covered by `tests/RecorderChecks` (`dotnet run`). The screens themselves have not been run on Windows yet, and the app has not been connected to a real CRM.

## Security note

`GeneralConsoleForCRM.zip` in this repository contains the original `App.config`, with a CRM user name, password and server addresses, and its own `.git` history. This repository is **public**. To protect that account:

1. Change that password.
2. Make the repository private, or remove the zip from its history.

The extracted source in `src/` has placeholders instead of the credentials and leaves out `Ids.xlsx`.
