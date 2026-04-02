using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrivilegedAuditSuite.App.Behaviors;

public static class ScrollViewerBehavior
{
    public static readonly DependencyProperty UseGentleMouseWheelProperty =
        DependencyProperty.RegisterAttached(
            "UseGentleMouseWheel",
            typeof(bool),
            typeof(ScrollViewerBehavior),
            new PropertyMetadata(false, OnUseGentleMouseWheelChanged));

    public static readonly DependencyProperty MouseWheelStepProperty =
        DependencyProperty.RegisterAttached(
            "MouseWheelStep",
            typeof(double),
            typeof(ScrollViewerBehavior),
            new PropertyMetadata(36d));

    public static bool GetUseGentleMouseWheel(DependencyObject element)
    {
        return (bool)element.GetValue(UseGentleMouseWheelProperty);
    }

    public static void SetUseGentleMouseWheel(DependencyObject element, bool value)
    {
        element.SetValue(UseGentleMouseWheelProperty, value);
    }

    public static double GetMouseWheelStep(DependencyObject element)
    {
        return (double)element.GetValue(MouseWheelStepProperty);
    }

    public static void SetMouseWheelStep(DependencyObject element, double value)
    {
        element.SetValue(MouseWheelStepProperty, value);
    }

    private static void OnUseGentleMouseWheelChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is not ScrollViewer scrollViewer)
        {
            return;
        }

        if ((bool)eventArgs.NewValue)
        {
            scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
        }
        else
        {
            scrollViewer.PreviewMouseWheel -= OnPreviewMouseWheel;
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs eventArgs)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        var step = GetMouseWheelStep(scrollViewer);
        var direction = eventArgs.Delta < 0 ? 1d : -1d;
        var nextOffset = Math.Max(0d, scrollViewer.VerticalOffset + direction * step);

        scrollViewer.ScrollToVerticalOffset(nextOffset);
        eventArgs.Handled = true;
    }
}
