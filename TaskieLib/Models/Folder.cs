using System.Collections.Generic;
using System;
using System.ComponentModel;

public class Folder : INotifyPropertyChanged
{
    private string _name;
    private Windows.UI.Color _iconColor;

    public string Name
    {
        get { return _name; }
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public Windows.UI.Color IconColor
    {
        get { return _iconColor; }
        set
        {
            if (_iconColor != value)
            {
                _iconColor = value;
                OnPropertyChanged(nameof(IconColor));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
