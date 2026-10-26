using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace BitchManager.Core;

/// <summary>
/// Minimal INotifyPropertyChanged base, the C# stand-in for SwiftUI's @Published.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Raise(name);
        return true;
    }

    public void Raise([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private readonly Dictionary<string, object?> _store = new();

    /// <summary>Compact backing store for the many @Published-style settings on the engine.</summary>
    protected T G<T>(T fallback, [CallerMemberName] string name = "")
        => _store.TryGetValue(name, out var v) ? (T)v! : fallback;

    protected void S<T>(T value, [CallerMemberName] string name = "")
    {
        if (_store.TryGetValue(name, out var old) && EqualityComparer<T>.Default.Equals((T)old!, value)) return;
        _store[name] = value;
        Raise(name);
    }

    /// <summary>Signals that everything changed (used after bulk updates such as loading a slot).</summary>
    public void RaiseAll() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}

public static class ObserveExtensions
{
    /// <summary>
    /// Runs <paramref name="update"/> now and again whenever one of <paramref name="props"/> changes
    /// on <paramref name="source"/>, for as long as <paramref name="owner"/> is in the visual tree.
    /// An empty list means "any property". This is how the code-built UI re-renders, much like a
    /// SwiftUI body recomputing when an @Published value changes.
    /// </summary>
    public static T Observe<T>(this T owner, INotifyPropertyChanged source, Action update, params string[] props)
        where T : FrameworkElement
    {
        void Handler(object? s, PropertyChangedEventArgs e)
        {
            if (props.Length == 0 || string.IsNullOrEmpty(e.PropertyName) || Array.IndexOf(props, e.PropertyName) >= 0)
                update();
        }

        var attached = false;
        void Attach()
        {
            if (attached) return;
            source.PropertyChanged += Handler;
            attached = true;
        }

        update();
        Attach();
        owner.Loaded += (_, _) => { if (!attached) { Attach(); update(); } };
        owner.Unloaded += (_, _) => { if (attached) { source.PropertyChanged -= Handler; attached = false; } };
        return owner;
    }
}
