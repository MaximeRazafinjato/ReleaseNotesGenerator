using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace ReleaseNotesGenerator.UI.Behaviors;

public static class FocusBehavior
{
    public static readonly AttachedProperty<bool> IsFocusedProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsFocused", typeof(FocusBehavior));

    static FocusBehavior()
    {
        IsFocusedProperty.Changed.AddClassHandler<Control>(OnIsFocusedChanged);
    }

    public static bool GetIsFocused(Control control)
    {
        return control.GetValue(IsFocusedProperty);
    }

    public static void SetIsFocused(Control control, bool value)
    {
        control.SetValue(IsFocusedProperty, value);
    }

    private static void OnIsFocusedChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is bool isFocused && isFocused)
        {
            control.Focus(NavigationMethod.Directional);
        }
    }
}