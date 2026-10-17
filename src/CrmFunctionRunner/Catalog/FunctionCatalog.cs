using CrmFunctionRunner.Infrastructure;
using Microsoft.Xrm.Sdk;
using MyConsole;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CrmFunctionRunner.Catalog
{
    public enum FunctionStatus { Ready, UseWithCare, ReadOnly, NeedsFix }

    public enum InputKind { Text, MultilineText, Guid, GuidList, TextList, Integer, YesNo, ExcelFile }

    public sealed class InputSpec
    {
        public string Key { get; set; }
        public InputKind Kind { get; set; }
        public string LabelEn { get; set; }
        public string LabelAr { get; set; }
        public string HelpEn { get; set; }
        public string HelpAr { get; set; }
        public bool Required { get; set; }
        public string Default { get; set; }
        public string Label => Loc.Pick(LabelEn, LabelAr);
        public string Help => Loc.Pick(HelpEn, HelpAr);
    }

    public sealed class FunctionInfo
    {
        public string Key { get; set; }
        public string Method { get; set; }
        public string GroupEn { get; set; }
        public string GroupAr { get; set; }
        public string SopRef { get; set; }
        public string TitleEn { get; set; }
        public string TitleAr { get; set; }
        public string DescriptionEn { get; set; }
        public string DescriptionAr { get; set; }
        public FunctionStatus Status { get; set; }
        public string NoteEn { get; set; }
        public string NoteAr { get; set; }
        /// <summary>False when the function calls an outside system (REST gateway, RabbitMQ); a dry run would still send those calls.</summary>
        public bool DryRunSupported { get; set; } = true;
        public string ExcelColumnsEn { get; set; }
        public string ExcelColumnsAr { get; set; }
        public List<InputSpec> Inputs { get; set; } = new List<InputSpec>();
        public Func<IOrganizationService, InputValues, object> Run { get; set; }

        public string Group => Loc.Pick(GroupEn, GroupAr);
        public string Title => Loc.Pick(TitleEn, TitleAr);
        public string Description => Loc.Pick(DescriptionEn, DescriptionAr);
        public string Note => Loc.Pick(NoteEn, NoteAr);
        public string ExcelColumns => Loc.Pick(ExcelColumnsEn, ExcelColumnsAr);
        public string StatusText => Loc.T("Status." + Status);
        public bool ChangesData => Status != FunctionStatus.ReadOnly;
        public string SearchText => string.Join(" ", TitleEn, TitleAr, Method, SopRef, DescriptionEn, DescriptionAr, GroupEn).ToLowerInvariant();
    }

    /// <summary>Typed access to the values typed into the input form.</summary>
    public sealed class InputValues
    {
        readonly IDictionary<string, string> _values;
        public InputValues(IDictionary<string, string> values) { _values = values; }

        public string Text(string key) => _values.TryGetValue(key, out var v) ? (v ?? "").Trim() : "";
        public bool Has(string key) => Text(key).Length > 0;
        public Guid Guid(string key) => System.Guid.Parse(Text(key));
        public Guid? GuidOrNull(string key) => Has(key) ? System.Guid.Parse(Text(key)) : (Guid?)null;
        public int? IntOrNull(string key) => Has(key) ? int.Parse(Text(key), CultureInfo.InvariantCulture) : (int?)null;
        public bool YesNo(string key) => Text(key) == "true";
        public List<string> Lines(string key) => SplitList(Text(key));
        public List<Guid> Guids(string key) => Lines(key).Select(System.Guid.Parse).ToList();

        public static List<string> SplitList(string text) =>
            (text ?? "").Split(new[] { '\r', '\n', ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
    }

    /// <summary>
    /// Every console function the runner offers. Status and SOP references follow the
    /// CRM-FIX quick index in IT-CRM-SOP-001 (Part J).
    /// </summary>
    public static class FunctionCatalog
    {
        // group names
        const string GCustomer = "Customer data", GCustomerAr = "بيانات العملاء";
        const string GLoans = "Loans and payments", GLoansAr = "القروض والمدفوعات";
        const string GLicences = "Licences", GLicencesAr = "الشهادات والتراخيص";
        const string GCases = "Cases and SLA", GCasesAr = "الحالات واتفاقيات مستوى الخدمة";
        const string GMessages = "Messages and integrations", GMessagesAr = "الرسائل والتكاملات";
        const string GAccess = "Users, teams and roles", GAccessAr = "المستخدمون والفرق والأدوار";
        const string GSetup = "Configuration scripts", GSetupAr = "نصوص الإعداد";
        const string GReports = "Reports and exports", GReportsAr = "التقارير والتصدير";

        static InputSpec Excel(string colsHintEn = null) => new InputSpec
        {
            Key = "excel", Kind = InputKind.ExcelFile, Required = true,
            LabelEn = "Excel file", LabelAr = "ملف Excel",
            HelpEn = "First row is the header row; data starts on row 2. " + (colsHintEn ?? ""),
            HelpAr = "الصف الأول هو صف العناوين، وتبدأ البيانات من الصف الثاني."
        };

        public static List<FunctionInfo> All { get; } = new List<FunctionInfo>
        {
            // ---------------- customer data ----------------
            new FunctionInfo
            {
                Key = "fix-contacts", Method = "FixContacts", GroupEn = GCustomer, GroupAr = GCustomerAr, SopRef = "CRM-FIX-02 (step 1)",
                TitleEn = "Report duplicate contacts", TitleAr = "تقرير العملاء المكررين",
                DescriptionEn = "Lists national IDs that belong to more than one contact record, with the number of records for each.",
                DescriptionAr = "يعرض أرقام الهوية الوطنية المرتبطة بأكثر من سجل عميل وعدد السجلات لكل منها.",
                Status = FunctionStatus.NeedsFix,
                NoteEn = "SOP: needs fix. The code reads the alias “countIds” but the query names it “countIdsIds”, so it stops with an error.",
                NoteAr = "دليل الإجراءات: تحتاج إلى إصلاح. الشيفرة تقرأ الاسم المستعار countIds بينما الاستعلام يسميه countIdsIds، فتتوقف بخطأ.",
                Run = (s, v) => { Applications.FixContacts(s); return null; }
            },
            new FunctionInfo
            {
                Key = "contact-dob", Method = "UpdateContactWithDOB", GroupEn = GCustomer, GroupAr = GCustomerAr,
                TitleEn = "Set contact birth dates from Hijri dates", TitleAr = "تحديث تواريخ ميلاد العملاء من التاريخ الهجري",
                DescriptionEn = "Converts each Hijri date to Gregorian and saves it as the contact's birth date.",
                DescriptionAr = "يحوّل كل تاريخ هجري إلى ميلادي ويحفظه تاريخًا لميلاد العميل.",
                Status = FunctionStatus.UseWithCare,
                ExcelColumnsEn = "A: contact ID (GUID) · B: Hijri birth date", ExcelColumnsAr = "A: معرّف العميل (GUID) · B: تاريخ الميلاد الهجري",
                Inputs =
                {
                    Excel(),
                    new InputSpec { Key = "skip", Kind = InputKind.Integer, LabelEn = "Skip first rows", LabelAr = "تخطي أول صفوف", HelpEn = "Optional. Use with “Take” to run a large file in parts.", HelpAr = "اختياري. استخدمه مع «عدد الصفوف» لتشغيل ملف كبير على أجزاء." },
                    new InputSpec { Key = "take", Kind = InputKind.Integer, LabelEn = "Take rows", LabelAr = "عدد الصفوف", HelpEn = "Optional. Leave empty to use the value in the settings file.", HelpAr = "اختياري. اتركه فارغًا لاستخدام القيمة في ملف الإعدادات." }
                },
                Run = (s, v) => { Applications.UpdateContactWithDOB(s); return null; }
            },
            new FunctionInfo
            {
                Key = "contact-indvno", Method = "UpdateContactWithIndvNo", GroupEn = GCustomer, GroupAr = GCustomerAr,
                TitleEn = "Set contact individual numbers", TitleAr = "تحديث الرقم الفردي للعملاء",
                DescriptionEn = "Finds each contact by national ID and saves the individual number (IndvNo) from the file.",
                DescriptionAr = "يبحث عن كل عميل برقم الهوية ويحفظ الرقم الفردي (IndvNo) من الملف.",
                Status = FunctionStatus.UseWithCare,
                ExcelColumnsEn = "A: national ID · B: individual number", ExcelColumnsAr = "A: رقم الهوية الوطنية · B: الرقم الفردي",
                Inputs = { Excel() },
                Run = (s, v) => { Applications.UpdateContactWithIndvNo(s); return null; }
            },

            // ---------------- loans ----------------
            new FunctionInfo
            {
                Key = "settle-loans", Method = "SettleLoans", GroupEn = GLoans, GroupAr = GLoansAr, SopRef = "CRM-FIX-08",
                TitleEn = "Re-settle loan repayments", TitleAr = "إعادة تسوية سداد القروض",
                DescriptionEn = "Clears each loan's installments and re-applies its completed settlement requests in date order, so paid amounts match the repayments received.",
                DescriptionAr = "يمسح أقساط كل قرض ثم يعيد تطبيق طلبات التسوية المكتملة بترتيب التاريخ، لتتطابق المبالغ المدفوعة مع السداد المستلم.",
                Status = FunctionStatus.UseWithCare,
                NoteEn = "SOP: use with care for allocation errors. In a dry run, steps that read records created earlier in the same run can stop early.",
                NoteAr = "دليل الإجراءات: تُستخدم بحذر لأخطاء التخصيص. في التشغيل التجريبي قد تتوقف الخطوات التي تقرأ سجلات أُنشئت في التشغيل نفسه.",
                Inputs = { new InputSpec { Key = "loans", Kind = InputKind.GuidList, Required = true, LabelEn = "Loan IDs", LabelAr = "معرّفات القروض", HelpEn = "One loan ID (GUID) per line.", HelpAr = "معرّف قرض واحد (GUID) في كل سطر." } },
                Run = (s, v) => { Applications.SettleLoans(s, v.Guids("loans")); return null; }
            },
            new FunctionInfo
            {
                Key = "wallet-settlement", Method = "UpdateFundingWalletSettlement", GroupEn = GLoans, GroupAr = GLoansAr, SopRef = "CRM-FIX-17",
                TitleEn = "Link settlement requests to their funding wallet", TitleAr = "ربط طلبات تسوية المحفظة بمحفظة التمويل",
                DescriptionEn = "Finds active funding-wallet settlement requests with a partner but no wallet, and sets the partner's wallet.",
                DescriptionAr = "يبحث عن طلبات تسوية محفظة التمويل النشطة التي لها شريك دون محفظة، ويعيّن محفظة الشريك.",
                Status = FunctionStatus.UseWithCare,
                Run = (s, v) => { Applications.UpdateFundingWalletSettlement(s); return null; }
            },
            new FunctionInfo
            {
                Key = "loan-requests-wallet", Method = "UpdateLoanRequests", GroupEn = GLoans, GroupAr = GLoansAr,
                TitleEn = "Correct loan request funding wallet and fees", TitleAr = "تصحيح محفظة التمويل والرسوم لطلبات القروض",
                DescriptionEn = "Sets the correct funding wallet on each loan request and recalculates fees: none for funding wallets; 500 or 750 for virtual wallets depending on the amount.",
                DescriptionAr = "يعيّن محفظة التمويل الصحيحة لكل طلب قرض ويعيد احتساب الرسوم: بدون رسوم للمحافظ التمويلية، و500 أو 750 للمحافظ الافتراضية حسب المبلغ.",
                Status = FunctionStatus.UseWithCare,
                ExcelColumnsEn = "A: loan request ID · B: correct funding wallet ID", ExcelColumnsAr = "A: معرّف طلب القرض · B: معرّف محفظة التمويل الصحيحة",
                Inputs = { Excel() },
                Run = (s, v) => { Applications.UpdateLoanRequests(s); return null; }
            },
            new FunctionInfo
            {
                Key = "loan-partner", Method = "UpdateLoanPartner", GroupEn = GLoans, GroupAr = GLoansAr, SopRef = "CRM-FIX-07",
                TitleEn = "Move loan requests to another partner", TitleAr = "نقل طلبات القروض إلى شريك آخر",
                DescriptionEn = "Sets the partner and request status on each loan request in the file.",
                DescriptionAr = "يعيّن الشريك وحالة الطلب لكل طلب قرض في الملف.",
                Status = FunctionStatus.UseWithCare,
                ExcelColumnsEn = "A: loan request ID", ExcelColumnsAr = "A: معرّف طلب القرض",
                Inputs =
                {
                    Excel(),
                    new InputSpec { Key = "partner", Kind = InputKind.Guid, Required = true, LabelEn = "Partner (account) ID", LabelAr = "معرّف الشريك (الحساب)" },
                    new InputSpec { Key = "status", Kind = InputKind.Guid, Required = true, LabelEn = "Request status ID", LabelAr = "معرّف حالة الطلب", Default = "d80f2cc0-89cc-eb11-baeb-00155d003c17", HelpEn = "Pre-filled with the status the console used.", HelpAr = "معبأ مسبقًا بالحالة التي كان يستخدمها تطبيق الكونسول." }
                },
                Run = (s, v) => { Applications.UpdateLoanPartner(s, v.Guid("partner"), v.Guid("status")); return null; }
            },
            new FunctionInfo
            {
                Key = "close-jana-loans", Method = "CloseJanaLoans", GroupEn = GLoans, GroupAr = GLoansAr,
                TitleEn = "Close active Jana loans by national ID", TitleAr = "إغلاق قروض جنى النشطة برقم الهوية",
                DescriptionEn = "For each national ID, finds the customer's active loans with the Jana partner and sets them to closed.",
                DescriptionAr = "لكل رقم هوية، يبحث عن قروض العميل النشطة مع شريك جنى ويغيّر حالتها إلى مغلقة.",
                Status = FunctionStatus.UseWithCare,
                NoteEn = "The partner and the closed status are fixed in the code.", NoteAr = "الشريك والحالة المغلقة ثابتان في الشيفرة.",
                Inputs = { new InputSpec { Key = "ids", Kind = InputKind.TextList, Required = true, LabelEn = "National IDs", LabelAr = "أرقام الهوية الوطنية", HelpEn = "One per line.", HelpAr = "رقم واحد في كل سطر." } },
                Run = (s, v) => { Applications.CloseJanaLoans(s, v.Lines("ids")); return null; }
            },
            new FunctionInfo
            {
                Key = "waseet-response", Method = "UpdateLoanReponseWaseet", GroupEn = GLoans, GroupAr = GLoansAr, SopRef = "CRM-FIX-16",
                TitleEn = "Apply a stuck Waseet loan response", TitleAr = "تطبيق استجابة Waseet العالقة",
                DescriptionEn = "Applies the Waseet loan creation response to a disbursement request that is stuck after the response arrived.",
                DescriptionAr = "يطبّق استجابة إنشاء القرض من Waseet على طلب صرف عالق بعد وصول الاستجابة.",
                Status = FunctionStatus.NeedsFix,
                NoteEn = "SOP: needs fix. The request and response are fixed in the code.", NoteAr = "دليل الإجراءات: تحتاج إلى إصلاح. الطلب والاستجابة ثابتان في الشيفرة.",
                Run = (s, v) => new Applications().UpdateLoanReponseWaseet(s)
            },
            new FunctionInfo
            {
                Key = "waseet-create-loans", Method = "LoanDisbursedRequest_AfterRelationShipOfficerApproval_CreateWaseetLoan", GroupEn = GLoans, GroupAr = GLoansAr,
                TitleEn = "Create Waseet loans for approved disbursements", TitleAr = "إنشاء قروض Waseet للصرف المعتمد",
                DescriptionEn = "For the disbursement request set in the code (after relationship officer approval), creates the customer profile and the loan on Waseet through the gateway.",
                DescriptionAr = "لطلب الصرف المحدد في الشيفرة (بعد اعتماد مسؤول العلاقة)، ينشئ ملف العميل والقرض في Waseet عبر البوابة.",
                Status = FunctionStatus.UseWithCare, DryRunSupported = false,
                NoteEn = "Calls the Waseet gateway (outside system).", NoteAr = "تستدعي بوابة Waseet (نظام خارجي).",
                Run = (s, v) => { Applications.LoanDisbursedRequest_AfterRelationShipOfficerApproval_CreateWaseetLoan(s); return null; }
            },

            // ---------------- licences ----------------
            new FunctionInfo
            {
                Key = "renew-license", Method = "RenewLicense", GroupEn = GLicences, GroupAr = GLicencesAr, SopRef = "CRM-FIX-04",
                TitleEn = "Renew productive-family certificates", TitleAr = "تجديد شهادات الأسر المنتجة",
                DescriptionEn = "Extends each certificate's expiry date by 3 years and creates a renewal request, in one transaction per certificate.",
                DescriptionAr = "يمدد تاريخ انتهاء كل شهادة 3 سنوات وينشئ طلب تجديد، في عملية واحدة لكل شهادة.",
                Status = FunctionStatus.NeedsFix,
                NoteEn = "SOP: needs fix. Columns B and C were never read before this version; check the result in a test environment first.",
                NoteAr = "دليل الإجراءات: تحتاج إلى إصلاح. لم يكن العمودان B وC يُقرآن قبل هذا الإصدار؛ تحقّق من النتيجة في بيئة اختبار أولًا.",
                ExcelColumnsEn = "A: certificate (licence) ID · B: current expiry date · C: requester contact ID", ExcelColumnsAr = "A: معرّف الشهادة · B: تاريخ الانتهاء الحالي · C: معرّف العميل مقدّم الطلب",
                Inputs = { Excel() },
                Run = (s, v) => { Applications.RenewLicense(s); return null; }
            },
            new FunctionInfo
            {
                Key = "license-job", Method = "RenewLicenseWithActiveLoan + SendNotificationLicenseWillbeExpiredNextMonth", GroupEn = GLicences, GroupAr = GLicencesAr, SopRef = "CRM-FIX-04",
                TitleEn = "Run the licence renewal job", TitleAr = "تشغيل مهمة تجديد الشهادات",
                DescriptionEn = "Renews expired certificates of customers with an active loan or group loan, then sends the 30-day expiry notification. Runs the workflows set in the settings file.",
                DescriptionAr = "يجدد الشهادات المنتهية للعملاء الذين لديهم قرض أو قرض جماعي نشط، ثم يرسل إشعار الانتهاء خلال 30 يومًا. يشغّل سير العمل المحدد في ملف الإعدادات.",
                Status = FunctionStatus.UseWithCare,
                Run = (s, v) => { Applications.RenewLicenseWithActiveLoan(s); Applications.SendNotificationLicenseWillbeExpiredNextMonth(s); return null; }
            },

            // ---------------- cases ----------------
            new FunctionInfo
            {
                Key = "check-duplicate-case", Method = "StopDuplicationCase", GroupEn = GCases, GroupAr = GCasesAr, SopRef = "CRM-FIX-19",
                TitleEn = "Check whether a new case would be blocked as a duplicate", TitleAr = "التحقق مما إذا كانت حالة جديدة ستُمنع كمكررة",
                DescriptionEn = "Reproduces the duplicate-case plugin check for a customer: shows whether the customer has an open case that blocks a new one.",
                DescriptionAr = "يعيد تنفيذ فحص إضافة منع تكرار الحالات لعميل: يبين ما إذا كان لدى العميل حالة مفتوحة تمنع إنشاء حالة جديدة.",
                Status = FunctionStatus.ReadOnly,
                Inputs =
                {
                    new InputSpec { Key = "customer", Kind = InputKind.Guid, Required = true, LabelEn = "Customer (contact) ID", LabelAr = "معرّف العميل" },
                    new InputSpec { Key = "owner", Kind = InputKind.Guid, LabelEn = "Owner (user) ID", LabelAr = "معرّف المالك (المستخدم)", HelpEn = "Optional.", HelpAr = "اختياري." },
                    new InputSpec { Key = "source", Kind = InputKind.Integer, LabelEn = "Request source (option value)", LabelAr = "مصدر الطلب (قيمة الخيار)", Default = "2" }
                },
                Run = (s, v) =>
                {
                    bool blocked = Applications.StopDuplicationCase(s, v.Guid("customer"), v.GuidOrNull("owner"), v.IntOrNull("source"));
                    return blocked ? Loc.Pick("Yes — the customer has an open case, so a new case would be blocked.", "نعم — لدى العميل حالة مفتوحة، لذا ستُمنع الحالة الجديدة.")
                                   : Loc.Pick("No — a new case would be allowed.", "لا — سيُسمح بإنشاء حالة جديدة.");
                }
            },
            new FunctionInfo
            {
                Key = "delete-bpf", Method = "DeleteBPFRecords", GroupEn = GCases, GroupAr = GCasesAr, SopRef = "CRM-FIX-20",
                TitleEn = "Reset a stuck case process (business process flow)", TitleAr = "إعادة ضبط مسار عملية حالة عالق",
                DescriptionEn = "Deletes the case's business process flow record so a new one starts. Afterwards, move the new process to the right stage.",
                DescriptionAr = "يحذف سجل مسار عملية الأعمال للحالة ليبدأ مسار جديد. بعد ذلك انقل المسار الجديد إلى المرحلة الصحيحة.",
                Status = FunctionStatus.Ready,
                Inputs = { new InputSpec { Key = "case", Kind = InputKind.Guid, Required = true, LabelEn = "Case ID", LabelAr = "معرّف الحالة" } },
                Run = (s, v) => { Applications.DeleteBPFRecords(s, v.Guid("case")); return null; }
            },
            new FunctionInfo
            {
                Key = "sla-matrix", Method = "SLAMatixForCases", GroupEn = GCases, GroupAr = GCasesAr, SopRef = "CRM-FIX-18",
                TitleEn = "Recalculate case escalation levels (SLA matrix)", TitleAr = "إعادة احتساب مستويات تصعيد الحالات (مصفوفة SLA)",
                DescriptionEn = "Recalculates the escalation level of open cases from the SLA matrix and the customer service calendar.",
                DescriptionAr = "يعيد احتساب مستوى التصعيد للحالات المفتوحة من مصفوفة SLA وتقويم خدمة العملاء.",
                Status = FunctionStatus.NeedsFix,
                NoteEn = "SOP: fix the time calculation first. Only cases owned by the team set in the code are recalculated.", NoteAr = "دليل الإجراءات: أصلح احتساب الوقت أولًا. يُعاد الاحتساب فقط للحالات المملوكة للفريق المحدد في الشيفرة.",
                Run = (s, v) => { Applications.SLAMatixForCases(s); return null; }
            },
            new FunctionInfo
            {
                Key = "migrate-cases", Method = "MigrateCases", GroupEn = GCases, GroupAr = GCasesAr,
                TitleEn = "Migrate cases to the new process", TitleAr = "ترحيل الحالات إلى العملية الجديدة",
                DescriptionEn = "Moves active cases that have a product but no case status or process yet to the new status, reason and process flow, and assigns them to the team set in the code.",
                DescriptionAr = "ينقل الحالات النشطة التي لها منتج ولا حالة أو مسار عملية لها بعد إلى الحالة والسبب ومسار العملية الجديد، ويسندها إلى الفريق المحدد في الشيفرة.",
                Status = FunctionStatus.UseWithCare,
                Inputs =
                {
                    new InputSpec { Key = "status", Kind = InputKind.Text, Required = true, LabelEn = "New status code", LabelAr = "رمز الحالة الجديدة", Default = "CSC_CSNew" },
                    new InputSpec { Key = "reason", Kind = InputKind.Text, Required = true, LabelEn = "Ticket reason code", LabelAr = "رمز سبب التذكرة", Default = "Migrate" }
                },
                Run = (s, v) =>
                {
                    string statusId = Applications.GetStatusFromCode(v.Text("status"), s);
                    string reasonId = Applications.GetTicketReasonByCode(v.Text("reason"), s);
                    if (string.IsNullOrEmpty(statusId) || string.IsNullOrEmpty(reasonId))
                        throw new InvalidOperationException("Could not find the status or the ticket reason for these codes.");
                    Applications.MigrateCases(s, statusId, reasonId);
                    return null;
                }
            },
            new FunctionInfo
            {
                Key = "restore-templates", Method = "UpdateIds", GroupEn = GCases, GroupAr = GCasesAr, SopRef = "CRM-FIX-22",
                TitleEn = "Restore template values from audit history", TitleAr = "استعادة قيم القوالب من سجل التدقيق",
                DescriptionEn = "For each template, reads the latest change in the audit history and puts the old values back.",
                DescriptionAr = "لكل قالب، يقرأ آخر تغيير في سجل التدقيق ويعيد القيم القديمة.",
                Status = FunctionStatus.NeedsFix,
                NoteEn = "SOP: fix the order first (audit entries are not sorted before the latest is taken).", NoteAr = "دليل الإجراءات: أصلح الترتيب أولًا (لا تُرتَّب سجلات التدقيق قبل أخذ أحدثها).",
                ExcelColumnsEn = "A: template (los_sdbqabooltemplate) ID", ExcelColumnsAr = "A: معرّف القالب (los_sdbqabooltemplate)",
                Inputs = { Excel() },
                Run = (s, v) => { Applications.UpdateIds(s); return null; }
            },

            // ---------------- messages ----------------
            new FunctionInfo
            {
                Key = "cancel-emails", Method = "CancelEmails", GroupEn = GMessages, GroupAr = GMessagesAr, SopRef = "CRM-FIX-21",
                TitleEn = "Cancel emails stuck in Pending Send", TitleAr = "إلغاء الرسائل العالقة في حالة انتظار الإرسال",
                DescriptionEn = "Cancels every email in Pending Send or Sending status.",
                DescriptionAr = "يلغي كل رسالة بريد في حالة انتظار الإرسال أو جارٍ الإرسال.",
                Status = FunctionStatus.NeedsFix,
                NoteEn = "SOP: add filters first. Today it cancels all pending emails, not only the ones the business chose.", NoteAr = "دليل الإجراءات: أضف عوامل التصفية أولًا. حاليًا تلغي كل الرسائل المعلقة وليس فقط ما حدده قطاع الأعمال.",
                Run = (s, v) => { Applications.CancelEmails(s); return null; }
            },
            new FunctionInfo
            {
                Key = "resend-sms", Method = "TestSMSWithRMQueue", GroupEn = GMessages, GroupAr = GMessagesAr, SopRef = "CRM-FIX-15",
                TitleEn = "Re-send an SMS message through RabbitMQ", TitleAr = "إعادة إرسال رسالة SMS عبر RabbitMQ",
                DescriptionEn = "Rebuilds the SMS from a RabbitMQ message record and its notification template, and pushes it to the queue again.",
                DescriptionAr = "يعيد بناء رسالة SMS من سجل رسالة RabbitMQ وقالب الإشعار، ويدفعها إلى الطابور مرة أخرى.",
                Status = FunctionStatus.NeedsFix, DryRunSupported = false,
                NoteEn = "SOP: needs fix (SMS only). Sends to RabbitMQ (outside system).", NoteAr = "دليل الإجراءات: تحتاج إلى إصلاح (الرسائل النصية فقط). ترسل إلى RabbitMQ (نظام خارجي).",
                Inputs = { new InputSpec { Key = "message", Kind = InputKind.Guid, Required = true, LabelEn = "RabbitMQ message record ID", LabelAr = "معرّف سجل رسالة RabbitMQ" } },
                Run = (s, v) => { Applications.TestSMSWithRMQueue(s, v.Guid("message")); return null; }
            },

            // ---------------- access ----------------
            new FunctionInfo
            {
                Key = "enable-disable-users", Method = "EnableDisableUsers", GroupEn = GAccess, GroupAr = GAccessAr, SopRef = "CRM-SRV-01 / CRM-SRV-04",
                TitleEn = "Enable or disable CRM users", TitleAr = "تفعيل مستخدمي CRM أو تعطيلهم",
                DescriptionEn = "Finds users by email address and enables or disables them in CRM. Also disable or enable them in Active Directory (CRM-ACC-02).",
                DescriptionAr = "يبحث عن المستخدمين بالبريد الإلكتروني ويفعّلهم أو يعطّلهم في CRM. عطّلهم أو فعّلهم أيضًا في Active Directory (CRM-ACC-02).",
                Status = FunctionStatus.UseWithCare,
                ExcelColumnsEn = "A: user email address", ExcelColumnsAr = "A: البريد الإلكتروني للمستخدم",
                Inputs =
                {
                    Excel(),
                    new InputSpec { Key = "enable", Kind = InputKind.YesNo, LabelEn = "Enable the users (untick to disable)", LabelAr = "تفعيل المستخدمين (ألغِ التحديد للتعطيل)", Default = "true" }
                },
                Run = (s, v) => { Applications.EnableDisableUsers(s, v.YesNo("enable")); return null; }
            },
            new FunctionInfo
            {
                Key = "role-to-users", Method = "AssociateRoleToUser", GroupEn = GAccess, GroupAr = GAccessAr, SopRef = "CRM-SRV-01",
                TitleEn = "Add a security role to users", TitleAr = "إضافة دور أمان إلى المستخدمين",
                DescriptionEn = "Adds the security role set in the code to each active user in the file, in the user's business unit.",
                DescriptionAr = "يضيف دور الأمان المحدد في الشيفرة إلى كل مستخدم نشط في الملف، ضمن وحدة العمل الخاصة به.",
                Status = FunctionStatus.UseWithCare,
                NoteEn = "The role name is fixed in the code. SOP prefers roles through teams.", NoteAr = "اسم الدور ثابت في الشيفرة. يفضّل دليل الإجراءات إسناد الأدوار عبر الفرق.",
                ExcelColumnsEn = "A: user email address", ExcelColumnsAr = "A: البريد الإلكتروني للمستخدم",
                Inputs = { Excel() },
                Run = (s, v) => { Applications.AssociateRoleToUser(s); return null; }
            },
            new FunctionInfo
            {
                Key = "role-to-teams", Method = "AssociateRoleToTeam", GroupEn = GAccess, GroupAr = GAccessAr,
                TitleEn = "Add a security role to all owner teams", TitleAr = "إضافة دور أمان إلى جميع فرق الملكية",
                DescriptionEn = "Adds the security role set in the code to every owner team, in each team's business unit.",
                DescriptionAr = "يضيف دور الأمان المحدد في الشيفرة إلى كل فرق الملكية، ضمن وحدة العمل لكل فريق.",
                Status = FunctionStatus.UseWithCare,
                NoteEn = "Affects every owner team. The role is fixed in the code.", NoteAr = "يؤثر على كل فرق الملكية. الدور ثابت في الشيفرة.",
                Run = (s, v) => { Applications.AssociateRoleToTeam(s); return null; }
            },
            new FunctionInfo
            {
                Key = "copy-team-members", Method = "CopyTeamMembers", GroupEn = GAccess, GroupAr = GAccessAr, SopRef = "CRM-SRV-01",
                TitleEn = "Copy members from one team to another", TitleAr = "نسخ الأعضاء من فريق إلى آخر",
                DescriptionEn = "Adds every member of the source team to the target team.",
                DescriptionAr = "يضيف كل أعضاء الفريق المصدر إلى الفريق الهدف.",
                Status = FunctionStatus.Ready,
                Inputs =
                {
                    new InputSpec { Key = "source", Kind = InputKind.Guid, Required = true, LabelEn = "Source team ID", LabelAr = "معرّف الفريق المصدر" },
                    new InputSpec { Key = "target", Kind = InputKind.Guid, Required = true, LabelEn = "Target team ID", LabelAr = "معرّف الفريق الهدف" }
                },
                Run = (s, v) => { Applications.CopyTeamMembers(s, v.Guid("source"), v.Guid("target")); return null; }
            },

            // ---------------- configuration scripts ----------------
            new FunctionInfo
            {
                Key = "representative-config", Method = "AddRepresenativeConfigurations", GroupEn = GSetup, GroupAr = GSetupAr, SopRef = "CRM-SRV-07",
                TitleEn = "Add partner representative configuration", TitleAr = "إضافة إعدادات ممثل الشريك",
                DescriptionEn = "For an active partner account with a representative, creates the customer-related-entity link and the agreement authorization records.",
                DescriptionAr = "لحساب شريك نشط له ممثل، ينشئ سجل ربط الجهة المرتبطة بالعميل وسجلات تفويض الاتفاقية.",
                Status = FunctionStatus.UseWithCare,
                Inputs = { new InputSpec { Key = "account", Kind = InputKind.Guid, Required = true, LabelEn = "Partner account ID", LabelAr = "معرّف حساب الشريك" } },
                Run = (s, v) => { Applications.AddRepresenativeConfigurations(s, v.Text("account")); return null; }
            },
            new FunctionInfo
            {
                Key = "add-ticket-reasons", Method = "AddTicketReasons", GroupEn = GSetup, GroupAr = GSetupAr, SopRef = "CRM-SRV-11",
                TitleEn = "Add ticket reasons to classifications", TitleAr = "إضافة أسباب التذاكر إلى التصنيفات",
                DescriptionEn = "Creates the ticket reasons set in the code under each ticket classification.",
                DescriptionAr = "ينشئ أسباب التذاكر المحددة في الشيفرة تحت كل تصنيف تذاكر.",
                Status = FunctionStatus.UseWithCare,
                NoteEn = "Reasons are fixed in the code. SOP: build in UAT and release as a standard change.", NoteAr = "الأسباب ثابتة في الشيفرة. دليل الإجراءات: أنشئها في UAT وأصدرها كتغيير قياسي.",
                Inputs = { new InputSpec { Key = "ids", Kind = InputKind.GuidList, Required = true, LabelEn = "Ticket classification IDs", LabelAr = "معرّفات تصنيفات التذاكر", HelpEn = "One per line.", HelpAr = "معرّف واحد في كل سطر." } },
                Run = (s, v) => { Applications.AddTicketReasons(s, v.Guids("ids").ToArray()); return null; }
            },
            new FunctionInfo
            {
                Key = "update-ticket-reasons", Method = "UpdateTicketReasons", GroupEn = GSetup, GroupAr = GSetupAr, SopRef = "CRM-SRV-11",
                TitleEn = "Update ticket reasons", TitleAr = "تحديث أسباب التذاكر",
                DescriptionEn = "Applies the ticket reason changes set in the code.", DescriptionAr = "يطبّق تغييرات أسباب التذاكر المحددة في الشيفرة.",
                Status = FunctionStatus.UseWithCare, NoteEn = "Values are fixed in the code.", NoteAr = "القيم ثابتة في الشيفرة.",
                Run = (s, v) => { Applications.UpdateTicketReasons(s); return null; }
            },
            new FunctionInfo
            {
                Key = "additional-field-config", Method = "AddAdditionalFieldConfiguration", GroupEn = GSetup, GroupAr = GSetupAr, SopRef = "CRM-SRV-11",
                TitleEn = "Add additional field configuration", TitleAr = "إضافة إعدادات الحقول الإضافية",
                DescriptionEn = "Creates the additional field configuration set in the code.", DescriptionAr = "ينشئ إعدادات الحقول الإضافية المحددة في الشيفرة.",
                Status = FunctionStatus.UseWithCare, NoteEn = "Values are fixed in the code.", NoteAr = "القيم ثابتة في الشيفرة.",
                Run = (s, v) => { Applications.AddAdditionalFieldConfiguration(s); return null; }
            },
            new FunctionInfo
            {
                Key = "sub-products", Method = "AddSubProductsAndAssociateReasons", GroupEn = GSetup, GroupAr = GSetupAr, SopRef = "CRM-SRV-11",
                TitleEn = "Add sub-products and link their reasons", TitleAr = "إضافة المنتجات الفرعية وربط أسبابها",
                DescriptionEn = "Creates sub-products from the file and links the ticket reasons set in the code.", DescriptionAr = "ينشئ المنتجات الفرعية من الملف ويربط أسباب التذاكر المحددة في الشيفرة.",
                Status = FunctionStatus.UseWithCare,
                ExcelColumnsEn = "As the code expects (see AddSubProductsAndAssociateReasons).", ExcelColumnsAr = "كما تتوقع الشيفرة (راجع AddSubProductsAndAssociateReasons).",
                Inputs = { Excel() },
                Run = (s, v) => { Applications.AddSubProductsAndAssociateReasons(s); return null; }
            },
            new FunctionInfo
            {
                Key = "processes-owner", Method = "UpdateProcessesOwner", GroupEn = GSetup, GroupAr = GSetupAr,
                TitleEn = "Change the owner of processes", TitleAr = "تغيير مالك العمليات",
                DescriptionEn = "Reassigns the processes (workflows) selected in the code to the owner set in the code.", DescriptionAr = "يعيد إسناد العمليات (سير العمل) المحددة في الشيفرة إلى المالك المحدد في الشيفرة.",
                Status = FunctionStatus.UseWithCare, NoteEn = "Processes and owner are fixed in the code.", NoteAr = "العمليات والمالك ثابتة في الشيفرة.",
                Run = (s, v) => { Applications.UpdateProcessesOwner(s); return null; }
            },

            // ---------------- reports ----------------
            new FunctionInfo
            {
                Key = "export-fetchxml", Method = "GetRecordsByFetchXml", GroupEn = GReports, GroupAr = GReportsAr, SopRef = "CRM-SRV-13",
                TitleEn = "Export records with FetchXML", TitleAr = "تصدير السجلات باستخدام FetchXML",
                DescriptionEn = "Runs a FetchXML query with paging and saves every record to records.csv in the evidence folder.",
                DescriptionAr = "ينفّذ استعلام FetchXML مع التقسيم إلى صفحات ويحفظ كل السجلات في الملف records.csv داخل مجلد الأدلة.",
                Status = FunctionStatus.ReadOnly,
                Inputs = { new InputSpec { Key = "fetch", Kind = InputKind.MultilineText, Required = true, LabelEn = "FetchXML", LabelAr = "FetchXML", HelpEn = "Copy it from Advanced Find → Download FetchXML. Do not include paging attributes.", HelpAr = "انسخه من البحث المتقدم ← تنزيل FetchXML. لا تُضمّن خصائص التقسيم إلى صفحات." } },
                Run = (s, v) =>
                {
                    var records = Applications.GetRecordsByFetchXml(s, v.Text("fetch"));
                    Engine.Evidence.WriteRecordsCsv(records, "records.csv");
                    return Loc.Pick($"{records.Count} records saved to records.csv.", $"حُفظ {records.Count} سجلًا في records.csv.");
                }
            },
            new FunctionInfo
            {
                Key = "agreements", Method = "GetAgreementActiveList", GroupEn = GReports, GroupAr = GReportsAr,
                TitleEn = "List a customer's active e-market agreements", TitleAr = "عرض اتفاقيات السوق الإلكتروني النشطة للعميل",
                DescriptionEn = "Shows the active agreement authorizations for a contact.", DescriptionAr = "يعرض تفويضات الاتفاقيات النشطة لعميل.",
                Status = FunctionStatus.ReadOnly,
                Inputs = { new InputSpec { Key = "contact", Kind = InputKind.Guid, Required = true, LabelEn = "Contact ID", LabelAr = "معرّف العميل" } },
                Run = (s, v) =>
                {
                    var list = Applications.GetAgreementActiveList(s, v.Guid("contact")).GetAwaiter().GetResult();
                    return list == null || list.Count == 0 ? Loc.Pick("No active agreements.", "لا توجد اتفاقيات نشطة.") : string.Join(Environment.NewLine, list);
                }
            },
        };
    }
}
