using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CrmFunctionRunner.Infrastructure
{
    /// <summary>English / Arabic interface text. Bind with {Binding [Key], Source={x:Static i:Loc.Instance}}.</summary>
    public sealed class Loc : INotifyPropertyChanged
    {
        public static Loc Instance { get; } = new Loc();
        public static bool IsArabic => Instance._arabic;
        public static string T(string key) => Instance[key];
        public static string Pick(string en, string ar) => IsArabic && !string.IsNullOrEmpty(ar) ? ar : en;

        bool _arabic;
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action LanguageChanged;

        public FlowDirection Direction => _arabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        public void SetArabic(bool arabic)
        {
            if (_arabic == arabic) return;
            _arabic = arabic;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Direction)));
            LanguageChanged?.Invoke();
        }

        public string this[string key]
        {
            get
            {
                if (Strings.TryGetValue(key, out var pair)) return _arabic ? pair[1] : pair[0];
                return key;
            }
        }

        static readonly Dictionary<string, string[]> Strings = new Dictionary<string, string[]>
        {
            ["AppTitle"] = new[] { "CRM Function Runner", "منفّذ وظائف CRM" },
            ["Language"] = new[] { "عربي", "English" },
            ["ChangeConnection"] = new[] { "Change connection", "تغيير الاتصال" },
            ["ProductionBanner"] = new[] { "PRODUCTION — changes made here affect live customers and are recorded for audit.", "بيئة الإنتاج — التغييرات هنا تؤثر على العملاء الفعليين وتُسجَّل لأغراض التدقيق." },
            ["Search"] = new[] { "Search functions or symptoms", "ابحث في الوظائف أو الأعراض" },
            ["PickFunction"] = new[] { "Choose a function on the left to start.", "اختر وظيفة من القائمة لتبدأ." },
            ["PickFunctionHint"] = new[] { "Every run follows the same steps: fill the inputs, run a dry run, confirm, run, and keep the evidence.", "كل تشغيل يمر بالخطوات نفسها: إدخال البيانات، ثم تشغيل تجريبي، ثم التأكيد، ثم التشغيل، ثم حفظ الأدلة." },

            ["Step1"] = new[] { "1  Inputs", "١  المدخلات" },
            ["Step2"] = new[] { "2  Dry run", "٢  تشغيل تجريبي" },
            ["Step3"] = new[] { "3  Confirm", "٣  التأكيد" },
            ["Step4"] = new[] { "4  Run", "٤  التشغيل" },
            ["Step5"] = new[] { "5  Result", "٥  النتيجة" },

            ["Status.Ready"] = new[] { "Ready", "جاهزة" },
            ["Status.UseWithCare"] = new[] { "Use with care", "تُستخدم بحذر" },
            ["Status.ReadOnly"] = new[] { "Read only", "قراءة فقط" },
            ["Status.NeedsFix"] = new[] { "Needs fix", "تحتاج إلى إصلاح" },
            ["NeedsFixBlocked"] = new[] { "This function is marked “needs fix” in the SOP. It can be viewed but not run until it is corrected. An administrator can allow it with AllowNeedsFixFunctions in the settings file.", "هذه الوظيفة مصنّفة «تحتاج إلى إصلاح» في دليل الإجراءات. يمكن عرضها ولا يمكن تشغيلها حتى تُصحَّح. يمكن للمسؤول السماح بها عبر الإعداد AllowNeedsFixFunctions." },
            ["NoDryRun"] = new[] { "This function calls an outside system, so a dry run would still send real requests. It goes straight to confirmation.", "تستدعي هذه الوظيفة نظامًا خارجيًا، لذلك فإن التشغيل التجريبي سيُرسل طلبات حقيقية. لذا تنتقل مباشرة إلى التأكيد." },

            ["SopReference"] = new[] { "SOP reference", "مرجع الإجراء" },
            ["Method"] = new[] { "Console function", "دالة تطبيق الكونسول" },
            ["Ticket"] = new[] { "Jira ticket number", "رقم تذكرة Jira" },
            ["TicketHelp"] = new[] { "Required. Every run is a controlled change (CRM-DAT-01).", "إلزامي. كل تشغيل يُعدّ تغييرًا مضبوطًا (CRM-DAT-01)." },
            ["Inputs"] = new[] { "Inputs", "المدخلات" },
            ["NoInputs"] = new[] { "This function has no inputs. It finds the records to change itself.", "لا تحتاج هذه الوظيفة إلى مدخلات، فهي تحدد السجلات بنفسها." },
            ["ExcelColumns"] = new[] { "Excel columns", "أعمدة ملف Excel" },
            ["Browse"] = new[] { "Choose file…", "اختر ملفًا…" },
            ["RowsInFile"] = new[] { "{0} rows. First rows:", "{0} صفًا. أول الصفوف:" },
            ["Required"] = new[] { "Required", "إلزامي" },

            ["RunDryRun"] = new[] { "Run dry run", "تشغيل تجريبي" },
            ["ContinueNoDryRun"] = new[] { "Continue to confirmation", "المتابعة إلى التأكيد" },
            ["DryRunExplain"] = new[] { "Nothing is saved to CRM. Reads run normally; every create, update, delete and status change is recorded instead of being sent.", "لا يُحفظ شيء في CRM. تُنفَّذ القراءة كالمعتاد، وتُسجَّل كل عمليات الإنشاء والتعديل والحذف وتغيير الحالة بدل إرسالها." },
            ["Stop"] = new[] { "Stop", "إيقاف" },
            ["Back"] = new[] { "Back to inputs", "العودة إلى المدخلات" },
            ["ContinueConfirm"] = new[] { "Continue to confirmation", "المتابعة إلى التأكيد" },
            ["PlannedChanges"] = new[] { "Planned changes", "التغييرات المخطط لها" },
            ["Changes"] = new[] { "Changes", "التغييرات" },
            ["Log"] = new[] { "Output", "المخرجات" },

            ["Col.Op"] = new[] { "Operation", "العملية" },
            ["Col.Entity"] = new[] { "Entity", "الكيان" },
            ["Col.Record"] = new[] { "Record", "السجل" },
            ["Col.Field"] = new[] { "Field", "الحقل" },
            ["Col.Before"] = new[] { "Before", "قبل" },
            ["Col.After"] = new[] { "After", "بعد" },
            ["Col.Status"] = new[] { "Status", "الحالة" },

            ["ConfirmTitle"] = new[] { "Confirm the run", "تأكيد التشغيل" },
            ["Environment"] = new[] { "Environment", "البيئة" },
            ["Function"] = new[] { "Function", "الوظيفة" },
            ["PlannedSummary"] = new[] { "Dry run result", "نتيجة التشغيل التجريبي" },
            ["Approver"] = new[] { "Approved by", "المعتمِد" },
            ["ApproverHelp"] = new[] { "Name of the person who approved this change in the ticket.", "اسم الشخص الذي اعتمد هذا التغيير في التذكرة." },
            ["ReviewedCheck"] = new[] { "I reviewed the dry run result and it matches the ticket.", "راجعت نتيجة التشغيل التجريبي وهي مطابقة للتذكرة." },
            ["ExternalCheck"] = new[] { "I understand this run sends real requests to an outside system.", "أفهم أن هذا التشغيل يرسل طلبات حقيقية إلى نظام خارجي." },
            ["TypeToConfirm"] = new[] { "Type {0} to confirm", "اكتب {0} للتأكيد" },
            ["RunOn"] = new[] { "Run on {0}", "تشغيل على {0}" },

            ["Running"] = new[] { "Running…", "جارٍ التشغيل…" },
            ["DryRunning"] = new[] { "Dry run in progress…", "التشغيل التجريبي جارٍ…" },
            ["Outcome.Succeeded"] = new[] { "Finished", "اكتمل" },
            ["Outcome.Failed"] = new[] { "Stopped with an error", "توقف بسبب خطأ" },
            ["Outcome.Cancelled"] = new[] { "Stopped by you", "أوقفته أنت" },
            ["Counts"] = new[] { "{0} changes saved · {1} failed · {2} records read", "{0} تغييرًا محفوظًا · {1} فشل · {2} سجلًا مقروءًا" },
            ["DryCounts"] = new[] { "{0} changes planned on {1} records · {2} records read", "{0} تغييرًا مخططًا على {1} سجلًا · {2} سجلًا مقروءًا" },
            ["EvidenceFolder"] = new[] { "Evidence folder", "مجلد الأدلة" },
            ["OpenFolder"] = new[] { "Open evidence folder", "فتح مجلد الأدلة" },
            ["CopyClosure"] = new[] { "Copy closure comment", "نسخ تعليق الإغلاق" },
            ["Copied"] = new[] { "Closure comment copied. Paste it into the Jira ticket.", "نُسخ تعليق الإغلاق. الصقه في تذكرة Jira." },
            ["StartAgain"] = new[] { "Run another function", "تشغيل وظيفة أخرى" },
            ["ReturnValue"] = new[] { "Result", "النتيجة" },

            ["Connect.Title"] = new[] { "Connect to CRM", "الاتصال بـ CRM" },
            ["Connect.Intro"] = new[] { "Choose the environment and how to sign in. Your password is used for this session only and is never saved.", "اختر البيئة وطريقة تسجيل الدخول. تُستخدم كلمة المرور لهذه الجلسة فقط ولا تُحفظ." },
            ["Connect.Windows"] = new[] { "My Windows account", "حساب Windows الخاص بي" },
            ["Connect.Password"] = new[] { "User name and password", "اسم المستخدم وكلمة المرور" },
            ["Connect.User"] = new[] { "User name (DOMAIN\\user)", "اسم المستخدم (DOMAIN\\user)" },
            ["Connect.Pwd"] = new[] { "Password", "كلمة المرور" },
            ["Connect.Go"] = new[] { "Connect", "اتصال" },
            ["Connect.Working"] = new[] { "Connecting…", "جارٍ الاتصال…" },
            ["Connect.NoEnvironments"] = new[] { "No environments are configured. Add Environment:… entries to CrmFunctionRunner.exe.config.", "لا توجد بيئات معرّفة. أضف إدخالات Environment:… إلى الملف CrmFunctionRunner.exe.config." },
            ["Cancel"] = new[] { "Cancel", "إلغاء" },

            ["Err.Ticket"] = new[] { "Enter the Jira ticket number.", "أدخل رقم تذكرة Jira." },
            ["Err.Required"] = new[] { "“{0}” is required.", "الحقل «{0}» إلزامي." },
            ["Err.Guid"] = new[] { "“{0}” must be a record ID (GUID), for example 9e88ae11-c80e-f011-b87b-00505692a571.", "يجب أن يكون «{0}» معرّف سجل (GUID)، مثل 9e88ae11-c80e-f011-b87b-00505692a571." },
            ["Err.GuidList"] = new[] { "“{0}” has a value that is not a record ID: {1}", "يحتوي «{0}» على قيمة ليست معرّف سجل: {1}" },
            ["Err.Int"] = new[] { "“{0}” must be a whole number.", "يجب أن يكون «{0}» رقمًا صحيحًا." },
            ["Err.File"] = new[] { "The file for “{0}” was not found.", "لم يُعثر على الملف الخاص بـ «{0}»." },
        };
    }

    public abstract class Observable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void Raise([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        protected void RaiseAll() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        protected bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            Raise(name);
            return true;
        }
    }
}
