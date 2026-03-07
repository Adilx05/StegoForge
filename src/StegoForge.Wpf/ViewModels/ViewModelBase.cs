using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StegoForge.Wpf.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errorsByProperty = new(StringComparer.Ordinal);

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _errorsByProperty.Count > 0;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return _errorsByProperty.Values.SelectMany(static errors => errors).ToArray();
        }

        return _errorsByProperty.TryGetValue(propertyName, out var errors)
            ? errors
            : Array.Empty<string>();
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void SetErrors(string propertyName, IEnumerable<string> errors)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentNullException.ThrowIfNull(errors);

        var normalized = errors
            .Where(static error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var hadExistingErrors = _errorsByProperty.Remove(propertyName);
        if (normalized.Count > 0)
        {
            _errorsByProperty[propertyName] = normalized;
        }

        if (hadExistingErrors || normalized.Count > 0)
        {
            OnErrorsChanged(propertyName);
            OnPropertyChanged(nameof(HasErrors));
        }
    }

    protected void ClearErrors(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        if (_errorsByProperty.Remove(propertyName))
        {
            OnErrorsChanged(propertyName);
            OnPropertyChanged(nameof(HasErrors));
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
}
