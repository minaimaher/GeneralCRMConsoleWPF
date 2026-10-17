using CrmFunctionRunner.Catalog;
using CrmFunctionRunner.Engine;
using CrmFunctionRunner.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace CrmFunctionRunner.Infrastructure
{
    /// <summary>Visible when the current step equals the parameter (comma-separated list allowed).</summary>
    public sealed class StepVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Step step) || parameter == null) return Visibility.Collapsed;
            foreach (var p in parameter.ToString().Split(','))
                if (Enum.TryParse(p.Trim(), out Step s) && s == step) return Visibility.Visible;
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    /// <summary>Step header state: 0 = done, 1 = current, 2 = to do.</summary>
    public sealed class StepStateBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Step step) || !Enum.TryParse(parameter?.ToString(), out Step mine)) return Brushes.Gray;
            var current = step == Step.Result ? Step.Result : step;
            if (current == mine) return Application.Current.Resources["Accent"];
            return current > mine ? Application.Current.Resources["Ok"] : Application.Current.Resources["Muted"];
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public sealed class BoolVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool v && v;
            if (value is string s) b = !string.IsNullOrEmpty(s);
            if (Invert) b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public sealed class StatusBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string key = "Muted";
            if (value is FunctionStatus fs)
                key = fs == FunctionStatus.Ready ? "Ok" : fs == FunctionStatus.UseWithCare ? "Warn" : fs == FunctionStatus.NeedsFix ? "Crit" : "Accent";
            else if (value is ChangeStatus cs)
                key = cs == ChangeStatus.Done ? "Ok" : cs == ChangeStatus.Failed ? "Crit" : "Accent";
            return Application.Current.Resources[key];
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    /// <summary>Chooses the editor template for an input field.</summary>
    public sealed class InputTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Text { get; set; }
        public DataTemplate Multiline { get; set; }
        public DataTemplate YesNo { get; set; }
        public DataTemplate Excel { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var f = item as InputField;
            if (f == null) return base.SelectTemplate(item, container);
            switch (f.Kind)
            {
                case InputKind.MultilineText:
                case InputKind.GuidList:
                case InputKind.TextList: return Multiline;
                case InputKind.YesNo: return YesNo;
                case InputKind.ExcelFile: return Excel;
                default: return Text;
            }
        }
    }
}
